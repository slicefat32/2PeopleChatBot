using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using Telegram.Bot;
using _2PeopleTB.DAL.Data;
using _2PeopleTB.DAL.Services;
using _2PeopleTB.AzureFunctions.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Configuration
var botToken = builder.Configuration["BotConfiguration:BotToken"]!;
var adminChatIds = builder.Configuration.GetSection("BotConfiguration:AdminChatIds").Get<List<long>>() ?? new List<long>();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

Console.WriteLine("Connection:");
Console.WriteLine(connectionString);
// Database
builder.Services.AddDbContext<TelegramBotDbContext>(options =>
    options.UseSqlServer(connectionString));

// Telegram Bot Client
builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

// DAL Services
builder.Services.AddScoped<RegisteredUsersService>();
builder.Services.AddScoped<MessageHistoryService>();

// Application Services
builder.Services.AddScoped<TelegramUpdateHandler>(sp =>
{
    var botClient = sp.GetRequiredService<ITelegramBotClient>();
    var usersService = sp.GetRequiredService<RegisteredUsersService>();
    var messageHistoryService = sp.GetRequiredService<MessageHistoryService>();

    return new TelegramUpdateHandler(botClient, usersService, messageHistoryService, adminChatIds);
});

// Logging
builder.Services.AddLogging();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<TelegramBotDbContext>();

        Console.WriteLine(connectionString);

        await dbContext.Database.EnsureCreatedAsync();

        Console.WriteLine("DB OK");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
        throw;
    }
}

app.Run();
