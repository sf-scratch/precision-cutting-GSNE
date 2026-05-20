using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;
using 精密切割系统.Model.cut;
using 精密切割系统.Utils;

namespace 精密切割系统.View.Pages.common
{
    /// <summary>
    /// CommonDimming.xaml 的交互逻辑
    /// </summary>
    public partial class CommonDimming : UserControl
    {
        private CancellationTokenSource _cts = new();

        private AsyncDelegateCommand<string> _updateExposureTime;
        public AsyncDelegateCommand<string> UpdateExposureTimeCommand => _updateExposureTime ??= new AsyncDelegateCommand<string>(ExecuteUpdateExposureTimeCommand);

        private async Task ExecuteUpdateExposureTimeCommand(string exposureTime)
        {
            if (!double.TryParse(exposureTime, out double updateExposureTime))
            {
                return;
            }
            int maxExposureTime = 5000;
            double currentExposureTime = CameraUtils.GetCameraExposureTime();
            if ((currentExposureTime == maxExposureTime && updateExposureTime > 0) || (currentExposureTime == 1 && updateExposureTime < 0))
            {
                return;
            }
            double newExposureTime = currentExposureTime + updateExposureTime;
            if (newExposureTime > maxExposureTime)
            {
                newExposureTime = maxExposureTime;
            }
            await CameraUtils.SetCameraExposureTimeAsync(newExposureTime);
        }

        public CommonDimming()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitData();
            StartMonitor();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        public async void InitData()
        {
            UserDefineDataModel userDefineData = await SqlHelper.GetOrCreateEntityAsync(() => new UserDefineDataModel());
            if (int.TryParse(userDefineData.LightSourceBrightnessCh1, out int light))
            {
                GlobalParams.intensityRatio = light / 100.0;
            }
            int intensity = CalculateIntensity(GlobalParams.intensityRatio);
            CameraUtils.SetLightIntensity(intensity, GlobalParams.LightIntensityChannel);
            SetLightRatio(Convert.ToDecimal(GlobalParams.intensityRatio), 0);
            ringPanel.Visibility = Visibility.Collapsed;
        }

        public void StartMonitor()
        {
            if (_cts.IsCancellationRequested)
            {
                _cts = new CancellationTokenSource();
            }
            CancellationToken token = _cts.Token;
            _ = Task.Run(async () =>
            {
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
                while (await timer.WaitForNextTickAsync(token))
                {
                    try
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            ExposureTime.Text = CameraUtils.GetCameraExposureTime().ToString();
                        });
                    }
                    catch (Exception ex)
                    {
                        Tools.LogError($"{MethodBase.GetCurrentMethod()?.Name}监控异常: {ex.Message}");
                    }
                }
            });
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="t_intensity"></param>
        /// <param name="type">0 高倍 1 低倍 2 环光</param>
        private void SetLightRatio(decimal t_intensity, int type)
        {
            switch (type)
            {
                case 0:
                    GlobalParams.intensityRatio = NormalizeIntensityRatio(t_intensity);
                    dirLightRatio.Text = (Math.Round(GlobalParams.intensityRatio * 100, 2)).ToString();
                    break;

                case 1:
                    GlobalParams.lowIntensityRatio = NormalizeIntensityRatio(t_intensity);
                    dirLightRatio.Text = (Math.Round(GlobalParams.lowIntensityRatio * 100, 2)).ToString();
                    break;

                case 2:
                    GlobalParams.RingIntensityRatio = NormalizeIntensityRatio(t_intensity);
                    ringLightRatio.Text = (Math.Round(GlobalParams.RingIntensityRatio * 100, 2)).ToString();
                    break;

                default:
                    break;
            }
        }

        public static int CalculateIntensity(double intensityRatio)
        {
            int intensity = (int)Math.Ceiling(intensityRatio * 100);
            intensity = Math.Clamp(intensity, 1, 100);
            return intensity;
        }

        public static double NormalizeIntensityRatio(decimal input)
        {
            // 确保值在 0.01 到 1 的范围内
            double normalizedValue = Math.Clamp((double)input, 0.01, 1.0);
            return normalizedValue;
        }

        private void SubFive_OnClick(object sender, RoutedEventArgs e)
        {
            AdjustIntensity(-0.05m, 0);
        }

        private void SubOne_OnClick(object sender, RoutedEventArgs e)
        {
            AdjustIntensity(-0.01m, 0);
        }

        private void AddOne_OnClick(object sender, RoutedEventArgs e)
        {
            AdjustIntensity(0.01m, 0);
        }

        private void AddFive_OnClick(object sender, RoutedEventArgs e)
        {
            AdjustIntensity(0.05m, 0);
        }

        /// <summary>
        /// adjustment 是小数，表示百分比的小数 比如0.05表示 百分之5
        /// </summary>
        /// <param name="adjustment"></param>
        /// <param name="type">0 高倍 1 低倍 2 环光</param>
        private void AdjustIntensity(decimal adjustment, int type)
        {
            decimal intensityRatio = Convert.ToDecimal(GlobalParams.intensityRatio);
            intensityRatio += adjustment;
            int intensity = CalculateIntensity(Convert.ToDouble(intensityRatio));
            CameraUtils.SetLightIntensity(intensity, GlobalParams.LightIntensityChannel);
            SetLightRatio(intensityRatio, type);
            CurrentUtils.UpdateLightSourceBrightness(SemiAutoCutService.Instance.CurrentChannelNum, intensity);
        }

        private void ringSubFive_Click(object sender, RoutedEventArgs e)
        {
            AdjustIntensity(-0.05m, 2);
        }

        private void ringSubOne_Click(object sender, RoutedEventArgs e)
        {
            AdjustIntensity(-0.01m, 2);
        }

        private void ringAddOne_Click(object sender, RoutedEventArgs e)
        {
            AdjustIntensity(0.01m, 2);
        }

        private void ringAddFive_Click(object sender, RoutedEventArgs e)
        {
            AdjustIntensity(0.05m, 2);
        }
    }
}