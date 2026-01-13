using Discord.Interactions;

namespace Lithium.Bot.Modules;

public sealed class GeneralModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("ping", "Vérifie la latence du bot")]
    public async Task PingAsync()
    {
        await RespondAsync("Pong !");
    }
}