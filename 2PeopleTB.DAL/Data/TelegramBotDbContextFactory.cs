using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace _2PeopleTB.DAL.Data
{
    public class TelegramBotDbContextFactory : IDesignTimeDbContextFactory<TelegramBotDbContext>
    {
        public TelegramBotDbContext CreateDbContext(string[] args)
        {
            // Читаємо appsettings.json з проєкту-стартапу
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "2PeopleTelegramBot"))
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<TelegramBotDbContext>();

            // Беремо connection string з appsettings.json
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(connectionString);

            return new TelegramBotDbContext(optionsBuilder.Options);
        }
    }
}
