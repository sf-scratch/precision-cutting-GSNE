using DryIoc;
using Emgu.CV;
using MathNet.Numerics;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using NPOI.HSSF.UserModel;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.Util;
using NPOI.XWPF.Model;
using NPOI.XWPF.UserModel;
using Org.BouncyCastle.Asn1.Crmf;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.Assets.config.menu;
using 精密切割系统.database.db.modle;
using 精密切割系统.Helpers;
using 精密切割系统.Model.cut;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.F2_ManualOperation;
using 精密切割系统.View.Pages.operate;

namespace 精密切割系统.View.Pages.F3_ModelCatalog
{
    /// <summary>
    /// MCDeviceDataConf.xaml 的交互逻辑
    /// </summary>
    public partial class MCDeviceDataConf : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;
        private FileTableItemModel currentModel;//当前配置
        private ObservableCollection<DataBean> ColList { get; set; } = new ObservableCollection<DataBean>();
        private string chName = GlobalParams.CH1;
        private FileTableItemChModel _chModel = null;
        private int id;
        private bool lookState = false;
        private FunctionSelectionModel _functionModel;
        private string cutWay = "高度";

        public MCDeviceDataConf()
        {
            InitializeComponent();
            mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        }

        private async void Label_Loaded(object sender, RoutedEventArgs e)
        {
            rightPage = mainWindow.rightFrame.Content as RightPage;
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BackFrom);
            rightPage.btnSure.SetRightClickedHandler(SureOk);
            rightPage.btnSure.GlobalRunOperateFlag = true;
            rightPage.btnBack.GlobalRunOperateFlag = true;
            operatePage = mainWindow.operateFrame.Content as OperatePage;

            // 查询当前用户配置为深度还是高度
            UserDefineDataModel userDefine = await SqlHelper.GetOrCreateEntityAsync(() => new UserDefineDataModel());
            cutWay = userDefine.ZAxisCutModel;
            if (cutWay.Equals("深度"))
            {
                cutHeightLabel.Content = "切割深度";
            }

            initFunctionSelection();

            _ = initView();
            rightPage.btnSure.Visibility = lookState ? Visibility.Collapsed : Visibility.Visible;
            if (lookState)
            {
                mainWindow.UpdateOperatePage(OperateData.GetMCDeviceDataOperate02(), Operate_Click);
                CutUtils.UpdateGlobalRunFlag(OperateData.GetMCDeviceDataOperate02());
            }
            else
            {
                mainWindow.UpdateOperatePage(OperateData.GetMCDeviceDataOperate(), Operate_Click);
                CutUtils.UpdateGlobalRunFlag(OperateData.GetMCDeviceDataOperate());
            }
            initGridView();
            //更新界面数据
            await UpdateTotalCutNumAsync();
        }

        private async void initFunctionSelection()
        {
            //获取相关数据
            var list = await SqlHelper.TableAsync<FunctionSelectionModel>()
                        .Where(t => t.Id == 1).ToListAsync();
            if (list.Count > 0)
            {
                _functionModel = list[0];
                deptSetLabel.Visibility = _functionModel.DepthStepsFunction ? Visibility.Visible : Visibility.Collapsed;
                loopLabel.Visibility = _functionModel.LoopFunction ? Visibility.Visible : Visibility.Collapsed;

                if (!_functionModel.DepthStepsFunction && _functionModel.LoopFunction)
                {
                    Grid.SetRow(loopLabel, 5);
                }
            }
        }

        private async void Operate_Click(object sender, int code)
        {
            NavigationParameters parameters;
            switch (code)
            {
                case 3001:
                    if (chName.Equals(GlobalParams.CH1))
                    {
                        chName = GlobalParams.CH2;
                    }
                    else if (chName.Equals(GlobalParams.CH2))
                    {
                        chName = GlobalParams.CH3;
                    }
                    else if (chName.Equals(GlobalParams.CH3))
                    {
                        chName = GlobalParams.CH4;
                    }
                    else
                    {
                        chName = GlobalParams.CH1;
                    }

                    //执行数据库数据保存。
                    if (this.FormSuccess())
                    {
                        _ = updataChData();
                        await SaveDataAsync();
                    }
                    else
                    {
                        MaterialSnack("数据异常", SnackType.ERROR);
                    }
                    break;

                case 3002:
                    string PrecutNo = inputPrecutProcessNo.Text;
                    if (string.IsNullOrEmpty(PrecutNo))
                    {
                        break;
                    }
                    List<PreCutModel> precutList = SqlHelper.Table<PreCutModel>()
                        .Where(t => t.PrecutNo == inputPrecutProcessNo.Text).ToList();
                    if (precutList.Count > 0)
                    {
                        string RePage = "Pages/F3_ModelCatalog/MCDeviceDataConf";
                        string RePageId = id.ToString();
                        mainWindow.NavigateToPage("Pages/F5_GeneralEfficiency/F5_1_1_PrecutDataDetails"
                            , $"PrecutNo={precutList[0].PrecutNo}&RePage={RePage}&RePageId={RePageId}&RePageUrl={QueryUtils.GetValueFromQueryParams(this, "url")}");
                    }
                    break;

                case 3003://功能选择
                    string paramsData = Uri.UnescapeDataString($"id={id}&look={lookState}");
                    mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCFunctionSelectionConf", paramsData);
                    break;

                case 3004://导入数据
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Filter = "xls文件 |*.xls";
                    openFileDialog.Title = "选中xls文件";
                    if (openFileDialog.ShowDialog() == true)
                    {
                        readExcle(openFileDialog.FileName);
                    }
                    break;

                case 3005://导入数据
                    exportExcle();
                    break;

                case 5002://校准参数
                    mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCCalibrationParameters", Uri.UnescapeDataString($"id={id}&look={lookState}"));
                    break;

                case 5003:
                    parameters = new() { { "id", id }, { "look", lookState } };
                    ContainerLocator.Container.Resolve<IRegionManager>().RequestNavigate(RegionName.MainRegion, nameof(AutomaticCompensationCutHeight), parameters);
                    break;

                case 5004:
                    parameters = new() { { "id", id }, { "look", lookState } };
                    ContainerLocator.Container.Resolve<IRegionManager>().RequestNavigate(RegionName.MainRegion, nameof(ScratchInspectionParameters), parameters);
                    break;
            }
        }

        private async void exportExcle()
        {
            // 获取桌面路径
            long timeStampMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = System.IO.Path.Combine(desktopPath, $"{currentModel.DeviceDataId}-{timeStampMilliseconds}.xls");
            //执行生成文件
            HSSFWorkbook workbook = new HSSFWorkbook();
            // 获取工作表的默认字体样式
            ICellStyle style = workbook.CreateCellStyle();
            // 设置背景色（例如：红色）
            style.FillForegroundColor = IndexedColors.BrightGreen.Index; // 设置背景色为红色
            style.FillPattern = FillPattern.SolidForeground; // 设置填充模式为纯色
            //创建表相关
            var listCh01 = await SqlHelper.TableAsync<FileTableItemChModel>().Where(t => t.ItemId == currentModel.Id).Where(t => t.ChName == GlobalParams.CH1).ToListAsync();
            if (listCh01.Count > 0)
            {
                updateSheet(workbook.CreateSheet("ch1"), listCh01[0], style);
            }
            var listCh02 = await SqlHelper.TableAsync<FileTableItemChModel>().Where(t => t.ItemId == currentModel.Id).Where(t => t.ChName == GlobalParams.CH2).ToListAsync();
            if (listCh02.Count > 0)
            {
                updateSheet(workbook.CreateSheet("ch2"), listCh02[0], style);
            }
            var listCh03 = await SqlHelper.TableAsync<FileTableItemChModel>().Where(t => t.ItemId == currentModel.Id).Where(t => t.ChName == GlobalParams.CH3).ToListAsync();
            if (listCh03.Count > 0)
            {
                updateSheet(workbook.CreateSheet("ch3"), listCh03[0], style);
            }
            var listCh04 = await SqlHelper.TableAsync<FileTableItemChModel>().Where(t => t.ItemId == currentModel.Id).Where(t => t.ChName == GlobalParams.CH4).ToListAsync();
            if (listCh04.Count > 0)
            {
                updateSheet(workbook.CreateSheet("ch4"), listCh04[0], style);
            }

            using (FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(stream);
                MaterialSnack($"导出成功：{filePath}", SnackType.SUCCESS);
            }
        }

        //填充表格
        private void updateSheet(ISheet sheetCh1, FileTableItemChModel model, ICellStyle style)
        {
            IRow row1 = sheetCh1.CreateRow(0);
            NPOI.SS.UserModel.ICell cell01 = row1.CreateCell(0);
            cell01.CellStyle = style; // 将样式应用到单元格
            cell01.SetCellValue("θ Deg.");
            NPOI.SS.UserModel.ICell cell02 = row1.CreateCell(1);
            cell02.CellStyle = style; // 将样式应用到单元格
            cell02.SetCellValue("切割方式");
            NPOI.SS.UserModel.ICell cell03 = row1.CreateCell(2);
            cell03.CellStyle = style; // 将样式应用到单元格
            cell03.SetCellValue("切割方向");
            NPOI.SS.UserModel.ICell cell04 = row1.CreateCell(3);
            cell04.CellStyle = style; // 将样式应用到单元格
            cell04.SetCellValue("切割刀数");
            NPOI.SS.UserModel.ICell cell05 = row1.CreateCell(4);
            cell05.CellStyle = style; // 将样式应用到单元格
            cell05.SetCellValue("校准");
            NPOI.SS.UserModel.ICell cell06 = row1.CreateCell(5);
            cell06.CellStyle = style; // 将样式应用到单元格
            cell06.SetCellValue("刀片角度");
            NPOI.SS.UserModel.ICell cell07 = row1.CreateCell(6);
            cell07.CellStyle = style; // 将样式应用到单元格
            cell07.SetCellValue("X偏移");
            IRow row2 = sheetCh1.CreateRow(1);
            row2.CreateCell(0).SetCellValue(model.ThetaDeg);
            row2.CreateCell(1).SetCellValue(model.CutMode);
            row2.CreateCell(2).SetCellValue(model.CutDir);
            row2.CreateCell(3).SetCellValue(model.CutLine);
            row2.CreateCell(4).SetCellValue(model.OffsetY);
            row2.CreateCell(5).SetCellValue(model.BladeAngle);
            row2.CreateCell(6).SetCellValue(model.OffsetX);
            //下部数据
            IRow row3 = sheetCh1.CreateRow(2);
            NPOI.SS.UserModel.ICell cell3 = row3.CreateCell(0);
            cell3.CellStyle = style; // 将样式应用到单元格
            cell3.SetCellValue("类型");
            for (int i = 0; i < 100; i++)
            {
                NPOI.SS.UserModel.ICell cell4 = row3.CreateCell(i + 1);
                cell4.CellStyle = style; // 将样式应用到单元格
                cell4.SetCellValue($"SQE{i + 1}");
            }
            //刀片高度
            IRow row4 = sheetCh1.CreateRow(3);
            row4.CreateCell(0).SetCellValue("刀片高度");
            string[] BladeHeightStr = model.BladeHeight.Split(",");
            for (int i = 0; i < BladeHeightStr.Length; i++)
            {
                row4.CreateCell(i + 1).SetCellValue(BladeHeightStr[i]);
            }

            //进刀速度
            IRow row5 = sheetCh1.CreateRow(4);
            row5.CreateCell(0).SetCellValue("进刀速度");
            string[] FeedSpeedStr = model.FeedSpeed.Split(",");
            for (int i = 0; i < FeedSpeedStr.Length; i++)
            {
                row5.CreateCell(i + 1).SetCellValue(FeedSpeedStr[i]);
            }
            //Y轴移动量
            IRow row6 = sheetCh1.CreateRow(5);
            row6.CreateCell(0).SetCellValue("Y轴移动量");
            string[] YIndexStr = model.YIndex.Split(",");
            for (int i = 0; i < YIndexStr.Length; i++)
            {
                row6.CreateCell(i + 1).SetCellValue(YIndexStr[i]);
            }

            //刀数
            IRow row7 = sheetCh1.CreateRow(6);
            row7.CreateCell(0).SetCellValue("刀数");
            string[] RepeatTimesStr = model.RepeatTimes.Split(",");
            for (int i = 0; i < RepeatTimesStr.Length; i++)
            {
                row7.CreateCell(i + 1).SetCellValue(RepeatTimesStr[i]);
            }
            if (_functionModel.DepthStepsFunction)
            {
                //刀片深度
                IRow row8 = sheetCh1.CreateRow(7);
                row8.CreateCell(0).SetCellValue("刀片深度");
                string[] DepthStepsStr = model.DepthSteps.Split(",");
                for (int i = 0; i < DepthStepsStr.Length; i++)
                {
                    row8.CreateCell(i + 1).SetCellValue(DepthStepsStr[i]);
                }
            }

            if (_functionModel.LoopFunction)
            {
                //循环
                IRow row9 = sheetCh1.CreateRow(_functionModel.DepthStepsFunction ? 8 : 7);
                row9.CreateCell(0).SetCellValue("循环");
                string[] LoopStr = model.Loop.Split(",");
                for (int i = 0; i < LoopStr.Length; i++)
                {
                    row9.CreateCell(i + 1).SetCellValue(LoopStr[i]);
                }
            }
        }

        //解析xls
        private async Task readExcle(string path)
        {
            try
            {
                using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    HSSFWorkbook workbook = new HSSFWorkbook(file);
                    //定义四组数据
                    for (int i = 0; i < 4; i++)
                    {
                        //获取对应Model
                        FileTableItemChModel chModelData = await getChModel(i);
                        //基础信息
                        ISheet sheet = workbook.GetSheetAt(i);
                        if (sheet == null) return;
                        Dictionary<string, string[]> mapDictionary = new Dictionary<string, string[]>();
                        for (int row = 0; row <= sheet.LastRowNum; row++)
                        {
                            IRow currentRow = sheet.GetRow(row);
                            if (row == 1)//基础信息
                            {
                                if (currentRow.LastCellNum > 0)
                                {
                                    for (int cellRow = 0; cellRow <= currentRow.LastCellNum - 1; cellRow++)
                                    {
                                        NPOI.SS.UserModel.ICell icell = currentRow.GetCell(cellRow);
                                        addBaseChModel(chModelData, cellRow, icell.ToString());
                                    }
                                }
                            }
                            if (row > 2)//具体数据
                            {
                                if (currentRow.LastCellNum > 0)
                                {
                                    string[] itemStrs = new string[currentRow.LastCellNum - 1];
                                    List<string> titleStrs = new List<string>();
                                    for (int cellRow = 0; cellRow <= currentRow.LastCellNum - 1; cellRow++)
                                    {
                                        if (cellRow != 0)
                                        {
                                            NPOI.SS.UserModel.ICell icell = currentRow.GetCell(cellRow);
                                            itemStrs[cellRow - 1] = icell.ToString();
                                        }
                                    }
                                    mapDictionary.Add(currentRow.GetCell(0).ToString(), itemStrs);

                                    //addChModel(chModelData, row, string.Join(",", itemStrs));
                                }
                            }
                        }
                        addChModel(chModelData, mapDictionary);
                        await SqlHelper.UpdateAsync(chModelData);
                    }
                    MaterialSnack("导入成功！", SnackType.SUCCESS);
                    //完成后刷新数据
                    _ = updataChData();
                }
            }
            catch (Exception)
            {
                MaterialSnack("文件异常或被占用，请重新选择文件！", SnackType.ERROR);
            }
        }

        private async Task<FileTableItemChModel> getChModel(int sheetRow)
        {
            if (sheetRow == 0)
            {
                var listCh01 = await SqlHelper.TableAsync<FileTableItemChModel>().Where(t => t.ItemId == currentModel.Id).Where(t => t.ChName == "Ch 1").ToListAsync();
                if (listCh01.Count > 0)
                {
                    return listCh01[0];
                }
            }
            if (sheetRow == 1)
            {
                var listCh02 = await SqlHelper.TableAsync<FileTableItemChModel>().Where(t => t.ItemId == currentModel.Id).Where(t => t.ChName == "Ch 2").ToListAsync();
                if (listCh02.Count > 0)
                {
                    return listCh02[0];
                }
            }
            if (sheetRow == 2)
            {
                var listCh03 = await SqlHelper.TableAsync<FileTableItemChModel>().Where(t => t.ItemId == currentModel.Id).Where(t => t.ChName == "Ch 3").ToListAsync();
                if (listCh03.Count > 0)
                {
                    return listCh03[0];
                }
            }
            if (sheetRow == 3)
            {
                var listCh04 = await SqlHelper.TableAsync<FileTableItemChModel>().Where(t => t.ItemId == currentModel.Id).Where(t => t.ChName == "Ch 4").ToListAsync();
                if (listCh04.Count > 0)
                {
                    return listCh04[0];
                }
            }
            return null;
        }

        private void addBaseChModel(FileTableItemChModel chModel, int cell, string data)
        {
            if (cell == 0)
            {
                chModel.ThetaDeg = data;
            }
            if (cell == 1)
            {
                chModel.CutMode = data;
            }
            if (cell == 2)
            {
                chModel.CutDir = data;
            }
            if (cell == 3)
            {
                chModel.CutLine = data;
            }
            if (cell == 4)
            {
                chModel.OffsetY = data;
            }
            if (cell == 5)
            {
                chModel.BladeAngle = data;
            }
            if (cell == 6)
            {
                chModel.OffsetX = data;
            }
        }

        private async void addChModel(FileTableItemChModel chModel, Dictionary<string, string[]> mapDictionary)
        {
            foreach (KeyValuePair<string, string[]> map in mapDictionary)
            {
                if (map.Key.Equals("刀片高度"))
                {
                    chModel.BladeHeight = string.Join(",", map.Value);
                }
                if (map.Key.Equals("进刀速度"))
                {
                    chModel.FeedSpeed = string.Join(",", map.Value);
                }
                if (map.Key.Equals("Y轴移动量"))
                {
                    chModel.YIndex = string.Join(",", map.Value);
                }
                if (map.Key.Equals("刀数"))
                {
                    chModel.RepeatTimes = string.Join(",", map.Value);
                }
                if (map.Key.Equals("刀片深度") && _functionModel.DepthStepsFunction)
                {
                    chModel.DepthSteps = string.Join(",", map.Value);
                }
                if (map.Key.Equals("循环") && _functionModel.LoopFunction)
                {
                    chModel.Loop = string.Join(",", map.Value);
                }
            }
        }

        //private async void addChModel(FileTableItemChModel chModel, int row,string data)
        //{
        //    if (row == 3)
        //    {
        //        chModel.BladeHeight = data;
        //    }
        //    if (row == 4)
        //    {
        //        chModel.FeedSpeed = data;
        //    }
        //    if (row == 5)
        //    {
        //        chModel.YIndex = data;
        //    }
        //    if (row == 6)
        //    {
        //        chModel.RepeatTimes = data;
        //    }
        //    if (row == 7)
        //    {
        //        chModel.DepthSteps = data;
        //    }
        //    if (row == 8)
        //    {
        //        chModel.Loop = data;
        //    }
        //    //await SqlHelper.UpdateAsync(_chModel);
        //}

        private void BackFrom(object sender, bool v)
        {
            string url = QueryUtils.GetValueFromQueryParams(this, "url");
            if (string.IsNullOrEmpty(url))
            {
                mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCDeviceDataListConf");
            }
            else
            {
                mainWindow.NavigateToPage(url);
            }
        }

        private async void SureOk(object sender, bool code)
        {
            //执行数据库数据保存。
            if (this.FormSuccess())
            {
                await SaveDataAsync();
                CurrentConfigurationModel currentConfigurationModel = CurrentUtils.GetCurrentConfiguration();
                if (currentConfigurationModel.DeviceDataId != currentModel.Id)
                {
                    var operationParams = await CurrentUtils.GetOperationParametersModelAsync();
                    if (operationParams.IsUpdateParamClearManualCompensation)
                    {
                        SemiAutoCutService.Instance.DepthCompensationValue = 0;
                    }
                }
                currentConfigurationModel.DeviceDataId = currentModel.Id;
                currentConfigurationModel.ChannelNum = GlobalParams.CH1;
                CurrentUtils.UpdateCurrentConfiguration(currentConfigurationModel);
                CurrentUtils.UpdateParams();
                MaterialSnack("保存成功！", SnackType.SUCCESS);
            }
            else
            {
                MaterialSnack("数据异常", SnackType.ERROR);
            }
        }

        private async Task initView()
        {
            Dictionary<string, string> ss = QueryUtils.getQuery(this);
            id = Tools.GetIntStringValue(ss["id"]);
            if (ss.ContainsKey("look"))
            {
                lookState = bool.Parse(ss["look"]);
            }
            //查询数据
            var tableList = await SqlHelper.TableAsync<FileTableItemModel>()
                   .Where(t => t.Id == id)
                   .ToListAsync();
            if (tableList.Count > 0)
            {
                currentModel = tableList[0];
                //view数据
                inputDeviceDataId.Text = currentModel.DeviceDataId;
                labDeviceDataNo.Text = currentModel.DeviceDataNo;
                inputSpindleRev.Text = currentModel.SpindleRev.ToString();
                inputPrecutProcessNo.Text = currentModel.PrecutProcessNo;
                inputCuttingChSeq.Text = currentModel.CuttingChSeq;
                inputRound.Text = currentModel.Round;
                inputWorkThickness.Text = currentModel.WorkThickness;
                inputTapeThickness.Text = currentModel.TapeThickness;
                inputSquareCh1.Text = currentModel.SquareCh1;
                inputSquareCh2.Text = currentModel.SquareCh2;
                if (currentModel.WorkShape == 1)//原型
                {
                    rabRound.IsChecked = true;
                    inputSquareCh1.IsEnabled = false;
                    inputSquareCh2.IsEnabled = false;
                    inputSquareCh1.Background = new SolidColorBrush(Color.FromRgb(240, 242, 245));
                    inputSquareCh2.Background = new SolidColorBrush(Color.FromRgb(240, 242, 245));
                    inputRound.IsEnabled = true;
                }
                else
                {
                    rabSquare.IsChecked = true;
                    inputSquareCh1.IsEnabled = true;
                    inputSquareCh2.IsEnabled = true;
                    inputRound.IsEnabled = false;
                    inputRound.Background = new SolidColorBrush(Color.FromRgb(240, 242, 245));
                }
                //查询目录
                var listFile = await SqlHelper.TableAsync<FileTableModel>().Where(t => t.Id == currentModel.DirectoryId).ToListAsync();
                if (listFile.Count > 0)
                {
                    labDirectoryName.Text = listFile[0].Name;
                }
                //刷新附表单数据
                _ = updataChData();
                onlyLook();
            }
        }

        //只限查看
        private void onlyLook()
        {
            if (lookState)
            {
                labDirectoryName.IsEnabled = false;
                labDeviceDataNo.IsEnabled = false;
                inputDeviceDataId.IsEnabled = false;
                inputSpindleRev.IsEnabled = false;
                inputPrecutProcessNo.IsEnabled = false;
                inputCuttingChSeq.IsEnabled = false;
                rabRound.IsEnabled = false;
                inputRound.IsEnabled = false;
                rabSquare.IsEnabled = false;
                inputSquareCh1.IsEnabled = false;
                inputSquareCh2.IsEnabled = false;
                inputWorkThickness.IsEnabled = false;
                inputTapeThickness.IsEnabled = false;
                inputThetaDeg.IsEnabled = false;
                ComBoxCutMode.IsEnabled = false;
                ComBoxCutDir.IsEnabled = false;
                ComBoxCutMethod.IsEnabled = false;
                relativeCutPosition.IsEnabled = false;
                inputCutLine.IsEnabled = false;
                absoluteCutPosition.IsEnabled = false;
                inputBladeAngle.IsEnabled = false;
                inputOffsetX.IsEnabled = false;
                alignX.IsEnabled = false;
                alignY.IsEnabled = false;
            }
        }

        //刷新附表单数据
        private async Task updataChData()
        {
            //获取配置Ch目录库
            var listCh = await SqlHelper.TableAsync<FileTableItemChModel>().Where(t => t.ItemId == currentModel.Id).Where(t => t.ChName == chName).ToListAsync();
            if (listCh != null && listCh.Count() > 0)
            {
                _chModel = listCh[0];
                //子项左侧数据
                labChName.Content = _chModel.ChName;
                labListName.Content = _chModel.ChName;
                inputThetaDeg.Text = _chModel.ThetaDeg;
                ComBoxCutMode.Text = _chModel.CutMode;
                ComBoxCutDir.Text = _chModel.CutDir;
                ComBoxCutMethod.Text = _chModel.ComBoxCutMethod;
                relativeCutPosition.Text = _chModel.RelativeCutPosition;
                inputCutLine.Text = _chModel.CutLine;
                absoluteCutPosition.Text = _chModel.AbsoluteCutPosition;
                inputBladeAngle.Text = _chModel.BladeAngle;
                //inputMoncutF.Text = _chModel.MoncutF;
                //inputMoncutR.Text = _chModel.MoncutR;
                inputOffsetX.Text = _chModel.OffsetX;
                alignX.Text = _chModel.AlignX;
                alignY.Text = _chModel.AlignY;
                ChangeCutMethod(_chModel.ComBoxCutMethod.Equals("相对") ? "绝对" : "相对");
                updateOperateLabel();
                //列表数据
                UpdateGridDataAsync();
            }
        }

        private void updateOperateLabel()
        {
            _ = Dispatcher.BeginInvoke(new Action(async () =>
            {
                TextBlock operateText = Tools.GetChildObject<TextBlock>(operatePage, "operateTxt3001");
                for (int i = 0; i < 30 && operateText == null; i++)
                {
                    operateText = Tools.GetChildObject<TextBlock>(operatePage, "operateTxt3001");
                    await Task.Delay(100);
                }
                if (operateText == null) return;
                string defnName = chName;
                if (chName.Equals(GlobalParams.CH1))
                {
                    defnName = GlobalParams.CH2;
                }
                else if (chName.Equals(GlobalParams.CH2))
                {
                    defnName = GlobalParams.CH3;
                }
                else if (chName.Equals(GlobalParams.CH3))
                {
                    defnName = GlobalParams.CH4;
                }
                else
                {
                    defnName = GlobalParams.CH1;
                }
                operateText.Text = defnName;
                GlobalParams.currentOperateBeanList[0].Title = defnName;
            }));
        }

        //初始化右侧元素
        private void UpdateGridDataAsync()
        {
            ColList.Clear();
            //绑定数据
            List<ChBean> list = new List<ChBean>();
            list.Add(new ChBean() { type = 1, data = _chModel.BladeHeight });
            list.Add(new ChBean() { type = 2, data = _chModel.FeedSpeed });
            list.Add(new ChBean() { type = 3, data = _chModel.YIndex });
            list.Add(new ChBean() { type = 4, data = _chModel.RepeatTimes });
            if (_functionModel != null && _functionModel.DepthStepsFunction)
            {
                list.Add(new ChBean() { type = 5, data = _chModel.DepthSteps });
            }
            if (_functionModel != null && _functionModel.LoopFunction)
            {
                list.Add(new ChBean() { type = 6, data = _chModel.Loop });
            }
            /*string[] list = { _chModel.BladeHeight, _chModel.FeedSpeed,
                _chModel.YIndex, _chModel.RepeatTimes,
                _chModel.DepthSteps, _chModel.Loop,
                _chModel.ZDownSpeed };*/
            //string[] list = { _chModel.BladeHeight };
            for (int i = 0; i < list.Count; i++)
            {
                string[] strs = list[i].data.Split(",");
                DataBean bean = new DataBean();
                bean.type = list[i].type;
                if (list[i].type == 2 || list[i].type == 4)
                {
                    bean.intputType = "Numeral";
                }
                else if (list[i].type == 6)
                {
                    bean.intputType = "Default";
                }
                else
                {
                    bean.intputType = "Decimal";
                }

                if (list[i].type == 2)
                {
                    bean.XPrecision = 4;
                }
                else
                {
                    bean.XPrecision = 5;
                }
                for (int n = 0; n < strs.Length; n++)
                {
                    // 格式化文本
                    string formattedValue = strs[n];
                    // 如果cutWay为深度模式，且是刀片高度，则要换算值
                    if (cutWay.Equals("深度") && bean.type == 1 && Tools.GetDoubleStringValue(formattedValue) != 0)
                    {
                        // 把高度换算为深度 = 工件1.5 + 膜0.7 - 刀片高度1.415 = 0.155
                        double tempValue = Tools.GetDoubleStringValue(currentModel.WorkThickness)
                            + Tools.GetDoubleStringValue(currentModel.TapeThickness) - Tools.GetDoubleStringValue(formattedValue);
                        formattedValue = tempValue.ToString("F3");
                        // Debug.WriteLine(tempValue.ToString("F3"));
                    }
                    if (bean.intputType.Equals("Numeral") || bean.intputType.Equals("Decimal"))
                    {
                        formattedValue = Tools.FormatDecimalString(formattedValue, bean.intputType.Equals("Decimal") ? bean.XPrecision : 0);
                    }
                    SetPropertyValue(bean, "Column" + n, formattedValue);
                }
                ColList.Add(bean);
            }

            lvDataView.ItemsSource = ColList;

            //如果是空或者小数位数不足-小数初始化为0
            initTbNumber();
        }

        //初始化布局
        private void initGridView()
        {
            lvDataView.ItemsSource = ColList;
            //绑定数据结束
            for (int i = 0; i < 100; i++)
            {
                GridViewColumn column = new GridViewColumn();
                column.Width = 150;
                //头部布局
                DataTemplate headerTemplate = new DataTemplate();
                FrameworkElementFactory factory = new FrameworkElementFactory(typeof(Label));
                factory.SetValue(Label.FontWeightProperty, FontWeights.Bold);
                factory.SetValue(Label.VerticalContentAlignmentProperty, System.Windows.VerticalAlignment.Center);
                factory.SetValue(Label.HorizontalContentAlignmentProperty, System.Windows.HorizontalAlignment.Center);
                // 创建一个SolidColorBrush，并设置TextBlock的背景颜色
                Color color = (Color)ColorConverter.ConvertFromString("#CAEAFE");
                factory.SetValue(Label.BackgroundProperty, new SolidColorBrush(color));
                factory.SetValue(Label.ContentProperty, "SEQ" + (i + 1));
                headerTemplate.VisualTree = factory;
                column.HeaderTemplate = headerTemplate;
                //内容布局
                FrameworkElementFactory inputFactory = new FrameworkElementFactory(typeof(InputTextBox));
                inputFactory.SetValue(InputTextBox.WidthProperty, 135.0);
                inputFactory.SetValue(InputTextBox.HeightProperty, 33.0);
                inputFactory.SetValue(InputTextBox.MarginProperty, new Thickness(0, 0, 0, 0));
                inputFactory.SetValue(InputTextBox.PaddingProperty, new Thickness(0));
                inputFactory.SetBinding(InputTextBox.TextProperty, new Binding("Column" + i));
                inputFactory.SetBinding(InputTextBox.InputTypeProperty, new Binding("intputType"));
                inputFactory.SetBinding(InputTextBox.XPrecisionProperty, new Binding("XPrecision"));
                if (lookState)
                {
                    inputFactory.SetValue(InputTextBox.IsEnabledProperty, false);
                }
                //Color color01 = (Color)ColorConverter.ConvertFromString("#CAEAFE");
                //inputFactory.SetValue(Label.BackgroundProperty, new SolidColorBrush(color01));
                DataTemplate contentTemplate = new DataTemplate();
                contentTemplate.VisualTree = inputFactory;
                column.CellTemplate = contentTemplate;
                //END
                dataGridView.Columns.Add(column);
            }
        }

        public void SetPropertyValue(object obj, string propertyName, object value)
        {
            PropertyInfo propertyInfo = obj.GetType().GetProperty(propertyName);
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(obj, value);
            }
        }

        public class DataBean
        {
            public int type { get; set; }
            public string intputType { get; set; }
            public int XPrecision { get; set; } = 3;
            public string Column0 { get; set; }
            public string Column1 { get; set; }
            public string Column2 { get; set; }
            public string Column3 { get; set; }
            public string Column4 { get; set; }
            public string Column5 { get; set; }
            public string Column6 { get; set; }
            public string Column7 { get; set; }
            public string Column8 { get; set; }
            public string Column9 { get; set; }
            public string Column10 { get; set; }
            public string Column11 { get; set; }
            public string Column12 { get; set; }
            public string Column13 { get; set; }
            public string Column14 { get; set; }
            public string Column15 { get; set; }
            public string Column16 { get; set; }
            public string Column17 { get; set; }
            public string Column18 { get; set; }
            public string Column19 { get; set; }
            public string Column20 { get; set; }
            public string Column21 { get; set; }
            public string Column22 { get; set; }
            public string Column23 { get; set; }
            public string Column24 { get; set; }
            public string Column25 { get; set; }
            public string Column26 { get; set; }
            public string Column27 { get; set; }
            public string Column28 { get; set; }
            public string Column29 { get; set; }
            public string Column30 { get; set; }
            public string Column31 { get; set; }
            public string Column32 { get; set; }
            public string Column33 { get; set; }
            public string Column34 { get; set; }
            public string Column35 { get; set; }
            public string Column36 { get; set; }
            public string Column37 { get; set; }
            public string Column38 { get; set; }
            public string Column39 { get; set; }
            public string Column40 { get; set; }
            public string Column41 { get; set; }
            public string Column42 { get; set; }
            public string Column43 { get; set; }
            public string Column44 { get; set; }
            public string Column45 { get; set; }
            public string Column46 { get; set; }
            public string Column47 { get; set; }
            public string Column48 { get; set; }
            public string Column49 { get; set; }
            public string Column50 { get; set; }
            public string Column51 { get; set; }
            public string Column52 { get; set; }
            public string Column53 { get; set; }
            public string Column54 { get; set; }
            public string Column55 { get; set; }
            public string Column56 { get; set; }
            public string Column57 { get; set; }
            public string Column58 { get; set; }
            public string Column59 { get; set; }
            public string Column60 { get; set; }
            public string Column61 { get; set; }
            public string Column62 { get; set; }
            public string Column63 { get; set; }
            public string Column64 { get; set; }
            public string Column65 { get; set; }
            public string Column66 { get; set; }
            public string Column67 { get; set; }
            public string Column68 { get; set; }
            public string Column69 { get; set; }
            public string Column70 { get; set; }
            public string Column71 { get; set; }
            public string Column72 { get; set; }
            public string Column73 { get; set; }
            public string Column74 { get; set; }
            public string Column75 { get; set; }
            public string Column76 { get; set; }
            public string Column77 { get; set; }
            public string Column78 { get; set; }
            public string Column79 { get; set; }
            public string Column80 { get; set; }
            public string Column81 { get; set; }
            public string Column82 { get; set; }
            public string Column83 { get; set; }
            public string Column84 { get; set; }
            public string Column85 { get; set; }
            public string Column86 { get; set; }
            public string Column87 { get; set; }
            public string Column88 { get; set; }
            public string Column89 { get; set; }
            public string Column90 { get; set; }
            public string Column91 { get; set; }
            public string Column92 { get; set; }
            public string Column93 { get; set; }
            public string Column94 { get; set; }
            public string Column95 { get; set; }
            public string Column96 { get; set; }
            public string Column97 { get; set; }
            public string Column98 { get; set; }
            public string Column99 { get; set; }
        }

        private void RabSquare_Checked(object sender, RoutedEventArgs e)
        {
            inputSquareCh1.IsEnabled = true;
            inputSquareCh2.IsEnabled = true;
            inputRound.IsEnabled = false;
            inputRound.Background = new SolidColorBrush(Color.FromRgb(240, 242, 245));
            inputSquareCh1.Background = null;
            inputSquareCh2.Background = null;
            if (currentModel != null)
            {
                currentModel.WorkShape = 2;
            }
        }

        private void RabRound_Checked(object sender, RoutedEventArgs e)
        {
            inputSquareCh1.IsEnabled = false;
            inputSquareCh2.IsEnabled = false;
            inputSquareCh1.Background = new SolidColorBrush(Color.FromRgb(240, 242, 245));
            inputSquareCh2.Background = new SolidColorBrush(Color.FromRgb(240, 242, 245));
            inputRound.Background = null;
            inputRound.IsEnabled = true;
            if (currentModel != null)
            {
                currentModel.WorkShape = 1;
            }
        }

        private async Task UpdateFocusClearZAsync()
        {
            float originCombineThickness = currentModel.WorkThickness.ToFloat() + currentModel.TapeThickness.ToFloat();
            float newCombineThickness = inputWorkThickness.Text.ToFloat() + inputTapeThickness.Text.ToFloat();
            if (originCombineThickness != newCombineThickness && Appsettings.FocusClearZ is not null)
            {
                Appsettings.FocusClearZ += originCombineThickness - newCombineThickness;
            }
        }

        //保存数据
        private async Task SaveDataAsync()
        {
            updateOperateLabel();
            await UpdateFocusClearZAsync();
            //tableItem信息
            currentModel.DeviceDataId = inputDeviceDataId.Text;
            currentModel.DeviceDataNo = labDeviceDataNo.Text;
            currentModel.SpindleRev = int.Parse(inputSpindleRev.Text.ToString());
            currentModel.PrecutProcessNo = inputPrecutProcessNo.Text;
            currentModel.CuttingChSeq = inputCuttingChSeq.Text;
            currentModel.Round = inputRound.Text;
            currentModel.WorkThickness = inputWorkThickness.Text;
            currentModel.TapeThickness = inputTapeThickness.Text;
            currentModel.SquareCh1 = inputSquareCh1.Text;
            currentModel.SquareCh2 = inputSquareCh2.Text;

            //currentModel.WorkShape
            //ChItem信息
            _chModel.ThetaDeg = inputThetaDeg.Text;
            _chModel.CutMode = ComBoxCutMode.Text;
            _chModel.CutDir = ComBoxCutDir.Text;
            _chModel.ComBoxCutMethod = ComBoxCutMethod.Text;
            _chModel.RelativeCutPosition = relativeCutPosition.Text;
            _chModel.CutLine = inputCutLine.Text;
            _chModel.AbsoluteCutPosition = absoluteCutPosition.Text;
            _chModel.BladeAngle = inputBladeAngle.Text;
            //_chModel.MoncutR = inputMoncutR.Text;
            _chModel.OffsetX = inputOffsetX.Text;
            _chModel.AlignX = alignX.Text;
            _chModel.AlignY = alignY.Text;
            //ChItem中表单信息
            for (int i = 0; i < ColList.Count; i++)
            {
                DataBean bean = ColList[i];
                Type type = bean.GetType();
                PropertyInfo[] properties = type.GetProperties();
                List<string> strs = new List<string>();
                for (int k = 0; k < properties.Length; k++)
                {
                    if (properties[k].GetValue(bean) != null && properties[k].Name.StartsWith("Column"))
                    {
                        string value = properties[k].GetValue(bean).ToString();
                        if (bean.type == 1)
                        {
                            if (cutWay.Equals("深度") && bean.type == 1 && Tools.GetDoubleStringValue(value) != 0)
                            {
                                // 把深度换算为高度 = 工件1.5 + 膜0.7 - 切割深度0.155 = 1.415
                                double tempValue = Tools.GetDoubleStringValue(currentModel.WorkThickness)
                                    + Tools.GetDoubleStringValue(currentModel.TapeThickness) - Tools.GetDoubleStringValue(value);
                                value = tempValue.ToString("F3");
                                Debug.WriteLine(tempValue.ToString("F3"));
                            }
                        }
                        strs.Add(value);
                    }
                    else
                    {
                        continue;
                    }
                }
                string[] itemStrs = strs.ToArray();
                if (bean.type == 1)
                {
                    _chModel.BladeHeight = string.Join(",", itemStrs);
                }
                else if (bean.type == 2)
                {
                    _chModel.FeedSpeed = string.Join(",", itemStrs);
                }
                else if (bean.type == 3)
                {
                    _chModel.YIndex = string.Join(",", itemStrs);
                }
                else if (bean.type == 4)
                {
                    _chModel.RepeatTimes = string.Join(",", itemStrs);
                }
                else if (bean.type == 5)
                {
                    _chModel.DepthSteps = string.Join(",", itemStrs);
                }
                else if (bean.type == 6)
                {
                    _chModel.Loop = string.Join(",", itemStrs);
                }
                else
                {
                    _chModel.ZDownSpeed = string.Join(",", itemStrs);
                }
                await SqlHelper.UpdateAsync(currentModel);
                await SqlHelper.UpdateAsync(_chModel);
                //更新界面数据
                await UpdateTotalCutNumAsync();
            }
        }

        private async Task UpdateTotalCutNumAsync()
        {
            CommonResult<List<CutStep>> cutStepResult = await AutoCutUtils.GenerateCutStepListAsync();
            if (!cutStepResult.IsSuccess || cutStepResult.Data is null)
            {
                MaterialSnack(cutStepResult.Message, SnackType.ERROR);
                return;
            }
            totalCutNum.Text = cutStepResult.Data.Count.ToString();
        }

        private void lvDataView_Loaded(object sender, RoutedEventArgs e)
        {
            //ListViewItem listViewItem = (ListViewItem)lvDataView.ItemContainerGenerator.ContainerFromIndex(0);
            //Debug.WriteLine(listViewItem);
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

        private class ChBean()
        {
            public int type { get; set; } = 0;
            public string data { get; set; } = "0";
        }

        private void ComBoxCutMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChangeCutMethod(ComBoxCutMethod.Text);
        }

        private void ChangeCutMethod(string value)
        {
            if (value.Equals("相对"))
            {
                // 选择了绝对
                absoluteCutPosition.Visibility = Visibility.Visible;
                absoluteCutPositionLabel.Visibility = Visibility.Visible;
                absoluteCutPositionUnit.Visibility = Visibility.Visible;

                relativeCutPosition.Visibility = Visibility.Collapsed;
                relativeCutPositionLabel.Visibility = Visibility.Collapsed;
                relativeCutPositionUnit.Visibility = Visibility.Collapsed;
            }
            else
            {
                // 选择了相对
                relativeCutPosition.Visibility = Visibility.Visible;
                relativeCutPositionLabel.Visibility = Visibility.Visible;
                relativeCutPositionUnit.Visibility = Visibility.Visible;

                absoluteCutPosition.Visibility = Visibility.Collapsed;
                absoluteCutPositionLabel.Visibility = Visibility.Collapsed;
                absoluteCutPositionUnit.Visibility = Visibility.Collapsed;
            }
        }
    }
}