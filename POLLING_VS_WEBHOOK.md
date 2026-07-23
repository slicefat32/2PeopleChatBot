# Long Polling vs Webhook - Порівняння

## Короткий підсумок

| Проєкт | Тип | Коли використовувати |
|--------|-----|---------------------|
| **2PeopleTelegramBot** | Long Polling | Розробка, тестування, невеликі боти |
| **2PeopleTB.WebAPI** | Webhook | Продакшн, масштабування, професійні боти |

---

## Детальне порівняння

### 1. Архітектура

#### Long Polling (2PeopleTelegramBot)
```
Console App → Telegram API (постійний запит кожні 1-2 сек)
                    ↓
            Отримує оновлення
                    ↓
            Обробляє в Main циклі
```

**Код:**
```csharp
botClient.StartReceiving(
    HandleUpdateAsync,
    HandlePollingErrorAsync,
    receiverOptions,
    cts.Token
);
```

#### Webhook (2PeopleTB.WebAPI)
```
Telegram API → POST https://yourdomain.com/api/telegram/webhook
                            ↓
                    TelegramController
                            ↓
                    TelegramUpdateHandler
```

**Код:**
```csharp
[HttpPost("webhook")]
public async Task<IActionResult> Webhook([FromBody] Update update)
{
    await _updateHandler.HandleUpdateAsync(update);
    return Ok();
}
```

---

### 2. Продуктивність

| Аспект | Long Polling | Webhook |
|--------|--------------|---------|
| **Затримка відповіді** | 1-2 секунди | <100ms |
| **Навантаження на сервер** | Постійне з'єднання | Тільки при повідомленні |
| **Споживання ресурсів** | Високе (постійний цикл) | Низьке (event-driven) |
| **Масштабування** | ❌ Важко | ✅ Легко (додати сервери) |

**Приклад:**
- **10 користувачів надсилають 1 повідомлення на годину:**
  - Long Polling: 3600 запитів/годину до Telegram
  - Webhook: 10 запитів/годину від Telegram

---

### 3. Деплой та хостинг

#### Long Polling
```bash
# Потрібен окремий процес
dotnet run

# Або Windows Service
sc create TelegramBot binPath="C:\path\to\bot.exe"

# Або systemd (Linux)
[Unit]
Description=Telegram Bot

[Service]
ExecStart=/usr/bin/dotnet /app/2PeopleTelegramBot.dll
Restart=always

[Install]
WantedBy=multi-user.target
```

**Проблеми:**
- ❌ Потрібно підтримувати окремий процес
- ❌ Важко моніторити
- ❌ Потрібен restart при оновленні

#### Webhook
```bash
# Стандартний ASP.NET деплой

# IIS
dotnet publish -c Release
# Копіюємо в IIS wwwroot

# Azure App Service
az webapp up --name mybot

# Docker
docker build -t telegram-bot .
docker run -d -p 80:80 telegram-bot
```

**Переваги:**
- ✅ Стандартний ASP.NET деплой
- ✅ Вбудований моніторинг
- ✅ Zero-downtime deployment

---

### 4. Dependency Injection

#### Long Polling
```csharp
// Manual DI
using var dbContext = new TelegramBotDbContext(optionsBuilder.Options);
_usersService = new RegisteredUsersService(dbContext);
_messageHistoryService = new MessageHistoryService(dbContext);
```

**Проблеми:**
- ❌ DbContext живе весь час (memory leak)
- ❌ Важко тестувати
- ❌ Немає lifecycle management

#### Webhook
```csharp
// ASP.NET DI
builder.Services.AddDbContext<TelegramBotDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<RegisteredUsersService>();
builder.Services.AddScoped<MessageHistoryService>();
```

**Переваги:**
- ✅ DbContext створюється на кожен запит (не ліке пам'ять)
- ✅ Легко тестувати (можна замокати сервіси)
- ✅ Proper lifecycle (Singleton, Scoped, Transient)

---

### 5. Логування

#### Long Polling
```csharp
Console.WriteLine($"[Новий користувач]: {chatId}");
Console.WriteLine($"Помилка: {ex.Message}");
```

**Проблеми:**
- ❌ Тільки Console
- ❌ Немає рівнів логування
- ❌ Важко фільтрувати
- ❌ Немає structured logging

#### Webhook
```csharp
_logger.LogInformation("Новий користувач: {ChatId}", chatId);
_logger.LogError(ex, "Помилка обробки update {UpdateId}", update.Id);
```

**Переваги:**
- ✅ Багато destination (Console, File, Database, AppInsights)
- ✅ Рівні (Debug, Info, Warning, Error)
- ✅ Structured logging
- ✅ Фільтрація за категоріями

**Приклад з Serilog:**
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/bot-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();
```

---

### 6. Моніторинг

#### Long Polling
- ❌ Потрібно писати вручну
- ❌ Важко інтегрувати з існуючими системами
- ❌ Немає вбудованих метрик

**Приклад (вручну):**
```csharp
var stopwatch = Stopwatch.StartNew();
await HandleUpdateAsync(update);
stopwatch.Stop();
Console.WriteLine($"Processed in {stopwatch.ElapsedMilliseconds}ms");
```

#### Webhook
- ✅ Application Insights (Azure)
- ✅ Prometheus + Grafana
- ✅ Вбудовані метрики ASP.NET
- ✅ Distributed tracing

**Приклад:**
```csharp
builder.Services.AddApplicationInsightsTelemetry();

// Автоматично:
// - Request duration
// - Success/failure rate
// - Exception tracking
// - Dependency tracking (SQL)
```

---

### 7. Безпека

#### Long Polling
- ⚠️ Бот робить запити → можна підмінити відповідь (MITM)
- ✅ Не потрібен публічний URL
- ✅ Не потрібен SSL сертифікат

#### Webhook
- ✅ Telegram робить запити → можна валідувати
- ⚠️ Потрібен публічний URL
- ⚠️ **ОБОВ'ЯЗКОВО** потрібен SSL сертифікат

**Валідація webhook:**
```csharp
[HttpPost("webhook")]
public async Task<IActionResult> Webhook([FromBody] Update update, [FromHeader(Name = "X-Telegram-Bot-Api-Secret-Token")] string secretToken)
{
    if (secretToken != _expectedSecretToken)
        return Unauthorized();
    
    await _updateHandler.HandleUpdateAsync(update);
    return Ok();
}
```

---

### 8. Вартість

#### Long Polling
**Переваги:**
- ✅ Можна запускати на власному комп'ютері (безкоштовно)
- ✅ Не потрібен домен
- ✅ Не потрібен SSL

**Вартість:** $0

#### Webhook
**Недоліки:**
- ⚠️ Потрібен хостинг
- ⚠️ Потрібен домен (або ngrok)
- ⚠️ Потрібен SSL сертифікат

**Вартість:**
- Azure App Service Free Tier: $0
- Azure App Service Basic: $13/місяць
- Shared VPS: $5/місяць
- Domain: $10/рік
- SSL (Let's Encrypt): $0

**Мінімум:** $0 (Azure Free + ngrok)
**Типово:** ~$20/місяць

---

### 9. Тестування

#### Long Polling
```csharp
// Важко тестувати
// Потрібно мокати TelegramBotClient
// Важко симулювати різні сценарії
```

**Проблеми:**
- ❌ Важко писати unit tests
- ❌ Важко симулювати Telegram оновлення
- ❌ Немає integration tests

#### Webhook
```csharp
// Unit tests
[Fact]
public async Task HandleUpdate_NewUser_RegistersUser()
{
    // Arrange
    var mockUsersService = new Mock<RegisteredUsersService>();
    var handler = new TelegramUpdateHandler(/*...*/);
    
    // Act
    await handler.HandleUpdateAsync(update);
    
    // Assert
    mockUsersService.Verify(s => s.AddUserAsync(123, "john"), Times.Once);
}

// Integration tests
public class TelegramControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Webhook_ValidUpdate_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/telegram/webhook", update);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

**Переваги:**
- ✅ Легко писати unit tests
- ✅ Легко писати integration tests
- ✅ Можна симулювати будь-які сценарії

---

### 10. Налаштування та конфігурація

#### Long Polling
**appsettings.json:**
```json
{
  "BotConfiguration": {
    "BotToken": "...",
    "AdminChatIds": [...]
  }
}
```

**Змінні середовища:**
```bash
# Не підтримується з коробки
# Потрібно додавати вручну
```

#### Webhook
**appsettings.json:**
```json
{
  "BotConfiguration": {
    "BotToken": "...",
    "AdminChatIds": [...],
    "WebhookUrl": "https://..."
  }
}
```

**Змінні середовища (автоматично):**
```bash
export BotConfiguration__BotToken="..."
export BotConfiguration__WebhookUrl="https://..."
```

**Azure App Settings:**
```bash
az webapp config appsettings set --name mybot \
  --settings BotConfiguration__BotToken="..."
```

---

## Коли використовувати що?

### Використовуйте Long Polling якщо:

✅ Розробка та локальне тестування
✅ Невеликий особистий бот
✅ Не потрібен 24/7 uptime
✅ Немає бюджету на хостинг
✅ Бот працює на вашому комп'ютері

**Приклади:**
- Особистий бот для автоматизації
- Тестування перед деплоєм
- Навчання/експерименти

---

### Використовуйте Webhook якщо:

✅ Продакшн бот (багато користувачів)
✅ Потрібен 24/7 uptime
✅ Важлива швидкість відповіді
✅ Потрібне масштабування
✅ Потрібен моніторинг та логування
✅ Інтеграція з іншими сервісами

**Приклади:**
- Комерційний бот
- Бот для компанії
- Бот з багатьма користувачами (100+)
- Бот з критичною функціональністю

---

## Міграція з Long Polling на Webhook

### Крок 1: Підготуйте WebAPI проєкт
```bash
cd 2PeopleTB.WebAPI
dotnet restore
```

### Крок 2: Налаштуйте appsettings.json
```json
{
  "BotConfiguration": {
    "BotToken": "SAME_AS_IN_CONSOLE_BOT",
    "AdminChatIds": [SAME_AS_IN_CONSOLE_BOT],
    "WebhookUrl": "https://yourdomain.com/api/telegram/webhook"
  }
}
```

### Крок 3: Зупиніть консольний бот
```bash
# Ctrl+C в терміналі де запущено консольний бот
```

### Крок 4: Видаліть webhook (якщо був)
```bash
curl -X POST "https://api.telegram.org/bot<TOKEN>/deleteWebhook"
```

### Крок 5: Запустіть WebAPI
```bash
dotnet run
# Webhook встановиться автоматично
```

### Крок 6: Перевірте
```bash
curl "https://api.telegram.org/bot<TOKEN>/getWebhookInfo"
```

Має повернути:
```json
{
  "url": "https://yourdomain.com/api/telegram/webhook",
  "pending_update_count": 0
}
```

---

## Підсумкова таблиця

| Критерій | Long Polling | Webhook | Переможець |
|----------|--------------|---------|------------|
| Швидкість | ⚠️ 1-2 сек | ✅ <100ms | **Webhook** |
| Вартість | ✅ $0 | ⚠️ $0-20/міс | **Long Polling** |
| Простота | ✅ Просто | ⚠️ Складніше | **Long Polling** |
| Масштабування | ❌ Важко | ✅ Легко | **Webhook** |
| Деплой | ⚠️ Manual | ✅ Стандартний | **Webhook** |
| Моніторинг | ❌ Немає | ✅ Вбудований | **Webhook** |
| Тестування | ❌ Важко | ✅ Легко | **Webhook** |
| Uptime | ⚠️ Залежить | ✅ 99.9% | **Webhook** |
| SSL/HTTPS | ✅ Не треба | ⚠️ Обов'язково | **Long Polling** |

---

## Рекомендації

### Для початку (навчання):
1. Почніть з **Long Polling** (2PeopleTelegramBot)
2. Розберіться як працює логіка
3. Протестуйте локально

### Для продакшну:
1. Перенесіть на **Webhook** (2PeopleTB.WebAPI)
2. Налаштуйте ngrok для розробки
3. Деплойте на Azure/іншому хостингу

### Гібридний підхід:
```
Розробка → Long Polling (швидко, локально)
    ↓
Тестування → Webhook + ngrok (як на продакшні)
    ↓
Продакшн → Webhook + Azure (масштабується, надійно)
```

---

## Висновок

**Обидва підходи мають місце бути:**

- **Long Polling** - ідеальний для швидкого старту, навчання, локальної розробки
- **Webhook** - правильний вибір для продакшн ботів з високими вимогами

**Наш проєкт підтримує обидва!** 🎉
- `2PeopleTelegramBot` - Long Polling
- `2PeopleTB.WebAPI` - Webhook

Логіка бізнесу ідентична, тільки спосіб отримання оновлень різний.
