# 🗄️ Настройка базы данных для Антиплагиат-Помощника

## 📋 Обзор

Приложение использует **SQL Server LocalDB** с **Entity Framework 6** для хранения данных. База данных создается автоматически при первом запуске.

## 🏗️ Структура базы данных

### Таблицы:
- **Projects** - Проекты пользователя
- **Documents** - Документы в проектах
- **Sources** - Источники литературы
- **Citations** - Цитаты в документах
- **PlagiarismResults** - Результаты проверки на плагиат
- **ProjectFavoritesSources** - Избранные источники (связующая таблица)

## 🚀 Способы настройки

### ✅ **Способ 1: Автоматическое создание (Рекомендуется)**

**Самый простой способ - ничего не делать!** 

1. **Запустите приложение**
2. **База данных создастся автоматически** при первом запуске
3. **Демо-данные** будут добавлены автоматически

**Что происходит автоматически:**
- Создается база данных `PlagiatAssistant` в LocalDB
- Создаются все необходимые таблицы
- Настраиваются связи между таблицами
- Добавляются демо-проекты и источники

---

### 🛠️ **Способ 2: Через Package Manager Console**

Если нужен больший контроль:

1. **Откройте Visual Studio**
2. **Откройте Package Manager Console:** `Tools` → `NuGet Package Manager` → `Package Manager Console`
3. **Выполните команды:**

```powershell
# Включить миграции
Enable-Migrations

# Создать первую миграцию
Add-Migration InitialCreate

# Применить миграцию к базе данных
Update-Database

# Проверить статус миграций
Get-Migrations
```

---

### 💻 **Способ 3: Ручное создание через SQL**

Для продвинутых пользователей:

1. **Откройте SQL Server Management Studio (SSMS)**
2. **Подключитесь к LocalDB:**
   - Server name: `(LocalDB)\MSSQLLocalDB`
   - Authentication: Windows Authentication
3. **Выполните скрипт** `database_schema.sql` (создан автоматически)

---

## 🔧 Проверка настройки

### Проверка через приложение:
1. Запустите приложение
2. Посмотрите в **Output Window** Visual Studio (`View` → `Output`)
3. Найдите сообщения о статусе базы данных

### Проверка через SSMS:
1. Подключитесь к `(LocalDB)\MSSQLLocalDB`
2. Найдите базу данных `PlagiatAssistant`
3. Проверьте наличие таблиц

### Проверка через код:
```csharp
// В любом месте приложения
var dbInfo = await DatabaseInitializer.GetDatabaseInfoAsync();
Console.WriteLine(dbInfo);
```

---

## ⚙️ Настройки подключения

### Текущая строка подключения:
```xml
<connectionStrings>
  <add name="DefaultConnection" 
       connectionString="data source=(LocalDb)\MSSQLLocalDB;initial catalog=PlagiatAssistant;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework" 
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

### Изменение строки подключения:
1. Откройте `App.config`
2. Измените параметр `connectionString`
3. Перезапустите приложение


---

## 🔍 Устранение неполадок

### ❌ **Проблема:** База данных не создается
**Решение:**
1. Проверьте установку SQL Server LocalDB
2. Запустите Visual Studio от имени администратора
3. Проверьте строку подключения в `App.config`

### ❌ **Проблема:** Ошибка подключения
**Решение:**
1. Убедитесь что LocalDB запущен: `sqllocaldb info`
2. Создайте экземпляр вручную: `sqllocaldb create MSSQLLocalDB`
3. Запустите экземпляр: `sqllocaldb start MSSQLLocalDB`

### ❌ **Проблема:** Таблицы не создаются
**Решение:**
1. Удалите базу данных: `sqllocaldb delete PlagiatAssistant`
2. Перезапустите приложение
3. Или используйте миграции через Package Manager Console

### ❌ **Проблема:** Старая схема базы данных
**Решение:**
```powershell
# В Package Manager Console
Add-Migration UpdateSchema
Update-Database
```

---

## 📊 Демо-данные

При первом запуске создаются:

### **Демо-проект:**
- Название: "Демо проект"
- Описание: "Проект для демонстрации возможностей приложения"

### **Демо-источники:**
1. **"Основы информационных технологий"** - Иванов И.И. (2023)
2. **"Современные методы анализа текста"** - Петров П.П. (2022)  
3. **"Искусственный интеллект в образовании"** - Сидоров С.С. (2024)

---

## 🔒 Безопасность

- **Локальное хранение:** Все данные хранятся локально в LocalDB
- **Нет внешних подключений:** База данных не доступна извне
- **Автоматическое резервное копирование:** LocalDB создает автоматические резервные копии
- **Шифрование:** Используется Windows Authentication

---

