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
        private AutoAlignPositionParamsViewModel _model;
        private bool runFlag = false;
        private bool startFlag = false;
        private int cutDirection = -1;
        private int currentCutLine = 0;
        private int exitStatus = 0;
        private bool exitFlag = false; // 退出标志
        private string plcCurrentNum = "0";
        private float yCurrentPosition = 0;
        private bool btnPauseFlag = false;
        private bool btnReStartFlag = false;

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

        private int sureFlag = 0;

        private void BtnSure_RightClicked(object? sender, bool e)
        {
            if (sureFlag == 0)
            {
                MaterialSnack("再次点击确认会清除已有数据，并保存当前数据，请确认！", SnackType.WARNING, 0);
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
            }
            else
            {
                SqlHelper.Add(tempModel);
            }
        }

        private void BtnCutStart_RightClicked(object? sender, bool e)
        {
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
            MaterialSnack("保存成功！", SnackType.SUCCESS);
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            GlobalParams.globalRunFlag = true;
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
                        MaterialSnack("再次点击，停止测量。", SnackType.WARNING);
                        return;
                    }
                    exitFlag = true;
                    break;

                case 2442:
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

        //导出导入 列 序号、测量值(μm)
        private void ExportData()
        {
            int count = _model.Rows.Count;
            if (count <= 0)
            {
                MaterialSnack("空数据不能导出", SnackType.ERROR);
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
                    MaterialSnack("没有数据可导出", SnackType.ERROR);
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
                    MaterialSnack("导出成功", SnackType.SUCCESS);
                }
                else
                {
                    MaterialSnack("导出失败", SnackType.ERROR);
                }
            }
            catch (Exception ex)
            {
                Tools.LogError(ex.Message);
            }
        }

        public static void DirectoryPathExists(string path)
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
                MaterialSnack("页面空数据不能导入", SnackType.ERROR);
                return;
            }
            if (isDoing)
            {
                MaterialSnack("执行导入中请等待上次结果完成！", SnackType.ERROR);
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
                                msg += (msg != "" ? "；" : "") + "数据行不能为空,第" + (i + 1) + "行";
                                break;
                            }
                            xh++;
                            if (item.RowIndex <= 0 || item.ActualValue == null || item.ActualValue == "")
                            {
                                checkSucess = false;
                                msg += (msg != "" ? "；" : "") + "数据行不能为空,第" + (i + 1) + "行";
                            }
                            if (xh != item.RowIndex)
                            {
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
                                    if (item.RowIndex == RowIndex)
                                    {
                                        item.ActualValue = ActualValue;
                                        SqlHelper.Execute("update auto_align_position set actual_value = '" + item.ActualValue + "' where row_index = " + item.RowIndex);
                                        break;
                                    }
                                }
                                ct++;
                            }
                            //将数据同步到轴补偿表position_compensation 的Y轴-反向 Y轴-反向
                            List<PositionCompensationModel> yList = await SqlHelper.TableAsync<PositionCompensationModel>().Where(t => t.AxisType == "Y轴-反向").ToListAsync();
                            if (yList != null && yList.Count == 1)
                            {
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
                                for (int i = 0; i < 500; i++)
                                {
                                    string AxisPosition = pos.Length > i && pos[i] != null && pos[i] != "" ? pos[i] : "0";              // _model.AxisPosition;
                                    string AxisCompensate = comp.Length > i && comp[i] != null && comp[i] != "" ? comp[i] : "0";       // _model.AxisCompensate;
                                    if (AxisPosition == "0")
                                    {
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
                            MaterialSnack("导入成功,共" + ct + "条", SnackType.SUCCESS);
                        }
                        else
                        {
                            MaterialSnack("导入失败," + msg, SnackType.ERROR);
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