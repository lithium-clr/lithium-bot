using System.Reflection;
using System.Runtime.InteropServices;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Lithium.Bot;

public sealed class BotService(
    ILogger<BotService> logger,
    IConfiguration config,
    DiscordSocketClient client,
    IServiceProvider services
) : IHostedService
{
    private readonly InteractionService _interactionService = new(client);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        client.Log += OnLogAsync;
        _interactionService.Log += OnLogAsync;
        client.Ready += OnReadyAsync;
        client.MessageReceived += OnMessageReceivedAsync;
        client.UserJoined += OnUserJoinedAsync;
        client.InteractionCreated += OnInteractionCreatedAsync;

        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), services);

        var token = config["Discord:Token"] 
                    ?? Environment.GetEnvironmentVariable("DISCORD_TOKEN");
        
        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();
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
        logger.LogInformation($"{client.CurrentUser} connected!");

        try
        {
#if DEBUG
            // Using a guild-specific command registration for debug builds is faster.
            await _interactionService.RegisterCommandsToGuildAsync(1451665796009951355);
#else
            await _interactionService.RegisterCommandsGloballyAsync();
#endif
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de l'enregistrement des commandes slash");
        }
        
        await client.SetActivityAsync(new Game("lithium.run", ActivityType.Watching));
    }

    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.Id == client.CurrentUser.Id) return;
    }

    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        var ctx = new SocketInteractionContext(client, interaction);
        await _interactionService.ExecuteCommandAsync(ctx, services);
    }

    private async Task OnUserJoinedAsync(SocketGuildUser user)
    {
        var defaultChannel = user.Guild.TextChannels.FirstOrDefault();
        
        if (defaultChannel is not null)
            await defaultChannel.SendMessageAsync($"Welcome to the server, {user.Mention} !");
    }
}