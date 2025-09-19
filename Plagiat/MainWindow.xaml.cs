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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
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
            
            // Настраиваем логирование для CitationService
            CitationService.LogAction = LogToDiagnostic;
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
        private void NewProjectButton_Click(object sender, RoutedEventArgs e)
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

        private async Task ParaphraseAllProblematicFragments()
        {
            try
            {
                if (_plagiarismResults == null || _plagiarismResults.Count == 0)
                {
                    ShowInfo("Нет проблемных фрагментов для перефразирования");
                    return;
                }

                var problematicFragments = _plagiarismResults
                    .Where(r => r.SimilarityPercentage > 50) // Фрагменты с высоким процентом схожести
                    .OrderBy(r => r.StartPosition)
                    .ToList();

                if (problematicFragments.Count == 0)
                {
                    ShowInfo("Нет фрагментов с высоким процентом схожести для перефразирования");
                    return;
                }

                // Открываем новый диалог пошагового перефразирования
                var dialog = new Views.MultiParaphraseDialog(problematicFragments, _openRouterService);
                dialog.Owner = this;

                if (dialog.ShowDialog() == true)
                {
                    var results = dialog.Results;
                    await ApplyParaphraseResults(results, problematicFragments);
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка перефразирования", ex.Message);
            }
        }

        private async Task ApplyParaphraseResults(Dictionary<int, string> results, List<PlagiarismResult> fragments)
        {
            try
            {
                SetLoading(true, "Применение перефразированных текстов...");

                // Сортируем результаты по позиции в убывающем порядке (с конца к началу)
                // чтобы изменения в тексте не сбивали позиции последующих фрагментов
                var sortedResults = results
                    .Select(kvp => new { Index = kvp.Key, Text = kvp.Value, Fragment = fragments[kvp.Key] })
                    .OrderByDescending(x => x.Fragment.StartPosition)
                    .ToList();

                int replacedCount = 0;
                foreach (var result in sortedResults)
                {
                    var fragment = result.Fragment;
                    var newText = result.Text;

                    // Заменяем только если текст изменился
                    if (newText != fragment.MatchedText)
                    {
                        ReplaceTextInEditor(fragment.StartPosition, fragment.EndPosition, newText);
                        replacedCount++;
                        LogToDiagnostic($"Заменен фрагмент на позиции {fragment.StartPosition}: '{fragment.MatchedText.Substring(0, Math.Min(30, fragment.MatchedText.Length))}...' → '{newText.Substring(0, Math.Min(30, newText.Length))}...'");
                    }
                }

                if (replacedCount > 0)
                {
                    UpdateStatus($"Перефразировано {replacedCount} фрагментов из {results.Count}");
                    
                    // Повторно проверяем плагиат после изменений
                    LogToDiagnostic("Повторная проверка плагиата после перефразирования...");
                    await CheckPlagiarism();
                }
                else
                {
                    UpdateStatus("Все фрагменты остались без изменений");
                }
            }
            catch (Exception ex)
            {
                LogToDiagnostic($"Ошибка применения результатов: {ex.Message}");
                ShowError("Ошибка применения результатов", ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }
        private void DocumentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _textChangeTimer?.Stop();
            _textChangeTimer?.Start();

            // ИСПРАВЛЕНИЕ: Обновляем контент только если документ уже загружен
            if (_currentDocument != null && DocumentTextBox != null)
            {
                try
                {
                    var newContent = DocumentTextBox.Text;

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
        
        // Старый метод для RichTextBox (оставляем для возможного возврата)
        private void DocumentRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Заглушка - пока используем TextBox
        }
        private void TextChangeTimer_Tick(object sender, EventArgs e)
        {
            _textChangeTimer.Stop();
            UpdateWordCount();
        }
        private async void DocumentTextBox_Drop(object sender, DragEventArgs e)
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
        private void DocumentTextBox_DragEnter(object sender, DragEventArgs e)
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
        
        // Старые методы для RichTextBox (оставляем для возможного возврата)
        private void DocumentRichTextBox_Drop(object sender, DragEventArgs e) { }
        private void DocumentRichTextBox_DragEnter(object sender, DragEventArgs e) { }
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
            try
            {
                if (_bibliography == null || _bibliography.Count == 0)
                {
                    ShowInfo("Библиография пуста. Сначала найдите цитаты.");
                    return;
                }

                // Диалог сохранения файла
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Сохранить библиографию",
                    Filter = "Текстовые файлы (*.txt)|*.txt|Документы Word (*.docx)|*.docx|Все файлы (*.*)|*.*",
                    DefaultExt = "txt",
                    FileName = $"bibliography_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Создаем библиографию для экспорта
                    var bibliography = new StringBuilder();
                    bibliography.AppendLine("БИБЛИОГРАФИЯ");
                    bibliography.AppendLine("=".PadRight(50, '='));
                    bibliography.AppendLine();
                    
                    for (int i = 0; i < _bibliography.Count; i++)
                    {
                        var source = _bibliography[i];
                        bibliography.AppendLine($"{i + 1}. {source.Title} - {source.Author}");
                    }

                    // Сохраняем в выбранный файл
                    File.WriteAllText(saveFileDialog.FileName, bibliography.ToString(), Encoding.UTF8);
                    
                    var fileName = Path.GetFileName(saveFileDialog.FileName);
                    UpdateStatus($"Библиография экспортирована: {fileName}");
                    LogToDiagnostic($"Библиография сохранена: {saveFileDialog.FileName}");
                    
                    ShowInfo($"Библиография успешно сохранена:\n{saveFileDialog.FileName}");
                }
                else
                {
                    LogToDiagnostic("Экспорт библиографии отменен пользователем");
                }
            }
            catch (Exception ex)
            {
                LogToDiagnostic($"Ошибка экспорта библиографии: {ex.Message}");
                ShowError("Ошибка экспорта", ex.Message);
            }
        }
        
        private void ClearDiagnosticButton_Click(object sender, RoutedEventArgs e)
        {
            if (DiagnosticTextBox != null)
            {
                DiagnosticTextBox.Text = "Лог очищен...\n";
            }
        }

        // Обработчики для вкладки перефразировки
        private async void StartParaphraseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем текст для перефразировки
                var inputText = ParaphraseInputTextBox?.Text?.Trim();
                
                if (string.IsNullOrEmpty(inputText) || inputText == "Введите здесь текст, который хотите перефразировать...")
                {
                    ShowWarning("Введите текст для перефразировки");
                    return;
                }

                // Показываем индикатор загрузки
                if (ParaphraseLoadingPanel != null)
                    ParaphraseLoadingPanel.Visibility = Visibility.Visible;
                if (StartParaphraseButton != null)
                    StartParaphraseButton.IsEnabled = false;

                UpdateStatus("Генерация вариантов перефразировки...");

                // Создаем временный результат плагиата для перефразировки
                var tempResult = new PlagiarismResult
                {
                    MatchedText = inputText,
                    StartPosition = 0,
                    EndPosition = inputText.Length,
                    SimilarityPercentage = 100 // Считаем что нужно полностью перефразировать
                };

                var tempResults = new List<PlagiarismResult> { tempResult };

                // Открываем диалог перефразировки с 3 вариантами
                var dialog = new Views.MultiParaphraseDialog(tempResults, _openRouterService);
                dialog.Owner = this;

                if (dialog.ShowDialog() == true)
                {
                    var results = dialog.Results;
                    if (results.ContainsKey(0))
                    {
                        var selectedVariant = results[0];
                        if (!string.IsNullOrEmpty(selectedVariant) && selectedVariant != inputText)
                        {
                            // Заменяем текст в поле ввода на выбранный вариант
                            ParaphraseInputTextBox.Text = selectedVariant;
                            UpdateStatus("Текст успешно перефразирован");
                            ShowInfo("Текст успешно перефразирован! Можете скопировать результат или перефразировать еще раз.");
                        }
                        else
                        {
                            UpdateStatus("Выбран исходный текст");
                        }
                    }
                }
                else
                {
                    UpdateStatus("Перефразировка отменена");
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка перефразировки", ex.Message);
                UpdateStatus("Ошибка при перефразировке");
            }
            finally
            {
                // Скрываем индикатор загрузки
                if (ParaphraseLoadingPanel != null)
                    ParaphraseLoadingPanel.Visibility = Visibility.Collapsed;
                if (StartParaphraseButton != null)
                    StartParaphraseButton.IsEnabled = true;
            }
        }

        private void ClearParaphraseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ParaphraseInputTextBox != null)
                {
                    ParaphraseInputTextBox.Text = "Введите здесь текст, который хотите перефразировать...";
                    ParaphraseInputTextBox.Foreground = new SolidColorBrush(Colors.Gray);
                    ParaphraseInputTextBox.Focus();
                    ParaphraseInputTextBox.SelectAll();
                }
                UpdateStatus("Поле ввода очищено");
            }
            catch (Exception ex)
            {
                ShowError("Ошибка", ex.Message);
            }
        }

        private void ParaphraseInputTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var textBox = sender as System.Windows.Controls.TextBox;
                if (textBox != null && textBox.Text == "Введите здесь текст, который хотите перефразировать...")
                {
                    textBox.Text = "";
                    textBox.Foreground = new SolidColorBrush(Colors.Black);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке фокуса: {ex.Message}");
            }
        }

        private void ParaphraseInputTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var textBox = sender as System.Windows.Controls.TextBox;
                if (textBox != null && string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = "Введите здесь текст, который хотите перефразировать...";
                    textBox.Foreground = new SolidColorBrush(Colors.Gray);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при потере фокуса: {ex.Message}");
            }
        }

        private void ParaphraseInputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                // Ctrl+Enter - запуск перефразировки
                if (e.Key == System.Windows.Input.Key.Enter && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
                {
                    e.Handled = true;
                    if (StartParaphraseButton != null && StartParaphraseButton.IsEnabled)
                    {
                        StartParaphraseButton_Click(StartParaphraseButton, new RoutedEventArgs());
                    }
                }
                // Ctrl+Delete - очистка
                else if (e.Key == System.Windows.Input.Key.Delete && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
                {
                    e.Handled = true;
                    if (ClearParaphraseButton != null)
                    {
                        ClearParaphraseButton_Click(ClearParaphraseButton, new RoutedEventArgs());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки клавиш: {ex.Message}");
            }
        }
        
        private void LogToDiagnostic(string message)
        {
            try
            {
                if (DiagnosticTextBox != null)
                {
                    var timestamp = DateTime.Now.ToString("HH:mm:ss");
                    var logMessage = $"[{timestamp}] {message}\n";
                    
                    Dispatcher.Invoke(() =>
                    {
                        DiagnosticTextBox.AppendText(logMessage);
                        DiagnosticTextBox.ScrollToEnd();
                    });
                }
            }
            catch
            {
                // Игнорируем ошибки логирования
            }
        }
        
        // ДИАГНОСТИКА: Ручной поиск цитат
        private async void FindCitationsManuallyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDocument == null)
            {
                ShowWarning("Сначала загрузите документ");
                return;
            }

            try
            {
                SetLoading(true, "Поиск цитат...");
                LogToDiagnostic("=== РУЧНОЙ ПОИСК ЦИТАТ ===");
                
                // ДИАГНОСТИКА: Проверим что у нас есть в документе
                LogToDiagnostic($"Документ ID: {_currentDocument.Id}");
                LogToDiagnostic($"Название: {_currentDocument.Title}");
                LogToDiagnostic($"Длина контента: {_currentDocument.Content?.Length ?? 0}");
                
                if (string.IsNullOrEmpty(_currentDocument.Content))
                {
                    LogToDiagnostic("ОШИБКА: Контент документа пуст!");
                    ShowError("Ошибка", "Контент документа пуст");
                    return;
                }
                
                // Простой тест с известной цитатой
                var testText = "Как отмечает Иванова Е.П., \"цифровизация образования создала новые вызовы для поддержания академической честности\" (Иванова, 2023, с. 15).";
                LogToDiagnostic("=== ТЕСТИРОВАНИЕ С ИЗВЕСТНЫМ ТЕКСТОМ ===");
                LogToDiagnostic($"Тестовый текст: {testText}");
                
                var testCitations = await _citationService.FindCitationsInTextAsync(testText, _currentDocument.Id);
                LogToDiagnostic($"Найдено цитат в тестовом тексте: {testCitations.Count}");
                
                LogToDiagnostic("=== ТЕСТИРОВАНИЕ С РЕАЛЬНЫМ ДОКУМЕНТОМ ===");
                await FindAndProcessCitations();
                LogToDiagnostic($"Поиск цитат завершен. Найдено: {_citations.Count}");
                UpdateStatus($"Поиск цитат завершен. Найдено: {_citations.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в FindCitationsManuallyButton_Click: {ex.Message}");
                ShowError("Ошибка поиска цитат", ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
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
                if (FindCitationsManuallyButton != null)
                    FindCitationsManuallyButton.IsEnabled = true;

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

            if (DocumentTextBox != null)
            {
                // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Отключаем обработчик события перед загрузкой
                DocumentTextBox.TextChanged -= DocumentTextBox_TextChanged;

                try
                {
                    var content = document.Content ?? "";
                    Console.WriteLine($"Загружаем в TextBox: {content.Length} символов");
                    Console.WriteLine($"Первые 50 символов: '{content.Substring(0, Math.Min(50, content.Length))}'");

                    // ПРОСТОЕ РЕШЕНИЕ: Просто загружаем текст как есть
                    DocumentTextBox.Text = content;

                    Console.WriteLine($"Текст успешно загружен в TextBox");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при загрузке текста в TextBox: {ex.Message}");
                    // Fallback
                    DocumentTextBox.Text = document.Content ?? "";
                }
                finally
                {
                    // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Включаем обработчик обратно
                    DocumentTextBox.TextChanged += DocumentTextBox_TextChanged;
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
                LogToDiagnostic("Начинаем поиск цитат в документе...");
                
                // ДИАГНОСТИКА: Проверяем актуальность контента
                if (DocumentTextBox != null)
                {
                    var currentTextBoxContent = DocumentTextBox.Text;
                    LogToDiagnostic($"Контент в TextBox: {currentTextBoxContent?.Length ?? 0} символов");
                    LogToDiagnostic($"Контент в _currentDocument: {_currentDocument.Content?.Length ?? 0} символов");
                    
                    if (_currentDocument.Content != currentTextBoxContent)
                    {
                        LogToDiagnostic("ВНИМАНИЕ: Контент в TextBox отличается от контента в документе!");
                        LogToDiagnostic("Обновляем контент документа...");
                        _currentDocument.Content = currentTextBoxContent;
                    }
                }
                
                // Находим цитаты в тексте
                var foundCitations = await _citationService.FindCitationsInTextAsync(_currentDocument.Content, _currentDocument.Id);
                LogToDiagnostic($"Найдено цитат: {foundCitations.Count}");
                
                // Очищаем старые цитаты
                _citations.Clear();
                
                // Обрабатываем каждую найденную цитату
                LogToDiagnostic($"Начинаем обработку {foundCitations.Count} найденных цитат...");
                
                for (int i = 0; i < foundCitations.Count; i++)
                {
                    var foundCitation = foundCitations[i];
                    LogToDiagnostic($"Обрабатываем цитату {i+1}/{foundCitations.Count}: {foundCitation.QuotedText.Substring(0, Math.Min(50, foundCitation.QuotedText.Length))}...");
                    
                    try
                    {
                    // Создаем копию цитаты для обработки
                    var citation = foundCitation;
                        LogToDiagnostic($"Цитата создана: ID={citation.Id}, DocumentId={citation.DocumentId}");
                        
                        // УПРОЩАЕМ: Пропускаем поиск источника для диагностики
                        LogToDiagnostic("Пропускаем поиск источника для ускорения диагностики");
                        
                        // УПРОЩАЕМ: Пропускаем сохранение в БД для диагностики
                        LogToDiagnostic("Пропускаем сохранение в БД для диагностики");
                        
                        // Добавляем в коллекцию напрямую
                        _citations.Add(citation);
                        LogToDiagnostic($"Цитата добавлена в коллекцию. Общий счет: {_citations.Count}");
                    }
                    catch (Exception ex)
                    {
                        LogToDiagnostic($"ОШИБКА при обработке цитаты {i+1}: {ex.Message}");
                    }
                }
                
                // ИСПРАВЛЕНО: Сохраняем цитаты в документ
                LogToDiagnostic("Сохраняем найденные цитаты в документ...");
                if (_currentDocument.Citations == null)
                    _currentDocument.Citations = new List<Citation>();
                    
                _currentDocument.Citations.Clear();
                foreach (var citation in _citations)
                {
                    _currentDocument.Citations.Add(citation);
                }
                LogToDiagnostic($"Сохранено {_currentDocument.Citations.Count} цитат в документ");
                
                // Обновляем отображение (НЕ вызываем LoadCitations - они уже в коллекции!)
                LogToDiagnostic("Вызываем LoadBibliography()...");
                LoadBibliography();
                
                // Обновляем отображение цитат
                LogToDiagnostic("Вызываем RefreshCitationsDisplay()...");
                RefreshCitationsDisplay();
                
                LogToDiagnostic($"Обработка цитат завершена. Цитат: {_citations.Count}, Источников: {_bibliography.Count}");
                UpdateStatus($"Найдено цитат: {_citations.Count}, источников: {_bibliography.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при поиске цитат: {ex.Message}");
                //ShowError("Ошибка поиска цитат", ex.Message);
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
                if (DocumentTextBox == null) return;
                
                // Убеждаемся что позиции в пределах текста
                var textLength = DocumentTextBox.Text.Length;
                startPosition = Math.Max(0, Math.Min(startPosition, textLength));
                endPosition = Math.Max(startPosition, Math.Min(endPosition, textLength));
                
                DocumentTextBox.SelectionStart = startPosition;
                DocumentTextBox.SelectionLength = endPosition - startPosition;
                DocumentTextBox.Focus();
                
                // Прокрутка к выделенному тексту
                DocumentTextBox.ScrollToLine(DocumentTextBox.GetLineIndexFromCharacterIndex(startPosition));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка выделения текста: {ex.Message}");
            }
        }
        private void ReplaceTextInEditor(int startPosition, int endPosition, string newText)
        {
            try
            {
                if (DocumentTextBox == null) return;
                
                var textLength = DocumentTextBox.Text.Length;
                startPosition = Math.Max(0, Math.Min(startPosition, textLength));
                endPosition = Math.Max(startPosition, Math.Min(endPosition, textLength));
                
                var originalText = DocumentTextBox.Text;
                var beforeText = originalText.Substring(0, startPosition);
                var afterText = originalText.Substring(endPosition);
                
                DocumentTextBox.Text = beforeText + newText + afterText;
                
                // Обновляем позицию курсора
                DocumentTextBox.SelectionStart = startPosition + newText.Length;
                DocumentTextBox.SelectionLength = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка замены текста: {ex.Message}");
            }
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
            LogToDiagnostic($"LoadCitations: очищаем коллекцию (было {_citations.Count} цитат)");
            _citations.Clear();
            
            if (_currentDocument?.Citations != null)
            {
                LogToDiagnostic($"LoadCitations: найдено {_currentDocument.Citations.Count} цитат в документе");
                foreach (var citation in _currentDocument.Citations)
                {
                    _citations.Add(citation);
                }
            }
            else
            {
                LogToDiagnostic("LoadCitations: _currentDocument.Citations равно null");
            }
            
            LogToDiagnostic($"LoadCitations: итого в коллекции {_citations.Count} цитат");
            
            // Обновляем отображение цитат
            RefreshCitationsDisplay();
        }

        private void LoadBibliography()
        {
            LogToDiagnostic("LoadBibliography: начинаем загрузку библиографии");
            _bibliography.Clear();
            
            if (_currentDocument?.Citations != null)
            {
                LogToDiagnostic($"LoadBibliography: найдено {_currentDocument.Citations.Count} цитат");
                
                // Создаем простую библиографию из цитат
                var counter = 1;
                foreach (var citation in _currentDocument.Citations)
                {
                    // Извлекаем автора из контекста цитаты
                    var author = ExtractAuthorFromContext(citation, _currentDocument.Content);
                    
                    // Создаем псевдо-источник для каждой цитаты
                    var pseudoSource = new Source
                    {
                        Id = counter,
                        Title = citation.QuotedText.Length > 50 
                            ? citation.QuotedText.Substring(0, 47) + "..." 
                            : citation.QuotedText,
                        Author = !string.IsNullOrEmpty(author) ? author : "Автор не указан",
                        Type = SourceType.Unknown,
                        IsComplete = false
                    };
                    
                    _bibliography.Add(pseudoSource);
                    counter++;
                }
                
                LogToDiagnostic($"LoadBibliography: создано {_bibliography.Count} записей в библиографии");
            }
            else
            {
                LogToDiagnostic("LoadBibliography: _currentDocument.Citations равно null");
            }
            
            // Обновляем отображение библиографии
            RefreshBibliographyDisplay();
        }

        private void RefreshBibliographyDisplay()
        {
            try
            {
                LogToDiagnostic("RefreshBibliographyDisplay вызван");
                
                if (BibliographyListBox == null || _bibliography == null)
                {
                    LogToDiagnostic("BibliographyListBox или _bibliography равны null");
                    return;
                }

                LogToDiagnostic($"Количество источников для отображения: {_bibliography.Count}");

                // Форматируем библиографию
                var formattedBibliography = new List<string>();
                for (int i = 0; i < _bibliography.Count; i++)
                {
                    var source = _bibliography[i];
                    var formatted = $"{i + 1}. {source.Title} - {source.Author}";
                    formattedBibliography.Add(formatted);
                    LogToDiagnostic($"📚 Источник {i + 1}: {formatted}");
                }

                // Обновляем отображение
                Dispatcher.Invoke(() =>
                {
                    BibliographyListBox.ItemsSource = formattedBibliography;
                });

                LogToDiagnostic($"Библиография успешно обновлена. Записей: {formattedBibliography.Count}");
            }
            catch (Exception ex)
            {
                LogToDiagnostic($"Ошибка в RefreshBibliographyDisplay: {ex.Message}");
            }
        }

        private string ExtractAuthorFromContext(Citation citation, string documentContent)
        {
            try
            {
                // Получаем контекст вокруг цитаты (100 символов до и после)
                var startPos = Math.Max(0, citation.StartPosition - 100);
                var endPos = Math.Min(documentContent.Length, citation.EndPosition + 100);
                var context = documentContent.Substring(startPos, endPos - startPos);
                
                LogToDiagnostic($"Контекст цитаты: {context.Substring(0, Math.Min(100, context.Length))}...");

                // Паттерны для поиска авторов
                var authorPatterns = new[]
                {
                    // Русские паттерны
                    @"как отмечает\s+([А-ЯЁ][а-яё]+(?:\s+[А-ЯЁ]\.[А-ЯЁ]\.?)?)",
                    @"по мнению\s+([А-ЯЁ][а-яё]+(?:\s+[А-ЯЁ]\.[А-ЯЁ]\.?)?)",
                    @"согласно\s+([А-ЯЁ][а-яё]+(?:\s+[А-ЯЁ]\.[А-ЯЁ]\.?)?)",
                    @"как пишет\s+([А-ЯЁ][а-яё]+(?:\s+[А-ЯЁ]\.[А-ЯЁ]\.?)?)",
                    @"([А-ЯЁ][а-яё]+\s+[А-ЯЁ]\.[А-ЯЁ]\.?)(?:\s+отмечает|\s+утверждает|\s+считает|\s+полагает)",
                    @"автор\s+([А-ЯЁ][а-яё]+(?:\s+[А-ЯЁ]\.[А-ЯЁ]\.?)?)",
                    @"исследователь\s+([А-ЯЁ][а-яё]+(?:\s+[А-ЯЁ]\.[А-ЯЁ]\.?)?)",
                    
                    // Английские паттерны
                    @"according to\s+([A-Z][a-z]+(?:\s+[A-Z]\.[A-Z]\.?)?)",
                    @"([A-Z][a-z]+\s+[A-Z]\.[A-Z]\.?)(?:\s+states|\s+argues|\s+believes)",
                    @"as\s+([A-Z][a-z]+(?:\s+[A-Z]\.[A-Z]\.?)?)\s+notes",
                    
                    // Ссылки в скобках
                    @"\(([А-ЯЁA-Z][а-яёa-z]+(?:\s+[А-ЯЁA-Z]\.[А-ЯЁA-Z]\.?)?),?\s*\d{4}\)",
                    @"\[([А-ЯЁA-Z][а-яёa-z]+(?:\s+[А-ЯЁA-Z]\.[А-ЯЁA-Z]\.?)?),?\s*\d{4}\]"
                };

                foreach (var pattern in authorPatterns)
                {
                    var match = Regex.Match(context, pattern, RegexOptions.IgnoreCase);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        var author = match.Groups[1].Value.Trim();
                        LogToDiagnostic($"Найден автор по паттерну '{pattern}': {author}");
                        return author;
                    }
                }

                LogToDiagnostic("Автор не найден в контексте цитаты");
                return null;
            }
            catch (Exception ex)
            {
                LogToDiagnostic($"Ошибка извлечения автора: {ex.Message}");
                return null;
            }
        }

        private void RefreshCitationsDisplay()
        {
            try
            {
                LogToDiagnostic("RefreshCitationsDisplay вызван");
                
                if (CitationsListBox == null || _citations == null)
                {
                    LogToDiagnostic("CitationsListBox или _citations равны null");
                    return;
                }

                // Получаем выбранный стиль цитирования
                var selectedStyle = CitationStyle.GOST; // По умолчанию
                if (CitationStyleComboBox?.SelectedItem is ComboBoxItem selectedItem)
                {
                    if (Enum.TryParse<CitationStyle>(selectedItem.Tag?.ToString(), out var style))
                    {
                        selectedStyle = style;
                    }
                }

                LogToDiagnostic($"Выбранный стиль: {selectedStyle}");
                LogToDiagnostic($"Количество цитат для отображения: {_citations.Count}");

                // Форматируем цитаты согласно выбранному стилю
                var formattedCitations = new List<string>();
                foreach (var citation in _citations)
                {
                    try
                    {
                        var formatted = _citationService.FormatCitation(citation, selectedStyle);
                        formattedCitations.Add(formatted);
                        LogToDiagnostic($"✅ {citation.Type}: {formatted}");
                    }
                    catch (Exception ex)
                    {
                        LogToDiagnostic($"Ошибка форматирования цитаты: {ex.Message}");
                        formattedCitations.Add($"[Ошибка] {citation.QuotedText.Substring(0, Math.Min(50, citation.QuotedText.Length))}...");
                    }
                }

                // Обновляем отображение
                Dispatcher.Invoke(() =>
                {
                    CitationsListBox.ItemsSource = formattedCitations;
                });

                LogToDiagnostic($"Цитаты успешно обновлены в интерфейсе. Отформатировано: {formattedCitations.Count}");
            }
            catch (Exception ex)
            {
                LogToDiagnostic($"Ошибка в RefreshCitationsDisplay: {ex.Message}");
            }
        }
        private string ShowParaphraseVariantsDialog(string originalText, List<string> variants)
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
