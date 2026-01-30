using Discord;
using Discord.Interactions;
using Lithium.Bot.Data;
using Lithium.Bot.Entities;
using Lithium.Bot.Services;
using Microsoft.EntityFrameworkCore;

namespace Lithium.Bot.Modules;

[Group("admin", "Administrative commands")]
[RequireUserPermission(GuildPermission.Administrator)]
[RequireContext(ContextType.Guild)]
public sealed class AdminModule(IServiceProvider services) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("purge", "Clears messages from the channel")]
    [RequireBotPermission(GuildPermission.ManageMessages)]
    public async Task PurgeAsync(
        [Summary("amount", "Number of messages to delete (1-100)")] [MinValue(1)] [MaxValue(100)]
        int amount)
    {
        await DeferAsync(ephemeral: true);

        var messages = await Context.Channel.GetMessagesAsync(amount).FlattenAsync();
        var validMessages = messages.Where(x => (DateTimeOffset.UtcNow - x.Timestamp).TotalDays < 14).ToList();

        if (Context.Channel is ITextChannel channel)
        {
            await channel.DeleteMessagesAsync(validMessages);
            await FollowupAsync($"🗑️ **{validMessages.Count}** messages were deleted.", ephemeral: true);
        }
    }

    [SlashCommand("ban", "Bans a user")]
    [RequireBotPermission(GuildPermission.BanMembers)]
    public async Task BanAsync(
        [Summary("user", "The user to be banned")]
        IUser user,
        [Summary("reason", "Reason for the ban")]
        string reason = "No reason provided",
        [Summary("prune_period", "Delete messages from how long ago?")]
        TimeFrame timeframe = TimeFrame.Hour)
    {
        var pruneDays = timeframe == TimeFrame.Week ? 7 : 1;

        await Context.Guild.AddBanAsync(user, pruneDays, reason);
        await RespondAsync($"🔨 User **{user.Username}** has been banned. Reason: {reason}");
    }

    [SlashCommand("webpanel", "Panel web Administration for bot")]
    [RequireBotPermission(GuildPermission.Administrator)]
    public async Task WebPanelAsync()
    {
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<LithiumContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var user = await db.Users.FirstOrDefaultAsync(u => u.DiscordId == Context.User.Id);

        if (user is not { Roles: "Master" })
        {
            await RespondAsync("❌ Access denied. Only users with the 'Master' role can generate tokens.",
                ephemeral: true);
            return;
        }

        try
        {
            var generatedToken = await tokenService.CreateTokenAsync(Context.User.Username);

            var builder = new EmbedBuilder()
                .WithTitle("🚀 Web Panel Administration")
                .WithDescription($"Hello **{Context.User.Username}**, your access has been generated!")
                .AddField("Your Unique Token", $"```text\n{generatedToken}\n```")
                .AddField("Access Link", "[Click here to login](https://lithiumbotpanel.example.com)")
                .WithColor(Color.Blue)
                .WithFooter(footer => footer.Text = "Do not share this token with anyone.")
                .WithCurrentTimestamp();

            await RespondAsync(embed: builder.Build(), ephemeral: true);
        }
        catch (Exception ex)
        {
            await RespondAsync("⚠️ An error occurred while generating your token. Please try again later.",
                ephemeral: true);
        }
    }
    
    public enum TimeFrame
    {
        Hour,
        Day,
        Week
    }
}