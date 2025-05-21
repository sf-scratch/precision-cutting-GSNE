using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Utils;
using 精密切割系统.ViewModel;

namespace 精密切割系统.Model.plc
{
    public class AlarmConfig
    {
        private static readonly Lazy<AlarmConfig> _lazy = new(() => new AlarmConfig());

        public static AlarmConfig Instance
        {
            get { return _lazy.Value; }
        }

        /// <summary>
        /// 报警起始地址
        /// </summary>
        public string StartAddress { get; set; }

        /// <summary>
        /// 总报警数量
        /// </summary>
        public int TotalAlarmCount { get; set; }

        private readonly AlarmInfo[] _alarmInfos;
        private readonly object _lock = new();
        private bool[]? _newestAlarms;


        private AlarmConfig()
        {
            string alarmConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets\\config\\AlarmConfig.json");
            string allText = File.ReadAllText(alarmConfigPath); 
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,  // 可选：忽略大小写
                Converters = { new JsonStringEnumConverter() }  // 可选：处理枚举
            };
            var configs = JsonSerializer.Deserialize<List<AlarmInfo>>(allText, options);
            if (configs != null)
            {
                _alarmInfos = configs.ToArray();
            }
            else
            {
                _alarmInfos = [];
            }
            TotalAlarmCount = _alarmInfos.Length;
            StartAddress = _alarmInfos.FirstOrDefault()?.Address ?? string.Empty;
            Task.Run(() => StartAlarmMonitoring(default));
        }

        private async Task StartAlarmMonitoring(CancellationToken token)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
            while (await timer.WaitForNextTickAsync(token))
            {
                try
                {
                    _newestAlarms = await PlcControl.tagControl.wholeDevice.ReadTotalAlarmsAsync();
                }
                catch (Exception ex)
                {
                    Tools.LogError($"报警监控异常: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 获取最新数据
        /// </summary>
        /// <returns></returns>
        public bool[]? GetNewestAlarms()
        {
            lock (_lock)
            {
                return _newestAlarms;
            }
        }

        /// <summary>
        /// 是否有激活的报警
        /// </summary>
        /// <returns></returns>
        public bool HasActiveAlarm()
        {
            lock (_lock)
            {
                if (_newestAlarms is null || _newestAlarms.Length == 0 || _newestAlarms.Length != _alarmInfos.Length) return true;
                for (int i = 0; i < _newestAlarms.Length; i++)
                {
                    if (_newestAlarms[i] && _alarmInfos[i].Level != AlarmLevel.None)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 是否有激活的错误报警
        /// </summary>
        /// <returns></returns>
        public bool HasActiveErrorAlarm()
        {
            if (GlobalParams.onlineFlag == false) return false; // 如果不在线，则不检查报警
            lock (_lock)
            {
                if (_newestAlarms is null || _newestAlarms.Length == 0 || _newestAlarms.Length != _alarmInfos.Length) return true;
                for (int i = 0; i < _newestAlarms.Length; i++)
                {
                    if (_newestAlarms[i] && _alarmInfos[i].Level == AlarmLevel.Error)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 获取已激活的报警信息
        /// </summary>
        /// <param name="alarms"></param>
        /// <param name="alarmInfos"></param>
        /// <returns></returns>
        public bool TryGetActiveAlarms(bool[] alarms, out List<AlarmInfo> alarmInfos)
        {
            alarmInfos = new List<AlarmInfo>();
            if (alarms.Length != TotalAlarmCount)
            {
                return false;
            }
            for (int i = 0; i < alarms.Length; i++)
            {
                if (alarms[i] && _alarmInfos[i].Level != AlarmLevel.None)
                {
                    alarmInfos.Add(_alarmInfos[i]);
                }
            }
            return alarmInfos.Count != 0;
        }
    }
}
