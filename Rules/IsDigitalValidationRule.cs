using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace 精密切割系统.Rules
{
    internal class IsDigitalValidationRule : ValidationRule
    {
        public int DigitalPlaces { get; set; } = 0;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            ValidationResult result;
            string? dateStr = value as string;
            if (dateStr != null && float.TryParse(dateStr, out float date))
            {
                if (GetDecimalPlacesFromString(dateStr) < DigitalPlaces)
                {
                    result = ValidationResult.ValidResult;
                }
                else
                {
                    result = new ValidationResult(false, $"最大支持小数点后{DigitalPlaces}位");
                }
            }
            else
            {
                result = new ValidationResult(false, "必须为数字");
            }

            return result;
        }

        /// <summary>
        /// 检测字符串的小数位数
        /// </summary>
        public static int GetDecimalPlacesFromString(string numberStr)
        {
            if (string.IsNullOrEmpty(numberStr))
                return 0;

            // 移除千位分隔符和货币符号
            string cleanStr = numberStr.Trim();

            // 查找小数点的位置
            int decimalPointIndex = cleanStr.IndexOf('.');

            if (decimalPointIndex == -1)
                return 0; // 没有小数点

            // 计算小数点后的字符数
            int decimalPlaces = cleanStr.Length - decimalPointIndex - 1;

            // 移除末尾的零（可选）
            while (decimalPlaces > 0 && cleanStr[^1] == '0')
            {
                decimalPlaces--;
                cleanStr = cleanStr[..^1];
            }

            return decimalPlaces;
        }
    }
}