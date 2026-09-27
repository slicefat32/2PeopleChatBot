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

            // Беремо connection string з appsettings.json
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(connectionString);

            return new TelegramBotDbContext(optionsBuilder.Options);
        }
    }
}
