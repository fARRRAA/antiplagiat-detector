using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Plagiat.Data;
using Plagiat.Models;

namespace Plagiat.Services
{
    public class DatabaseInitializer
    {
        public static async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                using (var context = new PlagiatContext())
                {
                    // Проверяем совместимость модели с базой данных
                    if (context.Database.Exists())
                    {
                        try
                        {
                            // Пытаемся проверить совместимость
                            context.Database.CompatibleWithModel(true);
                            Console.WriteLine("База данных совместима с моделью.");
                        }
                        catch (InvalidOperationException)
                        {
                            Console.WriteLine("Модель изменилась. Пересоздаем базу данных...");
                            context.Database.Delete();
                            context.Database.Create();
                            Console.WriteLine("База данных пересоздана успешно!");
                        }
                    }
                    else
                    {
                        Console.WriteLine("База данных не существует. Создаем...");
                        context.Database.Create();
                        Console.WriteLine("База данных создана успешно!");
                    }

                    // Проверяем подключение
                    await context.Database.Connection.OpenAsync();
                    Console.WriteLine("Подключение к базе данных установлено!");
                    
                    // Создаем демо-данные если база пустая
                    await SeedDemoDataAsync(context);
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка инициализации базы данных: {ex.Message}");
                return false;
            }
        }

        private static async Task SeedDemoDataAsync(PlagiatContext context)
        {
            try
            {
                // Проверяем есть ли уже данные
                if (await context.Projects.AnyAsync())
                {
                    Console.WriteLine("Демо-данные уже существуют.");
                    return;
                }

                Console.WriteLine("Создаем демо-данные...");

                // Создаем демо-проект
                var demoProject = new Project
                {
                    Name = "Демо проект",
                    Description = "Проект для демонстрации возможностей приложения",
                    CreatedAt = DateTime.Now,
                    Status = ProjectStatus.Active,
                    DefaultCitationStyle = CitationStyle.GOST
                };

                context.Projects.Add(demoProject);

                // Создаем демо-источники
                var sources = new[]
                {
                    new Source
                    {
                        Title = "Основы информационных технологий",
                        Author = "Иванов И.И.",
                        Publisher = "Наука",
                        Year = 2023,
                        Type = SourceType.Book,
                        IsComplete = true
                    },
                    new Source
                    {
                        Title = "Современные методы анализа текста",
                        Author = "Петров П.П.",
                        Publisher = "Техника", 
                        Year = 2022,
                        Type = SourceType.Book,
                        IsComplete = true
                    },
                    new Source
                    {
                        Title = "Искусственный интеллект в образовании",
                        Author = "Сидоров С.С.",
                        Publisher = "Образование",
                        Year = 2024,
                        Type = SourceType.Article,
                        IsComplete = true
                    }
                };

                context.Sources.AddRange(sources);

                // Сохраняем изменения
                await context.SaveChangesAsync();
                
                Console.WriteLine("Демо-данные созданы успешно!");
                Console.WriteLine($"- Создан проект: {demoProject.Name}");
                Console.WriteLine($"- Добавлено источников: {sources.Length}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания демо-данных: {ex.Message}");
            }
        }

        public static async Task<string> GetDatabaseInfoAsync()
        {
            try
            {
                using (var context = new PlagiatContext())
                {
                    var info = new System.Text.StringBuilder();
                    
                    info.AppendLine("=== ИНФОРМАЦИЯ О БАЗЕ ДАННЫХ ===");
                    info.AppendLine($"Строка подключения: {context.Database.Connection.ConnectionString}");
                    info.AppendLine($"База данных существует: {context.Database.Exists()}");
                    
                    if (context.Database.Exists())
                    {
                        var projectsCount = await context.Projects.CountAsync();
                        var documentsCount = await context.Documents.CountAsync();
                        var sourcesCount = await context.Sources.CountAsync();
                        var citationsCount = await context.Citations.CountAsync();
                        var plagiarismResultsCount = await context.PlagiarismResults.CountAsync();

                        info.AppendLine();
                        info.AppendLine("=== СТАТИСТИКА ТАБЛИЦ ===");
                        info.AppendLine($"Проекты: {projectsCount}");
                        info.AppendLine($"Документы: {documentsCount}");
                        info.AppendLine($"Источники: {sourcesCount}");
                        info.AppendLine($"Цитаты: {citationsCount}");
                        info.AppendLine($"Результаты проверки плагиата: {plagiarismResultsCount}");
                    }

                    return info.ToString();
                }
            }
            catch (Exception ex)
            {
                return $"Ошибка получения информации о базе данных: {ex.Message}";
            }
        }

        public static async Task<bool> TestConnectionAsync()
        {
            try
            {
                using (var context = new PlagiatContext())
                {
                    await context.Database.Connection.OpenAsync();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        
    }
}
