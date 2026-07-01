using Emgu.CV.Dnn;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using 精密切割系统.database.db.modle;
using 精密切割系统.Model.bunkering;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.Pages.common;
using 精密切割系统.ViewModel;
using ElectricalDischargeTruingModel = 精密切割系统.database.db.modle.ElectricalDischargeTruingModel;

namespace 精密切割系统.Helpers
{
    internal class CurrentUtils
    {
        // 刀片测高参数
        public static BladeHeightModel bladeHeightModel;

        // 型号参数
        public static FileTableItemModel fileTableItemModel;

        // 型号参数-当前切割面
        public static FileTableItemChModel fileTableItemChModel;

        // 型号参数-面列表
        public static List<FileTableItemChModel> fileTableItemChModels;

        // 电火花修刀
        public static ElectricalDischargeTruingModel electricalDischargeTruingModel;

        // 位置补偿
        public static List<PositionCompensationModel> positionCompensationModels;

        // 预切割
        public static PreCutModel preCutModel;

        // 各轴初始位置
        public static InitialPositionModel initialPositionModel;

        public static SpeedSettingModel speedSettingModel;
        public static OperationParametersModel operationParametersModel;
        public static PositionAlignmentModel positionAlignmentModel;

        public static async void UpdateParams()
        {
            // 加载测高参数
            bladeHeightModel = GetBladeHeightModel();
            // 加载型号参数配置
            fileTableItemModel = GetFileTableItemModel();
            // 加载当前切割面
            fileTableItemChModel = GetFileTableItemChModel();
            // 加载所有切割面
            fileTableItemChModels = GetFileTableItemChModels();
            // 加载电火花信息
            electricalDischargeTruingModel = GetElectricalDischargeTruingModel();
            // 加载位置补偿信息
            positionCompensationModels = GetPositionCompensationModels();
            // 加载预切割配置
            preCutModel = GetPreCutModel();
            initialPositionModel = GetInitialPositionModel();
            speedSettingModel = GetSpeedSettingModel();
            operationParametersModel = await GetOperationParametersModelAsync();
            positionAlignmentModel = GetPositionAlignmentModel();
        }

        public static async Task InitPlcDataAsync()
        {
            // 设置位置校准
            InitPositionAlignment(positionAlignmentModel);
            // 各轴运动速度 点动高速/低速速度  绝对运动速度
            await InitAxisSpeedIndexAsync(operationParametersModel);
            await InitUserDefineDataAsync();
            await AutoCutUtils.SetFunctionalParameters();
        }

        private static async Task InitUserDefineDataAsync()
        {
            UserDefineDataModel userDefineData = await SqlHelper.GetOrCreateEntityAsync(() => new UserDefineDataModel());
            await PlcControl.tagControl.wholeDevice.SetSpindleDirectionAsync(userDefineData.SpindleDirection);
            await PlcControl.tagControl.Xaxis.SetMaxSpeedAsync(userDefineData.MaxSpeedX.ToFloat());
            await PlcControl.tagControl.Yaxis.SetMaxSpeedAsync(userDefineData.MaxSpeedY.ToFloat());
            await PlcControl.tagControl.wholeDevice.SetVacuumBreakingTimeAsync(userDefineData.VacuumBreakingTime.ToInt());
        }

        public static void InitPositionAlignment(PositionAlignmentModel _model)
        {
            //赋值全局变量
            float result = 0;
            //theta轴切割中心点位置 X轴
            bool tryRe = float.TryParse(_model.ThetaCenterLocationX, out result);
            if (tryRe)
            {
                GlobalParams.thetaCenterLocationX = result;
            }
            //theta轴切割中心点位置 Y轴
            tryRe = float.TryParse(_model.ThetaCenterLocationY, out result);
            if (tryRe)
            {
                GlobalParams.thetaCenterLocationY = result;
            }
            //theta轴相机中心点位置 X轴
            tryRe = float.TryParse(_model.ThetaCameraLocationX, out result);
            if (tryRe)
            {
                GlobalParams.thetaCameraLocationX = result;
            }
            //theta轴相机中心点位置 Y轴
            tryRe = float.TryParse(_model.ThetaCameraLocationY, out result);
            if (tryRe)
            {
                GlobalParams.thetaCameraLocationY = result;
            }
            //相机 聚焦初始位置
            double result_d = 0;
            tryRe = double.TryParse(_model.InitPosition, out result_d);
            if (tryRe)
            {
                GlobalParams.initPosition = result_d;
            }
            //相机 高速的倍率
            tryRe = double.TryParse(_model.MultipleNum, out result_d);
            if (tryRe)
            {
                GlobalParams.multipleNum = result_d;
            }
            //X轴切割位置和相机位置的偏移量
            tryRe = float.TryParse(_model.CameraToCutXOffset, out result);
            if (tryRe)
            {
                GlobalParams.cameraToCutXOffset = result;
            }
            //相机焦点和切割位置的偏移量
            tryRe = float.TryParse(_model.CameraToCutYOffset, out result);
            if (tryRe)
            {
                GlobalParams.cameraToCutYOffset = result;
            }
            //切割Z1轴最大安全距离
            tryRe = float.TryParse(_model.CutZ1MaxLocation, out result);
            if (tryRe)
            {
                GlobalParams.cutZ1MaxLocation = result;
            }
            //相机和切割点的X轴的偏移量
            tryRe = float.TryParse(_model.CameraOffsetX, out result);
            if (tryRe)
            {
                GlobalParams.cameraOffsetX = result;
            }
            //相机和切割点的Y轴的偏移量
            tryRe = float.TryParse(_model.CameraOffsetY, out result);
            if (tryRe)
            {
                GlobalParams.cameraOffsetY = result;
            }
            GlobalParams.LightIntensityChannel = _model.LightIntensityChannel;
            GlobalParams.LowLightIntensityChannel = _model.LowLightIntensityChannel;
            GlobalParams.RingLightIntensityChannel = _model.RingLightIntensityChannel;
            GlobalParams.workDiscFocusPosition = _model.WorkDiscFocusPosition;
        }

        /// <summary>
        /// 设置各轴的速度和距离数据
        /// </summary>
        public static async Task InitAxisSpeedIndexAsync(OperationParametersModel operationParametersModel)
        {
            await PlcControl.tagControl.Xaxis.SetJogRelativeSpeedAsync(operationParametersModel.XScanSpeed.ToFloat());
            await PlcControl.tagControl.Yaxis.SetJogRelativeSpeedAsync(operationParametersModel.YScanSpeed.ToFloat());
            await PlcControl.tagControl.Z1axis.SetJogRelativeSpeedAsync(operationParametersModel.ZScanSpeed.ToFloat());
            //await PlcControl.tagControl.Z2axis.SetJogRelativeSpeedAsync(operationParametersModel.Z2ScanSpeed.ToFloat());
            await PlcControl.tagControl.ThetaAxis.SetJogRelativeSpeedAsync(operationParametersModel.RScanSpeed.ToFloat());
        }

        //获取当前配置集合
        public static CurrentConfigurationModel GetCurrentConfiguration()
        {
            var list = SqlHelper.Table<CurrentConfigurationModel>().Where(t => t.Id == 1).ToList();
            CurrentConfigurationModel current = new CurrentConfigurationModel();
            //数据不存在，则初始化数据
            if (list.Count() > 0)
            {
                return list[0];
            }
            else
            {
                SqlHelper.Add(current);
            }
            return current;
        }

        /// <summary>
        /// 修改当前切割面
        /// </summary>
        /// <param name="currentChNo"></param>
        public static async Task UpdateCurrentChAsync(string currentChNo)
        {
            // 设置当前切割面
            CurrentConfigurationModel currentModel = GetCurrentConfiguration();
            currentModel.ChannelNum = currentChNo;
            await SqlHelper.UpdateAsync(currentModel);
        }

        /// <summary>
        /// 获取当前切割面
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentCh()
        {
            CurrentConfigurationModel currentModel = GetCurrentConfiguration();
            return currentModel.ChannelNum;
        }

        //刷新当前配置集合
        public static async Task UpdateCurrentConfiguration(CurrentConfigurationModel model)
        {
            await SqlHelper.UpdateAsync(model);
        }

        public static BladeHeightModel GetBladeHeightModel()
        {
            long id = GetCurrentConfiguration().BladeHeightDataId;
            var listConf = SqlHelper.Table<BladeHeightModel>().Where(t => t.Id == id).ToList();
            BladeHeightModel _model = new BladeHeightModel();
            if (listConf.Count() > 0)
            {
                _model = listConf[0];
            }
            return _model;
        }

        public static async Task UpdateCutMarkWidthAsync(int channelNum, float cutMarkWidth)
        {
            UserDefineDataModel userDefineData = await SqlHelper.GetOrCreateEntityAsync(() => new UserDefineDataModel());
            if (userDefineData != null)
            {
                switch (string.Format(GlobalParams.StringFormatCH, channelNum))
                {
                    case GlobalParams.CH1:
                        userDefineData.BaselineWidthCh1 = cutMarkWidth.ToString(GlobalParams.RoughDecimalStringFormat);
                        break;

                    case GlobalParams.CH2:
                        userDefineData.BaselineWidthCh2 = cutMarkWidth.ToString(GlobalParams.RoughDecimalStringFormat);
                        break;

                    case GlobalParams.CH3:
                        userDefineData.BaselineWidthCh3 = cutMarkWidth.ToString(GlobalParams.RoughDecimalStringFormat);
                        break;

                    case GlobalParams.CH4:
                        userDefineData.BaselineWidthCh4 = cutMarkWidth.ToString(GlobalParams.RoughDecimalStringFormat);
                        break;

                    default:
                        break;
                }
                await SqlHelper.UpdateAsync(userDefineData);
            }
        }

        public static async void UpdateEdgeWidth(int channelNum, float edgeWidth)
        {
            UserDefineDataModel userDefineData = await SqlHelper.GetOrCreateEntityAsync(() => new UserDefineDataModel());
            if (userDefineData != null)
            {
                switch (string.Format(GlobalParams.StringFormatCH, channelNum))
                {
                    case GlobalParams.CH1:
                        userDefineData.EdgeWidthCh1 = edgeWidth.ToString(GlobalParams.RoughDecimalStringFormat);
                        break;

                    case GlobalParams.CH2:
                        userDefineData.EdgeWidthCh2 = edgeWidth.ToString(GlobalParams.RoughDecimalStringFormat);
                        break;

                    case GlobalParams.CH3:
                        userDefineData.EdgeWidthCh3 = edgeWidth.ToString(GlobalParams.RoughDecimalStringFormat);
                        break;

                    case GlobalParams.CH4:
                        userDefineData.EdgeWidthCh4 = edgeWidth.ToString(GlobalParams.RoughDecimalStringFormat);
                        break;

                    default:
                        break;
                }
                SqlHelper.Update(userDefineData);
            }
        }

        public static async void UpdateLightSourceBrightness(int channelNum, int light)
        {
            UserDefineDataModel userDefineData = await SqlHelper.GetOrCreateEntityAsync(() => new UserDefineDataModel());
            switch (string.Format(GlobalParams.StringFormatCH, channelNum))
            {
                case GlobalParams.CH1:
                    userDefineData.LightSourceBrightnessCh1 = light.ToString();
                    break;

                case GlobalParams.CH2:
                    userDefineData.LightSourceBrightnessCh2 = light.ToString();
                    break;

                case GlobalParams.CH3:
                    userDefineData.LightSourceBrightnessCh3 = light.ToString();
                    break;

                case GlobalParams.CH4:
                    userDefineData.LightSourceBrightnessCh4 = light.ToString();
                    break;

                default:
                    break;
            }
            SqlHelper.Update(userDefineData);
        }

        public static async Task<(float cutMarkWidth, float edgeWidth, int lightSourceBrightness)> GetWidthAndLightAsync(string currentChNo)
        {
            UserDefineDataModel userDefineData = await SqlHelper.GetOrCreateEntityAsync(() => new UserDefineDataModel());
            switch (currentChNo)
            {
                case GlobalParams.CH1:
                    if (float.TryParse(userDefineData.BaselineWidthCh1, out float baselineWidthCh1) &&
                        float.TryParse(userDefineData.EdgeWidthCh1, out float edgeWidthCh1) &&
                        int.TryParse(userDefineData.LightSourceBrightnessCh1, out int lightSourceBrightnessCh1))
                    {
                        return (baselineWidthCh1, edgeWidthCh1, lightSourceBrightnessCh1);
                    }
                    break;

                case GlobalParams.CH2:
                    if (float.TryParse(userDefineData.BaselineWidthCh2, out float baselineWidthCh2) &&
                        float.TryParse(userDefineData.EdgeWidthCh2, out float edgeWidthCh2) &&
                        int.TryParse(userDefineData.LightSourceBrightnessCh2, out int lightSourceBrightnessCh2))
                    {
                        return (baselineWidthCh2, edgeWidthCh2, lightSourceBrightnessCh2);
                    }
                    break;

                case GlobalParams.CH3:
                    if (float.TryParse(userDefineData.BaselineWidthCh3, out float baselineWidthCh3) &&
                        float.TryParse(userDefineData.EdgeWidthCh3, out float edgeWidthCh3) &&
                        int.TryParse(userDefineData.LightSourceBrightnessCh3, out int lightSourceBrightnessCh3))
                    {
                        return (baselineWidthCh3, edgeWidthCh3, lightSourceBrightnessCh3);
                    }
                    break;

                case GlobalParams.CH4:
                    if (float.TryParse(userDefineData.BaselineWidthCh4, out float baselineWidthCh4) &&
                        float.TryParse(userDefineData.EdgeWidthCh4, out float edgeWidthCh4) &&
                        int.TryParse(userDefineData.LightSourceBrightnessCh4, out int lightSourceBrightnessCh4))
                    {
                        return (baselineWidthCh4, edgeWidthCh4, lightSourceBrightnessCh4);
                    }
                    break;

                default:
                    break;
            }
            return (0, 0, 0);
        }

        public static FileTableItemChModel GetFileTableItemChModel()
        {
            var currentConfig = GetCurrentConfiguration();
            long deviceDataId = currentConfig.DeviceDataId;
            string channelNum = currentConfig.ChannelNum;

            var query = SqlHelper.Table<FileTableItemChModel>().Where(t => t.ItemId == deviceDataId);

            if (!string.IsNullOrEmpty(channelNum))
            {
                query = query.Where(t => t.ChName == channelNum);
            }
            return query.FirstOrDefault() ?? new FileTableItemChModel();
        }

        public static List<FileTableItemChModel> GetFileTableItemChModels()
        {
            var currentConfig = GetCurrentConfiguration();
            long deviceDataId = currentConfig.DeviceDataId;

            return SqlHelper.Table<FileTableItemChModel>().Where(t => t.ItemId == deviceDataId).ToList();
        }

        public static FileTableItemModel GetFileTableItemModel()
        {
            long id = GetCurrentConfiguration().DeviceDataId;
            var listConf = SqlHelper.Table<FileTableItemModel>().Where(t => t.Id == id).ToList();
            FileTableItemModel _model = new FileTableItemModel();
            if (listConf.Count() > 0)
            {
                _model = listConf[0];
            }
            return _model;
        }

        public static PreCutModel GetPreCutModel()
        {
            string id = GetFileTableItemModel().PrecutProcessNo;
            var listConf = SqlHelper.Table<PreCutModel>().Where(t => t.PrecutNo == id).ToList();
            PreCutModel _model = new PreCutModel();
            if (listConf.Count() > 0)
            {
                _model = listConf[0];
            }
            return _model;
        }

        public static InitialPositionModel GetInitialPositionModel()
        {
            long id = 1;
            var listConf = SqlHelper.Table<InitialPositionModel>().Where(t => t.Id == id).ToList();
            InitialPositionModel _model = null;
            if (listConf.Count() > 0)
            {
                _model = listConf[0];
            }
            return _model;
        }

        public static SpeedSettingModel GetSpeedSettingModel()
        {
            long id = 1;
            var listConf = SqlHelper.Table<SpeedSettingModel>().Where(t => t.Id == id).ToList();
            SpeedSettingModel _model = null;
            if (listConf.Count() > 0)
            {
                _model = listConf[0];
            }
            return _model;
        }

        public static async Task<OperationParametersModel> GetOperationParametersModelAsync()
        {
            return await SqlHelper.GetOrCreateEntityAsync(() => new OperationParametersModel());
        }

        public static PositionAlignmentModel GetPositionAlignmentModel()
        {
            PositionAlignmentModel positionAlignmentMode = new PositionAlignmentModel();
            var list = SqlHelper.Table<PositionAlignmentModel>().Where(t => t.Id == 1).ToList();
            if (list.Count > 0)
            {
                positionAlignmentMode = list[0];
            }
            return positionAlignmentMode;
        }

        public static ElectricalDischargeTruingModel GetElectricalDischargeTruingModel()
        {
            var listConf = SqlHelper.Table<ElectricalDischargeTruingModel>().ToList();
            return listConf.Count > 0 ? listConf[0] : new ElectricalDischargeTruingModel();
        }

        public static List<PositionCompensationModel> GetPositionCompensationModels()
        {
            return SqlHelper.Table<PositionCompensationModel>().ToList();
        }
    }
}