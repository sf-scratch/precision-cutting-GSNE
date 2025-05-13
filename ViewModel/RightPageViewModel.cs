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
using 精密切割系统.Model.common;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.common;

namespace 精密切割系统.ViewModel
{
    public class RightPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<RightButtonParams> RightButtonParams { get; set; }
        public ObservableCollection<ActiveAlarmModel> ActiveAlarms { get; set; }

        public RightPageViewModel()
        {
            RightButtonParams = WindowLayout.RightPageButtons;
            ActiveAlarms = new ObservableCollection<ActiveAlarmModel>();
            Task.Run(async () => 
            {
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
                while (await timer.WaitForNextTickAsync())
                {
                    try
                    {
                        bool[]? alarms = await PlcControl.plc.ReadDataAsync(AlarmConfig.Instance.StartAddress, (ushort)AlarmConfig.Instance.TotalAlarmCount);
                        Application.Current.Dispatcher.Invoke(() => ActiveAlarms.Clear());
                        if (alarms != null && AlarmConfig.Instance.TryGetActiveAlarms(alarms, out List<AlarmInfo> alarmInfos))
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                foreach (AlarmInfo alarmInfo in alarmInfos)
                                {
                                    ActiveAlarms.Add(new ActiveAlarmModel() { Address = alarmInfo.Address, Level = alarmInfo.Level, Message = alarmInfo.Message });
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Tools.LogError($"报警监控异常: {ex.Message}");
                    }
                }
            });
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
