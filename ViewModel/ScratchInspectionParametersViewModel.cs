using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Entities;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;

namespace 精密切割系统.ViewModel
{
    internal class ScratchInspectionParametersViewModel : CustomBindableBase
    {
        private readonly IRegionManager _regionManager;
        private int _id;
        private bool _look;

        private ScratchInspectionParametersEntity _scratchInspectionParametersEntity;

        public ScratchInspectionParametersEntity Entity
        {
            get { return _scratchInspectionParametersEntity; }
            set { SetProperty(ref _scratchInspectionParametersEntity, value); }
        }

        public ScratchInspectionParametersViewModel()
        {
        }

        public ScratchInspectionParametersViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        protected override void InitRightButton()
        {
            base.InitRightButton();
            AddRightButton(ButtonParams.Sure(SureAsync));
            AddRightButton(ButtonParams.Back(Back));
        }

        private void Back()
        {
            NavigateUtils.NavigateToPage("Pages/F3_ModelCatalog/MCDeviceDataConf", $"id={_id}&look={_look}");
        }

        private async Task SureAsync()
        {
            if (RegionUtils.FormError(_regionManager))
            {
                MaterialSnack(RegionUtils.FormErrorMessage, SnackType.WARNING);
                return;
            }
            MaterialSnack("保存中...", SnackType.INFO);
            await SqlHelper.UpdateAsync(Entity);
            MaterialSnack("保存成功", SnackType.SUCCESS);
        }

        protected override void InitBottomButton()
        {
            base.InitBottomButton();
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            if (navigationContext.Parameters.TryGetValue<int>("id", out var id))
            {
                _id = id;
            }
            if (navigationContext.Parameters.TryGetValue<bool>("look", out var look))
            {
                _look = look;
            }
            Entity = await SqlHelper.GetOrCreateEntityAsync(() => new ScratchInspectionParametersEntity());
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
        }
    }
}