using Emgu.CV.Dnn;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.Data;
using 精密切割系统.database.db.modle;
using 精密切割系统.Extensions;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.MeasureHeight;
using 精密切割系统.Model.plc;
using 精密切割系统.PubSubEvent;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;
using static 精密切割系统.Helpers.MaterialSnackUtils;

namespace 精密切割系统.View.Pages.F4_BladeMaintenance
{
    /// <summary>
    /// BMSetupDataConf.xaml 的交互逻辑
    /// </summary>
    public partial class BMSetupDataConf : Page
    {
        private readonly IEventAggregator _eventAggregator;
        private MainWindow _mainWindow;
        private RightPage _rightPage;
        private OperatePage _operatePage;
        private BladeHeightViewModel _viewModel;
        private BladeHeightModel _bladeHeightModel;
        private CancellationTokenSource _measureHeightCts;
        private CancellationTokenSource _monitorCts;

        public BMSetupDataConf()
        {
            InitializeComponent();
            _eventAggregator = ContainerLocator.Current.Resolve<IEventAggregator>();
            RealTimeInfo.Messages = [];
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _mainWindow = Application.Current.MainWindow as MainWindow ?? new MainWindow();
            _rightPage = _mainWindow.rightFrame.Content as RightPage ?? new RightPage(); ;
            _operatePage = _mainWindow.operateFrame.Content as OperatePage ?? new OperatePage();
            //底部操作按钮
            _operatePage.UpdateOperate([]);
            _rightPage.PanelAction.Visibility = Visibility.Visible;
            _rightPage.btnBack.Visibility = Visibility.Visible;
            _rightPage.btnBack.BackFlag = false;
            _rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            _rightPage.btnSure.Visibility = Visibility.Visible;
            _rightPage.btnSure.BackFlag = false;
            _rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked);
            _rightPage.btnStartSetup.Visibility = Visibility.Visible;
            _rightPage.btnStartSetup.BackFlag = false;
            _rightPage.btnStartSetup.SetRightClickedHandler(BtnStartSetup_RightClicked);
            _rightPage.btnCutStop.SetRightClickedHandler(BtnCutStop_RightClicked);
            LoadDBData();
            RealTimeInfo.Messages.Add(MessageModel.Create("进入测高界面..."));
        }

        private void BtnCutStop_RightClicked(object? sender, bool e)
        {
            StopMeasureHeight();
        }

        private void StopMeasureHeight()
        {
            _measureHeightCts.Cancel();
            _monitorCts.Cancel();
        }

        private async void BtnStartSetup_RightClicked(object? sender, bool e)
        {
            if (Appsettings.BladeOuterDiameter is null)
            {
                MaterialSnack("未设置刀片外径！", SnackType.WARNING);
                return;
            }
            try
            {
                _rightPage.btnStartSetup.Visibility = Visibility.Collapsed;
                _rightPage.btnCutStop.Visibility = Visibility.Visible;
                _monitorCts = new CancellationTokenSource();
                _ = AutoCutUtils.MonitoringAlarmAsync(StopMeasureHeight, AlarmConfig.Instance.HasAnyExceptConductivityAlarm, _eventAggregator, _monitorCts.Token);
                _measureHeightCts = new CancellationTokenSource();
                _eventAggregator.GetEvent<AutoRuningMessageEvent>().Subscribe(OnMessageReceived, ThreadOption.UIThread);
                CommonResult<float> curHeightZ = await AutoCutUtils.ProcessCombineMeasureHeightAsync(_eventAggregator, _measureHeightCts.Token);
                if (!curHeightZ.IsSuccess)
                {
                    MaterialSnack(curHeightZ.Message, SnackType.WARNING, 0);
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                MaterialSnack("测高停止！", SnackType.WARNING, 0);
            }
            finally
            {
                _rightPage.btnStartSetup.Visibility = Visibility.Visible;
                _rightPage.btnCutStop.Visibility = Visibility.Collapsed;
                _eventAggregator.GetEvent<AutoRuningMessageEvent>().Unsubscribe(OnMessageReceived);
            }
        }

        private void OnMessageReceived(MessageModel model)
        {
            RealTimeInfo.Messages.Add(model);
        }

        public void LoadDBData()
        {
            spindleRev.Text = BmSetupData.Instance.SpindleRev.ToString();
            heightMeasureTimes.Text = BmSetupData.Instance.HeightMeasureTimes.ToString();
            isAutomHeightMeasureBeforeCutting.IsChecked = BmSetupData.Instance.IsAutomHeightMeasureBeforeCutting;
        }

        private void BtnSure_RightClicked(object? sender, bool e)
        {
            if (this.HasFormError())
            {
                MaterialSnack("表单填写有误，请检查!", SnackType.ERROR);
                return;
            }
            BmSetupData.Instance.SpindleRev = spindleRev.Text.ToInt();
            BmSetupData.Instance.HeightMeasureTimes = heightMeasureTimes.Text.ToInt();
            BmSetupData.Instance.IsAutomHeightMeasureBeforeCutting = isAutomHeightMeasureBeforeCutting.IsChecked ?? false;
            MaterialSnack("测高参数已确认!", SnackType.SUCCESS);
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            string rePapg = QueryUtils.GetValueFromQueryParams(this, "RePage");
            string rePageId = QueryUtils.GetValueFromQueryParams(this, "RePageId");
            if (!string.IsNullOrEmpty(rePapg))
            {
                _mainWindow.NavigateToPage(rePapg, $"id={rePageId}");
                return;
            }
            _mainWindow.NavigateToPage("MainMenu");
        }
    }
}