using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Telegram.Bot;

namespace _2PeopleTB.AzureFunctions;

public class GetWebhookInfo
{
    private readonly ILogger<GetWebhookInfo> _logger;
    private readonly ITelegramBotClient _botClient;

    public GetWebhookInfo(ILogger<GetWebhookInfo> logger, ITelegramBotClient botClient)
    {
        _logger = logger;
        _botClient = botClient;
    }

    [Function("GetWebhookInfo")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "telegram/webhookinfo")]
        HttpRequestData req)
    {
        _logger.LogInformation("ℹ️ Отримання інформації про webhook...");

        try
        {
            var webhookInfo = await _botClient.GetWebhookInfo();

            var info = new
            {
                Url = webhookInfo.Url,
                HasCustomCertificate = webhookInfo.HasCustomCertificate,
                PendingUpdateCount = webhookInfo.PendingUpdateCount,
                LastErrorDate = webhookInfo.LastErrorDate,
                LastErrorMessage = webhookInfo.LastErrorMessage,
                MaxConnections = webhookInfo.MaxConnections,
                AllowedUpdates = webhookInfo.AllowedUpdates
            };

            _logger.LogInformation("✅ Інформація отримана: {Url}", webhookInfo.Url);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(JsonSerializer.Serialize(info, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            }));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Помилка отримання інформації про webhook");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Помилка: {ex.Message}");
            return errorResponse;
        }
    }
}
