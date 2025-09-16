-- Скрипт создания базы данных для Антиплагиат-Помощника
-- Выполните этот скрипт в SQL Server Management Studio

USE master;
GO

-- Создание базы данных
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'PlagiatAssistant')
BEGIN
    CREATE DATABASE [PlagiatAssistant];
END
GO

USE [PlagiatAssistant];
GO

-- Создание таблицы Projects
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Projects' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[Projects] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL,
        [LastModified] DATETIME2(7) NULL,
        [Status] INT NOT NULL DEFAULT 0, -- ProjectStatus enum
        [DefaultCitationStyle] INT NOT NULL DEFAULT 0, -- CitationStyle enum
        [Settings] NVARCHAR(MAX) NULL, -- JSON настройки
        CONSTRAINT [PK_Projects] PRIMARY KEY ([Id])
    );
END
GO

-- Создание таблицы Documents
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Documents' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[Documents] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Title] NVARCHAR(500) NOT NULL,
        [Content] NVARCHAR(MAX) NOT NULL,
        [OriginalFileName] NVARCHAR(255) NULL,
        [FileFormat] NVARCHAR(10) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL,
        [LastModified] DATETIME2(7) NULL,
        [UniquenessPercentage] FLOAT NOT NULL DEFAULT 0,
        [Status] INT NOT NULL DEFAULT 0, -- DocumentStatus enum
        [Language] NVARCHAR(10) NULL,
        [ProjectId] INT NULL, -- Внешний ключ к Projects
        CONSTRAINT [PK_Documents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Documents_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE SET NULL
    );
    
    -- Индекс для быстрого поиска по дате создания
    CREATE INDEX [IX_Documents_CreatedAt] ON [Documents] ([CreatedAt]);
END
GO

-- Создание таблицы Sources
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Sources' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[Sources] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Title] NVARCHAR(1000) NOT NULL,
        [Author] NVARCHAR(500) NULL,
        [Publisher] NVARCHAR(500) NULL,
        [Year] INT NULL,
        [Url] NVARCHAR(2000) NULL,
        [ISBN] NVARCHAR(50) NULL,
        [DOI] NVARCHAR(200) NULL,
        [Type] INT NOT NULL DEFAULT 0, -- SourceType enum
        [Volume] NVARCHAR(50) NULL,
        [Issue] NVARCHAR(50) NULL,
        [Pages] NVARCHAR(100) NULL,
        [City] NVARCHAR(200) NULL,
        [AccessDate] DATETIME2(7) NULL,
        [IsComplete] BIT NOT NULL DEFAULT 0,
        CONSTRAINT [PK_Sources] PRIMARY KEY ([Id])
    );
END
GO

-- Создание таблицы Citations
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Citations' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[Citations] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [DocumentId] INT NOT NULL,
        [QuotedText] NVARCHAR(MAX) NOT NULL,
        [StartPosition] INT NOT NULL,
        [EndPosition] INT NOT NULL,
        [Type] INT NOT NULL DEFAULT 0, -- CitationType enum
        [SourceId] INT NOT NULL,
        [PageNumber] NVARCHAR(20) NULL,
        [IsFormatted] BIT NOT NULL DEFAULT 0,
        [Style] INT NOT NULL DEFAULT 0, -- CitationStyle enum
        CONSTRAINT [PK_Citations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Citations_Documents] FOREIGN KEY ([DocumentId]) REFERENCES [Documents]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Citations_Sources] FOREIGN KEY ([SourceId]) REFERENCES [Sources]([Id]) ON DELETE CASCADE
    );
END
GO

-- Создание таблицы PlagiarismResults
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PlagiarismResults' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[PlagiarismResults] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [DocumentId] INT NOT NULL,
        [MatchedText] NVARCHAR(MAX) NOT NULL,
        [StartPosition] INT NOT NULL,
        [EndPosition] INT NOT NULL,
        [SimilarityPercentage] FLOAT NOT NULL,
        [SourceUrl] NVARCHAR(2000) NULL,
        [SourceTitle] NVARCHAR(1000) NULL,
        [SourceAuthor] NVARCHAR(500) NULL,
        [SourceDate] DATETIME2(7) NULL,
        [Level] INT NOT NULL DEFAULT 0, -- PlagiarismLevel enum
        [IsExcluded] BIT NOT NULL DEFAULT 0,
        [ExclusionReason] NVARCHAR(500) NULL,
        [IsProcessed] BIT NOT NULL DEFAULT 0,
        CONSTRAINT [PK_PlagiarismResults] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PlagiarismResults_Documents] FOREIGN KEY ([DocumentId]) REFERENCES [Documents]([Id]) ON DELETE CASCADE
    );
    
    -- Индекс для быстрого поиска по проценту схожести
    CREATE INDEX [IX_PlagiarismResults_SimilarityPercentage] ON [PlagiarismResults] ([SimilarityPercentage]);
END
GO

-- Создание связующей таблицы для избранных источников проектов
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProjectFavoritesSources' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[ProjectFavoritesSources] (
        [ProjectId] INT NOT NULL,
        [SourceId] INT NOT NULL,
        CONSTRAINT [PK_ProjectFavoritesSources] PRIMARY KEY ([ProjectId], [SourceId]),
        CONSTRAINT [FK_ProjectFavoritesSources_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProjectFavoritesSources_Sources] FOREIGN KEY ([SourceId]) REFERENCES [Sources]([Id]) ON DELETE CASCADE
    );
END
GO

-- Вставка демо-данных для тестирования
INSERT INTO [Projects] ([Name], [Description], [CreatedAt], [Status], [DefaultCitationStyle])
VALUES 
    ('Демо проект', 'Проект для демонстрации возможностей приложения', GETDATE(), 0, 0),
    ('Научная работа', 'Исследование в области информационных технологий', GETDATE(), 0, 0);
GO

-- Вставка демо-источников
INSERT INTO [Sources] ([Title], [Author], [Publisher], [Year], [Type], [IsComplete])
VALUES 
    ('Основы информационных технологий', 'Иванов И.И.', 'Наука', 2023, 0, 1),
    ('Современные методы анализа текста', 'Петров П.П.', 'Техника', 2022, 0, 1),
    ('Искусственный интеллект в образовании', 'Сидоров С.С.', 'Образование', 2024, 1, 1);
GO

PRINT 'База данных PlagiatAssistant успешно создана!';
PRINT 'Созданы таблицы:';
PRINT '- Projects (Проекты)';
PRINT '- Documents (Документы)';
PRINT '- Sources (Источники)';
PRINT '- Citations (Цитаты)';
PRINT '- PlagiarismResults (Результаты проверки плагиата)';
PRINT '- ProjectFavoritesSources (Избранные источники проектов)';
GO

