using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lithium.Bot.Services;

public sealed class BotService : IHostedService
{
    private readonly ILogger<BotService> _logger;
    private readonly IConfiguration _config;
    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _services;
    private readonly InteractionService _interactionService;

    // Flag to prevent re-registering commands on quick reconnections (Performance/Rate Limit)
    private bool _isInitialized = false;

    public BotService(
        ILogger<BotService> logger,
        IConfiguration config,
        DiscordSocketClient client,
        IServiceProvider services)
    {
        _logger = logger;
        _config = config;
        _client = client;
        _services = services;
        // Passing _client.Rest improves performance slightly for interactions
        _interactionService = new InteractionService(_client.Rest);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            string? token;

#if DEBUG
            token = _config["Discord:Token"];
#else
        // Release: token from environment variable (Docker / CI)
        token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
#endif

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException(
                    "Discord token not found. " +
                    "DEBUG expects appsettings.json (Discord:Token). " +
                    "RELEASE expects environment variable DISCORD_TOKEN."
                );
            }

            _client.Log += OnLogAsync;
            _interactionService.Log += OnLogAsync;
            _client.Ready += OnReadyAsync;

            
             //_client.MessageReceived += OnMessageReceivedAsync; 

            _client.UserJoined += OnUserJoinedAsync;
            _client.InteractionCreated += OnInteractionCreatedAsync;

            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Critical failure starting BotService.");
            throw; 
        }
    }


    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.LogoutAsync();
        await _client.StopAsync();
    }

    private Task OnLogAsync(LogMessage log)
    {
        var severity = log.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };

        _logger.Log(severity, log.Exception, "[Discord] {Source}: {Message}", log.Source, log.Message);
        return Task.CompletedTask;
    }

    private async Task OnReadyAsync()
    {
        // If already initialized, do nothing. Saves API calls on reconnect.
        if (_isInitialized) return;

        _logger.LogInformation("{User} connected!", _client.CurrentUser);

        try
        {
            var guildIdStr = _config["Discord:DebugGuildId"];

            if (ulong.TryParse(guildIdStr, out var guildId))
            {
#if DEBUG
                // Fast registration for development server
                _logger.LogInformation("DEBUG Mode: Registering commands to Guild {GuildId}...", guildId);
                await _interactionService.RegisterCommandsToGuildAsync(guildId);
#else
                // Global registration (can take up to 1 hour to propagate)
                _logger.LogInformation("RELEASE Mode: Registering commands Globally...");
                await _interactionService.RegisterCommandsGloballyAsync();
#endif
            }
            else
            {
                _logger.LogWarning("DebugGuildId not configured correctly in json.");
            }

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering Slash commands.");
        }

        await _client.SetActivityAsync(new Game("lithium.run", ActivityType.Watching));
    }

    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        try
        {
            var ctx = new SocketInteractionContext(_client, interaction);
            await _interactionService.ExecuteCommandAsync(ctx, _services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing interaction.");

            if (interaction.Type == InteractionType.ApplicationCommand)
            {
                // Ephemeral message to user (only visible to them)
                await interaction.RespondAsync("An internal error occurred while processing your command.", ephemeral: true);
            }
        }
    }

    private async Task OnUserJoinedAsync(SocketGuildUser user)
    {
        var defaultChannel = user.Guild.TextChannels.FirstOrDefault();

        if (defaultChannel is not null)
        {
            // Check permissions to avoid 50013 errors
            var permissions = user.Guild.CurrentUser.GetPermissions(defaultChannel);
            if (permissions.SendMessages)
            {
                await defaultChannel.SendMessageAsync($"Welcome to the server, {user.Mention}!");
            }
        }
    }
}