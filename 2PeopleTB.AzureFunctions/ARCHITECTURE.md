# Архітектура Azure Functions Bot

## Діаграма архітектури

```
┌─────────────────────────────────────────────────────────────────┐
│                         TELEGRAM SERVER                          │
│                                                                   │
│  User1 ──┐                                      ┌── User2       │
│          │  Webhook POST /api/telegram/webhook  │               │
│  User3 ──┼─────────────────────────────────────┼── User4       │
│          │     (Update JSON)                    │               │
│  Admin ──┘                                      └── User5       │
└─────────────────────────────────────────────────────────────────┘
							   │
							   │ HTTPS POST
							   ▼
┌─────────────────────────────────────────────────────────────────┐
│                      AZURE FUNCTIONS APP                         │
│                                                                   │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ TelegramWebhook Function                                   │ │
│  │  Route: POST /api/telegram/webhook                         │ │
│  │                                                             │ │
│  │  1. Отримує Update від Telegram                           │ │
│  │  2. Десеріалізує JSON → Update object                     │ │
│  │  3. Викликає TelegramUpdateHandler                        │ │
│  └────────────────────────┬───────────────────────────────────┘ │
│                           │                                      │
│                           ▼                                      │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ TelegramUpdateHandler Service                              │ │
│  │                                                             │ │
│  │  ┌──────────────────────────────────────────────────────┐ │ │
│  │  │ HandleUpdateAsync()                                   │ │ │
│  │  │                                                        │ │ │
│  │  │  1. Реєстрація користувача (якщо новий)              │ │ │
│  │  │  2. Розпізнавання команд (/start, /connect, etc.)    │ │ │
│  │  │  3. Relay повідомлень між партнерами                 │ │ │
│  │  │  4. Збереження історії в БД                          │ │ │
│  │  └──────────────────────────────────────────────────────┘ │ │
│  │                                                             │ │
│  │  ┌─────────────┐  ┌─────────────────────┐                 │ │
│  │  │ Admin       │  │ User Message Relay  │                 │ │
│  │  │ Commands    │  │                     │                 │ │
│  │  │             │  │ User1 → Partner     │                 │ │
│  │  │ /connect    │  │ User2 → Partner     │                 │ │
│  │  │ /disconnect │  │                     │                 │ │
│  │  │ /users      │  │ ActiveChats Dict    │                 │ │
│  │  │ /history    │  │ (in-memory state)   │                 │ │
│  │  │ /chat       │  │                     │                 │ │
│  │  └─────────────┘  └─────────────────────┘                 │ │
│  └────────────────────────┬───────────────────────────────────┘ │
│                           │                                      │
│                           ▼                                      │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ Helper Functions (Management)                              │ │
│  │                                                             │ │
│  │  • SetWebhook     - встановити webhook URL                │ │
│  │  • DeleteWebhook  - видалити webhook                      │ │
│  │  • GetWebhookInfo - інформація про webhook                │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                   │
└───────────────────────┬───────────────────────────────────────────┘
						│
						│ Dependency Injection
						▼
┌─────────────────────────────────────────────────────────────────┐
│                          DAL LAYER                               │
│                    (2PeopleTB.DAL project)                       │
│                                                                   │
│  ┌────────────────────┐  ┌────────────────────────────────────┐ │
│  │ RegisteredUsers    │  │ MessageHistory                      │ │
│  │ Service            │  │ Service                             │ │
│  │                    │  │                                     │ │
│  │ • GetUserAsync()   │  │ • SaveMessageAsync()               │ │
│  │ • AddUserAsync()   │  │ • GetMessageHistory...Async()      │ │
│  │ • GetAllUsers()    │  │ • GetUserMessageHistoryAsync()     │ │
│  │ • UserExistsAsync()│  │ • GetTotalMessagesCountAsync()     │ │
│  │ • UpdateUsername() │  │                                     │ │
│  └─────────┬──────────┘  └────────────┬───────────────────────┘ │
│            │                          │                          │
│            └──────────┬───────────────┘                          │
│                       │                                          │
│                       ▼                                          │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ TelegramBotDbContext (EF Core)                             │ │
│  │                                                             │ │
│  │  DbSet<RegisteredUser>                                     │ │
│  │  DbSet<MessageHistory>                                     │ │
│  └────────────────────────┬───────────────────────────────────┘ │
└───────────────────────────┼───────────────────────────────────────┘
							│
							│ SQL Connection
							▼
┌─────────────────────────────────────────────────────────────────┐
│                      AZURE SQL DATABASE                          │
│                   (або SQL Server LocalDB)                       │
│                                                                   │
│  ┌─────────────────────────┐  ┌────────────────────────────┐   │
│  │ RegisteredUsers Table   │  │ MessageHistories Table     │   │
│  │                         │  │                            │   │
│  │ • ChatId (PK)          │  │ • Id (PK)                 │   │
│  │ • Username             │  │ • FromChatId              │   │
│  │ • RegisteredAt         │  │ • ToChatId                │   │
│  └─────────────────────────┘  │ • MessageId               │   │
│                                │ • MessageType             │   │
│                                │ • TextContent             │   │
│                                │ • FileId                  │   │
│                                │ • SentAt                  │   │
│                                └────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

## Потік даних

### 1. Користувач надсилає повідомлення

```
User (Telegram) 
	→ Telegram Server 
	→ HTTPS POST to Azure Function 
	→ TelegramWebhook.Run()
	→ TelegramUpdateHandler.HandleUpdateAsync()
```

### 2. Обробка повідомлення

```
HandleUpdateAsync()
	├─ message.Text == "/start"?
	│   └─ Реєстрація → RegisteredUsersService.AddUserAsync()
	│
	├─ AdminChatIds.Contains(chatId) && message.Text.StartsWith("/")?
	│   └─ HandleAdminCommandsAsync()
	│       ├─ /connect → ActiveChats[user1] = user2
	│       ├─ /disconnect → ActiveChats[userId] = -1
	│       ├─ /users → RegisteredUsersService.GetAllUsersAsync()
	│       ├─ /history → MessageHistoryService.GetUserMessageHistoryAsync()
	│       └─ /chat → MessageHistoryService.GetMessageHistoryBetweenUsersAsync()
	│
	└─ ActiveChats[chatId] != -1?
		└─ HandleMessageRelayAsync()
			├─ CopyMessage(to: partnerId)
			└─ MessageHistoryService.SaveMessageAsync()
```

### 3. Відповідь користувачу

```
TelegramUpdateHandler
	→ ITelegramBotClient.SendMessage() / CopyMessage()
	→ Telegram Server
	→ User receives message
```

## Конфігурація (DI Container)

```
Program.cs
│
├─ ConfigurationBuilder
│   ├─ BotConfiguration:BotToken
│   ├─ BotConfiguration:AdminChatIds[]
│   └─ ConnectionStrings:DefaultConnection
│
├─ Services.AddDbContext<TelegramBotDbContext>()
│
├─ Services.AddSingleton<ITelegramBotClient>()
│
├─ Services.AddScoped<RegisteredUsersService>()
│
├─ Services.AddScoped<MessageHistoryService>()
│
└─ Services.AddScoped<TelegramUpdateHandler>()
```

## Serverless особливості

### Cold Start
```
First request → Spin up container (1-5 sec)
	↓
Subsequent requests → Use warm container (<100ms)
	↓
No requests for ~20min → Container deallocated
```

### Scaling
```
Traffic spike (100+ req/sec)
	↓
Azure автоматично створює нові instances
	↓
Load balancer розподіляє запити
	↓
Traffic drops → Instances деалоковуються
```

### Статичний стан (ActiveChats)

⚠️ **ВАЖЛИВО:** `static ConcurrentDictionary<long, long> ActiveChats`

```
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│  Instance 1  │  │  Instance 2  │  │  Instance 3  │
│              │  │              │  │              │
│ ActiveChats  │  │ ActiveChats  │  │ ActiveChats  │
│ (окремий!)   │  │ (окремий!)   │  │ (окремий!)   │
└──────────────┘  └──────────────┘  └──────────────┘
```

**Проблема:** При масштабуванні кожен instance має свій словник!

**Рішення (для production):**
- Azure Redis Cache (розподілений кеш)
- Azure SQL Table (зберігати ActiveChats в БД)
- Azure Cosmos DB (NoSQL для швидкого доступу)

## Моніторинг (Application Insights)

```
Azure Function
	│
	├─ Log.Information() → Application Insights
	├─ Exceptions → Application Insights
	├─ Performance metrics → Application Insights
	└─ Custom events → Application Insights
		│
		▼
Azure Portal → Monitoring → Logs/Metrics/Alerts
```

### Метрики
- Function execution count
- Average duration
- Error rate
- Memory usage
- HTTP status codes

## Безпека

```
┌─────────────────────────────────────┐
│ Function Authorization Level        │
├─────────────────────────────────────┤
│ TelegramWebhook → Anonymous         │  (публічний endpoint)
│ SetWebhook → Function               │  (потрібен function key)
│ DeleteWebhook → Function            │  (потрібен function key)
│ GetWebhookInfo → Function           │  (потрібен function key)
└─────────────────────────────────────┘
```

**Додатково для production:**
- IP whitelist (тільки Telegram IP)
- Secret token в webhook URL
- Rate limiting

## Порівняння з іншими архітектурами

| Компонент | Console | WebAPI | Azure Functions |
|-----------|---------|--------|-----------------|
| Update receiving | Long Polling | Webhook | Webhook |
| Handler | Inline function | Service class | Service class |
| Hosting | Self-hosted | IIS/Kestrel | Azure Cloud |
| Scaling | Manual | Manual | Automatic |
| DI Container | Manual | Built-in | Built-in |
| Configuration | ConfigurationBuilder | appsettings.json | local.settings.json |
| Database | Same | Same | Same |
| DAL | Same | Same | Same |

**Спільне:** Вся бізнес-логіка в `TelegramUpdateHandler` однакова! 🎯
