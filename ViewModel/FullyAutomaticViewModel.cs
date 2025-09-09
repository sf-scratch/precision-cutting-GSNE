using DryIoc;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using NPOI.OpenXmlFormats.Wordprocessing;
using NPOI.SS.Formula.Functions;
using OpenCvSharp;
using Prism.Events;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.DTOs;
using 精密切割系统.Entities;
using 精密切割系统.Extensions;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.HttpClients;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.sqlite;
using 精密切割系统.PubSubEvent;
using 精密切割系统.Utils;
using 精密切割系统.View.common;
using 精密切割系统.View.Dialogs;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.View.Pages.common;
using 精密切割系统.View.Pages.F4_BladeMaintenance;
using 精密切割系统.ViewModel.Dialogs;

namespace 精密切割系统.ViewModel
{
    public class FullyAutomaticViewModel : CustomBindableBase
    {
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private CancellationTokenSource _cts;

        public AsyncDelegateCommand AutoRunCommand { get; set; }
        public AsyncDelegateCommand InitCommand { get; set; }
        public AsyncDelegateCommand<string> CheckLunguCommand { get; set; }

        private DelegateCommand _setCutYCommand;
        public DelegateCommand SetCutYCommand =>
            _setCutYCommand ?? (_setCutYCommand = new DelegateCommand(ExecuteSetCutYCommand));

        void ExecuteSetCutYCommand()
        {
            NavigationParameters paramet = new()
            {
                { AutoCut.RedirectTarget, nameof(AutoCutSetCutPosition)},
                { nameof(AutoCutSetCutPositionType), AutoCutSetCutPositionType.SetCut}
            };
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(AutoCut), paramet);
        }

        private DelegateCommand _setSharpenYCommand;
        public DelegateCommand SetSharpenYCommand =>
            _setSharpenYCommand ?? (_setSharpenYCommand = new DelegateCommand(ExecuteSetSharpenYCommand));

        void ExecuteSetSharpenYCommand()
        {
            NavigationParameters paramet = new()
            {
                { AutoCut.RedirectTarget, nameof(AutoCutSetCutPosition)},
                { nameof(AutoCutSetCutPositionType), AutoCutSetCutPositionType.SetSharpen}
            };
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(AutoCut), paramet);
        }

        private string _lunguId;
        public string LunguId
        {
            get { return _lunguId; }
            set { _lunguId = value; RaisePropertyChanged(); }
        }

        private LunguSksjModel _lunguSksj;

        public LunguSksjModel LunguSksj
        {
            get { return _lunguSksj; }
            set { _lunguSksj = value; RaisePropertyChanged(); }
        }

        private SharpenParamsModel _sharpenParams;
        /// <summary>
        /// 磨刀参数
        /// </summary>
        public SharpenParamsModel SharpenParams
        {
            get { return _sharpenParams; }
            set { _sharpenParams = value; RaisePropertyChanged(); }
        }

        private CutParamsModel _cutParams;
        /// <summary>
        /// 切割参数
        /// </summary>
        public CutParamsModel CutParams
        {
            get { return _cutParams; }
            set { _cutParams = value; RaisePropertyChanged(); }
        }

        private float _sharpenY;
        public float SharpenY
        {
            get { return _sharpenY; }
            set { _sharpenY = value; RaisePropertyChanged(); }
        }

        private float _cutY;
        public float CutY
        {
            get { return _cutY; }
            set { _cutY = value; RaisePropertyChanged(); }
        }

        private bool _isProductEnvironment;

        public bool IsProductEnvironment
        {
            get { return _isProductEnvironment; }
            set 
            { 
                _isProductEnvironment = value; 
                if (_isProductEnvironment)
                {
                    HttpRestClient.UpdateProd();
                }
                else
                {
                    LunguId = "T25051502B0014";
                    HttpRestClient.UpdateDev();
                }
                RaisePropertyChanged();
            }
        }

        public string DeviceCode
        {
            get { return Appsettings.DeviceCode ?? string.Empty; }
            set
            {
                Appsettings.DeviceCode = value;
                RaisePropertyChanged();
            }
        }

        public FullyAutomaticViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            LunguId = string.Empty;
            AutoRunCommand = new AsyncDelegateCommand(AutoRunAsync);
            CheckLunguCommand = new AsyncDelegateCommand<string>(CheckLunguAsync);
            InitCommand = new AsyncDelegateCommand(InitAsync);
        }

        public FullyAutomaticViewModel()
        {
        }

        private async Task InitAsync()
        {
            long selectedConfigId = await SelectedConfigEntity.GetCurrentSelectedConfigIdAsync(SqlHelper.SQLiteAsync);
            SharpenParamsEntity? sharpenParamsEnt = await SqlHelper.TableAsync<SharpenParamsEntity>().Where(p => p.Id == selectedConfigId).FirstOrDefaultAsync();
            if (sharpenParamsEnt != null)
            {
                SharpenParams = MapperConfig.Mapper.Map<SharpenParamsModel>(sharpenParamsEnt);
                SharpenParams.IsExecuteLastSharpen = true;
            }
            CutParamsEntity? cutParamsEnt = await SqlHelper.TableAsync<CutParamsEntity>().Where(p => p.Id == selectedConfigId).FirstOrDefaultAsync();
            if (cutParamsEnt != null)
            {
                CutParams = MapperConfig.Mapper.Map<CutParamsModel>(cutParamsEnt);
            }
            CommonResult<List<float>> cutListResult = await AutoCutUtils.GetCutListAsync(CutParams);
            if (cutListResult.IsSuccess)
            {
                CutParams.CutNum = cutListResult.Data is null ? 0 : cutListResult.Data.Count;
            }
            else
            {
                MaterialSnackUtils.MaterialSnack($"切割序列获取失败，请检查切割参数配置！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
            }
        }

        private void InitRightButtonOnlyBack()
        {
            RightButtonCollection.Clear();
            RightButtonCollection.Add(RightButtonParams.YelloRightButton("检查轮毂", "FormatListChecks", () => _semaphore.ExecuteAsync(() => CheckLunguAsync(LunguId), "检查轮毂")));
            RightButtonCollection.Add(RightButtonParams.YelloRightButton("操作记录", "ClipboardTextClockOutline", GoToHistory));
            RightButtonCollection.Add(RightButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private void InitRightButton()
        {
            RightButtonCollection.Clear();
            RightButtonCollection.Add(RightButtonParams.GreenRightButton("自动执行", "LocationEnter", () => _semaphore.ExecuteAsync(AutoRunAsync, "自动执行")));
            RightButtonCollection.Add(RightButtonParams.YelloRightButton("检查轮毂", "FormatListChecks", () => _semaphore.ExecuteAsync(() => CheckLunguAsync(LunguId), "检查轮毂")));
            RightButtonCollection.Add(RightButtonParams.YelloRightButton("操作记录", "ClipboardTextClockOutline", GoToHistory));
            RightButtonCollection.Add(RightButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private void InitBottomButton()
        {
            BottomButtonCollection.Add(RightButtonParams.BlueButton("换刀片", "SawBlade", () => _semaphore.ExecuteAsync(ReplaceBlade, "换刀片")));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("换磨刀板", "Square", () => _semaphore.ExecuteAsync(ReplaceSharpeningBoard, "换磨刀板")));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("换硅片", "CircleOpacity", () => _semaphore.ExecuteAsync(ReplaceWafer, "换硅片")));
        }

        private void GoToHistory()
        {
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(AutoCutHistory));
        }

        private async Task CheckLunguAsync(string lunguId)
        {
            if (!GlobalParams.onlineFlag)
            {
                InitRightButton();
                MaterialSnackUtils.MaterialSnack("检查轮毂信息完成，可开始执行自动切割！", MaterialSnackUtils.SnackType.SUCCESS, 0, _eventAggregator);
                return;
            }
            InitRightButtonOnlyBack();
            MaterialSnackUtils.MaterialSnack("检查轮毂信息中...", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
            HttpUtilsResult<LunguInfoDTO> lunguResult = await HttpUtils.GetLunguInfoAsync(LunguId);
            if (lunguResult.Data is null)
            {
                MaterialSnackUtils.MaterialSnack(lunguResult.Msg, MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                return;
            }
            if (lunguResult.Data.CurrentGroup != "切割车间")
            {
                MaterialSnackUtils.MaterialSnack($"当前轮毂在{lunguResult.Data.CurrentGroup}，请检查！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                return;
            }
            HttpUtilsResult<LunguSksjDTO> lunguSksjResult = await HttpUtils.GetLunguSksjAsync(LunguId);
            if (lunguSksjResult.Data is null)
            {
                MaterialSnackUtils.MaterialSnack(lunguSksjResult.Msg, MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                return;
            }
            LunguSksj = MapperConfig.Mapper.Map<LunguSksjModel>(lunguSksjResult.Data);
            InitRightButton();
            MaterialSnackUtils.MaterialSnack("检查轮毂信息完成，可开始执行自动切割！", MaterialSnackUtils.SnackType.SUCCESS, 0, _eventAggregator);

        }

        private async Task AutoRunAsync()
        {
            if (!GlobalParams.onlineFlag)
            {
                NavigationParameters paramet = new NavigationParameters
                {
                    { "SharpenParams", SharpenParams },
                    { "CutParams", CutParams },
                    { "LunguSksj", LunguSksj },
                    { AutoCut.RedirectTarget, nameof(AutoCutRuning)}
                };
                _regionManager.RequestNavigate(RegionName.MainRegion, nameof(AutoCut), paramet);
                return;
            }
            if (!await PlcControl.tagControl.wholeDevice.IsCompletedSystemInitAsync())
            {
                MaterialSnackUtils.MaterialSnack("请完成系统初始化！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                return;
            }
            if (!await PlcControl.tagControl.wholeDevice.IsOpenVacuumSwitchAsync())
            {
                MaterialSnackUtils.MaterialSnack("请打开工作盘真空！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                return;
            }
            if (await PlcControl.tagControl.wholeDevice.IsOpenCutSecurityDoorAsync())
            {
                MaterialSnackUtils.MaterialSnack("请关闭切割安全门！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                return;
            }
            if (await PlcControl.tagControl.wholeDevice.IsOpenCameraSecurityDoorAsync())
            {
                MaterialSnackUtils.MaterialSnack("请关闭相机安全门！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                return;
            }
            if (Appsettings.SharpenY is not null && Appsettings.SharpenY.Value != SharpenY)
            {
                Appsettings.SharpenDistance += Appsettings.SharpenY - SharpenY;
                Appsettings.SharpenY = SharpenY;
            }
            if (Appsettings.CutY is not null && Appsettings.CutY.Value != CutY)
            {
                Appsettings.CutDistance += Appsettings.CutY - CutY;
                Appsettings.CutY = CutY;
            }
            NavigationParameters parameters = new NavigationParameters
            {
                { "SharpenParams", SharpenParams },
                { "CutParams", CutParams },
                { "LunguSksj", LunguSksj },
                { AutoCut.RedirectTarget, nameof(AutoCutRuning)}
            };
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(AutoCut), parameters);
        }

        private void Back()
        {
            NavigateUtils.NavigateToPage("MainMenu");
        }

        private async Task ReplaceWafer()
        {
            await using var timeoutToken = TaskUtils.GetTimeoutCancellationToken(TimeSpan.FromSeconds(60), _cts.Token);
            var res = await DialogHost.Show(SelectionDialog.NewInstance("移动并重置数据", "仅移动位置", "取消"));
            if (res is string dialogResult)
            {
                if (dialogResult == SelectionDialog.YES)
                {
                    await AutoCutUtils.ReplaceWaferAndResetAsync(default, timeoutToken.Token);
                }
                else
                {
                    await AutoCutUtils.ReplaceWaferAsync(default, timeoutToken.Token);
                }
            }
            CutY = Appsettings.CutY ?? 0;
        }

        private async Task ReplaceSharpeningBoard()
        {
            await using var timeoutToken = TaskUtils.GetTimeoutCancellationToken(TimeSpan.FromSeconds(60), _cts.Token);
            var res = await DialogHost.Show(SelectionDialog.NewInstance("移动并重置数据", "仅移动位置", "取消"));
            if (res is string dialogResult)
            {
                if (dialogResult == SelectionDialog.YES)
                {
                    await AutoCutUtils.ReplaceSharpeningBoardAndResetAsync(default, timeoutToken.Token);
                }
                else
                {
                    await AutoCutUtils.ReplaceSharpeningBoardAsync(default, timeoutToken.Token);
                }
            }
            SharpenY = Appsettings.SharpenY ?? 0;
        }

        private async Task ReplaceBlade()
        {
            await using var timeoutToken = TaskUtils.GetTimeoutCancellationToken(TimeSpan.FromSeconds(60), _cts.Token);
            await AutoCutUtils.ReplaceBladeAsync(default, timeoutToken.Token);
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            _cts = new CancellationTokenSource();
            LunguSksj = new LunguSksjModel();
            IsProductEnvironment = true;
            SharpenY = Appsettings.SharpenY ?? 0;
            CutY = Appsettings.CutY ?? 0;
            InitRightButtonOnlyBack();
            InitBottomButton();
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                _eventAggregator.GetEvent<SetFocusEvent>().Publish("lunguTextBox");
            }), DispatcherPriority.Loaded);
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
