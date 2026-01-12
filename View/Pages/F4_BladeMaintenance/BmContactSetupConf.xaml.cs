using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
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
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;
using static SQLite.SQLite3;


namespace 精密切割系统.View.Pages.F4_BladeMaintenance
{
    /// <summary>
    /// BmContactSetupConf.xaml 的交互逻辑
    /// </summary>
    public partial class BmContactSetupConf : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;
        BladeHeightModel _model = null;

        public BmContactSetupConf()
        {
            InitializeComponent();
            mainWindow = Application.Current.MainWindow as MainWindow;
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            MaterialSnack("进入测高模式成功！", SnackType.WARNING);
            rightPage = mainWindow.rightFrame.Content as RightPage;
            mainWindow.UpdateOperatePage(OperateData.GetContactSetupOperate(), OperateClicked);
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            rightPage.btnStartSetup.Visibility = Visibility.Visible;
            rightPage.btnStartSetup.SetRightClickedHandler(BtnContactSetupSure_RightClicked);
            initData();
            exitFlag = true;
        }
        bool exitFlag = true;
        private void OperateClicked(object? sender, int code)
        {
            switch (code)
            {
                case 4001:
                    // 刀片更换
                    if (CommonCheck.MlignStatusCheck())
                    {
                        exitFlag = false;
                        // 退出测高模式
                        PlcControl.tagControl.bladeMantance.RunBladeSetup(0);
                        PlcControl.tagControl.wholeDevice.SetPanelButtonsStauts(0);
                        Thread.Sleep(300);
                        // 新发送PLC进入模式，当模式进入成功后，跳转页面
                        // 进入刀片更换模式
                        MaterialSnack("进入刀片更换模式中...", SnackType.WARNING);
                        // 进入换刀模式
                        PlcControl.tagControl.bladeMantance.RunBladeReplace(1);
                        GlobalParams.globalRunFlag = true;
                        // 监听状态，如果模式准备完成，则跳转页面
                        Task.Run(() =>
                        {
                            bool flag = Tools.WaitForValue(DeviceKey.bladeMantanceStatusKey, 1);
                            GlobalParams.globalRunFlag = false;
                            if (flag)
                            {
                                mainWindow.NavigateToPage("Pages/F4_BladeMaintenance/BMBladeReplacementConf");
                            }
                            else
                            {
                                MaterialSnack("进入刀片更换失败！", SnackType.WARNING);
                            }
                        });
                    }
                    break;
                case 8004:
                    // 启用 禁用面板按钮
                    bool panelStatus = CommonCheck.GetParamsStatus(DeviceKey.panelStatusKey);
                    PlcControl.tagControl.wholeDevice.SetPanelButtonsStauts(panelStatus ? 0 : 1);
                    OperatePage.isSwitchOpen(!panelStatus, 8004);
                    break;
                case 4470:
                    exitFlag = false;
                    //Thread.Sleep(1);
                    // 测高参数
                    string RePage = "Pages/F4_BladeMaintenance/BmContactSetupConf";
                    string RePageId = _model.Id.ToString();
                    mainWindow.NavigateToPage("Pages/F4_BladeMaintenance/BMSetupDataConf", $"RePage={RePage}&RePageId={RePageId}");
                    break;
                case 4471:
                    // 历史数据清零
                    CurrentConfigurationModel currentConfigurationModel = CurrentUtils.GetCurrentConfiguration();
                    currentConfigurationModel.ClearedCutAllNum = 0;
                    currentConfigurationModel.ClearedCutAllDistance = 0;
                    CurrentUtils.UpdateCurrentConfiguration(currentConfigurationModel);
                    CurrentUtils.UpdateParams();
                    inputTextBox19.Text = "0";
                    inputTextBox18.Text = "0";
                    break;
                case 2422:
                    // 刀片状态信息
                    exitFlag = false;
                    // 测高参数
                    RePage = "Pages/F4_BladeMaintenance/BmContactSetupConf";
                    RePageId = _model.Id.ToString();
                    string paramsData = Uri.EscapeDataString($"RePage={RePage}&RePageId={RePageId}");
                    mainWindow.NavigateToPage("Pages/F4_BladeMaintenance/BladeInfo", $"pageName=Pages/F4_BladeMaintenance/BmContactSetupConf&urlParams={paramsData}");
                    break;
                default:
                    break;
            }
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            if (!GlobalParams.OnlineFlag)
            {
                mainWindow.NavigateToPage("MainMenu");
                return;
            }
            // 退出测高模式
            PlcControl.tagControl.bladeMantance.RunBladeSetup(0);
            PlcControl.tagControl.wholeDevice.SetPanelButtonsStauts(0);
            mainWindow.NavigateToPage("MainMenu");
        }

        //初始化数据
        private async Task initData()
        {
            //测高参数的数据
            List<BladeHeightModel> list = await SqlHelper.TableAsync<BladeHeightModel>()
                    .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                _model = new BladeHeightModel();
                _model.Id = 1;
                //await SqlHelper.AddAsync(_model);
            }
            else
            {
                _model = list[0];

            }
            tbChuckTableSize.Text = _model.ChuckTableSize;
            tbTableType.Text = _model.TableType;
            tbChuckTableShape.Text = _model.ChuckTableShape;
            if ("ROUND".Equals(_model.ChuckTableShape))
            {
                showRound.Visibility = Visibility.Visible;
            }
            else {
                showRound.Visibility = Visibility.Hidden; //隐藏
            }
            int retry = Tools.GetIntStringValue(_model.Retry);
            if (retry > 0)
            {
                secondSetupValuePanel.Visibility = retry > 1 ? Visibility.Visible : Visibility.Collapsed;
                threadSetupValuePanel.Visibility = retry > 2 ? Visibility.Visible : Visibility.Collapsed;
            }
            
            //刀片更换的数据
            ReplaceBladeModel _gh = new ReplaceBladeModel();
            List<ReplaceBladeModel> list_gh = await SqlHelper.TableAsync<ReplaceBladeModel>()
                    .Where(t => t.Id == 1).ToListAsync();
            if (list_gh.Count() == 0)
            {
                _gh = new ReplaceBladeModel();
                _model.Id = 1;
                //await SqlHelper.AddAsync(_model);
            }
            else
            {
                _gh = list_gh[0];
            }
            tbBladeLotID.Text = _gh.BladeLotID;
            tbBladeOutside.Text = _gh.BladeOutside;
            tbSpecName.Text = _gh.SpecName;
            tbHardBladeLength.Text = _gh.HardBladeLength;
            inputTextBox6.Text = _model.BladeHeight;
            // 切割数据
            inputTextBox14.Text = GlobalParams.cutAllDistance + "";
            inputTextBox15.Text = GlobalParams.cutAllNum + "";
            inputTextBox17.Text = GlobalParams.heightCutAllDistance + "";
            inputTextBox16.Text = GlobalParams.heightCutAllNum + "";
            CurrentConfigurationModel currentConfigurationModel = CurrentUtils.GetCurrentConfiguration();
            inputTextBox19.Text = currentConfigurationModel.ClearedCutAllDistance + "";
            inputTextBox18.Text = currentConfigurationModel.ClearedCutAllNum + "";

            initSetupValue();
            //如果是空或者小数位数不足-小数初始化为0
            initTbNumber();
        }

        public void initTbNumber()
        {
            List<InputTextBox> tbs = Tools.GetChildrenOfType<InputTextBox>(this);
            for (int i = 0; i < tbs.Count; i++)
            {
                tbs[i].initNumber();
            }
        }

        public void initSetupValue()
        {
            firstSetupValue.Text = "0.000";
            secondSetupValue.Text = "0.000";
            threadSetupValue.Text = "0.000";
        }
        bool checkRunFlag = false;
        private void BtnContactSetupSure_RightClicked(object? sender, bool e)
        {
            if (checkRunFlag)
            {
                return;
            }
            checkRunFlag = true;
            initSetupValue();
            List<float> setupValueList = new List<float>();
            Task.Run(() =>
            {
                // 发送测高开始信号到PLC
                PlcControl.tagControl.bladeMantance.StartSetup();
                GlobalParams.globalRunFlag = true;
                MaterialSnack("测高中...", SnackType.WARNING, 0);
                // 循环获取测高计数信息，如果等于
                string setupCount = "0";
                while (!_model.Retry.Equals(setupCount))
                {
                    string setupCountPlc = PlcControl.plc.GetPlcValueString(DeviceKey.setupNumberKey);
                    // 如果不相等，则记录值
                    if (!setupCount.Equals(setupCountPlc))
                    {
                        string setupValue = PlcControl.plc.GetPlcValueString(DeviceKey.setupValueKey);
                        setupValueList.Add(Tools.GetFloatStringValue(setupValue));
                        setupCount = setupCountPlc;
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            // 显示结果到页面
                            switch (setupCountPlc)
                            {
                                case "1":
                                    firstSetupValue.Text = setupValue;
                                    break;
                                case "2":
                                    secondSetupValue.Text = setupValue;
                                    break;
                                case "3":
                                    threadSetupValue.Text = setupValue;
                                    break;
                                default:
                                    break;
                            }
                        }));
                        
                    }
                    Thread.Sleep(200);
                }
                GlobalParams.globalRunFlag = false;
                if (setupValueList != null && setupValueList.Count > 0)
                {
                    // 计算3次的平均值，为测高值
                    string avgSetupValue = setupValueList.Average().ToString("F5");
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        MaterialSnack("测高完成！", SnackType.SUCCESS);
                        inputTextBox6.Text = avgSetupValue;
                        _model.BladeHeight = avgSetupValue;
                        SqlHelper.UpdateAsync(_model);
                        GlobalParams.heightCutAllDistance = 0;
                        GlobalParams.heightCutAllNum = 0;
                    }));
                } else
                {
                    MaterialSnack("测高失败！", SnackType.SUCCESS);
                }
                checkRunFlag = false;
            });
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (exitFlag)
            {
                BtnBack_RightClicked(null, false);
            }
            
        }
    }
}
