using Emgu.CV.Dnn;
using HslCommunication.Profinet.OpenProtocol;
using Microsoft.Win32;
using NPOI.OpenXmlFormats.Dml.Diagram;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel.Charts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Model.sqlite;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;
using static 精密切割系统.Helpers.MaterialSnackUtils;

namespace 精密切割系统.View.Pages.F7_ElectricSpark
{
    /// <summary>
    /// AutoAlignPosition.xaml 的交互逻辑
    /// </summary>
    public partial class AutoAlignPosition : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;
        AutoAlignPositionParamsViewModel _model;
        bool runFlag = false;
        bool startFlag = false;
        int cutDirection = -1;
        int currentCutLine = 0;
        int exitStatus = 0;
        bool exitFlag = false; // 退出标志
        string plcCurrentNum = "0";
        float yCurrentPosition = 0;
        bool btnPauseFlag = false;
        bool btnReStartFlag = false;
        public AutoAlignPosition()
        {
            InitializeComponent();
            mainWindow = Application.Current.MainWindow as MainWindow;

            // 初始化数据
            // Rows.Add(new CompDataRow { RowIndex = "1", ActualValue = "13.2223", AxisPosition = "" });

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            rightPage = mainWindow.rightFrame.Content as RightPage;
            operatePage = mainWindow.operateFrame.Content as OperatePage;
            mainWindow.UpdateOperatePage(OperateData.GetAutoAlignPositionOperate(), OperatePage_onClicked);
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);

            rightPage.btnSure.Visibility = Visibility.Visible;
            rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked);

            rightPage.btnCutStart.Visibility = Visibility.Visible;
            rightPage.btnCutStart.SetRightClickedHandler(BtnCutStart_RightClicked);

            rightPage.btnCutPause.Visibility = Visibility.Collapsed;
            rightPage.btnCutPause.GlobalRunOperateFlag = true;
            rightPage.btnCutPause.SetRightClickedHandler(BtnCutPause_RightClicked);

            rightPage.btnCutReStart.Visibility = Visibility.Collapsed;
            rightPage.btnCutReStart.SetRightClickedHandler(BtnCutReStart_RightClicked);

            rightPage.btnCutBackward.Visibility = Visibility.Visible;
            rightPage.btnCutFront.Visibility = Visibility.Visible;
            rightPage.btnCutBackward.SetRightClickedHandler(CutBackward);
            rightPage.btnCutFront.SetRightClickedHandler(CutFront);

            _model = new AutoAlignPositionParamsViewModel();

            List<AutoAlignPositionParamsModel> list = SqlHelper.Table<AutoAlignPositionParamsModel>().ToList();
            if (list.Count > 0)
            {
                AutoAlignPositionParamsModel model = list[0];
                _model.Id = model.Id;
                _model.SquareCh2 = model.SquareCh2;
                _model.SquareCh1 = model.SquareCh1;
                _model.WorkbenchCh2 = model.WorkbenchCh2;
                _model.WorkbenchCh1 = model.WorkbenchCh1;
                _model.BladeHeight = model.BladeHeight;
                _model.FeedSpeed = model.FeedSpeed;
                _model.TestCount = model.TestCount;
                _model.SpindleRev = model.SpindleRev;
                _model.YIndex = model.YIndex;
            }
            DataContext = _model;
            // 初始化已有数据
            List<AutoAlignPositionModel> autoAlignPositionModels = SqlHelper.Table<AutoAlignPositionModel>().ToList();
            foreach (var item in autoAlignPositionModels)
            {
                _model.Rows.Add(new CompDataRow { RowIndex = item.RowIndex, ActualValue = item.ActualValue, AxisPosition = item.AxisPosition });
            }
        }
        int sureFlag = 0;
        private void BtnSure_RightClicked(object? sender, bool e)
        {
            if (sureFlag == 0)
            {
                MaterialSnackUtils.MaterialSnack("再次点击确认会清除已有数据，并保存当前数据，请确认！", MaterialSnackUtils.SnackType.WARNING, 0);
                sureFlag = 1;
                return;
            }
            saveData();
        }

        private void CutFront(object sender, bool e)
        {
            SetCutDirection(0);
        }
        private void CutBackward(object sender, bool e)
        {
            SetCutDirection(1);
        }
        /// <summary>
        /// 切割方向 0 前切 1 后切
        /// </summary>
        private void SetCutDirection(int cutDirectionValue)
        {
           cutDirection = cutDirectionValue;
           cutDirectionInput.Text = cutDirectionValue == 0 ? "向前切" : "向后切";
        }
        private void BtnCutReStart_RightClicked(object? sender, bool e)
        {
            if (btnReStartFlag)
            {
                return;   
            }
            runFlag = false;
            btnReStartFlag = true;
        }

        private void BtnCutPause_RightClicked(object? sender, bool e)
        {
            if (btnPauseFlag)
            {
                return;
            }
            runFlag = true;
            testStatus.Text = "正在暂停！";
            btnPauseFlag = true;
        }

        private void SaveOrUpdateModel(AutoAlignPositionParamsModel tempModel)
        {
            if (tempModel.Id != 0)
            {
                SqlHelper.Update(tempModel);
            } else
            {
                SqlHelper.Add(tempModel);
            }
        }
        private void BtnCutStart_RightClicked(object? sender, bool e)
        {
            if (!CommonCheck.CutStatusCheck())
            {
                return;
            }
            yCurrentPosition = float.Parse(PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey));
            SaveOrUpdateModel(_model._model);
            _model.Rows.Clear();
            int testNumValue = Tools.GetIntStringValue(testCount.Text);
            float testYIndexValue = Tools.GetFloatStringValue(testYIndex.Text);
            currentIndex.Text = "0";
            totalLabel.Text = testCount.Text;
            testStatus.Text = "测量中...";
            bool setStartCutFlag = true;

            rightPage.btnCutPause.Visibility = Visibility.Visible;
            rightPage.btnCutStart.Visibility = Visibility.Collapsed;
            rightPage.btnCutBackward.Visibility = Visibility.Collapsed;
            rightPage.btnCutFront.Visibility = Visibility.Collapsed;
            rightPage.btnBack.Visibility = Visibility.Collapsed;
            rightPage.btnCutStart.Visibility = Visibility.Collapsed;
            // 设置状态
            Task.Run(async () => {
                GlobalParams.globalRunFlag = true;
                CheckError();
                // 开始测量 切割完一刀，就增加测量行
                for (int i = 0; i < testNumValue; i++)
                {
                    currentCutLine = i;
                    // 如果参数设置的切割刀数大于0的话，判断当前刀数是否等于，如果等于，则结束
                    if (_model.TestCount > 0 && plcCurrentNum.Equals(_model.TestCount.ToString()))
                    {
                        break;
                    }

                    // 获取当前速度
                    float feedSpeed = _model.FeedSpeed;
                    float cutDistance = 0;
                    // 设置切割参数
                    bool flag = SetParams(feedSpeed, ref cutDistance);
                    // 如果设置参数错误 则返回
                    if (!flag)
                    {
                        // MaterialSnackUtils.MaterialSnack("参数设置识别，请检查参数！", MaterialSnackUtils.SnackType.ERROR);
                        return;
                    }
                    if (setStartCutFlag)
                    {
                        PlcControl.tagControl.cutting.StartCut(0);
                        Thread.Sleep(10);
                        PlcControl.tagControl.cutting.StartCut(1);
                        MaterialSnackUtils.MaterialSnack("测量中....", MaterialSnackUtils.SnackType.SUCCESS, 0);
                        setStartCutFlag = false;

                    }
                    bool stopDisposeFlag = false;
                    string tempPlcCurrentNum = plcCurrentNum;
                    // 如果不相等，说明当前刀已经开始，要发送下一刀数据
                    do
                    {
                        string value = null;
                        try
                        {
                            value = PlcControl.plc.GetPlcValueString(DeviceKey.cutNumKey);
                        }
                        catch (Exception ex)
                        {
                            Tools.LogError("测量中，获取当前刀数失败");
                        }
                        if (value != null)
                        {
                            tempPlcCurrentNum = value;
                        }
                        Thread.Sleep(100);
                        if (runFlag && !stopDisposeFlag) {
                            stopDisposeFlag = true;
                            PlcControl.tagControl.cutting.StopCut(1);
                            MaterialSnackUtils.MaterialSnack("正在暂停....", MaterialSnackUtils.SnackType.WARNING, 0);
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                testStatus.Text = "正在暂停...";
                            });
                            Tools.WaitForValue(DeviceKey.cutStatusKey, 1);
                            GlobalParams.globalRunFlag = false;
                            btnPauseFlag = false;
                            MaterialSnackUtils.MaterialSnack("暂停中....", MaterialSnackUtils.SnackType.WARNING, 0);
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                testStatus.Text = "暂停中...";
                                rightPage.btnCutPause.Visibility = Visibility.Collapsed;
                                rightPage.btnCutReStart.Visibility = Visibility.Visible;
                            });
                            while (runFlag)
                            {
                                Thread.Sleep(100);
                            }
                            PlcControl.tagControl.cutting.StartCut(0);
                            Thread.Sleep(10);
                            PlcControl.tagControl.cutting.StartCut(1);
                            MaterialSnackUtils.MaterialSnack("测量中....", MaterialSnackUtils.SnackType.SUCCESS, 0);
                            Tools.LogInfo("发送测量开始切割信号！");
                            if (CutOperateUtils.MonitorCutStatus())
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    testStatus.Text = "测量中...";
                                    rightPage.btnCutPause.Visibility = Visibility.Visible;
                                    rightPage.btnCutReStart.Visibility = Visibility.Collapsed;
                                });
                            }
                            GlobalParams.globalRunFlag = true;
                            btnReStartFlag = false;
                            tempPlcCurrentNum = PlcControl.plc.GetPlcValueString(DeviceKey.cutNumKey);
                        }
                    } while (plcCurrentNum.Equals(tempPlcCurrentNum) && !exitFlag);
                    stopDisposeFlag = false;
                    plcCurrentNum = tempPlcCurrentNum;
                    string currentPosition = PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey);
                    // 回到主线程更新 Rows
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        currentIndex.Text = (i + 1).ToString();
                        // 设置进度
                        _model.Rows.Add(new CompDataRow
                        {
                            RowIndex = i + 1,
                            ActualValue = "",
                            AxisPosition = currentPosition
                        });
                       
                    });
                }
                // 完成切割
                // 发送结束信号
                PlcControl.tagControl.cutting.EndFullAutoCut();
                Tools.WaitForValue(DeviceKey.cutStatusKey, 1);
                GlobalParams.globalRunFlag = false;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    testStatus.Text = "已完成测量！";
                    MaterialSnackUtils.MaterialSnack("完成测量！", MaterialSnackUtils.SnackType.SUCCESS, 0);
                    exit();
                });
                if (CommonCheck.GetParamsStatus(DeviceKey.workpieceBlowingStatusKey))
                {
                    Thread.Sleep(3000);
                    await PlcControl.tagControl.wholeDevice.CloseWorkpieceBlowingAsync();
                }
            });
        }

        private void saveData()
        {
            // 删除已有数据
            SqlHelper.Execute("delete FROM auto_align_position");
            foreach (var item in _model.Rows)
            {
                // 记录到数据库
                SqlHelper.Add(new AutoAlignPositionModel
                {
                    RowIndex = item.RowIndex,
                    ActualValue = "",
                    AxisPosition = item.AxisPosition
                });
            }
            sureFlag = 0;
            MaterialSnackUtils.MaterialSnack("保存成功！", MaterialSnackUtils.SnackType.SUCCESS);
        }

        private void exit()
        {
            runFlag = false;
            startFlag = false;
            cutDirection = -1;
            currentCutLine = 0;
            exitStatus = 0;
            exitFlag = false; // 退出标志
            plcCurrentNum = "0";
            yCurrentPosition = 0;
            cutDirectionInput.Text = "-";
            rightPage.btnCutBackward.Visibility = Visibility.Visible;
            rightPage.btnCutFront.Visibility = Visibility.Visible;
            rightPage.btnCutStart.Visibility = Visibility.Visible;
            rightPage.btnCutPause.Visibility = Visibility.Collapsed;
            rightPage.btnCutReStart.Visibility = Visibility.Collapsed;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnCutStart.Visibility = Visibility.Visible;
            sureFlag = 0;
        }
        private void BtnBack_RightClicked(object? sender, bool e)
        {
            GlobalParams.globalRunFlag = true;
            // 退出切割模式
            PlcControl.tagControl.cutting.EnterFullAutoInit(0);
            GlobalParams.globalRunFlag = false;
            mainWindow.NavigateToPage("MainMenu");
        }

        private void OperatePage_onClicked(object? sender, int code)
        {
            switch (code)
            {
                case 2023:
                    // 手动校准 type 
                    mainWindow.mainFrame.Source = new Uri($"View/Pages/F2_ManualOperation/MQManualAlignmentConf.xaml?type=3", UriKind.Relative);
                    break;
                case 7002:
                    // 停止磨刀
                    if (exitStatus == 0)
                    {
                        exitStatus = 1;
                        MaterialSnackUtils.MaterialSnack("再次点击，停止测量。", MaterialSnackUtils.SnackType.WARNING);
                        return;
                    }
                    exitFlag = true;
                    break;
                case 2442:
                    CommonOperate.GetInstance().AutoFocus(2, mainWindow);
                    break;
                case 7004:
                    // 导出excel自动位置补偿 列表的数据
                    ExportData();
                    break;
                case 7005:
                    // 导入excel 到 自动位置补偿 列表
                    ImportData();
                    break;

                default:
                    break;
            }
        }

        private bool SetParams(float feedSpeed, ref float cutDistance)
        {
            // 查询通道信息
            FileTableItemChModel chModels = CurrentUtils.GetFileTableItemChModel();

            // 刀片测高位置
            BladeHeightModel bladeHeightModel = CurrentUtils.GetBladeHeightModel();
            float bladeHeight = float.Parse(bladeHeightModel.BladeHeight);

            // 刀片高度 = 工件厚度 + 膜的厚度 - 切割深度
            // Z轴下降位置 = 测高位置 - 刀片高度 + 补偿深度
            float zEndIndex = bladeHeight - _model.BladeHeight + GlobalParams.cutDepthOffset;

            if (zEndIndex > GlobalParams.cutZ1MaxLocation)
            {
                MaterialSnackUtils.MaterialSnack("Z1轴位置超限！", MaterialSnackUtils.SnackType.ERROR);
                Tools.LogError("Z1轴位置超限！");
                return false;
            }

            if (zEndIndex >= bladeHeight)
            {
                MaterialSnackUtils.MaterialSnack("Z1轴位置超限！", MaterialSnackUtils.SnackType.ERROR);
                return false;
            }
            if (cutDirection == -1)
            {
                MaterialSnackUtils.MaterialSnack("请设置切割方向！", MaterialSnackUtils.SnackType.WARNING);
                return false;
            }
            FileTableItemModel tableItemModel = CurrentUtils.GetFileTableItemModel();
            // 设置/计算切割相关参数
            float avgWorkbenchCh1 = tableItemModel.WorkbenchCh1 / 2;
            // Z轴开始位置 = Z轴下降位置 - 2
            float zStartLocation = zEndIndex - GlobalParams.zCutRaisedHeight;
            // X轴开始位置 theta轴中心点位置 - 
            float xStartLocation = GlobalParams.thetaCenterLocationX - avgWorkbenchCh1 - 10;
            // X轴结束位置
            float xEndLocation = xStartLocation + _model.SquareCh1 + 25f;

            // 设置切割方向 0 前切 1 后切
            if (cutDirection != 0 && cutDirection != 1)
            {
                MaterialSnackUtils.MaterialSnack("切割方向错误！", MaterialSnackUtils.SnackType.WARNING);
                return false;
            }

            // Y轴切割位置调整
            yCurrentPosition += (cutDirection == 0 ? 1 : -1) * (currentCutLine == 0 ? 0 : _model.YIndex);
            if (currentCutLine == 0)
            {
                yCurrentPosition += GlobalParams.cameraOffsetY;
            }
            cutDistance = (float)Math.Round(Math.Abs(xEndLocation - xStartLocation) / 1000, 4);
            float xDiffValue = xEndLocation - xStartLocation;
            float xStopLocation = (xStartLocation + (xDiffValue / 2)) - GlobalParams.cameraToCutXOffset + 15;
            float yStopLocation = yCurrentPosition - GlobalParams.cameraOffsetY;

            if (feedSpeed > 150)
            {
                MaterialSnackUtils.MaterialSnack("切割速度超限！", MaterialSnackUtils.SnackType.ERROR);
                return false;
            }

            // 设置暂停位置
            PlcControl.tagControl.cutting.SetStopLocation(xStopLocation, yStopLocation, GlobalParams.lastFocusZ2Location);

            // 设置切割参数并调用API执行切割 角度需要根据当前的CH来选择
            CutOperateUtils.SetCutParams(feedSpeed, zEndIndex, zStartLocation, xStartLocation, xEndLocation
                , ref yCurrentPosition, "0", "0", _model.SpindleRev.ToString(), cutDirection, _model.YIndex, false);

            return true;
        }

        /// <summary>
        /// 检查异常状态 急停后，要全部重新标定一次
        /// </summary>
        /// <returns></returns>
        private void CheckError()
        {
            Thread thread = new Thread(() =>
            {
                while (runFlag)
                {
                    ObservableCollection<AlarmItem> list = PlcControl.allAlarm;
                    if (list.Count > 0)
                    {
                        exitFlag = true;
                        runFlag = false;
                    }
                    Thread.Sleep(100);
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        //导出导入 列 序号、测量值(μm)
        private void ExportData()
        {
            int count = _model.Rows.Count;
            if (count<=0)
            {
                MaterialSnackUtils.MaterialSnack("空数据不能导出", MaterialSnackUtils.SnackType.ERROR);
                return;
            }
            try
            {
                //调用示例：“Report.xls”：文件名，reportDatas：数据列表List集合，propetryDic：属性名对应的中文名字典
                //获取页面数据
                List<ExportAutoDataVo> reportDatas = new List<ExportAutoDataVo>();
                foreach (var item in _model.Rows)
                {
                    ExportAutoDataVo evo = new ExportAutoDataVo();
                    evo.RowIndex = item.RowIndex;
                    evo.ActualValue = item.ActualValue;
                    evo.AxisPosition = item.AxisPosition;
                    reportDatas.Add(evo);
                }
                if (reportDatas.Count <= 0)
                {
                    MaterialSnackUtils.MaterialSnack("没有数据可导出", MaterialSnackUtils.SnackType.ERROR);
                    return;
                }                
                Dictionary<string, string> propetryDic = new Dictionary<string, string>
                {
                    ["RowIndex"] = "序号-不可修改",
                    ["ActualValue"] = "测量值(μm)",
                };
                string parentPath = System.Environment.CurrentDirectory + "\\excelData\\EsAutoAlignPosition\\";
                DirectoryPathExists(parentPath);
                DateTime now = DateTime.Now;
                // 标准日期和时间格式化字符串
                string format1 = now.ToString("yyyy_MM_dd_HHmmss"); // 24小时制
                string fileName = "erExcel" + format1 + ".xlsx";
                string filePath = parentPath + fileName;
                bool relust = ExcelHelper.WriteExcel(filePath, reportDatas, propetryDic, 1);
                if (relust)
                {
                    MaterialSnackUtils.MaterialSnack("导出成功", MaterialSnackUtils.SnackType.SUCCESS);
                }
                else
                {
                    MaterialSnackUtils.MaterialSnack("导出失败", MaterialSnackUtils.SnackType.ERROR);
                }
            }
            catch (Exception ex)
            {
                Tools.LogError(ex.Message);
            }
        }

        static public void DirectoryPathExists(string path)
        {
            if (Directory.Exists(path))
            {
                return;
            }
            Directory.CreateDirectory(path);
        }

        private static bool isDoing = false;

        //导出导入 列 序号、测量值(μm)
        private async void ImportData()
        {
            int count = _model.Rows.Count;
            if (count <= 0)
            {
                MaterialSnackUtils.MaterialSnack("页面空数据不能导入", MaterialSnackUtils.SnackType.ERROR);
                return;
            }
            if (isDoing)
            {
                MaterialSnackUtils.MaterialSnack("执行导入中请等待上次结果完成！", MaterialSnackUtils.SnackType.ERROR);
                return;
            }
            isDoing = true;
            try
            {
                OpenFileDialog openFile = new OpenFileDialog();
                openFile.Title = "选择Excel文件";
                openFile.Filter = "(excel文件)|*.xls;*.xlsx";
                //openFile.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string parentPath = System.Environment.CurrentDirectory + "\\excelData\\EsAutoAlignPosition\\";
                DirectoryPathExists(parentPath);
                openFile.InitialDirectory = parentPath;
                bool? result = openFile.ShowDialog();
                if (result == true)
                {
                    List<ExportAutoDataVo> reportDatas = ExcelHelper.ReadExcel(openFile.FileName, new ExportAutoDataVo(), 1);
                    if (reportDatas != null && reportDatas.Count > 0)
                    {
                        bool checkSucess = true;
                        string msg = "";
                        if (reportDatas.Count != _model.Rows.Count)
                        {
                            checkSucess = false;
                            msg += "导入数据和页面数据条数不一致，不能导入！";
                        }
                        int xh = 0;
                        for (int i = 0; i < reportDatas.Count; i++)
                        {
                            ExportAutoDataVo item = reportDatas[i];
                            if (item == null)
                            {
                                checkSucess = false;
                                msg += (msg!=""?"；":"")+"数据行不能为空,第" + (i + 1) + "行";
                                break;
                            }
                            xh++;
                            if (item.RowIndex<=0 || item.ActualValue == null || item.ActualValue == "")
                            {
                                checkSucess = false;
                                msg += (msg != "" ? "；" : "") + "数据行不能为空,第" + (i + 1) + "行";
                            }
                            if (xh != item.RowIndex) {
                                checkSucess = false;
                                msg += (msg != "" ? "；" : "") + "数据行序号非连续数字,第" + (i + 1) + "行";
                            }
                            //判断测量值是否是数字
                            bool isNumber = decimal.TryParse(reportDatas[i].ActualValue, out decimal targetVal);
                            if (!isNumber)
                            {
                                checkSucess = false;
                                msg += (msg != "" ? "；" : "") + "数据行测量值(μm)必须是数字,第" + (i + 1) + "行";
                            }
                            //判断位置是否是数字
                            //isNumber = decimal.TryParse(reportDatas[i].AxisPosition, out decimal targetVal2);
                            //if (!isNumber)
                            //{
                            //    checkSucess = false;
                            //    msg += (msg!=""?"；":"")+"数据行位置值(毫米)必须是数字,第" + (i + 1) + "行";
                            //}
                        }

                        if (checkSucess)
                        {                            
                            int ct = 0;
                            for (int i = 0; i < reportDatas.Count; i++)
                            {
                                int RowIndex = reportDatas[i].RowIndex;
                                string ActualValue = reportDatas[i].ActualValue;                              
                                if (string.IsNullOrEmpty(ActualValue))
                                {
                                    ActualValue = "";
                                }
                                 
                                // 格式化文本 4根据页面来
                                //string _Value1 = Tools.FormatDecimalString(ActualValue, 6);
                                //string _Value2 = Tools.FormatDecimalString(AxisPosition, 6);
                                foreach (var item in _model.Rows)
                                {
                                    if (item.RowIndex == RowIndex) {
                                        item.ActualValue = ActualValue;
                                        SqlHelper.Execute("update auto_align_position set actual_value = '"+ item.ActualValue + "' where row_index = " + item.RowIndex);
                                        break;
                                    }                                     
                                }
                                ct++;
                            }
                            //将数据同步到轴补偿表position_compensation 的Y轴-反向 Y轴-反向                            
                            List<PositionCompensationModel> yList = await SqlHelper.TableAsync<PositionCompensationModel>().Where(t => t.AxisType == "Y轴-反向").ToListAsync();
                            if (yList!=null && yList.Count==1) {
                                string[] pos = new string[500];
                                string[] comp = new string[500];
                                PositionCompensationModel y_model = yList[0];
                                foreach (var item in _model.Rows)
                                {
                                    int index = item.RowIndex - 1;
                                    //测量值 um 转换成毫米 加上 位置值  写入axis_compensate；位置值 写入axis_position
                                    String ActualValue = item.ActualValue;//测量值 um
                                    String AxisPosition = item.AxisPosition;
                                    decimal d_ActualValue = decimal.Parse(ActualValue);
                                    decimal d_m = d_ActualValue / 1000m;
                                    decimal d_AxisPosition = decimal.Parse(AxisPosition);
                                    decimal d_AxisCompensate = d_m + d_AxisPosition;
                                    String AxisCompensate = Convert.ToString(d_AxisCompensate);
                                    pos[index] = Convert.ToString(AxisPosition);
                                    comp[index] = AxisCompensate;
                                }
                                for (int i = 0; i < 500; i++) {
                                    string AxisPosition = pos.Length > i && pos[i]!=null && pos[i] != "" ? pos[i] : "0";              // _model.AxisPosition;
                                    string AxisCompensate = comp.Length > i && comp[i] != null && comp[i] != "" ? comp[i] : "0";       // _model.AxisCompensate;
                                    if (AxisPosition == "0") {
                                        pos[i] = "0";
                                    }
                                    if (AxisCompensate == "0")
                                    {
                                        comp[i] = "0";
                                    }
                                }
                                y_model.AxisPosition = string.Join(",", pos);
                                y_model.AxisCompensate = string.Join(",", comp);
                                await SqlHelper.UpdateAsync(y_model);
                            }
                            MaterialSnackUtils.MaterialSnack("导入成功,共" + ct + "条", MaterialSnackUtils.SnackType.SUCCESS);
                        }
                        else
                        {
                            MaterialSnackUtils.MaterialSnack("导入失败," + msg, MaterialSnackUtils.SnackType.ERROR);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Tools.LogError(ex.Message);
            }
            finally
            {
                isDoing = false;
            }
        }


        //导出excel数据类
        public class ExportAutoDataVo
        {
            public int RowIndex { get; set; }//顺序从1开始 1表示第一个
            public string ActualValue { get; set; }//测量值 微米 μm
            public string AxisPosition { get; set; } //位置毫米
        }

    }
}
