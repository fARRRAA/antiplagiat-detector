@echo off
echo Удаление базы данных PlagiatAssistant...

REM Удаляем базу данных через SQL Server LocalDB
sqllocaldb stop MSSQLLocalDB
sqllocaldb delete MSSQLLocalDB
sqllocaldb create MSSQLLocalDB

echo База данных пересоздана!
pause

