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

        private string _message;
        /// <summary>
        /// 异常信息
        /// </summary>
        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        public RunResult(bool isSuccess, string message)
        {
            _isSuccess = isSuccess;
            _message = message;
        }

        public static RunResult Fail(string message)
        {
            return new RunResult(false, message);
        }

        public static RunResult Success()
        {
            return new RunResult(true, string.Empty);
        }
    }
}
