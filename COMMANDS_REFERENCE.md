# Швидкий довідник команд

## 🚀 Швидкий старт (по проєктах)

### Console App
```powershell
# 1. Налаштуйте appsettings.json (BotToken, AdminChatIds)
# 2. Застосуйте міграції
cd 2PeopleTB.DAL
dotnet ef database update --startup-project ..\2PeopleTelegramBot

# 3. Запустіть
cd ..\2PeopleTelegramBot
dotnet run
```

### WebAPI
```powershell
# 1. Налаштуйте appsettings.json
# 2. Застосуйте міграції
cd 2PeopleTB.DAL
dotnet ef database update --startup-project ..\2PeopleTB.WebAPI

# 3. Запустіть API (термінал 1)
cd ..\2PeopleTB.WebAPI
dotnet run

# 4. Запустіть ngrok (термінал 2)
ngrok http 7092

# 5. Встановіть webhook
curl -X POST "https://YOUR_NGROK_URL/api/telegram/setwebhook"
```

### Azure Functions
```powershell
# 1. Налаштуйте local.settings.json
cd 2PeopleTB.AzureFunctions
cp local.settings.json.example local.settings.json
# Відредагуйте файл

# 2. Застосуйте міграції
cd ..\2PeopleTB.DAL
dotnet ef database update --startup-project ..\2PeopleTB.AzureFunctions

# 3. Запустіть Functions (термінал 1)
cd ..\2PeopleTB.AzureFunctions
func start

# 4. Запустіть ngrok (термінал 2)
ngrok http 7071

# 5. Встановіть webhook
curl "http://localhost:7071/api/telegram/setwebhook?url=https://YOUR_NGROK_URL/api/telegram/webhook"
```

---

## 🗄️ База даних

### Створити міграцію
```powershell
cd 2PeopleTB.DAL
dotnet ef migrations add MigrationName --startup-project ..\[PROJECT_NAME]
```

### Застосувати міграції
```powershell
# Console
dotnet ef database update --startup-project ..\2PeopleTelegramBot

# WebAPI
dotnet ef database update --startup-project ..\2PeopleTB.WebAPI

# Azure Functions
dotnet ef database update --startup-project ..\2PeopleTB.AzureFunctions
```

### Видалити останню міграцію
```powershell
dotnet ef migrations remove --startup-project ..\[PROJECT_NAME]
```

### Список міграцій
```powershell
dotnet ef migrations list --startup-project ..\[PROJECT_NAME]
```

### Видалити базу даних
```powershell
dotnet ef database drop --startup-project ..\[PROJECT_NAME]
```

---

## 🔧 Webhook управління

### WebAPI
```powershell
# Встановити webhook
curl -X POST "https://YOUR_DOMAIN/api/telegram/setwebhook"

# Отримати інформацію
curl -X GET "https://YOUR_DOMAIN/api/telegram/webhookinfo"

# Видалити webhook
curl -X POST "https://YOUR_DOMAIN/api/telegram/deletewebhook"
```

### Azure Functions
```powershell
# Встановити webhook (автоматичний URL)
curl "http://localhost:7071/api/telegram/setwebhook"

# Встановити webhook (кастомний URL)
curl "http://localhost:7071/api/telegram/setwebhook?url=https://YOUR_URL/api/telegram/webhook"

# Отримати інформацію
curl "http://localhost:7071/api/telegram/webhookinfo"

# Видалити webhook
curl "http://localhost:7071/api/telegram/deletewebhook"
```

### Console (перемкнутися з webhook на polling)
```powershell
# Видаліть webhook через WebAPI або Azure Functions
curl "http://localhost:7071/api/telegram/deletewebhook"

# Потім запустіть Console App
cd 2PeopleTelegramBot
dotnet run
```

---

## 🏗️ Збірка

### Збірка одного проєкту
```powershell
dotnet build 2PeopleTelegramBot\2PeopleTelegramBot.csproj
dotnet build 2PeopleTB.WebAPI\2PeopleTB.WebAPI.csproj
dotnet build 2PeopleTB.AzureFunctions\2PeopleTB.AzureFunctions.csproj
```

### Збірка всього рішення
```powershell
dotnet build
```

### Очистити
```powershell
dotnet clean
```

---

## 🚢 Деплой

### Azure Functions
```bash
# Publish до Azure
func azure functionapp publish [FUNCTION_APP_NAME]

# Publish з конкретного проєкту
cd 2PeopleTB.AzureFunctions
func azure functionapp publish [FUNCTION_APP_NAME]
```

### WebAPI (Docker)
```bash
# Build image
docker build -t telegram-bot-api -f 2PeopleTB.WebAPI/Dockerfile .

# Run container
docker run -d -p 443:443 \
  -e ConnectionStrings__DefaultConnection="YOUR_CONNECTION_STRING" \
  -e BotConfiguration__BotToken="YOUR_BOT_TOKEN" \
  -e BotConfiguration__AdminChatIds__0="YOUR_CHAT_ID" \
  telegram-bot-api
```

### Console (systemd service)
```bash
# Створіть файл /etc/systemd/system/telegram-bot.service
[Unit]
Description=Telegram Bot
After=network.target

[Service]
Type=simple
User=your-user
WorkingDirectory=/path/to/2PeopleTelegramBot
ExecStart=/usr/bin/dotnet run
Restart=always

[Install]
WantedBy=multi-user.target

# Запустіть service
sudo systemctl enable telegram-bot
sudo systemctl start telegram-bot
```

---

## 📊 Моніторинг

### Console App
```powershell
# Логи виводяться в консоль
# Для production використовуйте:
dotnet run > bot.log 2>&1
```

### WebAPI
```powershell
# Логи в консолі (Development)
dotnet run

# Логи в Application Insights (Production)
# Налаштуйте в appsettings.json:
{
  "ApplicationInsights": {
	"ConnectionString": "YOUR_CONNECTION_STRING"
  }
}
```

### Azure Functions
```bash
# Real-time logs
func azure functionapp logstream [FUNCTION_APP_NAME]

# Portal
# Azure Portal → Function App → Monitoring → Log stream

# Application Insights
# Azure Portal → Function App → Application Insights → Logs
```

---

## 🧪 Тестування

### Отримати свій Chat ID
1. Напишіть [@userinfobot](https://t.me/userinfobot)
2. Скопіюйте ваш Chat ID
3. Додайте в `AdminChatIds` у конфігурації

### Тестування локально з ngrok
```powershell
# 1. Запустіть проєкт
# Console: не потребує ngrok
# WebAPI/Functions: запустіть відповідний проєкт

# 2. Запустіть ngrok
ngrok http [PORT]
# WebAPI: 7092 (або з launchSettings.json)
# Functions: 7071

# 3. Скопіюйте HTTPS URL з ngrok
# Приклад: https://abc123.ngrok.io

# 4. Встановіть webhook
# WebAPI:
curl -X POST "https://abc123.ngrok.io/api/telegram/setwebhook"

# Functions:
curl "http://localhost:7071/api/telegram/setwebhook?url=https://abc123.ngrok.io/api/telegram/webhook"
```

### Перевірка webhook
```powershell
# WebAPI
curl "https://abc123.ngrok.io/api/telegram/webhookinfo"

# Functions
curl "http://localhost:7071/api/telegram/webhookinfo"

# Очікуваний результат:
{
  "Url": "https://abc123.ngrok.io/api/telegram/webhook",
  "HasCustomCertificate": false,
  "PendingUpdateCount": 0,
  ...
}
```

---

## 🐛 Troubleshooting

### Database connection failed
```powershell
# Перевірте connection string
# Console/WebAPI: appsettings.json
# Azure Functions: local.settings.json

# Перевірте, що SQL Server запущений
# LocalDB:
sqllocaldb start mssqllocaldb

# SQL Server:
# Windows: services.msc → SQL Server
```

### EF migrations не застосовуються
```powershell
# Видаліть базу даних та створіть заново
cd 2PeopleTB.DAL
dotnet ef database drop --startup-project ..\[PROJECT_NAME]
dotnet ef database update --startup-project ..\[PROJECT_NAME]
```

### Webhook не працює
```powershell
# 1. Перевірте, що ngrok запущений
ngrok http [PORT]

# 2. Перевірте webhook info
curl "[YOUR_WEBHOOK_INFO_ENDPOINT]"

# 3. Видаліть та встановіть заново
curl "[DELETE_WEBHOOK_ENDPOINT]"
curl "[SET_WEBHOOK_ENDPOINT]?url=https://YOUR_NGROK_URL/api/telegram/webhook"

# 4. Перевірте логи
# WebAPI: консоль
# Functions: func start --verbose
```

### Azure Functions cold start
```bash
# Використовуйте Premium Plan (no cold start)
az functionapp plan create \
  --name PremiumPlan \
  --resource-group TelegramBotRG \
  --location eastus \
  --sku EP1

# Або налаштуйте Always On (не доступно в Consumption)
```

### "ActiveChats not synced" у Azure Functions
```
Проблема: При автомасштабуванні кожен instance має свій static словник

Рішення:
1. Azure Redis Cache (розподілений кеш)
2. SQL Table для ActiveChats
3. Azure Cosmos DB
```

---

## 📦 Пакети та версії

### .NET SDK
```powershell
# Перевірити версію
dotnet --version
# Має бути: 10.x
```

### Azure Functions Core Tools
```powershell
# Перевірити версію
func --version
# Має бути: 4.x

# Встановити/оновити (npm)
npm install -g azure-functions-core-tools@4
```

### Ngrok
```powershell
# Перевірити версію
ngrok version

# Встановити (Windows - Chocolatey)
choco install ngrok

# або скачати з ngrok.com
```

---

## 🔐 Конфігурація для Production

### Environment Variables

**Azure Functions (Application Settings):**
```bash
az functionapp config appsettings set \
  --name [FUNCTION_APP_NAME] \
  --resource-group [RESOURCE_GROUP] \
  --settings \
	BotConfiguration__BotToken="[BOT_TOKEN]" \
	BotConfiguration__AdminChatIds__0="[CHAT_ID_1]" \
	BotConfiguration__AdminChatIds__1="[CHAT_ID_2]"

az functionapp config connection-string set \
  --name [FUNCTION_APP_NAME] \
  --resource-group [RESOURCE_GROUP] \
  --connection-string-type SQLAzure \
  --settings DefaultConnection="[CONNECTION_STRING]"
```

**WebAPI (Azure App Service):**
```bash
az webapp config appsettings set \
  --name [APP_NAME] \
  --resource-group [RESOURCE_GROUP] \
  --settings \
	BotConfiguration__BotToken="[BOT_TOKEN]" \
	BotConfiguration__AdminChatIds__0="[CHAT_ID]"

az webapp config connection-string set \
  --name [APP_NAME] \
  --resource-group [RESOURCE_GROUP] \
  --connection-string-type SQLAzure \
  --settings DefaultConnection="[CONNECTION_STRING]"
```

---

## 📚 Корисні посилання

- [Telegram Bot API](https://core.telegram.org/bots/api)
- [Telegram.Bot .NET Library](https://github.com/TelegramBots/Telegram.Bot)
- [Azure Functions Docs](https://docs.microsoft.com/azure/azure-functions/)
- [EF Core Docs](https://docs.microsoft.com/ef/core/)
- [ngrok](https://ngrok.com/)

---

**Зберігайте цей файл для швидкого доступу до команд! 📌**
