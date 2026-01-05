using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.database.db.modle;
using 精密切割系统.Entities;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.View.Pages.F4_BladeMaintenance;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.ViewModel
{
    internal class BMParameterMaintenance2ViewModel : CustomBindableBase
    {
        private readonly IRegionManager _regionManager;

        private BMParameterMaintenanceEntity _entity;

        public BMParameterMaintenanceEntity Entity
        {
            get { return _entity; }
            set { SetProperty(ref _entity, value); }
        }

        private string _measureHeightX;

        public string MeasureHeightX
        {
            get { return _measureHeightX; }
            set { SetProperty(ref _measureHeightX, value); }
        }

        private string _measureHeightY;

        public string MeasureHeightY
        {
            get { return _measureHeightY; }
            set { SetProperty(ref _measureHeightY, value); }
        }

        public BMParameterMaintenance2ViewModel()
        {
        }

        public BMParameterMaintenance2ViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        protected override void InitRightButton()
        {
            base.InitRightButton();
            AddRightButton(ButtonParams.GreenRightButton("保存", "/Assets/icon/tab_1/01/tab_12.png", SaveAsync));
            AddRightButton(ButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private void Back()
        {
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(BMParameterMaintenance));
        }

        private async Task SaveAsync()
        {
            await SqlHelper.UpdateAsync(Entity);
            InitialPositionModel initialPosition = await SqlHelper.GetOrCreateEntityAsync(() => new InitialPositionModel());
            initialPosition.BladeSetupInitX  = MeasureHeightX;
            initialPosition.BladeSetupInitY  = MeasureHeightY;
            await SqlHelper.UpdateAsync(initialPosition);
        }

        protected override void InitBottomButton()
        {
            base.InitBottomButton();
            AddBottomButton(ButtonParams.BlueButton("参数1", "", ToBMParameterMaintenance));
        }

        private void ToBMParameterMaintenance()
        {
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(BMParameterMaintenance));
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            Entity = await SqlHelper.GetOrCreateEntityAsync(() => new BMParameterMaintenanceEntity());
            InitialPositionModel initialPosition = await SqlHelper.GetOrCreateEntityAsync(() => new InitialPositionModel());
            MeasureHeightX = initialPosition.BladeSetupInitX;
            MeasureHeightY = initialPosition.BladeSetupInitY;
        }
    }
}