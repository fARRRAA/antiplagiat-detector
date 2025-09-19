using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Plagiat.Models;

namespace Plagiat.Services
{
    public class CitationService
    {
        private readonly OpenRouterService _openRouterService;
        public static Action<string> LogAction;

        public CitationService(OpenRouterService openRouterService)
        {
            _openRouterService = openRouterService;
        }
        
        private void LogDebug(string message)
        {
            LogAction?.Invoke(message);
            Console.WriteLine(message); // Дублируем в консоль
        }

        public async Task<List<Citation>> FindCitationsInTextAsync(string text, int documentId)
        {
            var citations = new List<Citation>();

            LogDebug($"Поиск цитат в тексте длиной {text.Length} символов...");
            LogDebug($"Первые 200 символов текста: {text.Substring(0, Math.Min(200, text.Length))}");

            // Поиск прямых цитат в кавычках
            var directQuotes = FindDirectQuotes(text);
            LogDebug($"Найдено прямых цитат: {directQuotes.Count}");
            foreach (var quote in directQuotes)
            {
                citations.Add(new Citation
                {
                    DocumentId = documentId,
                    QuotedText = quote.Text,
                    StartPosition = quote.StartPosition,
                    EndPosition = quote.EndPosition,
                    Type = CitationType.Direct,
                    IsFormatted = false
                });
            }

            // Поиск блочных цитат (отступы)
            var blockQuotes = FindBlockQuotes(text);
            LogDebug($"Найдено блочных цитат: {blockQuotes.Count}");
            foreach (var quote in blockQuotes)
            {
                citations.Add(new Citation
                {
                    DocumentId = documentId,
                    QuotedText = quote.Text,
                    StartPosition = quote.StartPosition,
                    EndPosition = quote.EndPosition,
                    Type = CitationType.Block,
                    IsFormatted = false
                });
            }

            // Поиск цитат с указанием источников (например, "по словам...", "как отмечает...")
            var indirectQuotes = FindIndirectQuotes(text);
            LogDebug($"Найдено косвенных цитат: {indirectQuotes.Count}");
            foreach (var quote in indirectQuotes)
            {
                citations.Add(new Citation
                {
                    DocumentId = documentId,
                    QuotedText = quote.Text,
                    StartPosition = quote.StartPosition,
                    EndPosition = quote.EndPosition,
                    Type = CitationType.Indirect,
                    IsFormatted = false
                });
            }

            // Поиск ссылок и номерных цитат
            var referenceQuotes = FindReferenceCitations(text);
            LogDebug($"Найдено ссылочных цитат: {referenceQuotes.Count}");
            foreach (var quote in referenceQuotes)
            {
                citations.Add(new Citation
                {
                    DocumentId = documentId,
                    QuotedText = quote.Text,
                    StartPosition = quote.StartPosition,
                    EndPosition = quote.EndPosition,
                    Type = CitationType.Reference,
                    IsFormatted = false
                });
            }

            // Использование AI для поиска дополнительных цитат (ВРЕМЕННО ОТКЛЮЧЕНО)
            try
            {
                Console.WriteLine("AI поиск цитат временно отключен для диагностики");
                // var aiQuotes = await _openRouterService.IdentifyQuotationsAsync(text);
                // var aiCitations = ParseAIQuotations(aiQuotes, documentId);
                // Console.WriteLine($"AI нашел дополнительных цитат: {aiCitations.Count}");
                // citations.AddRange(aiCitations);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка AI поиска цитат: {ex.Message}");
            }

            // Валидация и очистка найденных цитат
            citations = ValidateAndCleanCitations(citations);

            LogDebug($"Всего найдено цитат после валидации: {citations.Count}");
            return citations;
        }

        public async Task<Source> FindSourceForCitationAsync(Citation citation)
        {
            try
            {
                var sourceInfo = await _openRouterService.FindSourceInfoAsync(citation.QuotedText);

                // Парсинг JSON ответа от AI с использованием JsonConvert
                var source = ParseSourceInfoWithJsonConvert(sourceInfo);
                if (source != null)
                {
                    source.Type = DetermineSourceType(source);
                    source.IsComplete = ValidateSourceCompleteness(source);
                }

                return source;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка поиска источника: {ex.Message}");
                return CreateUnknownSource();
            }
        }

        public string FormatCitation(Citation citation, CitationStyle style)
        {
            // Простое форматирование без источников
            return FormatSimpleCitation(citation, style);
        }

        public string GenerateInTextCitation(Citation citation, CitationStyle style)
        {
            if (citation.Source == null)
                return "";

            switch (style)
            {
                case CitationStyle.GOST:
                    return GenerateGOSTInTextCitation(citation);
                case CitationStyle.APA:
                    return GenerateAPAInTextCitation(citation);
                case CitationStyle.MLA:
                    return GenerateMLAInTextCitation(citation);
                case CitationStyle.Chicago:
                    return GenerateChicagoInTextCitation(citation);
                case CitationStyle.Harvard:
                    return GenerateHarvardInTextCitation(citation);
                case CitationStyle.Vancouver:
                    return GenerateVancouverInTextCitation(citation);
                default:
                    return GenerateGOSTInTextCitation(citation);
            }
        }

        // УЛУЧШЕННЫЕ РЕГУЛЯРНЫЕ ВЫРАЖЕНИЯ ДЛЯ ПОИСКА ЦИТАТ

        private List<QuoteMatch> FindDirectQuotes(string text)
        {
            var quotes = new List<QuoteMatch>();

            // Улучшенные паттерны для поиска цитат в различных типах кавычек
            var patterns = new[]
            {
                @"«([^»]{5,500})»", // Русские кавычки - СНИЖЕН МИНИМУМ ДЛЯ ТЕСТИРОВАНИЯ
                @"""([^""]{5,500})""", // Английские двойные кавычки - СНИЖЕН МИНИМУМ
                @"'([^']{5,500})'", // Одинарные кавычки - СНИЖЕН МИНИМУМ  
                @"«([^»]*?)»(?:\s*\([^)]+\))?", // Кавычки с возможными ссылками
                @"""([^""]*?)""(?:\s*\([^)]+\))?", // Английские кавычки с ссылками
            };

            foreach (var pattern in patterns)
            {
                LogDebug($"Ищем по паттерну: {pattern}");
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                LogDebug($"Найдено совпадений: {matches.Count}");

                foreach (Match match in matches)
                {
                    var quotedText = match.Groups[1].Value.Trim();
                    LogDebug($"Потенциальная цитата: {quotedText.Substring(0, Math.Min(50, quotedText.Length))}...");

                    // Валидация найденной цитаты
                    if (IsValidQuote(quotedText))
                    {
                        LogDebug($"Цитата прошла валидацию");
                        quotes.Add(new QuoteMatch
                        {
                            Text = quotedText,
                            StartPosition = match.Index,
                            EndPosition = match.Index + match.Length
                        });
                    }
                    else
                    {
                        LogDebug($"Цитата НЕ прошла валидацию");
                    }
                }
            }

            return RemoveDuplicateQuotes(quotes);
        }

        private List<QuoteMatch> FindBlockQuotes(string text)
        {
            var quotes = new List<QuoteMatch>();
            var lines = text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Улучшенные паттерны для блочных цитат
                if (IsBlockQuoteLine(line))
                {
                    var quoteText = line.Trim();

                    // Проверяем следующие строки на продолжение цитаты
                    var fullQuote = quoteText;
                    var endLineIndex = i;

                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        var nextLine = lines[j];
                        if (IsBlockQuoteLine(nextLine))
                        {
                            fullQuote += "\n" + nextLine.Trim();
                            endLineIndex = j;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (IsValidQuote(fullQuote) && fullQuote.Length >= 50)
                    {
                        var startPos = GetLinePosition(text, i);
                        var endPos = GetLinePosition(text, endLineIndex) + lines[endLineIndex].Length;

                        quotes.Add(new QuoteMatch
                        {
                            Text = fullQuote.Trim(),
                            StartPosition = startPos,
                            EndPosition = endPos
                        });
                    }

                    i = endLineIndex; // Пропускаем обработанные строки
                }
            }

            return quotes;
        }

        private bool IsBlockQuoteLine(string line)
        {
            // Различные способы форматирования блочных цитат
            return line.StartsWith("    ") || // 4 пробела
                   line.StartsWith("\t") || // Табуляция
                   line.StartsWith("> ") || // Markdown стиль
                   Regex.IsMatch(line, @"^\s{2,}[А-ЯA-Z]"); // Отступ + заглавная буква
        }

        private List<QuoteMatch> FindIndirectQuotes(string text)
        {
            var quotes = new List<QuoteMatch>();

            // Расширенные паттерны для косвенных цитат
            var patterns = new[]
            {
                // Базовые паттерны
                @"по словам ([^,\.]{3,50}),?\s*([^\.]{20,300})",
                @"как отмечает ([^,\.]{3,50}),?\s*([^\.]{20,300})",
                @"согласно ([^,\.]{3,50}),?\s*([^\.]{20,300})",
                @"по мнению ([^,\.]{3,50}),?\s*([^\.]{20,300})",
                @"как пишет ([^,\.]{3,50}),?\s*([^\.]{20,300})",
                @"в работе ([^,\.]{3,50})\s+([^\.]{20,300})",
                @"исследование ([^,\.]{3,50}) показало,?\s*что\s+([^\.]{20,300})",
                
                // Дополнительные паттерны
                @"([А-ЯA-Z][а-яa-z]+\s+[А-ЯA-Z]\.[А-ЯA-Z]\.|[А-ЯA-Z][а-яa-z]+)\s+утверждает,?\s*что\s+([^\.]{20,300})",
                @"([А-ЯA-Z][а-яa-z]+\s+[А-ЯA-Z]\.[А-ЯA-Z]\.|[А-ЯA-Z][а-яa-z]+)\s+полагает,?\s*что\s+([^\.]{20,300})",
                @"([А-ЯA-Z][а-яa-z]+\s+[А-ЯA-Z]\.[А-ЯA-Z]\.|[А-ЯA-Z][а-яa-z]+)\s+считает,?\s*что\s+([^\.]{20,300})",
                @"в статье ([^,\.]{3,50})\s+говорится,?\s*что\s+([^\.]{20,300})",
                @"автор ([^,\.]{3,50})\s+подчеркивает,?\s*что\s+([^\.]{20,300})",
                @"как указывает ([^,\.]{3,50}),?\s*([^\.]{20,300})",
                @"по данным ([^,\.]{3,50}),?\s*([^\.]{20,300})",
                @"результаты исследования ([^,\.]{3,50})\s+показывают,?\s*что\s+([^\.]{20,300})",
                
                // Паттерны для ссылок на источники
                @"как показано в\s+([^,\.]{3,50}),?\s*([^\.]{20,300})",
                @"см\.?\s*([^,\.]{3,50}),?\s*([^\.]{20,300})",
                @"цит\.?\s*по:?\s*([^,\.]{3,50}),?\s*([^\.]{20,300})"
            };

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                foreach (Match match in matches)
                {
                    var fullMatch = match.Value;
                    if (IsValidQuote(fullMatch))
                    {
                        quotes.Add(new QuoteMatch
                        {
                            Text = fullMatch.Trim(),
                            StartPosition = match.Index,
                            EndPosition = match.Index + match.Length
                        });
                    }
                }
            }

            return RemoveDuplicateQuotes(quotes);
        }

        private List<QuoteMatch> FindReferenceCitations(string text)
        {
            var quotes = new List<QuoteMatch>();

            // Паттерны для поиска ссылок и номерных цитат
            var patterns = new[]
            {
                @"\[(\d+)\]", // [1], [2], etc.
                @"\[(\d+,\s*\d+(?:,\s*\d+)*)\]", // [1, 2, 3]
                @"\[(\d+-\d+)\]", // [1-5]
                @"\(([А-ЯA-Z][а-яa-z]+,?\s*\d{4}[а-я]?)\)", // (Иванов, 2023)
                @"\(([А-ЯA-Z][а-яa-z]+\s+et\s+al\.,?\s*\d{4}[а-я]?)\)", // (Иванов et al., 2023)
                @"\(([А-ЯA-Z][а-яa-z]+\s+и\s+др\.,?\s*\d{4}[а-я]?)\)", // (Иванов и др., 2023)
                @"(?:см\.|смотри|см\.\s*также)\s*\[(\d+(?:,\s*\d+)*)\]", // см. [1, 2]
                @"(?:см\.|смотри|см\.\s*также)\s*\(([^)]+)\)", // см. (Иванов, 2023)
            };

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    quotes.Add(new QuoteMatch
                    {
                        Text = match.Value,
                        StartPosition = match.Index,
                        EndPosition = match.Index + match.Length
                    });
                }
            }

            return quotes;
        }

        // УЛУЧШЕННЫЙ ПАРСИНГ JSON

        private Source ParseSourceInfoWithJsonConvert(string jsonResponse)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonResponse))
                    return null;

                // Очистка ответа от лишнего текста
                var cleanedJson = ExtractJsonFromResponse(jsonResponse);
                if (string.IsNullOrEmpty(cleanedJson))
                    return null;

                // Парсинг с помощью JsonConvert
                var jsonObject = JObject.Parse(cleanedJson);

                var source = new Source();

                // Безопасное извлечение данных
                source.Author = jsonObject["author"]?.ToString();
                source.Title = jsonObject["title"]?.ToString();
                source.Publisher = jsonObject["publisher"]?.ToString();
                source.Url = jsonObject["url"]?.ToString();
                source.DOI = jsonObject["doi"]?.ToString();
                source.ISBN = jsonObject["isbn"]?.ToString();
                source.City = jsonObject["city"]?.ToString();
                source.Volume = jsonObject["volume"]?.ToString();
                source.Issue = jsonObject["issue"]?.ToString();
                source.Pages = jsonObject["pages"]?.ToString();

                // Парсинг года
                if (jsonObject["year"] != null)
                {
                    if (int.TryParse(jsonObject["year"].ToString(), out int year))
                    {
                        source.Year = year;
                    }
                }

                // Парсинг даты доступа
                if (jsonObject["access_date"] != null)
                {
                    if (DateTime.TryParse(jsonObject["access_date"].ToString(), out DateTime accessDate))
                    {
                        source.AccessDate = accessDate;
                    }
                }

                // Парсинг типа источника
                if (jsonObject["type"] != null)
                {
                    var typeString = jsonObject["type"].ToString().ToLower();
                    source.Type = ParseSourceType(typeString);
                }

                return string.IsNullOrEmpty(source.Title) ? null : source;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Ошибка парсинга JSON: {ex.Message}");
                return ParseSourceInfoFallback(jsonResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки источника: {ex.Message}");
                return null;
            }
        }

        private string ExtractJsonFromResponse(string response)
        {
            // Поиск JSON в ответе
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                return response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

            // Если не найден объект, ищем массив
            jsonStart = response.IndexOf('[');
            jsonEnd = response.LastIndexOf(']');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                return response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

            return null;
        }

        private Source ParseSourceInfoFallback(string response)
        {
            // Fallback парсинг с регулярными выражениями
            try
            {
                var source = new Source();

                var authorMatch = Regex.Match(response, @"(?:author|автор)[:""]\s*[""']?([^""',\n]+)[""']?", RegexOptions.IgnoreCase);
                if (authorMatch.Success)
                    source.Author = authorMatch.Groups[1].Value.Trim();

                var titleMatch = Regex.Match(response, @"(?:title|название)[:""]\s*[""']?([^""',\n]+)[""']?", RegexOptions.IgnoreCase);
                if (titleMatch.Success)
                    source.Title = titleMatch.Groups[1].Value.Trim();

                var yearMatch = Regex.Match(response, @"(?:year|год)[:""]\s*(\d{4})", RegexOptions.IgnoreCase);
                if (yearMatch.Success && int.TryParse(yearMatch.Groups[1].Value, out int year))
                    source.Year = year;

                var publisherMatch = Regex.Match(response, @"(?:publisher|издательство)[:""]\s*[""']?([^""',\n]+)[""']?", RegexOptions.IgnoreCase);
                if (publisherMatch.Success)
                    source.Publisher = publisherMatch.Groups[1].Value.Trim();

                return string.IsNullOrEmpty(source.Title) ? null : source;
            }
            catch
            {
                return null;
            }
        }

        private SourceType ParseSourceType(string typeString)
        {
            switch (typeString.ToLower())
            {
                case "book":
                case "книга":
                case "монография":
                    return SourceType.Book;

                case "article":
                case "статья":
                case "journal":
                case "журнал":
                    return SourceType.Journal;

                case "website":
                case "веб-сайт":
                case "интернет":
                case "сайт":
                    return SourceType.Website;

                case "thesis":
                case "диссертация":
                case "дипломная":
                    return SourceType.Thesis;

                case "conference":
                case "конференция":
                case "доклад":
                    return SourceType.Conference;

                default:
                    return SourceType.Unknown;
            }
        }

        // ВАЛИДАЦИЯ ЦИТАТ

        private List<Citation> ValidateAndCleanCitations(List<Citation> citations)
        {
            var validCitations = new List<Citation>();

            foreach (var citation in citations)
            {
                if (IsValidCitation(citation))
                {
                    // Очистка и нормализация текста цитаты
                    citation.QuotedText = CleanQuoteText(citation.QuotedText);
                    validCitations.Add(citation);
                }
            }

            // Удаление дублирующихся цитат
            return RemoveDuplicateCitations(validCitations);
        }

        private bool IsValidCitation(Citation citation)
        {
            if (citation == null || string.IsNullOrWhiteSpace(citation.QuotedText))
                return false;

            // Минимальная длина цитаты
            if (citation.QuotedText.Length < 10)
                return false;

            // Максимальная длина цитаты
            if (citation.QuotedText.Length > 1000)
                return false;

            // Проверка на наличие осмысленного содержания
            if (!ContainsMeaningfulContent(citation.QuotedText))
                return false;

            // Проверка позиций
            if (citation.StartPosition < 0 || citation.EndPosition <= citation.StartPosition)
                return false;

            return true;
        }

        private bool IsValidQuote(string text)
        {
            LogDebug($"Валидация цитаты: '{text}'");
            
            if (string.IsNullOrWhiteSpace(text))
            {
                LogDebug("Отклонено: пустой текст");
                return false;
            }

            // Минимальная длина - СНИЖЕНО ДЛЯ ТЕСТИРОВАНИЯ
            if (text.Length < 5)
            {
                LogDebug($"Отклонено: слишком короткий ({text.Length} символов)");
                return false;
            }

            // Проверка на содержание букв (не только цифры и знаки)
            if (!Regex.IsMatch(text, @"[а-яёА-ЯЁa-zA-Z]"))
            {
                LogDebug("Отклонено: нет букв");
                return false;
            }

            // Проверка на осмысленное содержание
            var meaningful = ContainsMeaningfulContent(text);
            if (!meaningful)
            {
                LogDebug("Отклонено: недостаточно осмысленного содержания");
                return false;
            }
            
            LogDebug("Цитата принята!");
            return true;
        }

        private bool ContainsMeaningfulContent(string text)
        {
            // Проверка на наличие осмысленных слов
            var words = text.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var meaningfulWords = words.Count(w => w.Length > 2 && Regex.IsMatch(w, @"[а-яёА-ЯЁa-zA-Z]"));

            return meaningfulWords >= 3; // Минимум 3 осмысленных слова
        }

        private string CleanQuoteText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Удаление лишних пробелов и переносов
            text = Regex.Replace(text, @"\s+", " ");
            text = text.Trim();

            // Удаление HTML тегов если есть
            text = Regex.Replace(text, @"<[^>]+>", "");

            return text;
        }

        private List<QuoteMatch> RemoveDuplicateQuotes(List<QuoteMatch> quotes)
        {
            var unique = new List<QuoteMatch>();
            var seen = new HashSet<string>();

            foreach (var quote in quotes)
            {
                var normalized = quote.Text.ToLower().Trim();
                if (!seen.Contains(normalized))
                {
                    seen.Add(normalized);
                    unique.Add(quote);
                }
            }

            return unique;
        }

        private List<Citation> RemoveDuplicateCitations(List<Citation> citations)
        {
            var unique = new List<Citation>();
            var seen = new HashSet<string>();

            foreach (var citation in citations)
            {
                var key = $"{citation.DocumentId}_{citation.QuotedText.ToLower().Trim()}";
                if (!seen.Contains(key))
                {
                    seen.Add(key);
                    unique.Add(citation);
                }
            }

            return unique;
        }

        // ОСТАЛЬНЫЕ МЕТОДЫ (без изменений)

        private int GetLinePosition(string text, int lineIndex)
        {
            var lines = text.Split('\n');
            int position = 0;

            for (int i = 0; i < lineIndex && i < lines.Length; i++)
            {
                position += lines[i].Length + 1; // +1 для символа новой строки
            }

            return position;
        }

        private SourceType DetermineSourceType(Source source)
        {
            if (!string.IsNullOrEmpty(source.DOI) || !string.IsNullOrEmpty(source.Volume))
                return SourceType.Journal;

            if (!string.IsNullOrEmpty(source.Url))
                return SourceType.Website;

            if (!string.IsNullOrEmpty(source.ISBN))
                return SourceType.Book;

            return SourceType.Unknown;
        }

        private bool ValidateSourceCompleteness(Source source)
        {
            return !string.IsNullOrEmpty(source.Title) &&
                   !string.IsNullOrEmpty(source.Author) &&
                   source.Year.HasValue;
        }

        private Source CreateUnknownSource()
        {
            return new Source
            {
                Title = "Неизвестный источник",
                Author = "Автор не указан",
                Type = SourceType.Unknown,
                IsComplete = false
            };
        }

        private string FormatDirectCitation(Citation citation, string reference, CitationStyle style)
        {
            switch (style)
            {
                case CitationStyle.GOST:
                    return $"«{citation.QuotedText}» [{reference}]";
                case CitationStyle.APA:
                    return $"\"{citation.QuotedText}\" ({reference})";
                case CitationStyle.MLA:
                    return $"\"{citation.QuotedText}\" ({reference})";
                default:
                    return $"«{citation.QuotedText}» [{reference}]";
            }
        }

        private string FormatIndirectCitation(Citation citation, string reference, CitationStyle style)
        {
            switch (style)
            {
                case CitationStyle.GOST:
                    return $"{citation.QuotedText} [{reference}]";
                case CitationStyle.APA:
                    return $"{citation.QuotedText} ({reference})";
                case CitationStyle.MLA:
                    return $"{citation.QuotedText} ({reference})";
                default:
                    return $"{citation.QuotedText} [{reference}]";
            }
        }

        private string FormatBlockCitation(Citation citation, string reference, CitationStyle style)
        {
            return $"{citation.QuotedText}\n\n{reference}";
        }

        private string FormatEpigraphCitation(Citation citation, string reference, CitationStyle style)
        {
            return $"{citation.QuotedText}\n\n© {reference}";
        }

        private string FormatReferenceCitation(Citation citation, string reference, CitationStyle style)
        {
            return citation.QuotedText; // Ссылки обычно остаются как есть
        }

        private string GenerateGOSTInTextCitation(Citation citation)
        {
            return $"[{citation.Source.Id}]";
        }

        private string GenerateAPAInTextCitation(Citation citation)
        {
            var author = citation.Source.Author?.Split(' ').LastOrDefault() ?? "Unknown";
            return $"({author}, {citation.Source.Year})";
        }

        private string GenerateMLAInTextCitation(Citation citation)
        {
            var author = citation.Source.Author?.Split(' ').LastOrDefault() ?? "Unknown";
            return $"({author})";
        }

        private string GenerateChicagoInTextCitation(Citation citation)
        {
            return GenerateAPAInTextCitation(citation);
        }

        private string GenerateHarvardInTextCitation(Citation citation)
        {
            return GenerateAPAInTextCitation(citation);
        }

        private string GenerateVancouverInTextCitation(Citation citation)
        {
            return $"({citation.Source.Id})";
        }

        // Простое форматирование цитат
        private string FormatSimpleCitation(Citation citation, CitationStyle style)
        {
            var text = citation.QuotedText.Trim();
            
            // Обрезаем слишком длинные цитаты
            if (text.Length > 150)
            {
                text = text.Substring(0, 147) + "...";
            }

            switch (style)
            {
                case CitationStyle.GOST:
                    return citation.Type == CitationType.Direct 
                        ? $"«{text}»"
                        : $"📝 {text}";
                        
                case CitationStyle.APA:
                    return citation.Type == CitationType.Direct 
                        ? $"\"{text}\""
                        : $"{text}";
                        
                case CitationStyle.MLA:
                    return citation.Type == CitationType.Direct 
                        ? $"⟨{text}⟩"
                        : $"{text}";
                        
                case CitationStyle.Chicago:
                    return citation.Type == CitationType.Direct 
                        ? $"『{text}』"
                        : $"{text}";
                        
                case CitationStyle.Harvard:
                    return citation.Type == CitationType.Direct 
                        ? $"‹{text}›"
                        : $"{text}";
                        
                case CitationStyle.Vancouver:
                    return citation.Type == CitationType.Direct 
                        ? $"【{text}】"
                        : $"{text}";
                        
                default:
                    return citation.Type == CitationType.Direct 
                        ? $"«{text}»"
                        : $"{text}";
            }
        }

        private List<Citation> ParseAIQuotations(List<string> aiResponses, int documentId)
        {
            var citations = new List<Citation>();

            foreach (var response in aiResponses)
            {
                try
                {
                    // Улучшенная обработка JSON ответа от AI
                    var jsonString = ExtractJsonFromResponse(response);
                    if (!string.IsNullOrEmpty(jsonString))
                    {
                        var citationsArray = JArray.Parse(jsonString);

                        foreach (var item in citationsArray)
                        {
                            var citation = new Citation
                            {
                                DocumentId = documentId,
                                QuotedText = item["text"]?.ToString() ?? "",
                                StartPosition = item["startPosition"]?.ToObject<int>() ?? 0,
                                EndPosition = item["endPosition"]?.ToObject<int>() ?? 0,
                                Type = ParseCitationType(item["type"]?.ToString()),
                                IsFormatted = false
                            };

                            if (IsValidCitation(citation))
                            {
                                citations.Add(citation);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка парсинга AI ответа: {ex.Message}");
                    // Fallback - создаем простую цитату из ответа
                    if (!string.IsNullOrWhiteSpace(response) && response.Length > 10)
                    {
                        citations.Add(new Citation
                        {
                            DocumentId = documentId,
                            QuotedText = response.Trim(),
                            StartPosition = 0,
                            EndPosition = response.Length,
                            Type = CitationType.Indirect,
                            IsFormatted = false
                        });
                    }
                }
            }

            return citations;
        }

        private CitationType ParseCitationType(string typeString)
        {
            if (string.IsNullOrEmpty(typeString))
                return CitationType.Indirect;

            switch (typeString.ToLower())
            {
                case "direct":
                case "прямая":
                    return CitationType.Direct;
                case "indirect":
                case "косвенная":
                    return CitationType.Indirect;
                case "block":
                case "блочная":
                    return CitationType.Block;
                case "reference":
                case "ссылка":
                    return CitationType.Reference;
                default:
                    return CitationType.Indirect;
            }
        }
    }

    public class QuoteMatch
    {
        public string Text { get; set; }
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
    }
}
