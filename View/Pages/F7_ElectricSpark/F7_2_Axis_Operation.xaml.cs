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
using static NPOI.HSSF.Util.HSSFColor;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;
using 精密切割系统.Driver;
using System.Diagnostics;
using System.Security.Cryptography.Xml;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Utilities.Encoders;

namespace 精密切割系统.View.Pages.F7_ElectricSpark
{
    /// <summary>
    /// F7_2_Axis_Operation.xaml 的交互逻辑
    /// </summary>
    public partial class F7_2_Axis_Operation : Page
    {
        public F7_2_Axis_Operation()
        {
            InitializeComponent();
            tbxXLenght.Text = "2";
            tbxYLenght.Text = "2";
            tbxZLenght.Text = "2";
/*            aov = new F7_2_AxisOperationViewModel();

            // 创建触发器并绑定到Style
            DataTrigger trigger = new DataTrigger();
            trigger.Binding = new Binding("Fill") { Source = aov.xStatus };
            trigger.Value = true;
            trigger.Setters.Add(new Setter(Rectangle.FillProperty, Brushes.Green));

            DataTrigger trigger1 = new DataTrigger();
            trigger1.Binding = new Binding("Fill") { Source = aov.xStatus };
            trigger1.Value = false;
            trigger1.Setters.Add(new Setter(Rectangle.FillProperty, Brushes.Red));

            // 创建Style并添加触发器
            Style style = new Style();
            style.Triggers.Add(trigger);
            style.Triggers.Add(trigger1);

            // 应用Style
            rectXStatus.Style = style;*/
        }
        private bool _isRunning = false;
        private int btnSelected = 0;
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;
        private Driver.Axis curAxis;
        private string curSpeed;
        private string curDistance;
        private string curLocation;
        private F7_2_AxisOperationViewModel aov;

        private void btnXAxis_Click(object sender, RoutedEventArgs e)
        {
            //btnXAxis.Background = new SolidColorBrush(Colors.Green);
            BrushConverter brushConverter = new BrushConverter();
            btnXAxis.Background = (SolidColorBrush)brushConverter.ConvertFromInvariantString("#FFADD8E6");
            btnYAxis.Background = null;
            btnZAxis.Background = null;
            btnThetaAxis.Background = null;
            btnSelected = 1;
            curAxis = PlcControl.tagControl.Xaxis;
            curSpeed = tbxXSpeed.Text;
            curDistance = tbxXLenght.Text;
        }

        private void btnYAxis_Click(object sender, RoutedEventArgs e)
        {
            BrushConverter brushConverter = new BrushConverter();
            btnXAxis.Background = null;
            //btnYAxis.Background = new SolidColorBrush(Colors.Green);
            btnYAxis.Background = (SolidColorBrush)brushConverter.ConvertFromInvariantString("#FFADD8E6");
            btnZAxis.Background = null;
            btnThetaAxis.Background = null;
            btnSelected = 2;
            curAxis = PlcControl.tagControl.Yaxis;
            curSpeed = tbxYSpeed.Text;
            curDistance = tbxYLenght.Text;
        }

        private void btnZAxis_Click(object sender, RoutedEventArgs e)
        {
            BrushConverter brushConverter = new BrushConverter();
            btnXAxis.Background = null;
            btnYAxis.Background = null;
            //btnZAxis.Background = new SolidColorBrush(Colors.Green);
            btnZAxis.Background = (SolidColorBrush)brushConverter.ConvertFromInvariantString("#FFADD8E6");
            btnThetaAxis.Background = null;
            btnSelected = 3;
            curAxis = PlcControl.tagControl.Z1axis;
            curSpeed = tbxZSpeed.Text;
            curDistance = tbxZLenght.Text;
        }

        private void btnThetaAxis_Click(object sender, RoutedEventArgs e)
        {
            BrushConverter brushConverter = new BrushConverter();
            btnXAxis.Background = null;
            btnYAxis.Background = null;
            btnZAxis.Background = null;
            //btnThetaAxis.Background = new SolidColorBrush(Colors.Green);
            btnThetaAxis.Background = (SolidColorBrush)brushConverter.ConvertFromInvariantString("#FFADD8E6");
            btnSelected = 4;
            curAxis = PlcControl.tagControl.ThetaAxis;
            curSpeed = tbxThetaSpeed.Text;
            curDistance = tbxThetaLenght.Text;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            btnSelected = 0;
            mainWindow = Application.Current.MainWindow as MainWindow;
            rightPage = mainWindow.rightFrame.Content as RightPage;
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnSure.Visibility = Visibility.Visible;
            rightPage.btnSure.BackFlag = false;
            rightPage.btnSure.SetRightClickedHandler(btnSure);
            rightPage.btnBack.SetRightClickedHandler(back);

            if (PlcControl.allTags.ContainsKey("X点动和相对运动速度"))
            {
                string v = PlcControl.allTags["X点动和相对运动速度"].value;
                tbxXSpeed.Text = v.Length >= 8 ? v.Substring(0, 8) : v;
            }
            if (PlcControl.allTags.ContainsKey("Y点动和相对运动速度"))
            {
                string v = PlcControl.allTags["Y点动和相对运动速度"].value;
                tbxYSpeed.Text = v.Length >= 8 ? v.Substring(0, 8) : v;
            }
            if (PlcControl.allTags.ContainsKey("Z1点动和相对运动速度"))
            {
                string v = PlcControl.allTags["Z1点动和相对运动速度"].value;
                tbxZSpeed.Text = v.Length >= 8 ? v.Substring(0, 8) : v;
            }
            if (PlcControl.allTags.ContainsKey("Theta点动和相对运动速度"))
            {
                string v = PlcControl.allTags["Theta点动和相对运动速度"].value;
                tbxZSpeed.Text = v.Length >= 8 ? v.Substring(0, 8) : v;
            }

            if (PlcControl.allTags.ContainsKey("X点动和相对运动速度高速"))
            {
                string v = PlcControl.allTags["X点动和相对运动速度高速"].value;
                tbxXFastSpeed.Text = v.Length >= 8 ? v.Substring(0, 8) : v;
            }
            if (PlcControl.allTags.ContainsKey("Y点动和相对运动速度高速"))
            {
                string v = PlcControl.allTags["Y点动和相对运动速度高速"].value;
                tbxYFastSpeed.Text = v.Length >= 8 ? v.Substring(0, 8) : v;
            }
            if (PlcControl.allTags.ContainsKey("Z1点动和相对运动速度高速"))
            {
                string v = PlcControl.allTags["Z1点动和相对运动速度高速"].value;
                tbxZFastSpeed.Text = v.Length >= 8 ? v.Substring(0, 8) : v;
            }
            if (PlcControl.allTags.ContainsKey("Theta点动和相对运动速度高速"))
            {
                string v = PlcControl.allTags["Theta点动和相对运动速度高速"].value;
                tbxThetaFastSpeed.Text = v.Length >= 8 ? v.Substring(0, 8) : v;
            }
            mainWindow.UpdateOperatePage(OperateData.GetTab7140Operate(), null, TouchLeaveHandler, OperateClickHandler);

            _isRunning = true;
            new Thread(UpdateUI).Start();
        }

        private void UpdateUI()
        {
            
            while (_isRunning)
            {
                Thread.Sleep(100);

                // 使用Dispatcher调度到UI线程更新
                this.Dispatcher.Invoke(() =>
                {
                    if (PlcControl.allTags.ContainsKey("X轴当前位置"))
                    {
                        this.tbxXPosition.Text = PlcControl.allTags["X轴当前位置"].Value;
                    }
                    if (PlcControl.allTags.ContainsKey("Y轴当前位置"))
                    {
                        this.tbxYPosition.Text = PlcControl.allTags["Y轴当前位置"].Value;
                    }
                    if (PlcControl.allTags.ContainsKey("Z1轴当前位置"))
                    {
                        this.tbxZPosition.Text = PlcControl.allTags["Z1轴当前位置"].Value;
                    }
                    /*if (PlcControl.allTags.ContainsKey("Theta轴当前位置"))
                    {
                        this.tbxThetaPosition.Text = PlcControl.allTags["Theta轴当前位置"].Value;
                    }*/
                    // value = 1 运动  = 2 停止
                    if (PlcControl.allTags.ContainsKey("X轴当前电机状态"))
                    {
                        if (PlcControl.allTags["X轴当前电机状态"].Value == "1")
                        {
                            this.rectXStatus.Fill = new SolidColorBrush(Colors.Green);
                        }
                        else
                        {
                            this.rectXStatus.Fill = new SolidColorBrush(Colors.Gray);
                        }
                    }
                    if (PlcControl.allTags.ContainsKey("Y轴当前电机状态"))
                    {
                        if (PlcControl.allTags["Y轴当前电机状态"].Value == "1")
                        {
                            this.rectYStatus.Fill = new SolidColorBrush(Colors.Green);
                        }
                        else
                        {
                            this.rectYStatus.Fill = new SolidColorBrush(Colors.Gray);
                        }
                    }
                    if (PlcControl.allTags.ContainsKey("Z1轴当前电机状态"))
                    {
                        if (PlcControl.allTags["Z1轴当前电机状态"].Value == "1")
                        {
                            this.rectZStatus.Fill = new SolidColorBrush(Colors.Green);
                        }
                        else
                        {
                            this.rectZStatus.Fill = new SolidColorBrush(Colors.Gray);
                        }
                    }

                    // value = 1 ready
                    if (PlcControl.allTags.ContainsKey("Theta轴当前状态"))
                    {
                        if (PlcControl.allTags["Theta轴当前状态"].Value == "1")
                        {
                            this.rectThetaStatus.Fill = new SolidColorBrush(Colors.Gray);
                        }
                        else
                        {
                            this.rectThetaStatus.Fill = new SolidColorBrush(Colors.Green);
                        }
                    }
                });
            }
        }

        private void back(object sender, bool e)
        {
            //mainWindow.GotoF7();
            mainWindow.NavigateToPage("MainMenu");
            //mainWindow.mainFrame.NavigationService.GoBack();
        }

        private void btnSure(object sender, bool e)
        {
            //mainWindow.NavigateToPage("Pages/F5_GeneralEfficiency/F5_1_1_PrecutDataDetails", $"PrecutNo={currentModel.PrecutNo}");
        }

        private void SetSlowSpeed()
        {
            if (btnSelected == 1 && PlcControl.allTags.ContainsKey("X点动和相对运动速度"))
            {
                PlcControl.allTags["X点动和相对运动速度"].writeValue = tbxXSpeed.Text;
                PlcControl.plc.writeTag(PlcControl.allTags["X点动和相对运动速度"]);
            }
            else if (btnSelected == 2 && PlcControl.allTags.ContainsKey("Y点动和相对运动速度"))
            {
                PlcControl.allTags["Y点动和相对运动速度"].writeValue = tbxYSpeed.Text;
                PlcControl.plc.writeTag(PlcControl.allTags["Y点动和相对运动速度"]);
            }
            else if (btnSelected == 3 && PlcControl.allTags.ContainsKey("Z1点动和相对运动速度"))
            {
                PlcControl.allTags["Z1点动和相对运动速度"].writeValue = tbxZSpeed.Text;
                PlcControl.plc.writeTag(PlcControl.allTags["Z1点动和相对运动速度"]);
            }
            else if (btnSelected == 4 && PlcControl.allTags.ContainsKey("Theta点动和相对运动速度"))
            {
                PlcControl.allTags["Theta点动和相对运动速度"].writeValue = tbxThetaSpeed.Text;
                PlcControl.plc.writeTag(PlcControl.allTags["Theta点动和相对运动速度"]);
            }
        }

        private void SetSpeed()
        {
            if (btnSelected == 1 && PlcControl.allTags.ContainsKey("X点动和相对运动速度高速"))
            {
                PlcControl.allTags["X点动和相对运动速度高速"].writeValue = tbxXFastSpeed.Text;
                PlcControl.plc.writeTag(PlcControl.allTags["X点动和相对运动速度高速"]);
            }
            else if (btnSelected == 2 && PlcControl.allTags.ContainsKey("Y点动和相对运动速度高速"))
            {
                PlcControl.allTags["Y点动和相对运动速度高速"].writeValue = tbxYFastSpeed.Text;
                PlcControl.plc.writeTag(PlcControl.allTags["Y点动和相对运动速度高速"]);
            }
            else if (btnSelected == 3 && PlcControl.allTags.ContainsKey("Z1点动和相对运动速度高速"))
            {
                PlcControl.allTags["Z1点动和相对运动速度高速"].writeValue = tbxZFastSpeed.Text;
                PlcControl.plc.writeTag(PlcControl.allTags["Z1点动和相对运动速度高速"]);
            }
            else if (btnSelected == 4 && PlcControl.allTags.ContainsKey("Theta点动和相对运动速度高速"))
            {
                PlcControl.allTags["Theta点动和相对运动速度高速"].writeValue = tbxThetaFastSpeed.Text;
                PlcControl.plc.writeTag(PlcControl.allTags["Theta点动和相对运动速度高速"]);
            }
        }

        private string GetCurSpeed()
        {
            string curS = "5";
            if (btnSelected == 1)
            {
                curS = tbxXSpeed.Text;
            }
            else if (btnSelected == 2)
            {
                curS = tbxYSpeed.Text;
            }
            else if (btnSelected == 3)
            {
                curS = tbxZSpeed.Text;
            }
            else if (btnSelected == 4)
            {
                curS = tbxThetaSpeed.Text;
            }
            return curS;
        }

        private void OperateClickHandler(object sender, int code)
        {
            Debug.WriteLine("down");
            if (curAxis == null)
            {
                return;
            }
            switch (code)
            {
                case 7100: // 慢相对向前
                    SetSlowSpeed();
                    //curAxis.StartRelative("5", curDistance, 0);
                    curAxis.StartRelative(GetCurSpeed(), curDistance, 0);
                    break;
                case 7101:// 慢相对向后
                    SetSlowSpeed();
                    //curAxis.StartRelative("5", curDistance, 1);
                    curAxis.StartRelative(GetCurSpeed(), curDistance, 1);
                    break;
                case 7102:// 相对向前
                    SetSpeed();
                    //curAxis.StartRelative("20", curDistance, 0);
                    curAxis.StartRelative(GetCurSpeed(), curDistance, 0);
                    break;
                case 7103:// 相对向后
                    SetSpeed();
                    //curAxis.StartRelative("20", curDistance, 1);
                    curAxis.StartRelative(GetCurSpeed(), curDistance, 1);
                    break;
                case 7104:// 慢点动向前
                    SetSlowSpeed();
                    curAxis.SetHighSpeed("0");
                    Thread.Sleep(10);
                    curAxis.StartJog(0);
                    break;
                case 7105:// 慢点动向后
                    SetSlowSpeed();
                    curAxis.SetHighSpeed("0");
                    Thread.Sleep(10);
                    curAxis.StartJog(1);
                    break;
                case 7106:// 点动向前
                    SetSpeed();
                    curAxis.SetHighSpeed("1");
                    Thread.Sleep(10);
                    curAxis.StartJog(0);
                    break;
                case 7107:// 点动向后
                    SetSpeed();
                    curAxis.SetHighSpeed("1");
                    Thread.Sleep(10);
                    curAxis.StartJog(1);
                    break;
                default:
                    break;
            }
            SetSpeed();
            SetSlowSpeed();
        }

        private void TouchLeaveHandler(object sender, int code)
        {
            Debug.WriteLine("up");
            if (curAxis == null)
            {
                return;
            }
            switch (code)
            {
                case 7104:// 点动停止
                case 7105:
                case 7106:
                case 7107:
                    curAxis.StopMove();
                    break;
                default:
                    break;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //aov.xCurPosition = tbxXSpeed.Text;
        }
    }
}
