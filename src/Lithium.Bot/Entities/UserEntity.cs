using System.ComponentModel.DataAnnotations;

namespace Lithium.Bot.Entities;

public class UserEntity
{
    [Key] 
    public int Id { get; set; }
    
    public string NickName { get; set; }

    public ulong DiscordId { get; set; } 

    public int Xp { get; set; } = 0; //test 

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}