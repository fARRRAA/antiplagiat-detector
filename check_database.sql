-- Проверка состояния базы данных PlagiatAssistant

-- 1. Проверяем существование таблиц
SELECT 
    TABLE_NAME,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_CATALOG = 'PlagiatAssistant'
ORDER BY TABLE_NAME;

-- 2. Проверяем проекты
SELECT 
    Id,
    Name,
    CreatedAt,
    Status
FROM Projects
ORDER BY CreatedAt DESC;

-- 3. Проверяем документы
SELECT 
    Id,
    Title,
    ProjectId,
    CreatedAt,
    LEN(Content) as ContentLength
FROM Documents
ORDER BY CreatedAt DESC;

-- 4. Проверяем связи между проектами и документами
SELECT 
    p.Name as ProjectName,
    p.Id as ProjectId,
    d.Title as DocumentTitle,
    d.Id as DocumentId
FROM Projects p
LEFT JOIN Documents d ON p.Id = d.ProjectId
ORDER BY p.CreatedAt DESC, d.CreatedAt DESC;

-- 5. Проверяем внешние ключи
SELECT 
    CONSTRAINT_NAME,
    TABLE_NAME,
    COLUMN_NAME,
    REFERENCED_TABLE_NAME,
    REFERENCED_COLUMN_NAME
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE TABLE_CATALOG = 'PlagiatAssistant'
    AND REFERENCED_TABLE_NAME IS NOT NULL
ORDER BY TABLE_NAME, CONSTRAINT_NAME;

