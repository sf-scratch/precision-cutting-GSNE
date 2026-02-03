using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Helpers
{
    //public class TimeoutToken : IDisposable
    //{
    //    private readonly CancellationTokenSource _cts;
    //    public CancellationToken Token { get; }

    //    public TimeoutToken(TimeSpan timeout = default)
    //    {
    //        if (timeout == default)
    //        {
    //            timeout = TimeSpan.FromSeconds(1); // 默认1秒超时
    //        }
    //        _cts = new CancellationTokenSource(timeout);
    //        Token = _cts.Token;
    //    }

    //    public void Dispose() => _cts.Dispose();
    //}

    public class TimeoutToken : IAsyncDisposable
    {
        private readonly CancellationTokenSource? _cts;
        public CancellationToken Token { get; }

        public TimeoutToken(TimeSpan timeout, CancellationToken linkedToken = default)
        {
            _cts = linkedToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(linkedToken, new CancellationTokenSource(timeout).Token)
                : new CancellationTokenSource(timeout);
            Token = _cts.Token;
        }

        public async ValueTask DisposeAsync()
        {
            if (_cts != null)
            {
                await Task.Run(() => _cts.Dispose()); // 避免阻塞UI线程
            }
        }
    }
}