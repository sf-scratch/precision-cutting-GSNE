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
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Model.sqlite;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.F7_ElectricSpark
{
    /// <summary>
    /// FlangeTrimmingConf.xaml 的交互逻辑
    /// </summary>
    public partial class FlangeTrimmingConf : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;
        public FlangeTrimmingConf()
        {
            InitializeComponent();
            mainWindow = Application.Current.MainWindow as MainWindow; ;
        }
        bool positionLoadFlag = true;
        bool trimmingRunFlag = false;
        string progressCurrentCount = "0";
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            rightPage = mainWindow.rightFrame.Content as RightPage;
            operatePage = mainWindow.operateFrame.Content as OperatePage;


            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);

            rightPage.btnSure.Visibility = Visibility.Visible;
            rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked);

            rightPage.btnCutStart.Visibility = Visibility.Visible;
            rightPage.btnCutStart.SetRightClickedHandler(BtnCutStart_RightClicked);

            rightPage.btnCutPause.Visibility = Visibility.Visible;
            rightPage.btnCutPause.SetRightClickedHandler(BtnCutPause_RightClicked);

            mainWindow.UpdateOperatePage(OperateData.GetFlangeTrimmingOperate(), null, TouchLeaveHandler, TouchDownHandler);

            loadAxisPosition();
            loadData();
        }

        private void loadData()
        {
            // 加载数据
            List<FlangeTrimmingModel> model = SqlHelper.Table<FlangeTrimmingModel>().Where(t => t.Id.Equals("0")).ToList();
            if (model != null && model.Count > 0)
            {
                FlangeTrimmingModel flangeTrimmingModel = model[0];
                // 设置 xCenterPosition.Text
                xCenterPosition.Text = flangeTrimmingModel.XCenterPosition.ToString();

                // 设置 yCenterPosition.Text
                yCenterPosition.Text = flangeTrimmingModel.YCenterPosition.ToString();

                // 设置 zCenterPosition.Text
                zCenterPosition.Text = flangeTrimmingModel.ZCenterPosition.ToString();

                // 设置 yIndex.Text
                yIndex.Text = flangeTrimmingModel.CutIndex;

                // 设置 coXDistance.Text
                coXDistance.Text = flangeTrimmingModel.CoXDistance.ToString();

                // 设置 xAxisSpeed.Text
                xAxisSpeed.Text = flangeTrimmingModel.CutSpeed;

                // 设置 spindleRev.Text
                spindleRev.Text = flangeTrimmingModel.SpindleRev.ToString();

                // 设置 repectCount.Text
                repectCount.Text = flangeTrimmingModel.AllRepeatCount.ToString();

                // 设置 grindingStepInterval.Text
                grindingStepInterval.Text = flangeTrimmingModel.GrindingStepInterval.ToString();

                allCount.Text = flangeTrimmingModel.AllRepeatCount.ToString();
            }
        }
        private void loadAxisPosition()
        {
            Thread _thread = new Thread(async () =>
            {
                while (positionLoadFlag)
                {
                    // 获取PLC值
                    string xValue = PlcControl.plc.GetPlcValueString(DeviceKey.curLocationKey);
                    string yValue = PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey);
                    string zValue = PlcControl.plc.GetPlcValueString(DeviceKey.z2CurLocationKey);

                    // 使用Dispatcher.Invoke的异步版本更新UI
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        xCenterPosition.Text = xValue;
                        yCenterPosition.Text = yValue;
                        zCenterPosition.Text = zValue;
                    });

                    // 在工作线程中暂停，避免阻塞UI线程
                    Thread.Sleep(100);
                }
            });
            _thread.IsBackground = true;
            _thread.Start();
        }
        private void TouchLeaveHandler(object sender, int code)
        {
            switch (code)
            {
                case 7400:
                case 7403:
                    // X轴往右
                    // X轴往左
                    PlcControl.tagControl.Xaxis.StopMove();
                    break;
                case 7401:
                case 7404:
                    // Y轴往前
                    // Y轴往后
                    PlcControl.tagControl.Yaxis.StopMove();
                    break;
                case 7402:
                case 7405:
                    // Z轴往下
                    // Z轴往上
                    PlcControl.tagControl.Z1axis.StopMove();
                    break;
                default:
                    break;
            }
        }
        private void TouchDownHandler(object sender, int code)
        {
            switch (code)
            {
                case 7400:
                    // X轴往左
                    PlcControl.tagControl.Xaxis.StartJog(0);
                    break;
                case 7401:
                    // Y轴往后
                    PlcControl.tagControl.Yaxis.StartJog(1);
                    break;
                case 7402:
                    // Z轴往上
                    PlcControl.tagControl.Z1axis.StartJog(1);
                    break;
                case 7403:
                    // X轴往右
                    PlcControl.tagControl.Xaxis.StartJog(1);
                    break;
                case 7404:
                    // Y轴往前
                    PlcControl.tagControl.Yaxis.StartJog(0);
                    break;
                case 7405:
                    // Z轴往下
                    PlcControl.tagControl.Z1axis.StartJog(0);
                    break;
                default:
                    break;
            }
        }
        
        /// <summary>
        /// 更新进度
        /// </summary>
        private void updateProgress()
        {
            Task.Run(() => { 
                while (trimmingRunFlag)
                {
                    string plcCurrentCount = PlcControl.plc.GetPlcValueString(DeviceKey.trimmingCurrentCountKey);
                    if (!progressCurrentCount.Equals(plcCurrentCount))
                    {
                        progressCurrentCount = plcCurrentCount;
                    }
                    Application.Current.Dispatcher.Invoke(() => { 
                        currentCount.Text = progressCurrentCount;
                    });
                    Thread.Sleep(100);
                }
            });
        }
        private void BtnCutPause_RightClicked(object? sender, bool e)
        {
            trimmingRunFlag = false;
            PlcControl.tagControl.flange.StopTrimming();
        }

        private void BtnCutStart_RightClicked(object? sender, bool e)
        {
            trimmingRunFlag = true;
            PlcControl.tagControl.flange.StartTrimming();
            MaterialSnackUtils.MaterialSnack("修整中...", MaterialSnackUtils.SnackType.SUCCESS, -1);
            updateProgress();
        }

        private void BtnSure_RightClicked(object? sender, bool e)
        {
            List<FlangeTrimmingModel> model = SqlHelper.Table<FlangeTrimmingModel>().Where(t => t.Id.Equals("0")).ToList();
            FlangeTrimmingModel flangeTrimmingModel = new FlangeTrimmingModel();
            if (model == null || model.Count == 0)
            {
                flangeTrimmingModel.Id = 1;
            } 
            else
            {
                flangeTrimmingModel = model.FirstOrDefault();
            }
            flangeTrimmingModel.XCenterPosition = Tools.GetFloatStringValue(xCenterPosition.Text);
            flangeTrimmingModel.YCenterPosition = Tools.GetFloatStringValue(yCenterPosition.Text);
            flangeTrimmingModel.ZCenterPosition = Tools.GetFloatStringValue(zCenterPosition.Text);
            flangeTrimmingModel.CutIndex = yIndex.Text;
            flangeTrimmingModel.CoXDistance = Tools.GetFloatStringValue(coXDistance.Text);
            flangeTrimmingModel.CutSpeed = xAxisSpeed.Text;
            flangeTrimmingModel.SpindleRev = Tools.GetIntStringValue(spindleRev.Text);
            flangeTrimmingModel.AllRepeatCount = Tools.GetIntStringValue(repectCount.Text) ;
            flangeTrimmingModel.GrindingStepInterval = Tools.GetIntStringValue(grindingStepInterval.Text);

            allCount.Text = flangeTrimmingModel.AllRepeatCount.ToString();
            // 计算x轴开始和结束位置
            float avgDistance = flangeTrimmingModel.CoXDistance / 2;
            float xStartPosition = flangeTrimmingModel.XCenterPosition - avgDistance;
            float xEndPosition = flangeTrimmingModel.XCenterPosition + avgDistance;
            
            if (model == null || model.Count == 0)
            {
                SqlHelper.Add(flangeTrimmingModel);
            }
            else
            {
                SqlHelper.Update(flangeTrimmingModel);
            }

            PlcControl.tagControl.flange.SetFlangeParams(xStartPosition.ToString(), xEndPosition.ToString()
                , flangeTrimmingModel.YCenterPosition.ToString(), yIndex.Text
                , flangeTrimmingModel.ZCenterPosition.ToString(), repectCount.Text
                , spindleRev.Text,xAxisSpeed.Text, grindingStepInterval.Text);
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            positionLoadFlag = false;
            PlcControl.tagControl.flange.JoinTrimming(0);
            mainWindow.NavigateToPage("MainMenu");
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            positionLoadFlag = true;
            PlcControl.tagControl.flange.JoinTrimming(0);
        }
    }
}
