using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace 精密切割系统.Converters
{
    public class BooleanToColorConverter : IValueConverter
    {
        public Color TrueColor { get; set; } = Colors.Blue;
        public Color FalseColor { get; set; } = Colors.Transparent;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? new SolidColorBrush(TrueColor) : new SolidColorBrush(FalseColor);
            }
            return new SolidColorBrush(FalseColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}