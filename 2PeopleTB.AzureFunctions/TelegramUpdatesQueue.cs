using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using _2PeopleTB.AzureFunctions.Services;

namespace _2PeopleTB.AzureFunctions;

public class TelegramUpdatesQueue
{
    private readonly ILogger<TelegramUpdatesQueue> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public TelegramUpdatesQueue(ILogger<TelegramUpdatesQueue> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    [Function("TelegramUpdatesQueue")]
    public async Task Run(
        [QueueTrigger("telegram-updates", Connection = "AzureWebJobsStorage")] string updateJson,
        CancellationToken cancellationToken)
    {
        Update? update = null;
        try
        {
            update = JsonSerializer.Deserialize<Update>(updateJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (update is null)
            {
                _logger.LogWarning("Некоректне оновлення в черзі Telegram");
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var updateHandler = scope.ServiceProvider.GetRequiredService<TelegramUpdateHandler>();

            await updateHandler.HandleUpdateAsync(update, cancellationToken);
            _logger.LogInformation("✅ Update {UpdateId} оброблено з черги", update.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Не вдалося обробити Telegram Update {UpdateId}", update?.Id);
            throw;
        }
    }
}
