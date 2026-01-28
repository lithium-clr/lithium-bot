using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace Lithium.Bot.Data
{
    public class LithiumContextFactory : IDesignTimeDbContextFactory<LithiumContext>
    {
        public LithiumContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<LithiumContext>();

            string connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
                ?? "Host=localhost;Database=lithium_db;Username=postgres;Password=";

            optionsBuilder.UseNpgsql(connectionString);

            return new LithiumContext(optionsBuilder.Options);
        }
    }
}