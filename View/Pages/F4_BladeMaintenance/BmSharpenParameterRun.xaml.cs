using NPOI.OpenXmlFormats.Dml.Diagram;
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
using static 精密切割系统.Helpers.MaterialSnackUtils;

namespace 精密切割系统.View.Pages.F4_BladeMaintenance
{
    /// <summary>
    /// BmSharpenParameterRun.xaml 的交互逻辑
    /// </summary>
    public partial class BmSharpenParameterRun : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;
        public BmSharpenParameterRun()
        {
            InitializeComponent();
            mainWindow = Application.Current.MainWindow as MainWindow;
        }
        //获取参数
        string IdStr;
        string Flag;
        string BladeLotID;
        int cutDirection = -1;
        double intensity = GlobalParams.intensityRatio;
        private static int currentCutLine = 0;
        private string plcCurrentNum = "0";
        private bool setStartCutFlag = false;
        int cutNum = 0;
        static bool stopFlag = false;
        static bool runFlag = false;

        private void SubFive_OnClick(object sender, RoutedEventArgs e)
        {
            AdjustIntensity(-0.05m);
        }

        private void SubOne_OnClick(object sender, RoutedEventArgs e)
        {
            AdjustIntensity(-0.01m);
        }

        private void AddOne_OnClick(object sender, RoutedEventArgs e)
        {
            AdjustIntensity(0.01m);
        }

        private void AddFive_OnClick(object sender, RoutedEventArgs e)
        {
            AdjustIntensity(0.05m);
        }

        private void SetLightRatio(decimal t_intensity)
        {
            intensity = Convert.ToDouble(t_intensity);
            intensity = Math.Clamp(intensity, 0.01, 1);
            //Debug.WriteLine("值是多少t_intensity？===" + intensity);
            dirLightRatio.Text = (Math.Round(intensity * 100, 2)).ToString(); ;// (Math.Round((double)intensity / 255 * 100, 2)).ToString();
        }

        /// <summary>
        /// adjustment 是小数，表示百分比的小数 比如0.05表示 百分之5
        /// </summary>
        /// <param name="adjustment"></param>
        private void AdjustIntensity(decimal adjustment)
        {
            decimal t_intensity = Convert.ToDecimal(intensity);
            t_intensity += adjustment;  //0.8 + 0.05 = 0.85
            // 设置初始光源亮度v_intensity = 255*0.85 = 216.75 
            int v_intensity = (int)Math.Ceiling(t_intensity * 255);
            int reNum = Math.Clamp(v_intensity, 1, 255); //值在这个区间
            CameraUtils.SetLightIntensity(reNum, GlobalParams.LightIntensityChannel);
            //Debug.WriteLine("值是多少？===" + reNum);
            //t_intensity*100
            SetLightRatio(t_intensity);
        }

        BmSharpenParameterModel _model = new BmSharpenParameterModel();
        List<int> cutNumList = new List<int>();
        List<float> feedSpeedList = new List<float>();
        List<float> cutFeedSpeedList = new List<float>();
        float yCurrentPosition = 0;
        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            exitStatus = 0;
            rightPage = mainWindow.rightFrame.Content as RightPage;
            operatePage = mainWindow.operateFrame.Content as OperatePage;

            rightPage.PanelAction.Visibility = Visibility.Visible;
            /*rightPage.btnCutPause.Visibility = Visibility.Visible;
            rightPage.btnCutPause.GlobalRunOperateFlag = true;
            rightPage.btnCutPause.BackFlag = false;
            rightPage.btnCutPause.SetRightClickedHandler(BtnCutPause_RightClicked);*/

            mainWindow.UpdateOperatePage(OperateData.GetTab4402Operate(), OperatePage_onClicked);

            //获取参数
            IdStr = QueryUtils.GetValueFromQueryParams(this, "Id");
            Flag = QueryUtils.GetValueFromQueryParams(this, "Flag");
            BladeLotID = QueryUtils.GetValueFromQueryParams(this, "BladeLotID");
            string cutDirectionStr = QueryUtils.GetValueFromQueryParams(this, "cutDirection");
            if (!string.IsNullOrEmpty(cutDirectionStr))
            {
                if (int.TryParse(cutDirectionStr, out int tempCutDirection))
                {
                    cutDirection = tempCutDirection;
                }
            }
            sharpenTitle.Content = "磨刀进行状态";
            long id = long.Parse(IdStr);
            List<BmSharpenParameterModel> list = await SqlHelper.TableAsync<BmSharpenParameterModel>()
                            .Where(t => t.Id == id).ToListAsync();
            if (list.Count > 0) {
                _model = list[0];
                // 初始化数据
                bladeHeight.Text = _model.CutHeight + "";
                FeedSpeed.Text = _model.MoCutOneSpeed;

                cutNumList.Add(Tools.GetIntStringValue(_model.MoCutOneNo));
                cutNumList.Add(Tools.GetIntStringValue(_model.MoCutTwoNo));
                cutNumList.Add(Tools.GetIntStringValue(_model.MoCutThreeNo));
                cutNumList.Add(Tools.GetIntStringValue(_model.MoCutFourNo));
                cutNumList.Add(Tools.GetIntStringValue(_model.MoCutFiveNo));
                cutNumList.Add(Tools.GetIntStringValue(_model.MoCutSixNo));
                cutNumList.Add(Tools.GetIntStringValue(_model.MoCutSevenNo));
                cutNumList.Add(Tools.GetIntStringValue(_model.MoCutEightNo));
                cutNumList.Add(Tools.GetIntStringValue(_model.MoCutNineNo));
                cutNumList.Add(Tools.GetIntStringValue(_model.MoCutTenNo));

                // 累积cutFeedSpeedList
                feedSpeedList.Add(Tools.GetFloatStringValue(_model.MoCutOneSpeed));
                feedSpeedList.Add(Tools.GetFloatStringValue(_model.MoCutTwoSpeed));
                feedSpeedList.Add(Tools.GetFloatStringValue(_model.MoCutThreeSpeed));
                feedSpeedList.Add(Tools.GetFloatStringValue(_model.MoCutFourSpeed));
                feedSpeedList.Add(Tools.GetFloatStringValue(_model.MoCutFiveSpeed));
                feedSpeedList.Add(Tools.GetFloatStringValue(_model.MoCutSixSpeed));
                feedSpeedList.Add(Tools.GetFloatStringValue(_model.MoCutSevenSpeed));
                feedSpeedList.Add(Tools.GetFloatStringValue(_model.MoCutEightSpeed));
                feedSpeedList.Add(Tools.GetFloatStringValue(_model.MoCutNineSpeed));
                feedSpeedList.Add(Tools.GetFloatStringValue(_model.MoCutTenSpeed));
                for (int i = 0; i < cutNumList.Count; i++)
                {
                    int value = cutNumList[i];
                    for (int j = 0; j < value; j++)
                    {
                        cutFeedSpeedList.Add(feedSpeedList[i]);
                    }
                    cutNum += value;
                }

                currentCutNum.Text = "0";
                totalCutNum.Text = _model.CoCutNum > 0 ? (_model.CoCutNum > cutNum ? cutNum : _model.CoCutNum) + "" : cutNum.ToString();
            }
            dirLightRatio.Text = (Math.Round(intensity * 100, 2)).ToString();
            yCurrentPosition = float.Parse(PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey));
            Debug.WriteLine($"yCurrentPosition:{yCurrentPosition}");
            errorFlag = false;
            // 执行切割
            Thread _thread = new Thread(cut);
            _thread.IsBackground = true;
            _thread.Start();
        }

        private void OperatePage_onClicked(object? sender, int code)
        {
            switch (code)
            {
                case 2023:
                    // 手动校准 type 
                    mainWindow.mainFrame.Source = new Uri($"View/Pages/F2_ManualOperation/MQManualAlignmentConf.xaml?type=2", UriKind.Relative);
                    break;
                case 4406:
                    // 停止磨刀
                    if (exitStatus == 0)
                    {
                        exitStatus = 1;
                        MaterialSnackUtils.MaterialSnack("再次点击，停止磨刀。", MaterialSnackUtils.SnackType.WARNING);
                        return;
                    }
                    runFlag = false;
                    break;
                case 2442:
                    CommonOperate.GetInstance().AutoFocus(2, mainWindow);
                    break;
                default:
                    break;
            }
        }

        bool startRunFlag = false;
        private void BtnStart_RightClicked(object? sender, bool e)
        {
            // 如果有异常，则不能点击
            if (errorFlag)
            {
                return;
            }
            if (startRunFlag) {
                // MaterialSnackUtils.showOperateLimitMsg();
                return;
            }
            startRunFlag = true;
            stopFlag = false;
            errorFlag = false;
            Thread thread = new Thread(() => {
                // 如果在运行中，则显示暂停按钮
                Tools.WaitForValue(DeviceKey.cutStatusKey, 0);
                GlobalParams.globalRunFlag = true;
                Application.Current.Dispatcher.Invoke(() => {
                    rightPage.btnCutPause.Visibility = Visibility.Visible;
                    stopGrid.Visibility = Visibility.Collapsed;
                    rightPage.btnBack.Visibility = Visibility.Collapsed;
                    rightPage.btnCutStart.Visibility = Visibility.Collapsed;
                    mainWindow.UpdateOperatePage(OperateData.GetTab4402Operate(), OperatePage_onClicked);
                    sharpenTitle.Content = "磨刀进行状态";
                });
                startRunFlag = false;
            });
            thread.IsBackground = true;
            thread.Start();



        }
        private void exit()
        {
            runFlag = false;
            stopFlag = false;
            if (!errorFlag)
            {
                if (!Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.cutStatusKey)))
                {
                    GlobalParams.globalRunFlag = false;
                    return;
                }
                Task.Run(async () =>
                {
                    if (CommonCheck.GetParamsStatus(DeviceKey.workpieceBlowingStatusKey))
                    {
                        await Task.Delay(3000);
                        await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
                    }
                });
            }
            errorFlag = false;
            GlobalParams.globalRunFlag = false;
            ToSharpenParamenterForm();
        }
        public void ToSharpenParamenterForm()
        {
            // 如果需要磨刀后清除测高数据
            if ("1".Equals(_model.IfCorrectHeight))
            {
                // 清除测高数据
                updateData();
            }
            mainWindow.NavigateToPage("Pages/F4_BladeMaintenance/BmSharpenParameterForm", "Id=" + IdStr + "&Flag=" + Flag + "&BladeLotID=" + BladeLotID);
        }
        //换刀后修改高度数据为0，测量后修改为测量值
        public async void updateData()
        {
            BladeHeightModel _model;
            var list = await SqlHelper.TableAsync<BladeHeightModel>()
                    .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                _model = new BladeHeightModel();
                await SqlHelper.AddAsync(_model);
            }
            else
            {
                _model = list[0];

            }
            _model.BladeHeight = "0";
            //保存数据
            await SqlHelper.UpdateAsync(_model);
        }
        int exitStatus = 0;
        private void BtnBack_RightClicked(object? sender, bool e)
        {
            if (exitStatus == 0)
            {
                exitStatus = 1;
                MaterialSnackUtils.MaterialSnack("再次点击返回，停止磨刀。", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            runFlag = false;
        }
        bool pauseRunFlag = false;
        private void BtnCutPause_RightClicked(object? sender, bool e)
        {
            bool cutRunFlag = Tools.TrueFlag(DeviceKey.cutStatusKey);
            if (cutRunFlag)
            {
                return;
            }
            if (pauseRunFlag)
            {
                // MaterialSnackUtils.showOperateLimitMsg();
                return;
            }
            pauseRunFlag = true;
            stopFlag = true;
            PlcControl.tagControl.cutting.StopCut(1);
            MaterialSnackUtils.MaterialSnack("正在暂停...", MaterialSnackUtils.SnackType.WARNING, 0);
            Thread _thread = new Thread(() => {
                // 监听切割状态，如果是true，则显示开始按钮
                Tools.WaitForValue(DeviceKey.cutStatusKey, 1);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    stopGrid.Visibility = Visibility.Visible;
                    rightPage.btnCutPause.Visibility = Visibility.Collapsed;
                    rightPage.btnBack.Visibility = Visibility.Visible;
                    rightPage.btnBack.BackFlag = false;
                    rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
                    rightPage.btnCutStart.Visibility = Visibility.Visible;
                    rightPage.btnCutStart.BackFlag = false;
                    rightPage.btnCutStart.SetRightClickedHandler(BtnStart_RightClicked);
                    mainWindow.UpdateOperatePage(OperateData.GetTab4403Operate(), OperatePage_onClicked);
                    sharpenTitle.Content = "磨刀暂停状态";
                });
                
                pauseRunFlag = false;
            });
            _thread.IsBackground = true;
            _thread.Start();

        }

        private async void cut()
        {
            Thread.Sleep(1000);
            // 等待切割模式准备好
            bool joinFlag = Tools.WaitForValue(DeviceKey.cutStatusKey, 1);
            if (!joinFlag)
            {
                MaterialSnackUtils.MaterialSnack("进入磨刀模式失败！", SnackType.WARNING, 0);
                exit();
                return;
            }
            // 监听轴是否都为准备好的状态
            bool axisStatus = CommonCheck.AxisReady(false);
            do
            {
                axisStatus = CommonCheck.AxisReady(false);
                Thread.Sleep(100);
            } while (!axisStatus);
            // 循环刀数和速度
            if (cutNum <= 0)
            {
                MaterialSnackUtils.MaterialSnack("无磨刀数据！", MaterialSnackUtils.SnackType.ERROR);
                return;
            }
            Application.Current.Dispatcher.Invoke(() => {
                currentCutNum.Text = "1";
            });
            runFlag = true;
            CheckError();
            setStartCutFlag = true;
            GlobalParams.globalRunFlag = true;
            GlobalParams.upPosition = -100;
            GlobalParams.upRealPosition = -100;
            int timeout = 90;
            for (int i = 0; i < cutNum; i++)
            {
                currentCutLine = i;
                // 如果参数设置的切割刀数大于0的话，判断当前刀数是否等于，如果等于，则结束
                if (_model.CoCutNum > 0 && plcCurrentNum.Equals(_model.CoCutNum.ToString()))
                {
                    break;
                }
                
                // 获取当前速度
                float feedSpeed = cutFeedSpeedList[i];
                float cutDistance = 0;
                // 设置切割参数
                bool flag = SetParams(feedSpeed, yCurrentPosition, ref cutDistance);
                // 如果设置参数错误 则返回
                if (!flag) { 
                    return;
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FeedSpeed.Text = feedSpeed.ToString();
                });
                if (setStartCutFlag)
                {
                    PlcControl.tagControl.cutting.StartCut(0);
                    Thread.Sleep(10);
                    PlcControl.tagControl.cutting.StartCut(1);
                    MaterialSnackUtils.MaterialSnack("磨刀中....", MaterialSnackUtils.SnackType.SUCCESS, 0);
                    setStartCutFlag = false;

                    Application.Current.Dispatcher.Invoke(() => {
                        rightPage.btnCutPause.Visibility = Visibility.Visible;
                        rightPage.btnCutPause.GlobalRunOperateFlag = true;
                        rightPage.btnCutPause.BackFlag = false;
                        rightPage.btnCutPause.SetRightClickedHandler(BtnCutPause_RightClicked);
                    });
                }
                bool stopRunFlag = false;
                string tempPlcCurrentNum = plcCurrentNum;
                bool countFlag = false;
                // 如果不相等，说明当前刀已经开始，要发送下一刀数据
                do
                {
                    if (!runFlag)
                    {
                        break;
                    }
                    string value = null;
                    try
                    {
                        value = PlcControl.plc.GetPlcValueString(DeviceKey.cutNumKey);
                    }
                    catch (Exception ex) {
                        Tools.LogError("磨刀中，获取当前刀数失败");
                    }
                    if (value != null)
                    {
                        tempPlcCurrentNum = value;
                    }
                    Thread.Sleep(100);
                    // 如果是暂停，则一直监听切割状态，当为true的时候，说明已经停止，然后等待开始信号
                    if (stopFlag)
                    {
                        if (!stopRunFlag)
                        {
                            stopRunFlag = true;
                            Thread.Sleep(1500);
                            // 等待切割状态为true
                            // Tools.WaitForValue(DeviceKey.cutStatusKey, 1);

                            Stopwatch stopwatch = Stopwatch.StartNew();
                            string value1 = "1";
                            string value2 = "True";
                            while (stopwatch.IsRunning)
                            {
                                if (timeout > 0 && stopwatch.Elapsed.TotalSeconds > timeout)
                                {
                                    stopwatch.Stop();
                                }
                                Task.Delay(100);
                                String runValue = PlcControl.plc.GetPlcValueString(DeviceKey.cutStatusKey);
                                if (!countFlag)
                                {
                                    value = PlcControl.plc.GetPlcValueString(DeviceKey.cutNumKey);
                                    if (!plcCurrentNum.Equals(value))
                                    {
                                        CutOperateUtils.SetCutRecord(cutDistance);
                                        // 设置当前刀数
                                        Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            currentCutNum.Text = (i + 1).ToString();
                                        });
                                        countFlag = true;
                                    }
                                }
                                if (!value1.Equals(runValue) && !value2.Equals(runValue))
                                {
                                    continue;
                                }
                                else
                                {
                                    stopwatch.Stop();
                                }
                            }

                            MaterialSnackUtils.MaterialSnack("暂停中....", MaterialSnackUtils.SnackType.SUCCESS, 0);
                            GlobalParams.globalRunFlag = false;
                            if (CommonCheck.GetParamsStatus(DeviceKey.workpieceBlowingStatusKey))
                            {
                                // 吹气4秒
                                Thread.Sleep(4000);
                                await PlcControl.tagControl.wholeDevice.OpenWorkpieceBlowingAsync();
                            }
                            while (stopFlag)
                            {
                                if (!runFlag)
                                {
                                    break;
                                }

                                Thread.Sleep(50);
                            }
                            if (runFlag)
                            {
                                PlcControl.tagControl.cutting.StartCut(0);
                                Thread.Sleep(10);
                                PlcControl.tagControl.cutting.StartCut(1);
                                MaterialSnackUtils.MaterialSnack("磨刀中....", MaterialSnackUtils.SnackType.SUCCESS, 0);
                            }
                            
                        }
                    }
                } while (plcCurrentNum.Equals(tempPlcCurrentNum));
                stopRunFlag = false;
                if (!runFlag)
                {
                    break;
                }
                plcCurrentNum = tempPlcCurrentNum;
                if (!countFlag)
                {
                    CutOperateUtils.SetCutRecord(cutDistance);
                    // 设置当前刀数
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        currentCutNum.Text = (i + 1).ToString();
                    });
                }
                countFlag = false;
            }
            // 完成切割
            // 发送结束信号
            PlcControl.tagControl.cutting.EndFullAutoCut();
            // 等待切割状态为true
            if (!errorFlag)
            {
                Tools.WaitForValue(DeviceKey.cutStatusKey, 1);
                // 切割完成
                MaterialSnackUtils.MaterialSnack("磨刀完成！", MaterialSnackUtils.SnackType.SUCCESS);
            }
            exit();
        }


        /// <summary>
        /// 等待tag的值达到目标值，timeout和interval单位为秒，超时后会返回false
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="type">1 true 0 false</param>
        /// <returns></returns>
        public static bool WaitForValue(string tagName, int type, double timeout = 60)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            string value1 = type == 0 ? "0" : "1";
            string value2 = type == 0 ? "False" : "True";
            while (stopwatch.IsRunning)
            {
                if (timeout > 0 && stopwatch.Elapsed.TotalSeconds > timeout)
                {
                    stopwatch.Stop();
                    return false;
                }
                Task.Delay(100);
                String runValue = PlcControl.plc.GetPlcValueString(tagName);
                if (!value1.Equals(runValue) && !value2.Equals(runValue))
                {
                    continue;
                }
                else
                {
                    stopwatch.Stop();
                    return true;
                }
            }
            return false;
        }

        private bool SetParams(float feedSpeed, float yCurrentPosition, ref float cutDistance)
        {
            // 刀片测高位置
            BladeHeightModel bladeHeightModel = CurrentUtils.GetBladeHeightModel();
            float testBladeHeight = float.Parse(bladeHeightModel.BladeHeight); 

            // 刀片高度 = 工件厚度 + 膜的厚度 - 切割深度
            // Z轴下降位置 = 测高位置 - 刀片高度 + 补偿深度
            float zEndIndex = testBladeHeight - _model.CutHeight + GlobalParams.cutDepthOffset;
            if (zEndIndex > testBladeHeight)
            {
                MaterialSnackUtils.MaterialSnack("Z1轴位置超限！", MaterialSnackUtils.SnackType.ERROR);
                return false;
            }
            if (cutDirection == -1)
            {
                MaterialSnackUtils.MaterialSnack("请设置切割方向！", MaterialSnackUtils.SnackType.WARNING);
                return false;
            }
            FileTableItemModel tableItemModel = CurrentUtils.GetFileTableItemModel();
            // 设置/计算切割相关参数
            float avgWorkbenchCh1 = tableItemModel.WorkbenchCh1 / 2;
            // Z轴开始位置 = Z轴下降位置 - 2
            float zStartLocation = zEndIndex - GlobalParams.zCutRaisedHeight;
            // X轴开始位置 theta轴中心点位置 - 
            float xStartLocation = GlobalParams.thetaCenterLocationX - avgWorkbenchCh1 - _model.CoOffsetX;
            // X轴结束位置
            float xEndLocation = xStartLocation + _model.CoXDistance + _model.CoOffsetX;
            Debug.WriteLine($"xStartLocation:{xStartLocation} xEndLocation:{xEndLocation}");
            Debug.WriteLine($"avgWorkbenchCh1:{avgWorkbenchCh1} _model.CoXDistance:{_model.CoXDistance}");
            // 设置切割方向 0 前切 1 后切
            if (cutDirection != 0 && cutDirection != 1)
            {
                MaterialSnackUtils.MaterialSnack("切割方向错误！", MaterialSnackUtils.SnackType.WARNING);
                return false;
            }

            // Y轴切割位置调整
            yCurrentPosition += (cutDirection == 0 ? 1 : -1) * (currentCutLine == 0 ? 0 : _model.CoCutSize);
            if (currentCutLine == 0)
            {
                yCurrentPosition += (cutDirection == 0 ? 1 : -1) * GlobalParams.cameraOffsetY;
            }
            cutDistance = (float)Math.Round(Math.Abs(xEndLocation - xStartLocation) / 1000, 4);
            float xDiffValue = xEndLocation - xStartLocation;
            float xStopLocation = (xStartLocation + (xDiffValue / 2)) - GlobalParams.cameraToCutXOffset + 15;
            float yStopLocation = yCurrentPosition  + (cutDirection == 0 ? -1 : 1) * GlobalParams.cameraOffsetY;

            if (feedSpeed > 150)
            {
                MaterialSnackUtils.MaterialSnack("切割速度超限！", MaterialSnackUtils.SnackType.ERROR);
                return false;
            }

            // 设置暂停位置
            PlcControl.tagControl.cutting.SetStopLocation(xStopLocation, yStopLocation, GlobalParams.lastFocusZ2Location);

            // 设置切割参数并调用API执行切割 角度需要根据当前的CH来选择
            CutOperateUtils.SetCutParams(feedSpeed, zEndIndex, zStartLocation, xStartLocation, xEndLocation
                , ref yCurrentPosition, "0", "0", _model.RotateSpeed, cutDirection, _model.CoCutSize, false);

            return true;
        }

        static bool errorFlag = false;
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
                    if (AlarmConfig.Instance.HasActiveAlarm())
                    {
                        errorFlag = true;
                        runFlag = false;
                    }
                    Thread.Sleep(100);
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
    }
}
