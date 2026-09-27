using _2PeopleTB.DAL.Data;
using _2PeopleTB.DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace _2PeopleTB.DAL.Services;

public class ProcessedUpdatesService
{
    private readonly TelegramBotDbContext _context;

    public ProcessedUpdatesService(TelegramBotDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryStartProcessingAsync(int updateId, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.ProcessedTelegramUpdates.Add(new ProcessedTelegramUpdate { UpdateId = updateId });
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            _context.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task CompleteProcessingAsync(int updateId, CancellationToken cancellationToken = default)
    {
        var update = await _context.ProcessedTelegramUpdates.FindAsync([updateId], cancellationToken);
        if (update is null)
            return;

        update.ProcessedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AbandonProcessingAsync(int updateId, CancellationToken cancellationToken = default)
    {
        var update = await _context.ProcessedTelegramUpdates.FindAsync([updateId], cancellationToken);
        if (update is null)
            return;

        _context.ProcessedTelegramUpdates.Remove(update);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
