using Azure.Monitor.OpenTelemetry.Exporter;
using Azure.Storage.Queues;
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
var botToken = builder.Configuration["BotConfiguration:BotToken"]
               ?? builder.Configuration["BotConfiguration__BotToken"]
               ?? Environment.GetEnvironmentVariable("BotConfiguration_BotToken")!;

var adminChatIds = builder.Configuration.GetSection("BotConfiguration:AdminChatIds").Get<List<long>>() ?? new List<long>();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
var storageConnectionString = builder.Configuration["AzureWebJobsStorage"]!;

Console.WriteLine("Connection:");
Console.WriteLine(connectionString);
// Database
builder.Services.AddDbContext<TelegramBotDbContext>(options =>
    options.UseSqlServer(connectionString));

// Telegram Bot Client
builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
builder.Services.AddSingleton(new QueueClient(storageConnectionString, "telegram-updates"));

// DAL Services
builder.Services.AddScoped<RegisteredUsersService>();
builder.Services.AddScoped<MessageHistoryService>();
builder.Services.AddScoped<ProcessedUpdatesService>();

// Application Services
builder.Services.AddScoped<TelegramUpdateHandler>(sp =>
{
    var botClient = sp.GetRequiredService<ITelegramBotClient>();
    var usersService = sp.GetRequiredService<RegisteredUsersService>();
    var messageHistoryService = sp.GetRequiredService<MessageHistoryService>();
    var processedUpdatesService = sp.GetRequiredService<ProcessedUpdatesService>();

    return new TelegramUpdateHandler(botClient, usersService, messageHistoryService, processedUpdatesService, adminChatIds);
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

        var databaseCreated = await dbContext.Database.EnsureCreatedAsync();
        if (!databaseCreated)
        {
            // EnsureCreated does not add new tables to an existing database.
            await dbContext.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'[ProcessedTelegramUpdates]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [ProcessedTelegramUpdates] (
                        [UpdateId] int NOT NULL,
                        [ProcessedAt] datetime2 NULL,
                        CONSTRAINT [PK_ProcessedTelegramUpdates] PRIMARY KEY ([UpdateId])
                    );
                END
                """);
        }

        Console.WriteLine("DB OK");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
        throw;
    }
}

app.Run();
