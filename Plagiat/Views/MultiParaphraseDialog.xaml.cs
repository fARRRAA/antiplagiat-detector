using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Plagiat.Models;
using Plagiat.Services;

namespace Plagiat.Views
{
    public partial class MultiParaphraseDialog : Window
    {
        private readonly List<PlagiarismResult> _fragments;
        private readonly OpenRouterService _openRouterService;
        private int _currentIndex = 0;
        private List<string> _currentVariants = new List<string>();
        private readonly Dictionary<int, string> _selectedTexts = new Dictionary<int, string>();
        
        public Dictionary<int, string> Results => _selectedTexts;
        
        public MultiParaphraseDialog(List<PlagiarismResult> fragments, OpenRouterService openRouterService)
        {
            InitializeComponent();
            _fragments = fragments ?? throw new ArgumentNullException(nameof(fragments));
            _openRouterService = openRouterService ?? throw new ArgumentNullException(nameof(openRouterService));
            
            if (_fragments.Count == 0)
            {
                throw new ArgumentException("Список фрагментов не может быть пустым", nameof(fragments));
            }
            
            LoadCurrentFragment();
        }
        
        private async void LoadCurrentFragment()
        {
            try
            {
                UpdateUI();
                await GenerateVariants();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки фрагмента: {ex.Message}", "Ошибка", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void UpdateUI()
        {
            var current = _fragments[_currentIndex];
            
            // Обновляем прогресс
            ProgressTextBlock.Text = $"Фрагмент {_currentIndex + 1} из {_fragments.Count}";
            ProgressBar.Value = (double)(_currentIndex + 1) / _fragments.Count * 100;
            
            // Обновляем текст
            OriginalTextBox.Text = current.MatchedText;
            
            // Обновляем кнопки
            PreviousButton.IsEnabled = _currentIndex > 0;
            ApplyButton.IsEnabled = false;
            
            // Очищаем варианты
            ClearVariants();
            
            // Показываем статус загрузки
            StatusTextBlock.Text = "🔄 Генерация...";
            LoadingIndicator.Visibility = Visibility.Visible;
        }
        
        private void ClearVariants()
        {
            Variant1RadioButton.Content = "Загрузка...";
            Variant1RadioButton.IsChecked = false;
            Variant1RadioButton.IsEnabled = false;
            
            Variant2RadioButton.Content = "";
            Variant2RadioButton.IsChecked = false;
            var variant2Border = this.FindName("Variant2Border") as FrameworkElement;
            if (variant2Border != null)
                variant2Border.Visibility = Visibility.Collapsed;
            
            Variant3RadioButton.Content = "";
            Variant3RadioButton.IsChecked = false;
            var variant3Border = this.FindName("Variant3Border") as FrameworkElement;
            if (variant3Border != null)
                variant3Border.Visibility = Visibility.Collapsed;
            
            KeepOriginalRadioButton.IsChecked = false;
        }
        
        private async Task GenerateVariants()
        {
            try
            {
                var current = _fragments[_currentIndex];
                var options = new ParaphraseOptions
                {
                    Style = ParaphraseStyle.Academic,
                    Level = ParaphraseLevel.Medium
                };
                
                _currentVariants = await _openRouterService.ParaphraseTextAsync(current.MatchedText, options);
                
                DisplayVariants();
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "❌ Ошибка API";
                LoadingIndicator.Visibility = Visibility.Collapsed;
                
                // Показываем только оригинальный текст
                Variant1RadioButton.Content = "Ошибка генерации вариантов";
                Variant1RadioButton.IsEnabled = false;
                KeepOriginalRadioButton.IsChecked = true;
                ApplyButton.IsEnabled = true;
            }
        }
        
        private void DisplayVariants()
        {
            LoadingIndicator.Visibility = Visibility.Collapsed;
            
            if (_currentVariants.Count == 0)
            {
                StatusTextBlock.Text = "❌ Ошибка генерации";
                Variant1RadioButton.Content = "Варианты не сгенерированы";
                Variant1RadioButton.IsEnabled = false;
                KeepOriginalRadioButton.IsChecked = true;
                ApplyButton.IsEnabled = true;
                return;
            }
            
            StatusTextBlock.Text = "✅ Готово";
            
            // Показываем варианты
            for (int i = 0; i < Math.Min(_currentVariants.Count, 3); i++)
            {
                RadioButton radioButton = null;
                FrameworkElement border = null;
                
                switch (i)
                {
                    case 0:
                        radioButton = Variant1RadioButton;
                        // Вариант 1 всегда видим
                        break;
                    case 1:
                        radioButton = Variant2RadioButton;
                        border = this.FindName("Variant2Border") as FrameworkElement;
                        break;
                    case 2:
                        radioButton = Variant3RadioButton;
                        border = this.FindName("Variant3Border") as FrameworkElement;
                        break;
                }
                
                if (radioButton != null)
                {
                    radioButton.Content = _currentVariants[i];
                    radioButton.IsEnabled = true;
                    
                    if (border != null)
                    {
                        border.Visibility = Visibility.Visible;
                    }
                    
                    if (i == 0)
                    {
                        radioButton.IsChecked = true;
                        ApplyButton.IsEnabled = true;
                    }
                }
            }
        }
        
        private void VariantRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            ApplyButton.IsEnabled = true;
        }
        
        // Обработчики для индикаторов радиокнопок
        private void Variant1RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var indicator = this.FindName("Variant1Indicator") as FrameworkElement;
            if (indicator != null) indicator.Visibility = Visibility.Visible;
            ApplyButton.IsEnabled = true;
        }
        
        private void Variant1RadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            var indicator = this.FindName("Variant1Indicator") as FrameworkElement;
            if (indicator != null) indicator.Visibility = Visibility.Collapsed;
        }
        
        private void Variant2RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var indicator = this.FindName("Variant2Indicator") as FrameworkElement;
            if (indicator != null) indicator.Visibility = Visibility.Visible;
            ApplyButton.IsEnabled = true;
        }
        
        private void Variant2RadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            var indicator = this.FindName("Variant2Indicator") as FrameworkElement;
            if (indicator != null) indicator.Visibility = Visibility.Collapsed;
        }
        
        private void Variant3RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var indicator = this.FindName("Variant3Indicator") as FrameworkElement;
            if (indicator != null) indicator.Visibility = Visibility.Visible;
            ApplyButton.IsEnabled = true;
        }
        
        private void Variant3RadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            var indicator = this.FindName("Variant3Indicator") as FrameworkElement;
            if (indicator != null) indicator.Visibility = Visibility.Collapsed;
        }
        
        private void KeepOriginalRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var indicator = this.FindName("KeepOriginalIndicator") as FrameworkElement;
            if (indicator != null) indicator.Visibility = Visibility.Visible;
            ApplyButton.IsEnabled = true;
        }
        
        private void KeepOriginalRadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            var indicator = this.FindName("KeepOriginalIndicator") as FrameworkElement;
            if (indicator != null) indicator.Visibility = Visibility.Collapsed;
        }
        
        // Обработчики кликов по индикаторам
        private void Variant1Border_Click(object sender, MouseButtonEventArgs e)
        {
            Variant1RadioButton.IsChecked = true;
        }
        
        private void Variant2Border_Click(object sender, MouseButtonEventArgs e)
        {
            Variant2RadioButton.IsChecked = true;
        }
        
        private void Variant3Border_Click(object sender, MouseButtonEventArgs e)
        {
            Variant3RadioButton.IsChecked = true;
        }
        
        private void KeepOriginalBorder_Click(object sender, MouseButtonEventArgs e)
        {
            KeepOriginalRadioButton.IsChecked = true;
        }
        
        private string GetSelectedText()
        {
            if (Variant1RadioButton.IsChecked == true && _currentVariants.Count > 0)
                return _currentVariants[0];
            if (Variant2RadioButton.IsChecked == true && _currentVariants.Count > 1)
                return _currentVariants[1];
            if (Variant3RadioButton.IsChecked == true && _currentVariants.Count > 2)
                return _currentVariants[2];
            if (KeepOriginalRadioButton.IsChecked == true)
                return _fragments[_currentIndex].MatchedText;
            
            return _fragments[_currentIndex].MatchedText; // По умолчанию оригинал
        }
        
        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
                LoadCurrentFragment();
            }
        }
        
        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            // Сохраняем оригинальный текст как выбранный
            _selectedTexts[_currentIndex] = _fragments[_currentIndex].MatchedText;
            AutoMoveToNext();
        }
        
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedText = GetSelectedText();
            _selectedTexts[_currentIndex] = selectedText;
            AutoMoveToNext();
        }
        
        private void AutoMoveToNext()
        {
            if (_currentIndex < _fragments.Count - 1)
            {
                // Переходим к следующему фрагменту
                _currentIndex++;
                LoadCurrentFragment();
            }
            else
            {
                // Это был последний фрагмент - завершаем
                DialogResult = true;
                Close();
            }
        }
        
        private void MoveToNext()
        {
            if (_currentIndex < _fragments.Count - 1)
            {
                _currentIndex++;
                LoadCurrentFragment();
            }
            else
            {
                // Завершаем процесс
                DialogResult = true;
                Close();
            }
        }
        
        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTexts.Count < _fragments.Count)
            {
                var result = MessageBox.Show(
                    $"Обработано {_selectedTexts.Count} из {_fragments.Count} фрагментов.\nЗавершить перефразирование?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result != MessageBoxResult.Yes)
                    return;
            }
            
            DialogResult = true;
            Close();
        }
    }
}
