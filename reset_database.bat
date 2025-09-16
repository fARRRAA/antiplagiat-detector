@echo off
echo Удаление базы данных PlagiatAssistant из LocalDB...

REM Остановка экземпляра LocalDB
sqllocaldb stop MSSQLLocalDB

REM Удаление базы данных
sqllocaldb delete-database MSSQLLocalDB PlagiatAssistant

REM Запуск экземпляра LocalDB
sqllocaldb start MSSQLLocalDB

echo База данных удалена. Теперь можно запустить приложение.
pause

