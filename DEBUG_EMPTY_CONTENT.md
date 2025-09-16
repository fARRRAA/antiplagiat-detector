# 🔍 ОТЛАДКА проблемы с пустым содержимым документов

## ❌ **ПРОБЛЕМА:**
Документ "02_high_plagiarism" отображает заглушку "[Документ пуст или содержимое не загружено]" вместо реального содержимого.

## ✅ **ДОБАВЛЕНА ПОЛНАЯ ОТЛАДОЧНАЯ ИНФОРМАЦИЯ:**

### 1. **В `ProjectsTreeView_SelectedItemChanged`:**
```csharp
Console.WriteLine($"Выбран документ: {document.Title}");
Console.WriteLine($"ID документа: {document.Id}");
Console.WriteLine($"Контент документа (до загрузки): {document.Content?.Length ?? 0} символов");

if (string.IsNullOrEmpty(document.Content))
{
    Console.WriteLine("Контент пустой, загружаем из базы данных...");
    var fullDocument = await _dataService.GetDocumentByIdAsync(document.Id);
    if (fullDocument != null)
    {
        Console.WriteLine($"Загружен из БД: {fullDocument.Content?.Length ?? 0} символов");
        document = fullDocument;
    }
    else
    {
        Console.WriteLine("Документ не найден в базе данных!");
    }
}
```

### 2. **В `ImportDocument`:**
```csharp
Console.WriteLine($"Начинаем импорт файла: {filePath}");
var document = await _documentService.ImportDocumentAsync(filePath);
Console.WriteLine($"Документ импортирован: {document.Title}, контент: {document.Content?.Length ?? 0} символов");

if (string.IsNullOrEmpty(document.Content))
{
    Console.WriteLine("ОШИБКА: Контент документа пустой после импорта!");
}

Console.WriteLine($"Сохраняем документ в БД с ProjectId: {document.ProjectId}");
document = await _dataService.SaveDocumentAsync(document);
Console.WriteLine($"Документ сохранен в БД с ID: {document.Id}, контент: {document.Content?.Length ?? 0} символов");
```

### 3. **В `DataService.SaveDocumentAsync`:**
```csharp
await _context.SaveChangesAsync();
Console.WriteLine($"Документ сохранен в БД: ID={document.Id}, Title='{document.Title}', Content={document.Content?.Length ?? 0} символов");
```

### 4. **В `DataService.GetDocumentByIdAsync`:**
```csharp
var document = await _context.Documents
    .Include(d => d.PlagiarismResults)
    .Include(d => d.Citations.Select(c => c.Source))
    .FirstOrDefaultAsync(d => d.Id == documentId);

Console.WriteLine($"Загружен документ из БД: ID={documentId}, Title='{document?.Title}', Content={document?.Content?.Length ?? 0} символов");
```

## 🧪 **ТЕСТИРОВАНИЕ:**

### **Шаг 1: Запустите приложение**
- Откройте консоль (если есть) или проверьте логи
- Должны появиться сообщения о создании тестового документа

### **Шаг 2: Попробуйте импорт документа "02_high_plagiarism.txt"**
- Должны появиться сообщения о импорте файла
- Должно показать количество символов в контенте
- Должно показать сохранение в базу данных

### **Шаг 3: Выберите документ в дереве проектов**
- Должны появиться сообщения о загрузке документа
- Должно показать, загружается ли контент из базы данных

## 🔍 **ДИАГНОСТИКА:**

### **Если консоль показывает:**
- `"Документ импортирован: 02_high_plagiarism, контент: 0 символов"`
- `"ОШИБКА: Контент документа пустой после импорта!"`

**То проблема в `DocumentService.ImportDocumentAsync`**

### **Если консоль показывает:**
- `"Документ сохранен в БД с ID: X, контент: 0 символов"`

**То проблема в сохранении в базу данных**

### **Если консоль показывает:**
- `"Загружен документ из БД: ID=X, Content=0 символов"`

**То проблема в загрузке из базы данных**

### **Если консоль показывает:**
- `"Контент пустой, загружаем из базы данных..."`
- `"Документ не найден в базе данных!"`

**То документ не сохранился в базе данных**

## 🚀 **ОЖИДАЕМЫЙ РЕЗУЛЬТАТ:**

После запуска приложения и импорта документа консоль должна показать:
1. `"Документ импортирован: 02_high_plagiarism, контент: XXXX символов"`
2. `"Документ сохранен в БД с ID: X, контент: XXXX символов"`
3. `"Загружен документ из БД: ID=X, Content=XXXX символов"`
4. В редакторе должен появиться реальный текст документа

## 📝 **СЛЕДУЮЩИЕ ШАГИ:**

1. **Запустите приложение** и проверьте консольные сообщения
2. **Сообщите**, какие сообщения появляются в консоли
3. **Проверьте**, на каком этапе теряется содержимое документа
4. **Мы сможем точно определить** причину проблемы

