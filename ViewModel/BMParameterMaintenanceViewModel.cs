using DryIoc;
using OpenCvSharp.XFeatures2D;
using System;

using System;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using 精密切割系统.Data;
using 精密切割系统.database.db.modle;
using 精密切割系统.Entities;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.View.Pages.F2_ManualOperation;
using 精密切割系统.View.Pages.F4_BladeMaintenance;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.ViewModel
{
    internal class BMParameterMaintenanceViewModel : CustomBindableBase
    {
        private readonly IRegionManager _regionManager;

        private BMParameterMaintenanceEntity _entity;

        public BMParameterMaintenanceEntity Entity
        {
            get { return _entity; }
            set { SetProperty(ref _entity, value); }
        }

        private string _bladeOuterDiameter;

        public string BladeOuterDiameter
        {
            get { return _bladeOuterDiameter; }
            set { SetProperty(ref _bladeOuterDiameter, value); }
        }

        private string _bladeSetupInitZ1;

        public string BladeSetupInitZ1
        {
            get { return _bladeSetupInitZ1; }
            set { SetProperty(ref _bladeSetupInitZ1, value); }
        }

        public BMParameterMaintenanceViewModel()
        {
        }

        public BMParameterMaintenanceViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        protected override void InitRightButton()
        {
            base.InitRightButton();
            AddRightButton(ButtonParams.GreenRightButton("保存", "/Assets/icon/tab_1/01/tab_12.png", SaveAsync));
            AddRightButton(ButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        protected override void InitBottomButton()
        {
            base.InitBottomButton();
            AddBottomButton(ButtonParams.BlueButton("", "", default, buttonVisibility: System.Windows.Visibility.Hidden));
            AddBottomButton(ButtonParams.BlueButton("参数2", "", ToBMParameterMaintenance2));
        }

        private void ToBMParameterMaintenance2()
        {
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(BMParameterMaintenance2));
        }

        private async Task SaveAsync()
        {
            if (RegionUtils.FormError(_regionManager))
            {
                MaterialSnack(RegionUtils.FormErrorMessage, SnackType.WARNING);
                return;
            }
            try
            {
                await SqlHelper.UpdateAsync(Entity);
                Appsettings.BladeOuterDiameter = BladeOuterDiameter.ToFloat();
                var initialPosition = await SqlHelper.GetOrCreateEntityAsync(() => new InitialPositionModel());
                initialPosition.BladeSetupInitZ1 = BladeSetupInitZ1;
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

        private void Back()
        {
            NavigateUtils.NavigateToPage("MainMenu");
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            Entity = await SqlHelper.GetOrCreateEntityAsync(() => new BMParameterMaintenanceEntity());
            BladeOuterDiameter = Appsettings.BladeOuterDiameter?.ToString("F3") ?? string.Empty;
            var initialPosition = await SqlHelper.GetOrCreateEntityAsync(() => new InitialPositionModel());
            BladeSetupInitZ1 = initialPosition.BladeSetupInitZ1;
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
        }
    }
}