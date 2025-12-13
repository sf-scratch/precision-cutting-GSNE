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
    /// <summary>
    /// 通用布尔值转换器
    /// 可以将布尔值转换为任意两种状态
    /// </summary>
    public class BoolToValueConverter : IValueConverter
    {
        public string TrueValue { get; set; }
        public string FalseValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueValue : FalseValue;
            }
            // 处理非布尔值的情况
            return System.Convert.ToBoolean(value) ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string typedValue)
            {
                if (EqualityComparer<string>.Default.Equals(typedValue, TrueValue))
                    return true;

                if (EqualityComparer<string>.Default.Equals(typedValue, FalseValue))
                    return false;
            }

            return DependencyProperty.UnsetValue;
        }
    }
}