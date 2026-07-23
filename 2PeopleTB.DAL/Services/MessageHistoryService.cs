using _2PeopleTB.DAL.Data;
using _2PeopleTB.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace _2PeopleTB.DAL.Services
{
    public class MessageHistoryService
    {
        private readonly TelegramBotDbContext _context;

        public MessageHistoryService(TelegramBotDbContext context)
        {
            _context = context;
        }

        public async Task SaveMessageAsync(long fromChatId, long toChatId, int messageId, string messageType, string? textContent = null, string? fileId = null)
        {
            var messageHistory = new MessageHistory
            {
                FromChatId = fromChatId,
                ToChatId = toChatId,
                MessageId = messageId,
                MessageType = messageType,
                TextContent = textContent,
                FileId = fileId,
                SentAt = DateTime.UtcNow
            };

            _context.MessageHistories.Add(messageHistory);
            await _context.SaveChangesAsync();
        }

        public async Task<List<MessageHistory>> GetMessageHistoryBetweenUsersAsync(long chatId1, long chatId2, int count = 50)
        {
            return await _context.MessageHistories
                .Where(m => (m.FromChatId == chatId1 && m.ToChatId == chatId2) ||
                            (m.FromChatId == chatId2 && m.ToChatId == chatId1))
                .OrderByDescending(m => m.SentAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<MessageHistory>> GetUserMessageHistoryAsync(long chatId, int count = 50)
        {
            return await _context.MessageHistories
                .Where(m => m.FromChatId == chatId || m.ToChatId == chatId)
                .OrderByDescending(m => m.SentAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetTotalMessagesCountAsync(long chatId)
        {
            return await _context.MessageHistories
                .Where(m => m.FromChatId == chatId || m.ToChatId == chatId)
                .CountAsync();
        }
    }
}
