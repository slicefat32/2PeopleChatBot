# Міграція між архітектурами

Цей документ описує різницю між трьома проєктами та як мігрувати між ними.

## Порівняння архітектур

### 1. 2PeopleTelegramBot (Console - Long Polling)

**Архітектура:** Console Application  
**Оновлення:** Long Polling  
**Hosting:** Self-hosted (ваш сервер/комп'ютер)

**Переваги:**
- ✅ Найпростіше налаштування
- ✅ Не потрібен публічний HTTPS endpoint
- ✅ Працює за NAT/firewall
- ✅ Легко дебажити локально

**Недоліки:**
- ❌ Треба тримати програму запущеною 24/7
- ❌ Затримка в отриманні повідомлень (polling interval)
- ❌ Більше навантаження на сервер
- ❌ Складніше масштабувати

**Коли використовувати:**
- Тестування та розробка
- Особистий бот для невеликої кількості користувачів
- Немає можливості налаштувати HTTPS webhook
- Запуск на локальному комп'ютері

---

### 2. 2PeopleTB.WebAPI (ASP.NET Core - Webhook)

**Архітектура:** ASP.NET Core Web API  
**Оновлення:** Webhook  
**Hosting:** IIS, Kestrel, Docker, будь-який ASP.NET Core сервер

**Переваги:**
- ✅ Миттєва доставка повідомлень
- ✅ Менше навантаження на сервер
- ✅ RESTful API structure
- ✅ Легко інтегрувати з іншими API
- ✅ Можна додати Swagger/OpenAPI

**Недоліки:**
- ❌ Потрібен публічний HTTPS endpoint
- ❌ Треба тримати сервер запущеним 24/7
- ❌ Складніше локальне тестування (потрібен ngrok)
- ❌ Треба керувати сервером

**Коли використовувати:**
- Production бот з постійним трафіком
- Інтеграція з іншими веб-сервісами
- Коли у вас вже є ASP.NET Core інфраструктура
- Docker/Kubernetes deployment

---

### 3. 2PeopleTB.AzureFunctions (Serverless - Webhook)

**Архітектура:** Azure Functions (Serverless)  
**Оновлення:** Webhook  
**Hosting:** Azure Cloud

**Переваги:**
- ✅ Автоматичне масштабування
- ✅ Оплата лише за виконання (consumption plan)
- ✅ Не треба керувати сервером
- ✅ Вбудований моніторинг (Application Insights)
- ✅ Миттєва доставка повідомлень
- ✅ HTTPS автоматично

**Недоліки:**
- ❌ Залежність від Azure
- ❌ Cold start (перший запит може бути повільнішим)
- ❌ Складніше налаштувати спочатку
- ❌ Можуть бути витрати за великого трафіку

**Коли використовувати:**
- Production бот зі змінним навантаженням
- Не хочете керувати серверами
- Потрібне автоматичне масштабування
- Startup/невеликий проєкт з обмеженим бюджетом

---

## Міграція між проєктами

### З Console → WebAPI

1. **Код майже не змінюється:**
   - `TelegramUpdateHandler` однаковий
   - DAL сервіси однакові

2. **Зміни:**
   ```diff
   - StartReceiving (polling)
   + Webhook endpoint приймає POST
   ```

3. **Кроки:**
   - Налаштуйте HTTPS сервер
   - Додайте `TelegramController`
   - Встановіть webhook через `SetWebhook`

### З WebAPI → Azure Functions

1. **Код майже не змінюється:**
   - `TelegramUpdateHandler` копіюється as-is
   - DAL сервіси однакові

2. **Зміни:**
   ```diff
   - Controller з ASP.NET Core
   + Azure Function з HttpTrigger

   - Program.cs з WebApplication.CreateBuilder
   + Program.cs з FunctionsApplication.CreateBuilder
   ```

3. **Кроки:**
   - Створіть Function App в Azure
   - Налаштуйте connection strings через Application Settings
   - Деплойте через `func azure functionapp publish`

### З Console → Azure Functions (пряма міграція)

Це найбільша зміна:

1. **Змініть спосіб отримання оновлень:**
   ```diff
   - await bot.StartReceiving(HandleUpdateAsync, ...);
   + [HttpTrigger] з Update від Telegram
   ```

2. **Змініть DI (Dependency Injection):**
   ```diff
   - Вручну створюєте сервіси
   + Реєструйте через builder.Services
   ```

3. **Видаліть polling loop:**
   ```diff
   - while (!cts.Token.IsCancellationRequested) { ... }
   + Webhook автоматично викликається
   ```

---

## Спільний код (DAL)

Усі три проєкти використовують **однакову DAL**:

- `TelegramBotDbContext`
- `RegisteredUsersService`
- `MessageHistoryService`
- `RegisteredUser` model
- `MessageHistory` model

**Це означає:**
- База даних однакова для всіх трьох
- Можна легко переключатися між проєктами
- Міграції EF Core працюють для всіх

---

## Спільна бізнес-логіка

`TelegramUpdateHandler.cs` - **майже однаковий** у всіх проєктах:

### Console (2PeopleTelegramBot/Program.cs)
```csharp
// Вбудований у Program.cs
async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
{
	// логіка обробки
}
```

### WebAPI (2PeopleTB.WebAPI/Services/TelegramUpdateHandler.cs)
```csharp
public class TelegramUpdateHandler
{
	public async Task HandleUpdateAsync(Update update, CancellationToken ct)
	{
		// та ж логіка
	}
}
```

### Azure Functions (2PeopleTB.AzureFunctions/Services/TelegramUpdateHandler.cs)
```csharp
public class TelegramUpdateHandler
{
	public async Task HandleUpdateAsync(Update update, CancellationToken ct)
	{
		// та ж логіка
	}
}
```

**Висновок:** Можна просто копіювати клас між проєктами!

---

## Вибір проєкту: Decision Tree

```
Чи у вас є Azure підписка?
│
├─ НІ
│  │
│  └─ Чи можете налаштувати HTTPS сервер?
│     │
│     ├─ НІ  → Використовуйте Console (Long Polling)
│     └─ ТАК → Використовуйте WebAPI
│
└─ ТАК
   │
   └─ Чи очікуєте велике навантаження?
	  │
	  ├─ ТАК → Azure Functions (автомасштабування)
	  └─ НІ  → Console або WebAPI (дешевше)
```

---

## Вартість

### Console App
- **Hosting:** Ваш сервер (~$5-20/місяць VPS)
- **Database:** SQL Server Express (безкоштовно) або Azure SQL (~$5/місяць)
- **Трафік:** Безкоштовно (polling)

**Загалом:** ~$5-25/місяць

### WebAPI
- **Hosting:** Azure App Service (~$10-50/місяць) або VPS (~$5-20/місяць)
- **Database:** Azure SQL (~$5/місяць)
- **Трафік:** Мінімальний

**Загалом:** ~$10-60/місяць

### Azure Functions
- **Compute:** Consumption Plan
  - Перші 1M executions безкоштовно
  - Потім $0.20 за 1M executions
- **Database:** Azure SQL (~$5/місяць)
- **Storage:** ~$0.10/місяць

**Для невеликого бота (< 100k повідомлень/місяць):**
**Загалом:** ~$5-10/місяць (майже безкоштовно!)

---

## Міграція даних

База даних **однакова** для всіх проєктів!

```powershell
# Backup з одного проєкту
cd 2PeopleTelegramBot
dotnet ef database script --output backup.sql

# Restore в інший проєкт
cd ..\2PeopleTB.AzureFunctions
# Виконайте backup.sql на новій БД
```

Або просто використовуйте **той самий connection string** у всіх проєктах!

---

## Рекомендації

| Сценарій | Рекомендація |
|----------|--------------|
| Розробка/тестування | **Console** (найпростіше) |
| Невеликий особистий бот | **Console** або **Azure Functions** |
| Середній бот (<10k користувачів) | **WebAPI** або **Azure Functions** |
| Великий бот (>10k користувачів) | **Azure Functions** (масштабування) |
| Startup з обмеженим бюджетом | **Azure Functions** (consumption) |
| Корпоративне середовище | **WebAPI** (повний контроль) |
| Інтеграція з іншими API | **WebAPI** (RESTful structure) |

---

## Висновок

Всі три проєкти мають **однаковий функціонал** і **спільну DAL**.

**Різниця** - лише в способі отримання оновлень та hosting:

- **Console:** Long Polling → самий простий
- **WebAPI:** Webhook → найбільш контрольований
- **Azure Functions:** Webhook + Serverless → найбільш масштабований

Вибирайте на основі ваших потреб та інфраструктури! 🚀
