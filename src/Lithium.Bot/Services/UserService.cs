using System.Collections.Concurrent;
using Lithium.Bot.Data;
using Lithium.Bot.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lithium.Bot.Services;

public interface IUserService
{
    Task EnsureUserExistsAsync(ulong discordId, string username);
}

public sealed class UserService(IServiceProvider services, ILogger<UserService> logger) : IUserService
{
    private readonly ConcurrentDictionary<ulong, bool> _knownUsersCache = new();

    public async Task EnsureUserExistsAsync(ulong discordId, string username)
    {
        if (_knownUsersCache.ContainsKey(discordId)) return;

        try
        {
            using (var scope = services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LithiumContext>();

                var userExists = await db.Users.AnyAsync(u => u.DiscordId == discordId);

                if (!userExists)
                {
                    var newUser = new UserEntity
                    {
                        DiscordId = discordId,
                        UserName = username,
                        JoinedAt = DateTime.UtcNow,
                        Xp = 0
                    };

                    db.Users.Add(newUser);
                    await db.SaveChangesAsync();

                    logger.LogInformation("New user: {Username} ({Id})", username, discordId);
                }
            }

            _knownUsersCache.TryAdd(discordId, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in register {Id} in db.", discordId);
        }
    }
}