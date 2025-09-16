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
    public partial class MainWindow : Window
    {
        private readonly DocumentService _documentService;
        private readonly AntiPlagiatService _antiPlagiatService;
        private readonly OpenRouterService _openRouterService;
        
        private Document _currentDocument;
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
            
            _plagiarismResults = new ObservableCollection<PlagiarismResult>();
            _citations = new ObservableCollection<Citation>();
            _bibliography = new ObservableCollection<Source>();
            
            InitializeCollections();
            InitializeTimer();
            
            // Настройка темы HandyControl
            ConfigHelper.Instance.SetLang("ru");
        }

        private void InitializeCollections()
        {
            PlagiarismResultsListBox.ItemsSource = _plagiarismResults;
            CitationsListBox.ItemsSource = _citations;
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

        #region Event Handlers - Toolbar

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
            // TODO: Реализовать открытие существующего проекта
            UpdateStatus("Функция в разработке");
        }

        private void SaveProjectButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Реализовать сохранение проекта
            UpdateStatus("Функция в разработке");
        }

        private async void ImportDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите документ для импорта",
                Filter = "Все поддерживаемые|*.docx;*.txt;*.pdf;*.rtf|" +
                        "Word документы (*.docx)|*.docx|" +
                        "Текстовые файлы (*.txt)|*.txt|" +
                        "PDF файлы (*.pdf)|*.pdf|" +
                        "RTF файлы (*.rtf)|*.rtf",
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

        #endregion

        #region Event Handlers - Document Editor

        private void DocumentRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _textChangeTimer.Stop();
            _textChangeTimer.Start();
            
            if (_currentDocument != null)
            {
                _currentDocument.Content = new TextRange(DocumentRichTextBox.Document.ContentStart, 
                                                        DocumentRichTextBox.Document.ContentEnd).Text;
                _currentDocument.LastModified = DateTime.Now;
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

        #endregion

        #region Event Handlers - Results Panel

        private void PlagiarismResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlagiarismResultsListBox.SelectedItem is PlagiarismResult result)
            {
                HighlightTextInEditor(result.StartPosition, result.EndPosition);
            }
        }

        private void CitationStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: Обновить отображение цитат согласно выбранному стилю
            RefreshCitationsDisplay();
        }

        private void ExportBibliographyButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Реализовать экспорт библиографии
            UpdateStatus("Функция в разработке");
        }

        #endregion

        #region Event Handlers - Projects Tree

        private void ProjectsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is Document document)
            {
                LoadDocument(document);
            }
        }

        #endregion

        #region Document Operations

        private async Task ImportDocument(string filePath)
        {
            try
            {
                SetLoading(true, "Импорт документа...");

                var document = await _documentService.ImportDocumentAsync(filePath);
                
                LoadDocument(document);
                
                if (_currentProject != null)
                {
                    _currentProject.Documents.Add(document);
                    UpdateProjectsTree();
                }

                CheckPlagiarismButton.IsEnabled = true;
                UpdateStatus($"Документ '{document.Title}' успешно импортирован");
            }
            catch (Exception ex)
            {
                ShowError("Ошибка импорта", ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void LoadDocument(Document document)
        {
            _currentDocument = document;
            
            DocumentTitleTextBlock.Text = document.Title;
            DocumentInfoTextBlock.Text = $"Создан: {document.CreatedAt:dd.MM.yyyy HH:mm}";
            
            // Загружаем содержимое в RichTextBox
            DocumentRichTextBox.Document.Blocks.Clear();
            
            // Правильно добавляем текст в RichTextBox
            try
            {
                // Создаем новый FlowDocument с правильным форматированием
                var flowDocument = new FlowDocument();
                var paragraph = new Paragraph();
                
                // Разбиваем текст на строки и добавляем их правильно
                var lines = document.Content.Split(new[] { '\n', '\r' }, StringSplitOptions.None);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        paragraph.Inlines.Add(new Run(line));
                    }
                    paragraph.Inlines.Add(new LineBreak());
                }
                
                flowDocument.Blocks.Add(paragraph);
                DocumentRichTextBox.Document = flowDocument;
            }
            catch (Exception ex)
            {
                // Fallback - простой способ
                DocumentRichTextBox.Document.Blocks.Clear();
                DocumentRichTextBox.Document.Blocks.Add(new Paragraph(new Run(document.Content)));
            }
            
            UpdateWordCount();
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
                
                UniquenessProgressBar.Value = uniqueness;
                UniquenessProgressBar.Visibility = Visibility.Visible;
                
                ParaphraseAllButton.IsEnabled = _plagiarismResults.Any(r => r.Level != PlagiarismLevel.Acceptable);

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

        private async Task ParaphraseAllProblematicFragments()
        {
            try
            {
                SetLoading(true, "Перефразирование текста...");

                var problematicResults = _plagiarismResults
                    .Where(r => r.Level != PlagiarismLevel.Acceptable)
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
                        // Показываем диалог выбора варианта
                        var selectedVariant = await ShowParaphraseVariantsDialog(result.MatchedText, variants);
                        if (!string.IsNullOrEmpty(selectedVariant))
                        {
                            ReplaceTextInEditor(result.StartPosition, result.EndPosition, selectedVariant);
                        }
                    }
                }

                // Повторная проверка после перефразирования
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

        #endregion

        #region UI Updates

        private void UpdateWordCount()
        {
            if (_currentDocument != null)
            {
                var words = _currentDocument.Content.Split(new char[] { ' ', '\n', '\r', '\t' }, 
                                                          StringSplitOptions.RemoveEmptyEntries).Length;
                var chars = _currentDocument.Content.Length;

                WordCountTextBlock.Text = $"Слов: {words}";
                CharCountTextBlock.Text = $"Символов: {chars}";
            }
        }

        private void UpdatePlagiarismStatistics()
        {
            var critical = _plagiarismResults.Count(r => r.Level == PlagiarismLevel.Critical);
            var warning = _plagiarismResults.Count(r => r.Level == PlagiarismLevel.Warning);
            var acceptable = _plagiarismResults.Count(r => r.Level == PlagiarismLevel.Acceptable);

            CriticalCountTextBlock.Text = critical.ToString();
            WarningCountTextBlock.Text = warning.ToString();
            AcceptableCountTextBlock.Text = acceptable.ToString();
        }

        private void UpdateProjectsTree()
        {
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
            StatusTextBlock.Text = message;
            AppStatusTextBlock.Text = message;
        }

        private void SetLoading(bool isLoading, string message = "")
        {
            LoadingIndicator.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            if (isLoading && !string.IsNullOrEmpty(message))
            {
                UpdateStatus(message);
            }
        }

        #endregion

        #region Editor Highlighting

        private void HighlightPlagiarismInEditor()
        {
            // TODO: Реализовать подсветку проблемных фрагментов в редакторе
            foreach (var result in _plagiarismResults)
            {
                var color = GetColorByLevel(result.Level);
                // Применить подсветку к тексту
            }
        }

        private void HighlightTextInEditor(int startPosition, int endPosition)
        {
            try
            {
                var start = DocumentRichTextBox.Document.ContentStart.GetPositionAtOffset(startPosition);
                var end = DocumentRichTextBox.Document.ContentStart.GetPositionAtOffset(endPosition);
                
                if (start != null && end != null)
                {
                    DocumentRichTextBox.Selection.Select(start, end);
                    DocumentRichTextBox.ScrollToVerticalOffset(DocumentRichTextBox.Selection.Start.GetCharacterRect(LogicalDirection.Forward).Top);
                }
            }
            catch
            {
                // Игнорируем ошибки позиционирования
            }
        }

        private void ReplaceTextInEditor(int startPosition, int endPosition, string newText)
        {
            try
            {
                var start = DocumentRichTextBox.Document.ContentStart.GetPositionAtOffset(startPosition);
                var end = DocumentRichTextBox.Document.ContentStart.GetPositionAtOffset(endPosition);
                
                if (start != null && end != null)
                {
                    var range = new TextRange(start, end);
                    range.Text = newText;
                }
            }
            catch
            {
                // Игнорируем ошибки замены
            }
        }

        private Color GetColorByLevel(PlagiarismLevel level)
        {
            return level switch
            {
                PlagiarismLevel.Critical => Colors.Red,
                PlagiarismLevel.Warning => Colors.Orange,
                PlagiarismLevel.Acceptable => Colors.Green,
                _ => Colors.Transparent
            };
        }

        #endregion

        #region Data Loading

        private void LoadPlagiarismResults()
        {
            // Загрузка результатов проверки для текущего документа
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
            // Загрузка цитат для текущего документа
            _citations.Clear();
            if (_currentDocument?.Citations != null)
            {
                foreach (var citation in _currentDocument.Citations)
                {
                    _citations.Add(citation);
                }
            }
        }

        private void RefreshCitationsDisplay()
        {
            // TODO: Обновить отображение цитат согласно выбранному стилю
        }

        #endregion

        #region Dialogs

        private async Task<string> ShowParaphraseVariantsDialog(string originalText, List<string> variants)
        {
            // TODO: Создать и показать диалог выбора варианта перефразирования
            // Пока возвращаем первый вариант
            return variants.FirstOrDefault();
        }

        private void ShowError(string title, string message)
        {
            HandyControl.Controls.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowWarning(string message)
        {
            HandyControl.Controls.MessageBox.Show(message, "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ShowInfo(string message)
        {
            HandyControl.Controls.MessageBox.Show(message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Window Events

        protected override void OnClosed(EventArgs e)
        {
            _openRouterService?.Dispose();
            _antiPlagiatService?.Dispose();
            base.OnClosed(e);
        }

        #endregion
    }
}

