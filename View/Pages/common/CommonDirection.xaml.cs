using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using 精密切割系统.Utils;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.common
{
    /// <summary>
    /// CommonDirection.xaml 的交互逻辑
    /// </summary>
    public partial class CommonDirection : UserControl
    {
        // hi-speed状态 0 低速 1 高速
        private int hiSpeedStatus = 0;
        // 高速的倍率
        private double multipleNum = 0.1;
        // 扫描速度
        private double scanSpeed = 1;

        public CommonDirection()
        {
            InitializeComponent();
        }

        private void scanTopBtn_TouchDown(object sender, TouchEventArgs e)
        {
            DirectionHandle(0, 0);
        }
        
        private void scanTopBtn_TouchUp(object sender, TouchEventArgs e)
        {
            DirectionHandle(0, 1);
        }
        
        private void scanLeftBtn_TouchDown(object sender, TouchEventArgs e)
        {
            DirectionHandle(2, 0);
        }

        private void scanLeftBtn_TouchUp(object sender, TouchEventArgs e)
        {
            DirectionHandle(2, 1);
        }

        private void scanRightBtn_TouchDown(object sender, TouchEventArgs e)
        {
            DirectionHandle(3, 0);
        }

        private void scanRightBtn_TouchUp(object sender, TouchEventArgs e)
        {
            DirectionHandle(3, 1);
        }

        private void scanBottomBtn_TouchDown(object sender, TouchEventArgs e)
        {
            DirectionHandle(1, 0);
        }

        private void scanBottomBtn_TouchUp(object sender, TouchEventArgs e)
        {
            DirectionHandle(1, 1);
        }
        

        private void scrHighSpeedBtn_Click(object sender, RoutedEventArgs e)
        {
            if (hiSpeedStatus == 1)
            {
                hiSpeedStatus = 0;
            }
            else
            {
                hiSpeedStatus = 1;
            }
            SetHighBtnStatus(hiSpeedStatus);
            GlobalParams.heightSpeedStatus = hiSpeedStatus;
        }

        

        private void scanLeftBtn_TouchLeave(object sender, TouchEventArgs e)
        {
            DirectionHandle(2, 1);
        }

        private void scanBottomBtn_TouchLeave(object sender, TouchEventArgs e)
        {
            DirectionHandle(3, 1);
        }

        private void scanRightBtn_TouchLeave(object sender, TouchEventArgs e)
        {
            DirectionHandle(2, 1);
        }

        private void scanTopBtn_TouchLeave(object sender, TouchEventArgs e)
        {
            DirectionHandle(0, 1);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        private void UserControl_Loaded_1(object sender, RoutedEventArgs e)
        {
            
        }

        

        private void scanTopBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DirectionHandle(0, 0);
        }

        private void scanTopBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            DirectionHandle(0, 1);
        }

        private void scanTopBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            DirectionHandle(0, 1);
        }

        private void scanLeftBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DirectionHandle(2, 0);
        }

        private void scanLeftBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            DirectionHandle(2, 1);
        }

        private void scanLeftBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            DirectionHandle(2, 1);
        }

        private void scanRightBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DirectionHandle(3, 0);
        }

        private void scanRightBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            DirectionHandle(3, 1);
        }

        private void scanRightBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            DirectionHandle(3, 1);
        }

        private void scanBottomBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DirectionHandle(1, 0);
        }

        private void scanBottomBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            DirectionHandle(1, 1);
        }

        private void scanBottomBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            DirectionHandle(1, 1);
        }
        public void SetBtnImage(Image image, string direction, bool isSelected, int type)
        {
            string resourceName = null;
            if (type == 1)
            {
                resourceName = isSelected ? $"scr_{direction}_sel" : $"scr_{direction}";
            }
            else if (type == 2)
            {
                resourceName = isSelected ? $"scan_{direction}_sel" : $"scan_{direction}";
            }
            image.Source = Tools.BitmapImageToBitmap("/Assets/picture/" + resourceName + ".png");
        }
        public void SetHighBtnStatus(int tempHiSpeedStatus)
        {
            if (tempHiSpeedStatus == 0)
            {
                highSpeedBorder.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                highSpeedIcon.Source = Tools.BitmapImageToBitmap("/Assets/picture/scan_hi_speed_icon.png");
                multipleNum = 0.1;
                // 设置为低速
                PlcControl.tagControl.Xaxis.SetHighSpeed("0");
                PlcControl.tagControl.Yaxis.SetHighSpeed("0");
            }
            else
            {
                highSpeedBorder.Background = new SolidColorBrush(Color.FromRgb(23, 124, 250));
                highSpeedIcon.Source = Tools.BitmapImageToBitmap("/Assets/picture/scan_hi_speed_icon_sel.png");
                multipleNum = 1;
                // 设置为高速
                PlcControl.tagControl.Xaxis.SetHighSpeed("1");
                PlcControl.tagControl.Yaxis.SetHighSpeed("1");
            }
        }
        /// <summary>
        /// Handles directional movement based on input.
        /// </summary>
        /// <param name="direction">Direction (0: Up, 1: Down, 2: Left, 3: Right).</param>
        /// <param name="type">Operation type (0: Press, 1: Release).</param>
        private void DirectionHandle(int direction, int type)
        {
            // Early exit if axis is running and it's a press event.
            if (type == 0 && CommonCheck.AxisRunStatusCheck())
            {
                return;
            }

            // Define a lookup table for axis and jog direction.
            var axisData = new[]
            {
                new { Axis = PlcControl.tagControl.Yaxis, JogDirection = 1, Icon = scanTopIcon, IconName = "top" }, // Up
                new { Axis = PlcControl.tagControl.Yaxis, JogDirection = 0, Icon = scanBottomIcon, IconName = "bottom" }, // Down
                new { Axis = PlcControl.tagControl.Xaxis, JogDirection = 0, Icon = scanLeftIcon, IconName = "left" }, // Left
                new { Axis = PlcControl.tagControl.Xaxis, JogDirection = 1, Icon = scanRightIcon, IconName = "right" } // Right
            };

            // Validate direction input.
            if (direction < 0 || direction >= axisData.Length)
            {
                return; // Or throw an exception if invalid input is a critical error.
            }

            var data = axisData[direction];

            if (type == 0) // Press
            {
                data.Axis.StartJog(data.JogDirection);
                SetBtnImage(data.Icon, data.IconName, true, 2);
            }
            else // Release
            {
                data.Axis.StopMove();
                SetBtnImage(data.Icon, data.IconName, false, 2);
            }
        }
        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool isVisible)
            {
                if (isVisible)
                {
                    hiSpeedStatus = GlobalParams.heightSpeedStatus;
                    if (PlcControl.connectionStatus)
                    {
                        SetHighBtnStatus(hiSpeedStatus);
                    }
                }
            }
        }
    }
}
