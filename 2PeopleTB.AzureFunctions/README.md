# 2PeopleTB.AzureFunctions

Telegram бот на базі Azure Functions для з'єднання двох користувачів через webhook.

## Архітектура

Проєкт використовує Azure Functions v4 з .NET 10 для обробки Telegram webhook запитів.

### Основні компоненти

- **TelegramWebhook** - основна функція для обробки вхідних оновлень від Telegram
- **SetWebhook** - допоміжна функція для налаштування webhook URL
- **DeleteWebhook** - видалення webhook
- **GetWebhookInfo** - отримання інформації про поточний webhook
- **TelegramUpdateHandler** - сервіс з бізнес-логікою обробки повідомлень

## Налаштування

### 1. Конфігурація local.settings.json

Створіть або оновіть файл `local.settings.json`:

```json
{
	"IsEncrypted": false,
	"Values": {
		"AzureWebJobsStorage": "UseDevelopmentStorage=true",
		"FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
	},
	"ConnectionStrings": {
		"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TelegramBotDb;Trusted_Connection=True;TrustServerCertificate=True;"
	},
	"BotConfiguration": {
		"BotToken": "YOUR_BOT_TOKEN_HERE",
		"AdminChatIds": [123456789, 987654321]
	}
}
```

### 2. Azure Configuration (для production)

В Application Settings додайте:

- `ConnectionStrings__DefaultConnection` - рядок підключення до SQL Server
- `BotConfiguration__BotToken` - токен вашого бота
- `BotConfiguration__AdminChatIds__0` - ID першого адміна
- `BotConfiguration__AdminChatIds__1` - ID другого адміна (опційно)

## Запуск локально

### Передумови

1. Visual Studio 2026 або новіше
2. Azure Functions Core Tools v4
3. SQL Server LocalDB або SQL Server
4. .NET 10 SDK

### Кроки

1. **Застосуйте міграції бази даних**:
   ```powershell
   cd 2PeopleTB.DAL
   dotnet ef database update --startup-project ..\2PeopleTB.AzureFunctions
   ```

2. **Запустіть Azure Functions**:
   ```powershell
   cd 2PeopleTB.AzureFunctions
   func start
   ```

3. **Налаштуйте webhook** (використовуйте ngrok для локального тестування):
   ```powershell
   # Запустіть ngrok
   ngrok http 7071

   # Встановіть webhook (замініть YOUR_NGROK_URL)
   curl "http://localhost:7071/api/telegram/setwebhook?url=https://YOUR_NGROK_URL/api/telegram/webhook"
   ```

4. **Перевірте webhook**:
   ```powershell
   curl http://localhost:7071/api/telegram/webhookinfo
   ```

## Деплой в Azure

### 1. Створіть Function App

```bash
# Створіть Resource Group
az group create --name TelegramBotRG --location eastus

# Створіть Storage Account
az storage account create \
  --name telegrambotstorage \
  --resource-group TelegramBotRG \
  --location eastus \
  --sku Standard_LRS

# Створіть Function App
az functionapp create \
  --resource-group TelegramBotRG \
  --consumption-plan-location eastus \
  --runtime dotnet-isolated \
  --runtime-version 10 \
  --functions-version 4 \
  --name 2PeopleTelegramBot \
  --storage-account telegrambotstorage
```

### 2. Налаштуйте рядок підключення до БД

```bash
az functionapp config connection-string set \
  --name 2PeopleTelegramBot \
  --resource-group TelegramBotRG \
  --connection-string-type SQLAzure \
  --settings DefaultConnection="YOUR_AZURE_SQL_CONNECTION_STRING"
```

### 3. Додайте конфігурацію бота

```bash
az functionapp config appsettings set \
  --name 2PeopleTelegramBot \
  --resource-group TelegramBotRG \
  --settings \
	BotConfiguration__BotToken="YOUR_BOT_TOKEN" \
	BotConfiguration__AdminChatIds__0="123456789"
```

### 4. Деплойте код

```powershell
# З директорії проєкту
func azure functionapp publish 2PeopleTelegramBot
```

### 5. Встановіть webhook

```bash
# Отримайте URL вашої функції
$functionUrl = az functionapp function show \
  --name 2PeopleTelegramBot \
  --resource-group TelegramBotRG \
  --function-name SetWebhook \
  --query "invokeUrlTemplate" -o tsv

# Встановіть webhook
curl "$functionUrl&url=https://2peopletelegrambot.azurewebsites.net/api/telegram/webhook"
```

## Доступні endpoints

- `POST /api/telegram/webhook` - основний webhook endpoint для Telegram
- `GET/POST /api/telegram/setwebhook?url={webhookUrl}` - встановити webhook
- `GET/POST /api/telegram/deletewebhook` - видалити webhook
- `GET /api/telegram/webhookinfo` - інформація про webhook

## Команди адміністратора

- `/connect [ID_1] [ID_2]` - з'єднати двох користувачів
- `/disconnect [ID]` - роз'єднати користувача
- `/users` - список усіх зареєстрованих користувачів
- `/history [ID]` - історія повідомлень користувача
- `/chat [ID_1] [ID_2]` - переписка між двома користувачами

## Особливості Azure Functions

### Переваги webhook підходу

1. **Масштабованість** - автоматичне масштабування на основі навантаження
2. **Вартість** - оплата лише за виконання (consumption plan)
3. **Швидкість** - миттєва доставка повідомлень через webhook
4. **Надійність** - вбудований моніторинг та логування

### Відмінності від polling підходу

- ❌ Немає `StartReceiving` - Telegram сам надсилає оновлення
- ✅ Webhook endpoint приймає POST запити з Update об'єктами
- ✅ Статична IP не потрібна (використовується HTTPS URL)
- ✅ Підтримка HTTPS обов'язкова для webhook

## Моніторинг

### Application Insights

Azure Functions автоматично інтегрується з Application Insights. Переглядайте логи:

```bash
az monitor app-insights component show \
  --app 2PeopleTelegramBot \
  --resource-group TelegramBotRG
```

### Live Metrics

В Azure Portal → Function App → Application Insights → Live Metrics

## Troubleshooting

### Webhook не працює

1. Перевірте, чи встановлено webhook:
   ```
   curl https://YOUR_APP.azurewebsites.net/api/telegram/webhookinfo
   ```

2. Перевірте логи Function App в Application Insights

3. Telegram вимагає HTTPS з валідним сертифікатом

### Повідомлення не доставляються

1. Перевірте, чи з'єднані користувачі (`/users`)
2. Перегляньте логи Application Insights
3. Переконайтеся, що користувачі не заблокували бота

### Помилки бази даних

1. Перевірте рядок підключення в конфігурації
2. Застосуйте міграції EF Core
3. Перевірте firewall правила SQL Server для Azure Functions

## Різниця між проєктами

| Функція | 2PeopleTelegramBot (Console) | 2PeopleTB.WebAPI | 2PeopleTB.AzureFunctions |
|---------|------------------------------|------------------|--------------------------|
| Hosting | Self-hosted | ASP.NET Core | Azure Functions |
| Updates | Long Polling | Webhook | Webhook |
| Scaling | Manual | Manual/Docker | Automatic |
| Cost | Server 24/7 | Server 24/7 | Pay per execution |
| Setup | Простий | Середній | Середній |

## Ліцензія

MIT
