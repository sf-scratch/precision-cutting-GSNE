using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Helpers
{
    public class CommonResult
    {
        public bool IsSuccess { get; protected set; }
        public string Message { get; protected set; }

        protected CommonResult(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        public static CommonResult Success(string message = "")
            => new CommonResult(true, message);

        public static CommonResult Failure(string message)
            => new CommonResult(false, message);
    }

    public class CommonResult<T>
    {
        public bool IsSuccess { get; }
        public string Message { get; }
        public T? Data { get; }

        private CommonResult(bool success, string message, T? data)
        {
            IsSuccess = success;
            Message = message;
            Data = data;
        }

        public static CommonResult<T> Success(T data, string? message = null)
            => new CommonResult<T>(true, message ?? string.Empty, data);

        public static CommonResult<T> Failure(string message)
            => new CommonResult<T>(false, message, default);

        public static CommonResult<T> Error(Exception ex)
            => new CommonResult<T>(false, ex.Message, default);
    }
}
