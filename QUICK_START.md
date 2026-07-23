# Швидкий старт

## 1. Клонуйте репозиторій
```bash
git clone <your-repo>
cd 2PeopleTelegramBot
```

## 2. Налаштуйте appsettings.json

**Відкрийте:** `2PeopleTelegramBot/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TelegramBotDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "BotConfiguration": {
    "BotToken": "ВАШ_ТОКЕН_ВІД_BOTFATHER",
    "AdminChatIds": [
      ВАШ_CHAT_ID
    ]
  }
}
```

### Як отримати Bot Token?
1. Напишіть [@BotFather](https://t.me/BotFather) в Telegram
2. Виконайте команду `/newbot`
3. Дайте ім'я боту
4. Скопіюйте отриманий токен

### Як дізнатися свій Chat ID?
1. Напишіть [@userinfobot](https://t.me/userinfobot)
2. Скопіюйте `Id` (число)

## 3. Створіть базу даних

```bash
cd 2PeopleTelegramBot
dotnet ef database update --project ..\2PeopleTB.DAL\2PeopleTB.DAL.csproj
```

**Або** база даних створиться автоматично при першому запуску.

## 4. Запустіть бота

```bash
dotnet run
```

Ви побачите:
```
Запуск бота...
Бот @YourBotName успішно запущений і працює!
Натисніть Enter для зупинки бота...
```

## 5. Тестуйте бота

### Крок 1: Зареєструйтеся
Напишіть боту в Telegram:
```
/start
```

### Крок 2: Перевірте список користувачів (з акаунту адміна)
```
/users
```

Результат:
```
Список зареєстрованих користувачів:
• `123456789` (@yourname) — вільний
```

### Крок 3: Попросіть іншого користувача написати /start

### Крок 4: З'єднайте користувачів
```
/connect 123456789 987654321
```

### Крок 5: Тепер вони можуть обмінюватися повідомленнями!

---

## Команди адміністратора

| Команда | Опис |
|---------|------|
| `/users` | Список всіх користувачів |
| `/connect ID1 ID2` | З'єднати двох користувачів |
| `/disconnect ID` | Роз'єднати користувача |
| `/history ID [N]` | Історія користувача (N повідомлень) |
| `/chat ID1 ID2 [N]` | Переписка між користувачами |

---

## Можливі проблеми

### Помилка: "Bot token is invalid"
**Рішення:** Перевірте токен в `appsettings.json`

### Помилка: "Cannot connect to database"
**Рішення:** 
1. Перевірте, чи встановлений SQL Server / LocalDB
2. Змініть connection string на `.\\SQLEXPRESS` або `localhost`

### Бот не відповідає на команди
**Рішення:** Переконайтеся, що ваш Chat ID в списку `AdminChatIds`

---

## Структура проєкту

```
2PeopleTelegramBot/
├── 2PeopleTelegramBot/          # Головний проєкт
│   ├── Program.cs               # Логіка бота
│   └── appsettings.json         # Конфігурація (ВСЕ НАЛАШТУВАННЯ ТУТ!)
│
└── 2PeopleTB.DAL/               # Data Access Layer
    ├── Data/
    │   ├── TelegramBotDbContext.cs
    │   └── TelegramBotDbContextFactory.cs  ← Читає appsettings.json
    ├── Models/
    │   ├── RegisteredUser.cs
    │   └── MessageHistory.cs
    └── Services/
        ├── RegisteredUsersService.cs
        └── MessageHistoryService.cs
```

---

## Налаштування для різних баз даних

### LocalDB (за замовчуванням)
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TelegramBotDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

### SQL Server Express
```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=TelegramBotDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

### SQL Server з паролем
```json
"DefaultConnection": "Server=localhost;Database=TelegramBotDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
```

### Azure SQL
```json
"DefaultConnection": "Server=tcp:yourserver.database.windows.net,1433;Database=TelegramBotDb;User ID=yourusername;Password=yourpassword;Encrypt=True;"
```

---

## Додаткова документація

- [README.md](README.md) - Повна документація
- [ADMIN_COMMANDS.md](ADMIN_COMMANDS.md) - Детально про команди адміна
- [MESSAGE_HISTORY.md](MESSAGE_HISTORY.md) - Робота з історією
- [CHAT_COMMAND.md](CHAT_COMMAND.md) - Команда /chat
- [DBCONTEXT_FACTORY.md](DBCONTEXT_FACTORY.md) - Як працює конфігурація БД

---

## FAQ

### Чи можна додати декілька адміністраторів?
Так! Додайте ID в масив:
```json
"AdminChatIds": [123456789, 987654321, 555666777]
```

### Де зберігаються повідомлення?
В SQL Server базі даних. Таблиця `MessageHistories`.

### Чи зберігаються файли (фото/відео)?
Зберігається тільки `FileId` - посилання на файл у Telegram.

### Як експортувати історію?
Використовуйте SQL запити з [SQL_QUERIES.md](SQL_QUERIES.md)

### Як змінити connection string?
Змініть **тільки** в `appsettings.json`. Більше ніде!

---

## Підтримка

Для детальної інформації дивіться:
- [README.md](README.md) - Головна документація
- [TESTING.md](TESTING.md) - Тестування
- Всі `.md` файли в корені проєкту
