# Telegram Bot для спілкування між двома користувачами

## Налаштування проєкту

### 1. Конфігурація бази даних

У файлі `appsettings.json` змініть connection string під вашу базу даних:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TelegramBotDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**⚠️ Важливо:** Connection string зберігається **тільки в appsettings.json**. 
- ✅ Міграції EF Core автоматично читають його з цього файлу
- ✅ Програма також читає його звідти
- ✅ Змінюйте тільки в одному місці!

Для SQL Server Express використовуйте:
```
Server=.\\SQLEXPRESS;Database=TelegramBotDb;Trusted_Connection=True;TrustServerCertificate=True;
```

Для повноцінного SQL Server:
```
Server=localhost;Database=TelegramBotDb;User Id=your_user;Password=your_password;TrustServerCertificate=True;
```

### 2. Конфігурація бота

У файлі `appsettings.json` встановіть ваші значення:

```json
{
  "BotConfiguration": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "AdminChatIds": [
      YOUR_FIRST_ADMIN_CHAT_ID,
      YOUR_SECOND_ADMIN_CHAT_ID
    ]
  }
}
```

**Примітка:** Ви можете додати декілька адміністраторів у масив `AdminChatIds`. Всі вони матимуть доступ до адміністративних команд.

### 3. Створення/оновлення бази даних

Виконайте команду для застосування міграцій:

```bash
cd 2PeopleTelegramBot
dotnet ef database update --project ..\2PeopleTB.DAL\2PeopleTB.DAL.csproj
```

Або база даних буде створена автоматично при першому запуску.

### 4. Запуск бота

```bash
cd 2PeopleTelegramBot
dotnet run
```

## Структура проєкту

### 2PeopleTB.DAL
- **Models/RegisteredUser.cs** - модель користувача (ChatId, Username, RegisteredAt)
- **Models/MessageHistory.cs** - модель історії повідомлень (FromChatId, ToChatId, MessageType, TextContent, FileId, SentAt)
- **Data/TelegramBotDbContext.cs** - контекст Entity Framework
- **Services/RegisteredUsersService.cs** - сервіс для роботи з користувачами
- **Services/MessageHistoryService.cs** - сервіс для роботи з історією повідомлень

### 2PeopleTelegramBot
- **Program.cs** - головна логіка бота
- **appsettings.json** - конфігурація (токен, ChatId адміна, connection string)

## Підтримувані типи повідомлень

Бот автоматично зберігає в базу даних наступні типи повідомлень:
- 📝 **Text** - текстові повідомлення
- 🖼️ **Photo** - фотографії (з підписом)
- 🎤 **Voice** - голосові повідомлення
- 🎵 **Audio** - аудіофайли (з підписом)
- 🎬 **Video** - відео (з підписом)
- 📄 **Document** - документи (з підписом)
- 😊 **Sticker** - стікери
- 🎥 **VideoNote** - кружечки (відеоповідомлення)

## Команди адміністратора

- `/users` - показати список всіх зареєстрованих користувачів
- `/connect ID1 ID2` - з'єднати двох користувачів для спілкування
- `/disconnect ID` - роз'єднати користувача
- `/history ID [кількість]` - показати історію повідомлень користувача (за замовчуванням 20, максимум 100)
- `/chat ID1 ID2 [кількість]` - показати переписку між двома користувачами (за замовчуванням 50, максимум 200)
- `/chat ID1 ID2 [кількість]` - показати переписку між двома користувачами (за замовчуванням 50, максимум 200)

## Технології

- .NET 10.0
- Entity Framework Core 9.0
- SQL Server / LocalDB
- Telegram.Bot 22.10.2
