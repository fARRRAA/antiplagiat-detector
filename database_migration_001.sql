-- Миграция 001: Добавление связи между Project и Document
-- Дата создания: 2025-09-15

-- Проверяем, существует ли столбец ProjectId в таблице Documents
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Documents' AND COLUMN_NAME = 'ProjectId')
BEGIN
    -- Добавляем столбец ProjectId
    ALTER TABLE Documents
    ADD ProjectId int NULL;
    
    -- Создаем внешний ключ
    ALTER TABLE Documents
    ADD CONSTRAINT FK_Documents_Projects_ProjectId
    FOREIGN KEY (ProjectId) REFERENCES Projects(Id);
    
    -- Создаем индекс для производительности
    CREATE INDEX IX_Documents_ProjectId ON Documents(ProjectId);
    
    PRINT 'Столбец ProjectId добавлен в таблицу Documents';
END
ELSE
BEGIN
    PRINT 'Столбец ProjectId уже существует в таблице Documents';
END

-- Проверяем целостность данных
SELECT COUNT(*) as TotalDocuments FROM Documents;
SELECT COUNT(*) as TotalProjects FROM Projects;

PRINT 'Миграция 001 завершена успешно';

