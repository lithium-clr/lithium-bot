using System.Security.Cryptography;
using Lithium.Bot.Data;
using Lithium.Bot.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lithium.Bot.Services;

public interface ITokenService
{
    Task<string> CreateTokenAsync(string username);
    Task<bool> RemoveTokenAsync(string token);
    Task<bool> ValidateTokenAsync(string token);
}

public class TokenService : ITokenService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IServiceProvider services, ILogger<TokenService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<string> CreateTokenAsync(string username)
    {
        string secureToken = GenerateSecureKey();

        try
        {
            using (var scope = _services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LithiumContext>();

                var tokenEntry = new TokensEntity
                {
                    Token = secureToken,
                    ByUserName = username,
                    CreatedAt = DateTime.UtcNow
                };

                db.Tokens.Add(tokenEntry);
                await db.SaveChangesAsync();

                _logger.LogInformation("Token successfully generated for user: {User}", username);
                return secureToken;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save token for user: {User}", username);
            throw;
        }
    }

    public async Task<bool> RemoveTokenAsync(string token)
    {
        try
        {
            using (var scope = _services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LithiumContext>();
                
                var tokenEntity = await db.Tokens.FirstOrDefaultAsync(t => t.Token == token);
                
                if (tokenEntity != null)
                {
                    db.Tokens.Remove(tokenEntity);
                    await db.SaveChangesAsync();
                    _logger.LogInformation("Token successfully removed.");
                    return true;
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while removing token.");
            return false;
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        using (var scope = _services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LithiumContext>();
            return await db.Tokens.AnyAsync(t => t.Token == token);
        }
    }

    private string GenerateSecureKey()
    {
        var buffer = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(buffer);
        }
        return Convert.ToBase64String(buffer)
            .Replace("+", "")
            .Replace("/", "")
            .TrimEnd('=');
    }
}