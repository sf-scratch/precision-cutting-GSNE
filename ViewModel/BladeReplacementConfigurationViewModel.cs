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
    public class BladeReplacementConfigurationViewModel : CustomBindableBase
    {
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        public RelayCommand AutoRunCommand { get; set; }
        public RelayCommand InitCommand { get; set; }
        public RelayCommand<string> CheckLunguCommand { get; set; }
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        // 控制右侧按钮
        private ObservableCollection<RightButtonParams> _rightButtonParams;
        // 控制底部侧按钮
        public ObservableCollection<RightButtonParams> _operatePageButtonCollection;

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
                    HttpRestClient.UpdateDev();
                }
                RaisePropertyChanged();
            }
        }


        public BladeReplacementConfigurationViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            LunguId = string.Empty;
            _rightButtonParams = WindowLayout.RightPageButtons;
            _operatePageButtonCollection = WindowLayout.OperatePageButtons;
            AutoRunCommand = new RelayCommand(AutoRunAsync);
            CheckLunguCommand = new RelayCommand<string>(CheckLungu);
            InitCommand = new RelayCommand(Init);
        }

        public BladeReplacementConfigurationViewModel()
        {
        }

        private async void Init()
        {
            long selectedConfigId = await SelectedConfigEntity.GetCurrentSelectedConfigIdAsync(SqlHelper.SQLiteAsync);
            SharpenParamsEntity? sharpenParamsEnt = await SqlHelper.TableAsync<SharpenParamsEntity>().Where(p => p.Id == selectedConfigId).FirstOrDefaultAsync();
            if (sharpenParamsEnt != null)
            {
                SharpenParams = MapperConfig.Mapper.Map<SharpenParamsModel>(sharpenParamsEnt);
                SharpenParams.IsExecuteSharpen = true;
            }
            CutParamsEntity? cutParamsEnt = await SqlHelper.TableAsync<CutParamsEntity>().Where(p => p.Id == selectedConfigId).FirstOrDefaultAsync();
            if (cutParamsEnt != null)
            {
                CutParams = MapperConfig.Mapper.Map<CutParamsModel>(cutParamsEnt);
            }
        }

        private void InitRightButtonOnlyBack()
        {
            _rightButtonParams.Clear();
            _rightButtonParams.Add(RightButtonParams.YelloRightButton("检查轮毂", "/Assets/icon/menu_0/menu_0_2_white.png", () => CheckLungu(LunguId)));
            _rightButtonParams.Add(RightButtonParams.YelloRightButton("操作记录", "ClipboardTextClockOutline", GoToHistory));
            _rightButtonParams.Add(RightButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private void InitRightButton()
        {
            _rightButtonParams.Clear();
            _rightButtonParams.Add(RightButtonParams.GreenRightButton("自动执行", "/Assets/icon/right/enter.png", AutoRunAsync));
            _rightButtonParams.Add(RightButtonParams.YelloRightButton("检查轮毂", "/Assets/icon/menu_0/menu_0_2_white.png", () => CheckLungu(LunguId)));
            _rightButtonParams.Add(RightButtonParams.YelloRightButton("操作记录", "ClipboardTextClockOutline", GoToHistory));
            _rightButtonParams.Add(RightButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private void InitBottomButton()
        {
            _operatePageButtonCollection.Add(RightButtonParams.BlueRightButton("换刀片", "SawBlade", ReplaceBlade, null, 8));
            _operatePageButtonCollection.Add(RightButtonParams.BlueRightButton("换磨刀板", "Square", ReplaceSharpeningBoard, null, 8));
            _operatePageButtonCollection.Add(RightButtonParams.BlueRightButton("换硅片", "CircleOpacity", ReplaceWafer, null, 8));
        }

        private void GoToHistory()
        {
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(AutoCutHistory));
        }

        private async void CheckLungu(string lunguId)
        {
            if (!_semaphore.Wait(0)) // 尝试获取锁（0 = 不等待）
            {
                return; // 如果锁已被占用，直接返回
            }
            bool isInitSuccess = false;
            try
            {
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
                isInitSuccess = true;
                MaterialSnackUtils.MaterialSnack("检查轮毂信息完成，可开始执行自动切割！", MaterialSnackUtils.SnackType.SUCCESS, 0, _eventAggregator);
            }
            finally
            {
                if (isInitSuccess)
                {
                    InitRightButton();
                }
                else
                {
                    InitRightButtonOnlyBack();
                }
                _semaphore.Release(); // 释放锁
            }

        }

        private async void AutoRunAsync()
        {
            if (!GlobalParams.onlineFlag)
            {
                _regionManager.RequestNavigate(RegionName.MainRegion, nameof(AutoCut), new NavigationParameters { { "SharpenParams", SharpenParams }, { "CutParams", CutParams }, { "LunguSksj", LunguSksj } });
                return;
            }
            if (!await PlcControl.tagControl.wholeDevice.IsOpenVacuumSwitchAsync())
            {
                MaterialSnackUtils.MaterialSnack("未打开工作盘真空！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                return;
            }
            if (await PlcControl.tagControl.wholeDevice.IsOpenCutSecurityDoorAsync())
            {
                MaterialSnackUtils.MaterialSnack("切割安全门未关闭！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                return;
            }
            if (await PlcControl.tagControl.wholeDevice.IsOpenCameraSecurityDoorAsync())
            {
                MaterialSnackUtils.MaterialSnack("相机安全门未关闭！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
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
                { "LunguSksj", LunguSksj }
            };
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(AutoCut), parameters);
        }

        private void Back()
        {
            NavigateUtils.NavigateToPage("MainMenu");
        }

        private async void ReplaceWafer()
        {
            var res = await DialogHost.Show(new SelectionDialog());
            if (res is string dialogResult)
            {
                if (dialogResult == SelectionDialog.YES)
                {
                    await AutoCutUtils.ReplaceWaferAndResetAsync();
                }
                else
                {
                    await AutoCutUtils.ReplaceWaferAsync();
                }
            }
            CutY = Appsettings.CutY ?? 0;
        }

        private async void ReplaceSharpeningBoard()
        {
            var res = await DialogHost.Show(new SelectionDialog());
            if (res is string dialogResult)
            {
                if (dialogResult == SelectionDialog.YES)
                {
                    await AutoCutUtils.ReplaceSharpeningBoardAndResetAsync();
                }
                else
                {
                    await AutoCutUtils.ReplaceSharpeningBoardAsync();
                }
            }
            SharpenY = Appsettings.SharpenY ?? 0;
        }

        private async void ReplaceBlade()
        {
            await AutoCutUtils.ReplaceBladeAsync();
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
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
    }
}
