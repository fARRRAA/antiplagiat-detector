using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Plagiat.Data;
using Plagiat.Models;

namespace Plagiat.Services
{
    public class DataService : IDisposable
    {
        private readonly PlagiatContext _context;

        public DataService()
        {
            _context = new PlagiatContext();
        }

        #region Project Operations

        public async Task<List<Project>> GetProjectsAsync()
        {
            return await _context.Projects
                .Include(p => p.Documents)
                .OrderByDescending(p => p.LastModified ?? p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Project> GetProjectByIdAsync(int projectId)
        {
            return await _context.Projects
                .Include(p => p.Documents.Select(d => d.PlagiarismResults))
                .Include(p => p.Documents.Select(d => d.Citations.Select(c => c.Source)))
                .FirstOrDefaultAsync(p => p.Id == projectId);
        }

        public async Task<Project> SaveProjectAsync(Project project)
        {
            try
            {
                // Валидация обязательных полей
                if (string.IsNullOrWhiteSpace(project.Name))
                {
                    project.Name = "Новый проект";
                }

                // Устанавливаем значения по умолчанию
                if (project.CreatedAt == default(DateTime))
                {
                    project.CreatedAt = DateTime.Now;
                }
                
                if (project.Status == default(ProjectStatus))
                {
                    project.Status = ProjectStatus.Active;
                }

                if (project.Id == 0)
                {
                    _context.Projects.Add(project);
                }
                else
                {
                    var existing = await _context.Projects.FindAsync(project.Id);
                    if (existing != null)
                    {
                        existing.Name = project.Name;
                        existing.Description = project.Description;
                        existing.LastModified = DateTime.Now;
                        existing.Settings = project.Settings;
                        existing.DefaultCitationStyle = project.DefaultCitationStyle;
                    }
                }

                await _context.SaveChangesAsync();
                return project;
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                // Логируем детали ошибок валидации
                var errorMessages = new List<string>();
                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        errorMessages.Add($"Свойство: {validationError.PropertyName}, Ошибка: {validationError.ErrorMessage}");
                    }
                }
                
                var fullErrorMessage = string.Join("; ", errorMessages);
                throw new Exception($"Ошибка валидации при сохранении проекта: {fullErrorMessage}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении проекта: {ex.Message}", ex);
            }
        }

        public async Task DeleteProjectAsync(int projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project != null)
            {
                project.Status = ProjectStatus.Deleted;
                await _context.SaveChangesAsync();
            }
        }

        #endregion

        #region Document Operations

        public async Task<Document> SaveDocumentAsync(Document document)
        {
            try
            {
                // Валидация обязательных полей
                if (string.IsNullOrWhiteSpace(document.Title))
                {
                    document.Title = "Без названия";
                }
                
                // Обработка Content - всегда устанавливаем значение
                if (document.Content == null || string.IsNullOrWhiteSpace(document.Content))
                {
                    document.Content = "[Пустой документ]";
                }

                // Устанавливаем значения по умолчанию
                if (document.CreatedAt == default(DateTime))
                {
                    document.CreatedAt = DateTime.Now;
                }
                
                if (document.Status == default(DocumentStatus))
                {
                    document.Status = DocumentStatus.New;
                }
                
                // Проверяем, что ProjectId существует в базе данных
                if (document.ProjectId.HasValue && document.ProjectId.Value > 0)
                {
                    var projectExists = await _context.Projects.AnyAsync(p => p.Id == document.ProjectId.Value);
                    if (!projectExists)
                    {
                        // Если проект не существует, создаем новый
                        var newProject = new Project
                        {
                            Name = "Автоматически созданный проект",
                            CreatedAt = DateTime.Now,
                            Status = ProjectStatus.Active
                        };
                        _context.Projects.Add(newProject);
                        await _context.SaveChangesAsync();
                        
                        // Обновляем ProjectId документа
                        document.ProjectId = newProject.Id;
                    }
                }

                if (document.Id == 0)
                {
                    _context.Documents.Add(document);
                }
                else
                {
                    var existing = await _context.Documents.FindAsync(document.Id);
                    if (existing != null)
                    {
                        existing.Title = document.Title;
                        existing.Content = document.Content;
                        existing.LastModified = DateTime.Now;
                        existing.UniquenessPercentage = document.UniquenessPercentage;
                        existing.Status = document.Status;
                        existing.Language = document.Language;
                        existing.ProjectId = document.ProjectId;
                    }
                }

                await _context.SaveChangesAsync();
                Console.WriteLine($"Документ сохранен в БД: ID={document.Id}, Title='{document.Title}', Content={document.Content?.Length ?? 0} символов");
                return document;
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                // Логируем детали ошибок валидации
                var errorMessages = new List<string>();
                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        errorMessages.Add($"Свойство: {validationError.PropertyName}, Ошибка: {validationError.ErrorMessage}");
                    }
                }
                
                var fullErrorMessage = string.Join("; ", errorMessages);
                throw new Exception($"Ошибка валидации при сохранении документа: {fullErrorMessage}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении документа: {ex.Message}", ex);
            }
        }

        public async Task<Document> GetDocumentByIdAsync(int documentId)
        {
            var document = await _context.Documents
                .Include(d => d.PlagiarismResults)
                .Include(d => d.Citations.Select(c => c.Source))
                .FirstOrDefaultAsync(d => d.Id == documentId);
            
            Console.WriteLine($"Загружен документ из БД: ID={documentId}, Title='{document?.Title}', Content={document?.Content?.Length ?? 0} символов");
            return document;
        }

        public async Task DeleteDocumentAsync(int documentId)
        {
            var document = await _context.Documents.FindAsync(documentId);
            if (document != null)
            {
                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();
            }
        }

        #endregion

        #region Plagiarism Results Operations

        public async Task SavePlagiarismResultsAsync(List<PlagiarismResult> results)
        {
            foreach (var result in results)
            {
                if (result.Id == 0)
                {
                    _context.PlagiarismResults.Add(result);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<PlagiarismResult>> GetPlagiarismResultsByDocumentIdAsync(int documentId)
        {
            return await _context.PlagiarismResults
                .Where(pr => pr.DocumentId == documentId)
                .OrderByDescending(pr => pr.SimilarityPercentage)
                .ToListAsync();
        }

        public async Task ClearPlagiarismResultsAsync(int documentId)
        {
            var results = await _context.PlagiarismResults
                .Where(pr => pr.DocumentId == documentId)
                .ToListAsync();

            _context.PlagiarismResults.RemoveRange(results);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Citation Operations

        public async Task<Citation> SaveCitationAsync(Citation citation)
        {
            if (citation.Id == 0)
            {
                _context.Citations.Add(citation);
            }
            else
            {
                var existing = await _context.Citations.FindAsync(citation.Id);
                if (existing != null)
                {
                    existing.QuotedText = citation.QuotedText;
                    existing.StartPosition = citation.StartPosition;
                    existing.EndPosition = citation.EndPosition;
                    existing.Type = citation.Type;
                    existing.PageNumber = citation.PageNumber;
                    existing.IsFormatted = citation.IsFormatted;
                    existing.Style = citation.Style;
                }
            }

            await _context.SaveChangesAsync();
            return citation;
        }

        public async Task<List<Citation>> GetCitationsByDocumentIdAsync(int documentId)
        {
            return await _context.Citations
                .Include(c => c.Source)
                .Where(c => c.DocumentId == documentId)
                .OrderBy(c => c.StartPosition)
                .ToListAsync();
        }

        public async Task DeleteCitationAsync(int citationId)
        {
            var citation = await _context.Citations.FindAsync(citationId);
            if (citation != null)
            {
                _context.Citations.Remove(citation);
                await _context.SaveChangesAsync();
            }
        }

        #endregion

        #region Source Operations

        public async Task<Source> SaveSourceAsync(Source source)
        {
            if (source.Id == 0)
            {
                _context.Sources.Add(source);
            }
            else
            {
                var existing = await _context.Sources.FindAsync(source.Id);
                if (existing != null)
                {
                    existing.Title = source.Title;
                    existing.Author = source.Author;
                    existing.Publisher = source.Publisher;
                    existing.Year = source.Year;
                    existing.Url = source.Url;
                    existing.ISBN = source.ISBN;
                    existing.DOI = source.DOI;
                    existing.Type = source.Type;
                    existing.Volume = source.Volume;
                    existing.Issue = source.Issue;
                    existing.Pages = source.Pages;
                    existing.City = source.City;
                    existing.AccessDate = source.AccessDate;
                    existing.IsComplete = source.IsComplete;
                }
            }

            await _context.SaveChangesAsync();
            return source;
        }

        public async Task<List<Source>> GetAllSourcesAsync()
        {
            return await _context.Sources
                .OrderBy(s => s.Author)
                .ThenBy(s => s.Year)
                .ToListAsync();
        }

        public async Task<Source> FindSimilarSourceAsync(Source source)
        {
            return await _context.Sources
                .FirstOrDefaultAsync(s => 
                    s.Title == source.Title && 
                    s.Author == source.Author && 
                    s.Year == source.Year);
        }

        public async Task DeleteSourceAsync(int sourceId)
        {
            var source = await _context.Sources.FindAsync(sourceId);
            if (source != null)
            {
                _context.Sources.Remove(source);
                await _context.SaveChangesAsync();
            }
        }

        #endregion

        #region Statistics

        public async Task<ProjectStatistics> GetProjectStatisticsAsync(int projectId)
        {
            var project = await GetProjectByIdAsync(projectId);
            if (project == null) return null;

            var stats = new ProjectStatistics
            {
                ProjectId = projectId,
                DocumentCount = project.Documents.Count,
                TotalWords = project.Documents.Sum(d => d.Content?.Split(' ').Length ?? 0),
                AverageUniqueness = project.Documents.Any() 
                    ? project.Documents.Average(d => d.UniquenessPercentage) 
                    : 0,
                TotalCitations = project.Documents.Sum(d => d.Citations?.Count ?? 0),
                TotalSources = project.Documents
                    .SelectMany(d => d.Citations ?? new List<Citation>())
                    .Select(c => c.SourceId)
                    .Distinct()
                    .Count()
            };

            return stats;
        }

        public async Task<List<RecentActivity>> GetRecentActivityAsync(int limit = 10)
        {
            var activities = new List<RecentActivity>();

            // Последние проекты
            var recentProjects = await _context.Projects
                .Where(p => p.Status == ProjectStatus.Active)
                .OrderByDescending(p => p.LastModified ?? p.CreatedAt)
                .Take(limit / 2)
                .ToListAsync();

            activities.AddRange(recentProjects.Select(p => new RecentActivity
            {
                Type = "Project",
                Title = p.Name,
                Date = p.LastModified ?? p.CreatedAt,
                Id = p.Id
            }));

            // Последние документы
            var recentDocuments = await _context.Documents
                .OrderByDescending(d => d.LastModified ?? d.CreatedAt)
                .Take(limit / 2)
                .ToListAsync();

            activities.AddRange(recentDocuments.Select(d => new RecentActivity
            {
                Type = "Document",
                Title = d.Title,
                Date = d.LastModified ?? d.CreatedAt,
                Id = d.Id
            }));

            return activities.OrderByDescending(a => a.Date).Take(limit).ToList();
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }

    public class ProjectStatistics
    {
        public int ProjectId { get; set; }
        public int DocumentCount { get; set; }
        public int TotalWords { get; set; }
        public double AverageUniqueness { get; set; }
        public int TotalCitations { get; set; }
        public int TotalSources { get; set; }
    }

    public class RecentActivity
    {
        public string Type { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public int Id { get; set; }
    }
}

