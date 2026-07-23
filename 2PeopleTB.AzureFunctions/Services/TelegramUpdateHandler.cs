using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Types;
using _2PeopleTB.DAL.Services;

namespace _2PeopleTB.AzureFunctions.Services
{
    public class TelegramUpdateHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly RegisteredUsersService _usersService;
        private readonly MessageHistoryService _messageHistoryService;
        private readonly List<long> _adminChatIds;

        // Зберігання активних чатів (ChatId -> PartnerChatId)
        // -1 означає що користувач вільний
        private static readonly ConcurrentDictionary<long, long> ActiveChats = new();

        public TelegramUpdateHandler(
            ITelegramBotClient botClient,
            RegisteredUsersService usersService,
            MessageHistoryService messageHistoryService,
            List<long> adminChatIds)
        {
            _botClient = botClient;
            _usersService = usersService;
            _messageHistoryService = messageHistoryService;
            _adminChatIds = adminChatIds;
        }

        public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken = default)
        {
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
            if (_adminChatIds.Contains(chatId) && message.Text != null && message.Text.StartsWith("/"))
            {
                await HandleAdminCommandsAsync(message, cancellationToken);
                return;
            }

            // 2. ОБРОБКА СТАНДАРТНОЇ КОМАНДИ /start ДЛЯ КОРИСТУВАЧІВ
            if (message.Text == "/start")
            {
                ActiveChats.TryAdd(chatId, -1);
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "Вітаю! Ви успішно зареєструвалися в боті. Будь ласка, зачекайте, поки адміністратор з'єднає вас із співрозмовником.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            // 3. РЕЛЕ (ПЕРЕСИЛАННЯ ПОВІДОМЛЕНЬ МІЖ ПАРТНЕРАМИ)
            if (ActiveChats.TryGetValue(chatId, out long partnerId) && partnerId != -1)
            {
                await HandleMessageRelayAsync(message, chatId, partnerId, cancellationToken);
            }
            else
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "Ви зараз ні з ким не з'єднані. Очікуйте на підключення адміністратором.",
                    cancellationToken: cancellationToken
                );
            }
        }

        private async Task HandleMessageRelayAsync(Message message, long chatId, long partnerId, CancellationToken cancellationToken)
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

                // Пересилаємо повідомлення партнеру
                await _botClient.CopyMessage(
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
                await _botClient.SendMessage(chatId, "Не вдалося доставити повідомлення. Можливо, ваш партнер заблокував бота.", cancellationToken: cancellationToken);
            }
        }

        private async Task HandleAdminCommandsAsync(Message message, CancellationToken cancellationToken)
        {
            string[] parts = message.Text!.Split(' ');
            string command = parts[0].ToLower();

            // Команда: /connect [ID_1] [ID_2]
            if (command == "/connect" && parts.Length == 3)
            {
                if (long.TryParse(parts[1], out long user1) && long.TryParse(parts[2], out long user2))
                {
                    ActiveChats[user1] = user2;
                    ActiveChats[user2] = user1;

                    await _botClient.SendMessage(message.Chat.Id, $"Успішно з'єднано {user1} та {user2}!", cancellationToken: cancellationToken);
                    await _botClient.SendMessage(user1, "Ви підключені! Можете розпочати переписку.", cancellationToken: cancellationToken);
                    await _botClient.SendMessage(user2, "Ви підключені! Можете розпочати переписку.", cancellationToken: cancellationToken);
                }
                else
                {
                    await _botClient.SendMessage(message.Chat.Id, "❌ Невірний формат. Використовуйте: /connect [ID_1] [ID_2]", cancellationToken: cancellationToken);
                }
                return;
            }

            // Команда: /disconnect [ID]
            if (command == "/disconnect" && parts.Length == 2)
            {
                if (long.TryParse(parts[1], out long userId))
                {
                    if (ActiveChats.TryGetValue(userId, out long partnerId))
                    {
                        ActiveChats[userId] = -1;
                        if (partnerId != -1)
                        {
                            ActiveChats[partnerId] = -1;
                            await _botClient.SendMessage(partnerId, "Ваше з'єднання розірвано адміністратором.", cancellationToken: cancellationToken);
                        }

                        await _botClient.SendMessage(message.Chat.Id, $"Користувача {userId} роз'єднано.", cancellationToken: cancellationToken);
                        await _botClient.SendMessage(userId, "Ваше з'єднання розірвано адміністратором.", cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await _botClient.SendMessage(message.Chat.Id, "❌ Цей користувач не був у активних чатах.", cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    await _botClient.SendMessage(message.Chat.Id, "❌ Невірний формат. Використовуйте: /disconnect [ID]", cancellationToken: cancellationToken);
                }
                return;
            }

            // Команда: /users
            if (command == "/users")
            {
                var users = await _usersService.GetAllUsersAsync();
                if (users.Count == 0)
                {
                    await _botClient.SendMessage(message.Chat.Id, "Зареєстрованих користувачів немає.", cancellationToken: cancellationToken);
                    return;
                }

                string userList = "📋 Зареєстровані користувачі:\n\n";
                foreach (var user in users)
                {
                    string status = ActiveChats.TryGetValue(user.ChatId, out long partner)
                        ? (partner == -1 ? "🟡 Вільний" : $"🟢 З'єднаний з {partner}")
                        : "⚪ Не активний";

                    userList += $"ID: {user.ChatId}\n";
                    userList += $"   Username: @{user.Username}\n";
                    userList += $"   Статус: {status}\n";
                    userList += $"   Зареєстрований: {user.RegisteredAt:dd.MM.yyyy HH:mm}\n\n";
                }

                await _botClient.SendMessage(message.Chat.Id, userList, cancellationToken: cancellationToken);
                return;
            }

            // Команда: /history [ID]
            if (command == "/history" && parts.Length == 2)
            {
                if (long.TryParse(parts[1], out long userId))
                {
                    var history = await _messageHistoryService.GetUserMessageHistoryAsync(userId, count: 50);

                    if (history.Count == 0)
                    {
                        await _botClient.SendMessage(message.Chat.Id, $"Історії для користувача {userId} не знайдено.", cancellationToken: cancellationToken);
                        return;
                    }

                    string historyText = $"📜 Історія повідомлень користувача {userId}:\n\n";
                    foreach (var msg in history)
                    {
                        historyText += $"[{msg.SentAt:dd.MM HH:mm}] {msg.FromChatId} → {msg.ToChatId}\n";
                        historyText += $"   Тип: {msg.MessageType}\n";
                        if (!string.IsNullOrEmpty(msg.TextContent))
                            historyText += $"   Текст: {msg.TextContent}\n";
                        historyText += "\n";
                    }

                    await _botClient.SendMessage(message.Chat.Id, historyText, cancellationToken: cancellationToken);
                }
                else
                {
                    await _botClient.SendMessage(message.Chat.Id, "❌ Невірний формат. Використовуйте: /history [ID]", cancellationToken: cancellationToken);
                }
                return;
            }

            // Команда: /chat [ID_1] [ID_2]
            if (command == "/chat" && parts.Length == 3)
            {
                if (long.TryParse(parts[1], out long user1) && long.TryParse(parts[2], out long user2))
                {
                    var history = await _messageHistoryService.GetMessageHistoryBetweenUsersAsync(user1, user2, count: 100);

                    if (history.Count == 0)
                    {
                        await _botClient.SendMessage(message.Chat.Id, $"Історії переписки між {user1} та {user2} не знайдено.", cancellationToken: cancellationToken);
                        return;
                    }

                    // Отримуємо інформацію про користувачів
                    var userInfo1 = await _usersService.GetUserAsync(user1);
                    var userInfo2 = await _usersService.GetUserAsync(user2);

                    string username1 = userInfo1?.Username ?? user1.ToString();
                    string username2 = userInfo2?.Username ?? user2.ToString();

                    string chatHistory = $"💬 Переписка між @{username1} та @{username2}:\n\n";
                    foreach (var msg in history)
                    {
                        string senderName = msg.FromChatId == user1 ? username1 : username2;
                        chatHistory += $"[{msg.SentAt:dd.MM HH:mm}] @{senderName}:\n";

                        if (!string.IsNullOrEmpty(msg.TextContent))
                            chatHistory += $"   {msg.TextContent}\n";
                        else
                            chatHistory += $"   [{msg.MessageType}]\n";

                        chatHistory += "\n";
                    }

                    await _botClient.SendMessage(message.Chat.Id, chatHistory, cancellationToken: cancellationToken);
                }
                else
                {
                    await _botClient.SendMessage(message.Chat.Id, "❌ Невірний формат. Використовуйте: /chat [ID_1] [ID_2]", cancellationToken: cancellationToken);
                }
                return;
            }

            // Невідома команда
            await _botClient.SendMessage(
                message.Chat.Id,
                "❓ Доступні команди адміністратора:\n" +
                "/connect [ID_1] [ID_2] — з'єднати двох користувачів\n" +
                "/disconnect [ID] — роз'єднати користувача\n" +
                "/users — список усіх зареєстрованих користувачів\n" +
                "/history [ID] — історія повідомлень користувача\n" +
                "/chat [ID_1] [ID_2] — переписка між двома користувачами",
                cancellationToken: cancellationToken
            );
        }
    }
}
