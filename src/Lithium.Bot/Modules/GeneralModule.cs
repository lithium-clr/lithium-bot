using Discord;
using Discord.Interactions;
using System.Diagnostics;

namespace Lithium.Bot.Modules;

public sealed class GeneralModule : InteractionModuleBase<SocketInteractionContext>
{
    private static readonly HttpClient HttpClient = new();

    [SlashCommand("ping", "Checks bot latency and status page availability")]
    public async Task PingAsync()
    {
        await DeferAsync();

        var discordLatency = Context.Client.Latency;

        const string websiteUrl = "https://status.lithium.run";
        string siteStatusText;
        long siteLatencyMs;
        var isUp = false;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Head, websiteUrl);
            HttpClient.Timeout = TimeSpan.FromSeconds(5);

            var response = await HttpClient.SendAsync(requestMessage);
            stopwatch.Stop();
            siteLatencyMs = stopwatch.ElapsedMilliseconds;

            if (response.IsSuccessStatusCode)
            {
                isUp = true;
                siteStatusText = $"🟢 Online ({siteLatencyMs}ms)";
            }
            else
            {
                siteStatusText = $"🟠 Error {(int)response.StatusCode}";
            }
        }
        catch (Exception)
        {
            siteStatusText = "🔴 Unreachable";
        }

        var embed = new EmbedBuilder()
            .WithTitle("🏓 System Status")
            .WithUrl(websiteUrl)
            .WithColor(isUp ? Color.Green : Color.Red)
            .AddField("🤖 Bot Gateway", $"`{discordLatency}ms`", true)
            .AddField("🌐 Status Page", $"`{siteStatusText}`", true)
            .WithFooter(new EmbedFooterBuilder
                { Text = "Lithium Systems", IconUrl = Context.Client.CurrentUser.GetAvatarUrl() })
            .WithCurrentTimestamp()
            .Build();
        
        await FollowupAsync(embed: embed);
    }
}