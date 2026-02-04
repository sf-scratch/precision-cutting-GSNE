using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using 精密切割系统.Extensions;
using 精密切割系统.View.Controls;

namespace 精密切割系统.Helpers
{
    internal class RegionUtils
    {
        public const string FormErrorMessage = "表单存在错误，请检查!";

        public static bool FormError(IRegionManager regionManager)
        {
            var mainRegion = regionManager.Regions[RegionName.MainRegion];
            var activeView = mainRegion.ActiveViews.FirstOrDefault();
            if (activeView is FrameworkElement view)
            {
                return view.HasFormError();
            }
            return false;
        }

        public static object? GetActiveViewDataContext(IRegionManager? regionManager = default)
        {
            if (regionManager == null)
            {
                regionManager = ContainerLocator.Container.Resolve<IRegionManager>();
            }
            var mainRegion = regionManager.Regions[RegionName.MainRegion];
            var activeView = mainRegion.ActiveViews.FirstOrDefault();
            if (activeView is FrameworkElement view)
            {
                return view.DataContext;
            }
            return null;
        }
    }
}