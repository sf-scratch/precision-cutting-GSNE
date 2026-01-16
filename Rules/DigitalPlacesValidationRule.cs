using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace 精密切割系统.Rules
{
    internal class DigitalPlacesValidationRule : ValidationRule
    {
        public int DigitalPlaces { get; set; } = 0;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            ValidationResult result;
            string? dateStr = value as string;
            if (dateStr != null && float.TryParse(dateStr, out float date))
            {
                result = ValidationResult.ValidResult;
            }
            else
            {
                result = new ValidationResult(false, "必须为数字");
            }

            return result;
        }
    }
}