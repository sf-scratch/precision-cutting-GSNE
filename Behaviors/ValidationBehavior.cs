using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using 精密切割系统.Extensions;

namespace 精密切割系统.Behaviors
{
    public static class ValidationBehavior
    {
        public static readonly DependencyProperty ValidationRulesProperty =
            DependencyProperty.RegisterAttached(
                "ValidationRules",
                typeof(ValidationRuleCollection),
                typeof(ValidationBehavior),
                new PropertyMetadata(null, OnValidationRulesChanged));

        public static ValidationRuleCollection GetValidationRules(DependencyObject obj)
        {
            return (ValidationRuleCollection)obj.GetValue(ValidationRulesProperty);
        }

        public static void SetValidationRules(DependencyObject obj, ValidationRuleCollection value)
        {
            obj.SetValue(ValidationRulesProperty, value);
        }

        private static void OnValidationRulesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox && e.NewValue is ValidationRuleCollection rules)
            {
                var bindingExpression = textBox.GetBindingExpression(TextBox.TextProperty);
                if (bindingExpression?.ParentBinding is Binding originalBinding)
                {
                    var newBinding = originalBinding.CloneWithValidation(rules);
                    textBox.SetBinding(TextBox.TextProperty, newBinding);
                }
            }
        }
    }

    public class ValidationRuleCollection : List<ValidationRule>
    { }
}