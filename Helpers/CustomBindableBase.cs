using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.View.common;

namespace 精密切割系统.Helpers
{
    public class CustomBindableBase : BindableBase, INavigationAware
    {
        public virtual bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {
            WindowLayout.RightPageButtons.Clear();
            WindowLayout.OperatePageButtons.Clear();
        }

        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
            NavigateUtils.ClearRightPage();
            NavigateUtils.ClearMainFrame();
        }
    }
}
