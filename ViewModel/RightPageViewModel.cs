using DryIoc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.common;

namespace 精密切割系统.ViewModel
{
    public class RightPageViewModel : BindableBase
    {
        public ObservableCollection<ButtonParams> RightButtonParams { get; set; }
        public ObservableCollection<ActiveAlarmModel> ActiveAlarms { get; set; }

        public ObservableCollection<string> WaitingFuncNames { get; set; }

        private Visibility _alarmVisibility = Visibility.Visible;

        public Visibility AlarmVisibility
        {
            get => _alarmVisibility;
            set => SetProperty(ref _alarmVisibility, value);
        }

        private Visibility _waitingFuncNamesVisibility;

        public Visibility WaitingFuncNamesVisibility
        {
            get => _waitingFuncNamesVisibility;
            set => SetProperty(ref _waitingFuncNamesVisibility, value);
        }

        public RightPageViewModel()
        {
            RightButtonParams = WindowLayout.RightPageButtons;
            ActiveAlarms = new ObservableCollection<ActiveAlarmModel>();
            WaitingFuncNames = new ObservableCollection<string>();
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (!AtomicConfig.IsCutProcessing)
                        {
                            if (AlarmConfig.Instance.HasActiveErrorAlarm(false))
                            {
                                // 不在切割状态下，且有报警时，三色灯报警红色
                                await PlcControl.tagControl.wholeDevice.OpenRedLightAsync();
                            }
                            else
                            {
                                // 不在切割状态下，且有没报警时，三色灯报警黄色
                                await PlcControl.tagControl.wholeDevice.OpenYellowLightAsync();
                            }
                        }
                        bool[]? alarms = AlarmConfig.Instance.GetNewestAlarms();
                        if (alarms != null && AlarmConfig.Instance.TryGetActiveAlarms(alarms, out List<AlarmInfo> alarmInfos))
                        {
                            if (!IsSamely(alarmInfos, ActiveAlarms))
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    ActiveAlarms.Clear();
                                    foreach (AlarmInfo alarmInfo in alarmInfos)
                                    {
                                        ActiveAlarms.Add(new ActiveAlarmModel() { Address = alarmInfo.Address, Level = alarmInfo.Level, Message = alarmInfo.Message });
                                    }
                                    AlarmVisibility = Visibility.Visible;
                                });
                            }
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                ActiveAlarms.Clear();
                                AlarmVisibility = Visibility.Hidden;
                            });
                        }
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            WaitingFuncNames.Clear();
                            WaitingFuncNames.Add(KeyencePlc.PlcSemaphore.CurrentCount.ToString());
                            WaitingFuncNames.AddRange(TaskUtils.CurrentWaitingFuncDict.Values);
                            if (WaitingFuncNames.Count == 0)
                            {
                                WaitingFuncNamesVisibility = Visibility.Hidden;
                            }
                            else
                            {
                                WaitingFuncNamesVisibility = Visibility.Visible;
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Tools.LogError($"报警监控异常: {ex.Message}");
                    }
                    await Task.Delay(300);
                }
            }, TaskCreationOptions.LongRunning);
        }

        private bool IsSamely(List<AlarmInfo> alarmInfos, ObservableCollection<ActiveAlarmModel> activeAlarms)
        {
            if (alarmInfos.Count != activeAlarms.Count)
            {
                return false;
            }
            for (int i = 0; i < alarmInfos.Count; i++)
            {
                if (alarmInfos[i].Address != activeAlarms[i].Address)
                {
                    return false;
                }
            }
            return true;
        }
    }
}