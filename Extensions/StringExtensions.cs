using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;

namespace 精密切割系统.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// 获取字符串在实际显示中的宽度（中文算2个字符，英文算1个）
        /// </summary>
        public static int GetDisplayWidth(this string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            int width = 0;
            foreach (char c in text)
            {
                // 判断是否为中文字符（基本汉字范围）
                if (c >= 0x4E00 && c <= 0x9FFF) // 基本汉字
                    width += 2;
                else if (c >= 0x3400 && c <= 0x4DBF) // 扩展A
                    width += 2;
                else if (c >= 0x20000 && c <= 0x2A6DF) // 扩展B
                    width += 2;
                else
                    width += 1;
            }
            return width;
        }

        /// <summary>
        /// 按实际显示宽度进行右填充
        /// </summary>
        public static string PadRightDisplay(this string text, int totalWidth)
        {
            if (string.IsNullOrEmpty(text))
                return new string(' ', totalWidth);

            int currentWidth = text.GetDisplayWidth();
            int paddingNeeded = totalWidth - currentWidth;

            if (paddingNeeded <= 0)
                return text;

            return text + new string(' ', paddingNeeded);
        }

        /// <summary>
        /// 按实际显示宽度进行左填充
        /// </summary>
        public static string PadLeftDisplay(this string text, int totalWidth)
        {
            if (string.IsNullOrEmpty(text))
                return new string(' ', totalWidth);

            int currentWidth = text.GetDisplayWidth();
            int paddingNeeded = totalWidth - currentWidth;

            if (paddingNeeded <= 0)
                return text;

            return new string(' ', paddingNeeded) + text;
        }

        /// <summary>
        /// 更精确的中文字符判断（包括全角标点）
        /// </summary>
        public static int GetExactDisplayWidth(this string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            int width = 0;
            foreach (char c in text)
            {
                // 判断是否为全角字符（包括中文和全角标点）
                if (c > 0xFF) // 非ASCII字符都算2个宽度
                    width += 2;
                else
                    width += 1;
            }
            return width;
        }
    }
}