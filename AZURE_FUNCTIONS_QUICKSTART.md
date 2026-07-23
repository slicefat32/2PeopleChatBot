# Швидкий старт: Azure Functions Webhook

## Передумови

1. .NET 10 SDK
2. Azure Functions Core Tools
3. Ngrok (для локального тестування)
4. SQL Server / LocalDB
5. Telegram Bot Token

## Крок 1: Налаштування конфігурації

Створіть `2PeopleTB.AzureFunctions/local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
	"AzureWebJobsStorage": "UseDevelopmentStorage=true",
	"FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
	"BotConfiguration:BotToken": "YOUR_BOT_TOKEN_HERE",
	"BotConfiguration:AdminChatIds:0": "YOUR_TELEGRAM_ID",
	"ConnectionStrings:DefaultConnection": "Server=(local);Database=TelegramBotDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**Як отримати ваш Telegram ID:**
1. Напишіть боту [@userinfobot](https://t.me/userinfobot)
2. Він надішле ваш ID

## Крок 2: Застосування міграцій

```bash
cd 2PeopleTelegramBot
dotnet ef database update --project ../2PeopleTB.DAL/2PeopleTB.DAL.csproj
```

## Крок 3: Запуск Azure Functions

```bash
cd 2PeopleTB.AzureFunctions
func start
```

Ви побачите:
```
Functions:
	DeleteWebhook: [DELETE] http://localhost:7071/api/telegram/deletewebhook
	GetWebhookInfo: [GET] http://localhost:7071/api/telegram/getwebhookinfo
	SetWebhook: [POST] http://localhost:7071/api/telegram/setwebhook
	TelegramWebhook: [POST] http://localhost:7071/api/telegram/webhook
```

## Крок 4: Налаштування Ngrok тунелю

В новому терміналі:

```bash
ngrok http 7071
```

Ви отримаєте URL типу: `https://abc123.ngrok.io`

## Крок 5: Встановлення Webhook

### Варіант А: Через браузер/Postman

```
POST http://localhost:7071/api/telegram/setwebhook?url=https://abc123.ngrok.io/api/telegram/webhook
```

### Варіант Б: Через curl

```bash
curl -X POST "http://localhost:7071/api/telegram/setwebhook?url=https://abc123.ngrok.io/api/telegram/webhook"
```

Відповідь:
```json
{
  "ok": true,
  "result": true,
  "description": "Webhook was set"
}
```

## Крок 6: Перевірка Webhook

```bash
curl http://localhost:7071/api/telegram/getwebhookinfo
```

Ви побачите:
```json
{
  "url": "https://abc123.ngrok.io/api/telegram/webhook",
  "has_custom_certificate": false,
  "pending_update_count": 0
}
```

## Крок 7: Тестування

1. Відкрийте Telegram
2. Знайдіть вашого бота
3. Надішліть `/start`
4. Бот має відповісти привітанням

В логах Azure Functions ви побачите:
```
[2024-01-17 12:00:00] 📩 Отримано webhook від Telegram
[2024-01-17 12:00:00] [Новий користувач]: 123456789 (@yourname)
[2024-01-17 12:00:00] ✅ Update оброблено успішно
```

## Admin команди

Як адміністратор, ви можете використовувати:

- `/users` - список зареєстрованих користувачів
- `/connect 123456 789012` - з'єднати двох користувачів
- `/disconnect 123456` - роз'єднати користувача
- `/history 123456` - історія повідомлень користувача
- `/chat 123456 789012` - переписка між двома користувачами

## Troubleshooting

### Проблема: Function не стартує

**Помилка**: `Microsoft.Azure.WebJobs.Script: Did not find functions with language [dotnet-isolated]`

**Рішення**: Перевірте `local.settings.json`:
```json
"FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
```

### Проблема: Null configuration

**Помилка**: `builder.Configuration["BotConfiguration:BotToken"]` повертає `null`

**Рішення**: Конфігурація має бути в `Values`:
```json
{
  "Values": {
	"BotConfiguration:BotToken": "YOUR_TOKEN"
  }
}
```

### Проблема: База даних не існує

**Помилка**: `Cannot open database "TelegramBotDb" requested by the login`

**Рішення**: 
```bash
cd 2PeopleTelegramBot
dotnet ef database update --project ../2PeopleTB.DAL/2PeopleTB.DAL.csproj
```

### Проблема: Webhook не отримує повідомлення

**Перевірки**:
1. Ngrok запущений і показує forwarding URL
2. Webhook встановлений (перевірте через `/getwebhookinfo`)
3. URL в webhook містить `https://` (Telegram вимагає HTTPS)
4. Azure Functions запущені і показують listening на порту 7071

**Якщо все ще не працює**:
```bash
# Видаліть webhook
curl -X DELETE http://localhost:7071/api/telegram/deletewebhook

# Встановіть знову з правильним URL
curl -X POST "http://localhost:7071/api/telegram/setwebhook?url=https://YOUR_NEW_NGROK_URL/api/telegram/webhook"
```

### Проблема: Ngrok session expired

Ngrok безкоштовна версія має обмеження на час сесії. Після перезапуску:

1. Отримайте новий URL
2. Встановіть webhook знову з новим URL

## Production Deployment

### Azure Portal

1. **Створіть Function App**
   - Runtime: .NET 10
   - Plan: Consumption (serverless)

2. **Створіть SQL Database**
   - Azure SQL Database
   - Або використайте існуючий сервер

3. **Configuration → Application Settings**

   Додайте:
   - `BotConfiguration:BotToken` = ваш токен
   - `BotConfiguration:AdminChatIds:0` = ваш ID
   - `ConnectionStrings:DefaultConnection` = connection string

4. **Deploy**
   ```bash
   func azure functionapp publish YOUR-APP-NAME
   ```

5. **Встановіть Production Webhook**
   ```bash
   curl -X POST "https://YOUR-APP-NAME.azurewebsites.net/api/telegram/setwebhook?url=https://YOUR-APP-NAME.azurewebsites.net/api/telegram/webhook"
   ```

## Наступні кроки

- Налаштуйте Application Insights для моніторингу
- Додайте CI/CD через GitHub Actions
- Налаштуйте custom domain з SSL
- Розгляньте використання Azure Key Vault для секретів
