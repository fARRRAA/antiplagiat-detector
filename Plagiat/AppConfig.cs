using System;
using System.ComponentModel;
using System.Configuration;

namespace Plagiat
{
    /// <summary>
    /// Класс для хранения всех конфигурационных настроек приложения
    /// </summary>
    public class AppConfig : INotifyPropertyChanged
    {
        private static AppConfig _instance;
        private static readonly object _lock = new object();

        // Singleton реализация
        public static AppConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new AppConfig();
                    }
                }
                return _instance;
            }
        }

        private AppConfig()
        {
            // Устанавливаем значения по умолчанию
            SetDefaultValues();
        }

        #region API Configuration

        private string _openRouterApiKey = "";
        public string OpenRouterApiKey
        {
            get => _openRouterApiKey;
            set
            {
                _openRouterApiKey = value;
                OnPropertyChanged(nameof(OpenRouterApiKey));
            }
        }

        private string _openRouterBaseUrl = "https://openrouter.ai/api/v1";
        public string OpenRouterBaseUrl
        {
            get => _openRouterBaseUrl;
            set
            {
                _openRouterBaseUrl = value;
                OnPropertyChanged(nameof(OpenRouterBaseUrl));
            }
        }

        private string _antiPlagiatApiKey = "";
        public string AntiPlagiatApiKey
        {
            get => _antiPlagiatApiKey;
            set
            {
                _antiPlagiatApiKey = value;
                OnPropertyChanged(nameof(AntiPlagiatApiKey));
            }
        }

        private string _antiPlagiatBaseUrl = "https://api.antiplagiat.ru";
        public string AntiPlagiatBaseUrl
        {
            get => _antiPlagiatBaseUrl;
            set
            {
                _antiPlagiatBaseUrl = value;
                OnPropertyChanged(nameof(AntiPlagiatBaseUrl));
            }
        }

        #endregion

        #region Timeout Settings

        private int _httpTimeoutSeconds = 60;
        public int HttpTimeoutSeconds
        {
            get => _httpTimeoutSeconds;
            set
            {
                _httpTimeoutSeconds = Math.Max(10, Math.Min(300, value)); // 10-300 секунд
                OnPropertyChanged(nameof(HttpTimeoutSeconds));
            }
        }

        private int _retryAttempts = 3;
        public int RetryAttempts
        {
            get => _retryAttempts;
            set
            {
                _retryAttempts = Math.Max(1, Math.Min(10, value)); // 1-10 попыток
                OnPropertyChanged(nameof(RetryAttempts));
            }
        }

        private int _retryDelayMs = 1000;
        public int RetryDelayMs
        {
            get => _retryDelayMs;
            set
            {
                _retryDelayMs = Math.Max(100, Math.Min(10000, value)); // 100мс - 10с
                OnPropertyChanged(nameof(RetryDelayMs));
            }
        }

        #endregion

        #region Cache Settings

        private bool _enableCaching = true;
        public bool EnableCaching
        {
            get => _enableCaching;
            set
            {
                _enableCaching = value;
                OnPropertyChanged(nameof(EnableCaching));
            }
        }

        private int _cacheExpirationMinutes = 30;
        public int CacheExpirationMinutes
        {
            get => _cacheExpirationMinutes;
            set
            {
                _cacheExpirationMinutes = Math.Max(1, Math.Min(1440, value)); // 1 минута - 1 день
                OnPropertyChanged(nameof(CacheExpirationMinutes));
            }
        }

        #endregion

        #region AI Model Settings

        private string _defaultModel = "openai/gpt-3.5-turbo";
        public string DefaultModel
        {
            get => _defaultModel;
            set
            {
                _defaultModel = value;
                OnPropertyChanged(nameof(DefaultModel));
            }
        }

        private double _temperature = 0.7;
        public double Temperature
        {
            get => _temperature;
            set
            {
                _temperature = Math.Max(0.0, Math.Min(2.0, value)); // 0.0 - 2.0
                OnPropertyChanged(nameof(Temperature));
            }
        }

        private int _maxTokens = 2048;
        public int MaxTokens
        {
            get => _maxTokens;
            set
            {
                _maxTokens = Math.Max(100, Math.Min(8192, value)); // 100 - 8192 токенов
                OnPropertyChanged(nameof(MaxTokens));
            }
        }

        #endregion

        #region Batch Processing Settings

        private int _batchSize = 5;
        public int BatchSize
        {
            get => _batchSize;
            set
            {
                _batchSize = Math.Max(1, Math.Min(20, value)); // 1-20 элементов в батче
                OnPropertyChanged(nameof(BatchSize));
            }
        }

        private int _maxConcurrentRequests = 3;
        public int MaxConcurrentRequests
        {
            get => _maxConcurrentRequests;
            set
            {
                _maxConcurrentRequests = Math.Max(1, Math.Min(10, value)); // 1-10 параллельных запросов
                OnPropertyChanged(nameof(MaxConcurrentRequests));
            }
        }

        #endregion

        #region Application Settings

        private bool _autoSave = true;
        public bool AutoSave
        {
            get => _autoSave;
            set
            {
                _autoSave = value;
                OnPropertyChanged(nameof(AutoSave));
            }
        }

        private string _defaultCitationStyle = "ГОСТ";
        public string DefaultCitationStyle
        {
            get => _defaultCitationStyle;
            set
            {
                _defaultCitationStyle = value;
                OnPropertyChanged(nameof(DefaultCitationStyle));
            }
        }

        private bool _enableLogging = true;
        public bool EnableLogging
        {
            get => _enableLogging;
            set
            {
                _enableLogging = value;
                OnPropertyChanged(nameof(EnableLogging));
            }
        }

        #endregion

        private void SetDefaultValues()
        {
            // Загружаем значения из App.config
            LoadFromConfig();
        }
        
        private void LoadFromConfig()
        {
            try
            {
                // OpenRouter настройки
                OpenRouterApiKey = ConfigurationManager.AppSettings["OpenRouter.ApiKey"] ?? "";
                OpenRouterBaseUrl = ConfigurationManager.AppSettings["OpenRouter.BaseUrl"] ?? "https://openrouter.ai/api/v1";
                DefaultModel = ConfigurationManager.AppSettings["OpenRouter.Model"] ?? "deepseek/deepseek-chat-v3.1:free";
                
                if (int.TryParse(ConfigurationManager.AppSettings["OpenRouter.MaxRetries"], out int maxRetries))
                    RetryAttempts = maxRetries;
                    
                if (int.TryParse(ConfigurationManager.AppSettings["OpenRouter.TimeoutSeconds"], out int timeout))
                    HttpTimeoutSeconds = timeout;

                // Advego настройки
                AntiPlagiatApiKey = ConfigurationManager.AppSettings["Advego.ApiKey"] ?? "";
                AntiPlagiatBaseUrl = ConfigurationManager.AppSettings["Advego.BaseUrl"] ?? "https://api.advego.com/plagiatus";
                
                if (int.TryParse(ConfigurationManager.AppSettings["Advego.MaxRetries"], out int advegoRetries))
                    RetryAttempts = advegoRetries;
                    
                if (int.TryParse(ConfigurationManager.AppSettings["Advego.TimeoutSeconds"], out int advegoTimeout))
                    HttpTimeoutSeconds = advegoTimeout;

                // Кэш настройки
                if (bool.TryParse(ConfigurationManager.AppSettings["Cache.Enabled"], out bool cacheEnabled))
                    EnableCaching = cacheEnabled;
                    
                if (int.TryParse(ConfigurationManager.AppSettings["Cache.ExpirationMinutes"], out int cacheExpiration))
                    CacheExpirationMinutes = cacheExpiration;

                // Логирование
                if (bool.TryParse(ConfigurationManager.AppSettings["Logging.Enabled"], out bool loggingEnabled))
                    EnableLogging = loggingEnabled;

                // UI настройки
                if (bool.TryParse(ConfigurationManager.AppSettings["UI.AutoSave"], out bool autoSave))
                    AutoSave = autoSave;
            }
            catch (Exception ex)
            {
                // В случае ошибки используем значения по умолчанию
                Console.WriteLine($"Ошибка загрузки конфигурации: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Валидация конфигурации
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(OpenRouterBaseUrl) &&
                   !string.IsNullOrWhiteSpace(AntiPlagiatBaseUrl) &&
                   HttpTimeoutSeconds > 0 &&
                   RetryAttempts > 0 &&
                   MaxTokens > 0;
        }

        /// <summary>
        /// Получение маскированного API ключа для отображения
        /// </summary>
        public string GetMaskedApiKey(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 8)
                return "Не установлен";
            
            return apiKey.Substring(0, 4) + "..." + apiKey.Substring(apiKey.Length - 4);
        }
    }
}