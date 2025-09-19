using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Plagiat.Models;

namespace Plagiat.Services
{
    public class DocumentService
    {
        public async Task<Models.Document> ImportDocumentAsync(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var extension = fileInfo.Extension.ToLower();
                string content = "";

                // Проверяем существование файла
                if (!fileInfo.Exists)
                {
                    throw new FileNotFoundException($"Файл не найден: {filePath}");
                }

                Console.WriteLine($"Импорт файла: {filePath}");
                Console.WriteLine($"Размер файла: {fileInfo.Length} байт");
                Console.WriteLine($"Расширение: {extension}");

                switch (extension)
                {
                    case ".docx":
                        content = await ExtractFromDocxAsync(filePath);
                        break;
                    case ".txt":
                        content = await ExtractFromTxtAsync(filePath);
                        break;
                    case ".pdf":
                        content = await ExtractFromPdfAsync(filePath);
                        break;
                    case ".rtf":
                        content = await ExtractFromRtfAsync(filePath);
                        break;
                    default:
                        throw new NotSupportedException($"Формат файла {extension} не поддерживается");
                }

                Console.WriteLine($"Извлеченный контент: {content?.Length ?? 0} символов");

                // Проверяем, что контент не пустой
                if (string.IsNullOrWhiteSpace(content))
                {
                    // Если контент пустой, создаем заглушку
                    content = $"[Содержимое файла '{fileInfo.Name}' не удалось извлечь или файл пустой]";
                    Console.WriteLine("Контент пустой, создана заглушка");
                }

                var document = new Models.Document
                {
                    Title = System.IO.Path.GetFileNameWithoutExtension(filePath),
                    Content = content ?? "", // Гарантируем, что Content не null
                    OriginalFileName = fileInfo.Name,
                    FileFormat = extension,
                    CreatedAt = DateTime.Now,
                    Status = DocumentStatus.New
                };

                Console.WriteLine($"Создан документ: {document.Title}, контент: {document.Content.Length} символов");
                return document;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка импорта: {ex.Message}");
                throw new Exception($"Ошибка при импорте документа: {ex.Message}", ex);
            }
        }

        public Models.Document CreateFromText(string text, string title = null)
        {
            Console.WriteLine($"Создание документа из текста: {title}");
            Console.WriteLine($"Длина текста: {text?.Length ?? 0} символов");
            
            var document = new Models.Document
            {
                Title = title ?? "Новый документ",
                Content = text ?? "", // Гарантируем, что Content не null
                FileFormat = ".txt",
                CreatedAt = DateTime.Now,
                Status = DocumentStatus.New
            };
            
            Console.WriteLine($"Создан документ: {document.Title}, контент: {document.Content.Length} символов");
            return document;
        }

        public async Task<string> ExportDocumentAsync(Models.Document document, string outputPath, string format = ".docx")
        {
            try
            {
                var fileName = $"{document.Title}_{DateTime.Now:yyyyMMdd_HHmmss}{format}";
                var fullPath = System.IO.Path.Combine(outputPath, fileName);

                switch (format.ToLower())
                {
                    case ".txt":
                        await ExportToTxtAsync(document.Content, fullPath);
                        break;
                    case ".docx":
                        await ExportToDocxAsync(document.Content, fullPath);
                        break;
                    default:
                        throw new NotSupportedException($"Экспорт в формат {format} не поддерживается");
                }

                return fullPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при экспорте документа: {ex.Message}", ex);
            }
        }

        private async Task<string> ExtractFromDocxAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var content = new StringBuilder();
                    
                    using (var document = WordprocessingDocument.Open(filePath, false))
                    {
                        var body = document.MainDocumentPart.Document.Body;
                        foreach (var paragraph in body.Elements<Paragraph>())
                        {
                            content.AppendLine(paragraph.InnerText);
                        }
                    }
                    
                    return content.ToString();
                }
                catch (Exception ex)
                {
                    return $"[Ошибка извлечения из DOCX: {ex.Message}]";
                }
            });
        }

        private async Task<string> ExtractFromTxtAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Console.WriteLine($"Чтение TXT файла: {filePath}");
                    
                    // Попробуем разные кодировки
                    string content = null;
                    var encodings = new[] { Encoding.UTF8, Encoding.Default, Encoding.GetEncoding(1251) };
                    
                    foreach (var encoding in encodings)
                    {
                        try
                        {
                            content = File.ReadAllText(filePath, encoding);
                            Console.WriteLine($"Успешно прочитан файл с кодировкой {encoding.EncodingName}");
                            break;
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    
                    if (content == null)
                    {
                        // Последняя попытка - байты
                        var bytes = File.ReadAllBytes(filePath);
                        content = Encoding.UTF8.GetString(bytes);
                    }
                    
                    Console.WriteLine($"Прочитано {content.Length} символов из TXT файла");
                    Console.WriteLine($"Первые 100 символов: {content.Substring(0, Math.Min(100, content.Length))}");
                    
                    return content;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка чтения TXT файла: {ex.Message}");
                    return $"[Ошибка извлечения из TXT: {ex.Message}]";
                }
            });
        }

        private async Task<string> ExtractFromPdfAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var content = new StringBuilder();
                    
                    using (var reader = new PdfReader(filePath))
                    {
                        for (int i = 1; i <= reader.NumberOfPages; i++)
                        {
                            var text = PdfTextExtractor.GetTextFromPage(reader, i);
                            content.AppendLine(text);
                        }
                    }
                    
                    return content.ToString();
                }
                catch (Exception ex)
                {
                    return $"[Ошибка извлечения из PDF: {ex.Message}]";
                }
            });
        }

        private async Task<string> ExtractFromRtfAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Простое извлечение RTF - можно улучшить с помощью специальной библиотеки
                    var rtfContent = File.ReadAllText(filePath, Encoding.UTF8);

                    // Убираем RTF-теги (очень упрощенно)
                    var content = rtfContent;
                    content = System.Text.RegularExpressions.Regex.Replace(content, @"\\[a-z]+\d*\s?", "");
                    content = System.Text.RegularExpressions.Regex.Replace(content, @"[{}]", "");
                    
                    return content.Trim();
                }
                catch (Exception ex)
                {
                    return $"[Ошибка извлечения из RTF: {ex.Message}]";
                }
            });
        }

        private async Task ExportToTxtAsync(string content, string filePath)
        {
            await Task.Run(() => File.WriteAllText(filePath, content, Encoding.UTF8));
        }

        private async Task ExportToDocxAsync(string content, string filePath)
        {
            await Task.Run(() =>
            {
                using (var document = WordprocessingDocument.Create(filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
                {
                    var mainPart = document.AddMainDocumentPart();
                    mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                    var body = mainPart.Document.AppendChild(new Body());

                    var paragraphs = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var paragraphText in paragraphs)
                    {
                        var paragraph = body.AppendChild(new Paragraph());
                        var run = paragraph.AppendChild(new Run());
                        run.AppendChild(new Text(paragraphText));
                    }
                }
            });
        }

        public string DetectLanguage(string text)
        {
            // Простое определение языка по характерным символам
            var russianChars = System.Text.RegularExpressions.Regex.Matches(text, @"[а-яё]", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
            var englishChars = System.Text.RegularExpressions.Regex.Matches(text, @"[a-z]", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
            
            if (russianChars > englishChars)
                return "ru";
            else if (englishChars > russianChars)
                return "en";
            else
                return "unknown";
        }

        public DocumentStructure AnalyzeStructure(string content)
        {
            var structure = new DocumentStructure();
            var lines = content.Split('\n');

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine)) continue;

                // Определяем тип элемента
                if (IsHeading(trimmedLine))
                {
                    structure.Headings.Add(trimmedLine);
                }
                else if (IsList(trimmedLine))
                {
                    structure.Lists.Add(trimmedLine);
                }
                else
                {
                    structure.Paragraphs.Add(trimmedLine);
                }
            }

            return structure;
        }

        private bool IsHeading(string line)
        {
            // Простая эвристика для определения заголовков
            return line.Length < 100 && 
                   (line.ToUpper() == line || 
                    System.Text.RegularExpressions.Regex.IsMatch(line, @"^\d+\.?\s+[А-ЯЁ]") ||
                    System.Text.RegularExpressions.Regex.IsMatch(line, @"^[А-ЯЁ\s]+$"));
        }

        private bool IsList(string line)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(line, @"^[\d\-•·\*]\s+");
        }
    }

    public class DocumentStructure
    {
        public System.Collections.Generic.List<string> Headings { get; set; }
        public System.Collections.Generic.List<string> Paragraphs { get; set; }
        public System.Collections.Generic.List<string> Lists { get; set; }
        public System.Collections.Generic.List<string> Tables { get; set; }

        public DocumentStructure()
        {
            Headings = new System.Collections.Generic.List<string>();
            Paragraphs = new System.Collections.Generic.List<string>();
            Lists = new System.Collections.Generic.List<string>();
            Tables = new System.Collections.Generic.List<string>();
        }
    }
}

