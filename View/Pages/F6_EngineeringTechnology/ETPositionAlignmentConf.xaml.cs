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
using 精密切割系统.database;
using 精密切割系统.database.db.modle;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.cut;
using 精密切割系统.Utils;
using 精密切割系统.View.common;
using 精密切割系统.View.Controls;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;

namespace 精密切割系统.View.F6_EngineeringTechnology
{
    /// <summary>
    /// ETPositionAlignmentConf.xaml 的交互逻辑
    /// </summary>
    public partial class ETPositionAlignmentConf : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;

        //实体类
        private PositionAlignmentModel _model;

        public ETPositionAlignmentConf()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mainWindow = Application.Current.MainWindow as MainWindow;
            rightPage = mainWindow.rightFrame.Content as RightPage;
            operatePage = mainWindow.operateFrame.Content as OperatePage;

            //右侧显示
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible; //右侧显示 - 返回按钮显示
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            rightPage.btnSure.Visibility = Visibility.Visible; //右侧显示 - 确定按钮显示
            //rightPage.btnSure.
            rightPage.btnSure.BackFlag = false;
            rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked); //确定按钮事件

            //调用
            _ = initData();
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            mainWindow.NavigateToPage("MainMenu");
        }

        private void BtnSure_RightClicked(object? sender, bool e)
        {
            //确定执行完成后调用返回按钮事件？
            //页面填写值不满足规则  怎么阻止 确定按钮的返回事件呢？
            var success = this.FormSuccess();
            if (success)
            {
                this.saveData();
                MaterialSnack("操作成功", SnackType.SUCCESS);
                //返回
                //if (mainWindow.mainFrame.CanGoBack)
                //{
                //    mainWindow.mainFrame.GoBack();
                //}
            }
            else
            {
                MaterialSnack("数据异常", SnackType.ERROR);
            }
        }

        //校准参数（6.5）
        private async Task initData()
        {
            var list = await SqlHelper.TableAsync<PositionAlignmentModel>()
                            .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                _model = new PositionAlignmentModel();
                await SqlHelper.AddAsync(_model);
            }
            else
            {
                _model = list[0];
            }
            initView();
        }

        //数据显示
        private void initView()
        {
            inputThetaCenterLocationX.Text = Appsettings.ThetaCenterPoint.X.ToString(GlobalParams.RoughDecimalStringFormat);
            inputThetaCenterLocationY.Text = Appsettings.ThetaCenterPoint.Y.ToString(GlobalParams.RoughDecimalStringFormat);
            inputThetaCameraLocationX.Text = Appsettings.CameraThetaCenterPoint.X.ToString(GlobalParams.RoughDecimalStringFormat);
            inputThetaCameraLocationY.Text = Appsettings.CameraThetaCenterPoint.Y.ToString(GlobalParams.RoughDecimalStringFormat);
            inputCameraToCutXOffset.Text = Appsettings.CameraRelativeBladePosition.X.ToString(GlobalParams.RoughDecimalStringFormat);
            inputCameraToCutYOffset.Text = Appsettings.CameraRelativeBladePosition.Y.ToString(GlobalParams.RoughDecimalStringFormat);
            inputCutZ1MaxLocation.Text = _model.CutZ1MaxLocation;
            MeasurementHeightCompensation.Text = _model.MeasurementHeightCompensation;
            //inputCameraOffsetX.Text = _model.CameraOffsetX;
            //inputCameraOffsetY.Text = _model.CameraOffsetY;
            //inputHighMagToLowMagCameraXOffset.Text = _model.HighMagToLowMagCameraXOffset;
            //inputHighMagToLowMagCameraYOffset.Text = _model.HighMagToLowMagCameraYOffset;
            // inputInitPosition.Text = _model.InitPosition;
            //inputFocusRatio.Text = _model.FocusRatio;
            // inputMultipleNum.Text = _model.MultipleNum;
            //inputLightIntensityChannel.Text = _model.LightIntensityChannel + "";
            //inputLowLightIntensityChannel.Text = _model.LowLightIntensityChannel + "";
            //inputRingLightIntensityChannel.Text = _model.RingLightIntensityChannel + "";
            inputWorkDiscFocusPosition.Text = (Appsettings.FocusWorkpiecesClearZ ?? 0).ToString(GlobalParams.RoughDecimalStringFormat);
            inputFocusClearZPosition.Text = (Appsettings.FocusClearZ ?? 0).ToString(GlobalParams.RoughDecimalStringFormat);

            //如果是空或者小数位数不足-小数初始化为0
            initTbNumber();
        }

        //数据处理
        //返回上一页
        public void backPage()
        {
            //Router.ToMachineMaintenanceMenu();
        }

        //保存数据
        public async void saveData()
        {
            if (_model != null)
            {
                _model.ThetaCenterLocationX = inputThetaCenterLocationX.Text;
                _model.ThetaCameraLocationX = inputThetaCameraLocationX.Text;
                _model.ThetaCameraLocationY = inputThetaCameraLocationY.Text;
                _model.CameraToCutXOffset = inputCameraToCutXOffset.Text;
                _model.CutZ1MaxLocation = inputCutZ1MaxLocation.Text;
                _model.MeasurementHeightCompensation = MeasurementHeightCompensation.Text;
                //_model.CameraOffsetX = inputCameraOffsetX.Text;
                //_model.CameraOffsetY = inputCameraOffsetY.Text;
                //_model.HighMagToLowMagCameraXOffset = inputHighMagToLowMagCameraXOffset.Text;
                //_model.HighMagToLowMagCameraYOffset = inputHighMagToLowMagCameraYOffset.Text;
                //_model.LightIntensityChannel = Tools.GetIntStringValue(inputLightIntensityChannel.Text);
                //_model.LowLightIntensityChannel = Tools.GetIntStringValue(inputLowLightIntensityChannel.Text);
                //_model.RingLightIntensityChannel = Tools.GetIntStringValue(inputRingLightIntensityChannel.Text);
                _model.WorkDiscFocusPosition = Tools.GetFloatStringValue(inputWorkDiscFocusPosition.Text);
                Appsettings.FocusWorkpiecesClearZ = inputWorkDiscFocusPosition.Text.ToFloat();
                Appsettings.FocusClearZ = inputFocusClearZPosition.Text.ToFloat();
                float cameraToCutYOffset = inputCameraToCutYOffset.Text.ToFloat();
                float thetaCameraY = inputThetaCameraLocationY.Text.ToFloat();
                float thetaCenterY = thetaCameraY - cameraToCutYOffset;
                float thetaCameraX = inputThetaCameraLocationX.Text.ToFloat();
                float thetaCenterX = inputThetaCenterLocationX.Text.ToFloat();
                float cameraToCutXOffset = thetaCameraX - thetaCenterX;
                Appsettings.CameraThetaCenterPoint = new DataPoint<float>(thetaCameraX, thetaCameraY);
                Appsettings.CameraRelativeBladePosition = new DataPoint<float>(cameraToCutXOffset, cameraToCutYOffset);
                inputThetaCenterLocationY.Text = thetaCenterY.ToString(GlobalParams.RoughDecimalStringFormat);
                _model.ThetaCenterLocationY = thetaCenterY.ToString(GlobalParams.RoughDecimalStringFormat);
                inputCameraToCutXOffset.Text = cameraToCutXOffset.ToString(GlobalParams.RoughDecimalStringFormat);
                _model.CameraToCutXOffset = cameraToCutXOffset.ToString(GlobalParams.RoughDecimalStringFormat);
                await SqlHelper.UpdateAsync(_model);
                CurrentUtils.InitPositionAlignment(_model);
            }
            else
            {
                Tools.LogError("6.5数据异常！");
            }
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
    }
}