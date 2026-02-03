using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.F8_ElectricalDischargeTruing
{
    /// <summary>
    /// ElectricalDischargeTruing.xaml 的交互逻辑
    /// </summary>
    public partial class ElectricalDischargeTruing : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;

        // 运行状态 0 未运行 1 运行中 2 暂停中
        private static int _runFlag = 0;

        private ElectricalDischargeTruingViewModel model;

        public ElectricalDischargeTruing()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void BtnElectricalBack_RightClicked(object? sender, bool e)
        {
        }

        /// <summary>
        /// 暂停修刀
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnElectricalPause_RightClicked(object? sender, bool e)
        {
        }

        private void BtnElectricalStart_RightClicked(object? sender, bool e)
        {
        }

        private void OperatePage_onClicked(object? sender, int code)
        {
        }

        private void BtnSure_RightClicked(object sender, bool e)
        {
        }

        private void btnStart()
        {
        }

        public void Finish()
        {
        }

        private void save(bool showTips = true)
        {
        }

        /// <summary>
        /// 设置单元格状态
        /// </summary>
        /// <param name="status"></param>
        public void SetInputTextBoxEnable(bool status)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                List<InputTextBox> inputTextBoxes = Tools.GetChildrenOfType<InputTextBox>(this);
                inputTextBoxes.ForEach(inputTextBox =>
                {
                    if (!inputTextBox.Name.Equals("currentRepairNumInput") && !inputTextBox.Name.Equals("repeatCountInput")
                        && !inputTextBox.Name.Equals("allDressersNumInput") && !inputTextBox.Name.Equals("clearDressersNumInput"))
                    {
                        inputTextBox.IsEnabled = status;
                    }
                });
            });
        }

        public void initTbNumber()
        {
            List<InputTextBox> tbs = Tools.GetChildrenOfType<InputTextBox>(this);
            for (int i = 0; i < tbs.Count; i++)
            {
                tbs[i].initNumber();
            }
        }

        /// <summary>
        /// 表单内容是否错误  false是正常 true是出错了
        /// </summary>
        /// <returns>false表示没有错误，true表示出错了</returns>
        public bool FormError()
        {
            bool result = false;
            List<InputTextBox> tbs = Tools.GetChildrenOfType<InputTextBox>(this);
            for (int i = 0; i < tbs.Count; i++)
            {
                bool isError = tbs[i].XIsError;
                if (isError)
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// 表单内容验证通过  false是不通过 true是通过
        /// </summary>
        /// <returns>false是不通过 true是通过</returns>
        public bool FormSuccess()
        {
            return !FormError();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            BtnElectricalBack_RightClicked(null, false);
        }
    }
}