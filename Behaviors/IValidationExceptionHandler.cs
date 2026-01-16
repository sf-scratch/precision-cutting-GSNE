using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Behaviors
{
    internal interface IValidationExceptionHandler
    {
        /// <summary>
        /// 是否全部有效
        /// </summary>
        bool IsAllValid
        {
            get;
            set;
        }
    }
}