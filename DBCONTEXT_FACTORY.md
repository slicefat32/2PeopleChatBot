# TelegramBotDbContextFactory - Пояснення

## Навіщо потрібен TelegramBotDbContextFactory?

### Проблема

Коли ви виконуєте команди EF Core (міграції), такі як:
```bash
dotnet ef migrations add MyMigration
dotnet ef database update
```

**Програма не запущена!** EF Core tools працюють в **design-time** режимі.

### Рішення

`IDesignTimeDbContextFactory` - це фабрика, яка каже EF Core, як створити `DbContext` **без запуску програми**.

---

## Як це працює?

### До змін (погана практика ❌)

```csharp
public class TelegramBotDbContextFactory : IDesignTimeDbContextFactory<TelegramBotDbContext>
{
    public TelegramBotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TelegramBotDbContext>();
        
        // ❌ ПОГАНО: Захардкоджений connection string
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=TelegramBotDb;Trusted_Connection=True;"
        );

        return new TelegramBotDbContext(optionsBuilder.Options);
    }
}
```

**Проблеми:**
- ❌ Дублювання налаштувань (в `appsettings.json` та тут)
- ❌ Якщо змінити БД в `appsettings.json`, треба міняти ще й тут
- ❌ Важко підтримувати

---

### Після змін (правильно ✅)

```csharp
public class TelegramBotDbContextFactory : IDesignTimeDbContextFactory<TelegramBotDbContext>
{
    public TelegramBotDbContext CreateDbContext(string[] args)
    {
        // ✅ ДОБРЕ: Читаємо appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "2PeopleTelegramBot"))
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<TelegramBotDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        optionsBuilder.UseSqlServer(connectionString);

        return new TelegramBotDbContext(optionsBuilder.Options);
    }
}
```

**Переваги:**
- ✅ **Єдине джерело правди** - тільки `appsettings.json`
- ✅ Змінюєте connection string в одному місці
- ✅ Легко підтримувати

---

## Як це працює?

### Крок 1: Виконання команди міграції

```bash
cd 2PeopleTelegramBot
dotnet ef migrations add AddMessageHistory --project ..\2PeopleTB.DAL\2PeopleTB.DAL.csproj
```

### Крок 2: EF Core tools шукає фабрику

EF Core tools шукають клас, який реалізує `IDesignTimeDbContextFactory<TelegramBotDbContext>`.

### Крок 3: Виклик CreateDbContext

```csharp
// EF Core викликає:
var factory = new TelegramBotDbContextFactory();
var dbContext = factory.CreateDbContext(args);
```

### Крок 4: Фабрика читає appsettings.json

```csharp
// Поточна директорія: D:\Projects\...\2PeopleTB.DAL
var currentDir = Directory.GetCurrentDirectory(); 

// Переходимо до 2PeopleTelegramBot
var startupProjectDir = Path.Combine(currentDir, "..", "2PeopleTelegramBot");

// Читаємо appsettings.json з 2PeopleTelegramBot
var configuration = new ConfigurationBuilder()
    .SetBasePath(startupProjectDir)
    .AddJsonFile("appsettings.json")
    .Build();
```

### Крок 5: Створення DbContext

```csharp
var connectionString = configuration.GetConnectionString("DefaultConnection");
// "Server=(localdb)\\mssqllocaldb;Database=TelegramBotDb;..."

var options = new DbContextOptionsBuilder<TelegramBotDbContext>()
    .UseSqlServer(connectionString)
    .Options;

return new TelegramBotDbContext(options);
```

---

## Структура папок

```
D:\Projects\example\2PeopleTelegramBot\
├── 2PeopleTelegramBot\              ← Startup project
│   ├── appsettings.json             ← Connection string тут!
│   └── Program.cs
│
└── 2PeopleTB.DAL\                   ← Class library
    ├── Data\
    │   ├── TelegramBotDbContext.cs
    │   └── TelegramBotDbContextFactory.cs  ← Читає appsettings.json з startup проєкту
    └── Migrations\
```

---

## Коли використовується Factory vs Program.cs?

| Сценарій | Що використовується |
|----------|---------------------|
| `dotnet run` | `Program.cs` створює DbContext |
| `dotnet ef migrations add` | `TelegramBotDbContextFactory` створює DbContext |
| `dotnet ef database update` | `TelegramBotDbContextFactory` створює DbContext |

---

## Переваги централізованої конфігурації

### До (2 місця для зміни):

1. **appsettings.json**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;..."
  }
}
```

2. **TelegramBotDbContextFactory.cs**
```csharp
optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;...");
```

❌ Треба змінювати в 2 місцях!

---

### Після (1 місце для зміни):

**appsettings.json**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;..."
  }
}
```

✅ Змінюєте тільки тут!

---

## Можливі проблеми

### Помилка: "Could not find appsettings.json"

**Причина:** Неправильний шлях до startup проєкту.

**Рішення:** Перевірте шлях:
```csharp
var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "2PeopleTelegramBot");
Console.WriteLine($"Looking for appsettings.json in: {basePath}");
```

### Помилка: "ConnectionStrings:DefaultConnection not found"

**Причина:** Неправильна структура `appsettings.json`.

**Рішення:** Перевірте формат:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."  ← Має бути саме DefaultConnection
  }
}
```

---

## Альтернативні підходи

### Варіант 1: Змінні середовища

```csharp
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? configuration.GetConnectionString("DefaultConnection");
```

### Варіант 2: Параметри командного рядка

```bash
dotnet ef migrations add MyMigration -- --connection "Server=...;Database=...;"
```

```csharp
public TelegramBotDbContext CreateDbContext(string[] args)
{
    string? connectionString = null;
    
    // Перевіряємо аргументи
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--connection" && i + 1 < args.Length)
        {
            connectionString = args[i + 1];
            break;
        }
    }
    
    // Якщо не передано, читаємо з appsettings.json
    if (string.IsNullOrEmpty(connectionString))
    {
        var configuration = new ConfigurationBuilder()...
        connectionString = configuration.GetConnectionString("DefaultConnection");
    }
    
    var optionsBuilder = new DbContextOptionsBuilder<TelegramBotDbContext>();
    optionsBuilder.UseSqlServer(connectionString);
    return new TelegramBotDbContext(optionsBuilder.Options);
}
```

---

## Підсумок

**Раніше:**
- ❌ Connection string захардкоджений в `TelegramBotDbContextFactory`
- ❌ Треба міняти в 2 місцях

**Тепер:**
- ✅ Connection string тільки в `appsettings.json`
- ✅ Фабрика читає його автоматично
- ✅ Змінюєте в одному місці - працює скрізь

**Це називається "Single Source of Truth" (Єдине джерело правди)** ✨
