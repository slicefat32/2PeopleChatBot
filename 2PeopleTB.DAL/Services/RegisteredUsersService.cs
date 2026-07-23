using _2PeopleTB.DAL.Data;
using _2PeopleTB.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace _2PeopleTB.DAL.Services
{
    public class RegisteredUsersService
    {
        private readonly TelegramBotDbContext _context;

        public RegisteredUsersService(TelegramBotDbContext context)
        {
            _context = context;
        }

        public async Task<RegisteredUser?> GetUserAsync(long chatId)
        {
            return await _context.RegisteredUsers.FindAsync(chatId);
        }

        public async Task<List<RegisteredUser>> GetAllUsersAsync()
        {
            return await _context.RegisteredUsers.ToListAsync();
        }

        public async Task<bool> AddUserAsync(long chatId, string username)
        {
            var existingUser = await _context.RegisteredUsers.FindAsync(chatId);
            if (existingUser != null)
            {
                return false;
            }

            var user = new RegisteredUser
            {
                ChatId = chatId,
                Username = username,
                RegisteredAt = DateTime.UtcNow
            };

            _context.RegisteredUsers.Add(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateUsernameAsync(long chatId, string username)
        {
            var user = await _context.RegisteredUsers.FindAsync(chatId);
            if (user == null)
            {
                return false;
            }

            user.Username = username;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UserExistsAsync(long chatId)
        {
            return await _context.RegisteredUsers.AnyAsync(u => u.ChatId == chatId);
        }
    }
}
