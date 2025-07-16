using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace 精密切割系统.Converters
{
    public class StartsWithSlashConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path)
            {
                // 如果 ImagePath 以 "/" 开头，返回 true（表示隐藏 PackIcon）
                return path.StartsWith("/");
            }
            return Binding.DoNothing; // 默认显示
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
