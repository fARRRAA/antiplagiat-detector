# 🔍 ОТЛАДКА потери контента документа

## ❌ **ПРОБЛЕМА:**
Документ загружается с контентом, но через несколько шагов контент исчезает, показывая 0 слов и 0 символов.

## 🔧 **ДОБАВЛЕНА ОТЛАДКА:**

### **1. В методе `LoadDocument`:**
```csharp
Console.WriteLine($"LoadDocument вызван с документом: {document.Title}");
Console.WriteLine($"LoadDocument - контент документа: '{document.Content}' (длина: {document.Content?.Length ?? 0})");
Console.WriteLine($"LoadDocument - контент пустой: {string.IsNullOrWhiteSpace(document.Content)}");

_currentDocument = document;

Console.WriteLine($"LoadDocument - _currentDocument установлен: {_currentDocument.Title}");
Console.WriteLine($"LoadDocument - _currentDocument.Content: '{_currentDocument.Content}' (длина: {_currentDocument.Content?.Length ?? 0})");
```

### **2. В методе `UpdateWordCount`:**
```csharp
Console.WriteLine($"UpdateWordCount вызван. _currentDocument: {_currentDocument != null}");

if (_currentDocument != null)
{
    Console.WriteLine($"UpdateWordCount - _currentDocument.Title: {_currentDocument.Title}");
    Console.WriteLine($"UpdateWordCount - _currentDocument.Id: {_currentDocument.Id}");
    // Всегда считаем слова и символы, даже если контент пустой
    var content = _currentDocument.Content ?? "";
    Console.WriteLine($"UpdateWordCount - контент документа: '{content}' (длина: {content.Length})");
    Console.WriteLine($"UpdateWordCount - контент пустой: {string.IsNullOrWhiteSpace(content)}");
```

### **3. В методе `ProjectsTreeView_SelectedItemChanged`:**
```csharp
Console.WriteLine($"Перед LoadDocument: document.Content.Length = {document.Content?.Length ?? 0}");
LoadDocument(document);
Console.WriteLine($"После LoadDocument: _currentDocument.Content.Length = {_currentDocument?.Content?.Length ?? 0}");
```

### **4. В конце `LoadDocument`:**
```csharp
Console.WriteLine($"После установки контента: _currentDocument.Content.Length = {_currentDocument.Content?.Length ?? 0}");
Console.WriteLine($"Перед вызовом UpdateWordCount: _currentDocument.Content.Length = {_currentDocument.Content?.Length ?? 0}");
UpdateWordCount();
Console.WriteLine($"После вызова UpdateWordCount: _currentDocument.Content.Length = {_currentDocument.Content?.Length ?? 0}");
```

## 🧪 **ИНСТРУКЦИИ ПО ТЕСТИРОВАНИЮ:**

### **1. Запустите приложение с точками останова:**
- Поставьте точку останова в начале `LoadDocument`
- Поставьте точку останова в `UpdateWordCount`
- Поставьте точку останова в `ProjectsTreeView_SelectedItemChanged`

### **2. Загрузите документ и следите за консолью:**

**Ожидаемый вывод:**
```
Выбран документ: 02_high_plagiarism.txt
ID документа: 1
Контент документа (до загрузки): 2228 символов
Перед LoadDocument: document.Content.Length = 2228
LoadDocument вызван с документом: 02_high_plagiarism.txt
LoadDocument - контент документа: 'Искусственный интеллект является одной из наиболее...' (длина: 2228)
LoadDocument - контент пустой: False
LoadDocument - _currentDocument установлен: 02_high_plagiarism.txt
LoadDocument - _currentDocument.Content: 'Искусственный интеллект является одной из наиболее...' (длина: 2228)
Загружаем в RichTextBox: 2228 символов
Текст успешно загружен в RichTextBox: 2228 символов
После установки контента: _currentDocument.Content.Length = 2228
Перед вызовом UpdateWordCount: _currentDocument.Content.Length = 2228
UpdateWordCount вызван. _currentDocument: True
UpdateWordCount - _currentDocument.Title: 02_high_plagiarism.txt
UpdateWordCount - _currentDocument.Id: 1
UpdateWordCount - контент документа: 'Искусственный интеллект является одной из наиболее...' (длина: 2228)
UpdateWordCount - контент пустой: False
UpdateWordCount - подсчитано: 350 слов, 2228 символов
После вызова UpdateWordCount: _currentDocument.Content.Length = 2228
После LoadDocument: _currentDocument.Content.Length = 2228
```

### **3. Если контент исчезает, найдите где:**

**Возможные места потери контента:**
1. **В `ProjectsTreeView_SelectedItemChanged`** - если `document` перезаписывается
2. **В `LoadDocument`** - если `_currentDocument` перезаписывается
3. **В `UpdateWordCount`** - если `_currentDocument.Content` становится пустым
4. **В других методах** - если `_currentDocument` устанавливается в `null`

## 🎯 **ЧТО ИСКАТЬ:**

### **Если контент исчезает:**
1. **Найдите строку, где длина контента становится 0**
2. **Проверьте, не вызывается ли `_currentDocument = null`**
3. **Проверьте, не перезаписывается ли `_currentDocument.Content`**
4. **Проверьте, не вызывается ли `_currentDocument = new Document()`**

### **Возможные причины:**
1. **Entity Framework** загружает документ без контента
2. **Асинхронные операции** приводят к race condition
3. **События UI** перезаписывают `_currentDocument`
4. **Другие методы** очищают контент

## 🚀 **СЛЕДУЮЩИЕ ШАГИ:**

1. **Запустите приложение** с отладкой
2. **Загрузите документ** и следите за консолью
3. **Найдите точное место** где контент исчезает
4. **Сообщите результат** - какие строки показывают потерю контента

**Теперь у нас есть полная отладка для отслеживания потери контента!** 🔍
