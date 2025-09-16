using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Plagiat
{
    public static class ConfigManager
    {
        private static readonly string ConfigDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "АнтиплагиатПомощник"
        );

        private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");
        private static readonly string SecureConfigFilePath = Path.Combine(ConfigDirectory, "secure.json");

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>
        /// Загрузка конфигурации при запуске приложения
        /// </summary>
        public static async Task LoadConfigurationAsync()
        {
            try
            {
                EnsureConfigDirectoryExists();

                // Загружаем основную конфигурацию
                await LoadMainConfigAsync();

                // Загружаем защищенную конфигурацию (API ключи)
                await LoadSecureConfigAsync();

                Console.WriteLine("Конфигурация успешно загружена");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке конфигурации: {ex.Message}");
                // При ошибке используем конфигурацию по умолчанию
            }
        }

        /// <summary>
        /// Сохранение конфигурации
        /// </summary>
        public static async Task SaveConfigurationAsync()
        {
            try
            {
                EnsureConfigDirectoryExists();

                await SaveMainConfigAsync();
                await SaveSecureConfigAsync();

                Console.WriteLine("Конфигурация успешно сохранена");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении конфигурации: {ex.Message}");
                throw;
            }
        }

        private static void EnsureConfigDirectoryExists()
        {
            if (!Directory.Exists(ConfigDirectory))
            {
                Directory.CreateDirectory(ConfigDirectory);
            }
        }

        private static async Task LoadMainConfigAsync()
        {
            if (!File.Exists(ConfigFilePath))
                return;

            var json = File.ReadAllText(ConfigFilePath);
            var configData = JsonConvert.DeserializeObject<ConfigData>(json, JsonSettings);

            if (configData != null)
            {
                ApplyMainConfig(configData);
            }
        }

        private static async Task LoadSecureConfigAsync()
        {
            if (!File.Exists(SecureConfigFilePath))
                return;

            var encryptedJson = File.ReadAllText(SecureConfigFilePath);
            var decryptedJson = DecryptString(encryptedJson);
            var secureData = JsonConvert.DeserializeObject<SecureConfigData>(decryptedJson, JsonSettings);

            if (secureData != null)
            {
                ApplySecureConfig(secureData);
            }
        }

        private static async Task SaveMainConfigAsync()
        {
            var configData = new ConfigData
            {
                OpenRouterBaseUrl = AppConfig.Instance.OpenRouterBaseUrl,
                AntiPlagiatBaseUrl = AppConfig.Instance.AntiPlagiatBaseUrl,
                HttpTimeoutSeconds = AppConfig.Instance.HttpTimeoutSeconds,
                RetryAttempts = AppConfig.Instance.RetryAttempts,
                RetryDelayMs = AppConfig.Instance.RetryDelayMs,
                EnableCaching = AppConfig.Instance.EnableCaching,
                CacheExpirationMinutes = AppConfig.Instance.CacheExpirationMinutes,
                DefaultModel = AppConfig.Instance.DefaultModel,
                Temperature = AppConfig.Instance.Temperature,
                MaxTokens = AppConfig.Instance.MaxTokens,
                BatchSize = AppConfig.Instance.BatchSize,
                MaxConcurrentRequests = AppConfig.Instance.MaxConcurrentRequests,
                AutoSave = AppConfig.Instance.AutoSave,
                DefaultCitationStyle = AppConfig.Instance.DefaultCitationStyle,
                EnableLogging = AppConfig.Instance.EnableLogging
            };

            var json = JsonConvert.SerializeObject(configData, JsonSettings);
            File.WriteAllText(ConfigFilePath, json);
        }

        private static async Task SaveSecureConfigAsync()
        {
            var secureData = new SecureConfigData
            {
                OpenRouterApiKey = AppConfig.Instance.OpenRouterApiKey,
                AntiPlagiatApiKey = AppConfig.Instance.AntiPlagiatApiKey
            };

            var json = JsonConvert.SerializeObject(secureData, JsonSettings);
            var encryptedJson = EncryptString(json);
            File.WriteAllText(SecureConfigFilePath, encryptedJson);
        }

        private static void ApplyMainConfig(ConfigData config)
        {
            AppConfig.Instance.OpenRouterBaseUrl = config.OpenRouterBaseUrl ?? AppConfig.Instance.OpenRouterBaseUrl;
            AppConfig.Instance.AntiPlagiatBaseUrl = config.AntiPlagiatBaseUrl ?? AppConfig.Instance.AntiPlagiatBaseUrl;
            AppConfig.Instance.HttpTimeoutSeconds = config.HttpTimeoutSeconds;
            AppConfig.Instance.RetryAttempts = config.RetryAttempts;
            AppConfig.Instance.RetryDelayMs = config.RetryDelayMs;
            AppConfig.Instance.EnableCaching = config.EnableCaching;
            AppConfig.Instance.CacheExpirationMinutes = config.CacheExpirationMinutes;
            AppConfig.Instance.DefaultModel = config.DefaultModel ?? AppConfig.Instance.DefaultModel;
            AppConfig.Instance.Temperature = config.Temperature;
            AppConfig.Instance.MaxTokens = config.MaxTokens;
            AppConfig.Instance.BatchSize = config.BatchSize;
            AppConfig.Instance.MaxConcurrentRequests = config.MaxConcurrentRequests;
            AppConfig.Instance.AutoSave = config.AutoSave;
            AppConfig.Instance.DefaultCitationStyle = config.DefaultCitationStyle ?? AppConfig.Instance.DefaultCitationStyle;
            AppConfig.Instance.EnableLogging = config.EnableLogging;
        }

        private static void ApplySecureConfig(SecureConfigData config)
        {
            AppConfig.Instance.OpenRouterApiKey = config.OpenRouterApiKey ?? "";
            AppConfig.Instance.AntiPlagiatApiKey = config.AntiPlagiatApiKey ?? "";
        }

        /// <summary>
        /// Простое шифрование для API ключей (базовая защита)
        /// </summary>
        private static string EncryptString(string plainText)
        {
            var data = Encoding.UTF8.GetBytes(plainText);
            var entropy = Encoding.UTF8.GetBytes(Environment.MachineName);

            try
            {
                var encrypted = ProtectedData.Protect(data, entropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encrypted);
            }
            catch
            {
                // Если шифрование не удалось, возвращаем исходный текст в base64
                return Convert.ToBase64String(data);
            }
        }

        /// <summary>
        /// Расшифровка строки
        /// </summary>
        private static string DecryptString(string encryptedText)
        {
            try
            {
                var data = Convert.FromBase64String(encryptedText);
                var entropy = Encoding.UTF8.GetBytes(Environment.MachineName);

                try
                {
                    var decrypted = ProtectedData.Unprotect(data, entropy, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(decrypted);
                }
                catch
                {
                    // Если расшифровка не удалась, пытаемся декодировать как простой base64
                    return Encoding.UTF8.GetString(data);
                }
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Сброс конфигурации к значениям по умолчанию
        /// </summary>
        public static async Task ResetToDefaultsAsync()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                    File.Delete(ConfigFilePath);

                if (File.Exists(SecureConfigFilePath))
                    File.Delete(SecureConfigFilePath);

                // Создаем новый экземпляр конфигурации
                var field = typeof(AppConfig).GetField("_instance",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                field?.SetValue(null, null);

                Console.WriteLine("Конфигурация сброшена к значениям по умолчанию");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сбросе конфигурации: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Получение пути к директории конфигурации
        /// </summary>
        public static string GetConfigDirectory() => ConfigDirectory;

        /// <summary>
        /// Проверка существования файлов конфигурации
        /// </summary>
        public static bool ConfigExists() => File.Exists(ConfigFilePath) || File.Exists(SecureConfigFilePath);
    }

    // Классы для сериализации конфигурации
    internal class ConfigData
    {
        public string OpenRouterBaseUrl { get; set; }
        public string AntiPlagiatBaseUrl { get; set; }
        public int HttpTimeoutSeconds { get; set; }
        public int RetryAttempts { get; set; }
        public int RetryDelayMs { get; set; }
        public bool EnableCaching { get; set; }
        public int CacheExpirationMinutes { get; set; }
        public string DefaultModel { get; set; }
        public double Temperature { get; set; }
        public int MaxTokens { get; set; }
        public int BatchSize { get; set; }
        public int MaxConcurrentRequests { get; set; }
        public bool AutoSave { get; set; }
        public string DefaultCitationStyle { get; set; }
        public bool EnableLogging { get; set; }
    }

    internal class SecureConfigData
    {
        public string OpenRouterApiKey { get; set; }
        public string AntiPlagiatApiKey { get; set; }
    }
}
