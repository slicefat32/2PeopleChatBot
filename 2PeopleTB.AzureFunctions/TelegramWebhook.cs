using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Telegram.Bot.Types;

namespace _2PeopleTB.AzureFunctions;

public class TelegramWebhook
{
    private readonly ILogger<TelegramWebhook> _logger;
    private readonly QueueClient _updatesQueue;

    public TelegramWebhook(ILogger<TelegramWebhook> logger, QueueClient updatesQueue)
    {
        _logger = logger;
        _updatesQueue = updatesQueue;
    }

    [Function("TelegramWebhook")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "telegram/webhook")]
        HttpRequestData req)
    {
        _logger.LogInformation("📩 Отримано webhook від Telegram");

        try
        {
            // Зчитуємо тіло запиту
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Перевіряємо формат до постановки в чергу, щоб не повторювати некоректні запити.
            var update = JsonSerializer.Deserialize<Update>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (update == null)
            {
                _logger.LogWarning("⚠️ Не вдалося десеріалізувати Update");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid update");
                return badResponse;
            }

            await _updatesQueue.CreateIfNotExistsAsync();
            await _updatesQueue.SendMessageAsync(requestBody);
            _logger.LogInformation("✅ Update {UpdateId} додано до черги", update.Id);

            var response = req.CreateResponse(HttpStatusCode.OK);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Помилка обробки webhook");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Internal server error");
            return errorResponse;
        }
    }
}
