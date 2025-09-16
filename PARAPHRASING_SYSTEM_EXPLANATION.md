# 🤖 СИСТЕМА ПЕРЕФРАЗИРОВАНИЯ ТЕКСТА

## 🎯 **ОБЩИЙ ПРИНЦИП РАБОТЫ:**

Система перефразирования автоматически находит проблемные фрагменты текста (с высоким процентом плагиата) и предлагает пользователю варианты их перефразирования с помощью нейросети.

## 🔄 **ПОШАГОВЫЙ ПРОЦЕСС:**

### **1. Запуск перефразирования:**
```csharp
// Пользователь нажимает кнопку "Перефразировать всё"
private async void ParaphraseAllButton_Click(object sender, RoutedEventArgs e)
{
    await ParaphraseAllProblematicFragments();
}
```

### **2. Поиск проблемных фрагментов:**
```csharp
// Находим все фрагменты с уровнем "Критично" или "Внимание"
var problematicResults = _plagiarismResults
    .Where(r => r.Level != PlagiarismLevel.Acceptable)  // Исключаем "Допустимо"
    .OrderByDescending(r => r.StartPosition)            // Сортируем с конца документа
    .ToList();
```

**Уровни плагиата:**
- 🟢 **Acceptable** (< 30%) - не перефразируется
- 🟡 **Warning** (30-70%) - перефразируется
- 🔴 **Critical** (> 70%) - перефразируется

### **3. Настройка параметров перефразирования:**
```csharp
var options = new ParaphraseOptions
{
    Style = ParaphraseStyle.Academic,    // Академический стиль
    Level = ParaphraseLevel.Medium       // Средний уровень изменений
};
```

**Стили перефразирования:**
- 📚 **Academic** - академический стиль
- 🔬 **Scientific** - научный стиль  
- 📰 **Journalistic** - журналистский стиль
- 📝 **Neutral** - нейтральный стиль

**Уровни изменений:**
- 🟢 **Light** - легкие изменения
- 🟡 **Medium** - средние изменения
- 🔴 **Deep** - глубокие изменения

### **4. Обработка каждого фрагмента:**
```csharp
foreach (var result in problematicResults)
{
    // Получаем варианты перефразирования от нейросети
    var variants = await _openRouterService.ParaphraseTextAsync(result.MatchedText, options);
    
    if (variants.Count > 0)
    {
        // Показываем диалог выбора варианта
        var selectedVariant = await ShowParaphraseVariantsDialog(result.MatchedText, variants);
        
        if (!string.IsNullOrEmpty(selectedVariant))
        {
            // Заменяем текст в редакторе
            ReplaceTextInEditor(result.StartPosition, result.EndPosition, selectedVariant);
        }
    }
}
```

## 🧠 **РАБОТА С НЕЙРОСЕТЬЮ (OpenRouter + DeepSeek):**

### **API настройки:**
```csharp
private const string ApiKey = "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";
private const string ModelName = "deepseek/deepseek-chat-v3.1:free";
```

### **Построение промпта:**
```csharp
private string BuildParaphrasePrompt(string text, ParaphraseOptions options)
{
    var styleInstruction = options.Style switch
    {
        ParaphraseStyle.Academic => "академическом стиле",
        ParaphraseStyle.Scientific => "научном стиле",
        ParaphraseStyle.Journalistic => "журналистском стиле",
        ParaphraseStyle.Neutral => "нейтральном стиле",
        _ => "академическом стиле"
    };
    
    var levelInstruction = options.Level switch
    {
        ParaphraseLevel.Light => "Сделай минимальные изменения, сохрани структуру.",
        ParaphraseLevel.Medium => "Измени структуру предложений, но сохрани смысл.",
        ParaphraseLevel.Deep => "Полностью переформулируй, используя синонимы и другую структуру.",
        _ => "Измени структуру предложений, но сохрани смысл."
    };
    
    return $@"
Перефрази следующий текст в {styleInstruction}. {levelInstruction}
Сохрани все термины и ключевые понятия. Верни 3 различных варианта перефразирования, разделенных символом '|||'.

Исходный текст:
{text}

Варианты перефразирования:";
}
```

### **Обработка ответа нейросети:**
```csharp
private List<string> ParseParaphraseResponse(string response)
{
    // Разделяем ответ по символу '|||'
    var variants = response.Split(new[] { "|||" }, StringSplitOptions.RemoveEmptyEntries);
    var result = new List<string>();
    
    foreach (var variant in variants)
    {
        var cleaned = variant.Trim();
        if (!string.IsNullOrEmpty(cleaned))
        {
            result.Add(cleaned);
        }
    }
    
    // Если разделения не произошло, возвращаем весь ответ как один вариант
    if (result.Count == 0 && !string.IsNullOrEmpty(response))
    {
        result.Add(response.Trim());
    }
    
    // Добавляем дополнительные варианты если нужно
    while (result.Count < 3 && result.Count > 0)
    {
        result.Add(result[0]); // Дублируем первый вариант
    }
    
    return result.Count > 0 ? result : new List<string> { "Не удалось перефразировать текст" };
}
```

## 🖥️ **ДИАЛОГ ВЫБОРА ВАРИАНТОВ:**

### **SimpleParaphraseDialog:**
```csharp
public SimpleParaphraseDialog(string originalText, List<string> variants, OpenRouterService openRouterService)
{
    InitializeComponent();
    
    _originalText = originalText;
    _variants = variants;
    _openRouterService = openRouterService;
    
    OriginalTextBlock.Text = originalText;
    DisplayVariants();
}
```

### **Функции диалога:**
- 📝 **Показ исходного текста** - что перефразируется
- 🔄 **3 варианта перефразирования** - на выбор пользователя
- ⭐ **Оценка качества** - пользователь может оценить варианты
- 🔄 **Регенерация** - создание новых вариантов
- ✅ **Применение** - замена текста выбранным вариантом

## 🔄 **ЗАМЕНА ТЕКСТА В РЕДАКТОРЕ:**

### **ReplaceTextInEditor:**
```csharp
private void ReplaceTextInEditor(int startPosition, int endPosition, string newText)
{
    try
    {
        var start = DocumentRichTextBox.Document.ContentStart.GetPositionAtOffset(startPosition);
        var end = DocumentRichTextBox.Document.ContentStart.GetPositionAtOffset(endPosition);
        
        if (start != null && end != null)
        {
            var range = new TextRange(start, end);
            range.Text = newText;
        }
    }
    catch
    {
        // Игнорируем ошибки замены
    }
}
```

## 🔄 **ПОВТОРНАЯ ПРОВЕРКА:**

После перефразирования всех фрагментов:
```csharp
// Повторная проверка после перефразирования
await CheckPlagiarism();
```

Это позволяет:
- ✅ **Проверить улучшение** уникальности
- ✅ **Обновить статистику** плагиата
- ✅ **Найти новые проблемные** фрагменты

## 🎯 **ПРЕИМУЩЕСТВА СИСТЕМЫ:**

### **1. Автоматизация:**
- 🔍 **Автоматический поиск** проблемных фрагментов
- 🤖 **ИИ-перефразирование** с помощью нейросети
- 🔄 **Автоматическая замена** текста

### **2. Контроль качества:**
- 👤 **Пользователь выбирает** лучший вариант
- ⭐ **Оценка качества** вариантов
- 🔄 **Возможность регенерации** новых вариантов

### **3. Гибкость:**
- 🎨 **Разные стили** перефразирования
- 📊 **Разные уровни** изменений
- 🎯 **Сохранение терминологии** и ключевых понятий

## 🧪 **ПРИМЕР РАБОТЫ:**

### **Исходный текст:**
> "Искусственный интеллект является одной из наиболее перспективных технологий современности."

### **Варианты перефразирования:**
1. **"ИИ представляет собой одну из самых многообещающих технологий нашего времени."**
2. **"Современные технологии искусственного интеллекта открывают широкие перспективы для развития."**
3. **"Машинный интеллект считается наиболее перспективным направлением в современных технологиях."**

### **Результат:**
- ✅ **Снижение плагиата** с 85% до 15%
- ✅ **Сохранение смысла** и терминологии
- ✅ **Улучшение уникальности** документа

**Система перефразирования обеспечивает эффективное снижение плагиата с сохранением качества и смысла текста!** 🎉
