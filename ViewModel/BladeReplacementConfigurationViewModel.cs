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
            LunguId = "T25051502B0002";
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
            _rightButtonParams.Add(RightButtonParams.YelloRightButton("检查轮毂", "/Assets/icon/menu_0/menu_0_2_white.png", () => Init(LunguId)));
            _rightButtonParams.Add(RightButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private void InitRightButton()
        {
            _rightButtonParams.Clear();
            _rightButtonParams.Add(RightButtonParams.GreenRightButton("自动执行", "/Assets/icon/right/enter.png", AutoRunAsync));
            _rightButtonParams.Add(RightButtonParams.YelloRightButton("检查轮毂", "/Assets/icon/menu_0/menu_0_2_white.png", () => Init(LunguId)));
            _rightButtonParams.Add(RightButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private void InitBottomButton()
        {
            _operatePageButtonCollection.Add(RightButtonParams.BlueRightButton("换刀片", "/Assets/icon/tab_1/03/tab_02.png", ReplaceBlade, null, 8));
            _operatePageButtonCollection.Add(RightButtonParams.BlueRightButton("换磨刀板", "/Assets/icon/tab_1/03/tab_05.png", ReplaceSharpeningBoard, null, 8));
            _operatePageButtonCollection.Add(RightButtonParams.BlueRightButton("换硅片", "/Assets/icon/tab_1/03/tab_05.png", ReplaceWafer, null, 8));
        }

        private async void Init(string lunguId)
        {
            if (!_semaphore.Wait(0)) // 尝试获取锁（0 = 不等待）
            {
                return; // 如果锁已被占用，直接返回
            }
            bool isInitSuccess = false;
            try
            {
                //await PdaUtils.ComputerPracticeAsync(lunguId);
                //PdaUtils.AddSharpen(0.5f, 10);
                //PdaUtils.AddSharpen(0.2f, 11);
                //PdaUtils.AddWearAmountAfterCircle(0.035f, 10);
                ////PdaUtils.AddStandardCutSpeed("1");
                ////PdaUtils.AddStandardSharpenSpeed("2");
                ////PdaUtils.AddResidueSharpenTimes("3");
                ////PdaUtils.AddTotalSharpenTimes("4");
                ////PdaUtils.AddToolMarkWidth("5");
                ////PdaUtils.AddToolMarkActualWidth("6");
                ////PdaUtils.AddFirstToolMarkWidth("7");
                ////PdaUtils.AddMaximumCollapseAngle("8");
                ////PdaUtils.AddMaxCutSpeed("9");
                ////PdaUtils.AddSingleCollapseAngle("10");
                //await PdaUtils.SetCompletedAsync();
                //await PdaUtils.QualifiedAsync();
                //await PdaUtils.ScrapAsync(Cv2.ImRead("C:\\MySpace\\Dev\\ProjectXiHua\\precision-cutting-321\\bin\\x64\\Debug\\net8.0-windows\\image\\638851675110259848_cropMatJpg.jpg"));
                //轮毂信息
                //LunguSksjDTO lunguSksjDTO = new LunguSksjDTO();
                //默认theta轴初始在5的位置，防止theta抖动为负值
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
                LunguSksjDTO lunguSksj = lunguSksjResult.Data;
                LunguSks = MapperConfig.Mapper.Map<LunguSksjModel>(lunguSksj);
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
                    CutHeight = float.Parse(sharpenParam.CutThickness) + sharpenParam.CoJiaoHeight - AutoCutUtils.GetSharpenDeep(LunguSks.ABAverageThickness),
                    CoOffsetX = sharpenParam.CoOffsetX,
                    CutSize = 0.3f,
                    CutNum = 0,
                    HightestCutSpeed = SharpenService.GetCutSpeed(lunguSksj.ABAverageThickness / 1000, false),
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
                    CutHeight = float.Parse(fileTable.TapeThickness) + float.Parse(fileTable.WorkThickness) - AutoCutUtils.GetCuttingDeep(LunguSks.ABAverageThickness),
                    TapeThickness = fileTable.TapeThickness,
                    SpindleRev = fileTable.SpindleRev,
                    PrecutProcessNo = fileTable.PrecutProcessNo,
                    //MaxCutSpeed = await CutService.GetCutSpeed(LunguId, lunguSksjDTO.ExistingBlade),
                    //CutNum = await AutoCutUtils.GetTotalCutTimesAsync(LunguId, lunguSksjDTO.ExistingBlade) ?? 0, 
                    MaxCutSpeed = 0,
                    CutNum = 0,
                    WorkThickness = fileTable.WorkThickness,
                    DeviceDataNo = fileTable.DeviceDataNo,
                    OffsetX = fileTableCh.OffsetX.ToInt()
                };
                isInitSuccess = true;
                MaterialSnackUtils.MaterialSnack("", MaterialSnackUtils.SnackType.INFO, 0, _eventAggregator);
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
                _regionManager.RequestNavigate(RegionName.MainRegion, nameof(AutoCut), new NavigationParameters { { "SharpenParams", SharpenParams }, { "CutParams", CutParams }, { "LunguId", LunguId } });
                return;
            }
            //if (!await PlcControl.tagControl.wholeDevice.IsOpenVacuumSwitchAsync())
            //{
            //    MaterialSnackUtils.MaterialSnack("未打开工作盘真空！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
            //    return;
            //}
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
            NavigationParameters parameters = new NavigationParameters
            {
                { "SharpenParams", SharpenParams },
                { "CutParams", CutParams },
                { "LunguSks", LunguSks }
            };
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(AutoCut), parameters);
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
            await PlcControl.tagControl.wholeDevice.CloseBuzzerAsync();
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
