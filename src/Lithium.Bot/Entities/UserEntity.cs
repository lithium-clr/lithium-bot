using System.ComponentModel.DataAnnotations;

namespace Lithium.Bot.Entities;

public class UserEntity
{
    [Key] public int Id { get; init; }

    public required string UserName { get; set; }
    public ulong DiscordId { get; init; }
    public int Xp { get; set; }
    public int Level { get; set; }
    public string Roles { get; set; } = "Member";
    public DateTime JoinedAt { get; init; } = DateTime.UtcNow;
}