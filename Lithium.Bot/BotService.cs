using System.Runtime.InteropServices;
using Discord;
using Discord.WebSocket;

namespace Lithium.Bot;

public sealed class BotService(
    ILogger<BotService> logger,
    IConfiguration config,
    DiscordSocketClient client
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        client.Log += OnLogAsync;
        client.Ready += OnReadyAsync;
        client.MessageReceived += OnMessageReceivedAsync;
        client.UserJoined += OnUserJoinedAsync;

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
        await client.SetActivityAsync(new Game("lithium.run", ActivityType.Watching));
    }

    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.Id == client.CurrentUser.Id) return;

        if (message.Content == "!ping")
            await message.Channel.SendMessageAsync("Pong!");
    }

    private async Task OnUserJoinedAsync(SocketGuildUser user)
    {
        var defaultChannel = user.Guild.TextChannels.FirstOrDefault();
        
        if (defaultChannel is not null)
            await defaultChannel.SendMessageAsync($"Welcome to the server, {user.Mention} !");
    }
}