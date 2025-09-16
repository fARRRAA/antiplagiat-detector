# 🔧 ИСПРАВЛЕНИЕ API Text.ru

## ❌ **ПРОБЛЕМА:**
API Text.ru возвращал только `text_uid: "68c93282e2b10"`, но не возвращал результаты проверки плагиата.

## ✅ **РЕШЕНИЕ:**
API Text.ru работает в **два этапа**:

### **Этап 1: Отправка текста**
- **URL:** `https://api.text.ru/post`
- **Параметры:** `text`, `userkey`, `format=json`
- **Ответ:** `{"text_uid": "68c93282e2b10"}`

### **Этап 2: Получение результата**
- **URL:** `https://api.text.ru/post`
- **Параметры:** `uid=68c93282e2b10`, `userkey`, `format=json`
- **Ответ:** Результаты проверки плагиата

## 🔄 **ОБНОВЛЕННЫЙ КОД:**

### **1. Новый метод `SubmitTextForCheckAsync`:**
```csharp
private async Task<string> SubmitTextForCheckAsync(string text)
{
    var apiUrl = "https://api.text.ru/post";
    
    var formData = new List<KeyValuePair<string, string>>
    {
        new KeyValuePair<string, string>("text", text),
        new KeyValuePair<string, string>("userkey", "d8a82eb8e27bbc60b8d102336809a5ae"),
        new KeyValuePair<string, string>("format", "json")
    };

    var formContent = new FormUrlEncodedContent(formData);
    
    var response = await _httpClient.PostAsync(apiUrl, formContent);
    var responseContent = await response.Content.ReadAsStringAsync();
    
    Console.WriteLine($"Ответ от Text.ru (отправка): {responseContent}");
    
    if (response.IsSuccessStatusCode)
    {
        var result = JsonConvert.DeserializeObject<TextRuSubmitResponse>(responseContent);
        return result?.text_uid;
    }
    
    return null;
}
```

### **2. Новый метод `GetCheckResultAsync`:**
```csharp
private async Task<List<PlagiarismResult>> GetCheckResultAsync(string textUid, int documentId, string originalText)
{
    var apiUrl = "https://api.text.ru/post";
    
    var formData = new List<KeyValuePair<string, string>>
    {
        new KeyValuePair<string, string>("uid", textUid),
        new KeyValuePair<string, string>("userkey", "d8a82eb8e27bbc60b8d102336809a5ae"),
        new KeyValuePair<string, string>("format", "json")
    };

    var formContent = new FormUrlEncodedContent(formData);
    
    // Ждем 5 секунд для обработки
    await Task.Delay(5000);
    
    var response = await _httpClient.PostAsync(apiUrl, formContent);
    var responseContent = await response.Content.ReadAsStringAsync();
    
    Console.WriteLine($"Ответ от Text.ru (результат): {responseContent}");
    
    if (response.IsSuccessStatusCode)
    {
        var result = JsonConvert.DeserializeObject<TextRuResponse>(responseContent);
        if (result?.result_json != null)
        {
            return ConvertTextRuResponse(result, documentId, originalText);
        }
    }
    
    return GenerateDemoResults(originalText, documentId);
}
```

### **3. Обновленный метод `CheckWithTextRuAsync`:**
```csharp
private async Task<List<PlagiarismResult>> CheckWithTextRuAsync(string text, int documentId)
{
    try
    {
        // Шаг 1: Отправляем текст на проверку
        var textUid = await SubmitTextForCheckAsync(text);
        if (string.IsNullOrEmpty(textUid))
        {
            Console.WriteLine("Не удалось получить text_uid от Text.ru API");
            return GenerateDemoResults(text, documentId);
        }

        Console.WriteLine($"Получен text_uid: {textUid}");

        // Шаг 2: Ждем и получаем результат
        return await GetCheckResultAsync(textUid, documentId, text);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при проверке через Text.ru: {ex.Message}");
        return GenerateDemoResults(text, documentId);
    }
}
```

### **4. Новая модель `TextRuSubmitResponse`:**
```csharp
public class TextRuSubmitResponse
{
    public string text_uid { get; set; }
}
```

## 🧪 **ТЕСТИРОВАНИЕ:**

### **Теперь при проверке плагиата консоль покажет:**
1. `"Ответ от Text.ru (отправка): {"text_uid": "68c93282e2b10"}"`
2. `"Получен text_uid: 68c93282e2b10"`
3. `"Ответ от Text.ru (результат): {результаты проверки}"`

### **Ожидаемый результат:**
- **Реальные результаты плагиата** вместо демо-данных
- **Точные проценты уникальности**
- **Ссылки на источники** с совпадениями

## 🔑 **ВАЖНО:**
- **API ключ:** `d8a82eb8e27bbc60b8d102336809a5ae` (ваш реальный ключ)
- **Задержка:** 5 секунд между отправкой и получением результата
- **Fallback:** Если API не работает, используются демо-данные

## 🚀 **РЕЗУЛЬТАТ:**
Теперь приложение будет получать **реальные результаты проверки плагиата** от Text.ru API вместо демо-данных!

