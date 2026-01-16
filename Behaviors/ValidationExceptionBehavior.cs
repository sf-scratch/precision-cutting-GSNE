using Microsoft.Xaml.Behaviors;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using 精密切割系统.Utils;

namespace 精密切割系统.Behaviors
{
    /// <summary>
    /// 验证行为类,可以获得附加到的对象
    /// </summary>
    public class ValidationExceptionBehavior : Behavior<FrameworkElement>
    {
        /// <summary>
        /// 错误计数器
        /// </summary>
        private int _validationExceptionCount = 0;

        private EventHandler<ValidationErrorEventArgs> _validationHandler;

        /// <summary>
        /// 附加对象时
        /// </summary>
        protected override void OnAttached()
        {
            _validationHandler = new EventHandler<ValidationErrorEventArgs>(this.OnValidationError);
            //附加对象时，给对象增加一个监听验证错误事件的能力，注意该事件是冒泡的
            this.AssociatedObject.AddHandler(Validation.ErrorEvent, _validationHandler);
        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.RemoveHandler(Validation.ErrorEvent, _validationHandler);
        }

        #region 获取实现接口的对象

        /// <summary>
        /// 获取对象
        /// </summary>
        /// <returns></returns>
        private IValidationExceptionHandler? GetValidationExceptionHandler()
        {
            if (this.AssociatedObject.DataContext is IValidationExceptionHandler handler)
            {
                return handler;
            }
            return null;
        }

        #endregion 获取实现接口的对象

        #region 验证事件方法

        /// <summary>
        /// 验证事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnValidationError(object? sender, ValidationErrorEventArgs e)
        {
            try
            {
                var handler = GetValidationExceptionHandler();

                var element = e.OriginalSource as UIElement;

                if (handler == null || element == null)
                    return;

                if (e.Action == ValidationErrorEventAction.Added)
                {
                    _validationExceptionCount++;
                }
                else if (e.Action == ValidationErrorEventAction.Removed)
                {
                    _validationExceptionCount--;
                }
                handler.IsAllValid = _validationExceptionCount == 0;
            }
            catch (Exception ex)
            {
                Tools.LogDebug($"ValidationExceptionBehavior OnValidationError异常:{ex}");
            }
        }

        #endregion 验证事件方法
    }
}