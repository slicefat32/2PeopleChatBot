using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Telegram.Bot.Types;
using _2PeopleTB.AzureFunctions.Services;

namespace _2PeopleTB.AzureFunctions;

public class TelegramWebhook
{
    private readonly ILogger<TelegramWebhook> _logger;
    private readonly TelegramUpdateHandler _updateHandler;

    public TelegramWebhook(ILogger<TelegramWebhook> logger, TelegramUpdateHandler updateHandler)
    {
        _logger = logger;
        _updateHandler = updateHandler;
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

            // Десеріалізуємо Update
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

            // Обробляємо оновлення
            await _updateHandler.HandleUpdateAsync(update, CancellationToken.None);

            _logger.LogInformation("✅ Update оброблено успішно");

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
