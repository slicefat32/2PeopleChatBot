using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace _2PeopleTB.DAL.Data
{
    public class TelegramBotDbContextFactory : IDesignTimeDbContextFactory<TelegramBotDbContext>
    {
        public TelegramBotDbContext CreateDbContext(string[] args)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var functionsDirectory = Path.Combine(currentDirectory, "2PeopleTB.AzureFunctions");
            if (!Directory.Exists(functionsDirectory))
            {
                functionsDirectory = Path.GetFullPath(Path.Combine(currentDirectory, "..", "2PeopleTB.AzureFunctions"));
            }

            // Читаємо налаштування Azure Functions незалежно від поточної директорії dotnet ef.
            var configuration = new ConfigurationBuilder()
                .SetBasePath(functionsDirectory)
                .AddJsonFile("local.settings.json", optional: false, reloadOnChange: false)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<TelegramBotDbContext>();

            // Azure Functions keeps local configuration in the Values object. In Azure,
            // the same setting is available as the ConnectionStrings__DefaultConnection
            // environment variable.
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? configuration["ConnectionStrings:DefaultConnection"]
                ?? configuration["Values:ConnectionStrings:DefaultConnection"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "The DefaultConnection connection string is missing. Set " +
                    "ConnectionStrings__DefaultConnection, or add " +
                    "Values:ConnectionStrings:DefaultConnection to local.settings.json.");
            }

            optionsBuilder.UseSqlServer(connectionString);

            return new TelegramBotDbContext(optionsBuilder.Options);
        }
    }
}
