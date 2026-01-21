using Discord;
using Discord.Interactions;

namespace Lithium.Bot.Modules
{
    [Group("admin", "Administrative commands")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireContext(ContextType.Guild)]
    public class AdminModule : InteractionModuleBase<SocketInteractionContext>
    {
        public enum TimeFrame
        {
            Hour,
            Day,
            Week
        }

        [SlashCommand("purge", "Clears messages from the channel")]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        public async Task PurgeAsync(
            [Summary("amount", "Number of messages to delete (1-100)")]
            [MinValue(1)] [MaxValue(100)] int amount)
        {
            await DeferAsync(ephemeral: true);

            var messages = await Context.Channel.GetMessagesAsync(amount).FlattenAsync();

            var validMessages = messages.Where(x => (DateTimeOffset.UtcNow - x.Timestamp).TotalDays < 14);

            if (Context.Channel is ITextChannel channel)
            {
                await channel.DeleteMessagesAsync(validMessages);
                await FollowupAsync($"🗑️ **{validMessages.Count()}** messages were deleted.", ephemeral: true);
            }
        }

        [SlashCommand("ban", "Bans a user")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task BanAsync(
            [Summary("user", "The user to be banned")] IUser user,
            [Summary("reason", "Reason for the ban")] string reason = "No reason provided",
            [Summary("prune_period", "Delete messages from how long ago?")] TimeFrame timeframe = TimeFrame.Hour)
        {
            int pruneDays = timeframe == TimeFrame.Week ? 7 : 1;

            await Context.Guild.AddBanAsync(user, pruneDays, reason);
            await RespondAsync($"🔨 User **{user.Username}** has been banned. Reason: {reason}");
        }
    }
}