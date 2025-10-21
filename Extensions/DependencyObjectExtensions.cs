using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;

namespace 精密切割系统.Extensions
{
    internal static class DependencyObjectExtensions
    {
        /// <summary>
        /// 表单内容是否错误  false是正常 true是出错了
        /// </summary>
        /// <returns>false表示没有错误，true表示出错了</returns>
        public static bool HasFormError(this DependencyObject dependencyObject)
        {
            bool result = false;
            List<InputTextBox> tbs = Tools.GetChildrenOfType<InputTextBox>(dependencyObject);
            for (int i = 0; i < tbs.Count; i++)
            {
                tbs[i].RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
                bool isError = tbs[i].XIsError;
                if (isError)
                {
                    result = true;
                    break;
                }
            }
            return result;
        }
    }
}