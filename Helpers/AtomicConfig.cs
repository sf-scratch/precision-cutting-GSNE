using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Helpers
{
    public static class AtomicConfig
    {
        private static int _cutProcessingFlag = 0; // 0 = false, 1 = true

        public static bool IsCutProcessing
        {
            get => Interlocked.CompareExchange(ref _cutProcessingFlag, 0, 0) == 1;
            set => Interlocked.Exchange(ref _cutProcessingFlag, value ? 1 : 0);
        }

        // 原子性的检查并设置
        public static bool TrySetCutProcessing()
        {
            return Interlocked.CompareExchange(ref _cutProcessingFlag, 1, 0) == 0;
        }

        // 原子性的设置
        public static void SetCutProcessing(bool value)
        {
            Interlocked.Exchange(ref _cutProcessingFlag, value ? 1 : 0);
        }
    }
}