using Plagiat.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Plagiat.Views
{
    public partial class SimpleParaphraseDialog : Window
    {
        private List<string> _variants;
        private string _selectedVariant;
        private readonly OpenRouterService _openRouterService;
        private readonly string _originalText;

        public string SelectedVariant => _selectedVariant;

        public SimpleParaphraseDialog(string originalText, List<string> variants, OpenRouterService openRouterService)
        {
            InitializeComponent();

            _originalText = originalText;
            _variants = variants;
            _openRouterService = openRouterService;

            OriginalTextBlock.Text = originalText;
            DisplayVariants();
        }

        private void DisplayVariants()
        {
            VariantsPanel.Children.Clear();

            for (int i = 0; i < _variants.Count; i++)
            {
                var variant = _variants[i];
                var variantPanel = CreateVariantPanel(variant, i + 1);
                VariantsPanel.Children.Add(variantPanel);
            }

            // Обновляем макет скролла и его содержимого
            EnsureScrollViewerUpdated();

            // Прокручиваем к началу списка вариантов
            ScrollToTop();
        }

        private Border CreateVariantPanel(string variant, int number)
        {
            var border = new Border
            {
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(0),
                BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70)), // #FF3F3F46
                BorderThickness = new Thickness(2),
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)), // #FF2D2D30
                CornerRadius = new CornerRadius(8)
            };

            var stackPanel = new StackPanel();

            // Заголовок с радиокнопкой
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Background = new SolidColorBrush(Color.FromRgb(37, 37, 38)), // #FF252526
                Margin = new Thickness(0, 0, 0, 0)
            };

            var radioButton = new RadioButton
            {
                Content = $"✨ Вариант {number}",
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = Brushes.White,
                GroupName = "ParaphraseVariants",
                Margin = new Thickness(15, 10, 0, 10),
                VerticalAlignment = VerticalAlignment.Center
            };

            radioButton.Checked += (s, e) => {
                _selectedVariant = variant;
                ApplyButton.IsEnabled = true;
                // Подсвечиваем выбранный вариант
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)); // #FF007ACC
                border.BorderThickness = new Thickness(3);
            };

            radioButton.Unchecked += (s, e) => {
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70));
                border.BorderThickness = new Thickness(2);
            };

            headerPanel.Children.Add(radioButton);
            stackPanel.Children.Add(headerPanel);

            // Текст варианта
            var textBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)), // #FF1E1E1E
                BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(15, 0, 15, 10),
                Padding = new Thickness(15)
            };

            var textBlock = new TextBlock
            {
                Text = variant,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.White,
                FontSize = 13,
                LineHeight = 18.0
            };

            textBorder.Child = textBlock;
            stackPanel.Children.Add(textBorder);

            // Статистика
            var changes = CalculateChanges(_originalText, variant);
            var statsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(15, 0, 15, 15),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var statsText = new TextBlock
            {
                Text = $"📊 Изменений: {changes.Changes} | 📏 Длина: {variant.Length} символов | 📝 Слов: {variant.Split(' ').Length}",
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                FontSize = 11,
                FontWeight = FontWeights.Normal
            };

            statsPanel.Children.Add(statsText);
            stackPanel.Children.Add(statsPanel);

            border.Child = stackPanel;
            return border;
        }

        private (int Changes, double Similarity) CalculateChanges(string original, string variant)
        {
            var originalWords = original.Split(' ');
            var variantWords = variant.Split(' ');

            int changes = Math.Abs(originalWords.Length - variantWords.Length);

            for (int i = 0; i < Math.Min(originalWords.Length, variantWords.Length); i++)
            {
                if (originalWords[i] != variantWords[i])
                {
                    changes++;
                }
            }

            double similarity = 1.0 - (double)changes / Math.Max(originalWords.Length, variantWords.Length);
            return (changes, similarity);
        }

        private async void RegenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RegenerateButton.IsEnabled = false;
                RegenerateButton.Content = "🔄 Генерация...";
                RegenerateButton.Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150));

                // Показываем индикатор загрузки
                var loadingText = new TextBlock
                {
                    Text = "⏳ Генерируем новые варианты перефразирования...",
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 20)
                };

                VariantsPanel.Children.Clear();
                VariantsPanel.Children.Add(loadingText);

                // Прокручиваем к началу при показе загрузки
                EnsureScrollViewerUpdated();
                ScrollToTop();

                var options = new ParaphraseOptions
                {
                    Style = ParaphraseStyle.Academic,
                    Level = ParaphraseLevel.Medium
                };

                var newVariants = await _openRouterService.ParaphraseTextAsync(_originalText, options);
                if (newVariants.Count > 0)
                {
                    _variants = newVariants;
                    DisplayVariants();
                    _selectedVariant = null;
                    ApplyButton.IsEnabled = false;
                }
                else
                {
                    var errorText = new TextBlock
                    {
                        Text = "❌ Не удалось сгенерировать новые варианты",
                        Foreground = new SolidColorBrush(Color.FromRgb(255, 100, 100)),
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 20, 0, 20)
                    };
                    VariantsPanel.Children.Clear();
                    VariantsPanel.Children.Add(errorText);

                    // Обновляем скролл при показе ошибки
                    EnsureScrollViewerUpdated();
                    ScrollToTop();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации новых вариантов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                // Показываем ошибку в панели
                var errorText = new TextBlock
                {
                    Text = $"❌ Ошибка: {ex.Message}",
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 100, 100)),
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 20),
                    TextWrapping = TextWrapping.Wrap
                };
                VariantsPanel.Children.Clear();
                VariantsPanel.Children.Add(errorText);

                // Обновляем скролл при показе ошибки
                EnsureScrollViewerUpdated();
                ScrollToTop();
            }
            finally
            {
                RegenerateButton.IsEnabled = true;
                RegenerateButton.Content = "🔄 Новые варианты";
                RegenerateButton.Foreground = Brushes.White;
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ScrollToTop()
        {
            // Прокручиваем к началу списка вариантов
            if (VariantsScrollViewer != null)
            {
                VariantsScrollViewer.ScrollToTop();
            }
        }

        private void ScrollToBottom()
        {
            // Прокручиваем к концу списка вариантов
            if (VariantsScrollViewer != null)
            {
                VariantsScrollViewer.ScrollToBottom();
            }
        }

        private void EnsureScrollViewerUpdated()
        {
            // Обновляем макет и размеры содержимого скролла
            if (VariantsScrollViewer != null)
            {
                VariantsScrollViewer.UpdateLayout();
                VariantsPanel.UpdateLayout();
            }
        }
    }
}

