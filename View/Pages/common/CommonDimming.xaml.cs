using System;
using System.Collections.Generic;
using System.Linq;
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
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;

namespace 精密切割系统.View.Pages.common
{
    /// <summary>
    /// CommonDimming.xaml 的交互逻辑
    /// </summary>
    public partial class CommonDimming : UserControl
    {
        public CommonDimming()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitData();
        }
        public void InitData()
        {
            if (CameraUtils.currentCameraIndex == 0)
            {
                int intensity = CalculateIntensity(GlobalParams.intensityRatio);
                CameraUtils.SetLightIntensity(intensity, GlobalParams.LightIntensityChannel);
                SetLightRatio(Convert.ToDecimal(GlobalParams.intensityRatio), 0);
                ringPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                int lowIntensity = CalculateIntensity(GlobalParams.lowIntensityRatio);
                int ringIntensity = CalculateIntensity(GlobalParams.RingIntensityRatio);
                CameraUtils.SetLightIntensity(lowIntensity, GlobalParams.LowLightIntensityChannel);
                CameraUtils.SetLightIntensity(ringIntensity, GlobalParams.RingLightIntensityChannel);
                SetLightRatio(Convert.ToDecimal(GlobalParams.lowIntensityRatio), 1);
                SetLightRatio(Convert.ToDecimal(GlobalParams.RingIntensityRatio), 2);
                ringPanel.Visibility = Visibility.Visible;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="t_intensity"></param>
        /// <param name="type">0 高倍 1 低倍 2 环光</param>
        private void SetLightRatio(decimal t_intensity, int type)
        {
            switch(type) {
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
            int intensity = (int)Math.Ceiling(intensityRatio * 255);
            intensity = Math.Clamp(intensity, 1, 255);
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
            AdjustIntensity(-0.05m, CameraUtils.currentCameraIndex);
        }

        private void SubOne_OnClick(object sender, RoutedEventArgs e)
        {
            AdjustIntensity(-0.01m, CameraUtils.currentCameraIndex);
        }

        private void AddOne_OnClick(object sender, RoutedEventArgs e)
        {
            AdjustIntensity(0.01m, CameraUtils.currentCameraIndex);
        }

        private void AddFive_OnClick(object sender, RoutedEventArgs e)
        {
            AdjustIntensity(0.05m, CameraUtils.currentCameraIndex);
        }

        /// <summary>
        /// adjustment 是小数，表示百分比的小数 比如0.05表示 百分之5
        /// </summary>
        /// <param name="adjustment"></param>
        /// <param name="type">0 高倍 1 低倍 2 环光</param>
        private void AdjustIntensity(decimal adjustment, int type)
        {
            decimal t_intensity = Convert.ToDecimal(type == 0 ? GlobalParams.intensityRatio : type == 1 
                ? GlobalParams.lowIntensityRatio : GlobalParams.RingIntensityRatio);
            t_intensity += adjustment;  //0.8 + 0.05 = 0.85
            int v_intensity = (int)Math.Ceiling(t_intensity * 255);
            int reNum = Math.Clamp(v_intensity, 1, 255); //值在这个区间
            int channel = type == 0 ? GlobalParams.LightIntensityChannel : type == 1
                ? GlobalParams.LowLightIntensityChannel : GlobalParams.RingLightIntensityChannel;
            if (reNum == 1)
            {
                CameraUtils.TurnOffChannel(channel);
            } else
            {
                CameraUtils.TurnOnChannel(channel);
            }
            CameraUtils.SetLightIntensity(reNum, channel);
            SetLightRatio(t_intensity, type);
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
