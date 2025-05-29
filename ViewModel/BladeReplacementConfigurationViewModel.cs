using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using OpenCvSharp;
using Prism.Events;
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
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.HttpClients;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.sqlite;
using 精密切割系统.PubSubEvent;
using 精密切割系统.Utils;
using 精密切割系统.View.common;
using 精密切割系统.View.Pages.Auto;

namespace 精密切割系统.ViewModel
{
    public class BladeReplacementConfigurationViewModel : CustomBindableBase
    {
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        public RelayCommand AutoRunCommand { get; set; }
        public RelayCommand<string> InitCommand { get; set; }
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

        private LunguSksjModel _lunguSks;

        public LunguSksjModel LunguSks
        {
            get { return _lunguSks; }
            set { _lunguSks = value; RaisePropertyChanged(); }
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

        public BladeReplacementConfigurationViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            LunguId = CameraUtils.GetLunguId();
            _rightButtonParams = WindowLayout.RightPageButtons;
            _operatePageButtonCollection = WindowLayout.OperatePageButtons;
            AutoRunCommand = new RelayCommand(AutoRunAsync);
            InitCommand = new RelayCommand<string>(Init);
        }

        public BladeReplacementConfigurationViewModel()
        {
        }

        private void InitRightButtonOnlyBack()
        {
            _rightButtonParams.Clear();
            _rightButtonParams.Add(RightButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private void InitRightButton()
        {
            _rightButtonParams.Clear();
            _rightButtonParams.Add(RightButtonParams.GreenRightButton("自动执行", "/Assets/icon/right/enter.png", AutoRunAsync));
            _rightButtonParams.Add(RightButtonParams.GreenRightButton("重置磨刀", "/Assets/icon/menu_6/menu_6_1_white.png", SharpenService.Instance.Reset));
            _rightButtonParams.Add(RightButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private void InitBottomButton()
        {
            _operatePageButtonCollection.Add(RightButtonParams.BlueRightButton("换刀片", "/Assets/icon/tab_1/03/tab_02.png", ReplaceBlade, null, 8));
            _operatePageButtonCollection.Add(RightButtonParams.BlueRightButton("换磨刀板", "/Assets/icon/tab_1/03/tab_05.png", ReplaceSharpeningBoard, null, 8));
            _operatePageButtonCollection.Add(RightButtonParams.BlueRightButton("换硅片", "/Assets/icon/tab_1/03/tab_05.png", ReplaceSharpeningBoard, null, 8));
        }

        private async void Init(string lunguId)
        {
            if (!_semaphore.Wait(0)) // 尝试获取锁（0 = 不等待）
            {
                return; // 如果锁已被占用，直接返回
            }
            try
            {
                //轮毂信息
                //LunguSksjDTO lunguSksjDTO = new LunguSksjDTO();
                LunguSksjDTO? lunguSksjDTO = await HttpUtils.GetLunguSksjAsync(lunguId);
                if (lunguSksjDTO == null)
                {
                    InitRightButtonOnlyBack();
                    MaterialSnackUtils.MaterialSnack("轮毂信息获取失败！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                    return;
                }
                LunguSks = MapperConfig.Mapper.Map<LunguSksjModel>(lunguSksjDTO);

                //磨刀参数
                int bmSharpParamId = 1;
                List<BmSharpenParameterModel> list = await SqlHelper.TableAsync<BmSharpenParameterModel>()
                                    .Where(t => t.Id == bmSharpParamId).ToListAsync();
                if (list.Count <= 0)
                {
                    MaterialSnackUtils.MaterialSnack("磨刀参数获取错误！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                    return;
                }
                BmSharpenParameterModel sharpenParam = list[0];
                SharpenParams = new SharpenParamsModel
                {
                    RotateSpeed = sharpenParam.RotateSpeed.ToInt(),
                    CutThickness = sharpenParam.CutThickness,
                    CoJiaoHeight = sharpenParam.CoJiaoHeight,
                    CutHeight = float.Parse(sharpenParam.CutThickness) + sharpenParam.CoJiaoHeight - AutoCutUtils.GetSharpenDeep(LunguSks.BladeType),
                    CoOffsetX = sharpenParam.CoOffsetX,
                    CutSize = 0.3f,
                    CutNum = 0,
                    HightestCutSpeed = SharpenService.GetCutSpeed(lunguSksjDTO.ABAverageThickness / 1000, false),
                    CutNum1 = 0,
                    CutNum2 = 0
                };

                //切割参数
                long fileTableId = CurrentUtils.GetCurrentConfiguration().DeviceDataId;
                // 查询配置信息
                var listConf = await SqlHelper.TableAsync<FileTableItemModel>().Where(t => t.Id == fileTableId).ToListAsync();
                if (listConf.Count == 0)
                {
                    MaterialSnackUtils.MaterialSnack("切割参数获取错误！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                    return;
                }
                FileTableItemModel fileTable = listConf[0];
                // 查询通道信息
                List<FileTableItemChModel> chModels = await SqlHelper.TableAsync<FileTableItemChModel>()
                    .Where(t => t.ItemId == fileTable.Id).ToListAsync();
                if (chModels.Count == 0)
                {
                    MaterialSnackUtils.MaterialSnack("切割通道参数获取错误！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                    return;
                }
                FileTableItemChModel fileTableCh = chModels[0];
                CutParams = new CutParamsModel
                {
                    CutHeight = float.Parse(fileTable.TapeThickness) + float.Parse(fileTable.WorkThickness) - AutoCutUtils.GetCuttingDeep(LunguSks.BladeType),
                    TapeThickness = fileTable.TapeThickness,
                    SpindleRev = fileTable.SpindleRev,
                    PrecutProcessNo = fileTable.PrecutProcessNo,
                    MaxCutSpeed = CutService.GetCutSpeed(lunguSksjDTO.ABAverageThickness / 1000),
                    CutNum = fileTableCh.CutLine.ToInt(),
                    WorkThickness = fileTable.WorkThickness,
                    DeviceDataNo = fileTable.DeviceDataNo,
                    OffsetX = fileTableCh.OffsetX.ToInt()
                };
                InitRightButton();
                MaterialSnackUtils.MaterialSnack("", MaterialSnackUtils.SnackType.INFO, 0, _eventAggregator);
            }
            finally
            {
               _semaphore.Release(); // 释放锁
            }

        }

        private async void AutoRunAsync()
        {
            if (!await PlcControl.tagControl.wholeDevice.IsOpenVacuumSwitchAsync())
            {
                MaterialSnackUtils.MaterialSnack("未打开工作盘真空！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                return;
            }
            NavigationParameters parameters = new NavigationParameters
            {
                { "SharpenParams", SharpenParams },
                { "CutParams", CutParams },
                { "LunguId", LunguId }
            };
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(AutoCutRuning), parameters);
        }

        private void Back()
        {
            NavigateUtils.NavigateToPage("MainMenu");
        }

        private async void ReplaceWafer()
        {
            await AutoCutUtils.ReplaceWaferAsync();
        }

        private async void ReplaceSharpeningBoard()
        {
            await AutoCutUtils.ReplaceSharpeningBoardAsync();
        }

        private async void ReplaceBlade()
        {
            await AutoCutUtils.ReplaceBladeAsync();
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            LunguSks = new LunguSksjModel();
            SharpenParams = new SharpenParamsModel();
            CutParams = new CutParamsModel();
            InitRightButtonOnlyBack();
            InitBottomButton();
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                _eventAggregator.GetEvent<SetFocusEvent>().Publish("lunguTextBox");
            }), DispatcherPriority.Loaded);
        }
    }
}
