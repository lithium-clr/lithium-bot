using Microsoft.EntityFrameworkCore;
using Lithium.Bot.Entities; 
namespace Lithium.Bot.Data;

public class LithiumContext : DbContext
{
    public LithiumContext(DbContextOptions<LithiumContext> options) : base(options)
    {
    }
    public DbSet<UserEntity> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserEntity>()
            .HasIndex(u => u.DiscordId)
            .IsUnique();
    }
}