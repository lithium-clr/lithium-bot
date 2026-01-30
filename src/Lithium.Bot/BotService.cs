using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Reflection;
using Lithium.Bot.Services;

namespace Lithium.Bot;

public sealed class BotService(
    ILogger<BotService> logger,
    IConfiguration config,
    IUserService userService,
    DiscordSocketClient client,
    IServiceProvider services
) : IHostedService
{
    private InteractionService _interactionService = null!;

    // Flag to prevent re-registering commands on quick reconnections (Performance/Rate Limit)
    private bool _isInitialized;

    // Passing _client.Rest improves performance slightly for interactions

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Starting BotService...");

            _interactionService = new InteractionService(client.Rest);

            var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");

            client.Log += OnLogAsync;
            _interactionService.Log += OnLogAsync;
            client.Ready += OnReadyAsync;

            client.MessageReceived += OnMessageReceivedAsync;
            client.UserJoined += OnUserJoinedAsync;
            client.InteractionCreated += OnInteractionCreatedAsync;

            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), services);

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            logger.LogInformation("BotService started successfully!");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Critical failure starting BotService.");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await client.LogoutAsync();
        await client.StopAsync();
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

        logger.Log(severity, log.Exception, "[Discord] {Source}: {Message}", log.Source, log.Message);
        return Task.CompletedTask;
    }

    private async Task OnReadyAsync()
    {
        if (_isInitialized) return;

        logger.LogInformation("{User} connected!", client.CurrentUser);

        try
        {
#if DEBUG
            var debugGuildString = Environment.GetEnvironmentVariable("DEBUG_GUILD");

            if (!string.IsNullOrEmpty(debugGuildString))
            {
                var debugGuildId = ulong.Parse(debugGuildString);
                await _interactionService.RegisterCommandsToGuildAsync(debugGuildId);
            }
#else
            await _interactionService.RegisterCommandsGloballyAsync();
#endif

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering Slash commands.");
        }

        await client.SetActivityAsync(new Game("lithium.run", ActivityType.Watching));
    }

    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        try
        {
            var ctx = new SocketInteractionContext(client, interaction);
            await _interactionService.ExecuteCommandAsync(ctx, services);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing interaction.");

            if (interaction.Type is InteractionType.ApplicationCommand)
            {
                // Ephemeral message to user (only visible to them)
                await interaction.RespondAsync("An internal error occurred while processing your command.",
                    ephemeral: true);
            }
        }
    }

    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.IsBot) return;
        await (_ = userService.EnsureUserExistsAsync(message.Author.Id, message.Author.Username));
    }

    private async Task OnUserJoinedAsync(SocketGuildUser user)
    {
        var channel = user.Guild.SystemChannel
                      ?? user.Guild.TextChannels.FirstOrDefault(c =>
                          user.Guild.CurrentUser.GetPermissions(c).SendMessages);

        if (channel is not null)
        {
            var embed = new EmbedBuilder
            {
                Title = $"Welcome to {user.Guild.Name}! 🚀",
                Description = $"Hey {user.Mention}, we are thrilled to have you here!\n" +
                              "Please make sure to read the rules and introduce yourself.",
                Color = Color.Gold,
                Timestamp = DateTimeOffset.Now
            };

            embed.WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());

            embed.WithFooter(footer =>
            {
                footer.Text = $"Member #{user.Guild.MemberCount}";
                footer.IconUrl = user.Guild.IconUrl;
            });

            await channel.SendMessageAsync(text: $"Welcome, {user.Mention}!", embed: embed.Build());
        }
    }
}