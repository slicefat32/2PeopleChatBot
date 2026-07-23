using _2PeopleTB.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace _2PeopleTB.DAL.Data
{
    public class TelegramBotDbContext : DbContext
    {
        public TelegramBotDbContext(DbContextOptions<TelegramBotDbContext> options) 
            : base(options)
        {
        }

        public DbSet<RegisteredUser> RegisteredUsers { get; set; }
        public DbSet<MessageHistory> MessageHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RegisteredUser>(entity =>
            {
                entity.HasKey(e => e.ChatId);
                entity.Property(e => e.ChatId).ValueGeneratedNever(); // Не використовувати автоінкремент
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.RegisteredAt).IsRequired();
            });

            modelBuilder.Entity<MessageHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.FromChatId).IsRequired();
                entity.Property(e => e.ToChatId).IsRequired();
                entity.Property(e => e.MessageId).IsRequired();
                entity.Property(e => e.MessageType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TextContent).HasMaxLength(4096);
                entity.Property(e => e.FileId).HasMaxLength(200);
                entity.Property(e => e.SentAt).IsRequired();

                entity.HasIndex(e => e.FromChatId);
                entity.HasIndex(e => e.ToChatId);
                entity.HasIndex(e => e.SentAt);
            });
        }
    }
}
