# Локальна розробка з Ngrok - Швидкий старт

## Крок 1: Встановіть ngrok

### Windows (Chocolatey)
```bash
choco install ngrok
```

### Windows (ручне встановлення)
1. Завантажте з https://ngrok.com/download
2. Розпакуйте `ngrok.exe`
3. Додайте до PATH або запускайте з папки

### Реєстрація (безкоштовно)
```bash
ngrok config add-authtoken YOUR_AUTH_TOKEN
```
Отримайте токен на https://dashboard.ngrok.com/get-started/your-authtoken

---

## Крок 2: Запустіть WebAPI

```bash
cd D:\Projects\example\2PeopleTelegramBot\2PeopleTB.WebAPI
dotnet run
```

Ви побачите:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7001
      
🚀 Telegram Bot WebAPI запущено!
📡 Webhook URL: не налаштовано
```

**Запам'ятайте порт:** `7001` (або який у вас)

---

## Крок 3: Запустіть ngrok (НОВИЙ ТЕРМІНАЛ!)

```bash
ngrok http https://localhost:7001
```

Ви побачите:
```
Session Status                online
Account                       Your Name (Plan: Free)
Version                       3.x.x
Region                        United States (us)
Latency                       50ms
Web Interface                 http://127.0.0.1:4040
Forwarding                    https://1a2b-3c4d-5e6f.ngrok-free.app -> https://localhost:7001

Connections                   ttl     opn     rt1     rt5     p50     p90
                              0       0       0.00    0.00    0.00    0.00
```

**Важливо:** Скопіюйте URL з рядка `Forwarding`:
```
https://1a2b-3c4d-5e6f.ngrok-free.app
```

---

## Крок 4: Оновіть appsettings.json

**Відкрийте:** `2PeopleTB.WebAPI/appsettings.json`

**Змініть WebhookUrl:**
```json
{
  "BotConfiguration": {
    "BotToken": "8847702084:AAGt_fDEU3mbw16TQRVL1Qcwnb5KiPljpII",
    "AdminChatIds": [6520341847],
    "WebhookUrl": "https://1a2b-3c4d-5e6f.ngrok-free.app/api/telegram/webhook"
  }
}
```

⚠️ **Замініть на ваш ngrok URL!**

---

## Крок 5: Перезапустіть WebAPI

**Зупиніть:** `Ctrl+C` в терміналі де запущено `dotnet run`

**Запустіть знову:**
```bash
dotnet run
```

Ви побачите:
```
🚀 Telegram Bot WebAPI запущено!
📡 Webhook URL: https://1a2b-3c4d-5e6f.ngrok-free.app/api/telegram/webhook
info: ... Webhook встановлено: https://1a2b-3c4d-5e6f.ngrok-free.app/api/telegram/webhook
```

---

## Крок 6: Тестуйте бота!

Напишіть боту в Telegram:
```
/start
```

В консолі WebAPI побачите:
```
info: ... Received update: 123456789
[Новий користувач]: 6520341847 (@yourname)
```

В ngrok терміналі побачите:
```
POST /api/telegram/webhook     200 OK
```

---

## Перегляд запитів (Web UI)

Відкрийте в браузері:
```
http://127.0.0.1:4040
```

Побачите всі HTTP запити від Telegram в реальному часі!

---

## Типові проблеми

### ❌ Ngrok показує "Invalid Host Header"

**Причина:** Ngrok безкоштовний план додає перевірку header.

**Рішення:** Додайте в `appsettings.Development.json`:
```json
{
  "AllowedHosts": "*"
}
```

Або запускайте ngrok так:
```bash
ngrok http https://localhost:7001 --host-header=localhost:7001
```

---

### ❌ "Webhook встановлено" але повідомлення не приходять

**Перевірка 1:** Переконайтеся що WebAPI запущено
```bash
# Має бути "Now listening on: https://localhost:7001"
```

**Перевірка 2:** Перевірте ngrok
```bash
# Має бути "Session Status: online"
# Має бути "Forwarding: https://... -> https://localhost:7001"
```

**Перевірка 3:** Перевірте webhook в Telegram
```bash
curl "https://api.telegram.org/bot8847702084:AAGt_fDEU3mbw16TQRVL1Qcwnb5KiPljpII/getWebhookInfo"
```

Має повернути:
```json
{
  "ok": true,
  "result": {
    "url": "https://1a2b-3c4d-5e6f.ngrok-free.app/api/telegram/webhook",
    "has_custom_certificate": false,
    "pending_update_count": 0
  }
}
```

---

### ❌ Ngrok URL змінюється при кожному запуску

**Причина:** Безкоштовний план ngrok генерує випадковий URL.

**Рішення 1:** Оплатіть ngrok ($5/місяць) для статичного домену

**Рішення 2:** Використовуйте ngrok config:
```bash
ngrok http 7001 --domain=your-static-domain.ngrok.app
```

**Рішення 3:** Автоматично оновлюйте webhook при запуску (вже реалізовано в Program.cs)

---

## Workflow для розробки

### Щоразу коли починаєте роботу:

**Термінал 1:**
```bash
cd 2PeopleTB.WebAPI
dotnet run
```

**Термінал 2:**
```bash
ngrok http https://localhost:7001
```

**Скопіюйте ngrok URL з терміналу 2 → вставте в appsettings.json → перезапустіть термінал 1**

---

### Коли закінчуєте роботу:

**Термінал 1:** `Ctrl+C` (зупинити WebAPI)

**Термінал 2:** `Ctrl+C` (зупинити ngrok)

---

## Альтернативи ngrok

### Cloudflare Tunnel (безкоштовно, статичний домен)
```bash
cloudflared tunnel --url https://localhost:7001
```

### LocalTunnel (безкоштовно)
```bash
npx localtunnel --port 7001 --subdomain mybot
```

### Serveo (SSH тунель)
```bash
ssh -R mybot:80:localhost:7001 serveo.net
```

---

## Корисні команди

### Перевірити чи працює WebAPI
```bash
curl https://localhost:7001/api/telegram/health
```

### Перевірити чи працює через ngrok
```bash
curl https://1a2b-3c4d-5e6f.ngrok-free.app/api/telegram/health
```

### Видалити webhook
```bash
curl -X POST "https://api.telegram.org/bot8847702084:AAGt_fDEU3mbw16TQRVL1Qcwnb5KiPljpII/deleteWebhook"
```

### Переключитися назад на long polling (консольний бот)
```bash
# Видаліть webhook
curl -X POST "https://api.telegram.org/bot<TOKEN>/deleteWebhook"

# Запустіть консольний бот
cd 2PeopleTelegramBot
dotnet run
```

---

## Чек-лист перед тестуванням

- [ ] WebAPI запущено (`dotnet run` в терміналі 1)
- [ ] Ngrok запущено (`ngrok http https://localhost:7001` в терміналі 2)
- [ ] Ngrok URL скопійовано
- [ ] `appsettings.json` оновлено з ngrok URL
- [ ] WebAPI перезапущено
- [ ] Лог показує "Webhook встановлено"
- [ ] Написали боту `/start` в Telegram
- [ ] Бачите лог "Received update" в консолі

✅ **Якщо всі кроки виконано - бот має працювати!**

---

## Продакшн деплой

Коли готові до продакшну:

1. **Змініть в appsettings.json:**
```json
"WebhookUrl": "https://yourdomain.com/api/telegram/webhook"
```

2. **Опублікуйте на Azure/іншому хостингу**

3. **Ngrok більше не потрібен!**

Детальніше: [WEBHOOK_ARCHITECTURE.md](WEBHOOK_ARCHITECTURE.md)
