using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.database.db.modle;
using 精密切割系统.Helpers;
using 精密切割系统.ViewModel;

namespace 精密切割系统.Model.plc
{
    public static class SpeedManager
    {
        private static bool _isHighSpeed;

        // 静态属性改变事件
        public static event EventHandler<bool> IsHighSpeedPropertyChanged;

        public static bool IsHighSpeed
        {
            get { return _isHighSpeed; }
            set
            {
                if (SetProperty(ref _isHighSpeed, value))
                {
                    // 触发静态事件
                    IsHighSpeedPropertyChanged?.Invoke(null, value);

                    // 执行PLC操作
                    HandleSpeedChange(value);
                }
            }
        }

        private static async void HandleSpeedChange(bool isHighSpeed)
        {
            if (isHighSpeed)
            {
                var list = SqlHelper.Table<OperationParametersModel>().Where(t => t.Id == 1).ToList();
                if (list.Count < 1) return;
                var operationParam = list.FirstOrDefault();
                if (operationParam is null) return;
                await Task.Run(async () =>
                {
                    // 设置为高速
                    await PlcControl.tagControl.Xaxis.SetHighSpeedAsync(1);
                    await PlcControl.tagControl.Xaxis.SetJogRelativeSpeedAsync(operationParam.XScanSpeed.ToFloat());
                    await PlcControl.tagControl.Yaxis.SetHighSpeedAsync(1);
                    await PlcControl.tagControl.Yaxis.SetJogRelativeSpeedAsync(operationParam.YScanSpeed.ToFloat());
                    await PlcControl.tagControl.Z1axis.SetHighSpeedAsync(1);
                    await PlcControl.tagControl.Z1axis.SetJogRelativeSpeedAsync(operationParam.ZScanSpeed.ToFloat());
                    await PlcControl.tagControl.Z2axis.SetHighSpeedAsync(1);
                    await PlcControl.tagControl.Z2axis.SetJogRelativeSpeedAsync(2);
                    await PlcControl.tagControl.ThetaAxis.SetHighSpeedAsync(1);
                    await PlcControl.tagControl.ThetaAxis.SetJogRelativeSpeedAsync(operationParam.RScanSpeed.ToFloat());
                });
            }
            else
            {
                await Task.Run(async () =>
                {
                    // 设置为低速
                    await PlcControl.tagControl.Xaxis.SetHighSpeedAsync(0);
                    await PlcControl.tagControl.Yaxis.SetHighSpeedAsync(0);
                    await PlcControl.tagControl.Z1axis.SetHighSpeedAsync(0);
                    await PlcControl.tagControl.Z2axis.SetHighSpeedAsync(0);
                    await PlcControl.tagControl.ThetaAxis.SetHighSpeedAsync(0);
                });
            }
        }

        private static bool SetProperty<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            return true;
        }
    }
}