using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;

using System.Threading;

namespace 精密切割系统.Helpers
{
    public class ManualPropertyMonitor<T>
    {
        private Timer _timer;
        private List<T> _values;
        private readonly object _lockObject = new object();
        private Func<T> _valueGetter;
        private bool _isMonitoring = false;

        public ManualPropertyMonitor()
        {
            _values = new List<T>();
        }

        /// <summary>
        /// 开始监控属性
        /// </summary>
        /// <param name="valueGetter">获取属性值的方法</param>
        public void StartMonitoring(Func<T> valueGetter, int period)
        {
            if (_isMonitoring)
            {
                throw new InvalidOperationException("监控已在运行中");
            }

            _valueGetter = valueGetter ?? throw new ArgumentNullException(nameof(valueGetter));
            _values.Clear();
            _isMonitoring = true;

            // 创建定时器，每50ms执行一次，立即开始
            _timer = new Timer(CollectValue, null, 0, period);

            Console.WriteLine("属性监控已启动，每50ms采集一次数据...");
        }

        /// <summary>
        /// 停止监控并返回采集的数据
        /// </summary>
        public List<T> StopMonitoring()
        {
            if (!_isMonitoring)
            {
                throw new InvalidOperationException("监控未在运行中");
            }

            _isMonitoring = false;
            _timer?.Dispose();
            _timer = null;

            Console.WriteLine($"属性监控已停止，共采集 {_values.Count} 个数据点");

            // 返回数据的副本，避免外部修改内部集合
            return new List<T>(_values);
        }

        /// <summary>
        /// 获取当前是否正在监控
        /// </summary>
        public bool IsMonitoring => _isMonitoring;

        /// <summary>
        /// 获取当前已采集的数据点数
        /// </summary>
        public int DataCount => _values.Count;

        private void CollectValue(object state)
        {
            if (!_isMonitoring) return;

            try
            {
                lock (_lockObject)
                {
                    T value = _valueGetter();
                    _values.Add(value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"采集数据时发生错误: {ex.Message}");
                // 可以选择停止监控或继续尝试
            }
        }

        /// <summary>
        /// 清空已采集的数据
        /// </summary>
        public void ClearData()
        {
            lock (_lockObject)
            {
                _values.Clear();
            }
        }

        /// <summary>
        /// 获取当前数据的快照（线程安全）
        /// </summary>
        public List<T> GetCurrentData()
        {
            lock (_lockObject)
            {
                return new List<T>(_values);
            }
        }
    }
}