using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Plagiat.Models;

namespace Plagiat.Services
{
    public class BibliographyService
    {
        private readonly OpenRouterService _openRouterService;

        public BibliographyService(OpenRouterService openRouterService)
        {
            _openRouterService = openRouterService;
        }

        /// <summary>
        /// Генерирует библиографию из списка цитат
        /// </summary>
        public List<Source> GenerateBibliography(List<Citation> citations, CitationStyle style)
        {
            var sources = citations
                .Where(c => c.Source != null)
                .Select(c => c.Source)
                .GroupBy(s => new { s.Title, s.Author, s.Year })
                .Select(g => g.First())
                .ToList();

            return SortSources(sources, style);
        }

        /// <summary>
        /// Асинхронно обогащает источники недостающими данными
        /// </summary>
        public async Task<List<Source>> EnrichSourcesAsync(List<Source> sources)
        {
            // Параллельная обработка для повышения производительности
            var enrichmentTasks = sources.Select(EnrichSingleSourceAsync).ToArray();
            var enrichedSources = await Task.WhenAll(enrichmentTasks);

            return enrichedSources.ToList();
        }

        /// <summary>
        /// Обогащает один источник недостающими данными
        /// </summary>
        public async Task<Source> EnrichSingleSourceAsync(Source source)
        {
            if (source.IsComplete)
                return source;

            try
            {
                var searchQuery = BuildSearchQuery(source);
                var enrichmentData = await _openRouterService.FindSourceInfoAsync(searchQuery);

                return MergeSourceData(source, enrichmentData);
            }
            catch (Exception ex)
            {
                // Логирование ошибки (можно добавить систему логирования)
                Console.WriteLine($"Ошибка при обогащении источника: {ex.Message}");
                return source;
            }
        }

        /// <summary>
        /// Форматирует библиографию согласно выбранному стилю
        /// </summary>
        public string FormatBibliography(List<Source> sources, CitationStyle style)
        {
            var bibliography = new StringBuilder();
            var sortedSources = SortSources(sources, style);

            for (int i = 0; i < sortedSources.Count; i++)
            {
                var source = sortedSources[i];
                var formattedEntry = FormatBibliographyEntry(source, style, i + 1);
                bibliography.AppendLine(formattedEntry);
            }

            return bibliography.ToString();
        }

        /// <summary>
        /// Экспортирует библиографию в файл
        /// </summary>
        public async Task<string> ExportBibliographyAsync(List<Source> sources, CitationStyle style, string filePath)
        {
            var bibliography = FormatBibliography(sources, style);

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var directory = Path.GetDirectoryName(filePath) ?? Environment.CurrentDirectory;
            var extension = Path.GetExtension(filePath);

            var fullPath = Path.Combine(directory, $"{fileName}_bibliography{extension}");

            await Task.Run(() => File.WriteAllText(fullPath, bibliography, Encoding.UTF8));

            return fullPath;
        }

        /// <summary>
        /// Проверяет источники на наличие проблем валидации
        /// </summary>
        public List<Source> ValidateBibliography(List<Source> sources)
        {
            return sources.Where(HasValidationIssues).ToList();
        }

        /// <summary>
        /// Находит дублирующиеся источники
        /// </summary>
        public List<Source> FindDuplicateSources(List<Source> sources)
        {
            var duplicates = new List<Source>();
            var seen = new HashSet<string>();

            foreach (var source in sources)
            {
                var key = GenerateSourceKey(source);
                if (seen.Contains(key))
                {
                    duplicates.Add(source);
                }
                else
                {
                    seen.Add(key);
                }
            }

            return duplicates;
        }

        /// <summary>
        /// Получает статистику библиографии
        /// </summary>
        public BibliographyStatistics GetBibliographyStatistics(List<Source> sources)
        {
            return new BibliographyStatistics
            {
                TotalSources = sources.Count,
                CompleteSources = sources.Count(s => s.IsComplete),
                IncompleteSources = sources.Count(s => !s.IsComplete),
                SourcesByType = sources.GroupBy(s => s.Type)
                                     .ToDictionary(g => g.Key, g => g.Count()),
                DuplicateCount = FindDuplicateSources(sources).Count,
                ValidationIssues = ValidateBibliography(sources).Count
            };
        }

        #region Приватные методы

        /// <summary>
        /// Сортирует источники согласно стилю цитирования
        /// </summary>
        private List<Source> SortSources(List<Source> sources, CitationStyle style)
        {
            return style switch
            {
                CitationStyle.GOST => sources.OrderBy(s => s.Author).ThenBy(s => s.Year).ToList(),
                CitationStyle.APA => sources.OrderBy(s => GetLastName(s.Author)).ThenBy(s => s.Year).ToList(),
                CitationStyle.MLA => sources.OrderBy(s => GetLastName(s.Author)).ThenBy(s => s.Title).ToList(),
                CitationStyle.Chicago => sources.OrderBy(s => GetLastName(s.Author)).ThenBy(s => s.Year).ToList(),
                CitationStyle.Harvard => sources.OrderBy(s => GetLastName(s.Author)).ThenBy(s => s.Year).ToList(),
                CitationStyle.Vancouver => sources.OrderBy(s => s.Id).ToList(),
                CitationStyle.IEEE => sources.OrderBy(s => s.Author).ThenBy(s => s.Year).ToList(),
                CitationStyle.Nature => sources.OrderBy(s => s.Author).ThenBy(s => s.Year).ToList(),
                _ => sources.OrderBy(s => s.Author).ThenBy(s => s.Year).ToList()
            };
        }

        /// <summary>
        /// Извлекает фамилию из полного имени автора
        /// </summary>
        private string GetLastName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "";

            var parts = fullName.Trim().Split(' ', (char)StringSplitOptions.RemoveEmptyEntries);
            return parts.LastOrDefault() ?? fullName;
        }

        /// <summary>
        /// Строит поисковый запрос для источника
        /// </summary>
        private string BuildSearchQuery(Source source)
        {
            var queryParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(source.Title))
                queryParts.Add($"title:\"{source.Title}\"");

            if (!string.IsNullOrWhiteSpace(source.Author))
                queryParts.Add($"author:\"{source.Author}\"");

            if (source.Year.HasValue)
                queryParts.Add($"year:{source.Year}");

            if (!string.IsNullOrWhiteSpace(source.DOI))
                queryParts.Add($"doi:\"{source.DOI}\"");

            return string.Join(" AND ", queryParts);
        }

        /// <summary>
        /// Объединяет данные источника с обогащенными данными
        /// </summary>
        private Source MergeSourceData(Source original, string enrichmentData)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(enrichmentData))
                    return original;

                // Используем Newtonsoft.Json для надежного парсинга
                var enrichmentJson = JObject.Parse(enrichmentData);
                var enriched = original.Clone(); // Предполагаем, что у Source есть метод Clone

                // Обогащаем недостающие поля
                if (string.IsNullOrWhiteSpace(enriched.Publisher))
                    enriched.Publisher = enrichmentJson["publisher"]?.ToString();

                if (string.IsNullOrWhiteSpace(enriched.DOI))
                    enriched.DOI = enrichmentJson["doi"]?.ToString();

                if (string.IsNullOrWhiteSpace(enriched.City))
                    enriched.City = enrichmentJson["city"]?.ToString();

                if (string.IsNullOrWhiteSpace(enriched.Volume))
                    enriched.Volume = enrichmentJson["volume"]?.ToString();

                if (string.IsNullOrWhiteSpace(enriched.Issue))
                    enriched.Issue = enrichmentJson["issue"]?.ToString();

                if (string.IsNullOrWhiteSpace(enriched.Pages))
                    enriched.Pages = enrichmentJson["pages"]?.ToString();

                if (string.IsNullOrWhiteSpace(enriched.Journal))
                    enriched.Journal = enrichmentJson["journal"]?.ToString();

                if (!enriched.Year.HasValue)
                {
                    if (int.TryParse(enrichmentJson["year"]?.ToString(), out int year))
                        enriched.Year = year;
                }

                if (string.IsNullOrWhiteSpace(enriched.Url))
                    enriched.Url = enrichmentJson["url"]?.ToString();

                enriched.IsComplete = ValidateSourceCompleteness(enriched);
                return enriched;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Ошибка парсинга JSON при обогащении источника: {ex.Message}");
                return original;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка при объединении данных источника: {ex.Message}");
                return original;
            }
        }

        /// <summary>
        /// Форматирует запись библиографии согласно стилю
        /// </summary>
        private string FormatBibliographyEntry(Source source, CitationStyle style, int number)
        {
            var baseFormat = source.GetFormattedReference(style);

            return style switch
            {
                CitationStyle.GOST => $"{number}. {baseFormat}",
                CitationStyle.Vancouver => $"{number}. {baseFormat}",
                CitationStyle.IEEE => $"[{number}] {baseFormat}",
                CitationStyle.Nature => $"{number}. {baseFormat}",
                _ => baseFormat
            };
        }

        /// <summary>
        /// Проверяет источник на наличие проблем валидации
        /// </summary>
        private bool HasValidationIssues(Source source)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(source.Title))
                return true;

            if (string.IsNullOrWhiteSpace(source.Author))
                return true;

            // Проверка специфичных для типа полей
            return source.Type switch
            {
                SourceType.Book => string.IsNullOrWhiteSpace(source.Publisher) || !source.Year.HasValue,
                SourceType.Journal => string.IsNullOrWhiteSpace(source.Journal) ||
                                    string.IsNullOrWhiteSpace(source.Volume) || !source.Year.HasValue,
                SourceType.Website => string.IsNullOrWhiteSpace(source.Url) || !source.AccessDate.HasValue,
                SourceType.Conference => string.IsNullOrWhiteSpace(source.ConferenceName) || !source.Year.HasValue,
                SourceType.Thesis => string.IsNullOrWhiteSpace(source.Institution) || !source.Year.HasValue,
                SourceType.Report => string.IsNullOrWhiteSpace(source.Institution) || !source.Year.HasValue,
                _ => !source.Year.HasValue
            };
        }

        /// <summary>
        /// Проверяет полноту источника
        /// </summary>
        private bool ValidateSourceCompleteness(Source source)
        {
            return !HasValidationIssues(source);
        }

        /// <summary>
        /// Генерирует уникальный ключ для источника
        /// </summary>
        private string GenerateSourceKey(Source source)
        {
            var keyParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(source.Title))
                keyParts.Add(source.Title.ToLowerInvariant().Trim());

            if (!string.IsNullOrWhiteSpace(source.Author))
                keyParts.Add(source.Author.ToLowerInvariant().Trim());

            if (source.Year.HasValue)
                keyParts.Add(source.Year.ToString());

            if (!string.IsNullOrWhiteSpace(source.DOI))
                keyParts.Add(source.DOI.ToLowerInvariant().Trim());

            return string.Join("|", keyParts);
        }

        #endregion
    }

    /// <summary>
    /// Статистика библиографии
    /// </summary>
    public class BibliographyStatistics
    {
        public int TotalSources { get; set; }
        public int CompleteSources { get; set; }
        public int IncompleteSources { get; set; }
        public Dictionary<SourceType, int> SourcesByType { get; set; } = new();
        public int DuplicateCount { get; set; }
        public int ValidationIssues { get; set; }
    }
}
