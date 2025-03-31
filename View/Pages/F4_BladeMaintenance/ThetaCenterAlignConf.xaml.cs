using NPOI.SS.Formula.Functions;
using Osklib.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Model.sqlite;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.F4_BladeMaintenance
{
    /// <summary>
    /// ThetaCenterAlignConf.xaml 的交互逻辑
    /// </summary>
    public partial class ThetaCenterAlignConf : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;
        ThetaCenterAlignModel thetaCenterAlignModel = new ThetaCenterAlignModel();
        public ThetaCenterAlignConf()
        {
            InitializeComponent();
            mainWindow = Application.Current.MainWindow as MainWindow;
        }
        // 当前状态，0 参数设置 1 切割中 2 切割完成，确认中
        int status = 0;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            rightPage = mainWindow.rightFrame.Content as RightPage;
            operatePage = mainWindow.operateFrame.Content as OperatePage;

            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);

            rightPage.btnSure.Visibility = Visibility.Visible;
            rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked);

            rightPage.btnCutStart.Visibility = Visibility.Visible;
            rightPage.btnCutStart.SetRightClickedHandler(BtnCutStart_RightClicked);

            rightPage.btnCutPause.SetRightClickedHandler(BtnCutPause_RightClicked);
            rightPage.btnCutReStart.SetRightClickedHandler(BtnCutReStart_RightClicked);

            mainWindow.UpdateOperatePage(OperateData.GetThetaCenterAlignConfOperate(), OperateClickHandler);
            var list = SqlHelper.Table<ThetaCenterAlignModel>()
                       .Where(t => t.Id == 1).ToList();
            // 加载参数信息
            thetaCenterAlignModel = list.Count > 0 ? list[0] : new ThetaCenterAlignModel();
            cutSpeed.Text = thetaCenterAlignModel?.cutSpeed;
            workThickness.Text = thetaCenterAlignModel?.workThickness;
            workSize.Text = thetaCenterAlignModel?.workSize;
            bladeHeight.Text = thetaCenterAlignModel?.bladeHeight;
            spindleSpeed.Text = thetaCenterAlignModel?.spindleSpeed;
            tapeThickness.Text = thetaCenterAlignModel?.tapeThickness;
        }

        private void BtnCutReStart_RightClicked(object? sender, bool e)
        {
            if (btnRunFlag)
            {
                return;
            }
            btnRunFlag = true;
            pauseFlag = false;
            restartFlag = true;
            MaterialSnackUtils.MaterialSnack("切割中....", MaterialSnackUtils.SnackType.SUCCESS, 0);
        }
        bool btnRunFlag = false;
        private void BtnCutPause_RightClicked(object? sender, bool e)
        {
            if (btnRunFlag)
            {
                return;
            }
            pauseFlag = true;
            btnRunFlag = true;
            PlcControl.tagControl.cutting.StopCut(1);
            Task.Run(() =>
            {
                MaterialSnackUtils.MaterialSnack("正在暂停！", MaterialSnackUtils.SnackType.WARNING);
                if (CutOperateUtils.MonitorCutStatusFalse("False", 90000))
                {
                    MaterialSnackUtils.MaterialSnack("暂停中...", MaterialSnackUtils.SnackType.WARNING, 0);
                }
                else
                {
                    // 如果停止失败，则强行结束切割
                    Tools.LogError("暂停失败！强行退出切割状态！");
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    rightPage.btnCutPause.Visibility = Visibility.Collapsed;
                    rightPage.btnCutReStart.Visibility = Visibility.Visible;
                });
                PlcControl.tagControl.wholeDevice.SetYellowLightFlash(1);
                // PlcControl.tagControl.wholeDevice.SetBuzzerStatus(1);
                btnRunFlag = false;
            });
        }

        private void BtnCutStart_RightClicked(object? sender, bool e)
        {
            if (runFlag)
            {
                return;
            }
            DisposeCut();
        }
        CancellationTokenSource cts = new CancellationTokenSource();
        bool runFlag = false;
        bool pauseFlag = false;
        bool restartFlag = false;
        int timeout = 90;
        float globalZStartLocation = 0;
        int currentCutLine = 0;
        string thetaDeg = "0";
        /// <summary>
        /// 处理切割逻辑
        /// </summary>
        private void DisposeCut()
        {
            // 测高信息
            BladeHeightModel bladeHeightModel = CurrentUtils.GetBladeHeightModel();
            // 判断是否已测高
            if (string.IsNullOrEmpty(bladeHeightModel.BladeHeight) || bladeHeightModel.BladeHeight.Equals("0"))
            {
                MaterialSnackUtils.MaterialSnack("请先测高！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            
            // 切2刀，第一刀为当前角度，第二刀为当前角度+90度
            Task.Run(() =>
            {
                runFlag = true;
                string tempCurrentCutLine = "0";
                for (int i = 0; i < 2; i++)
                {
                    if (i == 1)
                    {
                        thetaDeg = "90";
                    }
                    // 设置切割参数
                    SetCutParams(thetaDeg, bladeHeightModel.BladeHeight);
                    if (i == 0 || restartFlag)
                    {
                        Thread.Sleep(500);
                        PlcControl.tagControl.cutting.StartCut(0);
                        Thread.Sleep(10);
                        PlcControl.tagControl.cutting.StartCut(1);
                        if (i == 0)
                        {
                            CheckError();
                        }
                        if (CutOperateUtils.MonitorCutStatus())
                        {
                            btnRunFlag = false;
                            status = 1;
                            DisposeStatus();
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                // 显示暂停按钮 隐藏开始按钮、确认按钮、返回按钮
                                rightPage.btnCutPause.Visibility = Visibility.Visible;
                                rightPage.btnCutStart.Visibility = Visibility.Collapsed;
                                rightPage.btnSure.Visibility = Visibility.Collapsed;
                                rightPage.btnBack.Visibility = Visibility.Collapsed;
                            });
                            MaterialSnackUtils.MaterialSnack("切割中....", MaterialSnackUtils.SnackType.SUCCESS, 0);
                        }
                        Tools.LogInfo("发送开始切割信号！");
                    }
                    
                    string currentCount = "0";
                    do
                    {
                        // 定期检查切割进度
                        currentCount = PlcControl.plc.GetPlcValueString(DeviceKey.cutNumKey);
                        Thread.Sleep(100);
                        // 再检查是否有报警信息，有报警则暂停
                        if (cts.Token.IsCancellationRequested)
                            runFlag = false;
                    } while (runFlag && tempCurrentCutLine.Equals(currentCount));
                    tempCurrentCutLine = currentCount;
                    currentCutLine++;
                    // 监听Z轴是否上升，如果上升，则表面当前刀已完成 20.58 21
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    while (stopwatch.Elapsed.TotalSeconds < timeout)
                    {
                        String runValue = PlcControl.plc.GetPlcValueString(DeviceKey.z1CurLocationKey);
                        if (float.Parse(globalZStartLocation.ToString()) - float.Parse(runValue) < -0.01)
                        {
                            continue;
                        }
                        else
                        {
                            stopwatch.Stop();
                            break;
                        }
                    }
                    stopwatch.Stop();
                    // 如果是停机中，则暂停运行
                    while (pauseFlag)
                    {
                        if (cts.Token.IsCancellationRequested)
                            pauseFlag = false;
                        Thread.Sleep(100);
                    }
                }
                //  发送结束切割信号
                PlcControl.tagControl.cutting.EndFullAutoCut();
                // 等待切割结束
                if (CutOperateUtils.MonitorCutStatusFalse())
                {
                    Tools.LogInfo("切割结束");
                }
                MaterialSnackUtils.MaterialSnack("切割完成！", MaterialSnackUtils.SnackType.WARNING);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    rightPage.btnCutPause.Visibility = Visibility.Collapsed;
                    rightPage.btnCutStart.Visibility = Visibility.Visible;
                    rightPage.btnSure.Visibility = Visibility.Visible;
                    rightPage.btnBack.Visibility = Visibility.Visible;
                });
                runFlag = false;
                status = 2;
                DisposeStatus();
            });
        }

        /// <summary>
        /// 检查异常状态 急停后，要全部重新标定一次
        /// </summary>
        /// <returns></returns>
        private void CheckError()
        {
            Thread thread = new Thread(() =>
            {
                while (runFlag)
                {
                    ObservableCollection<AlarmItem> list = PlcControl.allAlarm;
                    if (list.Count > 0)
                    {
                        Tools.LogError("异常报警！");
                        runFlag = false;
                        cts.Cancel();
                    }
                    Thread.Sleep(50);
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private void SetCutParams(string thetaDeg, string bladeHeightValue)
        {
            // Y 开始位置
            float yCutLocation = GlobalParams.thetaCenterLocationY;
            // 切割X轴偏移量
            float offsetX = 10;
            float workSize = Tools.GetFloatStringValue(thetaCenterAlignModel.workSize);
            float avgWorkWidth = workSize / 2;
            float bladeHeight = float.Parse(bladeHeightValue); // 设置刀具高度，单位毫米
            // 构建切割数据
            float zEndIndex = bladeHeight - Tools.GetFloatStringValue(thetaCenterAlignModel.bladeHeight);
            float zStartLocation = zEndIndex - GlobalParams.zCutRaisedHeight;
            // X轴开始位置
            float xStartLocation = 122 - avgWorkWidth - offsetX;
            // float xStartLocation = GlobalParams.thetaCenterLocationX - avgWorkWidth - offsetX;
            float xEndLocation = xStartLocation + workSize + offsetX;
            globalZStartLocation = zStartLocation;
            // 打印所有传入参数
            Tools.LogInfo("Z轴开始位置：" + zStartLocation);
            Tools.LogInfo("X轴开始位置：" + xStartLocation);
            Tools.LogInfo("X轴结束位置：" + xEndLocation);
            Tools.LogInfo("Y轴开始位置：" + yCutLocation);
            Tools.LogInfo("刀高度：" + bladeHeight);
            Tools.LogInfo("刀角度：" + thetaDeg);
            // 发送切割参数
            PlcControl.tagControl.cutting.SetCutParams(Tools.GetFloatStringValue(thetaCenterAlignModel.cutSpeed)
                , zEndIndex.ToString(), zStartLocation, xStartLocation.ToString()
                , xEndLocation.ToString(), yCutLocation.ToString(), "0", thetaDeg, "20000", 0);
        }

        private void BtnSure_RightClicked(object? sender, bool e)
        {
            // 如果状态是0 则保存配置信息
            if (status == 0)
            {
                thetaCenterAlignModel.cutSpeed = cutSpeed.Text;
                thetaCenterAlignModel.workThickness = workThickness.Text;
                thetaCenterAlignModel.workSize = workSize.Text;
                thetaCenterAlignModel.bladeHeight = bladeHeight.Text;
                thetaCenterAlignModel.spindleSpeed = spindleSpeed.Text;
                thetaCenterAlignModel.tapeThickness = tapeThickness.Text;
                if (thetaCenterAlignModel.Id != 1)
                {
                    SqlHelper.Add(thetaCenterAlignModel);
                } else
                {
                    SqlHelper.Update(thetaCenterAlignModel);
                }
                MaterialSnackUtils.MaterialSnack("保存成功！", MaterialSnackUtils.SnackType.SUCCESS);
            } else if (status == 2)
            {
                // 计算中心点位置
            }
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            if (status == 0)
            {
                // 回复切割面到Ch 1
                CurrentUtils.InitCutCh();
                // 退出切割模式
                PlcControl.tagControl.cutting.EnterFullAutoInit(0);
                mainWindow.NavigateToPage("MainMenu");
            }
            else if (status == 2) 
            { 
                status = 0;
                DisposeStatus();
            }
            
        }
        // 0度中心点X坐标
        private string centerPoint0X;
        // 0度中心点Y坐标
        private string centerPoint0Y;
        // 90度中心点X坐标
        private string centerPoint90X;
        // 90度中心点Y坐标
        private string centerPoint90Y;
        private void OperateClickHandler(object sender, int code)
        {
            switch (code)
            {
                case 44001:
                    status = 2;
                    break;
                case 44002:
                    CommonOperate.GetInstance().AutoFocus(2, mainWindow);
                    break;
                case 44003:
                    // 实行测量 旋转角度 找到中心点
                    if (thetaDeg.Equals("0"))
                    {
                        centerPoint0X = PlcControl.plc.GetPlcValueString(DeviceKey.curLocationKey);
                        centerPoint0Y = PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey);
                        PlcControl.tagControl.ThetaAxis.StartAbsolute("90", "90");
                        thetaDeg = "90";
                    } else if (thetaDeg.Equals("90"))
                    {
                        centerPoint90X = PlcControl.plc.GetPlcValueString(DeviceKey.curLocationKey);
                        centerPoint90Y = PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey);
                        PlcControl.tagControl.ThetaAxis.StartAbsolute("90", "0");
                        thetaDeg = "0";
                    }
                    if (!string.IsNullOrEmpty(centerPoint0X) && !string.IsNullOrEmpty(centerPoint0Y)
                        && !string.IsNullOrEmpty(centerPoint90X) && !string.IsNullOrEmpty(centerPoint90Y))
                    {
                        var zeroDegreePoint = (centerPoint0X, centerPoint0Y);
                        var ninetyDegreePoint = (centerPoint90X, centerPoint90Y);

                        // 调用方法计算原点坐标
                        var origin = CalculateOrigin(zeroDegreePoint, ninetyDegreePoint);
                        thetaCenterX.Text = origin.Item1.ToString("F4");
                        thetaCenterY.Text = origin.Item2.ToString("F4");
                        // 输出结果
                        Console.WriteLine($"原点坐标为: ({origin.Item1}, {origin.Item2})");
                    }
                    break;
                default:
                    break;
            }
            DisposeStatus();
        }

        private void DisposeStatus()
        {
            bool inputStatus = true;
            Application.Current.Dispatcher.Invoke(() =>
            {
                switch (status)
                {
                    case 0:
                        thetaCenterParamsGrid.Visibility = Visibility.Visible;
                        dimmingGrid.Visibility = Visibility.Collapsed;
                        directionGrid.Visibility = Visibility.Collapsed;
                        centerPanel.Visibility = Visibility.Collapsed;
                        break;
                    case 1:
                        // 文本框全部禁用
                        inputStatus = false;
                        thetaCenterParamsGrid.Visibility = Visibility.Visible;
                        dimmingGrid.Visibility = Visibility.Collapsed;
                        directionGrid.Visibility = Visibility.Collapsed;
                        break;
                    case 2:
                        thetaCenterParamsGrid.Visibility = Visibility.Collapsed;
                        centerPanel.Visibility = Visibility.Visible;
                        dimmingGrid.Visibility = Visibility.Visible;
                        directionGrid.Visibility = Visibility.Visible;
                        break;
                    default:
                        break;
                }
                InputStatusDispose(inputStatus);
            });
        }
        // 定义一个公共方法来计算原点坐标
        public static (double, double) CalculateOrigin((string, string) zeroDegreePoint, (string, string) ninetyDegreePoint)
        {
            // 0度时的交叉点坐标
            double x0 = Tools.GetDoubleStringValue(zeroDegreePoint.Item1);
            double y0 = Tools.GetDoubleStringValue(zeroDegreePoint.Item2);

            // 90度时的交叉点坐标
            double x90 = Tools.GetDoubleStringValue(ninetyDegreePoint.Item1);
            double y90 = Tools.GetDoubleStringValue(ninetyDegreePoint.Item2);

            // 计算原点坐标
            double originX = (x0 + y90) / 2;
            double originY = (y0 + x90) / 2;

            return (originX, originY);
        }
        private void InputStatusDispose(bool inputStatus)
        {
            workSize.IsEnabled = inputStatus;
            workThickness.IsEnabled = inputStatus;
            tapeThickness.IsEnabled = inputStatus;
            bladeHeight.IsEnabled = inputStatus;
            cutSpeed.IsEnabled = inputStatus;
            spindleSpeed.IsEnabled = inputStatus;
        }
    }
}
