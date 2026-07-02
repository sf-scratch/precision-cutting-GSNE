using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Helpers;
using 精密切割系统.Helpers.GTN;
using 精密切割系统.Model.common;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.ViewModel
{
    internal class SuctionCupReplacementViewModel : CustomBindableBase
    {
        protected override void InitRightButton()
        {
            base.InitRightButton();
            AddRightButton(ButtonParams.Back(Back));
        }

        private void Back()
        {
            NavigateUtils.NavigateToPage("MainMenu");
        }

        protected override void InitBottomButton()
        {
            base.InitBottomButton();
            AddBottomButton(ButtonParams.BlueButton("工作盘真空", "VacuumOutline", OutputConfig.Instance.TriggerWorkVacuumSwitchAsync));
        }
    }
}