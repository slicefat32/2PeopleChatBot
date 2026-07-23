using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using Telegram.Bot;

namespace _2PeopleTB.AzureFunctions;

public class SetWebhook
{
    private readonly ILogger<SetWebhook> _logger;
    private readonly ITelegramBotClient _botClient;
    private readonly IConfiguration _configuration;

    public SetWebhook(ILogger<SetWebhook> logger, ITelegramBotClient botClient, IConfiguration configuration)
    {
        _logger = logger;
        _botClient = botClient;
        _configuration = configuration;
    }

    [Function("SetWebhook")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "telegram/setwebhook")]
        HttpRequestData req)
    {
        _logger.LogInformation("🔧 Налаштування webhook...");

        try
        {
            // Отримуємо webhook URL з query параметрів або конфігурації
            string? webhookUrl = req.Query["url"];

            if (string.IsNullOrEmpty(webhookUrl))
            {
                // Якщо URL не передано, використовуємо автоматичний
                var functionUrl = req.Url.GetLeftPart(UriPartial.Authority);
                webhookUrl = $"{functionUrl}/api/telegram/webhook";
            }

            await _botClient.SetWebhook(webhookUrl);

            _logger.LogInformation("✅ Webhook встановлено: {WebhookUrl}", webhookUrl);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Webhook встановлено: {webhookUrl}");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Помилка встановлення webhook");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Помилка: {ex.Message}");
            return errorResponse;
        }
    }
}
