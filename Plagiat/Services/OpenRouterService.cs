using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Plagiat.Services
{
    public class OpenRouterService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly string _modelName;

        public OpenRouterService()
        {
            _baseUrl = AppConfig.Instance.OpenRouterBaseUrl;
            _apiKey = AppConfig.Instance.OpenRouterApiKey;
            _modelName = AppConfig.Instance.DefaultModel;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "http://localhost");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "Antiplagiat Assistant");
        }

        public async Task<List<string>> ParaphraseTextAsync(string text, ParaphraseOptions options)
        {
            try
            {
                var prompt = BuildParaphrasePrompt(text, options);
                var response = await SendChatRequestAsync(prompt);
                
                return ParseParaphraseResponse(response);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при перефразировании: {ex.Message}", ex);
            }
        }

        public async Task<string> FindSourceInfoAsync(string quotedText)
        {
            try
            {
                var prompt = $@"
Найди информацию об источнике для следующей цитаты. Верни результат в формате JSON:
{{
    ""author"": ""автор"",
    ""title"": ""название работы"",
    ""year"": год,
    ""publisher"": ""издательство"",
    ""type"": ""book/article/website/journal""
}}

Цитата: {quotedText}

Если не можешь найти точную информацию, верни null для соответствующих полей.";

                var response = await SendChatRequestAsync(prompt);
                return response;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при поиске источника: {ex.Message}", ex);
            }
        }

        public async Task<string> DetectLanguageAsync(string text)
        {
            try
            {
                var prompt = $@"
Определи язык следующего текста. Верни только код языка (ru, en, de, fr, etc.):

{text.Substring(0, Math.Min(text.Length, 500))}";

                var response = await SendChatRequestAsync(prompt);
                return response.Trim().ToLower();
            }
            catch (Exception ex)
            {
                return "ru"; // По умолчанию русский
            }
        }

        public async Task<List<string>> IdentifyQuotationsAsync(string text)
        {
            try
            {
                var prompt = $@"
Найди все цитаты в тексте (прямые и косвенные). Верни результат в формате JSON массива:
[
    {{
        ""text"": ""текст цитаты"",
        ""type"": ""direct/indirect/block"",
        ""startPosition"": начальная_позиция,
        ""endPosition"": конечная_позиция
    }}
]

Текст для анализа:
{text}";

                var response = await SendChatRequestAsync(prompt);
                return new List<string> { response };
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при поиске цитат: {ex.Message}", ex);
            }
        }

        private string BuildParaphrasePrompt(string text, ParaphraseOptions options)
        {
            string styleInstruction;
            switch (options.Style)
            {
                case ParaphraseStyle.Academic:
                    styleInstruction = "академическом научном стиле";
                    break;
                case ParaphraseStyle.Scientific:
                    styleInstruction = "строгом научном стиле";
                    break;
                case ParaphraseStyle.Journalistic:
                    styleInstruction = "публицистическом стиле";
                    break;
                default:
                    styleInstruction = "нейтральном стиле";
                    break;
            }

            string levelInstruction;
            switch (options.Level)
            {
                case ParaphraseLevel.Light:
                    levelInstruction = "Сделай легкие изменения, сохранив большую часть структуры.";
                    break;
                case ParaphraseLevel.Medium:
                    levelInstruction = "Сделай умеренные изменения в структуре и формулировках.";
                    break;
                case ParaphraseLevel.Deep:
                    levelInstruction = "Полностью переформулируй текст, сохранив только смысл.";
                    break;
                default:
                    levelInstruction = "Сделай умеренные изменения.";
                    break;
            }

            return $@"
Перефрази следующий текст в {styleInstruction}. {levelInstruction}
Сохрани все термины и ключевые понятия. Верни 3 различных варианта перефразирования, разделенных символом '|||'.

Исходный текст:
{text}

Варианты перефразирования:";
        }

        private List<string> ParseParaphraseResponse(string response)
        {
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

        private async Task<string> SendChatRequestAsync(string prompt)
        {
            var requestBody = new
            {
                model = _modelName,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = AppConfig.Instance.MaxTokens,
                temperature = AppConfig.Instance.Temperature
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"API Error: {response.StatusCode} - {responseContent}");
            }

            var chatResponse = JsonConvert.DeserializeObject<ChatCompletionResponse>(responseContent);
            return chatResponse?.Choices?[0]?.Message?.Content ?? "Нет ответа от AI";
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public class ParaphraseOptions
    {
        public ParaphraseStyle Style { get; set; } = ParaphraseStyle.Academic;
        public ParaphraseLevel Level { get; set; } = ParaphraseLevel.Medium;
        public bool PreserveTerminology { get; set; } = true;
    }

    public enum ParaphraseStyle
    {
        Academic,
        Scientific,
        Journalistic,
        Neutral
    }

    public enum ParaphraseLevel
    {
        Light,
        Medium,
        Deep
    }

    // Response models
    public class ChatCompletionResponse
    {
        public Choice[] Choices { get; set; }
    }

    public class Choice
    {
        public Message Message { get; set; }
    }

    public class Message
    {
        public string Content { get; set; }
    }
}
