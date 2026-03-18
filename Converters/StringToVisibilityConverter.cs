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
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue && parameter is string targetValues)
            {
                // 将多个目标值组合成一个字符串，用逗号分隔
                // 例如： "Device551,Device552,Device553"
                string[] targets = targetValues.Split(',', StringSplitOptions.RemoveEmptyEntries);

                // 去除每个目标值两端的空格
                for (int i = 0; i < targets.Length; i++)
                {
                    targets[i] = targets[i].Trim();
                }

                // 判断当前值是否在目标列表中
                bool isMatch = targets.Contains(strValue);

                // 保持原有的逻辑：匹配则显示Visible
                return isMatch ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}