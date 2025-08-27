using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.HttpClients
{
    public class HttpUtilsResult<T>
    {
        public static readonly HttpUtilsResult<string> None = new HttpUtilsResult<string>();

        /// <summary>
        /// 结果信息
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public T? Data { get; set; }

        public static HttpUtilsResult<T> Fail(string msg)
        {
            return new HttpUtilsResult<T>
            {
                Msg = msg,
                Data = default
            };
        }

        public static HttpUtilsResult<T> Success(T data)
        {
            return new HttpUtilsResult<T>
            {
                Msg = "操作成功",
                Data = data
            };
        }
    }

    public class HttpUtilsResult
    {
        public static readonly HttpUtilsResult<string> None = new HttpUtilsResult<string>();

        /// <summary>
        /// 结果信息
        /// </summary>
        public string Msg { get; set; }

        public static HttpUtilsResult Fail(string msg)
        {
            return new HttpUtilsResult
            {
                Msg = msg
            };
        }

        public static HttpUtilsResult Success()
        {
            return new HttpUtilsResult
            {
                Msg = "操作成功"
            };
        }
    }
}
