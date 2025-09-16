using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Plagiat.Models;

namespace Plagiat
{
    public class PlagiarismLevelToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PlagiarismLevel level)
            {
                switch (level)
                {
                    case PlagiarismLevel.Critical:
                        return new SolidColorBrush(Colors.Red);
                    case PlagiarismLevel.Warning:
                        return new SolidColorBrush(Colors.Orange);
                    case PlagiarismLevel.Acceptable:
                        return new SolidColorBrush(Colors.LightGreen);
                }
            }
            return new SolidColorBrush(Colors.LightGray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}