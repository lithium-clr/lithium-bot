using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lithium.Bot.Data;

public sealed class LithiumContextFactory : IDesignTimeDbContextFactory<LithiumContext>
{
    public LithiumContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LithiumContext>();

        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
                               ?? "Host=localhost;Database=lithium_db;Username=postgres;Password=";

        optionsBuilder.UseNpgsql(connectionString);

        return new LithiumContext(optionsBuilder.Options);
    }
}