# Тестування конфігурації

## Перевірте правильність налаштувань

### 1. Перевірте appsettings.json

Переконайтеся, що:
- ✅ `BotToken` встановлений (отримайте від [@BotFather](https://t.me/BotFather))
- ✅ `AdminChatIds` містить принаймні один Chat ID
- ✅ `ConnectionString` вказує на доступний SQL Server

### 2. Отримання Chat ID

Для отримання вашого Chat ID:
1. Напишіть боту [@userinfobot](https://t.me/userinfobot)
2. Або використайте [@getmyid_bot](https://t.me/getmyid_bot)
3. Скопіюйте `Id` (число) і додайте його в `AdminChatIds`

### 3. Перевірка компіляції

```bash
cd D:\Projects\example\2PeopleTelegramBot\2PeopleTelegramBot
dotnet build
```

Якщо компіляція успішна, ви побачите:
```
Build succeeded.
```

### 4. Перший запуск

```bash
dotnet run
```

Ви повинні побачити:
```
Запуск бота...
Бот @YOUR_BOT_NAME успішно запущений і працює!
Натисніть Enter для зупинки бота...
```

### 5. Тестування команд

1. Відкрийте Telegram і знайдіть вашого бота
2. Надішліть `/start` (з акаунту, який є в `AdminChatIds`)
3. Надішліть `/users` — повинен з'явитися список зареєстрованих користувачів

### 6. Тестування підключення двох користувачів

1. Попросіть двох користувачів написати боту `/start`
2. З акаунту адміністратора надішліть:
   ```
   /users
   ```
   Ви побачите список з Chat ID користувачів
3. З'єднайте їх:
   ```
   /connect CHAT_ID_1 CHAT_ID_2
   ```
4. Тепер вони можуть обмінюватися повідомленнями через бота

### 7. Можливі проблеми

#### Помилка підключення до БД
```
A network-related or instance-specific error occurred
```
**Рішення:** Змініть `ConnectionString` на:
```json
"Server=.\\SQLEXPRESS;Database=TelegramBotDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

#### Bot token is invalid
```
Telegram API Error: [401] Unauthorized
```
**Рішення:** Перевірте `BotToken` в `appsettings.json`

#### Get<List<long>> не працює
```
CS1061: Does not contain a definition for 'Get'
```
**Рішення:** Додайте пакет:
```bash
dotnet add package Microsoft.Extensions.Configuration.Binder
```
(Вже додано в проєкт)

## Готово! 🎉

Якщо всі кроки пройшли успішно, ваш бот готовий до роботи!
