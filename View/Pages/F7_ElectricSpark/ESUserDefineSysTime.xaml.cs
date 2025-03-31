using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.Helpers;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.View.page.right;
using static log4net.Appender.RollingFileAppender;

namespace 精密切割系统.View.Pages.F7_ElectricSpark
{
    /// <summary>
    /// ESUserDefineSysTime.xaml 的交互逻辑
    /// </summary>
    public partial class ESUserDefineSysTime : Page
    {
        private DispatcherTimer timer;
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        public ESUserDefineSysTime()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mainWindow = Application.Current.MainWindow as MainWindow;
            rightPage = mainWindow.rightFrame.Content as RightPage;
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            rightPage.btnSure.Visibility = Visibility.Visible;
            rightPage.btnSure.BackFlag = false;
            rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked);
            //底部操作按钮
            mainWindow.UpdateOperatePage([], null);

            UpdateTime();
            InitTime();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1); // 每秒更新一次
            timer.Tick += Timer_Tick;
            timer.Start(); 
        }
        private void BtnBack_RightClicked(object? sender, bool e)
        {
            mainWindow.NavigateToPage("Pages\\F7_ElectricSpark\\ESUserDefineDataConf");
        }

        private void BtnSure_RightClicked(object? sender, bool e)
        {
            //
            //执行数据库数据保存。
            var success = this.FormSuccess();
            if (success)
            {
                saveData();
            }
            else
            {
                MaterialSnackUtils.MaterialSnack("数据异常", MaterialSnackUtils.SnackType.ERROR);
            }
        }
        public async void saveData()
        {
            string year = tbYear.Text.Trim(); // 年份（四位） 
            string month = tbMonth.Text.Trim(); // 月份（两位）
            string day = tbDay.Text.Trim(); // 日期（两位） 
            string hour = tbHour.Text.Trim(); // 小时（两位） 
            if ("00" == hour && "0." == hour) {
                tbHour.Text = "0";
                hour = "0";
            }
            string minute = tbMinute.Text.Trim(); // 分钟（两位） 
            if ("00" == minute && "0." == minute)
            {
                tbMinute.Text = "0";
                minute = "0";
            }
            string second = tbSecond.Text.Trim(); // 秒钟（两位） 
            if ("00" == second && "0." == second)
            {
                tbMinute.Text = "0";
                second = "0";
            }
            string dateTimeStr = year + "-" + month + "-" + day + " " + hour + ":" + minute + ":" + second;
            DateTime dateTime;
            bool isTry = DateTime.TryParse(dateTimeStr, out dateTime);
            if (isTry)
            {
                bool isSucess = UpdateTimeHelper.SetDate(dateTime);
                if (isSucess)
                {
                    MaterialSnackUtils.MaterialSnack("设置成功！", MaterialSnackUtils.SnackType.SUCCESS);
                }
                else 
                {
                    MaterialSnackUtils.MaterialSnack("设置失败！", MaterialSnackUtils.SnackType.SUCCESS);
                }
            }
            else
            {
                MaterialSnackUtils.MaterialSnack("无法转换成日期！", MaterialSnackUtils.SnackType.ERROR);
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateTime();
        }

        private void UpdateTime()
        {
            DateTime now = DateTime.Now;
            tbNowTime.Text = now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void InitTime()
        {
            DateTime dateTime = DateTime.Now;
            string year = dateTime.ToString("yyyy"); // 年份（四位）
            tbYear.Text = year;
            string month = dateTime.ToString("MM"); // 月份（两位）
            tbMonth.Text = month;
            string day = dateTime.ToString("dd"); // 日期（两位）
            tbDay.Text = day;
            string hour = dateTime.ToString("HH"); // 小时（两位）
            tbHour.Text = hour;
            string minute = dateTime.ToString("mm"); // 分钟（两位）
            tbMinute.Text = minute;
            string second = dateTime.ToString("ss"); // 秒钟（两位） 
            tbSecond.Text = second;
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
                tbs[i].RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
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

    }
}
