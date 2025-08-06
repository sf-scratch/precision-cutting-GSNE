using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Extensions
{
    public static class AsyncExtensions
    {
        /// <summary>
        /// 为CancellationToken添加默认超时
        /// </summary>
        /// <param name="token">原始CancellationToken</param>
        /// <param name="timeout">默认超时时间</param>
        /// <returns>带超时的CancellationToken</returns>
        public static CancellationToken WithDefaultTimeout(this CancellationToken token, TimeSpan timeout = default)
        {
            if (timeout == default)
            {
                timeout = TimeSpan.FromSeconds(1); // 默认1秒超时
            }

            if (token == CancellationToken.None || !token.CanBeCanceled)
            {
                var cts = new CancellationTokenSource(timeout);
                return cts.Token;
            }

            return token;
        }
    }
}
