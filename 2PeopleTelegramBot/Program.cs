using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using _2PeopleTB.DAL.Data;
using _2PeopleTB.DAL.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

class Program
{
    private static string BotToken = null!;
    private static List<long> AdminChatIds = null!;
    private static string ConnectionString = null!;

    // Зберігання користувачів у пам'яті (ChatId -> PartnerChatId)
    // Якщо значення -1, то користувач вільний (немає партнера)
    private static readonly ConcurrentDictionary<long, long> ActiveChats = new();

    private static RegisteredUsersService _usersService = null!;
    private static MessageHistoryService _messageHistoryService = null!;

    static async Task Main(string[] args)
    {
        // Завантаження конфігурації
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        BotToken = configuration["BotConfiguration:BotToken"]!;
        AdminChatIds = configuration.GetSection("BotConfiguration:AdminChatIds")
            .Get<List<long>>() ?? new List<long>();
        ConnectionString = configuration.GetConnectionString("DefaultConnection")!;

        // Ініціалізація DbContext та сервісу
        var optionsBuilder = new DbContextOptionsBuilder<TelegramBotDbContext>();
        optionsBuilder.UseSqlServer(ConnectionString);

        using var dbContext = new TelegramBotDbContext(optionsBuilder.Options);

        // Створення бази даних, якщо вона не існує
        await dbContext.Database.EnsureCreatedAsync();

        _usersService = new RegisteredUsersService(dbContext);
        _messageHistoryService = new MessageHistoryService(dbContext);

        var botClient = new TelegramBotClient(BotToken);

        using var cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // Отримувати всі типи оновлень
        };

        Console.WriteLine("Запуск бота...");
        botClient.StartReceiving(
            HandleUpdateAsync,
            HandlePollingErrorAsync,
            receiverOptions,
            cts.Token
        );

        var me = await botClient.GetMe();
        Console.WriteLine($"Бот @{me.Username} успішно запущений і працює!");
        Console.WriteLine("Натисніть Enter для зупинки бота...");
        Console.ReadLine();

        cts.Cancel();
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Обробляємо лише текстові та інші повідомлення
        if (update.Message is not { } message)
            return;

        long chatId = message.Chat.Id;

        // Реєструємо користувача, якщо він новий
        if (!await _usersService.UserExistsAsync(chatId))
        {
            string username = message.From?.Username ?? "без нікнейму";
            await _usersService.AddUserAsync(chatId, username);
            Console.WriteLine($"[Новий користувач]: {chatId} (@{username})");
        }

        // 1. ОБРОБКА КОМАНД АДМІНІСТРАТОРА
        if (AdminChatIds.Contains(chatId) && message.Text != null && message.Text.StartsWith("/"))
        {
            await HandleAdminCommandsAsync(botClient, message, cancellationToken);
            return;
        }

        // 2. ОБРОБКА СТАНДАРТНОЇ КОМАНДИ /start ДЛЯ КОРИСТУВАЧІВ
        if (message.Text == "/start")
        {
            ActiveChats.TryAdd(chatId, -1); // Додаємо вільним у словник
            await botClient.SendMessage(
                chatId: chatId,
                text: "Вітаю! Ви успішно зареєструвалися в боті. Будь ласка, зачекайте, поки адміністратор з'єднає вас із співрозмовником.",
                cancellationToken: cancellationToken
            );
            return;
        }

        // 3. РЕЛЕ (ПЕРЕСИЛАННЯ ПОВІДОМЛЕНЬ МІЖ ПАРТНЕРАМИ)
        if (ActiveChats.TryGetValue(chatId, out long partnerId) && partnerId != -1)
        {
            try
            {
                // Визначаємо тип повідомлення та витягуємо відповідні дані
                string messageType = "Unknown";
                string? textContent = null;
                string? fileId = null;

                if (message.Text != null)
                {
                    messageType = "Text";
                    textContent = message.Text;
                }
                else if (message.Photo != null && message.Photo.Length > 0)
                {
                    messageType = "Photo";
                    fileId = message.Photo[^1].FileId;
                    textContent = message.Caption;
                }
                else if (message.Voice != null)
                {
                    messageType = "Voice";
                    fileId = message.Voice.FileId;
                }
                else if (message.Audio != null)
                {
                    messageType = "Audio";
                    fileId = message.Audio.FileId;
                    textContent = message.Caption;
                }
                else if (message.Video != null)
                {
                    messageType = "Video";
                    fileId = message.Video.FileId;
                    textContent = message.Caption;
                }
                else if (message.Document != null)
                {
                    messageType = "Document";
                    fileId = message.Document.FileId;
                    textContent = message.Caption;
                }
                else if (message.Sticker != null)
                {
                    messageType = "Sticker";
                    fileId = message.Sticker.FileId;
                }
                else if (message.VideoNote != null)
                {
                    messageType = "VideoNote";
                    fileId = message.VideoNote.FileId;
                }

                // Метод CopyMessage просто дублює повідомлення (текст, фото, стікер, голос) 
                // у чат партнера без плашки "Переслано від..."
                await botClient.CopyMessage(
                    chatId: partnerId,
                    fromChatId: chatId,
                    messageId: message.MessageId,
                    cancellationToken: cancellationToken
                );

                // Зберігаємо повідомлення в історію
                await _messageHistoryService.SaveMessageAsync(
                    fromChatId: chatId,
                    toChatId: partnerId,
                    messageId: message.MessageId,
                    messageType: messageType,
                    textContent: textContent,
                    fileId: fileId
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка пересилання повідомлення від {chatId} до {partnerId}: {ex.Message}");
                await botClient.SendMessage(chatId, "Не вдалося доставити повідомлення. Можливо, ваш партнер заблокував бота.", cancellationToken: cancellationToken);
            }
        }
        else
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "Ви зараз ні з чим не з'єднані. Очікуйте на підключення адміністратором.",
                cancellationToken: cancellationToken
            );
        }
    }

    private static async Task HandleAdminCommandsAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        string[] parts = message.Text!.Split(' ');
        string command = parts[0].ToLower();

        // Команда: /connect [ID_1] [ID_2]
        if (command == "/connect" && parts.Length == 3)
        {
            if (long.TryParse(parts[1], out long user1) && long.TryParse(parts[2], out long user2))
            {
                // Записуємо зв'язок в обидва боки
                ActiveChats[user1] = user2;
                ActiveChats[user2] = user1;

                // Повідомляємо адміна
                await botClient.SendMessage(message.Chat.Id, $"Успішно з'єднано {user1} та {user2}!", cancellationToken: cancellationToken);

                // Повідомляємо користувачів
                await botClient.SendMessage(user1, "Адміністратор підключив вас до розмови. Можете спілкуватися! 😉", cancellationToken: cancellationToken);
                await botClient.SendMessage(user2, "Адміністратор підключив вас до розмови. Можете спілкуватися! 😉", cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendMessage(message.Chat.Id, "Невірний формат ID. Спробуйте: /connect [ID_1] [ID_2]", cancellationToken: cancellationToken);
            }
        }
        // Команда: /disconnect [ID]
        else if (command == "/disconnect" && parts.Length == 2)
        {
            if (long.TryParse(parts[1], out long user))
            {
                if (ActiveChats.TryGetValue(user, out long partner) && partner != -1)
                {
                    ActiveChats[user] = -1;
                    ActiveChats[partner] = -1;

                    await botClient.SendMessage(message.Chat.Id, $"Користувачів {user} та {partner} успішно роз'єднано.", cancellationToken: cancellationToken);
                    await botClient.SendMessage(user, "Ваш діалог було завершено адміністратором. ❌", cancellationToken: cancellationToken);
                    await botClient.SendMessage(partner, "Ваш діалог було завершено адміністратором. ❌", cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.SendMessage(message.Chat.Id, "Цей користувач не перебуває в активному діалозі.", cancellationToken: cancellationToken);
                }
            }
        }
        // Команда: /users (показує список усіх, хто запустив бота)
        else if (command == "/users")
        {
            var users = await _usersService.GetAllUsersAsync();
            string list = "Список зареєстрованих користувачів:\n";
            foreach (var user in users)
            {
                ActiveChats.TryGetValue(user.ChatId, out long partner);
                string status = partner != -1 ? $"в чаті з {partner}" : "вільний";
                list += $"• `{user.ChatId}` (@{user.Username}) — {status}\n";
            }
            await botClient.SendMessage(message.Chat.Id, list, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
        }
        // Команда: /history [ID] [кількість] (показує історію повідомлень користувача)
        else if (command == "/history" && parts.Length >= 2)
        {
            if (long.TryParse(parts[1], out long userId))
            {
                int count = 20;
                if (parts.Length == 3 && int.TryParse(parts[2], out int customCount))
                {
                    count = Math.Min(customCount, 100); // Максимум 100 повідомлень
                }

                var history = await _messageHistoryService.GetUserMessageHistoryAsync(userId, count);

                if (history.Count == 0)
                {
                    await botClient.SendMessage(message.Chat.Id, $"Історія повідомлень для користувача {userId} порожня.", cancellationToken: cancellationToken);
                    return;
                }

                string historyText = $"📜 Історія повідомлень користувача `{userId}` (останні {history.Count}):\n\n";
                foreach (var msg in history.OrderBy(m => m.SentAt))
                {
                    string direction = msg.FromChatId == userId ? "➡️" : "⬅️";
                    string msgInfo = $"{direction} {msg.SentAt:dd.MM.yyyy HH:mm} | {msg.MessageType}";
                    if (!string.IsNullOrEmpty(msg.TextContent))
                    {
                        msgInfo += $"\n   {msg.TextContent.Substring(0, Math.Min(50, msg.TextContent.Length))}...";
                    }
                    historyText += msgInfo + "\n\n";
                }

                await botClient.SendMessage(message.Chat.Id, historyText, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendMessage(message.Chat.Id, "Невірний формат ID. Спробуйте: /history [ID] [кількість]", cancellationToken: cancellationToken);
            }
        }
        // Команда: /chat [ID1] [ID2] [кількість] (показує переписку між двома користувачами)
        else if (command == "/chat" && parts.Length >= 3)
        {
            if (long.TryParse(parts[1], out long user1Id) && long.TryParse(parts[2], out long user2Id))
            {
                int count = 50;
                if (parts.Length == 4 && int.TryParse(parts[3], out int customCount))
                {
                    count = Math.Min(customCount, 200); // Максимум 200 повідомлень
                }

                var chatHistory = await _messageHistoryService.GetMessageHistoryBetweenUsersAsync(user1Id, user2Id, count);

                if (chatHistory.Count == 0)
                {
                    await botClient.SendMessage(message.Chat.Id, $"Переписка між користувачами {user1Id} та {user2Id} порожня.", cancellationToken: cancellationToken);
                    return;
                }

                // Отримуємо імена користувачів
                var user1 = await _usersService.GetUserAsync(user1Id);
                var user2 = await _usersService.GetUserAsync(user2Id);
                string user1Name = user1?.Username ?? user1Id.ToString();
                string user2Name = user2?.Username ?? user2Id.ToString();

                string chatText = $"💬 Переписка між `{user1Name}` та `{user2Name}` (останні {chatHistory.Count}):\n\n";

                foreach (var msg in chatHistory.OrderBy(m => m.SentAt))
                {
                    string senderName = msg.FromChatId == user1Id ? user1Name : user2Name;
                    string emoji = msg.FromChatId == user1Id ? "👤" : "👥";

                    string msgInfo = $"{emoji} **{senderName}** ({msg.SentAt:dd.MM HH:mm})\n";
                    msgInfo += $"   📋 {msg.MessageType}";

                    if (!string.IsNullOrEmpty(msg.TextContent))
                    {
                        string text = msg.TextContent.Length > 100 
                            ? msg.TextContent.Substring(0, 100) + "..." 
                            : msg.TextContent;
                        msgInfo += $"\n   💬 _{text}_";
                    }

                    chatText += msgInfo + "\n\n";
                }

                await botClient.SendMessage(message.Chat.Id, chatText, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendMessage(message.Chat.Id, "Невірний формат ID. Спробуйте: /chat [ID1] [ID2] [кількість]", cancellationToken: cancellationToken);
            }
        }
        else
        {
            await botClient.SendMessage(message.Chat.Id, "Невідома команда. Доступні:\n/connect ID1 ID2\n/disconnect ID\n/users\n/history ID [кількість]\n/chat ID1 ID2 [кількість]", cancellationToken: cancellationToken);
        }
    }

    private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Помилка Telegram API: [{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
}