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
using 精密切割系统.database.db.modle;
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
            _viewModel = new BladeHeightViewModel();
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
            try
            {
                _rightPage.btnStartSetup.Visibility = Visibility.Collapsed;
                _rightPage.btnCutStop.Visibility = Visibility.Visible;
                _monitorCts = new CancellationTokenSource();
                _ = AutoCutUtils.MonitoringAlarmAsync(StopMeasureHeight, AlarmConfig.Instance.HasAnyExceptConductivityAlarm, _eventAggregator, _monitorCts.Token);
                _measureHeightCts = new CancellationTokenSource();
                _eventAggregator.GetEvent<AutoRuningMessageEvent>().Subscribe(OnMessageReceived, ThreadOption.UIThread);
                await PlcControl.tagControl.bladeMantance.SetSetupParamsAsync(CurrentUtils.GetBladeHeightModel());
                await PlcControl.tagControl.bladeMantance.SetZAxisMaxDistanceAsync(AutoCutUtils.CaculateZAxisMaxDistance(55.1f));
                CommonResult<float> curHeightZ = await AutoCutUtils.ProcessMeasureHeightAsync(HeightMeasurementMode.Contact, default, _eventAggregator, _measureHeightCts.Token);
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

        public async void LoadDBData()
        {
            List<BladeHeightModel> list = await SqlHelper.TableAsync<BladeHeightModel>().Where(t => t.Id == 1).ToListAsync();
            if (list.Count > 0)
            {
                _viewModel._bladeHeightModel = list[0];           
            }
            else
            {
                await SqlHelper.AddAsync(_viewModel._bladeHeightModel);
            }
            DataContext = _viewModel;
            _viewModel.SetupDefault = "CONTACT";
            _viewModel.CallOperatorWhenAutoSetup = "NO";
            _viewModel.PrecutAfterNonContactSetup = "NO";
            

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

        private async void BtnSure_RightClicked(object? sender, bool e)
        {
            if (string.IsNullOrEmpty(_viewModel.ChuckTableSize))
            {
                MaterialSnackUtils.MaterialSnack("请选择工作盘尺寸", MaterialSnackUtils.SnackType.ERROR);
                return;
            }
            if (string.IsNullOrEmpty(_viewModel.ChuckTableShape))
            {
                MaterialSnackUtils.MaterialSnack("请选择工作盘形状", MaterialSnackUtils.SnackType.ERROR);
                return;
            }
            if (string.IsNullOrEmpty(_viewModel.TableType))
            {
                MaterialSnackUtils.MaterialSnack("请选择工作盘种类", MaterialSnackUtils.SnackType.ERROR);
                return;
            }
            //执行数据库数据保存。
            var success = this.FormSuccess();
            if (success)
            {
                await SaveData();
                MaterialSnackUtils.MaterialSnack("操作成功", MaterialSnackUtils.SnackType.SUCCESS);
                PlcControl.tagControl.bladeMantance.SetSetupParams(_viewModel._bladeHeightModel);
            }
            else
            {
                MaterialSnackUtils.MaterialSnack("数据异常", MaterialSnackUtils.SnackType.ERROR);
            }
        }

        public async Task SaveData()
        {
            _bladeHeightModel = _viewModel._bladeHeightModel;
            if (_viewModel.IsBladeUnitInch)
            {
                _bladeHeightModel.Unit = "inch";
            }
            else if (_viewModel.IsBladeUnitMm)
            {
                _bladeHeightModel.Unit = "mm";
            }
            if (_bladeHeightModel != null)
            {              
                await SqlHelper.UpdateAsync(_bladeHeightModel);
            }
        }

        /// <summary>
        /// 表单内容是否错误  false是正常 true是出错了
        /// </summary>
        /// <returns>false表示没有错误，true表示出错了</returns>
        public bool FormError()
        {
            bool result = false;
            List<InputTextBox> tbs = Tools.GetChildrenOfType<InputTextBox>(this);
            for (int i = 0; i < tbs.Count; i++)
            {
                InputTextBox tb = tbs[i];
                tb.ValidationCheck();
                bool isError = tb.XIsError;
                if (isError)
                {
                    result = true;
                }
            }
            return result;
        }

        /// <summary>
        /// 表单内容验证通过  false是不通过 true是通过
        /// </summary>
        /// <returns>false是不通过 true是通过</returns>
        public bool FormSuccess()
        {
            return !FormError();
        }
    }
}
