# 🔧 ИСПРАВЛЕНИЕ ошибки внешнего ключа (FOREIGN KEY)

## ❌ **Проблема:**
```
SqlException: The INSERT statement conflicted with the FOREIGN KEY constraint "FK_dbo.Documents_dbo.Projects_ProjectId". 
The conflict occurred in database "PlagiatAssistant", table "dbo.Projects", column 'Id'.
```

**Причина:** Документ пытается ссылаться на проект, который не существует в базе данных.

## ✅ **ИСПРАВЛЕНИЯ:**

### 1. **Исправлен метод `LoadTestDocument`**
```csharp
// БЫЛО: Проект сохранялся, но ID не получался
await _dataService.SaveProjectAsync(_currentProject);

// СТАЛО: Получаем проект с ID после сохранения
_currentProject = await _dataService.SaveProjectAsync(_currentProject);
```

### 2. **Исправлен метод `ImportDocument`**
```csharp
// ДОБАВЛЕНО: Проверка и сохранение проекта перед сохранением документа
if (_currentProject.Id == 0)
{
    _currentProject = await _dataService.SaveProjectAsync(_currentProject);
}

document.ProjectId = _currentProject.Id;
document = await _dataService.SaveDocumentAsync(document);
```

### 3. **Исправлено создание тестового документа**
```csharp
// ДОБАВЛЕНО: Проверка ID проекта перед созданием документа
if (_currentProject.Id == 0)
{
    _currentProject = await _dataService.SaveProjectAsync(_currentProject);
}

testDocument.ProjectId = _currentProject.Id;
testDocument = await _dataService.SaveDocumentAsync(testDocument);
```

### 4. **Улучшен `DataService.SaveDocumentAsync`**
```csharp
// ДОБАВЛЕНО: Проверка существования проекта
if (document.ProjectId.HasValue && document.ProjectId.Value > 0)
{
    var projectExists = await _context.Projects.AnyAsync(p => p.Id == document.ProjectId.Value);
    if (!projectExists)
    {
        // Автоматически создаем проект, если он не существует
        var newProject = new Project
        {
            Name = "Автоматически созданный проект",
            CreatedAt = DateTime.Now,
            Status = ProjectStatus.Active
        };
        _context.Projects.Add(newProject);
        await _context.SaveChangesAsync();
        
        document.ProjectId = newProject.Id;
    }
}
```

## 🎯 **РЕЗУЛЬТАТ:**

### ✅ **Теперь приложение:**
1. **Всегда сохраняет проект** перед созданием документа
2. **Получает правильный ID** проекта после сохранения
3. **Проверяет существование проекта** перед сохранением документа
4. **Автоматически создает проект** если он не существует
5. **Предотвращает ошибки внешнего ключа**

## 🧪 **ТЕСТИРОВАНИЕ:**

### **Шаг 1: Запустите приложение**
- Тестовый проект должен создаться автоматически
- Тестовый документ должен сохраниться без ошибок

### **Шаг 2: Попробуйте импорт**
- Импортируйте `simple_test.txt`
- Документ должен сохраниться в существующий проект

### **Шаг 3: Проверьте базу данных**
- Проект должен существовать с правильным ID
- Документ должен ссылаться на существующий проект

## 🔍 **ОТЛАДКА:**

### **Если ошибка всё ещё возникает:**
1. **Проверьте логи** - должны показать создание проекта
2. **Проверьте базу данных** - проект должен существовать
3. **Перезапустите приложение** - база данных пересоздастся

### **Для проверки базы данных:**
```sql
-- Проверьте существование проектов
SELECT * FROM Projects;

-- Проверьте документы и их ProjectId
SELECT Id, Title, ProjectId FROM Documents;
```

## 🚀 **ПРИЛОЖЕНИЕ ГОТОВО!**

Ошибка внешнего ключа больше не должна возникать. Документы будут правильно связываться с проектами.

