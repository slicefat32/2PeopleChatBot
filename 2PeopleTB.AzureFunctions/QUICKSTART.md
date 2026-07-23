# Швидкий старт: Azure Functions Bot

## Мінімальні кроки для запуску

### 1. Налаштування конфігурації

Скопіюйте `local.settings.json.example` в `local.settings.json`:

```powershell
cd 2PeopleTB.AzureFunctions
cp local.settings.json.example local.settings.json
```

Відредагуйте `local.settings.json`:

```json
{
	"BotConfiguration": {
		"BotToken": "7123456789:AAHdqTcvCH1vGWJxfSeofSAs0K5PALdsaw",
		"AdminChatIds": [YOUR_TELEGRAM_ID]
	}
}
```

💡 Щоб дізнатися свій Telegram ID, напишіть боту [@userinfobot](https://t.me/userinfobot)

### 2. База даних

```powershell
# З кореневої директорії проєкту
cd ..\2PeopleTB.DAL
dotnet ef database update --startup-project ..\2PeopleTB.AzureFunctions
```

### 3. Запуск локально

```powershell
cd ..\2PeopleTB.AzureFunctions
func start
```

Ви побачите:

```
Functions:
	DeleteWebhook: [GET,POST] http://localhost:7071/api/telegram/deletewebhook
	GetWebhookInfo: [GET] http://localhost:7071/api/telegram/webhookinfo
	SetWebhook: [GET,POST] http://localhost:7071/api/telegram/setwebhook
	TelegramWebhook: [POST] http://localhost:7071/api/telegram/webhook
```

### 4. Налаштування webhook через ngrok

#### 4.1. Встановіть ngrok

Завантажте з [ngrok.com](https://ngrok.com/download)

#### 4.2. Запустіть ngrok

```powershell
ngrok http 7071
```

Скопіюйте HTTPS URL (наприклад: `https://abc123.ngrok.io`)

#### 4.3. Встановіть webhook

```powershell
# PowerShell
$ngrokUrl = "https://abc123.ngrok.io"
Invoke-WebRequest "http://localhost:7071/api/telegram/setwebhook?url=$ngrokUrl/api/telegram/webhook"

# або curl
curl "http://localhost:7071/api/telegram/setwebhook?url=https://abc123.ngrok.io/api/telegram/webhook"
```

#### 4.4. Перевірте webhook

```powershell
curl http://localhost:7071/api/telegram/webhookinfo
```

Ви повинні побачити:

```json
{
  "Url": "https://abc123.ngrok.io/api/telegram/webhook",
  "HasCustomCertificate": false,
  "PendingUpdateCount": 0,
  ...
}
```

### 5. Тестування

1. Знайдіть свого бота в Telegram
2. Надішліть `/start`
3. Як адмін, надішліть `/users` щоб побачити зареєстрованих користувачів

## Деплой в Azure (Quick)

### 1. Створіть ресурси

```bash
# Змінні
RG="TelegramBotRG"
LOCATION="eastus"
STORAGE="telegrambotstor$(Get-Random -Maximum 9999)"
FUNCTION_APP="2PeopleTelegramBot"

# Створіть Resource Group
az group create --name $RG --location $LOCATION

# Storage Account
az storage account create `
  --name $STORAGE `
  --resource-group $RG `
  --location $LOCATION `
  --sku Standard_LRS

# Function App
az functionapp create `
  --resource-group $RG `
  --consumption-plan-location $LOCATION `
  --runtime dotnet-isolated `
  --runtime-version 10 `
  --functions-version 4 `
  --name $FUNCTION_APP `
  --storage-account $STORAGE
```

### 2. Налаштуйте SQL Database (опційно, можна використати існуючий)

```bash
SQL_SERVER="telegrambot-sql-$(Get-Random -Maximum 9999)"
SQL_DB="TelegramBotDb"
SQL_ADMIN="sqladmin"
SQL_PASSWORD="YourStrongPassword123!"

# SQL Server
az sql server create `
  --name $SQL_SERVER `
  --resource-group $RG `
  --location $LOCATION `
  --admin-user $SQL_ADMIN `
  --admin-password $SQL_PASSWORD

# Database
az sql db create `
  --resource-group $RG `
  --server $SQL_SERVER `
  --name $SQL_DB `
  --service-objective S0

# Firewall (дозвіл Azure сервісам)
az sql server firewall-rule create `
  --resource-group $RG `
  --server $SQL_SERVER `
  --name AllowAzureServices `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0
```

### 3. Налаштуйте конфігурацію

```bash
# Connection String
$connectionString = "Server=tcp:$SQL_SERVER.database.windows.net,1433;Database=$SQL_DB;User ID=$SQL_ADMIN;Password=$SQL_PASSWORD;Encrypt=True;"

az functionapp config connection-string set `
  --name $FUNCTION_APP `
  --resource-group $RG `
  --connection-string-type SQLAzure `
  --settings DefaultConnection="$connectionString"

# Bot Configuration
az functionapp config appsettings set `
  --name $FUNCTION_APP `
  --resource-group $RG `
  --settings `
	BotConfiguration__BotToken="YOUR_BOT_TOKEN" `
	BotConfiguration__AdminChatIds__0="YOUR_TELEGRAM_ID"
```

### 4. Деплой

```powershell
cd 2PeopleTB.AzureFunctions
func azure functionapp publish $FUNCTION_APP
```

### 5. Встановіть webhook

```bash
$functionUrl = "https://$FUNCTION_APP.azurewebsites.net"
Invoke-WebRequest "$functionUrl/api/telegram/setwebhook?url=$functionUrl/api/telegram/webhook"
```

### 6. Перевірте

```bash
Invoke-WebRequest "$functionUrl/api/telegram/webhookinfo"
```

## Troubleshooting

### "Database does not exist"

```powershell
cd 2PeopleTB.DAL
dotnet ef database update --startup-project ..\2PeopleTB.AzureFunctions
```

### "Webhook failed"

Перевірте:
1. HTTPS URL (Telegram вимагає HTTPS)
2. Валідний SSL сертифікат (ngrok автоматично забезпечує)
3. Бот токен правильний

```powershell
curl http://localhost:7071/api/telegram/webhookinfo
```

### Логи Azure Functions

```bash
# Real-time logs
func azure functionapp logstream $FUNCTION_APP

# Portal
# Azure Portal → Function App → Monitoring → Log stream
```

## Команди

### Користувацькі
- `/start` - реєстрація

### Адміністраторські
- `/connect [ID1] [ID2]` - з'єднати користувачів
- `/disconnect [ID]` - роз'єднати
- `/users` - список користувачів
- `/history [ID]` - історія користувача
- `/chat [ID1] [ID2]` - переписка між користувачами

## Корисні посилання

- [Azure Functions документація](https://docs.microsoft.com/azure/azure-functions/)
- [Telegram Bot API](https://core.telegram.org/bots/api)
- [ngrok](https://ngrok.com/)
- [Azure Portal](https://portal.azure.com/)
