using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.ViewModel
{
    internal class FindCenterLineViewModel : CustomBindableBase
    {
        private string? _navigationPageName;

        private string _line1Position;

        public string Line1Position
        {
            get { return _line1Position; }
            set { SetProperty(ref _line1Position, value); }
        }

        private string _line2Position;

        public string Line2Position
        {
            get { return _line2Position; }
            set { SetProperty(ref _line2Position, value); }
        }

        private string _centerLinePosition;

        public string CenterLinePosition
        {
            get { return _centerLinePosition; }
            set { SetProperty(ref _centerLinePosition, value); }
        }

        protected override void InitRightButton()
        {
            base.InitRightButton();
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

        protected override void InitBottomButton()
        {
            base.InitBottomButton();
            AddBottomButton(ButtonParams.BlueButton("边1", "VectorLine", SureLine1Async));
            AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
            AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
            AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
            AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
            AddBottomButton(ButtonParams.BlueButton("边2", "VectorLine", SureLine2Async));
            AddBottomButton(ButtonParams.BlueButton("中心", "FormatVerticalAlignCenter", MoveToCenterLineAsync));
            AddBottomButton(ButtonParams.BlueButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private async Task MoveToCenterLineAsync()
        {
            if (float.TryParse(_line1Position, out float line1Postion) && float.TryParse(_line2Position, out float line2Position))
            {
                float centerLinePosition = (line1Postion + line2Position) / 2;
                CenterLinePosition = centerLinePosition.ToString(GlobalParams.DecimalStringFormat);
                await PlcControl.tagControl.Yaxis.StartAbsoluteAsync(centerLinePosition, 50, default);
                MaterialSnack("已移动到中心位置！", SnackType.SUCCESS);
            }
            else
            {
                MaterialSnack("边1或边2 数据异常！", SnackType.WARNING);
            }
        }

        private async Task SureLine2Async()
        {
            var curY = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync();
            if (curY is not null)
            {
                Line2Position = curY.Value.ToString(GlobalParams.DecimalStringFormat);
            }
        }

        private async Task SureLine1Async()
        {
            var curY = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync();
            if (curY is not null)
            {
                Line1Position = curY.Value.ToString(GlobalParams.DecimalStringFormat);
            }
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            _navigationPageName = navigationContext.Parameters.GetValue<string>("NavigationPageName");
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
        }
    }
}