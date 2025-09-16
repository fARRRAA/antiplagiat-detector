-- Скрипт для удаления базы данных PlagiatAssistant
-- Выполните этот скрипт в SQL Server Management Studio или через sqlcmd

USE master;
GO

-- Закрываем все подключения к базе данных
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'PlagiatAssistant')
BEGIN
    ALTER DATABASE PlagiatAssistant SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE PlagiatAssistant;
    PRINT 'База данных PlagiatAssistant удалена успешно.';
END
ELSE
BEGIN
    PRINT 'База данных PlagiatAssistant не найдена.';
END
GO

PRINT 'Теперь можно запустить приложение - база данных будет создана заново.';

