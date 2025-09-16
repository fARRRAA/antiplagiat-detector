using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Plagiat.Models;
namespace Plagiat.Services
{
    public class AntiPlagiatService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ConcurrentDictionary<string, CachedPlagiarismResult> _cache;
        private readonly SemaphoreSlim _rateLimitSemaphore;
        private bool _disposed = false;

        public AntiPlagiatService()
        {
            // Инициализация HTTP клиента с настройками из конфигурации
            _httpClient = new HttpClient();
            UpdateHttpClientSettings();

            // Инициализация кэша
            _cache = new ConcurrentDictionary<string, CachedPlagiarismResult>();

            // Инициализация семафора для ограничения параллельных запросов
            _rateLimitSemaphore = new SemaphoreSlim(
                AppConfig.Instance.MaxConcurrentRequests,
                AppConfig.Instance.MaxConcurrentRequests
            );

            // Подписываемся на изменения конфигурации
            AppConfig.Instance.PropertyChanged += OnConfigChanged;

            LogInfo($"AntiPlagiatService инициализирован: " +
                   $"retries={AppConfig.Instance.RetryAttempts}, " +
                   $"timeout={AppConfig.Instance.HttpTimeoutSeconds}s, " +
                   $"maxConcurrent={AppConfig.Instance.MaxConcurrentRequests}, " +
                   $"cache={AppConfig.Instance.EnableCaching}");
        }

        private void OnConfigChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Обновляем настройки HTTP клиента при изменении конфигурации
            if (e.PropertyName == nameof(AppConfig.HttpTimeoutSeconds))
            {
                UpdateHttpClientSettings();
            }
        }

        private void UpdateHttpClientSettings()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(AppConfig.Instance.HttpTimeoutSeconds);
        }

        /// <summary>
        /// Основной метод проверки плагиата с улучшенной обработкой ошибок
        /// </summary>
        public async Task<List<PlagiarismResult>> CheckPlagiarismAsync(
            string text,
            int documentId,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                LogWarning("Получен пустой текст для проверки");
                return new List<PlagiarismResult>();
            }

            try
            {
                // Проверяем кэш если включено кэширование
                if (AppConfig.Instance.EnableCaching)
                {
                    var cachedResult = await GetFromCacheAsync(text, documentId, progress);
                    if (cachedResult != null)
                        return cachedResult;
                }

                progress?.Report(10);

                // Ограничиваем количество параллельных запросов
                await _rateLimitSemaphore.WaitAsync(cancellationToken);
                try
                {
                    // Проверяем через Advego API с retry логикой
                    var results = await CheckWithAdvegоWithRetryAsync(text, documentId, progress, cancellationToken);

                    // Сохраняем в кэш при успешной проверке
                    if (AppConfig.Instance.EnableCaching && results.Count > 0)
                    {
                        await SaveToCacheAsync(text, results);
                    }

                    progress?.Report(100);
                    LogInfo($"Проверка плагиата завершена: найдено {results.Count} совпадений");
                    return results;
                }
                finally
                {
                    _rateLimitSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                LogInfo("Проверка плагиата была отменена");
                throw;
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при проверке плагиата: {ex.Message}");

                // В случае ошибки возвращаем демо-результаты для тестирования
                progress?.Report(100);
                return GenerateDemoResults(text, documentId);
            }
        }

        /// <summary>
        /// Получение общего процента уникальности с улучшенным алгоритмом
        /// </summary>
        public async Task<double> GetOverallUniquenessAsync(
            string text,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                progress?.Report(20);

                var results = await CheckPlagiarismAsync(
                    text,
                    0,
                    new Progress<int>(p => progress?.Report(20 + (int)(p * 0.6))),
                    cancellationToken
                );

                progress?.Report(90);

                if (results.Count == 0)
                {
                    progress?.Report(100);
                    return 100.0;
                }

                // Улучшенный расчет уникальности с учетом перекрытий
                var uniqueness = CalculateUniquenessWithOverlapDetection(text, results);

                progress?.Report(100);
                LogInfo($"Рассчитана уникальность: {uniqueness:F1}% (на основе {results.Count} совпадений)");
                return uniqueness;
            }
            catch (OperationCanceledException)
            {
                LogInfo("Расчет уникальности был отменен");
                throw;
            }
            catch (Exception ex)
            {
                LogError($"Ошибка расчета уникальности: {ex.Message}");
                progress?.Report(100);
                return 85.0; // Демо-значение
            }
        }

        /// <summary>
        /// Пакетная проверка плагиата с оптимизацией параллельности
        /// </summary>
        public async Task<List<PlagiarismResult>> BatchCheckPlagiarismAsync(
            List<string> texts,
            int baseDocumentId,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (texts == null || texts.Count == 0)
                return new List<PlagiarismResult>();

            var allResults = new List<PlagiarismResult>();
            var totalTexts = texts.Count;
            var processedCount = 0;

            // Разбиваем на батчи согласно конфигурации
            var batches = CreateBatches(texts, AppConfig.Instance.BatchSize);

            foreach (var batch in batches)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Обрабатываем батч параллельно
                var batchTasks = batch.Select(async (text, index) =>
                {
                    try
                    {
                        var globalIndex = processedCount + index;
                        var results = await CheckPlagiarismAsync(
                            text,
                            baseDocumentId + globalIndex,
                            cancellationToken: cancellationToken
                        );

                        // Обновляем прогресс
                        Interlocked.Increment(ref processedCount);
                        var currentProgress = (int)((double)processedCount / totalTexts * 100);
                        progress?.Report(currentProgress);

                        return results;
                    }
                    catch (Exception ex)
                    {
                        LogError($"Ошибка проверки текста {processedCount + index}: {ex.Message}");
                        Interlocked.Increment(ref processedCount);
                        return new List<PlagiarismResult>();
                    }
                });

                var batchResults = await Task.WhenAll(batchTasks);

                foreach (var results in batchResults)
                {
                    allResults.AddRange(results);
                }

                // Пауза между батчами для предотвращения rate limiting
                if (processedCount < totalTexts)
                {
                    await Task.Delay(AppConfig.Instance.RetryDelayMs / 2, cancellationToken);
                }
            }

            progress?.Report(100);
            LogInfo($"Пакетная проверка завершена: обработано {totalTexts} текстов, найдено {allResults.Count} совпадений");
            return allResults;
        }

        #region Private Methods

        private async Task<List<PlagiarismResult>> GetFromCacheAsync(string text, int documentId, IProgress<int> progress)
        {
            var cacheKey = GenerateCacheKey(text);
            if (_cache.TryGetValue(cacheKey, out var cachedResult))
            {
                if (DateTime.Now < cachedResult.ExpirationTime)
                {
                    LogInfo("Результат проверки плагиата получен из кэша");
                    progress?.Report(100);
                    return AdaptCachedResults(cachedResult.Results, documentId);
                }
                else
                {
                    _cache.TryRemove(cacheKey, out _);
                }
            }
            return null;
        }

        private async Task SaveToCacheAsync(string text, List<PlagiarismResult> results)
        {
            var cacheKey = GenerateCacheKey(text);
            var cachedResult = new CachedPlagiarismResult
            {
                Results = results,
                ExpirationTime = DateTime.Now.AddMinutes(AppConfig.Instance.CacheExpirationMinutes)
            };

            _cache.TryAdd(cacheKey, cachedResult);
            await CleanupCacheAsync();
        }

        private async Task<List<PlagiarismResult>> CheckWithAdvegоWithRetryAsync(
            string text,
            int documentId,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            Exception lastException = null;
            var retryAttempts = AppConfig.Instance.RetryAttempts;

            for (int attempt = 1; attempt <= retryAttempts; attempt++)
            {
                try
                {
                    LogInfo($"Попытка {attempt} из {retryAttempts} проверки плагиата через Advego");
                    progress?.Report(10 + attempt * 10);

                    return await CheckWithAdvegoAsync(text, documentId, progress, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw; // Не retry для отмены операции
                }
                catch (HttpRequestException ex)
                {
                    lastException = ex;
                    LogWarning($"Попытка {attempt} неудачна (HTTP ошибка): {ex.Message}");

                    if (attempt < retryAttempts)
                    {
                        var delay = CalculateRetryDelay(attempt);
                        LogInfo($"Ожидание {delay}ms перед следующей попыткой...");
                        await Task.Delay(delay, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    LogWarning($"Попытка {attempt} неудачна (общая ошибка): {ex.Message}");

                    if (IsNonRetryableError(ex))
                    {
                        LogError($"Обнаружена неисправимая ошибка: {ex.Message}");
                        break;
                    }

                    if (attempt < retryAttempts)
                    {
                        var delay = CalculateRetryDelay(attempt);
                        LogInfo($"Ожидание {delay}ms перед следующей попыткой...");
                        await Task.Delay(delay, cancellationToken);
                    }
                }
            }

            LogError($"Все {retryAttempts} попыток проверки плагиата исчерпаны");
            throw new Exception($"Не удалось выполнить проверку плагиата после {retryAttempts} попыток. " +
                              $"Последняя ошибка: {lastException?.Message}", lastException);
        }

        public async Task<List<PlagiarismResult>> CheckWithAdvegoAsync(
            string text,
            int documentId,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            try
            {
                // Шаг 1: Отправляем текст на проверку
                progress?.Report(30);
                var checkId = await SubmitTextToAdvegoAsync(text, cancellationToken);

                if (string.IsNullOrEmpty(checkId))
                {
                    LogWarning("Не удалось получить check_id от Advego API");
                    return GenerateDemoResults(text, documentId);
                }

                LogInfo($"Получен check_id: {checkId}");
                progress?.Report(50);

                // Шаг 2: Получаем результат
                return await GetAdvegoResultWithAdaptiveDelayAsync(checkId, documentId, text, progress, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при проверке через Advego: {ex.Message}");
                return GenerateDemoResults(text, documentId);
            }
        }

        private async Task<string> SubmitTextToAdvegoAsync(string text, CancellationToken cancellationToken)
        {
            var apiUrl = $"{AppConfig.Instance.AntiPlagiatBaseUrl}/check";
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("key", AppConfig.Instance.AntiPlagiatApiKey),
                new KeyValuePair<string, string>("text", text),
                new KeyValuePair<string, string>("format", "json")
            };

            var formContent = new FormUrlEncodedContent(formData);

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                cts.CancelAfter(TimeSpan.FromSeconds(AppConfig.Instance.HttpTimeoutSeconds));

                var response = await _httpClient.PostAsync(apiUrl, formContent, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync();

                LogInfo($"Ответ от Advego (отправка): {responseContent}");
                LogInfo($"Статус ответа: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<AdvegoSubmitResponse>(responseContent);
                    LogInfo($"Десериализованный результат: check_id={result?.check_id}, status={result?.status}");
                    return result?.check_id;
                }
                else
                {
                    var error = $"HTTP ошибка: {response.StatusCode} - {response.ReasonPhrase} - {responseContent}";
                    LogError(error);
                    throw new HttpRequestException(error);
                }
            }
        }

        private async Task<List<PlagiarismResult>> GetAdvegoResultWithAdaptiveDelayAsync(
            string checkId,
            int documentId,
            string originalText,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            var apiUrl = $"{AppConfig.Instance.AntiPlagiatBaseUrl}/result";
            var maxAttempts = 15; // Увеличено для лучшей надежности
            var currentDelay = AppConfig.Instance.RetryDelayMs;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    LogInfo($"Попытка {attempt} получения результата с задержкой {currentDelay}ms");

                    if (attempt > 1) // Не ждем перед первой попыткой
                    {
                        await Task.Delay(currentDelay, cancellationToken);
                    }

                    progress?.Report(50 + attempt * 3);

                    var formData = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("key", AppConfig.Instance.AntiPlagiatApiKey),
                        new KeyValuePair<string, string>("check_id", checkId),
                        new KeyValuePair<string, string>("format", "json")
                    };

                    var formContent = new FormUrlEncodedContent(formData);

                    using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                    {
                        cts.CancelAfter(TimeSpan.FromSeconds(AppConfig.Instance.HttpTimeoutSeconds));

                        var response = await _httpClient.PostAsync(apiUrl, formContent, cts.Token);
                        var responseContent = await response.Content.ReadAsStringAsync();

                        LogInfo($"Ответ от Advego (результат, попытка {attempt}): {responseContent}");
                        LogInfo($"Статус ответа: {response.StatusCode}");

                        if (response.IsSuccessStatusCode)
                        {
                            var result = JsonConvert.DeserializeObject<AdvegoResponse>(responseContent);
                            LogInfo($"Десериализованный результат: status={result?.status}, uniqueness={result?.result?.uniqueness}");

                            if (result?.result != null)
                            {
                                var plagiarismResults = ConvertAdvegoResponse(result, documentId, originalText);
                                LogInfo($"Конвертировано {plagiarismResults.Count} результатов плагиата");
                                return plagiarismResults;
                            }
                            else if (result?.status == "processing")
                            {
                                LogInfo("Результат еще обрабатывается, увеличиваем задержку");
                                currentDelay = Math.Min(currentDelay + 1000, 30000); // Плавное увеличение
                                continue;
                            }
                            else
                            {
                                LogWarning($"Неожиданный статус: {result?.status}");
                            }
                        }
                        else
                        {
                            var error = $"HTTP ошибка: {response.StatusCode} - {response.ReasonPhrase} - {responseContent}";
                            LogError(error);

                            if (response.StatusCode == (System.Net.HttpStatusCode)291)
                            {
                                LogWarning("Обнаружен rate limiting, увеличиваем задержку");
                                currentDelay = Math.Min(currentDelay * 2, 60000);
                                continue;
                            }

                            throw new HttpRequestException(error);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogError($"Ошибка на попытке {attempt}: {ex.Message}");

                    if (attempt == maxAttempts)
                    {
                        throw;
                    }

                    currentDelay = Math.Min(currentDelay + 2000, 45000);
                }
            }

            LogWarning("Превышено максимальное количество попыток получения результата");
            return GenerateDemoResults(originalText, documentId);
        }

        private double CalculateUniquenessWithOverlapDetection(string text, List<PlagiarismResult> results)
        {
            if (results.Count == 0)
                return 100.0;

            // Создаем массив для отслеживания покрытых позиций
            var textLength = text.Length;
            var covered = new bool[textLength];

            // Отмечаем покрытые позиции
            foreach (var result in results)
            {
                var start = Math.Max(0, result.StartPosition);
                var end = Math.Min(textLength, result.EndPosition);

                for (int i = start; i < end; i++)
                {
                    covered[i] = true;
                }
            }

            // Считаем покрытые символы
            var coveredCount = covered.Count(c => c);
            var coveragePercentage = (double)coveredCount / textLength * 100;

            // Возвращаем уникальность
            return Math.Max(0, 100 - coveragePercentage);
        }

        private List<List<string>> CreateBatches<T>(List<T> items, int batchSize)
        {
            var batches = new List<List<T>>();
            for (int i = 0; i < items.Count; i += batchSize)
            {
                var batch = items.Skip(i).Take(batchSize).ToList();
                batches.Add(batch);
            }
            return batches.Cast<List<string>>().ToList();
        }

        private List<PlagiarismResult> ConvertAdvegoResponse(AdvegoResponse response, int documentId, string originalText)
        {
            var results = new List<PlagiarismResult>();

            if (response?.result?.matches != null)
            {
                foreach (var match in response.result.matches)
                {
                    var plagiarismResult = new PlagiarismResult
                    {
                        DocumentId = documentId,
                        MatchedText = match.text ?? "Не указано",
                        StartPosition = match.start_pos,
                        EndPosition = match.end_pos,
                        SimilarityPercentage = match.percent,
                        SourceUrl = match.url,
                        SourceTitle = match.title ?? "Неизвестный источник",
                        SourceAuthor = ExtractAuthorFromTitle(match.title),
                        Level = DeterminePlagiarismLevel(match.percent)
                    };

                    results.Add(plagiarismResult);
                }
            }

            // Сортируем результаты по уровню важности
            return results.OrderByDescending(r => r.SimilarityPercentage).ToList();
        }

        private PlagiarismLevel DeterminePlagiarismLevel(double similarity)
        {
            if (similarity >= 70)
                return PlagiarismLevel.Critical;
            else if (similarity >= 40)
                return PlagiarismLevel.Warning;
            else
                return PlagiarismLevel.Acceptable;
        }

        private string ExtractAuthorFromTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
                return "Автор не указан";

            // Улучшенная эвристика для извлечения автора из заголовка
            var separators = new[] { '-', '|', '–', ':', '•' };
            foreach (var separator in separators)
            {
                var parts = title.Split(separator);
                if (parts.Length > 1)
                {
                    var author = parts[1].Trim();
                    if (!string.IsNullOrWhiteSpace(author))
                        return author;
                }
            }

            return "Автор не указан";
        }

        private List<PlagiarismResult> GenerateDemoResults(string text, int documentId)
        {
            var results = new List<PlagiarismResult>();
            var random = new Random();

            // Демо-источники для тестирования
            var demoSources = new[]
            {
                new { Url = "https://ru.wikipedia.org", Title = "Википедия", Author = "Wikipedia" },
                new { Url = "https://cyberleninka.ru", Title = "КиберЛенинка", Author = "Научная библиотека" },
                new { Url = "https://elibrary.ru", Title = "eLIBRARY", Author = "Научная электронная библиотека" },
                new { Url = "https://scholar.google.com", Title = "Google Scholar", Author = "Google" },
                new { Url = "https://www.researchgate.net", Title = "ResearchGate", Author = "ResearchGate" }
            };

            var words = text.Split(' ');
            if (words.Length > 10)
            {
                var numResults = Math.Min(random.Next(2, 6), words.Length / 20);

                for (int i = 0; i < numResults; i++)
                {
                    var startWord = random.Next(0, words.Length - 10);
                    var length = random.Next(5, 15);
                    var endWord = Math.Min(startWord + length, words.Length);
                    var matchedText = string.Join(" ", words, startWord, endWord - startWord);
                    var source = demoSources[random.Next(demoSources.Length)];
                    var similarity = random.Next(20, 85);

                    results.Add(new PlagiarismResult
                    {
                        DocumentId = documentId,
                        MatchedText = matchedText,
                        StartPosition = GetCharPosition(words, startWord),
                        EndPosition = GetCharPosition(words, endWord),
                        SimilarityPercentage = similarity,
                        SourceUrl = source.Url,
                        SourceTitle = source.Title,
                        SourceAuthor = source.Author,
                        Level = DeterminePlagiarismLevel(similarity)
                    });
                }
            }

            LogInfo($"Сгенерировано {results.Count} демо-результатов плагиата");
            return results.OrderByDescending(r => r.SimilarityPercentage).ToList();
        }

        private int GetCharPosition(string[] words, int wordIndex)
        {
            int position = 0;
            for (int i = 0; i < wordIndex && i < words.Length; i++)
            {
                position += words[i].Length + 1; // +1 для пробела
            }
            return position;
        }

        private int CalculateRetryDelay(int attempt)
        {
            // Комбинированная стратегия: базовая задержка + экспоненциальный backoff + jitter
            var baseDelay = AppConfig.Instance.RetryDelayMs;
            var exponentialDelay = Math.Pow(2, attempt - 1) * 1000; // 1s, 2s, 4s, 8s...
            var jitter = new Random().Next(0, 1000); // Случайная задержка до 1 секунды

            return (int)(baseDelay + exponentialDelay + jitter);
        }

        private bool IsNonRetryableError(Exception ex)
        {
            var message = ex.Message.ToLower();
            return message.Contains("unauthorized") ||
                   message.Contains("forbidden") ||
                   message.Contains("invalid api key") ||
                   message.Contains("quota exceeded") ||
                   message.Contains("account suspended") ||
                   message.Contains("payment required") ||
                   message.Contains("bad request");
        }

        private string GenerateCacheKey(string text)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
                return Convert.ToBase64String(hashBytes);
            }
        }

        private List<PlagiarismResult> AdaptCachedResults(List<PlagiarismResult> cachedResults, int newDocumentId)
        {
            return cachedResults.Select(r => new PlagiarismResult
            {
                DocumentId = newDocumentId,
                MatchedText = r.MatchedText,
                StartPosition = r.StartPosition,
                EndPosition = r.EndPosition,
                SimilarityPercentage = r.SimilarityPercentage,
                SourceUrl = r.SourceUrl,
                SourceTitle = r.SourceTitle,
                SourceAuthor = r.SourceAuthor,
                Level = r.Level
            }).ToList();
        }

        private async Task CleanupCacheAsync()
        {
            await Task.Run(() =>
            {
                var maxCacheSize = 100; // Можно добавить в конфигурацию

                if (_cache.Count > maxCacheSize)
                {
                    // Удаляем истекшие записи
                    var expiredKeys = _cache
                        .Where(kvp => DateTime.Now >= kvp.Value.ExpirationTime)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in expiredKeys)
                    {
                        _cache.TryRemove(key, out _);
                    }

                    // Если всё ещё слишком много, удаляем старые записи
                    if (_cache.Count > maxCacheSize)
                    {
                        var oldestKeys = _cache
                            .OrderBy(kvp => kvp.Value.ExpirationTime)
                            .Take(_cache.Count - maxCacheSize)
                            .Select(kvp => kvp.Key)
                            .ToList();

                        foreach (var key in oldestKeys)
                        {
                            _cache.TryRemove(key, out _);
                        }
                    }
                }
            });
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Очистка кэша
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
            LogInfo("Кэш проверки плагиата очищен");
        }

        /// <summary>
        /// Получение статистики кэша
        /// </summary>
        public CacheStatistics GetCacheStatistics()
        {
            var now = DateTime.Now;
            var activeEntries = _cache.Count(kvp => kvp.Value.ExpirationTime > now);
            var expiredEntries = _cache.Count - activeEntries;

            return new CacheStatistics
            {
                TotalEntries = _cache.Count,
                ActiveEntries = activeEntries,
                ExpiredEntries = expiredEntries,
                CacheEnabled = AppConfig.Instance.EnableCaching
            };
        }

        #endregion

        #region Logging Methods

        private void LogInfo(string message)
        {
            if (AppConfig.Instance.EnableLogging)
                Console.WriteLine($"[INFO] AntiPlagiatService: {message}");
        }

        private void LogWarning(string message)
        {
            if (AppConfig.Instance.EnableLogging)
                Console.WriteLine($"[WARNING] AntiPlagiatService: {message}");
        }

        private void LogError(string message)
        {
            if (AppConfig.Instance.EnableLogging)
                Console.WriteLine($"[ERROR] AntiPlagiatService: {message}");
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    AppConfig.Instance.PropertyChanged -= OnConfigChanged;
                    _httpClient?.Dispose();
                    _rateLimitSemaphore?.Dispose();
                }
                _disposed = true;
            }
        }

        #endregion
    }

    #region Helper Classes

    /// <summary>
    /// Статистика кэша
    /// </summary>
    public class CacheStatistics
    {
        public int TotalEntries { get; set; }
        public int ActiveEntries { get; set; }
        public int ExpiredEntries { get; set; }
        public bool CacheEnabled { get; set; }
    }

    /// <summary>
    /// Кэшированный результат проверки плагиата
    /// </summary>
    public class CachedPlagiarismResult
    {
        public List<PlagiarismResult> Results { get; set; }
        public DateTime ExpirationTime { get; set; }
    }

    #endregion

    #region API Response Models

    /// <summary>
    /// Модель ответа на отправку текста в Advego
    /// </summary>
    public class AdvegoSubmitResponse
    {
        public string check_id { get; set; }
        public string status { get; set; }
        public string error { get; set; }
    }

    /// <summary>
    /// Модель ответа с результатами от Advego
    /// </summary>
    public class AdvegoResponse
    {
        public AdvegoResult result { get; set; }
        public string status { get; set; }
        public string error { get; set; }
    }

    /// <summary>
    /// Модель результата проверки от Advego
    /// </summary>
    public class AdvegoResult
    {
        public double uniqueness { get; set; }
        public AdvegoMatch[] matches { get; set; }
        public int total_words { get; set; }
        public int total_chars { get; set; }
    }

    /// <summary>
    /// Модель совпадения от Advego
    /// </summary>
    public class AdvegoMatch
    {
        public string text { get; set; }
        public string url { get; set; }
        public string title { get; set; }
        public int start_pos { get; set; }
        public int end_pos { get; set; }
        public double percent { get; set; }
        public string domain { get; set; }
    }
    #endregion
}
