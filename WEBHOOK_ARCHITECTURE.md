# Telegram Bot WebAPI - Webhook Architecture

## Огляд

Це ASP.NET Core WebAPI проєкт, який обробляє Telegram оновлення через **webhook** замість **long polling**.

## Переваги Webhook над Long Polling

| Long Polling (консольний бот) | Webhook (WebAPI) |
|-------------------------------|------------------|
| ❌ Постійне з'єднання | ✅ Запити тільки при новому повідомленні |
| ❌ Працює тільки коли запущено | ✅ Працює 24/7 на сервері |
| ❌ Не масштабується | ✅ Легко масштабувати |
| ❌ Потребує окремого процесу | ✅ Інтегрується в існуючу інфраструктуру |
| ❌ Важко моніторити | ✅ Легко логувати та моніторити |

---

## Архітектура

```
Telegram Server
    ↓
    ↓ POST https://yourdomain.com/api/telegram/webhook
    ↓
TelegramController
    ↓
TelegramUpdateHandler
    ↓
├── RegisteredUsersService
├── MessageHistoryService
└── SQL Server Database
```

---

## Структура проєкту

```
2PeopleTB.WebAPI/
├── Controllers/
│   └── TelegramController.cs        # Webhook endpoint
├── Services/
│   └── TelegramUpdateHandler.cs     # Логіка обробки оновлень
├── Program.cs                       # Налаштування DI та middleware
├── appsettings.json                 # Конфігурація
└── 2PeopleTB.WebAPI.csproj
```

---

## Налаштування

### 1. appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TelegramBotDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "BotConfiguration": {
    "BotToken": "YOUR_BOT_TOKEN",
    "AdminChatIds": [
      123456789,
      987654321
    ],
    "WebhookUrl": "https://yourdomain.com/api/telegram/webhook"
  }
}
```

**Важливо:**
- `WebhookUrl` - публічний HTTPS URL вашого сервера
- Telegram **вимагає HTTPS** (не HTTP!)
- Для локальної розробки використовуйте ngrok або подібні інструменти

### 2. Dependency Injection (Program.cs)

```csharp
// DbContext
builder.Services.AddDbContext<TelegramBotDbContext>(options =>
    options.UseSqlServer(connectionString));

// Telegram Bot Client (Singleton)
builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

// DAL Services (Scoped - новий екземпляр на кожен HTTP запит)
builder.Services.AddScoped<RegisteredUsersService>();
builder.Services.AddScoped<MessageHistoryService>();

// Application Services
builder.Services.AddScoped<TelegramUpdateHandler>();
```

---

## Endpoints

### POST /api/telegram/webhook

**Призначення:** Отримує оновлення від Telegram

**Request:**
```json
{
  "update_id": 123456789,
  "message": {
    "message_id": 123,
    "from": {
      "id": 987654321,
      "username": "john_doe"
    },
    "chat": {
      "id": 987654321,
      "type": "private"
    },
    "text": "Привіт!"
  }
}
```

**Response:**
```
200 OK
```

**Примітка:** Завжди повертаємо 200 OK, навіть якщо є помилка, щоб Telegram не повторював запит.

### GET /api/telegram/health

**Призначення:** Health check endpoint

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2024-07-17T12:30:00Z"
}
```

---

## Локальна розробка з ngrok

Telegram webhook вимагає HTTPS, але локально у вас HTTP. Використовуйте **ngrok**:

### 1. Встановіть ngrok
```bash
# Завантажте з https://ngrok.com/download
# Або через chocolatey:
choco install ngrok
```

### 2. Запустіть WebAPI
```bash
cd 2PeopleTB.WebAPI
dotnet run
```

Бот запуститься на `https://localhost:7001` (або іншому порту)

### 3. Запустіть ngrok
```bash
ngrok http https://localhost:7001
```

Отримаєте щось таке:
```
Forwarding  https://abc123.ngrok.io -> https://localhost:7001
```

### 4. Оновіть appsettings.json
```json
{
  "BotConfiguration": {
    "WebhookUrl": "https://abc123.ngrok.io/api/telegram/webhook"
  }
}
```

### 5. Перезапустіть WebAPI

Бот автоматично встановить webhook при запуску.

---

## Встановлення Webhook вручну

Якщо потрібно встановити webhook вручну:

```bash
curl -X POST "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/setWebhook" \
  -H "Content-Type: application/json" \
  -d '{"url": "https://yourdomain.com/api/telegram/webhook"}'
```

### Перевірка webhook:
```bash
curl "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getWebhookInfo"
```

### Видалення webhook:
```bash
curl -X POST "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/deleteWebhook"
```

---

## Деплой на продакшн

### Варіант 1: Azure App Service

```bash
# Створіть App Service
az webapp create --resource-group myResourceGroup --plan myAppServicePlan --name myTelegramBot --runtime "DOTNET|10.0"

# Опублікуйте
dotnet publish -c Release
az webapp deployment source config-zip --resource-group myResourceGroup --name myTelegramBot --src ./bin/Release/net10.0/publish.zip
```

**URL:** `https://myTelegramBot.azurewebsites.net/api/telegram/webhook`

### Варіант 2: Docker + будь-який хостинг

**Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["2PeopleTB.WebAPI/2PeopleTB.WebAPI.csproj", "2PeopleTB.WebAPI/"]
COPY ["2PeopleTB.DAL/2PeopleTB.DAL.csproj", "2PeopleTB.DAL/"]
RUN dotnet restore "2PeopleTB.WebAPI/2PeopleTB.WebAPI.csproj"
COPY . .
WORKDIR "/src/2PeopleTB.WebAPI"
RUN dotnet build "2PeopleTB.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "2PeopleTB.WebAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "2PeopleTB.WebAPI.dll"]
```

```bash
docker build -t telegram-bot-api .
docker run -d -p 80:80 -p 443:443 telegram-bot-api
```

### Варіант 3: IIS

1. Опублікуйте проєкт:
```bash
dotnet publish -c Release -o ./publish
```

2. Створіть сайт в IIS з SSL сертифікатом
3. Вкажіть шлях до `./publish`
4. Переконайтеся що `.NET 10 Hosting Bundle` встановлено

---

## Логування

### Консольні логи

```csharp
app.Logger.LogInformation("🚀 Telegram Bot WebAPI запущено!");
app.Logger.LogInformation("📡 Webhook URL: {WebhookUrl}", webhookUrl);
```

### Логи обробки запитів

```csharp
_logger.LogInformation("Received update: {UpdateId}", update.Id);
_logger.LogError(ex, "Error processing update {UpdateId}", update.Id);
```

### Додати File Logging (Serilog)

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File
```

**Program.cs:**
```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/bot-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
```

---

## Моніторинг

### Application Insights (Azure)

```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

**appsettings.json:**
```json
{
  "ApplicationInsights": {
    "InstrumentationKey": "YOUR_KEY"
  }
}
```

**Program.cs:**
```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

---

## Тестування

### 1. Локальне тестування з ngrok

```bash
# Terminal 1
dotnet run

# Terminal 2
ngrok http https://localhost:7001
```

Напишіть боту в Telegram - побачите логи в консолі.

### 2. Тестування webhook вручну

```bash
curl -X POST "https://yourdomain.com/api/telegram/webhook" \
  -H "Content-Type: application/json" \
  -d '{
    "update_id": 123,
    "message": {
      "message_id": 1,
      "from": {"id": 123456789, "username": "test"},
      "chat": {"id": 123456789, "type": "private"},
      "text": "/start"
    }
  }'
```

### 3. Health check

```bash
curl "https://yourdomain.com/api/telegram/health"
```

---

## Troubleshooting

### Бот не відповідає

1. **Перевірте webhook:**
```bash
curl "https://api.telegram.org/bot<TOKEN>/getWebhookInfo"
```

Має бути:
```json
{
  "url": "https://yourdomain.com/api/telegram/webhook",
  "has_custom_certificate": false,
  "pending_update_count": 0,
  "last_error_date": 0
}
```

2. **Перевірте логи:**
```bash
dotnet run
# Напишіть боту в Telegram
# Подивіться чи є лог: "Received update: ..."
```

3. **Перевірте HTTPS:**
- Webhook **ОБОВ'ЯЗКОВО** має бути HTTPS
- Сертифікат має бути валідним

### Помилка "Invalid SSL certificate"

**Рішення:** Використовуйте валідний SSL сертифікат (Let's Encrypt, Cloudflare і т.д.)

Для ngrok - це не проблема, ngrok надає валідний сертифікат.

### Webhook встановлюється, але оновлення не приходять

**Причина:** Firewall блокує запити від Telegram.

**Рішення:** Дозвольте IP Telegram у firewall:
```
149.154.160.0/20
91.108.4.0/22
```

---

## Порівняння з консольним ботом

| Аспект | Консольний (2PeopleTelegramBot) | WebAPI (2PeopleTB.WebAPI) |
|--------|----------------------------------|---------------------------|
| **Архітектура** | Long Polling | Webhook |
| **Запуск** | `dotnet run` в консолі | IIS/Azure/Docker |
| **Масштабування** | ❌ Не можна | ✅ Легко (додати ще сервери) |
| **Логування** | Console.WriteLine | ILogger + Serilog + AppInsights |
| **Моніторинг** | ❌ Важко | ✅ Легко (AppInsights, Prometheus) |
| **Деплой** | ❌ Потрібен окремий процес | ✅ Стандартний ASP.NET деплой |
| **DI** | Manual (new ...) | ✅ ASP.NET DI Container |
| **Тестування** | ❌ Важко | ✅ Легко (unit + integration tests) |

---

## Підсумок

✅ **Готово:**
- Webhook endpoint (`POST /api/telegram/webhook`)
- Health check (`GET /api/telegram/health`)
- Dependency Injection
- Логування
- Всі команди бота (connect, disconnect, users, history, chat)
- Збереження історії повідомлень

🚀 **Наступні кроки:**
1. Налаштуйте ngrok для локальної розробки
2. Опублікуйте на Azure/інший хостинг для продакшну
3. Додайте SSL сертифікат
4. Встановіть webhook
5. Тестуйте!
