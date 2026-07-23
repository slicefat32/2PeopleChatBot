# Приклад налаштування декількох адміністраторів

## appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TelegramBotDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "BotConfiguration": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "AdminChatIds": [
      123456789,    // Перший адміністратор
      987654321,    // Другий адміністратор
      555666777     // Третій адміністратор (додайте скільки потрібно)
    ]
  }
}
```

## Як дізнатися свій Chat ID?

1. Напишіть боту [@userinfobot](https://t.me/userinfobot)
2. Або напишіть [@getmyid_bot](https://t.me/getmyid_bot)
3. Ці боти повернуть ваш Chat ID

## Приклад з одним адміністратором

```json
{
  "BotConfiguration": {
    "BotToken": "8847702084:AAGt_fDEU3mbw16TQRVL1Qcwnb5KiPljpII",
    "AdminChatIds": [
      6520341847
    ]
  }
}
```

## Приклад з трьома адміністраторами

```json
{
  "BotConfiguration": {
    "BotToken": "8847702084:AAGt_fDEU3mbw16TQRVL1Qcwnb5KiPljpII",
    "AdminChatIds": [
      6520341847,
      1234567890,
      9876543210
    ]
  }
}
```

## Як працює перевірка адміністратора в коді?

```csharp
// В Program.cs, рядок 92-97:
if (AdminChatIds.Contains(chatId) && message.Text != null && message.Text.StartsWith("/"))
{
    await HandleAdminCommandsAsync(botClient, message, cancellationToken);
    return;
}
```

Бот перевіряє, чи знаходиться `chatId` відправника повідомлення у списку `AdminChatIds`. 
Якщо так — виконує адміністративні команди.
Якщо ні — обробляє як звичайне повідомлення користувача.
