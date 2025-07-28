using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.common;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.F2_ManualOperation
{
    /// <summary>
    /// MQSemiAutomaticCuttingStop.xaml 的交互逻辑
    /// </summary>
    public partial class MQSemiAutomaticCuttingStop : Page
    {
        private readonly SemiAutoCutService _semiAutoCutService;
        private static CameraCommon? _cameraCommon;
        private MQSemiAutomaticCuttingStopViewModel _viewModel;
        private MainWindow _mainWindow;
        private RightPage _rightPage;
        // 手动调整基准线标识
        bool _adjustDatumLineFlag = false;
        // 创建一个定时器
        System.Timers.Timer? _timer = null;

        public MQSemiAutomaticCuttingStop()
        {
            InitializeComponent();
            _mainWindow = Application.Current.MainWindow as MainWindow ?? new MainWindow();
            _semiAutoCutService = SemiAutoCutService.Instance;
            _viewModel = new MQSemiAutomaticCuttingStopViewModel();
            DataContext = _viewModel;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _rightPage = _mainWindow.rightFrame.Content as RightPage ?? new RightPage();
            _rightPage.PanelAction.Visibility = Visibility.Visible;
            _rightPage.btnCutReStart.Visibility = Visibility.Visible;
            _rightPage.btnCutReStart.SetRightClickedHandler(ReCutStart);
            _rightPage.btnCutStop.Visibility = Visibility.Visible;
            _rightPage.btnCutStop.SetRightClickedHandler(CutStop);
            GlobalParams.cutStatusInfo = 2;
            // 加载参数
            string[] query = Uri.UnescapeDataString(NavigationService.CurrentSource.OriginalString).Split("?");
            if (query.Length == 2 )
            {
                var runViewModel = JsonConvert.DeserializeObject<MQSemiAutomaticCuttingRunViewModel>(query[1]);
                if (runViewModel is not null )
                {
                    _viewModel.DeviceDataNo = runViewModel.DeviceDataNo;
                    _viewModel.DeviceDataId = runViewModel.DeviceDataId;
                    _viewModel.RunCutLine = runViewModel.RunCutLine;
                    _viewModel.AllRunCutLine = runViewModel.AllRunCutLine;
                    _viewModel.ChannelNum = runViewModel.ChannelNum;
                    _viewModel.BladeHeight = runViewModel.BladeHeight.ToString();
                    _viewModel.FeedSpeed = runViewModel.FeedSpeed.ToString();
                    _viewModel.DepthCompensation = runViewModel.DepthCompensation;
                    _viewModel.ChangeFeedSpeed = runViewModel.ChangeFeedSpeed;
                    _viewModel.ExpectedProcessingEndTime = runViewModel.ExpectedProcessingEndTime;
                    _viewModel.AllCutLine = runViewModel.AllCutLine;
                    _viewModel.AllCutLineLength = runViewModel.AllCutLineLength.ToString();
                }
            }
            _cameraCommon = AutoCutUtils.GetCameraCommon();
            if (_cameraCommon is null)
            {
                MaterialSnackUtils.MaterialSnack("相机获取失败！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            _viewModel.CutWidth = Tools.GetDoubleStringValue(Tools.FormatDecimalString((_cameraCommon._cutMarkWidth / 1000).ToString(), 4));
            _viewModel.DdgesWidth = Tools.GetDoubleStringValue(Tools.FormatDecimalString((_cameraCommon._edgeChipWidth / 1000).ToString(), 4));
            UpdateDefineDataModel();
        }

        //根据默认配置控制对应显示和隐藏
        private void UpdateDefineDataModel()
        {
            UserDefineDataModel userDefineModel = CurrentUtils.getUserDefineDataModel();
            bool isSpeedChange = "NO".Equals(userDefineModel.SpeedChange);
            bool isHeightChange = "NO".Equals(userDefineModel.HeightChange);
            if (isSpeedChange)//速度变更
            {
                ChangeFeedSpeed1.Visibility = Visibility.Hidden;
                ChangeFeedSpeed2.Visibility = Visibility.Hidden;
                ChangeFeedSpeed3.Visibility = Visibility.Hidden;
            }
            if (isHeightChange)//高度补偿
            {
                HeightChange1.Visibility = Visibility.Hidden;
                HeightChange2.Visibility = Visibility.Hidden;
                HeightChange3.Visibility = Visibility.Hidden;
            }
            _mainWindow.UpdateOperatePage(OperateData.GetSemiAutoCuttingStopOperate(!isSpeedChange, !isHeightChange)
                , OperateClickHandler, OperateTouchLeaveHandler, OperateTouchDownHandler);
        }

        private void UpdateMenu2()
        {
            _mainWindow.UpdateOperatePage(OperateData.GetSemiAutoCuttingStopTwoOperate(), OperateClickHandler);
        }

        private void DisposeDatumLine(int code)
        {
            if (code == 23040)
            {
                _cameraCommon?.SetEdgeWidth(-1, 2);
            }
            else if (code == 23041)
            {
                _cameraCommon?.SetEdgeWidth(1, 2);
            }
            else if (code == 23407)
            {
                _cameraCommon?.SetCutMarkWidth(-1, 2);

            }
            else if (code == 23408)
            {
                _cameraCommon?.SetCutMarkWidth(1, 2);
            }

            _viewModel.CutWidth = Tools.GetDoubleStringValue(Tools.FormatDecimalString((_cameraCommon._cutMarkWidth / 1000).ToString(), 4));
            _viewModel.DdgesWidth = Tools.GetDoubleStringValue(Tools.FormatDecimalString((_cameraCommon._edgeChipWidth / 1000).ToString(), 4));
        }
        public void OperateTouchDownHandler(object sender, int code)
        {
            switch (code)
            {
                case 23407:
                // 基准线调窄
                case 23408:
                // 基准线调宽
                case 23040:
                // 崩边调窄
                case 23041:
                    DisposeDatumLine(code); // 初始调用 DisposeDatumLine
                    _adjustDatumLineFlag = true;
                    if (_timer != null)
                    {
                        _timer.Stop();
                    }
                    // 创建定时器
                    _timer = new System.Timers.Timer
                    {
                        Interval = 500, // 初始延迟 500 毫秒
                        AutoReset = false // 每次触发后需要手动重新启动
                    };
                    _timer.Elapsed += (sender, e) =>
                    {
                        if (_timer != null)
                        {
                            if (!_adjustDatumLineFlag)
                            {
                                _timer.Stop();
                                _timer.Dispose(); // 释放资源
                                return;
                            }

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                DisposeDatumLine(code);
                            });

                            // 重新设置间隔为 100 毫秒并重新启动定时器
                            _timer.Interval = 100;
                            _timer.Start();
                        }
                    };
                    _timer.Start(); // 启动定时器
                    break;
                default:
                    break;
            }
        }
        public void OperateTouchLeaveHandler(object sender, int code)
        {
            switch (code)
            {
                case 23407:
                // 基准线调窄
                case 23408:
                // 基准线调宽
                case 23040:
                // 崩边调窄
                case 23041:
                    _adjustDatumLineFlag = false;
                    if (_timer != null)
                    {
                        _timer.Stop();
                        _timer.Dispose();
                        _timer = null;
                    }
                    break;
                default:
                    break;
            }
        }
        public void OperateClickHandler(object sender, int code)
        {
            switch (code) {
                case 2409:
                    // 确认基准线
                    string yCurrentPosition = PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey);
                    float offset = CutOperateUtils.yStopLocation - float.Parse(yCurrentPosition);
                    MaterialSnackUtils.MaterialSnack("基准线已确认！", MaterialSnackUtils.SnackType.SUCCESS);
                    Tools.LogInfo($"最新基准线：{GlobalParams.cameraOffsetY}");
                    break;
                case 2401:
                    float tempDepthCompensation = Tools.GetFloatStringValue(_viewModel.DepthCompensation);
                    // 高度补偿
                    _semiAutoCutService.DepthCompensationValue = tempDepthCompensation;
                    MaterialSnackUtils.MaterialSnack("刀片高度补偿设置成功！", MaterialSnackUtils.SnackType.SUCCESS);
                    break;
                case 2403:
                    float tempChangeFeedSpeed = Tools.GetFloatStringValue(_viewModel.ChangeFeedSpeed);
                    // 速度更改
                    _semiAutoCutService.FeedSpeedCompCompensationValue = tempChangeFeedSpeed;
                    MaterialSnackUtils.MaterialSnack("变更进刀速度成功！", MaterialSnackUtils.SnackType.SUCCESS);
                    break;
                case 2442:
                    // 聚焦
                    if (!CommonCheck.FocusStatsCheck())
                    {
                        break;
                    }
                    CommonOperate.GetInstance().AutoFocus(2, _mainWindow, null);
                    break;
                case 2412:
                    UpdateMenu2();
                    // 调光
                    ShowDimming(1);
                    break;
                case 2411:
                    UpdateDefineDataModel();
                    // 调光
                    ShowDimming(0);
                    break;
                case 2422:
                    // 刀片状态信息
                    _mainWindow.NavigateToPage("Pages/F4_BladeMaintenance/BladeInfo", "pageName=Pages/F2_ManualOperation/MQSemiAutomaticCuttingStop");
                    break;
                case 2405:
                    _mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCDeviceDataListConf");
                    break;
                default:
                    break;
            }
       }
        /// <summary>
        /// 显示调光
        /// </summary>
        /// <param name="status">0 不显示 1 显示</param>
        public void ShowDimming(int status)
        {
            if (status == 0)
            {
                //dimmingGrid.Visibility = Visibility.Collapsed;
                //cutLineWidthGrid.Visibility = Visibility.Visible;
                //linesRecordGrid.Visibility = Visibility.Visible;
                //compGrid.Visibility = Visibility.Visible;
            } else
            {
                //dimmingGrid.Visibility = Visibility.Visible;
                //cutLineWidthGrid.Visibility = Visibility.Collapsed;
                //linesRecordGrid.Visibility = Visibility.Collapsed;
                //compGrid.Visibility = Visibility.Collapsed;
            }
        }

        // 继续切割
        private async void ReCutStart(object? sender, bool e)
        {
            await MQSemiAutomaticCuttingRun.ContinueAsync();
            _mainWindow.mainFrame.GoBack();
        }

        private async void CutStop(object? sender, bool e)
        {
            await MQSemiAutomaticCuttingRun.StopAsync(ServicePauseResult.Stop);
        }

        private void cutRecognition_Click(object sender, RoutedEventArgs e)
        {
            MaterialSnackUtils.MaterialSnack("识别中...", MaterialSnackUtils.SnackType.WARNING, 0);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string fileName = $"check_{timestamp}.png";
            _cameraCommon.SaveWriteableBitmap(fileName);
            Thread.Sleep(1000);
            double[] widthInfo = CommonOperate.GetCutEdgeWidth(fileName);
            if (widthInfo == null || widthInfo[0] == 0 || widthInfo[1] == 0)
            {
                return;
            }
            double cutWidthValue = CameraOperateUtils.ConvertToPictureBoxSize(widthInfo[0]);
            double edgesWidthValue = CameraOperateUtils.ConvertToPictureBoxSize(widthInfo[1]);
            _cameraCommon.DrawLineForWidth((float)cutWidthValue, (float)edgesWidthValue);
            _viewModel.CutWidth = Tools.GetDoubleStringValue(Tools.FormatDecimalString((_cameraCommon._cutMarkWidth / 1000).ToString(), 4));
            _viewModel.DdgesWidth = Tools.GetDoubleStringValue(Tools.FormatDecimalString((_cameraCommon._edgeChipWidth / 1000).ToString(), 4));
            MaterialSnackUtils.MaterialSnack("识别完成！", MaterialSnackUtils.SnackType.SUCCESS);
        }

        private void stopCheckCheckbox_Click(object sender, RoutedEventArgs e)
        {
        }

        private void repeatedCheckbox_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
