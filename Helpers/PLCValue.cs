using MathNet.Numerics.Random;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using 精密切割系统.database.db.modle;
using 精密切割系统.ViewModel;

namespace 精密切割系统.Helpers
{
    /// <summary>
    /// 极简PLC值管理器
    /// </summary>
    public static class PLCValue
    {
        private static readonly ConcurrentDictionary<string, short> _values = new ConcurrentDictionary<string, short>();

        static PLCValue()
        {
            _values.TryAdd("DM2000", 0);
            Task.Factory.StartNew(UpdateNewestPLCValueAsync, TaskCreationOptions.LongRunning);
        }

        private static async Task UpdateNewestPLCValueAsync()
        {
            //Random rand = new Random();
            while (true)
            {
                foreach (var key in _values.Keys.ToList())
                {
                    // 模拟PLC值的变化
                    //short newValue = (short)(rand.NextInt64()); // 随机生成0到100之间的值

                    short newValue = await PlcControl.plc.ReadDataAsync<short>(key) ?? 0;
                    _values[key] = newValue;

                    //Stopwatch stopwatch = Stopwatch.StartNew();
                    //short newValue = await PlcControl.plc.ReadDataAsync<short>(key) ?? 0;
                    //stopwatch.Stop();
                    //TimeSpan timeSpan = TimeSpan.FromSeconds(stopwatch.Elapsed.TotalMilliseconds);
                    //_values[key] = (short)stopwatch.ElapsedMilliseconds;
                }
                await Task.Delay(50); // 每秒更新一次
            }
        }

        public static short SlightVibration
        {
            get => Get("DM2000");
            set => Set("DM2000", value);
        }

        /// <summary>
        /// 设置PLC值
        /// </summary>
        public static void Set(string address, short value)
        {
            _values.AddOrUpdate(address, value, (key, oldValue) => value);
        }

        /// <summary>
        /// 获取PLC值
        /// </summary>
        public static short Get(string address, short defaultValue = 0)
        {
            return _values.TryGetValue(address, out short value) ? value : defaultValue;
        }
    }
}