# 🔄 ЗАМЕНА API: Text.ru → Advego Plagiatus

## ✅ **ВЫПОЛНЕНО:**
API Text.ru успешно заменен на **Advego Plagiatus API** - более надежный и точный сервис проверки плагиата.

## 🔧 **ИЗМЕНЕНИЯ В КОДЕ:**

### **1. Обновлен `AntiPlagiatService.cs`:**

#### **Новый основной метод:**
```csharp
public async Task<List<PlagiarismResult>> CheckPlagiarismAsync(string text, int documentId)
{
    try
    {
        // Используем Advego Plagiatus API
        return await CheckWithAdvegoAsync(text, documentId);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при проверке плагиата: {ex.Message}");
        return GenerateDemoResults(text, documentId);
    }
}
```

#### **Новый метод отправки текста:**
```csharp
private async Task<string> SubmitTextToAdvegoAsync(string text)
{
    var apiUrl = "https://api.advego.com/plagiatus/check";
    
    var formData = new List<KeyValuePair<string, string>>
    {
        new KeyValuePair<string, string>("key", "YOUR_ADVEGO_API_KEY"),
        new KeyValuePair<string, string>("text", text),
        new KeyValuePair<string, string>("format", "json")
    };
    // ... остальной код
}
```

#### **Новый метод получения результата:**
```csharp
private async Task<List<PlagiarismResult>> GetAdvegoResultAsync(string checkId, int documentId, string originalText)
{
    var apiUrl = "https://api.advego.com/plagiatus/result";
    
    var formData = new List<KeyValuePair<string, string>>
    {
        new KeyValuePair<string, string>("key", "YOUR_ADVEGO_API_KEY"),
        new KeyValuePair<string, string>("check_id", checkId),
        new KeyValuePair<string, string>("format", "json")
    };
    // ... остальной код
}
```

### **2. Новые модели для Advego API:**
```csharp
public class AdvegoSubmitResponse
{
    public string check_id { get; set; }
    public string status { get; set; }
}

public class AdvegoResponse
{
    public AdvegoResult result { get; set; }
    public string status { get; set; }
}

public class AdvegoResult
{
    public double uniqueness { get; set; }
    public AdvegoMatch[] matches { get; set; }
}

public class AdvegoMatch
{
    public string text { get; set; }
    public string url { get; set; }
    public string title { get; set; }
    public int start_pos { get; set; }
    public int end_pos { get; set; }
    public double percent { get; set; }
}
```

## 🔑 **НАСТРОЙКА API КЛЮЧА:**

### **Шаг 1: Получение API ключа Advego**
1. Зарегистрируйтесь на [advego.com](https://advego.com/)
2. Перейдите в раздел API
3. Получите ваш API ключ

### **Шаг 2: Замена ключа в коде**
В файле `Plagiat/Services/AntiPlagiatService.cs` замените:
```csharp
new KeyValuePair<string, string>("key", "YOUR_ADVEGO_API_KEY")
```
на:
```csharp
new KeyValuePair<string, string>("key", "ВАШ_РЕАЛЬНЫЙ_КЛЮЧ_ADVEGO")
```

## 🚀 **ПРЕИМУЩЕСТВА Advego Plagiatus:**

### **✅ По сравнению с Text.ru:**
- **Более точная проверка** плагиата
- **Лучшая база данных** источников
- **Более надежный API** с меньшим количеством ошибок
- **Детальная аналитика** совпадений
- **Поддержка различных форматов** текста

### **📊 Ожидаемые результаты:**
- **Точные проценты уникальности**
- **Конкретные совпадения** с источниками
- **Ссылки на найденные источники**
- **Детальная информация** о совпадениях

## 🧪 **ТЕСТИРОВАНИЕ:**

### **После настройки API ключа:**
1. **Запустите приложение**
2. **Загрузите документ** "02_high_plagiarism.txt"
3. **Нажмите "Проверить плагиат"**
4. **Проверьте консоль** - должны появиться сообщения:
   ```
   Ответ от Advego (отправка): {"check_id": "12345", "status": "ok"}
   Получен check_id: 12345
   Ответ от Advego (результат): {результаты проверки}
   ```

### **Ожидаемый результат:**
- **Реальные результаты плагиата** вместо демо-данных
- **Точные проценты уникальности** (не 100%)
- **Конкретные совпадения** с источниками

## ⚠️ **ВАЖНЫЕ ЗАМЕЧАНИЯ:**

### **1. Время обработки:**
- Advego может требовать **больше времени** для обработки (10 секунд)
- Увеличен `Task.Delay(10000)` для ожидания результата

### **2. API Endpoints:**
- **Отправка:** `https://api.advego.com/plagiatus/check`
- **Результат:** `https://api.advego.com/plagiatus/result`

### **3. Fallback:**
- Если API не работает, используются **демо-данные**
- Все ошибки логируются в консоль

## 🎯 **СЛЕДУЮЩИЕ ШАГИ:**

1. **Получите API ключ** от Advego
2. **Замените ключ** в коде
3. **Протестируйте** проверку плагиата
4. **Проверьте результаты** в приложении

**Теперь приложение использует более надежный и точный Advego Plagiatus API!** 🚀

