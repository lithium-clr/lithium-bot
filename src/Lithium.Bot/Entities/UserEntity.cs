using System.ComponentModel.DataAnnotations;

namespace Lithium.Bot.Entities;

public class UserEntity
{
    [Key] 
    public int Id { get; set; }
    
    public required string UserName { get; set; }

    public ulong DiscordId { get; set; } 

    public int Xp { get; set; } = 0;

    public int Level { get; set; } = 0;

    public string Roles { get; set; } = "Member";

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}