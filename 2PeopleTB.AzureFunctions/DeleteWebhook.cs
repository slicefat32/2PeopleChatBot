using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Telegram.Bot;

namespace _2PeopleTB.AzureFunctions;

public class DeleteWebhook
{
    private readonly ILogger<DeleteWebhook> _logger;
    private readonly ITelegramBotClient _botClient;

    public DeleteWebhook(ILogger<DeleteWebhook> logger, ITelegramBotClient botClient)
    {
        _logger = logger;
        _botClient = botClient;
    }

    [Function("DeleteWebhook")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "telegram/deletewebhook")]
        HttpRequestData req)
    {
        _logger.LogInformation("🗑️ Видалення webhook...");

        try
        {
            await _botClient.DeleteWebhook();

            _logger.LogInformation("✅ Webhook видалено");

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("Webhook видалено успішно");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Помилка видалення webhook");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Помилка: {ex.Message}");
            return errorResponse;
        }
    }
}
