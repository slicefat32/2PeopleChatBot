# ✅ Azure Functions Портування - Завершено

## Що було зроблено

### 1. ✅ Оновлено проєкт 2PeopleTB.AzureFunctions

#### Додані пакети:
- `Telegram.Bot` (22.10.2)
- `Microsoft.EntityFrameworkCore.SqlServer` (9.0.2)
- Project Reference на `2PeopleTB.DAL`

#### Створені файли:

**Services/**
- `TelegramUpdateHandler.cs` - бізнес-логіка (скопійована з WebAPI)

**Functions/**
- `Function1.cs` (TelegramWebhook) - основний webhook endpoint
- `SetWebhook.cs` - допоміжна функція для налаштування webhook
- `DeleteWebhook.cs` - видалення webhook
- `GetWebhookInfo.cs` - інформація про webhook

**Configuration/**
- `local.settings.json` - налаштування для локальної розробки
- `local.settings.json.example` - приклад для production
- `host.json` - налаштування Functions App

**Documentation:**
- `README.md` - повна документація Azure Functions
- `QUICKSTART.md` - швидкий старт
- `MIGRATION.md` - міграція між архітектурами
- `ARCHITECTURE.md` - архітектурні діаграми

**Root Documentation:**
- `ARCHITECTURE_COMPARISON.md` - детальне порівняння всіх трьох архітектур

---

## Архітектура Azure Functions

```
Telegram Updates (POST)
	↓
TelegramWebhook Function
	↓
TelegramUpdateHandler Service
	↓
├── RegisteredUsersService (DAL)
└── MessageHistoryService (DAL)
	↓
TelegramBotDbContext (EF Core)
	↓
SQL Server Database
```

---

## Ключові відмінності від WebAPI

### Program.cs
```diff
- var builder = WebApplication.CreateBuilder(args);
+ var builder = FunctionsApplication.CreateBuilder(args);

- builder.Services.AddControllers();
+ // Functions не потребує контролерів

- app.UseHttpsRedirection();
- app.MapControllers();
+ // Functions автоматично мапить функції
```

### Webhook Endpoint
```diff
WebAPI (TelegramController.cs):
- [HttpPost("webhook")]
- public async Task<IActionResult> Webhook([FromBody] Update update)

Azure Functions (Function1.cs):
+ [Function("TelegramWebhook")]
+ public async Task<HttpResponseData> Run(
+     [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "telegram/webhook")]
+     HttpRequestData req)
```

### Dependency Injection
**Однакове!** Обидва використовують `builder.Services.Add*`

### TelegramUpdateHandler
**Повністю однаковий!** Скопійований as-is з WebAPI.

---

## Локальний запуск

### 1. Налаштуйте конфігурацію
```powershell
cd 2PeopleTB.AzureFunctions
cp local.settings.json.example local.settings.json
# Відредагуйте BotToken та AdminChatIds
```

### 2. Застосуйте міграції
```powershell
cd ..\2PeopleTB.DAL
dotnet ef database update --startup-project ..\2PeopleTB.AzureFunctions
```

### 3. Запустіть Functions
```powershell
cd ..\2PeopleTB.AzureFunctions
func start
```

### 4. Налаштуйте webhook (ngrok)
```powershell
# Термінал 2
ngrok http 7071

# Встановіть webhook
curl "http://localhost:7071/api/telegram/setwebhook?url=https://YOUR_NGROK_URL/api/telegram/webhook"
```

---

## Production Deploy

### Azure CLI
```bash
# 1. Створіть Function App
az functionapp create \
  --resource-group TelegramBotRG \
  --consumption-plan-location eastus \
  --runtime dotnet-isolated \
  --functions-version 4 \
  --name 2PeopleTelegramBot \
  --storage-account telegrambotstorage

# 2. Налаштуйте connection string
az functionapp config connection-string set \
  --name 2PeopleTelegramBot \
  --resource-group TelegramBotRG \
  --connection-string-type SQLAzure \
  --settings DefaultConnection="YOUR_CONNECTION_STRING"

# 3. Налаштуйте bot config
az functionapp config appsettings set \
  --name 2PeopleTelegramBot \
  --resource-group TelegramBotRG \
  --settings \
	BotConfiguration__BotToken="YOUR_BOT_TOKEN" \
	BotConfiguration__AdminChatIds__0="YOUR_CHAT_ID"

# 4. Деплойте
func azure functionapp publish 2PeopleTelegramBot

# 5. Встановіть webhook
curl "https://2peopletelegrambot.azurewebsites.net/api/telegram/setwebhook?url=https://2peopletelegrambot.azurewebsites.net/api/telegram/webhook"
```

---

## Функціонал (однаковий у всіх проєктах)

### Користувацькі команди
- `/start` - реєстрація

### Адміністраторські команди
- `/connect [ID1] [ID2]` - з'єднати користувачів
- `/disconnect [ID]` - роз'єднати
- `/users` - список користувачів
- `/history [ID]` - історія користувача
- `/chat [ID1] [ID2]` - переписка між користувачами

### Підтримка типів повідомлень
- ✅ Text
- ✅ Photo
- ✅ Voice
- ✅ Audio
- ✅ Video
- ✅ Document
- ✅ Sticker
- ✅ VideoNote

---

## Переваги Azure Functions

### ✅ Автомасштабування
```
0 запитів → 0 instances (економія $$$)
100 запитів/сек → автоматично створює instances
Навантаження спадає → instances деалокуються
```

### ✅ Consumption Plan ціноутворення
```
Перші 1,000,000 executions/місяць - БЕЗКОШТОВНО
Потім $0.20 за 1M executions
```

**Для невеликого бота (<100k повідомлень/міс):**
- **Functions:** ~$5-10/міс (майже безкоштовно)
- **WebAPI (App Service):** ~$15-50/міс
- **Console (VPS):** ~$5-20/міс

### ✅ Моніторинг
- Application Insights вбудований
- Real-time логи
- Performance metrics
- Alerts

---

## Недоліки Azure Functions

### ⚠️ Cold Start
```
Перший запит після паузи (~20 хв без активності):
Латентність: 1-5 секунд

Наступні запити (warm container):
Латентність: 50-200 мс
```

**Рішення:** Premium Plan (дорожче, але no cold start)

### ⚠️ Статичний стан не масштабується

```csharp
// ❌ Проблема
private static ConcurrentDictionary<long, long> ActiveChats = new();

// Instance 1 має свій словник
// Instance 2 має свій словник
// → Несинхронізовано!
```

**Рішення для production:**
- Azure Redis Cache (розподілений кеш)
- SQL Table для ActiveChats
- Azure Cosmos DB

---

## Порівняння з іншими проєктами

| Характеристика | Console | WebAPI | Azure Functions |
|----------------|---------|--------|-----------------|
| **Спосіб оновлень** | Long Polling | Webhook | Webhook |
| **Складність** | ⭐ Просто | ⭐⭐ Середньо | ⭐⭐⭐ Складніше |
| **Затримка** | 1-5 сек | <500 мс | <500 мс (warm) |
| **Масштабування** | ❌ Немає | ⚠️ Ручне | ✅ Авто |
| **Вартість (малий бот)** | $0-20/міс | $15-65/міс | $5-20/міс |
| **Cold Start** | ❌ Немає | ❌ Немає | ⚠️ Є (~2-5 сек) |
| **HTTPS потрібен** | ❌ Ні | ✅ Так | ✅ Так (вбудований) |
| **Керування сервером** | ✅ Треба | ✅ Треба | ❌ Не треба |

---

## Спільний код

### 100% спільний DAL
- `TelegramBotDbContext`
- `RegisteredUser` model
- `MessageHistory` model
- `RegisteredUsersService`
- `MessageHistoryService`

### 95% спільна бізнес-логіка
- `TelegramUpdateHandler` - майже однаковий у WebAPI та Azure Functions
- Відмінності лише в entry point (Controller vs Function)

**Це означає:** легко мігрувати між проєктами!

---

## Рекомендації

### Використовуйте Azure Functions, якщо:
- ✅ Потрібен production-ready бот
- ✅ Обмежений бюджет
- ✅ Змінне навантаження
- ✅ Не хочете керувати сервером

### Використовуйте WebAPI, якщо:
- ✅ Потрібна інтеграція з багатьма API
- ✅ Є DevOps команда
- ✅ Корпоративна інфраструктура
- ✅ Критично низька латентність (без cold start)

### Використовуйте Console, якщо:
- ✅ Тільки розробка/тестування
- ✅ Особистий бот
- ✅ Немає можливості налаштувати HTTPS

---

## Наступні кроки (опційно, для production)

### 1. Розподілений стан (замість static ActiveChats)
```csharp
// Використовуйте Azure Redis Cache
services.AddStackExchangeRedisCache(options =>
{
	options.Configuration = configuration["Redis:ConnectionString"];
});
```

### 2. IP Whitelist (тільки Telegram IPs)
```json
// host.json
{
  "extensions": {
	"http": {
	  "customHeaders": {
		"X-Forwarded-For": "{client-ip}"
	  }
	}
  }
}
```

### 3. Secret Token
```csharp
// Додайте секретний токен у URL webhook
await botClient.SetWebhook($"{webhookUrl}?secret={secretToken}");

// Перевіряйте в функції
if (req.Query["secret"] != secretToken)
	return req.CreateResponse(HttpStatusCode.Unauthorized);
```

### 4. Durable Functions (для складної оркестрації)
- Якщо потрібні довготривалі процеси
- State management

---

## Збірка - Успішна ✅

```powershell
dotnet build 2PeopleTB.AzureFunctions/2PeopleTB.AzureFunctions.csproj
# Build succeeded.
```

Всі файли створені, код портований, збірка успішна! 🚀

---

## Документація

1. **README.md** - повна документація
2. **QUICKSTART.md** - швидкий старт
3. **MIGRATION.md** - міграція між архітектурами
4. **ARCHITECTURE.md** - архітектурні діаграми
5. **ARCHITECTURE_COMPARISON.md** (root) - порівняння всіх проєктів

---

**Готово! Telegram бот портований на Azure Functions архітектуру! 🎉**
