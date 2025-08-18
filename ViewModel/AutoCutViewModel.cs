using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Helpers;
using 精密切割系统.Model.cut;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.View.Pages.F4_BladeMaintenance;

namespace 精密切割系统.ViewModel
{
    public class AutoCutViewModel : CustomBindableBase
    {
        private readonly IRegionManager _regionManager;

        public AutoCutViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            string? redirectTarget = navigationContext.Parameters.GetValue<string>(AutoCut.RedirectTarget);
            if (redirectTarget != null )
            {
                _regionManager.RequestNavigate(RegionName.AutoCutStateRegion, redirectTarget, navigationContext.Parameters);
            }
        }
    }
}
