using Discord;
using Discord.WebSocket;
using Lithium.Bot.Services;
using Lithium.Bot.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("The variable 'DB_CONNECTION' not found");
}

builder.Services.AddDbContext<LithiumContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSingleton<DiscordSocketClient>(_ => new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers
}));
builder.Services.AddSingleton<IUserService, UserService>();

builder.Services.AddHostedService<BotService>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();