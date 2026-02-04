using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.View.Pages.common;
using 精密切割系统.View.Pages.F2_ManualOperation;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.ViewModel
{
    internal class AuxiliaryLineViewModel : CustomBindableBase
    {
        private static CameraCommon? _cameraCommon;
        private DynamicIntervalTimer _intervalTimer;
        private string? _navigationPageName;

        private DelegateCommand _loadedCommand;

        public DelegateCommand LoadedCommand => _loadedCommand ??= new DelegateCommand(ExecuteLoadedCommand);

        private void ExecuteLoadedCommand()
        {
            _cameraCommon = AutoCutUtils.GetCameraCommon();
        }

        protected override void InitRightButton()
        {
            base.InitRightButton();
        }

        protected override void InitBottomButton()
        {
            base.InitBottomButton();
            AddBottomButton(ButtonParams.BlueButton("辅助线调窄", "UnfoldLessHorizontal", null, AuxiliaryLineNarrowing, StopUpdateCameraCommonLine));
            AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
            AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
            AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
            AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
            AddBottomButton(ButtonParams.BlueButton("辅助线调宽", "UnfoldMoreHorizontal", null, AuxiliaryLineWidening, StopUpdateCameraCommonLine));
            AddBottomButton(ButtonParams.BlueButton("辅助线开启/关闭", "Update", OpenOrCloseAuxiliaryLineAsync));
            AddBottomButton(ButtonParams.BlueButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private async Task OpenOrCloseAuxiliaryLineAsync()
        {
            var userDefine = await SqlHelper.GetOrCreateEntityAsync(() => new UserDefineDataModel());
            userDefine.HasEdgeLine = !userDefine.HasEdgeLine;
            await SqlHelper.UpdateAsync(userDefine);
            _cameraCommon?.Flash();
        }

        private void Back()
        {
            if (_navigationPageName is not null)
            {
                NavigationParameters parameters = new NavigationParameters { { "TemporaryNavigate", true } };
                ContainerLocator.Container.Resolve<IRegionManager>().RequestNavigate(RegionName.MainRegion, _navigationPageName, parameters);
            }
            else
            {
                NavigateUtils.NavigateToPage("Pages/F2_ManualOperation/MQManualAlignmentConf");
            }
        }

        private void StopUpdateCameraCommonLine()
        {
            _intervalTimer.Stop();
        }

        private void AuxiliaryLineWidening()
        {
            _cameraCommon?.SetEdgeWidth(CameraOperateUtils.DatumLineChangeStep, 2);
            _intervalTimer.RegisterAction(() => _cameraCommon?.SetEdgeWidth(CameraOperateUtils.DatumLineChangeStep, 2));
            _intervalTimer.Start();
        }

        private void AuxiliaryLineNarrowing()
        {
            _cameraCommon?.SetEdgeWidth(-CameraOperateUtils.DatumLineChangeStep, 2);
            _intervalTimer.RegisterAction(() => _cameraCommon?.SetEdgeWidth(-CameraOperateUtils.DatumLineChangeStep, 2));
            _intervalTimer.Start();
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            _intervalTimer = new DynamicIntervalTimer(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(30));
            _navigationPageName = navigationContext.Parameters.GetValue<string>("NavigationPageName");
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
            _intervalTimer.Dispose();
        }
    }
}