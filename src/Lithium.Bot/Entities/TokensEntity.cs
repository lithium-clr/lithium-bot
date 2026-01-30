using System.ComponentModel.DataAnnotations;

namespace Lithium.Bot.Entities;

public class TokensEntity
{
    [Key]
    public int Id { get; init; }

    public required string Token { get; set; }
    public required string ByUserName { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}