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

        var token = config["Discord:Token"] 
                    ?? Environment.GetEnvironmentVariable("DISCORD_TOKEN");
        
        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private Task OnLogAsync(LogMessage log)
    {
        logger.LogInformation(log.ToString());
        return Task.CompletedTask;
    }

    private Task OnReadyAsync()
    {
        logger.LogInformation($"{client.CurrentUser} connected!");
        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.Id == client.CurrentUser.Id) return;

        if (message.Content == "!ping")
            await message.Channel.SendMessageAsync($"Pong! 🏓 ({client.Latency}ms)");
    }
}