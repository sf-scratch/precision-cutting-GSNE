using MathNet.Numerics;
using Microsoft.Win32;
using NPOI.SS.UserModel.Charts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using 精密切割系统.Assets.config.menu;
using 精密切割系统.database;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;
using static 精密切割系统.View.Controls.InputTextBox;

namespace 精密切割系统.View.F7_ElectricSpark
{
    /// <summary>
    /// ESAxisDataConf.xaml 的交互逻辑
    /// </summary>
    public partial class ESAxisDataConf : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;

        private PositionCompensationModel? _model;
        private List<PositionCompensationModel> list;
        //3列数据展示
        ObservableCollection<PositionCompensationModel> Col0_99 { get; set; } = new ObservableCollection<PositionCompensationModel>();
        ObservableCollection<PositionCompensationModel> Col100_199 { get; set; } = new ObservableCollection<PositionCompensationModel>();
        ObservableCollection<PositionCompensationModel> Col200_299 { get; set; } = new ObservableCollection<PositionCompensationModel>();
        ObservableCollection<string> AxisTypeList { get; set; } = [];

        public ESAxisDataConf()
        {
            InitializeComponent();
            pre_listView.ItemsSource = Col0_99;//将数据绑定到列表 
            pre_listView1.ItemsSource = Col100_199;//将数据绑定到列表 
            pre_listView2.ItemsSource = Col200_299;//将数据绑定到列表 
            AxisTypeList.Add("X轴");
            AxisTypeList.Add("Y轴");
            AxisTypeList.Add("Z1轴");
            AxisTypeList.Add("X轴-反向");
            AxisTypeList.Add("Y轴-反向");
            AxisTypeList.Add("Z1轴-反向");
            cbbAxis.ItemsSource = AxisTypeList;

            //cbShowAxis.ItemsSource= AxisTypeList;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            
            mainWindow = Application.Current.MainWindow as MainWindow; 
            rightPage = mainWindow.rightFrame.Content as RightPage;
            operatePage = mainWindow.operateFrame.Content as OperatePage;
            operatePage.UpdateOperate([]);
            //右侧显示
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible; //右侧显示 - 返回按钮显示
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            //rightPage.btnSure.Visibility = Visibility.Visible; //右侧显示 - 确定按钮显示
            //rightPage.btnSure.BackFlag = false;
            //rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked); //确定按钮事件

            //cbbAxis.SelectedIndex = 0;
            //string AxisType = cbbAxis.SelectedItem.ToString();
            //_ = initData(AxisType);
        }
        //private void BtnSure_RightClicked(object? sender, bool e)
        //{

        //}
        private void BtnBack_RightClicked(object? sender, bool e)
        {
            mainWindow.NavigateToPage("MainMenu");
        }
        private async Task initData(string AxisType)
        {
            //string AxisType = cbbAxis.Text;
            list = await SqlHelper.TableAsync<PositionCompensationModel>().ToListAsync();
            //数据不存在，则初始化数据
            if (list != null && list.Count > 0)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].AxisType == AxisType)
                    {
                        _model = list[i];
                        break;
                    }
                }
            }
            else {
                _model = null;
            }            
            initView();
        }

        //数据显示
        private void initView()
        {
            string[] pos = new string[500];
            string[] comp = new string[500];
            string[] gratingRuler = new string[500];
            if (_model != null)
            {
                pos = _model.AxisPosition.Split(',');
                comp = _model.AxisCompensate.Split(',');
                gratingRuler = _model.AxisGratingRuler.Split(',');
            }
            Col0_99.Clear();
            Col100_199.Clear();
            Col200_299.Clear();
            //页面控件
            for (int i = 0; i < 500; i++)
            {
                PositionCompensationModel temp = new PositionCompensationModel();
                temp.Id = i;    
                string AxisPosition = _model != null && pos != null && (pos.Length) > i ? pos[i] : "0.0000";        // _model.AxisPosition;
                string AxisCompensate = _model != null && comp != null && (comp.Length) > i ? comp[i] : "0.0000";    // _model.AxisCompensate;
                string AxisGratingRuler = _model != null && gratingRuler != null && (gratingRuler.Length) > i ? gratingRuler[i] : "0.0000";    //  _model.AxisGratingRuler;
                if (string.IsNullOrEmpty(AxisPosition))
                {
                    AxisPosition = "0";
                }
                if (string.IsNullOrEmpty(AxisCompensate))
                {
                    AxisCompensate = "0";
                }
                if (string.IsNullOrEmpty(AxisGratingRuler))
                {
                    AxisGratingRuler = "0";
                }
                // 格式化文本 3根据页面来
                string _Value1 = Tools.FormatDecimalString(AxisPosition, 6);
                string _Value2 = Tools.FormatDecimalString(AxisCompensate, 6);
                string _Value3 = Tools.FormatDecimalString(AxisGratingRuler, 6);
                temp.AxisPosition = _Value1;
                temp.AxisCompensate = _Value2;
                temp.AxisGratingRuler = _Value3;
                if (i < 167)
                {
                    Col0_99.Add(temp);
                }
                else if (i >= 167 && i < 334)
                {
                    Col100_199.Add(temp);
                }
                else if (i >= 334 && i < 500)
                {
                    //if (Col200_299.Count == 100)
                    //{
                    //    Col200_299[i - 200].AxisPosition = temp.AxisPosition;
                    //    Col200_299[i - 200].AxisCompensate = temp.AxisCompensate;
                    //}
                    //else
                    //{
                    //    Col200_299.Add(temp);
                    //}
                    Col200_299.Add(temp);
                }
                
            }
        }

        /// <summary>
        /// 运动轴选择事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void cbbAxis_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            // 获取选中的项
            string AxisType = comboBox.SelectedItem.ToString();
            // 需要的业务逻辑处理
            if (AxisType != null)
            {
                _ = initData(AxisType);
            }
           
        }

        /// <summary>
        /// 页面3列表格数据合并到新的list
        /// </summary>
        private void getViewList() {
            //
            list = new List<PositionCompensationModel>();
            for (int i = 0; i < Col0_99.Count; i++)
            {
                list.Add(Col0_99[i]);
            }
            for (int i = 0; i < Col100_199.Count; i++)
            {
                list.Add(Col100_199[i]);
            }
            for (int i = 0; i < Col200_299.Count; i++)
            {
                list.Add(Col200_299[i]);
            }
        }
        /// <summary>
        /// 保存按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnSave10_Click(object sender, RoutedEventArgs e)
        {
            string AxisType = cbbAxis.Text;
            if (string.IsNullOrEmpty(AxisType)) {
                MaterialSnackUtils.MaterialSnack("请选择运动轴", MaterialSnackUtils.SnackType.ERROR);
                return;
            }
            var success = this.FormSuccess();
            if (!success)
            {
                MaterialSnackUtils.MaterialSnack("数据异常", MaterialSnackUtils.SnackType.ERROR);
                return;
            }
            if (_model == null) {
                _model = new PositionCompensationModel();
            }
            _model.AxisType = AxisType;
            //位置
            string[] pos = new string[500];
            string[] comp = new string[500];
            string[] gratingRuler = new string[500];

            //获取页面数据
            this.getViewList();
            //
            for (int i = 0; i < list.Count; i++) {
                pos[i] = list[i].AxisPosition;
                comp[i] = list[i].AxisCompensate;
                gratingRuler[i] = list[i].AxisGratingRuler;
            }
            _model.AxisPosition = string.Join(",", pos);
            _model.AxisCompensate = string.Join(",", comp);
            _model.AxisGratingRuler = string.Join(",", gratingRuler);
            if (_model.Id > 0)
            {
                await SqlHelper.UpdateAsync(_model);
            }
            else 
            {
                await SqlHelper.AddAsync(_model);
            }
            CurrentUtils.UpdateParams();
            MaterialSnackUtils.MaterialSnack("操作成功", MaterialSnackUtils.SnackType.SUCCESS);
            //重新加载数据
            //_ = initData();
        }

        /// <summary>
        /// 确认绝对位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            //DeviceApi deviceApi = DeviceApi.GetInstance();
            //deviceApi.StartAbsoluteMotion(comboBox1.Text, "10", textBox1.Text);
        }

        /// <summary>
        /// 获取当前位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            //string location = DeviceApi.GetInstance().GetPlcValueString(comboBox1.Text.Equals("Y轴")
            //    ? DeviceKey.yCurLocationKey : DeviceKey.z1CurLocationKey);
            //textBox2.Text = location;
        }

        //将页面数据导出
        private void btnExport_Click_1(object sender, RoutedEventArgs e)
        { 
            string AxisType = cbbAxis.Text;
            if (string.IsNullOrEmpty(AxisType))
            {
                MaterialSnackUtils.MaterialSnack("请选择运动轴", MaterialSnackUtils.SnackType.ERROR);
                return;
            }
            try
            {
                //调用示例：“Report.xls”：文件名，reportDatas：数据列表List集合，propetryDic：属性名对应的中文名字典
                //获取页面数据
                this.getViewList();
                if (list.Count<=0)
                {
                    MaterialSnackUtils.MaterialSnack("没有数据可导出", MaterialSnackUtils.SnackType.ERROR);
                    return;
                }
                List<ExportDataVo> reportDatas = new List<ExportDataVo>();
                for (int i = 0; i < list.Count; i++)
                {
                    ExportDataVo dataVo = new ExportDataVo();
                    string AxisPosition = list[i].AxisPosition;
                    string AxisCompensate = list[i].AxisCompensate;
                    string AxisGratingRuler = list[i].AxisGratingRuler;
                    dataVo.AxisType = AxisType;
                    dataVo.AxisPosition = AxisPosition;
                    dataVo.AxisCompensate = AxisCompensate;
                    dataVo.AxisGratingRuler = AxisGratingRuler;
                    reportDatas.Add(dataVo);
                }
                Dictionary<string, string> propetryDic = new Dictionary<string, string>
                {
                    ["AxisType"] = "运作轴名称",
                    ["AxisPosition"] = "位置(毫米)",
                    ["AxisCompensate"] = "实际位置激光(毫米)",
                    ["AxisGratingRuler"] = "实际位置光栅尺(毫米)",
                };
                string parentPath = System.Environment.CurrentDirectory + "\\excelData\\EsAxisData\\";
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

        private void btnImport_Click_1(object sender, RoutedEventArgs e)
        {
            if (!(cbbAxis.SelectedItem != null))
            {
                MaterialSnackUtils.MaterialSnack("请选择运动轴！", MaterialSnackUtils.SnackType.ERROR);
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
                string parentPath = System.Environment.CurrentDirectory + "\\excelData\\EsAxisData\\";
                DirectoryPathExists(parentPath);
                openFile.InitialDirectory = parentPath;
                bool? result = openFile.ShowDialog();
                if (result == true)
                {
                    List<ExportDataVo> reportDatas = ExcelHelper.ReadExcel(openFile.FileName, new ExportDataVo(), 1);
                    if (reportDatas != null && reportDatas.Count > 0)
                    {
                        bool checkSucess = true;
                        string msg = "";
                        for (int i = 0; i < reportDatas.Count; i++)
                        {
                            ExportDataVo  item = reportDatas[i];
                            if (item == null)
                            {
                                checkSucess = false;
                                msg += "；数据行不能为空,第" + (i + 1) + "行";
                                break;
                            }
                            if (item.AxisType == null || item.AxisType == "" || item.AxisPosition == null || item.AxisPosition == "" || item.AxisCompensate == null || item.AxisCompensate == "")
                            {
                                checkSucess = false;
                                msg += "；数据行不能为空,第" + (i + 1) + "行";
                            }
                            if (!item.AxisType.Equals(cbbAxis.Text))
                            {
                                //存在不是当前选中轴的数据
                                checkSucess = false;
                                msg += "；数据行运作轴名称)与系统选中值不一致,第" + (i + 1) + "行";
                            }
                            //判断位置是否是数字
                            bool isNumber = decimal.TryParse(reportDatas[i].AxisPosition, out decimal targetVal);
                            if (!isNumber)
                            {
                                checkSucess = false;
                                msg += "；数据行位置(毫米)必须是数字,第" + (i + 1) + "行";
                            }
                            //判断位置是否是数字
                            isNumber = decimal.TryParse(reportDatas[i].AxisCompensate, out decimal targetVal2);
                            if (!isNumber)
                            {
                                checkSucess = false;
                                msg += "；数据行实际位置激光(毫米)必须是数字,第" + (i + 1) + "行";
                            }
                            //判断位置是否是数字
                            isNumber = decimal.TryParse(reportDatas[i].AxisGratingRuler, out decimal targetVal3);
                            if (!isNumber)
                            {
                                checkSucess = false;
                                msg += "；数据行实际位置光栅尺(毫米)必须是数字,第" + (i + 1) + "行";
                            }
                        }
                        if (checkSucess)
                        {
                            int ct = 0;
                            for (int i = 0; i < reportDatas.Count; i++)
                            {
                                string AxisPosition = reportDatas[i].AxisPosition;
                                string AxisCompensate = reportDatas[i].AxisCompensate;
                                string AxisGratingRuler = reportDatas[i].AxisGratingRuler;
                                if (string.IsNullOrEmpty(AxisPosition))
                                {
                                    AxisPosition = "0";
                                }
                                if (string.IsNullOrEmpty(AxisCompensate))
                                {
                                    AxisCompensate = "0";
                                }
                                if (string.IsNullOrEmpty(AxisGratingRuler))
                                {
                                    AxisGratingRuler = "0";
                                }
                                // 格式化文本 4根据页面来
                                string _Value1 = Tools.FormatDecimalString(AxisPosition, 6);
                                string _Value2 = Tools.FormatDecimalString(AxisCompensate, 6);
                                string _Value3 = Tools.FormatDecimalString(AxisGratingRuler, 6);

                                if (i < 167 && Col0_99.Count >= i)
                                {
                                    Col0_99[i].AxisPosition = _Value1;
                                    Col0_99[i].AxisCompensate = _Value2;
                                    Col0_99[i].AxisGratingRuler = _Value3;
                                }
                                else if (i >= 167 && i < 334 && Col100_199.Count >= (i - 167))
                                {
                                    Col100_199[i - 167].AxisPosition = _Value1;
                                    Col100_199[i - 167].AxisCompensate = _Value2;
                                    Col100_199[i - 167].AxisGratingRuler = _Value3;
                                }
                                else if (i >= 334 && i < 500 && Col200_299.Count >= (i - 334))
                                {
                                    Col200_299[i - 334].AxisPosition = _Value1;
                                    Col200_299[i - 334].AxisCompensate = _Value2;
                                    Col200_299[i - 334].AxisGratingRuler = _Value3;
                                }
                                ct++;                                
                            }
                            pre_listView.ItemsSource = new ObservableCollection<PositionCompensationModel>();                            
                            pre_listView1.ItemsSource = new ObservableCollection<PositionCompensationModel>();                            
                            pre_listView2.ItemsSource = new ObservableCollection<PositionCompensationModel>();

                            pre_listView.ItemsSource = Col0_99;//将数据绑定到列表 
                            pre_listView1.ItemsSource = Col100_199;//将数据绑定到列表 
                            pre_listView2.ItemsSource = Col200_299;//将数据绑定到列表 

                            MaterialSnackUtils.MaterialSnack("导入成功,共" + ct + "条", MaterialSnackUtils.SnackType.SUCCESS);
                        }
                        else
                        {
                            MaterialSnackUtils.MaterialSnack("导入失败", MaterialSnackUtils.SnackType.ERROR);
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
                InputTextBox? tb = tbs[i];
                if (tb != null)
                {
                    tb.ValidationCheck();
                }
                bool isError = tb.XIsError;
                if (isError)
                {
                    result = true;
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

        public class ExportDataVo 
        {
            public string AxisType { get; set; }
            public string AxisPosition { get; set; }
            public string AxisCompensate { get; set; }
            public string AxisGratingRuler { get; set; }
        }

    }
}
