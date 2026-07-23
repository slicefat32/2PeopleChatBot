# Azure Functions Webhook Architecture

## Огляд

Проект `2PeopleTB.AzureFunctions` реалізує webhook-based архітектуру для Telegram бота, на відміну від polling-based підходу в консольній application.

## Ключові відмінності

### Консольний бот (2PeopleTelegramBot)
- **Polling**: Бот постійно запитує Telegram API про нові повідомлення
- **Працює постійно**: Процес повинен бути завжди запущеним
- **Конфігурація**: `appsettings.json`
- **Використання**: Локальна розробка, тестування

### Azure Functions Webhook (2PeopleTB.AzureFunctions)
- **Webhook**: Telegram викликає ваш endpoint при новому повідомленні
- **Serverless**: Запускається тільки коли приходить повідомлення
- **Конфігурація**: `local.settings.json` (локально) / Application Settings (Azure)
- **Використання**: Production, масштабування

## Конфігурація

### Локальна розробка

В Azure Functions конфігурація зберігається в `local.settings.json` у форматі **environment variables**:

```json
{
  "IsEncrypted": false,
  "Values": {
	"AzureWebJobsStorage": "UseDevelopmentStorage=true",
	"FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
	"BotConfiguration:BotToken": "YOUR_BOT_TOKEN",
	"BotConfiguration:AdminChatIds:0": "123456789",
	"BotConfiguration:AdminChatIds:1": "987654321",
	"ConnectionStrings:DefaultConnection": "Server=(local);Database=TelegramBotDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**ВАЖЛИВО**: 
- В Azure Functions **немає** секцій `ConnectionStrings` та `BotConfiguration` на верхньому рівні
- Все має бути в `Values` як плоскі ключі з двокрапкою
- Для масивів: `BotConfiguration:AdminChatIds:0`, `BotConfiguration:AdminChatIds:1`, і т.д.

### Production (Azure Portal)

В Azure Portal додайте Application Settings:
- `BotConfiguration:BotToken` = ваш токен
- `BotConfiguration:AdminChatIds:0` = ID першого адміна
- `ConnectionStrings:DefaultConnection` = connection string до Azure SQL

## Архітектура коду

### Program.cs
- Налаштування DI (Dependency Injection)
- Реєстрація сервісів (DbContext, TelegramBotClient, Handler)
- Ініціалізація бази даних

### TelegramWebhook.cs
- HTTP endpoint для отримання Updates від Telegram
- Десеріалізація JSON
- Виклик TelegramUpdateHandler

### TelegramUpdateHandler.cs
- Вся бізнес-логіка з консольного Program.cs
- Реєстрація користувачів
- Relay повідомлень
- Admin команди

### Інші функції
- `SetWebhook.cs` - встановлює webhook URL
- `DeleteWebhook.cs` - видаляє webhook
- `GetWebhookInfo.cs` - інформація про поточний webhook

## Налаштування webhook

### 1. Запустіть локально
```bash
cd 2PeopleTB.AzureFunctions
func start
```

### 2. Використайте ngrok для тунелю (локально)
```bash
ngrok http 7071
```

### 3. Встановіть webhook
Викличте endpoint:
```
POST http://localhost:7071/api/telegram/setwebhook?url=https://YOUR_NGROK_URL/api/telegram/webhook
```

Або в Production:
```
POST https://your-function-app.azurewebsites.net/api/telegram/setwebhook?url=https://your-function-app.azurewebsites.net/api/telegram/webhook
```

### 4. Перевірте webhook
```
GET http://localhost:7071/api/telegram/getwebhookinfo
```

## Deployment в Azure

### 1. Створіть Resources
- Azure Function App (.NET 10 Isolated)
- Azure SQL Database
- Application Insights (опціонально)

### 2. Налаштуйте Application Settings
Додайте всі змінні з `local.settings.json`

### 3. Deploy
```bash
func azure functionapp publish YOUR-FUNCTION-APP-NAME
```

### 4. Встановіть webhook
```
POST https://your-function-app.azurewebsites.net/api/telegram/setwebhook?url=https://your-function-app.azurewebsites.net/api/telegram/webhook
```

## Troubleshooting

### Проблема: `builder.Configuration["BotConfiguration:BotToken"]` повертає `null`

**Причина**: В Azure Functions конфігурація має бути в `Values` секції `local.settings.json`

**Рішення**: Перевірте формат:
```json
{
  "Values": {
	"BotConfiguration:BotToken": "YOUR_TOKEN"
  }
}
```

НЕ використовуйте вкладені об'єкти:
```json
// ❌ НЕ ПРАЦЮЄ в Azure Functions
{
  "BotConfiguration": {
	"BotToken": "YOUR_TOKEN"
  }
}
```

### Проблема: Webhook не отримує повідомлення

1. Перевірте чи webhook встановлений: `GET /api/telegram/getwebhookinfo`
2. Переконайтеся що URL доступний ззовні (використовуйте ngrok локально)
3. Telegram вимагає HTTPS (ngrok надає це автоматично)

### Проблема: База даних не створюється

В `Program.cs` є автоматичне створення:
```csharp
await dbContext.Database.EnsureCreatedAsync();
```

Але для Production краще використовувати міграції:
```bash
dotnet ef database update --project ../2PeopleTB.DAL/2PeopleTB.DAL.csproj
```

## Переваги Webhook над Polling

✅ **Масштабування**: Автоматичне масштабування під навантаженням  
✅ **Costs**: Платите тільки за виконання (serverless)  
✅ **Швидкість**: Миттєва доставка повідомлень  
✅ **Reliability**: Azure керує інфраструктурою  

## Недоліки

❌ **Складність налаштування**: Потрібен публічний HTTPS endpoint  
❌ **Локальна розробка**: Потрібен ngrok або подібний інструмент  
❌ **Cold start**: Перший запит може бути повільним  

## Рекомендації

- **Розробка**: Використовуйте консольний бот з polling
- **Production**: Використовуйте Azure Functions з webhook
- **CI/CD**: Налаштуйте GitHub Actions для автоматичного deployment
- **Monitoring**: Використовуйте Application Insights для логів
