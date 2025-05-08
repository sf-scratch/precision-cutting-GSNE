using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut
{
    public class RunResult
    {
        private bool _isSuccess;
        /// <summary>
        /// 是否执行成功
        /// </summary>
        public bool IsSuccess
        {
            get { return _isSuccess; }
            set { _isSuccess = value; }
        }

        private RunExceptionType _type;
        /// <summary>
        /// 运行异常类型
        /// </summary>
        public RunExceptionType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        private string _message;
        /// <summary>
        /// 异常信息
        /// </summary>
        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        public RunResult(bool isSuccess, RunExceptionType type, string message)
        {
            _isSuccess = isSuccess;
            _type = type;
            _message = message;
        }

        public static RunResult Fail(RunExceptionType type, string message)
        {
            return new RunResult(false, type, message);
        }

        public static RunResult Success()
        {
            return new RunResult(true, RunExceptionType.None, string.Empty);
        }
    }

    public enum RunExceptionType
    {
        /// <summary>
        /// 无异常
        /// </summary>
        None,

        /// <summary>
        /// 刀片报废
        /// </summary>
        BladeScrap,

        /// <summary>
        /// 停止
        /// </summary>
        Stop,

        /// <summary>
        /// 其他
        /// </summary>
        Other
    }
}
