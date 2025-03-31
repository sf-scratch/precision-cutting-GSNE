using Emgu.CV.CvEnum;
using NPOI.OpenXmlFormats.Dml;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Model.position.Bayesian;
using 精密切割系统.Model.position.correction;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.ViewModel;
using static 精密切割系统.Helpers.MaterialSnackUtils;

namespace 精密切割系统.View.Pages
{
    /// <summary>
    /// FrmMain.xaml 的交互逻辑
    /// </summary>
    public partial class FrmMain : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;

        public FrmMain()
        {
            InitializeComponent();
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mainWindow = Application.Current.MainWindow as MainWindow;
            rightPage = mainWindow.rightFrame.Content as RightPage;

            testAxisInput.SelectedIndex = 0;
            directionInput.SelectedIndex = 0;
            testStartPositionInput.Text = "0";
            targetPositionInput.Text = "1";
            testNumInput.Text = "10";
            testIndexNumInput.Text = "1";

            //右侧显示
            rightPage.PanelAction.Visibility = Visibility.Visible; 
            rightPage.btnBack.Visibility = Visibility.Visible; //右侧显示 - 返回按钮显示
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            //底部操作按钮
            mainWindow.UpdateOperatePage([], null);

            Thread displayTagValue = new Thread(new ThreadStart(updateUiDisplayValue));
            displayTagValue.IsBackground = true;
            displayTagValue.Start();
            stopTime.Text = stopTimeValue + "";

            GlobalParams.upPosition = -100;
            // 上一次光栅尺
            GlobalParams.upRealPosition = -100;
            GlobalParams.allDeepValue = 0;

            // yCompCheckbox.IsChecked = true;
            // z1CompCheckbox.IsChecked = true;

        }
        //返回到列表页面
        private void BtnBack_RightClicked(object? sender, bool e)
        {
            mainWindow.NavigateToPage("MainMenu");
        }

        Thread _thread;
        static bool runFlag = false;
        static bool pauseFlag = false;
        static int stopTimeValue = 5000;
        static string defaultSpeed = "10";
        private void confirmPositionBtn_Click(object sender, RoutedEventArgs e)
        {
            string testAxis = testAxisInput.Text;
            string key = testAxis.Equals("X轴") ? DeviceKey.curSpeedKey : testAxis.Equals("Y轴")
                ? DeviceKey.yCurSpeedKey : testAxis.Equals("Z1轴") ? DeviceKey.z1CurSpeedKey : "";
            if (key == null || key.Equals(""))
            {
                return;
            }
            string testStartPositionText = testStartPositionInput.Text;
            float testStartPosition = float.Parse(testStartPositionText);
            float positionDiff = float.Parse(targetPositionInput.Text);
            float diffValue = directionInput.Text.Equals("反向") ? testStartPosition + positionDiff : testStartPosition - positionDiff;
            switch (testAxis)
            {
                case "X轴":
                    PlcControl.tagControl.Xaxis.StartAbsolute("10", diffValue + "");
                    Thread.Sleep(1000);
                    Tools.WaitForValue(PlcControl.allTags[DeviceKey.curSpeedKey], "0");
                    PlcControl.tagControl.Xaxis.StartAbsolute("10", testStartPositionText);
                    break;
                case "Y轴":
                    PlcControl.tagControl.Yaxis.StartAbsolute("10", diffValue + "");
                    Thread.Sleep(1000);
                    Tools.WaitForValue(PlcControl.allTags[DeviceKey.yCurSpeedKey], "0");
                    PlcControl.tagControl.Yaxis.StartAbsolute("10", testStartPositionText);
                    break;
                case "Z1轴":
                    PlcControl.tagControl.Z1axis.StartAbsolute("10", diffValue + "");
                    Thread.Sleep(1000);
                    Tools.WaitForValue(PlcControl.allTags[DeviceKey.z1CurSpeedKey], "0");
                    PlcControl.tagControl.Z1axis.StartAbsolute("10", testStartPositionText);
                    break;
                default:
                    break;
            }
        }
        private void updateUiDisplayValue()
        {
            //Random random = new Random();
            while (true)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    string testAxis = testAxisInput.Text;
                    // 当前轴 X轴 Y轴 Z1轴
                    // 定位到开始位置
                    switch (testAxis)
                    {
                        case "X轴":
                            currentPositionInput.Text = PlcControl.plc.GetPlcValueString(DeviceKey.curLocationKey);
                            gratingCurrentPositionInput.Text = "0";
                            break;
                        case "Y轴":
                            currentPositionInput.Text = PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey);
                            gratingCurrentPositionInput.Text = PlcControl.plc.GetPlcValueString(DeviceKey.yGratingRulerCurLocationKey);
                            break;
                        case "Z1轴":
                            currentPositionInput.Text = PlcControl.plc.GetPlcValueString(DeviceKey.z1CurLocationKey);
                            gratingCurrentPositionInput.Text = PlcControl.plc.GetPlcValueString(DeviceKey.z1GratingRulerCurLocationKey);
                            break;
                        default:
                            break;
                    }

                }));
                Thread.Sleep(1000); // 休眠1秒
            }
        }
        private void startTestBtn_Click(object sender, RoutedEventArgs e)
        {
            if (runFlag)
            {
                return;
            }
            if (string.IsNullOrEmpty(directionInput.Text))
            {
                MaterialSnackUtils.MaterialSnack("请选择测量方向!", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            // 是否双向
            string directionInputValue = directionInput.Text;
            bool twoWayFlag = directionInputValue.Equals("双向") ;
            testStatus.Text = "测量中...";
            runFlag = true;
            // 获取开始位置
            string startPositionText = testStartPositionInput.Text;
            float startPosition = float.Parse(startPositionText);
            string testAxis = testAxisInput.Text;
            GlobalParams.upPosition = -100;
            GlobalParams.upRealPosition = -100;
            // 当前轴 X轴 Y轴 Z1轴
            // 定位到开始位置
            switch (testAxis)
            {
                case "X轴":
                    PlcControl.tagControl.Xaxis.StartAbsolute("10", startPositionText);
                    break;
                case "Y轴":
                    PlcControl.tagControl.Yaxis.StartAbsolute("10", startPositionText);
                    break;
                case "Z1轴":
                    PlcControl.tagControl.Z1axis.StartAbsolute("10", startPositionText);
                    break;
                default:
                    break;
            }
            // 光栅尺方式
            // AutoAdjustmentRuler(startPositionText, testAxis);
            string key = testAxis.Equals("X轴") ? DeviceKey.curSpeedKey : testAxis.Equals("Y轴")
                ? DeviceKey.yCurSpeedKey : testAxis.Equals("Z1轴") ? DeviceKey.z1CurSpeedKey : "";
            if (key == null || key.Equals(""))
            {
                return;
            }
            string positionKey = testAxis + "光栅尺当前位置";
            Thread.Sleep(500);
            // 监听是否已移动完成
            switch (testAxis)
            {
                case "X轴":
                    Tools.WaitForValue(PlcControl.allTags[DeviceKey.curSpeedKey], "0");
                    break;
                case "Y轴":
                    Tools.WaitForValue(PlcControl.allTags[DeviceKey.yCurSpeedKey], "0");
                    break;
                case "Z1轴":
                    Tools.WaitForValue(PlcControl.allTags[DeviceKey.z1CurSpeedKey], "0");
                    break;
                default:
                    break;
            }
            if (repeatedCheckbox.IsChecked == true)
            {
                RepeatedRun();
            } else
            {
                actualPositions.Text = PlcControl.plc.GetPlcValueString(positionKey);
                motorPositions.Text = PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey);
                // 获取步进和次数
                string indexText = testIndexNumInput.Text;
                float index = float.Parse(indexText);
                float start = startPosition;
                string numText = testNumInput.Text;
                stopTimeValue = int.Parse(stopTime.Text);
                bool stepFlag = stepCheckbox.IsChecked == true;
                // 开启线程，循环次数走步进距离
                _thread = new Thread(() =>
                {
                    int tempNum = int.Parse(numText);
                    int i = 0;
                    // 如果是双向，则数量 * 2
                    int num = tempNum;
                    if (twoWayFlag)
                    {
                        num = num * 2;
                    }
                    while (runFlag && i < num)
                    {
                        if (i == tempNum)
                        {
                            Thread.Sleep(6000);
                        }
                        // 如果大于第一轮，则减
                        if (i >= tempNum)
                        {
                            // 等于1 正向 2 反向
                            if (directionInputValue.Equals("正向") || directionInputValue.Equals("双向"))
                            {
                                start = start -= index;
                            }
                            else
                            {
                                start = start += index;
                            }
                        }
                        else
                        {
                            if (directionInputValue.Equals("正向") || directionInputValue.Equals("双向"))
                            {
                                start = start += index;
                            }
                            else
                            {
                                start = start -= index;
                            }
                        }
                        start = (float)Math.Round(start, GlobalParams.decimalPlaces);
                        Application.Current.Dispatcher.Invoke(new Action(() => currentNumInput.Text = (i + 1) + ""));
                        string resultPosition = "";
                        switch (testAxis)
                        {
                            case "X轴":
                                PlcControl.tagControl.Xaxis.StartAbsolute("10", start + "");
                                Thread.Sleep(500);
                                Tools.WaitForValue(PlcControl.allTags[DeviceKey.curSpeedKey], "0");
                                break;
                            case "Y轴":
                                resultPosition = start.ToString();
                                // 获取补偿后的位置
                                if (stepFlag)
                                {
                                    resultPosition = PlcControl.GetCompensateStep2(-index, testAxis);
                                }
                                
                                PlcControl.tagControl.Yaxis.StartAbsolute("10", resultPosition);
                                Thread.Sleep(500);
                                Tools.WaitForValue(PlcControl.allTags[DeviceKey.yCurSpeedKey], "0");
                                break;
                            case "Z1轴":
                                PlcControl.tagControl.Z1axis.StartAbsolute("10", start + "");
                                Thread.Sleep(500);
                                Tools.WaitForValue(PlcControl.allTags[DeviceKey.z1CurSpeedKey], "0");
                                break;
                            default:
                                break;
                        }
                        // 光栅尺方式
                        // AutoAdjustmentRuler(start + "", testAxis);

                        Thread.Sleep(stopTimeValue);
                        
                        Application.Current.Dispatcher.Invoke(new Action(() => {
                            if (stepFlag)
                            {
                                // 设置光栅尺位置
                                actualPositions.Text += (string.IsNullOrEmpty(actualPositions.Text) ? "" : ",")
                                + (Tools.GetFloatStringValue(PlcControl.plc.GetPlcValueString(DeviceKey.yGratingRulerCurLocationKey))).ToString("F6");
                                // 设置电机位置
                                motorPositions.Text += (string.IsNullOrEmpty(motorPositions.Text) ? "" : ",")
                                    + (Tools.GetFloatStringValue(PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey))).ToString("F6");
                            } else
                            {
                                actualPositions.Text += "," + PlcControl.plc.GetPlcValueString(positionKey);
                            }

                            if (compCheckbox.IsChecked == true)
                            {
                                Task.Run(() => {
                                    Thread.Sleep(500);
                                    Application.Current.Dispatcher.Invoke(new Action(() =>
                                    {
                                        GlobalParams.upRealPosition = float.Parse(PlcControl.plc.GetPlcValueString(DeviceKey.yGratingRulerCurLocationKey));
                                    }));

                                });
                            }
                        }));
                        // 暂停
                        while (pauseFlag)
                        {
                            Thread.Sleep(100);
                        }
                        i++;
                    }
                    Application.Current.Dispatcher.Invoke(new Action(() => testStatus.Text = "已完成！"));
                    runFlag = false;
                });
                _thread.IsBackground = true;
                _thread.Start();
            }
            
        }
        /// <summary>
        /// 自动校准光栅尺位置
        /// </summary>
        private void AutoAdjustmentRuler(string targetPosition, string axisName)
        {
            if (GlobalParams.runCompFlag)
            {
                targetPosition = PlcControl.GetCompensate(targetPosition, axisName, 1);
            }
            // 先通过电机位置走到目标位置
            SetAbsolute(targetPosition, axisName);
            string rulerPositionKey = "";
            string currentPositionKey = "";
            switch (axisName)
            {
                case "Y轴":
                    rulerPositionKey = DeviceKey.yGratingRulerCurLocationKey;
                    currentPositionKey = DeviceKey.yCurLocationKey;
                    break;
                case "Z1轴":
                    rulerPositionKey = DeviceKey.z1GratingRulerCurLocationKey;
                    currentPositionKey = DeviceKey.z1CurLocationKey;
                    break;
                default:
                    Console.WriteLine("未识别的测试轴！");
                    return;
            }

            if (!string.IsNullOrEmpty(rulerPositionKey))
            {
                double targetPositionFloat = Math.Round(Tools.GetDoubleStringValue(targetPosition), GlobalParams.decimalPlaces);
                const double tolerance = 0.0002; // 公差值
                const int consecutiveAttemptsLimit = 1; // 连续尝试次数限制
                const int timeoutMilliseconds = 4000; // 超时时间

                int consecutiveAttempts = 0; // 连续满足公差的尝试次数
                bool flag = true;
                DateTime startTime = DateTime.Now;
                int i = 0;
                do
                {
                    // 获取当前光栅尺位置数据
                    string rulerPositionString = PlcControl.plc.GetPlcValueString(rulerPositionKey);
                    double rulerPositionFloat = Math.Round(Tools.GetDoubleStringValue(rulerPositionString), GlobalParams.decimalPlaces);

                    // 计算差值
                    double diffValue = rulerPositionFloat - targetPositionFloat;

                    // 判断是否在公差范围内
                    if (Math.Abs(diffValue) <= tolerance)
                    {
                        consecutiveAttempts++;
                        if (consecutiveAttempts >= consecutiveAttemptsLimit)
                        {
                            Console.WriteLine("位置误差已连续三次满足要求，校准完成！");
                            flag = false;
                            break;
                        }
                    }
                    else
                    {
                        // 如果误差超出公差范围，重置连续计数器
                        consecutiveAttempts = 0;
                    }
                    // 根据误差调整位置
                    double currentPositionFloat = Tools.GetDoubleStringValue(PlcControl.plc.GetPlcDefaultValueString(currentPositionKey));
                    SetAbsolute((currentPositionFloat - diffValue).ToString(), axisName);

                    // 检查超时时间
                    if ((DateTime.Now - startTime).TotalMilliseconds > timeoutMilliseconds)
                    {
                        Console.WriteLine("校准超时退出！");
                        flag = false;
                        break;
                    }
                    i++;
                } while (flag);
                Debug.WriteLine(i);
            }
            else
            {
                Console.WriteLine("未设置光栅尺位置键，无法校准！");
            }
        }


        /// <summary>
        /// 重复走
        /// </summary>
        public void RepeatedRun()
        {
            if (string.IsNullOrEmpty(testNumInput.Text))
            {
                MaterialSnackUtils.MaterialSnack("请输入循环次数！", SnackType.ERROR);
                return;
            }
            // 开始位置
            string testStartPositionText = testStartPositionInput.Text;
            float testStartPosition = float.Parse(testStartPositionText);
            // 正反向
            string directionInputValue = directionInput.Text;
            string testAxis = testAxisInput.Text;
            // 循环次数
            int repeatedNum = Tools.GetIntStringValue(testNumInput.Text);
            string testIndexNum = testIndexNumInput.Text;
            string positionKey = testAxis + "光栅尺当前位置";
            string motorPositionKey = testAxis + "当前位置";
            bool targetFlag = true;
            stopTimeValue = int.Parse(stopTime.Text);
            int i = 0;
            Thread repeatThread = new Thread(() =>
            {
                while (runFlag && i < repeatedNum)
                {
                    string targetPosition = testStartPositionText;
                    if (targetFlag)
                    {
                        if (directionInputValue.Equals("正向"))
                        {
                            targetPosition = (testStartPosition + Tools.GetFloatStringValue(testIndexNum)).ToString();
                        }
                        else if (directionInputValue.Equals("反向"))
                        {
                            targetPosition = (testStartPosition - Tools.GetFloatStringValue(testIndexNum)).ToString();
                        }
                    }
                    Application.Current.Dispatcher.Invoke(new Action(() => currentNumInput.Text = (i + 1) + ""));
                    switch (testAxis)
                    {
                        case "X轴":
                            PlcControl.tagControl.Xaxis.StartAbsolute("10", targetPosition + "");
                            Thread.Sleep(500);
                            Tools.WaitForValue(PlcControl.allTags[DeviceKey.curSpeedKey], "0");
                            break;
                        case "Y轴":
                            PlcControl.tagControl.Yaxis.StartAbsolute("10", targetPosition + "");
                            Thread.Sleep(500);
                            Tools.WaitForValue(PlcControl.allTags[DeviceKey.yCurSpeedKey], "0");
                            break;
                        case "Z1轴":
                            PlcControl.tagControl.Z1axis.StartAbsolute("10", targetPosition + "");
                            Thread.Sleep(500);
                            Tools.WaitForValue(PlcControl.allTags[DeviceKey.z1CurSpeedKey], "0");
                            break;
                        default:
                            break;
                    }
                    if (targetFlag)
                    {
                        Thread.Sleep(stopTimeValue);
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            actualPositions.Text += (string.IsNullOrEmpty(actualPositions.Text) ? "" : ",") + PlcControl.plc.GetPlcValueString(motorPositionKey);
                        }));
                    } else
                    {
                        Thread.Sleep(stopTimeValue);
                    }
                    targetFlag = !targetFlag;
                    // 暂停
                    while (pauseFlag)
                    {
                        Thread.Sleep(100);
                    }
                    i++;
                }

                Application.Current.Dispatcher.Invoke(new Action(() => testStatus.Text = "已完成！"));
                runFlag = false;
                pauseFlag = false;
            });
            repeatThread.IsBackground = true;
            repeatThread.Start();
        }

        private void pauseTestBtn_Click(object sender, RoutedEventArgs e)
        {
            pauseFlag = !pauseFlag;
            pauseTestBtn.Content = pauseFlag ? "继续测量" : "暂停测量";
            testStatus.Text = pauseFlag ? "暂停中..." : "测量中...";
        }

        private void endTestBtn_Click(object sender, RoutedEventArgs e)
        {
            runFlag = false;
            pauseFlag = false;
            pauseTestBtn.Content = "暂停测量";
            testStatus.Text = "准备中...";
        }
        private void compCheckbox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            GlobalParams.runCompFlag = checkBox.IsChecked == true;
            Debug.WriteLine(GlobalParams.runCompFlag);
        }
        bool absoluteFlag = false;
        private void customConfirmPositionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (absoluteFlag)
            {
                return;
            }
            string testAxis = testAxisInput.Text;
            float currentLocation = Tools.GetFloatStringValue(PlcControl.GetCurrentLocation(testAxis, 0));
            absoluteFlag = true;
            string compPositionStr = "0";
            bool stepFlag = stepCheckbox.IsChecked == true;
            string resultPosition = customTargetPositionInput.Text;
            float index = Tools.GetFloatStringValue(resultPosition) - currentLocation;
            // 开始绝对运动
            switch (testAxis)
            {
                case "X轴":
                    compPositionStr = PlcControl.tagControl.Xaxis.StartAbsolute("10", resultPosition); Thread.Sleep(500);
                    Tools.WaitForValue(PlcControl.allTags[DeviceKey.curSpeedKey], "0");
                    break;
                case "Y轴":
                    // 获取补偿后的位置
                    if (stepFlag)
                    {
                        resultPosition = PlcControl.GetCompensateStep2(index, testAxis);
                    }
                    compPositionStr = PlcControl.tagControl.Yaxis.StartAbsolute("10", resultPosition); Thread.Sleep(500);
                    Tools.WaitForValue(PlcControl.allTags[DeviceKey.yCurSpeedKey], "0");
                    break;
                case "Z1轴":
                    compPositionStr = PlcControl.tagControl.Z1axis.StartAbsolute("10", resultPosition); Thread.Sleep(500);
                    Tools.WaitForValue(PlcControl.allTags[DeviceKey.z1CurSpeedKey], "0");
                    break;
                default:
                    break;
            }
            if (compCheckbox.IsChecked == true)
            {
                Task.Run(() => {
                    Thread.Sleep(1000);
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        
                        GlobalParams.upRealPosition = float.Parse(PlcControl.plc.GetPlcValueString(DeviceKey.yGratingRulerCurLocationKey));
                    }));
                    
                });
            }
            
            absoluteFlag = false;
        }

        private void SetAbsolute(string targetPosition, string axisName)
        {
            if (absoluteFlag)
            {
                return;
            }
            absoluteFlag = true;
            // 开始绝对运动
            switch (axisName)
            {
                case "X轴":
                    PlcControl.tagControl.Xaxis.StartAbsolute(defaultSpeed, targetPosition); Thread.Sleep(500);
                    Tools.WaitForValue(PlcControl.allTags[DeviceKey.curSpeedKey], "0");
                    break;
                case "Y轴":
                    PlcControl.tagControl.Yaxis.StartAbsolute(defaultSpeed, targetPosition); Thread.Sleep(500);
                    Tools.WaitForValue(PlcControl.allTags[DeviceKey.yCurSpeedKey], "0");
                    break;
                case "Z1轴":
                    PlcControl.tagControl.Z1axis.StartAbsolute(defaultSpeed, targetPosition); Thread.Sleep(500);
                    Tools.WaitForValue(PlcControl.allTags[DeviceKey.z1CurSpeedKey], "0");
                    break;
                default:
                    break;
            }
            Task.Run(() => {
                Thread.Sleep(1000);
                GlobalParams.upPosition = float.Parse(targetPosition);
                GlobalParams.upRealPosition = float.Parse(PlcControl.plc.GetPlcValueString(DeviceKey.yGratingRulerCurLocationKey));
            });
            absoluteFlag = false;
        }


        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            runFlag = false;
            pauseFlag = false;
        }

        private void repeatedCheckbox_Click(object sender, RoutedEventArgs e)
        {

        }

        private void rulerPositionBtn_Click(object sender, RoutedEventArgs e)
        {
            AutoAdjustmentRuler(customTargetPositionInput.Text, testAxisInput.Text);
        }

        private void stepTestBtn_Click(object sender, RoutedEventArgs e)
        {
            if (absoluteFlag)
            {
                return;
            }

            string numText = testNumInput.Text;

            string testAxis = testAxisInput.Text;
            absoluteFlag = true;
            string compPositionStr = "0";
            // 步进距离
            string stepValue = testIndexNumInput.Text;
            // 开始绝对运动
            switch (testAxis)
            {
                case "X轴":
                    compPositionStr = PlcControl.tagControl.Xaxis.StartAbsolute("10", customTargetPositionInput.Text); Thread.Sleep(500);
                    Tools.WaitForValue(PlcControl.allTags[DeviceKey.curSpeedKey], "0");
                    break;
                case "Y轴":
                    // 获取补偿后的位置
                    string resultPosition = PlcControl.GetCompensateStep(stepValue, testAxis, Tools.GetFloatStringValue(testStartPositionInput.Text));
                    compPositionStr = PlcControl.tagControl.Yaxis.StartAbsolute("10", resultPosition); Thread.Sleep(500);
                    Tools.WaitForValue(PlcControl.allTags[DeviceKey.yCurSpeedKey], "0");
                    actualPositions.Text += (string.IsNullOrEmpty(actualPositions.Text) ? "" : ",") + resultPosition;
                    break;
                case "Z1轴":
                    compPositionStr = PlcControl.tagControl.Z1axis.StartAbsolute("10", customTargetPositionInput.Text); Thread.Sleep(500);
                    Tools.WaitForValue(PlcControl.allTags[DeviceKey.z1CurSpeedKey], "0");
                    break;
                default:
                    break;
            }
            if (compCheckbox.IsChecked == true)
            {
                Task.Run(() => {
                    Thread.Sleep(1000);
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {

                        GlobalParams.upRealPosition = float.Parse(PlcControl.plc.GetPlcValueString(DeviceKey.yGratingRulerCurLocationKey));
                    }));

                });
            }

            absoluteFlag = false;
        }

        private void compTestBtn_Click(object sender, RoutedEventArgs e)
        {

            PositionCorrection.CalculateCompensation();

            List<PositionCompensationModel> models = CurrentUtils.GetPositionCompensationModels();
            PositionCompensationModel axisModel = models.Find(item => item.AxisType.Equals("Y轴-反向"));

            // 将位置和补偿数据解析为数组
            float[] positionNumbers = axisModel.AxisPosition.Split(",").Select(float.Parse).ToArray();
            float[] compensateNumbers = axisModel.AxisCompensate.Split(",").Select(float.Parse).ToArray();
            float lastYCurrentPosition = 95.23313f;
            int directionType = 1;
            float stepIndex = 1f;
            for (int i = 0; i < positionNumbers.Length; i++)
            {
                if (compensateNumbers[i] == 0)
                {
                    break;
                }
                float lastLocationComp = PlcControl.CalculateCompensation(axisModel, lastYCurrentPosition, directionType);
                float compTargetLocation = lastYCurrentPosition + ((directionType == 0 ? stepIndex : -stepIndex) * (i == 0 ? 0 : 1));
                float targetLocationComp = PlcControl.CalculateCompensation(axisModel, compTargetLocation, directionType);
                // 用目标点位的补偿值 - 上一目标值的补偿值 = 2个点之间的差值
                float comp = (float)Math.Round(targetLocationComp - lastLocationComp, GlobalParams.decimalPlaces);
                float tempTargetLocation = compTargetLocation;
                if (directionType == 0)
                {
                    tempTargetLocation += comp;
                }
                else if (directionType == 1)
                {
                    tempTargetLocation -= comp;
                }
                lastYCurrentPosition = tempTargetLocation;
                actualPositions.Text += "," + tempTargetLocation;
                motorPositions.Text += "," + compTargetLocation;
            }

        }

        private void z1CompCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            PlcControl.tagControl.cutting.SetZ1AxisCompStatus(0);
        }

        private void yCompCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            PlcControl.tagControl.cutting.SetYAxisCompStatus(1);
        }

        private void yCompCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            PlcControl.tagControl.cutting.SetYAxisCompStatus(0);
        }

        private void z1CompCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            PlcControl.tagControl.cutting.SetZ1AxisCompStatus(1);
        }
    }
}
