using Emgu.CV.CvEnum;
using NPOI.OpenXmlFormats.Dml;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Model.position.Bayesian;
using 精密切割系统.Model.position.correction;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages
{
    /// <summary>
    /// FrmMain.xaml 的交互逻辑
    /// </summary>
    public partial class FrmMain : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;

        public FrmMain()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mainWindow = Application.Current.MainWindow as MainWindow;
            rightPage = mainWindow.rightFrame.Content as RightPage;

            testAxisInput.SelectedIndex = 0;
            directionInput.SelectedIndex = 0;
            testStartPositionInput.Text = "0";
            targetPositionInput.Text = "1";
            testNumInput.Text = "10";
            testIndexNumInput.Text = "1";

            //右侧显示
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible; //右侧显示 - 返回按钮显示
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            //底部操作按钮
            mainWindow.UpdateOperatePage([], null);

            Thread displayTagValue = new Thread(new ThreadStart(updateUiDisplayValue));
            displayTagValue.IsBackground = true;
            displayTagValue.Start();
            stopTime.Text = stopTimeValue + "";

            GlobalParams.upPosition = -100;
            // 上一次光栅尺
            GlobalParams.upRealPosition = -100;
            GlobalParams.allDeepValue = 0;

            // yCompCheckbox.IsChecked = true;
            // z1CompCheckbox.IsChecked = true;
        }

        //返回到列表页面
        private void BtnBack_RightClicked(object? sender, bool e)
        {
            mainWindow.NavigateToPage("MainMenu");
        }

        private Thread _thread;
        private static bool runFlag = false;
        private static bool pauseFlag = false;
        private static int stopTimeValue = 5000;
        private static string defaultSpeed = "10";

        private void confirmPositionBtn_Click(object sender, RoutedEventArgs e)
        {
        }

        private void updateUiDisplayValue()
        {
        }

        private void startTestBtn_Click(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// 重复走
        /// </summary>
        public void RepeatedRun()
        {
        }

        private void pauseTestBtn_Click(object sender, RoutedEventArgs e)
        {
            pauseFlag = !pauseFlag;
            pauseTestBtn.Content = pauseFlag ? "继续测量" : "暂停测量";
            testStatus.Text = pauseFlag ? "暂停中..." : "测量中...";
        }

        private void endTestBtn_Click(object sender, RoutedEventArgs e)
        {
            runFlag = false;
            pauseFlag = false;
            pauseTestBtn.Content = "暂停测量";
            testStatus.Text = "准备中...";
        }

        private void compCheckbox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            GlobalParams.runCompFlag = checkBox.IsChecked == true;
            Debug.WriteLine(GlobalParams.runCompFlag);
        }

        private bool absoluteFlag = false;

        private void customConfirmPositionBtn_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            runFlag = false;
            pauseFlag = false;
        }

        private void repeatedCheckbox_Click(object sender, RoutedEventArgs e)
        {
        }

        private void rulerPositionBtn_Click(object sender, RoutedEventArgs e)
        {
        }

        private void stepTestBtn_Click(object sender, RoutedEventArgs e)
        {
        }

        private void compTestBtn_Click(object sender, RoutedEventArgs e)
        {
        }

        private void z1CompCheckbox_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void yCompCheckbox_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void yCompCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
        }

        private void z1CompCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
        }
    }
}