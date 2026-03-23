using DryIoc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.Pages.F2_ManualOperation;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.ViewModel
{
    internal class AutomaticCuttingConfViewModel : CustomBindableBase
    {
        private readonly SemiAutoCutService _semiAutoCutService = SemiAutoCutService.Instance;
        private readonly IRegionManager _regionManager;

        private AutomaticCuttingConfModel _model = new();

        public AutomaticCuttingConfModel Model
        {
            get { return _model; }
            set { SetProperty(ref _model, value); }
        }

        public AutomaticCuttingConfViewModel()
        {
        }

        public AutomaticCuttingConfViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        protected override void InitRightButton()
        {
            base.InitRightButton();
            AddRightButton(ButtonParams.GreenRightButton("开始", "/Assets/icon/right/enter.png", StartAsync));
            AddRightButton(ButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", BackAsync));
        }

        private async Task StartAsync()
        {
            if (AlarmConfig.Instance.HasActiveErrorAlarm())
            {
                MaterialSnack(AlarmConfig.HasErrorAlarmMessage, SnackType.WARNING);
                return;
            }
            if (WarmUpHelper.IsRuning)
            {
                MaterialSnack("请先结束暖机再开始切割！", SnackType.WARNING);
                return;
            }
            if (ThetaAlignService.ChDictionary is null || ThetaAlignService.ChDictionary.Count == 0)
            {
                MaterialSnack("请先完成自动切割页面中的手动校准！", SnackType.WARNING);
                return;
            }
            if (Appsettings.BladeOuterDiameter is null)
            {
                MaterialSnack("未设置刀片外径！", SnackType.WARNING);
                return;
            }
            CommonResult<List<ChCutStep>> cutStepResult = await AutoCutUtils.GenerateCutStepListAsync(ThetaAlignService.ChDictionary);
            if (!cutStepResult.IsSuccess || cutStepResult.Data is null)
            {
                MaterialSnack(cutStepResult.Message, SnackType.WARNING);
                return;
            }
            _semiAutoCutService.CutDirection = CutDirection.Backward;
            var userDefineData = await SqlHelper.GetOrCreateEntityAsync(() => new UserDefineDataModel());
            float cutYPositiveLimit = userDefineData.CutYPositiveLimit.ToFloat();
            float cutYNegativeLimit = userDefineData.CutYNegativeLimit.ToFloat();
            var chCutSteps = cutStepResult.Data;
            foreach (var chCutStep in chCutSteps)
            {
                float yPositon = chCutStep.CutSteps.First().ChannelStartY;
                for (int i = 0; i < chCutStep.CutSteps.Count; i++)
                {
                    if (yPositon > cutYPositiveLimit)
                    {
                        MaterialSnack($"{chCutStep.ChName}面 第{i + 1}刀，将超出切割正限位！", SnackType.WARNING);
                        return;
                    }
                    else if (yPositon < cutYNegativeLimit)
                    {
                        MaterialSnack($"{chCutStep.ChName}面 第{i + 1}刀，将超出切割负限位！", SnackType.WARNING);
                        return;
                    }
                    CutStep cutStep = chCutStep.CutSteps[i];
                    switch (_semiAutoCutService.CutDirection)
                    {
                        case CutDirection.Backward:
                            yPositon -= cutStep.NextStepDistance;
                            break;

                        case CutDirection.Forward:
                            yPositon += cutStep.NextStepDistance;
                            break;

                        default: break;
                    }
                }
            }
            _semiAutoCutService.CutLine = 0;
            _semiAutoCutService.SpindleRev = Model.SpindleRev.ToInt();
            NavigationParameters parameters = new() { { "cutSteps", chCutSteps }, { "backPageName", nameof(AutomaticCuttingConf) } };
            ContainerLocator.Container.Resolve<IRegionManager>().RequestNavigate(RegionName.MainRegion, nameof(MQSemiAutomaticCuttingRun), parameters);
            var chDictionary = ThetaAlignService.ChDictionary;
        }

        private async Task BackAsync()
        {
            var operationParams = await CurrentUtils.GetOperationParametersModelAsync();
            if (operationParams.IsExitCutClearManualCompensation)
            {
                _semiAutoCutService.DepthCompensationValue = 0;
            }
            _semiAutoCutService.FeedSpeedCompCompensationValue = 0;
            NavigateUtils.NavigateToPage("MainMenu");
        }

        protected override void InitBottomButton()
        {
            base.InitBottomButton();
            AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
            AddBottomButton(ButtonParams.BlueButton("高度补偿", "/Assets/icon/tab_1/02/tab_20.png", UpdateDepthCompensation));
            AddBottomButton(ButtonParams.BlueButton("型号参数", "/Assets/icon/tab_0/tab_02.png", () => NavigateUtils.NavigateToPage("Pages/F3_ModelCatalog/MCDeviceDataConf", $"id={CurrentUtils.GetCurrentConfiguration().DeviceDataId}&look={false}")));
            AddBottomButton(ButtonParams.BlueButton("手动校准", "/Assets/icon/tab_1/02/tab_21.png", NavigateToManualAlignmentAsync));
            AddBottomButton(ButtonParams.BlueButton("切割水", "/Assets/icon/tab_0/tab_05.png", PlcControl.tagControl.wholeDevice.TriggerCuttingWaterAsync, isOpenFunc: PlcControl.tagControl.wholeDevice.IsOpenSpindleCuttingWaterAsync, openOrCloseVisibility: System.Windows.Visibility.Visible));
            AddBottomButton(ButtonParams.BlueButton("暖机", "/Assets/icon/menu_2/menu_2_3_white.png", WarmUpHelper.TriggerWarmUpAsync));
            AddBottomButton(ButtonParams.BlueButton("速度更改", "/Assets/icon/tab_1/02/tab_25.png", UpdateFeedSpeedCompCompensation));
            AddBottomButton(ButtonParams.BlueButton("刀片状态信息", "/Assets/icon/tab_1/03/tab_03.png", () => NavigateUtils.NavigateToPage("Pages/F4_BladeMaintenance/BladeInfo")));
            AddBottomButton(ButtonParams.BlueButton("预切启动", "/Assets/icon/tab_1/02/tab_27.png", TirggerPrecut));
            AddBottomButton(ButtonParams.BlueButton("C/T真空", "/Assets/icon/tab_1/02/tab_23.png", TriggerVacuumSwitchAsync, isOpenFunc: PlcControl.tagControl.wholeDevice.IsOpenVacuumSwitchAsync, openOrCloseVisibility: System.Windows.Visibility.Visible));
        }

        private async Task TriggerVacuumSwitchAsync()
        {
            var operationParameter = await CurrentUtils.GetOperationParametersModelAsync();
            if (!operationParameter.IsAutoShutOffWaterWhenCuttingCompleted && operationParameter.IsAutoShutOffWaterWhenCloseVacuum)
            {
                await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
            }
            if (SemiAutoCutService.Instance.HasNotTakenOutWorkpiecesAfterCuttingCompleted)
            {
                await AutoCutUtils.ReplaceWaferAsync(default, TaskUtils.GetTimeoutCancellationToken(TimeSpan.FromSeconds(120)).Token);
            }
            await PlcControl.tagControl.wholeDevice.TriggerVacuumSwitchAsync();
        }

        private void TirggerPrecut()
        {
            _semiAutoCutService.TriggerPrecut(true);
        }

        private void UpdateFeedSpeedCompCompensation()
        {
            if (RegionUtils.FormError(_regionManager))
            {
                MaterialSnack(RegionUtils.FormErrorMessage, SnackType.WARNING);
                return;
            }
            _semiAutoCutService.FeedSpeedCompCompensationValue = Model.ChangeFeedSpeed.ToFloat();
            MaterialSnack($"变更进刀速度设置为 {_semiAutoCutService.FeedSpeedCompCompensationValue}！", SnackType.SUCCESS);
        }

        private async Task NavigateToManualAlignmentAsync()
        {
            CommonResult result = await AutoCutUtils.EnterManualAlignmentAsync();
            if (result.IsSuccess)
            {
                _regionManager.RequestNavigate(RegionName.MainRegion, nameof(ManualAlignment));
            }
            else
            {
                MaterialSnack(result.Message, SnackType.WARNING);
            }
        }

        private void UpdateDepthCompensation()
        {
            if (RegionUtils.FormError(_regionManager))
            {
                MaterialSnack(RegionUtils.FormErrorMessage, SnackType.WARNING);
                return;
            }
            _semiAutoCutService.DepthCompensationValue = Model.DepthCompensation.ToFloat();
            MaterialSnack($"刀片高度补偿设置为 {_semiAutoCutService.DepthCompensationValue}！", SnackType.SUCCESS);
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            Model.DepthCompensation = _semiAutoCutService.DepthCompensationValue.ToString();
            Model.ChangeFeedSpeed = _semiAutoCutService.FeedSpeedCompCompensationValue.ToString();
            FileTableItemModel fileTableItem = CurrentUtils.GetFileTableItemModel();
            Model.DirectoryName = "Root";
            Model.DeviceDataNo = fileTableItem.DeviceDataNo;
            Model.DeviceDataId = fileTableItem.DeviceDataId;
            Model.AfterReplaceBladeCutTimes = (Appsettings.AfterReplaceBladeCutTimes ?? 0).ToString();
            Model.AfterReplaceBladeCutLength = (Appsettings.AfterReplaceBladeCutLength / 1000 ?? 0).ToString("F2");
            Model.AfterMeasureHeightCutTimes = (Appsettings.AfterMeasureHeightCutTimes ?? 0).ToString();
            Model.AfterMeasureHeightCutLength = (Appsettings.AfterMeasureHeightCutLength / 1000 ?? 0).ToString("F2");
            Model.AfterClearDataCutTimes = (Appsettings.AfterClearDataCutTimes ?? 0).ToString();
            Model.AfterClearDataCutLength = (Appsettings.AfterClearDataCutLength / 1000 ?? 0).ToString("F2");
            Model.SpindleRev = fileTableItem.SpindleRev.ToString();
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
        }
    }
}