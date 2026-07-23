# SQL Запити для аналізу історії повідомлень

## Загальна статистика

### Загальна кількість повідомлень
```sql
SELECT COUNT(*) AS TotalMessages
FROM MessageHistories;
```

### Кількість повідомлень по типах
```sql
SELECT 
    MessageType,
    COUNT(*) AS Count,
    CAST(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM MessageHistories) AS DECIMAL(5,2)) AS Percentage
FROM MessageHistories
GROUP BY MessageType
ORDER BY Count DESC;
```

### Найактивніші користувачі (по відправленим повідомленням)
```sql
SELECT TOP 10
    ru.ChatId,
    ru.Username,
    COUNT(*) AS MessagesSent
FROM MessageHistories mh
INNER JOIN RegisteredUsers ru ON mh.FromChatId = ru.ChatId
GROUP BY ru.ChatId, ru.Username
ORDER BY MessagesSent DESC;
```

## Статистика по користувачам

### Вся історія конкретного користувача
```sql
SELECT 
    mh.Id,
    mh.FromChatId,
    mh.ToChatId,
    mh.MessageType,
    mh.TextContent,
    mh.SentAt,
    CASE 
        WHEN mh.FromChatId = 123456789 THEN 'Sent'
        ELSE 'Received'
    END AS Direction
FROM MessageHistories mh
WHERE mh.FromChatId = 123456789 OR mh.ToChatId = 123456789
ORDER BY mh.SentAt DESC;
```

### Статистика користувача по типах повідомлень
```sql
SELECT 
    MessageType,
    COUNT(*) AS Count
FROM MessageHistories
WHERE FromChatId = 123456789
GROUP BY MessageType
ORDER BY Count DESC;
```

### Топ співрозмовників користувача
```sql
SELECT TOP 5
    CASE 
        WHEN FromChatId = 123456789 THEN ToChatId
        ELSE FromChatId
    END AS PartnerId,
    ru.Username,
    COUNT(*) AS MessagesExchanged
FROM MessageHistories mh
LEFT JOIN RegisteredUsers ru ON 
    ru.ChatId = CASE 
        WHEN FromChatId = 123456789 THEN ToChatId
        ELSE FromChatId
    END
WHERE FromChatId = 123456789 OR ToChatId = 123456789
GROUP BY 
    CASE 
        WHEN FromChatId = 123456789 THEN ToChatId
        ELSE FromChatId
    END,
    ru.Username
ORDER BY MessagesExchanged DESC;
```

## Аналіз за часом

### Повідомлення за сьогодні
```sql
SELECT COUNT(*) AS MessagesToday
FROM MessageHistories
WHERE CAST(SentAt AS DATE) = CAST(GETUTCDATE() AS DATE);
```

### Повідомлення за останні 7 днів
```sql
SELECT 
    CAST(SentAt AS DATE) AS Date,
    COUNT(*) AS MessageCount
FROM MessageHistories
WHERE SentAt >= DATEADD(DAY, -7, GETUTCDATE())
GROUP BY CAST(SentAt AS DATE)
ORDER BY Date DESC;
```

### Повідомлення за останній місяць (по днях)
```sql
SELECT 
    CAST(SentAt AS DATE) AS Date,
    MessageType,
    COUNT(*) AS Count
FROM MessageHistories
WHERE SentAt >= DATEADD(MONTH, -1, GETUTCDATE())
GROUP BY CAST(SentAt AS DATE), MessageType
ORDER BY Date DESC, Count DESC;
```

### Активність по годинах (UTC)
```sql
SELECT 
    DATEPART(HOUR, SentAt) AS Hour,
    COUNT(*) AS MessageCount
FROM MessageHistories
GROUP BY DATEPART(HOUR, SentAt)
ORDER BY Hour;
```

## Історія між двома користувачами

### Всі повідомлення між двома користувачами
```sql
SELECT 
    mh.Id,
    mh.FromChatId,
    ruFrom.Username AS FromUsername,
    mh.ToChatId,
    ruTo.Username AS ToUsername,
    mh.MessageType,
    mh.TextContent,
    mh.SentAt
FROM MessageHistories mh
LEFT JOIN RegisteredUsers ruFrom ON mh.FromChatId = ruFrom.ChatId
LEFT JOIN RegisteredUsers ruTo ON mh.ToChatId = ruTo.ChatId
WHERE 
    (mh.FromChatId = 123456789 AND mh.ToChatId = 987654321)
    OR
    (mh.FromChatId = 987654321 AND mh.ToChatId = 123456789)
ORDER BY mh.SentAt DESC;
```

### Статистика спілкування між двома користувачами
```sql
SELECT 
    FromChatId,
    ru.Username,
    MessageType,
    COUNT(*) AS Count
FROM MessageHistories mh
LEFT JOIN RegisteredUsers ru ON mh.FromChatId = ru.ChatId
WHERE 
    (FromChatId = 123456789 AND ToChatId = 987654321)
    OR
    (FromChatId = 987654321 AND ToChatId = 123456789)
GROUP BY FromChatId, ru.Username, MessageType
ORDER BY FromChatId, Count DESC;
```

## Пошук і фільтрація

### Пошук по тексту
```sql
SELECT 
    mh.Id,
    mh.FromChatId,
    ru.Username,
    mh.MessageType,
    mh.TextContent,
    mh.SentAt
FROM MessageHistories mh
LEFT JOIN RegisteredUsers ru ON mh.FromChatId = ru.ChatId
WHERE mh.TextContent LIKE '%ключове слово%'
ORDER BY mh.SentAt DESC;
```

### Тільки медіа-повідомлення
```sql
SELECT 
    mh.Id,
    mh.FromChatId,
    ru.Username,
    mh.MessageType,
    mh.FileId,
    mh.SentAt
FROM MessageHistories mh
LEFT JOIN RegisteredUsers ru ON mh.FromChatId = ru.ChatId
WHERE mh.MessageType IN ('Photo', 'Video', 'Voice', 'Audio', 'Document', 'VideoNote')
ORDER BY mh.SentAt DESC;
```

### Повідомлення з файлами за останній тиждень
```sql
SELECT 
    MessageType,
    COUNT(*) AS Count
FROM MessageHistories
WHERE 
    FileId IS NOT NULL
    AND SentAt >= DATEADD(DAY, -7, GETUTCDATE())
GROUP BY MessageType
ORDER BY Count DESC;
```

## Очищення та обслуговування

### Видалити повідомлення старше 90 днів
```sql
DELETE FROM MessageHistories
WHERE SentAt < DATEADD(DAY, -90, GETUTCDATE());
```

### Видалити всю історію користувача
```sql
DELETE FROM MessageHistories
WHERE FromChatId = 123456789 OR ToChatId = 123456789;
```

### Видалити історію між двома користувачами
```sql
DELETE FROM MessageHistories
WHERE 
    (FromChatId = 123456789 AND ToChatId = 987654321)
    OR
    (FromChatId = 987654321 AND ToChatId = 123456789);
```

## Експорт даних

### Експорт в CSV формат (приклад для SQL Server)
```sql
-- Створення CSV з історією користувача
SELECT 
    mh.SentAt AS 'Дата/Час',
    CASE 
        WHEN mh.FromChatId = 123456789 THEN 'Відправлено'
        ELSE 'Отримано'
    END AS 'Напрямок',
    mh.MessageType AS 'Тип',
    ISNULL(mh.TextContent, '') AS 'Текст'
FROM MessageHistories mh
WHERE mh.FromChatId = 123456789 OR mh.ToChatId = 123456789
ORDER BY mh.SentAt;
```

## Performance

### Перевірка індексів
```sql
SELECT 
    i.name AS IndexName,
    s.name AS TableName,
    i.type_desc AS IndexType
FROM sys.indexes i
INNER JOIN sys.tables s ON i.object_id = s.object_id
WHERE s.name = 'MessageHistories';
```

### Статистика використання індексів
```sql
SELECT 
    OBJECT_NAME(s.object_id) AS TableName,
    i.name AS IndexName,
    s.user_seeks,
    s.user_scans,
    s.user_lookups,
    s.user_updates
FROM sys.dm_db_index_usage_stats s
INNER JOIN sys.indexes i ON s.object_id = i.object_id AND s.index_id = i.index_id
WHERE OBJECT_NAME(s.object_id) = 'MessageHistories';
```
