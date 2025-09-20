using HandyControl.Controls;
using HandyControl.Tools;
using Microsoft.Win32;
using Plagiat.Models;
using Plagiat.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
namespace Plagiat
{
    public partial class MainWindow : System.Windows.Window
    {
        private readonly DocumentService _documentService;
        private readonly AntiPlagiatService _antiPlagiatService;
        private readonly OpenRouterService _openRouterService;
        private readonly DataService _dataService;
        private readonly CitationService _citationService;
        private readonly BibliographyService _bibliographyService;
        private Models.Document _currentDocument;
        private Project _currentProject;
        private ObservableCollection<PlagiarismResult> _plagiarismResults;
        private ObservableCollection<Citation> _citations;
        private ObservableCollection<Source> _bibliography;
        private DispatcherTimer _textChangeTimer;
        public MainWindow()
        {
            InitializeComponent();
            _documentService = new DocumentService();
            _antiPlagiatService = new AntiPlagiatService();
            _openRouterService = new OpenRouterService();
            _dataService = new DataService();
            _citationService = new CitationService(_openRouterService);
            _bibliographyService = new BibliographyService(_openRouterService);
            _plagiarismResults = new ObservableCollection<PlagiarismResult>();
            _citations = new ObservableCollection<Citation>();
            _bibliography = new ObservableCollection<Source>();
            InitializeTimer();
            ConfigHelper.Instance.SetLang("ru");
            Loaded += MainWindow_Loaded;
        }
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeCollections();
            await InitializeDatabaseAsync();
            UpdateStatus("Готов к работе");
        }
        private async Task InitializeDatabaseAsync()
        {
            try
            {
                SetLoading(true, "Инициализация базы данных...");
                var success = await Services.DatabaseInitializer.InitializeDatabaseAsync();
                if (success)
                {
                    UpdateStatus("База данных готова к работе");
                    var dbInfo = await Services.DatabaseInitializer.GetDatabaseInfoAsync();
                    System.Diagnostics.Debug.WriteLine(dbInfo);
                }
                else
                {
                    UpdateStatus("Ошибка инициализации базы данных");
                    ShowError("Ошибка базы данных", 
                        "Не удалось инициализировать базу данных. Проверьте подключение к SQL Server LocalDB.");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("Ошибка подключения к базе данных");
                ShowError("Ошибка базы данных", ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }
        private void InitializeCollections()
        {
            if (PlagiarismResultsListBox != null)
                PlagiarismResultsListBox.ItemsSource = _plagiarismResults;
            if (CitationsListBox != null)
                CitationsListBox.ItemsSource = _citations;
            if (BibliographyListBox != null)
                BibliographyListBox.ItemsSource = _bibliography;
        }
        private void InitializeTimer()
        {
            _textChangeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _textChangeTimer.Tick += TextChangeTimer_Tick;
        }
        private async void NewProjectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentProject = new Project
                {
                    Name = "Новый проект",
                    CreatedAt = DateTime.Now
                };
                UpdateProjectsTree();
                UpdateStatus("Создан новый проект");
            }
            catch (Exception ex)
            {
                ShowError("Ошибка при создании проекта", ex.Message);
            }
        }
        private void OpenProjectButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Функция в разработке");
        }
        private void SaveProjectButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Функция в разработке");
        }
        private async void ImportDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите документ для импорта",
                Filter = "Все поддерживаемые|*.docx;*.txt;*.pdf;*.rtf|Word документы (*.docx)|*.docx|Текстовые файлы (*.txt)|*.txt|PDF файлы (*.pdf)|*.pdf|RTF файлы (*.rtf)|*.rtf",
                Multiselect = false
            };
            if (openFileDialog.ShowDialog() == true)
            {
                await ImportDocument(openFileDialog.FileName);
            }
        }
        private async void CheckPlagiarismButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDocument == null || string.IsNullOrEmpty(_currentDocument.Content))
            {
                ShowWarning("Нет документа для проверки");
                return;
            }
            await CheckPlagiarism();
        }
        private async void ParaphraseAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_plagiarismResults.Count == 0)
            {
                ShowWarning("Нет проблемных фрагментов для перефразирования");
                return;
            }
            await ParaphraseAllProblematicFragments();
        }
        private void DocumentRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _textChangeTimer?.Stop();
            _textChangeTimer?.Start();

            // ИСПРАВЛЕНИЕ: Обновляем контент только если документ уже загружен
            if (_currentDocument != null && DocumentRichTextBox != null)
            {
                try
                {
                    var textRange = new TextRange(DocumentRichTextBox.Document.ContentStart,
                                                DocumentRichTextBox.Document.ContentEnd);

                    // ИСПРАВЛЕНИЕ: Сохраняем форматирование переносов строк
                    var newContent = textRange.Text;

                    // Обновляем только если контент действительно изменился
                    if (_currentDocument.Content != newContent)
                    {
                        _currentDocument.Content = newContent;
                        _currentDocument.LastModified = DateTime.Now;
                        Console.WriteLine($"Контент обновлен: {newContent.Length} символов");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обновлении содержимого документа: {ex.Message}");
                }
            }
        }
        private void TextChangeTimer_Tick(object sender, EventArgs e)
        {
            _textChangeTimer.Stop();
            UpdateWordCount();
        }
        private async void DocumentRichTextBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    await ImportDocument(files[0]);
                }
            }
        }
        private void DocumentRichTextBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    var extension = Path.GetExtension(files[0]).ToLower();
                    if (new[] { ".docx", ".txt", ".pdf", ".rtf" }.Contains(extension))
                    {
                        e.Effects = DragDropEffects.Copy;
                    }
                    else
                    {
                        e.Effects = DragDropEffects.None;
                    }
                }
            }
        }
        private void PlagiarismResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlagiarismResultsListBox?.SelectedItem is PlagiarismResult result)
            {
                HighlightTextInEditor(result.StartPosition, result.EndPosition);
            }
        }
        private void CitationStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshCitationsDisplay();
        }
        private void ExportBibliographyButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Функция в разработке");
        }
        private async void ProjectsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is Models.Document document)
            {
                try
                {
                    Console.WriteLine($"Выбран документ: {document.Title}");
                    UpdateStatus($"Загрузка документа: {document.Title}");

                    // ИСПРАВЛЕНИЕ: Если контент пуст, загружаем из БД
                    if (string.IsNullOrEmpty(document.Content) && document.Id > 0)
                    {
                        Console.WriteLine("Контент пуст, загружаем из базы данных...");
                        var fullDocument = await _dataService.GetDocumentByIdAsync(document.Id);
                        if (fullDocument != null && !string.IsNullOrEmpty(fullDocument.Content))
                        {
                            Console.WriteLine($"Загружен из БД: {fullDocument.Content.Length} символов");
                            document.Content = fullDocument.Content; // Копируем контент
                        }
                        else
                        {
                            Console.WriteLine("Документ не найден в базе данных или контент пуст!");
                            UpdateStatus("Ошибка: документ пуст");
                            return;
                        }
                    }

                    Console.WriteLine($"Перед LoadDocument: document.Content.Length = {document.Content?.Length ?? 0}");
                    LoadDocument(document);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки документа: {ex.Message}");
                    ShowError("Ошибка загрузки документа", ex.Message);
                }
            }
        }
        private async Task ImportDocument(string filePath)
        {
            try
            {
                Console.WriteLine($"Начинаем импорт файла: {filePath}");
                SetLoading(true, "Импорт документа...");

                var document = await _documentService.ImportDocumentAsync(filePath);
                Console.WriteLine($"Документ импортирован: {document.Title}, контент: {document.Content?.Length ?? 0} символов");

                // ПРОВЕРКА: Убеждаемся, что контент не пуст
                if (string.IsNullOrEmpty(document.Content))
                {
                    Console.WriteLine("ОШИБКА: Контент документа пуст после импорта!");
                    ShowError("Ошибка импорта", "Не удалось извлечь содержимое из файла");
                    return;
                }

                // Устанавливаем название если его нет
                if (string.IsNullOrWhiteSpace(document.Title))
                {
                    document.Title = Path.GetFileNameWithoutExtension(filePath);
                }

                // ИСПРАВЛЕНИЕ: Создаем копию контента для предотвращения потери
                var contentBackup = document.Content;

                // Сохраняем в БД
                if (_currentProject != null)
                {
                    if (_currentProject.Id == 0)
                    {
                        _currentProject = await _dataService.SaveProjectAsync(_currentProject);
                    }

                    document.ProjectId = _currentProject.Id;
                    Console.WriteLine($"Сохраняем документ в БД с ProjectId: {document.ProjectId}");

                    // ИСПРАВЛЕНИЕ: Восстанавливаем контент перед сохранением
                    document.Content = contentBackup;

                    document = await _dataService.SaveDocumentAsync(document);
                    Console.WriteLine($"Документ сохранен в БД с ID: {document.Id}, контент: {document.Content?.Length ?? 0} символов");

                    _currentProject.Documents.Add(document);
                    UpdateProjectsTree();
                }

                // ИСПРАВЛЕНИЕ: Еще раз убеждаемся, что контент на месте
                document.Content = contentBackup;

                LoadDocument(document);

                if (CheckPlagiarismButton != null)
                    CheckPlagiarismButton.IsEnabled = true;

                UpdateStatus($"Документ '{document.Title}' успешно импортирован ({document.Content.Length} символов)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка импорта: {ex.Message}");
                ShowError("Ошибка импорта", ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void LoadDocument(Models.Document document)
        {
            Console.WriteLine($"LoadDocument вызван с документом: {document.Title}");
            Console.WriteLine($"LoadDocument - контент документа: длина {document.Content?.Length ?? 0}");

            _currentDocument = document;

            if (DocumentTitleTextBlock != null)
                DocumentTitleTextBlock.Text = document.Title;
            if (DocumentInfoTextBlock != null)
                DocumentInfoTextBlock.Text = $"Создан: {document.CreatedAt:dd.MM.yyyy HH:mm}";

            if (DocumentRichTextBox != null)
            {
                // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Отключаем обработчик события перед загрузкой
                DocumentRichTextBox.TextChanged -= DocumentRichTextBox_TextChanged;

                try
                {
                    DocumentRichTextBox.Document.Blocks.Clear();

                    var content = document.Content ?? "";
                    Console.WriteLine($"Загружаем в RichTextBox: {content.Length} символов");

                    if (!string.IsNullOrEmpty(content))
                    {
                        var flowDocument = new FlowDocument();
                        var paragraph = new Paragraph();

                        // ИСПРАВЛЕНИЕ ПЕРЕНОСОВ СТРОК
                        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (i > 0)
                                paragraph.Inlines.Add(new LineBreak());

                            paragraph.Inlines.Add(new Run(lines[i]));
                        }

                        flowDocument.Blocks.Add(paragraph);
                        DocumentRichTextBox.Document = flowDocument;
                    }

                    Console.WriteLine($"Текст успешно загружен в RichTextBox");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при загрузке текста в RichTextBox: {ex.Message}");
                    // Fallback
                    DocumentRichTextBox.Document.Blocks.Clear();
                    DocumentRichTextBox.Document.Blocks.Add(new Paragraph(new Run(document.Content ?? "")));
                }
                finally
                {
                    // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Включаем обработчик обратно
                    DocumentRichTextBox.TextChanged += DocumentRichTextBox_TextChanged;
                }
            }

            UpdateWordCount();
            UpdatePlagiarismStatistics();
            LoadPlagiarismResults();
            LoadCitations();
        }
        private async Task CheckPlagiarism()
        {
            try
            {
                SetLoading(true, "Проверка на плагиат...");
                var results = await _antiPlagiatService.CheckPlagiarismAsync(_currentDocument.Content, _currentDocument.Id);
                var uniqueness = await _antiPlagiatService.GetOverallUniquenessAsync(_currentDocument.Content);
                _plagiarismResults.Clear();
                foreach (var result in results)
                {
                    _plagiarismResults.Add(result);
                }
                _currentDocument.UniquenessPercentage = uniqueness; 
                _currentDocument.Status = DocumentStatus.Analyzed;  
                UpdatePlagiarismStatistics();
                HighlightPlagiarismInEditor();
                
                // ДОБАВЛЕНО: Обнаружение цитат после проверки плагиата
                await FindAndProcessCitations();
                
                if (UniquenessProgressBar != null)
                {
                    UniquenessProgressBar.Value = uniqueness;
                    UniquenessProgressBar.Visibility = Visibility.Visible;
                }
                if (ParaphraseAllButton != null)
                    ParaphraseAllButton.IsEnabled = _plagiarismResults.Count > 0;
                UpdateStatus($"Проверка завершена. Уникальность: {uniqueness:F1}%");
            }
            catch (Exception ex)
            {
                ShowError("Ошибка проверки", ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async Task FindAndProcessCitations()
        {
            try
            {
                Console.WriteLine("Начинаем поиск цитат в документе...");
                
                // Находим цитаты в тексте
                var foundCitations = await _citationService.FindCitationsInTextAsync(_currentDocument.Content, _currentDocument.Id);
                Console.WriteLine($"Найдено цитат: {foundCitations.Count}");
                
                // Очищаем старые цитаты
                _citations.Clear();
                
                // Обрабатываем каждую найденную цитату
                foreach (var foundCitation in foundCitations)
                {
                    Console.WriteLine($"Обрабатываем цитату: {foundCitation.QuotedText.Substring(0, Math.Min(50, foundCitation.QuotedText.Length))}...");
                    
                    // Создаем копию цитаты для обработки
                    var citation = foundCitation;
                    
                    // Ищем источник для цитаты
                    var source = await _citationService.FindSourceForCitationAsync(citation);
                    if (source != null)
                    {
                        citation.Source = source;
                        citation.SourceId = source.Id;
                        
                        // Сохраняем источник в базу данных
                        source = await _dataService.SaveSourceAsync(source);
                        citation.SourceId = source.Id;
                    }
                    
                    // Сохраняем цитату в базу данных
                    var savedCitation = await _dataService.SaveCitationAsync(citation);
                    
                    // Добавляем в коллекцию
                    _citations.Add(savedCitation);
                    
                    // Добавляем источник в библиографию если его там нет
                    if (source != null && !_bibliography.Any(s => s.Id == source.Id))
                    {
                        _bibliography.Add(source);
                    }
                }
                
                // Обновляем отображение
                LoadCitations();
                LoadBibliography();
                
                Console.WriteLine($"Обработка цитат завершена. Цитат: {_citations.Count}, Источников: {_bibliography.Count}");
                UpdateStatus($"Найдено цитат: {_citations.Count}, источников: {_bibliography.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при поиске цитат: {ex.Message}");
                ShowError("Ошибка поиска цитат", ex.Message);
            }
        }

        private async Task ParaphraseAllProblematicFragments()
        {
            try
            {
                SetLoading(true, "Перефразирование текста...");
                var problematicResults = _plagiarismResults
                    .OrderByDescending(r => r.StartPosition)
                    .ToList();
                var options = new ParaphraseOptions
                {
                    Style = ParaphraseStyle.Academic,
                    Level = ParaphraseLevel.Medium
                };
                foreach (var result in problematicResults)
                {
                    var variants = await _openRouterService.ParaphraseTextAsync(result.MatchedText, options);
                    if (variants.Count > 0)
                    {
                        var selectedVariant = await ShowParaphraseVariantsDialog(result.MatchedText, variants);
                        if (!string.IsNullOrEmpty(selectedVariant))
                        {
                            ReplaceTextInEditor(result.StartPosition, result.EndPosition, selectedVariant);
                        }
                    }
                }
                await CheckPlagiarism();
                UpdateStatus("Перефразирование завершено");
            }
            catch (Exception ex)
            {
                ShowError("Ошибка перефразирования", ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }
        private void UpdateWordCount()
        {
            Console.WriteLine($"UpdateWordCount вызван. _currentDocument: {_currentDocument != null}");

            if (_currentDocument != null)
            {
                var content = _currentDocument.Content ?? "";
                Console.WriteLine($"UpdateWordCount - контент: длина {content.Length}");

                // ИСПРАВЛЕНИЕ: Более точный подсчет слов
                var words = 0;
                if (!string.IsNullOrWhiteSpace(content))
                {
                    // Убираем лишние пробелы и считаем слова
                    words = content.Split(new char[] { ' ', '\n', '\r', '\t', '\f', '\v' },
                                        StringSplitOptions.RemoveEmptyEntries).Length;
                }

                var chars = content.Length;

                Console.WriteLine($"UpdateWordCount - подсчитано: {words} слов, {chars} символов");

                // Обновляем интерфейс
                Dispatcher.Invoke(() =>
                {
                    if (WordCountTextBlock != null)
                        WordCountTextBlock.Text = $"Слов: {words}";
                    if (CharCountTextBlock != null)
                        CharCountTextBlock.Text = $"Символов: {chars}";
                });

                var status = string.IsNullOrWhiteSpace(content) ?
                    "Документ пуст" :
                    $"Документ загружен: {words} слов, {chars} символов";

                UpdateStatus(status);
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    if (WordCountTextBlock != null)
                        WordCountTextBlock.Text = "Слов: 0";
                    if (CharCountTextBlock != null)
                        CharCountTextBlock.Text = "Символов: 0";
                });

                UpdateStatus("Документ не выбран");
            }
        }
        private void UpdatePlagiarismStatistics()
        {
            var critical = _plagiarismResults.Count(r => r.Level == PlagiarismLevel.Critical);
            var warning = _plagiarismResults.Count(r => r.Level == PlagiarismLevel.Warning);
            var acceptable = _plagiarismResults.Count(r => r.Level == PlagiarismLevel.Acceptable);
            if (CriticalCountTextBlock != null)
                CriticalCountTextBlock.Text = critical.ToString();
            if (WarningCountTextBlock != null)
                WarningCountTextBlock.Text = warning.ToString();
            if (AcceptableCountTextBlock != null)
                AcceptableCountTextBlock.Text = acceptable.ToString();
        }
        private void UpdateProjectsTree()
        {
            if (ProjectsTreeView == null) return;
            ProjectsTreeView.Items.Clear();
            if (_currentProject != null)
            {
                var projectItem = new TreeViewItem
                {
                    Header = _currentProject.Name,
                    Tag = _currentProject
                };
                foreach (var document in _currentProject.Documents)
                {
                    var docItem = new TreeViewItem
                    {
                        Header = document.Title,
                        Tag = document
                    };
                    projectItem.Items.Add(docItem);
                }
                projectItem.IsExpanded = true;
                ProjectsTreeView.Items.Add(projectItem);
            }
        }
        private void UpdateStatus(string message)
        {
            if (StatusTextBlock != null)
                StatusTextBlock.Text = message;
            if (AppStatusTextBlock != null)
                AppStatusTextBlock.Text = message;
        }
        private void SetLoading(bool isLoading, string message = "")
        {
            if (LoadingIndicator != null)
                LoadingIndicator.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            if (isLoading && !string.IsNullOrEmpty(message))
            {
                UpdateStatus(message);
            }
        }
        private void HighlightPlagiarismInEditor()
        {
            foreach (var result in _plagiarismResults)
            {
                var color = GetColorByLevel(result.Level);
            }
        }
        private void HighlightTextInEditor(int startPosition, int endPosition)
        {
            try
            {
                if (DocumentRichTextBox == null) return;
                var start = DocumentRichTextBox.Document.ContentStart.GetPositionAtOffset(startPosition);
                var end = DocumentRichTextBox.Document.ContentStart.GetPositionAtOffset(endPosition);
                if (start != null && end != null)
                {
                    DocumentRichTextBox.Selection.Select(start, end);
                    DocumentRichTextBox.ScrollToVerticalOffset(DocumentRichTextBox.Selection.Start.GetCharacterRect(LogicalDirection.Forward).Top);
                }
            }
            catch { }
        }
        private void ReplaceTextInEditor(int startPosition, int endPosition, string newText)
        {
            try
            {
                if (DocumentRichTextBox == null) return;
                var start = DocumentRichTextBox.Document.ContentStart.GetPositionAtOffset(startPosition);
                var end = DocumentRichTextBox.Document.ContentStart.GetPositionAtOffset(endPosition);
                if (start != null && end != null)
                {
                    var range = new TextRange(start, end);
                    range.Text = newText;
                }
            }
            catch { }
        }
        private Color GetColorByLevel(PlagiarismLevel level)
        {
            switch (level)
            {
                case PlagiarismLevel.Critical:
                    return Colors.Red;
                case PlagiarismLevel.Warning:
                    return Colors.Orange;
                case PlagiarismLevel.Acceptable:
                    return Colors.Green;
                default:
                    return Colors.Transparent;
            }
        }
        private void LoadPlagiarismResults()
        {
            _plagiarismResults.Clear();
            if (_currentDocument?.PlagiarismResults != null)
            {
                foreach (var result in _currentDocument.PlagiarismResults)
                {
                    _plagiarismResults.Add(result);
                }
                UpdatePlagiarismStatistics();
            }
        }
        private void LoadCitations()
        {
            _citations.Clear();
            if (_currentDocument?.Citations != null)
            {
                foreach (var citation in _currentDocument.Citations)
                {
                    _citations.Add(citation);
                }
            }
        }

        private void LoadBibliography()
        {
            _bibliography.Clear();
            if (_currentDocument?.Citations != null)
            {
                foreach (var citation in _currentDocument.Citations)
                {
                    if (citation.Source != null && !_bibliography.Any(s => s.Id == citation.Source.Id))
                    {
                        _bibliography.Add(citation.Source);
                    }
                }
            }
        }
        private void RefreshCitationsDisplay() { }
        private async Task<string> ShowParaphraseVariantsDialog(string originalText, List<string> variants)
        {
            var dialog = new Views.SimpleParaphraseDialog(originalText, variants, _openRouterService);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                return dialog.SelectedVariant;
            }
            return null;
        }
        private void ShowError(string title, string message)
        {
            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        private void ShowWarning(string message)
        {
            System.Windows.MessageBox.Show(message, "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        private void ShowInfo(string message)
        {
            System.Windows.MessageBox.Show(message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        protected override void OnClosed(EventArgs e)
        {
            _openRouterService?.Dispose();
            _antiPlagiatService?.Dispose();
            base.OnClosed(e);
        }
        public static implicit operator HandyControl.Controls.Window(MainWindow v)
        {
            throw new NotImplementedException();
        }
    }
}
