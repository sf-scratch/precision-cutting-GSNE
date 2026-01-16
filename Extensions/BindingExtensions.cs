using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using 精密切割系统.Behaviors;

namespace 精密切割系统.Extensions
{
    public static class BindingExtensions
    {
        public static Binding CloneWithValidation(this Binding original, ValidationRuleCollection rules)
        {
            var newBinding = new Binding
            {
                Path = original.Path,
                UpdateSourceTrigger = original.UpdateSourceTrigger,
                Mode = original.Mode,
                Converter = original.Converter,
                ConverterParameter = original.ConverterParameter,
                ConverterCulture = original.ConverterCulture,
                StringFormat = original.StringFormat,
                TargetNullValue = original.TargetNullValue,
                FallbackValue = original.FallbackValue,
                BindsDirectlyToSource = original.BindsDirectlyToSource,
                NotifyOnValidationError = true,
                ValidatesOnDataErrors = original.ValidatesOnDataErrors,
                ValidatesOnExceptions = original.ValidatesOnExceptions
            };

            // 根据原始 Binding 的设置条件性复制
            if (original.Source != null)
            {
                newBinding.Source = original.Source;
            }
            else if (original.RelativeSource != null)
            {
                newBinding.RelativeSource = original.RelativeSource;
            }
            else if (!string.IsNullOrEmpty(original.ElementName))
            {
                newBinding.ElementName = original.ElementName;
            }

            // 添加验证规则
            if (rules != null)
            {
                foreach (var rule in rules)
                {
                    newBinding.ValidationRules.Add(rule);
                }
            }

            return newBinding;
        }
    }
}