using System.Collections.Concurrent;
using Lithium.Bot.Data;
using Lithium.Bot.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection; 
using Microsoft.Extensions.Logging;

namespace Lithium.Bot.Services;

public interface IUserService
{
    Task EnsureUserExistsAsync(ulong discordId, string username);
}

public class UserService : IUserService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<UserService> _logger;

    private readonly ConcurrentDictionary<ulong, bool> _knownUsersCache = new();

    public UserService(IServiceProvider services, ILogger<UserService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task EnsureUserExistsAsync(ulong discordId, string username)
    {

        if (_knownUsersCache.ContainsKey(discordId))
        {
            return; 
        }

        try
        {
            using (var scope = _services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LithiumContext>();

                var userExists = await db.Users.AnyAsync(u => u.DiscordId == discordId);

                if (!userExists)
                {
                    var newUser = new UserEntity
                    {
                        DiscordId = discordId,
                        NickName = username,
                        JoinedAt = DateTime.UtcNow,
                        Xp = 0
                       
                    };

                    db.Users.Add(newUser);
                    await db.SaveChangesAsync();

                    _logger.LogInformation("New user: {Username} ({Id})", username, discordId);
                }
            }


            _knownUsersCache.TryAdd(discordId, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in register {Id} in db.", discordId);
        }
    }
}