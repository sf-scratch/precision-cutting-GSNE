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
using 精密切割系统.Entities;
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
        private CancellationTokenSource _measureHeightCts;
        private CancellationTokenSource _monitorCts;

        private BMSetupDataConfViewModel ViewModel { get; set; } = new BMSetupDataConfViewModel();

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
            DataContext = ViewModel;
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
            if (this.HasFormError())
            {
                MaterialSnack("表单填写有误，请检查!", SnackType.ERROR);
                return;
            }
            if (AlarmConfig.Instance.HasActiveErrorAlarm())
            {
                MaterialSnack(AlarmConfig.HasErrorAlarmMessage, SnackType.WARNING);
                return;
            }
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
                _ = AutoCutUtils.MonitoringAlarmAsync(StopMeasureHeight, AlarmConfig.Instance.HasAutoRunUnexpectedAlarms, _eventAggregator, _monitorCts.Token);
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
                BMParameterMaintenanceEntity bMParameter = await SqlHelper.GetOrCreateEntityAsync(() => new BMParameterMaintenanceEntity());
                bMParameter.MeasureHeightHistory = string.Join(",", ViewModel.BladeMeasureList);
                await SqlHelper.UpdateAsync(bMParameter);
                _eventAggregator.GetEvent<AutoRuningMessageEvent>().Unsubscribe(OnMessageReceived);
            }
        }

        private void OnMessageReceived(MessageModel model)
        {
            var (success, Index, Value) = AutoCutUtils.ExtractNumbersPrecise(model.Message);
            if (success)
            {
                if (ViewModel is not null && Index - 1 < ViewModel.BladeMeasureList.Count && Index - 1 >= 0)
                {
                    ViewModel.CurrentMeasureValue = (float)Value;
                    ViewModel.BladeMeasureList[Index - 1].FieldValue = (float)Value;
                }
            }
            RealTimeInfo.Messages.Add(model);
        }

        public async void LoadDBData()
        {
            if (!GlobalParams.HasTheta)
            {
                InitialPositionModel? initPos = await AutoCutUtils.GetInitialPositionAsync();
                if (initPos is not null)
                {
                    var bmParams = await SqlHelper.GetOrCreateEntityAsync(() => new BMParameterMaintenanceEntity());
                    bmParams.ThetaStartingToMovePosition = initPos.BladeSetupInitX;
                    await SqlHelper.UpdateAsync(bmParams);
                }
            }
            ViewModel.BMParameter = await SqlHelper.GetOrCreateEntityAsync(() => new BMParameterMaintenanceEntity());
            ViewModel.BladeOuterDiameter = Appsettings.BladeOuterDiameter?.ToString("F3") ?? string.Empty;
            var initialPosition = await SqlHelper.GetOrCreateEntityAsync(() => new InitialPositionModel());
            ViewModel.BladeSetupInitZ1 = initialPosition.BladeSetupInitZ1;
            var caculateResult = await AutoCutUtils.CaculateActulMeasureHeightSlowSpeedRangedAsync(ViewModel.BMParameter.MeasureHeightSlowSpeedRange.ToFloat());
            if (caculateResult.IsSuccess)
            {
                float measureHeightHighSpeed = ViewModel.BMParameter.MeasureHeightHighSpeed.ToFloat();
                float measureHeightSlowSpeed = ViewModel.BMParameter.MeasureHeightSlowSpeed.ToFloat();
                float measureHeightSlowSpeedRange = caculateResult.Data;
                await PlcControl.tagControl.bladeMantance.SetMeasureHeightParams(measureHeightHighSpeed, measureHeightSlowSpeed, measureHeightSlowSpeedRange);
            }
            else
            {
                MaterialSnack(caculateResult.Message, SnackType.ERROR);
                return;
            }
        }

        private async void BtnSure_RightClicked(object? sender, bool e)
        {
            if (this.HasFormError())
            {
                MaterialSnack("表单填写有误，请检查!", SnackType.ERROR);
                return;
            }
            try
            {
                await SqlHelper.UpdateAsync(ViewModel.BMParameter);
                Appsettings.BladeOuterDiameter = ViewModel.BladeOuterDiameter.ToFloat();
                var initialPosition = await SqlHelper.GetOrCreateEntityAsync(() => new InitialPositionModel());
                initialPosition.BladeSetupInitZ1 = ViewModel.BladeSetupInitZ1;
                await SqlHelper.UpdateAsync(initialPosition);
                NavigateUtils.ToOperateButton();
                MaterialSnack("测高参数已确认!", SnackType.SUCCESS);
            }
            catch (Exception ex)
            {
                MaterialSnack("保存测高参数失败:" + ex.Message, SnackType.ERROR);
                return;
            }
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

        private async void heightMeasureTimes_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.HasFormError())
            {
                return;
            }
            if (ViewModel is not null)
            {
                ViewModel.BladeMeasureList.Clear();
                BMParameterMaintenanceEntity bMParameter = await SqlHelper.GetOrCreateEntityAsync(() => new BMParameterMaintenanceEntity());
                if (bMParameter.MeasureHeightHistory != null)
                {
                    string[] historys = bMParameter.MeasureHeightHistory.Split(",");
                    for (int i = 1; i <= ViewModel.BMParameter.HeightMeasureTimes.ToInt(); i++)
                    {
                        int index = i - 1;
                        ViewModel.BladeMeasureList.Add(new BladeMeasureData()
                        {
                            FieldName = i.ToString(),
                            FieldValue = index < historys.Length ? historys[index].ToFloat() : 0
                        });
                    }
                }
                ViewModel.CurrentMeasureValue = ViewModel.BladeMeasureList.LastOrDefault()?.FieldValue ?? 0;
            }
        }
    }
}