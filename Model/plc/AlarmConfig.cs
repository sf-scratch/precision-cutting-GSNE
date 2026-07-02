using DryIoc.ImTools;
using HslCommunication.BasicFramework;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Documents;
using 精密切割系统.Helpers;
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

        public const string HasErrorAlarmMessage = "存在未处理的告警，请先处理告警！";

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
            if (!GlobalParams.OnlineFlag)
            {
                return; // 如果不在线，则不初始化报警配置
            }
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
            //Task.Factory.StartNew(() => StartAlarmMonitoring(default), TaskCreationOptions.LongRunning);
        }

        private async Task StartAlarmMonitoring(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    //_newestAlarms = await PlcControl.tagControl.wholeDevice.ReadTotalAlarmsAsync();
                }
                catch (Exception ex)
                {
                    Tools.LogError($"报警监控异常: {ex.Message}");
                }
                await Task.Delay(200);
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
        /// 目标地址是否有激活的报警
        /// </summary>
        /// <param name="targetAddres"></param>
        /// <returns></returns>
        public bool HasTargetActiveAlarm(out bool[]? newestAlarms, params string[] targetAddresses)
        {
            newestAlarms = null;

            // 1. 快速失败检查
            if (!GlobalParams.OnlineFlag)
                return false;

            lock (_lock)
            {
                // 2. 检查数据有效性
                if (_newestAlarms == null || _newestAlarms.Length == 0 || _alarmInfos == null)
                    return false;

                if (_newestAlarms.Length != _alarmInfos.Length)
                    return false;

                // 3. 如果没有指定目标地址，则不检查
                if (targetAddresses == null || targetAddresses.Length == 0)
                    return false;

                // 4. 使用 HashSet 提高查找性能（只创建一次）
                var targetAddressSet = new HashSet<string>(targetAddresses, StringComparer.OrdinalIgnoreCase);

                // 5. 预分配数组（但只在需要时填充）
                bool[]? tempAlarms = null;
                bool foundAlarm = false;

                // 6. 遍历检查
                for (int i = 0; i < _newestAlarms.Length; i++)
                {
                    // 快速检查：首先检查是否为有效报警
                    if (!_newestAlarms[i] || _alarmInfos[i].Level == AlarmLevel.None)
                        continue;

                    // 检查地址是否在目标列表中
                    if (targetAddressSet.Contains(_alarmInfos[i].Address))
                    {
                        // 延迟创建数组，只在找到报警时才创建
                        tempAlarms ??= new bool[_newestAlarms.Length];
                        tempAlarms[i] = true;
                        foundAlarm = true;
                    }
                }

                // 7. 设置输出参数并返回
                if (foundAlarm)
                {
                    newestAlarms = tempAlarms;
                }

                return foundAlarm;
            }
        }

        public bool HasTargetActiveAlarm(params string[] targetAddresses)
        {
            // 1. 快速失败检查
            if (!GlobalParams.OnlineFlag)
                return false;

            lock (_lock)
            {
                // 2. 检查数据有效性
                if (_newestAlarms == null || _newestAlarms.Length == 0 || _alarmInfos == null)
                    return false;

                if (_newestAlarms.Length != _alarmInfos.Length)
                    return false;

                // 3. 如果没有指定目标地址，则不检查
                if (targetAddresses == null || targetAddresses.Length == 0)
                    return false;

                // 4. 使用 HashSet 提高查找性能
                var targetAddressSet = new HashSet<string>(targetAddresses, StringComparer.OrdinalIgnoreCase);

                // 5. 遍历检查，找到第一个符合条件的报警就立即返回
                for (int i = 0; i < _newestAlarms.Length; i++)
                {
                    // 快速检查：首先检查是否为有效报警
                    if (!_newestAlarms[i] || _alarmInfos[i].Level == AlarmLevel.None)
                        continue;

                    // 检查地址是否在目标列表中
                    if (targetAddressSet.Contains(_alarmInfos[i].Address))
                    {
                        return true; // 找到符合条件的报警，立即返回
                    }
                }

                // 6. 没有找到符合条件的报警
                return false;
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
        public bool HasActiveErrorAlarm(params string[] notCheckedAddres)
        {
            if (!GlobalParams.OnlineFlag) return false; // 如果不在线，则不检查报警

            lock (_lock)
            {
                if (_newestAlarms == null || _newestAlarms.Length == 0 || _newestAlarms.Length != _alarmInfos.Length) return true;

                // 将排除地址转换为HashSet提高查找性能
                var excludeAddresses = notCheckedAddres?.Length > 0 ? new HashSet<string>(notCheckedAddres) : null;

                for (int i = 0; i < _newestAlarms.Length; i++)
                {
                    if (_newestAlarms[i] && _alarmInfos[i].Level == AlarmLevel.Error && (excludeAddresses == null || !excludeAddresses.Contains(_alarmInfos[i].Address)))
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
        public bool HasActiveErrorAlarm(out bool[]? newestAlarms, params string[] excludeAddresses)
        {
            newestAlarms = null;

            // 1. 快速失败：不在线直接返回
            if (!GlobalParams.OnlineFlag)
                return false;

            lock (_lock)
            {
                // 2. 数据有效性检查（修正了逻辑错误：原代码返回 true 应该是 false）
                if (_newestAlarms == null || _newestAlarms.Length == 0 || _alarmInfos == null || _newestAlarms.Length != _alarmInfos.Length)
                    return false;

                // 3. 准备排除集合（延迟创建）
                HashSet<string>? excludeSet = null;
                if (excludeAddresses != null && excludeAddresses.Length > 0)
                {
                    excludeSet = new HashSet<string>(excludeAddresses, StringComparer.OrdinalIgnoreCase);
                }

                // 4. 延迟创建结果数组
                bool[]? resultArray = null;
                bool hasErrorAlarm = false;
                int length = _newestAlarms.Length;

                // 5. 优化循环：使用局部变量减少数组访问
                bool[] localAlarms = _newestAlarms;
                AlarmInfo[] localInfos = _alarmInfos;

                for (int i = 0; i < length; i++)
                {
                    // 按检查成本排序条件：先检查最简单的条件
                    if (!localAlarms[i]) continue;                    // 1. 没有报警
                    if (localInfos[i].Level != AlarmLevel.Error) continue; // 2. 不是错误级别

                    // 3. 检查排除列表（最耗时的检查放在最后）
                    if (excludeSet != null && excludeSet.Contains(localInfos[i].Address))
                        continue;

                    // 找到符合条件的报警，创建数组并标记
                    resultArray ??= new bool[length];
                    resultArray[i] = true;
                    hasErrorAlarm = true;
                }

                // 6. 设置输出参数
                if (hasErrorAlarm)
                {
                    newestAlarms = resultArray;
                }

                return hasErrorAlarm;
            }
        }

        /// <summary>
        /// 是否有激活的错误报警
        /// </summary>
        /// <returns></returns>
        public bool HasActiveErrorAlarm(bool alarmReadExceptionDefaultValue = true)
        {
            if (!GlobalParams.OnlineFlag) return false; // 如果不在线，则不检查报警

            lock (_lock)
            {
                if (_newestAlarms == null || _newestAlarms.Length == 0 || _newestAlarms.Length != _alarmInfos.Length) return alarmReadExceptionDefaultValue;
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

        public bool HasTargetActiveErrorAlarm(params string[] targetAddres)
        {
            if (!GlobalParams.OnlineFlag) return false; // 如果不在线，则不检查报警
            lock (_lock)
            {
                if (_newestAlarms == null || _newestAlarms.Length == 0 || _newestAlarms.Length != _alarmInfos.Length) return true;

                var targetAddresses = targetAddres?.Length > 0 ? new HashSet<string>(targetAddres) : null;

                for (int i = 0; i < _newestAlarms.Length; i++)
                {
                    if (_newestAlarms[i] && _alarmInfos[i].Level == AlarmLevel.Error && (targetAddresses != null && targetAddresses.Contains(_alarmInfos[i].Address)))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 是否有轴报警
        /// </summary>
        /// <returns></returns>
        public bool HasActiveAxisAlarm()
        {
            if (GlobalParams.OnlineFlag == false) return false; // 如果不在线，则不检查报警
            lock (_lock)
            {
                if (_newestAlarms is null || _newestAlarms.Length == 0 || _newestAlarms.Length != _alarmInfos.Length) return true;
                var indexes = _alarmInfos.Select((info, index) => new { info, index })
                    .Where(x => x.info.Address == "MR61000" || x.info.Address == "MR61100" || x.info.Address == "MR61200" || x.info.Address == "MR61300" || x.info.Address == "MR61400")
                    .Select(x => x.index)
                    .ToList();
                foreach (int index in indexes)
                {
                    if (_newestAlarms.AsSpan(index, index + 8).IndexOf(true) == -1)
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

        /// <summary>
        /// 自动运行预料外的报警
        /// </summary>
        /// <returns></returns>
        public bool HasAutoRunUnexpectedAlarms()
        {
            return Instance.HasActiveErrorAlarm("MR60408", "MR61000", "MR61100", "MR61200", "MR61300", "MR61400");
        }

        /// <summary>
        /// 自动运行预料外的报警
        /// </summary>
        /// <returns></returns>
        public bool HasAutoRunUnexpectedAlarms(out bool[]? newestAlarms)
        {
            return Instance.HasActiveErrorAlarm(out newestAlarms, "MR60408", "MR61000", "MR61100", "MR61200", "MR61300", "MR61400");
        }

        /// <summary>
        /// 轴错误的报警
        /// </summary>
        /// <returns></returns>
        public bool HasAxisErrorAlarms()
        {
            return Instance.HasActiveErrorAlarm("MR61000", "MR61100", "MR61200", "MR61300", "MR61400", "MR61006", "MR61106", "MR61206", "MR61306", "MR61406");
        }

        /// <summary>
        /// 测高导电异常
        /// </summary>
        public bool HasConductivityAlarm()
        {
            return Instance.HasTargetActiveErrorAlarm("MR60408");
        }

        /// <summary>
        /// 主轴冷却水异常
        /// </summary>
        public bool HasSpindleCoolingWaterAlarm()
        {
            return Instance.HasTargetActiveErrorAlarm("MR60400");
        }

        /// <summary>
        /// 测高导电异常以外的任何异常
        /// </summary>
        public bool HasAnyExceptConductivityAlarm()
        {
            return Instance.HasActiveErrorAlarm("MR60408");
        }
    }
}