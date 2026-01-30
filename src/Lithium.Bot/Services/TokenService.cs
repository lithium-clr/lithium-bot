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

public sealed class TokenService(IServiceProvider services, ILogger<TokenService> logger) : ITokenService
{
    public async Task<string> CreateTokenAsync(string username)
    {
        var secureToken = GenerateSecureKey();

        try
        {
            using var scope = services.CreateScope();
            
            var db = scope.ServiceProvider.GetRequiredService<LithiumContext>();

            var tokenEntry = new TokensEntity
            {
                Token = secureToken,
                ByUserName = username,
                CreatedAt = DateTime.UtcNow
            };

            db.Tokens.Add(tokenEntry);
            await db.SaveChangesAsync();

            logger.LogInformation("Token successfully generated for user: {User}", username);
            return secureToken;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save token for user: {User}", username);
            throw;
        }
    }

    public async Task<bool> RemoveTokenAsync(string token)
    {
        try
        {
            using var scope = services.CreateScope();
            
            var db = scope.ServiceProvider.GetRequiredService<LithiumContext>();
            var tokenEntity = await db.Tokens.FirstOrDefaultAsync(t => t.Token == token);

            if (tokenEntity is null) 
                return false;
            
            db.Tokens.Remove(tokenEntity);
            await db.SaveChangesAsync();
            logger.LogInformation("Token successfully removed.");
                
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while removing token.");
            return false;
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        using var scope = services.CreateScope();
        
        var db = scope.ServiceProvider.GetRequiredService<LithiumContext>();
        return await db.Tokens.AnyAsync(t => t.Token == token);
    }

    private static string GenerateSecureKey()
    {
        var buffer = new byte[32];
        
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(buffer);
        
        return Convert.ToBase64String(buffer)
            .Replace("+", "")
            .Replace("/", "")
            .TrimEnd('=');
    }
}