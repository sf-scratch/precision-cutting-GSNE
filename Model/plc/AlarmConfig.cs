using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using 精密切割系统.Model.cut;
using 精密切割系统.Utils;

namespace 精密切割系统.Model.plc
{
    public class AlarmConfig
    {
        private static readonly Lazy<AlarmConfig> _lazy = new(() => new AlarmConfig());

        public static AlarmConfig Instance
        {
            get { return _lazy.Value; }
        }

        public string StartAddress { get; set; }

        public int TotalAlarmCount { get; set; }

        private List<AlarmInfo> _alarmInfos;

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
                _alarmInfos = configs;
            }
            else
            {
                _alarmInfos = new List<AlarmInfo>();
            }
            TotalAlarmCount = _alarmInfos.Count;
            StartAddress = _alarmInfos.FirstOrDefault()?.Address ?? string.Empty;
        }

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
