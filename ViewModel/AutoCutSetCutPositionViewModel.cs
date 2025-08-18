using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.PubSubEvent;
using 精密切割系统.Utils;
using 精密切割系统.View.Pages.F4_BladeMaintenance;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.ViewModel
{
    public class AutoCutSetCutPositionViewModel : CustomBindableBase
    {
        private readonly IRegionManager _regionManager;
        private AutoCutSetCutPositionType _setType;
        private CancellationTokenSource? _axisInfoCts;

        private string _title;
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private float _originPositionY;
        public float OriginPositionY
        {
            get { return _originPositionY; }
            set { SetProperty(ref _originPositionY, value); }
        }

        private float _currentPositionY;
        public float CurrentPositionY
        {
            get { return _currentPositionY; }
            set { SetProperty(ref _currentPositionY, value); }
        }

        public AutoCutSetCutPositionViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public AutoCutSetCutPositionViewModel()
        {
        }

        private void InitRightButton()
        {
            RightButtonCollection.Clear();
            RightButtonCollection.Add(RightButtonParams.YelloRightButton("确认位置", "CogBox", Sure));
            RightButtonCollection.Add(RightButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private void Sure()
        {
            if (float.IsNaN(CurrentPositionY))
            {
                MaterialSnackUtils.MaterialSnack($"当前位置获取失败，请重试！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            switch (_setType)
            {
                case AutoCutSetCutPositionType.SetCut:
                    Appsettings.CutY = CurrentPositionY;
                    MaterialSnackUtils.MaterialSnack($"切割开始位置设置成功！", MaterialSnackUtils.SnackType.SUCCESS);
                    break;
                case AutoCutSetCutPositionType.SetSharpen:
                    Appsettings.SharpenY = CurrentPositionY;
                    MaterialSnackUtils.MaterialSnack($"磨刀开始位置设置成功！", MaterialSnackUtils.SnackType.SUCCESS);
                    break;
                default:
                    break;
            }
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(BladeReplacementConfiguration));
        }

        private void Back()
        {
            StopGetAxisInfo();
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(BladeReplacementConfiguration));
        }

        private void StartGetAxisInfo()
        {
            StopGetAxisInfo(); // 确保旧任务停止
            _axisInfoCts = new CancellationTokenSource();
            CancellationToken token = _axisInfoCts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
                    while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
                    {
                        await UpdateAxisPositionAsync().ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Tools.LogError($"轴位置监控任务异常: {ex.Message}");
                }
            }, _axisInfoCts.Token);
        }

        private void StopGetAxisInfo()
        {
            _axisInfoCts?.Cancel();
            _axisInfoCts?.Dispose();
            _axisInfoCts = null;
        }

        private async Task UpdateAxisPositionAsync()
        {
            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                float? location = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync()
                    .WaitAsync(timeoutCts.Token)
                    .ConfigureAwait(false);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentPositionY = location.HasValue ? MathF.Round(location.Value, 3).ToActualY() : float.NaN;
                });
            }
            catch (Exception ex)
            {
                Tools.LogError($"更新轴位置失败: {ex.Message}");
                CurrentPositionY = float.NaN;
            }
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            StartGetAxisInfo();
            InitRightButton();
            try
            {
                _setType = navigationContext.Parameters.GetValue<AutoCutSetCutPositionType>(nameof(AutoCutSetCutPositionType));
                switch (_setType)
                {
                    case AutoCutSetCutPositionType.SetCut:
                        Title = "设置切割位置";
                        OriginPositionY = Appsettings.CutY ?? 0;
                        await AutoCutUtils.GoPreCutLineAsync(_axisInfoCts?.Token ?? default);
                        break;
                    case AutoCutSetCutPositionType.SetSharpen:
                        Title = "设置磨刀位置";
                        OriginPositionY = Appsettings.SharpenY ?? 0;
                        await AutoCutUtils.GoPreSharpenLineAsync(_axisInfoCts?.Token ?? default);
                        break;
                    default:
                        break;
                }
            }
            catch (OperationCanceledException) { }
        }
    }

    public enum AutoCutSetCutPositionType
    {
        SetCut,
        SetSharpen
    }
}
