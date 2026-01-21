using Discord;
using Discord.WebSocket;
using Lithium.Bot.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DiscordSocketClient>(_ => new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers
}));

builder.Services.AddHostedService<BotService>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();