using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Model.common;
using 精密切割系统.View.common;

namespace 精密切割系统.Helpers
{
    public class CustomBindableBase : BindableBase, INavigationAware
    {
        // 控制右侧按钮
        protected ObservableCollection<ButtonParams> RightButtonCollection { get; } = WindowLayout.RightPageButtons;

        // 控制底部侧按钮
        protected ObservableCollection<ButtonParams> BottomButtonCollection { get; } = WindowLayout.OperatePageButtons;

        protected virtual void InitRightButton()
        {
            RightButtonCollection.Clear();
        }

        protected virtual void InitBottomButton()
        {
            BottomButtonCollection.Clear();
        }

        protected void AddRightButton(ButtonParams button)
        {
            RightButtonCollection.Add(button);
        }

        protected void AddBottomButton(ButtonParams button)
        {
            BottomButtonCollection.Add(button);
        }

        public virtual bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {
            RightButtonCollection.Clear();
            BottomButtonCollection.Clear();
        }

        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
            NavigateUtils.ClearOperatePage();
            NavigateUtils.ClearMainFrame();
            InitRightButton();
            InitBottomButton();
        }
    }
}