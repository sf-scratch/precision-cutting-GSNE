using Newtonsoft.Json;
using NPOI.OpenXml4Net.OPC.Internal.Unmarshallers;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.DTOs;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.HttpClients;
using 精密切割系统.Model.plc;
using 精密切割系统.Model.sqlite;
using 精密切割系统.Utils;
using 精密切割系统.ViewModel;
using System.Runtime.InteropServices;
using OpenCvSharp.WpfExtensions;
using SciCamera.Net;
using 精密切割系统.View.Pages.common;
using System.IO;
using System.Windows;
using System.Drawing.Imaging;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using Emgu.CV.Reg;
using 精密切割系统.PubSubEvent;
using 精密切割系统.Model.common;
using System.Windows.Interop;
using Prism.Events;
using 精密切割系统.View.Dialogs;
using 精密切割系统.Extensions;
using static SQLite.SQLite3;
using Point = OpenCvSharp.Point;
using Emgu.CV.Bioinspired;
using System.Windows.Shapes;

namespace 精密切割系统.Model.cut
{
    public class AutoCutUtils
    {
        private static CancellationToken RunAutoCancellationToken { get; set; } = new CancellationToken();

        /// <summary>
        /// 自动切割
        /// </summary>
        public static async Task RunAuto()
        {
            LunguSksjDTO? lunguInfo = await HttpUtils.GetLunguSksjAsync(CameraUtils.GetLunguId());

            // 开始测高
            //float? afterHeightMeasurementZ = await ProcessMeasureHeightAsync();
            float? afterHeightMeasurementZ = 0;
            if (afterHeightMeasurementZ == null)
            {
                MaterialSnackUtils.MaterialSnack("测高失败！", MaterialSnackUtils.SnackType.ERROR);
                return;
            }
            // 切割校准
            //float cutCalibratTheta = await CalibratCutAsync();
            // 磨刀校准
            //float sharpenCalibratTheta = await CalibratSharpenAsync();
            // 获取型号目录ID
            long deviceDataId = CurrentUtils.GetCurrentConfiguration().DeviceDataId;
            // 获取切割序列
            Queue<CutStep>? cutSteps = await GetAllCutSequenceAsync(deviceDataId, afterHeightMeasurementZ.Value);
            if (cutSteps == null)
            {
                MaterialSnackUtils.MaterialSnack("获取切割序列失败！", MaterialSnackUtils.SnackType.ERROR);
                return;
            }
            int bmSharpParamId = 1;
            // 磨刀板尺寸
            DataRectangleF sharpenRect = GlobalParams.SharpenRect;
            // 获取磨刀序列
            Queue<SharpenStep>? sharpenSteps = await GetAllSharpenStepSequenceAsync(bmSharpParamId, afterHeightMeasurementZ.Value, sharpenRect);
            if (sharpenSteps == null)
            {
                MaterialSnackUtils.MaterialSnack("获取磨刀序列失败！", MaterialSnackUtils.SnackType.ERROR);
                return;
            }
            // theta轴中心点位置
            DataPoint<float> thetaCenterPoint = GlobalParams.ThetaCenterPoint;
            //工件半径
            float workpieceRadius = GlobalParams.WorkpieceRadius;
            //工件中心点到theta轴中心点距离
            float centerDistance = GlobalParams.CenterDistance;
            // 切割多少次开始磨刀
            int cutCount = GlobalParams.CutThenSharpenStepNum;
            // 磨刀次数
            int sharpCount = GlobalParams.SharpenStepNum;
            //Y轴切割起始位置
            float cutStartY = 0;
            //Y轴磨刀起始位置
            float sharpStartY = 0;
            ProcessResult sharpenResult;
            ProcessResult cutResult;
            while (!RunAutoCancellationToken.IsCancellationRequested)
            {
                //开始磨刀
                sharpenResult = await ProcessSharpenAsync(thetaCenterPoint, sharpenRect, sharpStartY, sharpenSteps, sharpCount);
                if (!sharpenResult.IsSuccess)
                {
                    throw new Exception("磨刀异常！");
                }
                //记录当前Y位置，作为下次磨刀的起始位置
                sharpStartY = sharpenResult.CurrentY;
                //磨刀序列是否已经使用完
                if (sharpenSteps.Count == 0)
                {
                    //提示换磨刀板
                    Tools.LogDebug("提示换磨刀板");
                    //获取磨刀序列
                    sharpenSteps = await GetAllSharpenStepSequenceAsync(bmSharpParamId, afterHeightMeasurementZ.Value, sharpenRect);
                    if (sharpenSteps == null)
                    {
                        MaterialSnackUtils.MaterialSnack("获取磨刀序列失败！", MaterialSnackUtils.SnackType.ERROR);
                        return;
                    }
                    //指定磨刀数是否磨刀完毕
                    if (sharpenResult.RemainTimes != 0)
                    {
                        //开始磨刀
                        sharpenResult = await ProcessSharpenAsync(thetaCenterPoint, sharpenRect, sharpStartY, sharpenSteps, sharpenResult.RemainTimes);
                        if (!sharpenResult.IsSuccess)
                        {
                            throw new Exception("磨刀异常！");
                        }
                        //记录当前Y位置，作为下次磨刀的起始位置
                        sharpStartY = sharpenResult.CurrentY;
                    }
                }

                //开始切割
                cutResult = await ProcessSemicircleCutSequenceAsync(thetaCenterPoint, workpieceRadius, centerDistance, cutStartY, cutSteps, cutCount);
                if (!cutResult.IsSuccess)
                {
                    throw new Exception("切割异常！");
                }
                //记录当前Y位置，作为下次切割的起始位置
                cutStartY = cutResult.CurrentY;
                //切割序列是否已经切割完毕
                if (cutSteps.Count == 0)
                {
                    //提示换工件
                    Tools.LogDebug("提示换工件");
                    //获取切割序列
                    cutSteps = await GetAllCutSequenceAsync(deviceDataId, afterHeightMeasurementZ.Value);
                    if (cutSteps == null)
                    {
                        MaterialSnackUtils.MaterialSnack("获取切割序列失败！", MaterialSnackUtils.SnackType.ERROR);
                        return;
                    }
                    //指定切割数是否切割完毕
                    if (cutResult.RemainTimes != 0)
                    {
                        //开始切割
                        cutResult = await ProcessSemicircleCutSequenceAsync(thetaCenterPoint, workpieceRadius, centerDistance, cutStartY, cutSteps, cutResult.RemainTimes);
                        if (!cutResult.IsSuccess)
                        {
                            throw new Exception("切割异常！");
                        }
                        //记录当前Y位置，作为下次切割的起始位置
                        cutStartY = cutResult.CurrentY;
                    }
                }
            }

        }

        /// <summary>
        /// 换刀片
        /// </summary>
        /// <returns></returns>
        public static async Task ReplaceBladeAsync(IEventAggregator? eventAggregator = null)
        {
            MaterialSnackUtils.MaterialSnack("轴运动中！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
            Task taskZ1 = PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0);
            Task taskZ2 = PlcControl.tagControl.Z2axis.StartAbsoluteAsync(0);
            Task taskX = PlcControl.tagControl.Xaxis.StartAbsoluteAsync(0);
            Task taskY = PlcControl.tagControl.Yaxis.StartAbsoluteAsync(0);
            Task taskTheta = PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(0);
            await Task.WhenAll(taskX, taskY, taskZ1, taskZ2, taskTheta);
            MaterialSnackUtils.MaterialSnack("请更换刀片！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
        }

        /// <summary>
        /// 换磨刀板
        /// </summary>
        /// <returns></returns>
        public static async Task ReplaceSharpeningBoardAsync(IEventAggregator? eventAggregator = null)
        {
            MaterialSnackUtils.MaterialSnack("轴运动中！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
            Task taskZ1 = PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0);
            Task taskZ2 = PlcControl.tagControl.Z2axis.StartAbsoluteAsync(0);
            Task taskX = PlcControl.tagControl.Xaxis.StartAbsoluteAsync(0);
            Task taskY = PlcControl.tagControl.Yaxis.StartAbsoluteAsync(0);
            Task taskTheta = PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(0);
            await Task.WhenAll(taskX, taskY, taskZ1, taskZ2, taskTheta);
            //清空记录
            Appsettings.SharpenY = null;
            Appsettings.SharpenThetaDegQueue = null;
            MaterialSnackUtils.MaterialSnack("请更换磨刀板！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
        }

        /// <summary>
        /// 换硅片
        /// </summary>
        /// <returns></returns>
        public static async Task ReplaceWaferAsync(IEventAggregator? eventAggregator = null)
        {
            MaterialSnackUtils.MaterialSnack("轴运动中！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
            Task taskZ1 = PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0);
            Task taskZ2 = PlcControl.tagControl.Z2axis.StartAbsoluteAsync(0);
            Task taskX = PlcControl.tagControl.Xaxis.StartAbsoluteAsync(0);
            Task taskY = PlcControl.tagControl.Yaxis.StartAbsoluteAsync(0);
            Task taskTheta = PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(0);
            await Task.WhenAll(taskX, taskY, taskZ1, taskZ2, taskTheta);
            //清空记录
            Appsettings.CutY = null;
            Appsettings.CutThetaDegQueue = null;
            MaterialSnackUtils.MaterialSnack("请更换硅片！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
        }

        /// <summary>
        /// 执行测高
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<float?> ProcessMeasureHeightAsync(HeightMeasurementMode mode, CancellationToken token, IDialogService dialogService, IEventAggregator? eventAggregator = null)
        {
            //测高前移动到初始位置，手动吹水
            //await PlcControl.tagControl.cutting.RunMotionAsync(0, 0, token);
            //await PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0, default, token);
            //await PlcControl.tagControl.wholeDevice.OpenBuzzerAsync();
            //await dialogService.ShowDialogWindowAsync(nameof(ConfirmDialog), new DialogParameters() { { "ButtonContent", "请手动完成吹水操作" } }, r => { }, nameof(ConfirmDialogWindow));
            //await PlcControl.tagControl.wholeDevice.CloseBuzzerAsync();
            InitialPositionModel? initPos = await GetInitialPositionAsync();
            if (initPos is null) return null;
            switch (mode)
            {
                //接触测高
                case HeightMeasurementMode.Contact:
                    int? thetaDeg = Appsettings.ContactHeightMeasurementThetaDeg;
                    if (thetaDeg == null)
                    {
                        MaterialSnackUtils.MaterialSnack("接触测高配置文件异常！", MaterialSnackUtils.SnackType.ERROR, 0, eventAggregator);
                        return null;
                    }
                    Appsettings.ContactHeightMeasurementThetaDeg = thetaDeg.Value + 1;
                    await PlcControl.tagControl.bladeMantance.SetBladeSetuInitPositionAsync(initPos.BladeSetupInitX, initPos.BladeSetupInitY, thetaDeg.Value);
                    await PlcControl.tagControl.bladeMantance.StartContactHeightMeasurement();
                    break;
                //非接触测高
                case HeightMeasurementMode.NoContact:
                    await PlcControl.tagControl.bladeMantance.SetBladeSetuInitPositionAsync(initPos.NoContactBladeSetupInitX, initPos.NoContactBladeSetupInitY);
                    await PlcControl.tagControl.bladeMantance.StartNoContactHeightMeasurement();
                    break;
                default:
                    break;
            }
            while (true)
            {
                if (mode is HeightMeasurementMode.NoContact)
                {
                    //测高前移动到初始位置，主轴旋转，开始吹水吹气
                    await PlcControl.tagControl.cutting.RunMotionAsync(0, 50, token);
                    //主轴有旋转速度，则不需要手动触发
                    if (await PlcControl.tagControl.wholeDevice.GetSpindleSpeedAsync() == 0)
                    {
                        eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("主轴开始旋转"));
                        await PlcControl.tagControl.wholeDevice.TriggerSpindleManuallyRunAsync();
                    }
                    //eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("光纤传感器开始吹水"));
                    //await PlcControl.tagControl.bladeMantance.OpenOpticalFiberSensorBlowingWaterAsync(2);
                    eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("光纤传感器开始吹气"));
                    await PlcControl.tagControl.bladeMantance.OpenOpticalFiberSensorBlowingAsync(5);
                }

                //等待测高准备完成信号
                await PlcControl.tagControl.bladeMantance.WaitReadyToMeasureHeightAsync(token);
                //进入测高模式
                await PlcControl.tagControl.bladeMantance.StartBladeSetupAsync();
                eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("开始测高！"));
                BladeHeightModel bladeHeightModel;
                //测高参数的数据
                List<BladeHeightModel> list = await SqlHelper.TableAsync<BladeHeightModel>()
                        .Where(t => t.Id == 1).ToListAsync();
                //数据不存在，则初始化数据
                if (list == null || list.Count == 0)
                {
                    MaterialSnackUtils.MaterialSnack("获取测高参数失败！", MaterialSnackUtils.SnackType.ERROR, 0, eventAggregator);
                    return null;
                }
                bladeHeightModel = list[0];
                if (!int.TryParse(bladeHeightModel.Retry, out int retry))
                {
                    MaterialSnackUtils.MaterialSnack("测高参数异常！", MaterialSnackUtils.SnackType.ERROR, 0, eventAggregator);
                    return null;
                }
                // 发送测高开始信号到PLC
                await PlcControl.tagControl.bladeMantance.StartSetupAsync();
                List<float> setupValueList = new List<float>();
                int? measureHeightTimes = await PlcControl.tagControl.bladeMantance.GetHeightMeasurementSetupNumber();
                if (measureHeightTimes == null)
                {
                    MaterialSnackUtils.MaterialSnack("测高次数获取失败！", MaterialSnackUtils.SnackType.ERROR, 0, eventAggregator);
                    return null;
                }
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
                while (measureHeightTimes.Value < retry && await timer.WaitForNextTickAsync(token))
                {
                    int? curMeasureHeightTimes = await PlcControl.tagControl.bladeMantance.GetHeightMeasurementSetupNumber();
                    if (curMeasureHeightTimes == null)
                    {
                        MaterialSnackUtils.MaterialSnack("测高次数获取失败！", MaterialSnackUtils.SnackType.ERROR, 0, eventAggregator);
                        return null;
                    }
                    // 如果不相等，则记录值
                    if (curMeasureHeightTimes.Value != measureHeightTimes.Value)
                    {
                        float? setupValue = await PlcControl.tagControl.bladeMantance.GetHeightMeasurementSetupValue();
                        if (setupValue == null)
                        {
                            MaterialSnackUtils.MaterialSnack("测高值获取失败！", MaterialSnackUtils.SnackType.ERROR, 0, eventAggregator);
                            return null;
                        }
                        eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"第{curMeasureHeightTimes.Value}次测高：{setupValue.Value}"));
                        setupValueList.Add(setupValue.Value);
                        measureHeightTimes = curMeasureHeightTimes;
                    }
                }
                if (setupValueList.Count == 0)
                {
                    return null;
                }
                //等待完成测高信号
                await PlcControl.tagControl.bladeMantance.WaitHeightMeasurementCompletedAsync(token);
                eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"测高平均值：{setupValueList.Average()}"));
                float maxDeviation = setupValueList.Max() - setupValueList.Min();
                eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"测高最大偏差：{maxDeviation}"));
                if (maxDeviation >= 0.01)
                {
                    eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"测高偏差过大，重新测高"));
                    continue;
                }
                // 计算3次的平均值，为测高值
                return setupValueList.Average();
            }
        }

        private static async Task<InitialPositionModel?> GetInitialPositionAsync()
        {
            InitialPositionModel? initialPosition = null;
            var list = await SqlHelper.TableAsync<InitialPositionModel>().Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() != 0)
            {
                initialPosition = list[0];
            }
            return initialPosition;
        }

        

        /// <summary>
        /// 切割校准
        /// </summary>
        /// <returns>theta轴角度</returns>
        public static async Task<float> CalibratCutAsync(DataPoint<float> workpieceCenterPoint, float workpieceRadius, CancellationToken token)
        {
            return 0;
            await WaitAllAxisStopAsync(token);
            await PlcControl.tagControl.Yaxis.StartAbsoluteAsync(workpieceCenterPoint.Y + 10, default, token);
            await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(workpieceCenterPoint.X - workpieceRadius, default, token);
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token);
            CancellationToken linkedToken = linkedCts.Token;
            Task slowSpeedMoveTask = PlcControl.tagControl.Xaxis.StartAbsoluteAsync(workpieceCenterPoint.X + workpieceRadius, 7f, linkedToken);
            //Task slowSpeedMoveTask = PlcControl.tagControl.Xaxis.StartRelativeAsync(1f, workpieceRadius * 2, 0, linkedToken);
            Task grabTimerTask = Task.Run(async () =>
            {
                try
                {
                    using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
                    while (await timer.WaitForNextTickAsync(linkedToken))
                    {
                        WriteableBitmap? localBitmap = GrabWriteableBitmap();
                        if (localBitmap != null)
                        {
                            Mat mat = localBitmap.ToMat();
                            Cv2.ImWrite($"C:\\Users\\17632\\Desktop\\image\\{DateTime.Now.Ticks}_CalibratCutAsync.jpg", mat);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    //正常取消任务
                }
            }, linkedToken);
            await Task.WhenAny(slowSpeedMoveTask, grabTimerTask).ContinueWith(a => cts.Cancel(), linkedToken);

            //for (float distance = 0; distance <= workpieceRadius * 2; distance += 1f)
            //{
            //    await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(1f, workpieceCenterPoint.X + distance, token);
            //    WriteableBitmap? localBitmap = GrabWriteableBitmap();
            //    if (localBitmap != null)
            //    {
            //        Mat mat = localBitmap.ToMat();
            //        Cv2.ImWrite($"C:\\Users\\17632\\Desktop\\image\\{DateTime.Now.Ticks}_CalibratCutAsync.jpg", mat);
            //    }
            //}
            return 0;
        }

        /// <summary>
        /// 磨刀校准
        /// </summary>
        /// <returns>theta轴角度</returns>
        public static async Task<float> CalibratSharpenAsync(DataRectangleF sharpenRect, CancellationToken token)
        {
            return 0;
            await WaitAllAxisStopAsync(token);
            await PlcControl.tagControl.Yaxis.StartAbsoluteAsync(sharpenRect.Bottom - 10, default, token);
            await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(sharpenRect.X, default, token);
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token);
            CancellationToken linkedToken = linkedCts.Token;
            Task slowSpeedMoveTask = PlcControl.tagControl.Xaxis.StartAbsoluteAsync(sharpenRect.X + sharpenRect.Width, 7, token);
            Task grabTimerTask = Task.Run(async () =>
            {
                try
                {
                    using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
                    while (await timer.WaitForNextTickAsync(linkedToken))
                    {
                        WriteableBitmap? localBitmap = GrabWriteableBitmap();
                        if (localBitmap != null)
                        {
                            Mat mat = localBitmap.ToMat();
                            Cv2.ImWrite($"C:\\Users\\17632\\Desktop\\image\\{DateTime.Now.Ticks}_CalibratSharpenAsync.jpg", mat);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    //正常取消任务
                }
            }, linkedToken);
            await Task.WhenAny(slowSpeedMoveTask, grabTimerTask).ContinueWith(a => cts.Cancel(), linkedToken);
            return 0;
        }

        public static async Task WaitAllAxisStopAsync(CancellationToken token)
        {
            Task taskWhole = TaskUtils.WaitExpectedResultAsync(PlcControl.tagControl.wholeDevice.IsSpindleStopAsync, token);
            Task taskX = PlcControl.tagControl.Xaxis.WaitAxisStopAsync(token);
            Task taskY = PlcControl.tagControl.Yaxis.WaitAxisStopAsync(token);
            Task taskZ1 = PlcControl.tagControl.Z1axis.WaitAxisStopAsync(token);
            Task taskZ2 = PlcControl.tagControl.Z2axis.WaitAxisStopAsync(token);
            await Task.WhenAll(taskWhole, taskX, taskY, taskZ1, taskZ2);
        }

        public static CameraCommon? GetCameraCommon()
        {
            MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null || !CommonCheck.AxisReady(false))
            {
                return null;
            }
            // 获取相机页面
            List<CameraCommon> cameraCommons = Tools.GetChildrenOfType<CameraCommon>(mainWindow);
            if (cameraCommons.Count == 0)
            {
                return null;
            }
            return cameraCommons.FirstOrDefault();
        }

        public static async Task<float?> AutoFocusAsync(IEventAggregator? eventAggregator = null, CancellationToken token = default)
        {
            eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("开始相机对焦..."));
            CameraCommon? cameraCommon = GetCameraCommon();
            if (cameraCommon is null)
            {
                MaterialSnackUtils.MaterialSnack("相机获取失败！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
                return null;
            }
            float focusClearZ = Appsettings.FocusClearZ ?? 0;
            await PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(0, default, token);
            await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(focusClearZ, 2, token);
            // 模糊度大于200时，直接返回清晰位置
            if (cameraCommon.localBitmap != null)
            {
                double tenengradBlurriness = VisualUtils.CalculateTenengrad2(cameraCommon.localBitmap);
                eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"当前位置：{focusClearZ} 当前模糊度：{tenengradBlurriness}"));
                if (tenengradBlurriness > 200)
                {
                    eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"图像已清晰无需再次聚焦"));
                    return focusClearZ;
                }
            }
            float? roughFocusPosition = await AutoFocusAsync(cameraCommon, focusClearZ, 0.5f, 0.05f, token, eventAggregator);
            if (roughFocusPosition == null)
            {
                MaterialSnackUtils.MaterialSnack("粗调聚焦失败！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
                return null;
            }
            // 进行精调聚焦
            return await AutoFocusAsync(cameraCommon, roughFocusPosition.Value, 0.05f, 0.01f, token, eventAggregator);
        }

        private static async Task<float?> AutoFocusAsync(CameraCommon cameraCommon, float startPositionZ2, float margin, float singleMoveDistance, CancellationToken token, IEventAggregator? eventAggregator = null)
        {
            float lastBlurriness = 0;
            float lastPosition = 0;
            for (float newPosition = startPositionZ2 - margin; newPosition <= startPositionZ2 + margin; newPosition += singleMoveDistance)
            {
                await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(newPosition, default, token);
                if (cameraCommon.localBitmap != null)
                {
                    float tenengradBlurriness = (float)VisualUtils.CalculateTenengrad2(cameraCommon.localBitmap);
                    eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"当前位置：{newPosition} 当前模糊度：{tenengradBlurriness}"));
                    if (lastBlurriness > 0 && lastBlurriness - tenengradBlurriness > 0.5)
                    {
                        // 找到最清晰的位置，停止循环并移动到上一个位置
                        eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"最清晰的图片已找到，Z2位置{lastPosition}"));
                        // 调用plc方法，走到上一个位置
                        await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(lastPosition, 0.2f, token);
                        return lastPosition;
                    }
                    lastBlurriness = tenengradBlurriness;
                    lastPosition = newPosition;
                }
                else
                {
                    eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("聚焦获取当前帧失败！"));
                }
            }
            return null;
        }

        /// <summary>
        /// 工作盘吹气
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task WorkpieceBlowingAsync(IEventAggregator? eventAggregator = null, CancellationToken token = default)
        {
            eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("开始工件吹气..."));
            try
            {
                await PlcControl.tagControl.wholeDevice.OpenWorkpieceBlowingAsync();
                await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(190, 20, token);
                await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(2, 20, token);
            }
            finally
            {
                await PlcControl.tagControl.wholeDevice.CloseWorkpieceBlowingAsync();
            }
        }

        private static async Task<Queue<CutStep>?> GetAllCutSequenceAsync(long id, float afterHeightMeasurementZ)
        {
            // 判断是否确认配置信息
            if (id == 0)
            {
                MaterialSnackUtils.MaterialSnack("未确认配置信息！", MaterialSnackUtils.SnackType.WARNING);
                return null;
            }
            // 查询配置信息
            var listConf = await SqlHelper.TableAsync<FileTableItemModel>().Where(t => t.Id == id).ToListAsync();
            if (listConf.Count == 0)
            {
                MaterialSnackUtils.MaterialSnack("未确认配置信息！", MaterialSnackUtils.SnackType.WARNING);
                return null;
            }
            FileTableItemModel fileTable = listConf[0];
            // 查询通道信息
            List<FileTableItemChModel> chModels = await SqlHelper.TableAsync<FileTableItemChModel>()
                .Where(t => t.ItemId == fileTable.Id).ToListAsync();

            Queue<CutStep>? cutSteps = new Queue<CutStep>();
            int[] chSeqs = Tools.StringToIntegerArray(fileTable.CuttingChSeq);
            foreach (int chIndex in chSeqs)
            {
                int tempChIndex = chIndex - 1;
                // 获取当前子ch信息 
                FileTableItemChModel ch = chModels[tempChIndex];
                Queue<CutStep>? cutStepsQueue = GetCutSequence(fileTable, ch, afterHeightMeasurementZ);
                if (cutStepsQueue == null)
                {
                    Tools.LogError("获取切割序列失败！");
                    return null;
                }
                while (cutStepsQueue.TryDequeue(out CutStep? cutStep))
                {
                    cutSteps.Enqueue(cutStep);
                }

            }
            return cutSteps;
        }

        private static async Task<Queue<SharpenStep>?> GetAllSharpenStepSequenceAsync(int bmSharpParamId, float afterHeightMeasurementZ, DataRectangleF sharpenRect)
        {
            List<BmSharpenParameterModel> list = await SqlHelper.TableAsync<BmSharpenParameterModel>()
                                .Where(t => t.Id == bmSharpParamId).ToListAsync();
            if (list.Count <= 0)
            {
                Tools.LogError("磨刀参数获取错误！");
                MaterialSnackUtils.MaterialSnack("磨刀参数获取错误！", MaterialSnackUtils.SnackType.WARNING);
                return null;
            }
            BmSharpenParameterModel sharpenParam = list[0];

            Queue<SharpenStep> sharpenSteps = new Queue<SharpenStep>();
            // 获取两个theta的磨刀序列
            float theta0 = 0;
            float theta90 = 90;
            Queue<SharpenStep>? sharpenStepsTheta0 = GetSharpenStepSequence(sharpenParam, theta0, afterHeightMeasurementZ, sharpenRect.Height);
            if (sharpenStepsTheta0 != null)
            {
                while (sharpenStepsTheta0.TryDequeue(out SharpenStep? sharpenStep))
                {
                    sharpenSteps.Enqueue(sharpenStep);
                }
            }
            Queue<SharpenStep>? sharpenStepsTheta90 = GetSharpenStepSequence(sharpenParam, theta90, afterHeightMeasurementZ, sharpenRect.Width);
            if (sharpenStepsTheta90 != null)
            {
                while (sharpenStepsTheta90.TryDequeue(out SharpenStep? sharpenStep))
                {
                    sharpenSteps.Enqueue(sharpenStep);
                }
            }

            return sharpenSteps;
        }

        /// <summary>
        /// 磨刀
        /// </summary>
        /// <returns>磨刀剩余刀数</returns>
        private static async Task<ProcessResult> ProcessSharpenAsync(DataPoint<float> thetaCenterPoint, DataRectangleF sharpenRect, float startY, Queue<SharpenStep> sharpenStepsQueue, int totalSharpTimes)
        {
            Tools.LogDebug("开始磨刀");
            float recordSharpenY = startY;
            int sharpTimes;
            for (sharpTimes = 0; sharpTimes < totalSharpTimes; sharpTimes++)
            {
                if (sharpenStepsQueue.TryDequeue(out SharpenStep? sharpenStep))
                {
                    try
                    {
                        if (sharpenStep.IsChanelFirstStep)
                        {
                            // 通道第一次切割，切割矩形最下边切为起始位置
                            recordSharpenY = GeometryUtils.FindBottomTangentY(thetaCenterPoint, sharpenRect, sharpenStep.ThetaDeg);
                        }
                        recordSharpenY = CalculateCutY(recordSharpenY, sharpenStep.CutSize, sharpenStep.Direction);
                        LineSegment? line = CalculateRectangleCuttingLine(thetaCenterPoint, sharpenRect, sharpenStep.ThetaDeg, recordSharpenY, 3);
                        if (line == null)
                        {
                            Tools.LogDebug("获取磨刀线失败！");
                            return ProcessResult.FAIL;
                        }
                        await PlcControl.tagControl.cutting.SetCutParamsAsync(sharpenStep.FeedSpeed, sharpenStep.CutZ, sharpenStep.CutZ - 2, line.StartPoint.X,
                            line.EndPoint.X, line.StartPoint.Y, "0", sharpenStep.ThetaDeg, 20000, sharpenStep.Direction);
                        await PlcControl.tagControl.cutting.StartCutAsync();
                    }
                    catch (Exception ex)
                    {
                        Tools.LogDebug($"执行磨刀步骤失败！{ex.Message}");
                        return ProcessResult.FAIL;
                    }
                }
                else
                {
                    break;
                }
            }
            Tools.LogDebug("结束磨刀");
            return new ProcessResult(totalSharpTimes - sharpTimes, recordSharpenY);
        }

        /// <summary>
        /// 执行切割
        /// </summary>
        /// <returns>未切割刀数</returns>
        private static async Task<ProcessResult> ProcessSemicircleCutSequenceAsync(DataPoint<float> thetaCenterPoint, float workpieceRadius, float centerDistance, float startY, Queue<CutStep> cutStepsQueue, int totalCutTimes)
        {
            Tools.LogDebug("开始切割");
            //记录切割Y轴位置
            float recordCutY = startY;
            //记录已切割次数
            int cutTimes;
            for (cutTimes = 0; cutTimes < totalCutTimes; cutTimes++)
            {
                if (cutStepsQueue.TryDequeue(out CutStep? cutStep))
                {
                    try
                    {
                        if (cutStep.IsChanelFirstStep)
                        {
                            // 每个通道theta可能不同，如果是通道第一个切割步骤，则重新计算起始Y轴位置
                            //计算工件圆心坐标
                            DataPoint<float> workpieceCenterPoint = GeometryUtils.RotatePoint(new DataPoint<float>(thetaCenterPoint.X, thetaCenterPoint.Y + centerDistance), thetaCenterPoint, cutStep.ThetaDeg);
                            //Y轴切割起始位置
                            recordCutY = GeometryUtils.FindBottomTangentY(thetaCenterPoint, workpieceCenterPoint, workpieceRadius, cutStep.ThetaDeg);
                        }
                        recordCutY = CalculateCutY(recordCutY, cutStep.CutSize, cutStep.Direction);
                        LineSegment? cutLine = CalculateSemicircleCuttingLine(thetaCenterPoint, cutStep.ThetaDeg, workpieceRadius, centerDistance, recordCutY);
                        if (cutLine == null)
                        {
                            Tools.LogError("获取切割线失败！");
                            return ProcessResult.FAIL;
                        }
                        //深度模式会有多次不同深度的切割
                        foreach (float cutZ in cutStep.CutZ)
                        {
                            await PlcControl.tagControl.cutting.SetCutParamsAsync(cutStep.FeedSpeed, cutZ, cutZ - 2, cutLine.StartPoint.X,
                                cutLine.EndPoint.X, cutLine.StartPoint.Y, "0", cutStep.ThetaDeg, 20000, cutStep.Direction);
                            await PlcControl.tagControl.cutting.StartCutAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Tools.LogError($"执行切割步骤失败！{ex.Message}");
                        return ProcessResult.FAIL;
                    }
                }
                else
                {
                    break;
                }
            }
            Tools.LogDebug("结束切割");
            return new ProcessResult(totalCutTimes - cutTimes, recordCutY);
        }

        /// <summary>
        /// 获取切割序列
        /// </summary>
        /// <param name="fileTableItemModel"></param>
        /// <param name="ch"></param>
        /// <returns></returns>
        private static Queue<CutStep>? GetCutSequence(FileTableItemModel fileTableItemModel, FileTableItemChModel ch, float afterHeightMeasurementZ)
        {
            if (!float.TryParse(fileTableItemModel.TapeThickness, out float tapeThickness) ||
                !float.TryParse(fileTableItemModel.WorkThickness, out float workThickness) ||
                !int.TryParse(ch.CutLine, out int totalCutNum) ||
                !float.TryParse(ch.ThetaDeg, out float thetaDeg) ||
                !Tools.TryParseStringToFloatArray(ch.BladeHeight, out float[] setBladeHeights) ||
                !Tools.TryParseStringToFloatArray(ch.DepthSteps, out float[] depthSteps) ||
                !Tools.TryParseStringToFloatArray(ch.FeedSpeed, out float[] feedSpeeds) ||
                !Tools.TryParseStringToFloatArray(ch.YIndex, out float[] yIndexs) ||
                !Tools.TryParseStringToFloatArray(ch.RepeatTimes, out float[] repeatTimes))
            {
                return null; // 任意一个转换失败则返回null
            }
            CutDirection cutDirection = ch.CutDir == "FRONT" ? CutDirection.Forward : CutDirection.Backward;


            // 检查索引是否连续
            int maxIndex = CutUtils.AreIndexesContinuous(setBladeHeights, feedSpeeds, yIndexs, repeatTimes);
            if (maxIndex == 0)
            {
                MaterialSnackUtils.MaterialSnack("切割参数错误！", MaterialSnackUtils.SnackType.ERROR);
                Tools.LogError("切割参数错误！");
                return null;
            }
            // 获取循环控制信息
            List<string> loops = Tools.StringToStringArray(ch.Loop).ToList();
            List<int> sequences = CutUtils.CombineSequences(GenerateNumberList(maxIndex), loops);
            Queue<CutStep> cutStepQueue = new Queue<CutStep>();
            foreach (int i in sequences)
            {
                float repeatTime = repeatTimes[i];
                float setBladeHeight = setBladeHeights[i];
                float feedSpeed = feedSpeeds[i];
                float yIndex = yIndexs[i];
                float depthStep = depthSteps[i];
                // 刀数
                for (int repeatNum = 0; repeatNum < repeatTime; repeatNum++)
                {
                    CutStep cutStep = new CutStep
                    {
                        CutZ = [],
                        FeedSpeed = feedSpeed,
                        CutSize = yIndex,
                        Direction = cutDirection,
                        ThetaDeg = thetaDeg
                    };
                    // 判断深度模式
                    if (depthStep == 0)
                    {
                        cutStep.CutZ.Add(afterHeightMeasurementZ - setBladeHeight);
                    }
                    else
                    {
                        // 固定切割深度，多次切割以达到目标深度
                        float cuttingDepth = tapeThickness + workThickness - setBladeHeight;
                        int stepCount = (int)(cuttingDepth / depthStep);
                        if (stepCount == 0)
                        {
                            // 固定切割深度大于目标深度
                            cutStep.CutZ.Add(afterHeightMeasurementZ - setBladeHeight);
                        }
                        else
                        {
                            float curBladeHeight = tapeThickness + workThickness;
                            for (int stepNum = 0; stepNum < stepCount; stepNum++)
                            {
                                curBladeHeight -= depthStep;
                                cutStep.CutZ.Add(afterHeightMeasurementZ - curBladeHeight);
                            }
                            if (curBladeHeight > setBladeHeight)
                            {
                                //未完全到达目标深度
                                cutStep.CutZ.Add(afterHeightMeasurementZ - setBladeHeight);
                            }
                        }
                    }
                    cutStepQueue.Enqueue(cutStep);
                    //切割数够了就返回
                    if (cutStepQueue.Count == totalCutNum)
                    {
                        return cutStepQueue;
                    }
                }
            }
            // 切割数不够，继续添加，直到达到切割数
            if (cutStepQueue.Count < totalCutNum)
            {
                int cycleCount = (totalCutNum - cutStepQueue.Count) / cutStepQueue.Count;
                int remainCount = (totalCutNum - cutStepQueue.Count) % cutStepQueue.Count;
                List<CutStep> list = cutStepQueue.ToList();
                for (int i = 0; i < cycleCount; i++)
                {
                    foreach (CutStep step in list)
                    {
                        cutStepQueue.Enqueue(step.DeepCopy());
                    }
                }
                for (int i = 0; i < remainCount; i++)
                {
                    cutStepQueue.Enqueue(list[i].DeepCopy());
                }
            }
            // 设置第一个切割线的标记
            if (cutStepQueue.Count > 0)
            {
                cutStepQueue.Peek().IsChanelFirstStep = true;
            }
            return cutStepQueue;
        }

        private static Queue<SharpenStep>? GetSharpenStepSequence(BmSharpenParameterModel sharpenParam, float theta, float afterHeightMeasurementZ, float sharpenDistance)
        {
            try
            {
                // 初始化数据
                Tuple<float, float>[] sharpenNumAndSpeed =
                [
                    new Tuple<float, float>(Tools.GetIntStringValue(sharpenParam.MoCutOneNo), Tools.GetFloatStringValue(sharpenParam.MoCutOneSpeed)),
                    new Tuple<float, float>(Tools.GetIntStringValue(sharpenParam.MoCutTwoNo), Tools.GetFloatStringValue(sharpenParam.MoCutTwoSpeed)),
                    new Tuple<float, float>(Tools.GetIntStringValue(sharpenParam.MoCutThreeNo), Tools.GetFloatStringValue(sharpenParam.MoCutThreeSpeed)),
                    new Tuple<float, float>(Tools.GetIntStringValue(sharpenParam.MoCutFourNo), Tools.GetFloatStringValue(sharpenParam.MoCutFourSpeed)),
                    new Tuple<float, float>(Tools.GetIntStringValue(sharpenParam.MoCutFiveNo), Tools.GetFloatStringValue(sharpenParam.MoCutFiveSpeed)),
                    new Tuple<float, float>(Tools.GetIntStringValue(sharpenParam.MoCutSixNo), Tools.GetFloatStringValue(sharpenParam.MoCutSixSpeed)),
                    new Tuple<float, float>(Tools.GetIntStringValue(sharpenParam.MoCutSevenNo), Tools.GetFloatStringValue(sharpenParam.MoCutSevenSpeed)),
                    new Tuple<float, float>(Tools.GetIntStringValue(sharpenParam.MoCutEightNo), Tools.GetFloatStringValue(sharpenParam.MoCutEightSpeed)),
                    new Tuple<float, float>(Tools.GetIntStringValue(sharpenParam.MoCutNineNo), Tools.GetFloatStringValue(sharpenParam.MoCutNineSpeed)),
                    new Tuple<float, float>(Tools.GetIntStringValue(sharpenParam.MoCutTenNo), Tools.GetFloatStringValue(sharpenParam.MoCutTenSpeed))
                ];
                float setBladeHeight = sharpenParam.CutHeight;
                float cutSize = sharpenParam.CoCutSize;
                CutDirection cutDirection = sharpenParam.CoCutDirection == "FRONT" ? CutDirection.Forward : CutDirection.Backward;
                float curSharpenDistance = 0;

                Queue<SharpenStep> sharpenSteps = new Queue<SharpenStep>();
                foreach (var tuple in sharpenNumAndSpeed)
                {
                    //切割刀数
                    for (int i = 0; i < tuple.Item1; i++)
                    {
                        sharpenSteps.Enqueue(new SharpenStep
                        {
                            CutZ = afterHeightMeasurementZ - setBladeHeight,
                            FeedSpeed = tuple.Item2,
                            CutSize = cutSize,
                            Direction = cutDirection,
                            ThetaDeg = theta,
                        });
                        curSharpenDistance += cutSize;
                        //磨刀距离达到设定值
                        if (curSharpenDistance >= sharpenDistance)
                        {
                            return sharpenSteps;
                        }
                    }
                }
                List<SharpenStep> list = sharpenSteps.ToList();
                // 磨刀距离未达到设定值，继续添加，直到达到
                while (curSharpenDistance < sharpenDistance)
                {
                    foreach (SharpenStep step in list)
                    {
                        curSharpenDistance += cutSize;
                        if (curSharpenDistance >= sharpenDistance)
                        {
                            break;
                        }
                        sharpenSteps.Enqueue(step.DeepCopy());
                    }
                }
                // 设置第一个切割线的标记
                if (sharpenSteps.Count > 0)
                {
                    sharpenSteps.Peek().IsChanelFirstStep = true;
                }
                return sharpenSteps;
            }
            catch (Exception ex)
            {
                Tools.LogError($"获取磨刀序列失败！{ex.Message}");
                return null;
            }
        }

        public static float CalculateCutY(float cutY, float cutSize, CutDirection cutDirection)
        {

            //计算切割线的偏移
            var cutYOffset = cutDirection switch
            {
                CutDirection.Forward => cutSize,
                CutDirection.Backward => -cutSize,
                _ => 0,
            };
            //计算下一个切割线的Y坐标
            float nextCutY = cutY + cutYOffset;
            return nextCutY;
        }

        public static LineSegment? CalculateRectangleCuttingLine(DataPoint<float> thetaCenterPoint, DataRectangleF rectangleF, float rotationAngle, float cutY, float margin)
        {
            //计算切割线与工件矩形的交点
            LineSegment? line = GeometryUtils.CalculateRectangleIntersectionLine(thetaCenterPoint, rectangleF, rotationAngle, cutY);
            if (line == null)
            {
                return null;
            }
            //加上边距
            line.StartPoint.X -= margin;
            line.EndPoint.X += margin;
            //计算交点可能有误差，重新设置Y坐标，保证切割线起始点和结束点的Y坐标一致
            line.StartPoint.Y = cutY;
            line.EndPoint.Y = cutY;
            return line;
        }

        /// <summary>
        /// 计算切割线 (半圆工件)
        /// </summary>
        /// <param name="thetaCenterPoint">theta中心的位置</param>
        /// <param name="margin"></param>
        /// <param name="workpieceRadius"></param>
        /// <param name="centerDistance"></param>
        /// <param name="cutY"></param>
        /// <param name="cutYStep"></param>
        /// <param name="cutDirection"></param>
        /// <returns>切割线始终是起始点在X负方向，结束点在X轴正方向</returns>
        public static LineSegment? CalculateSemicircleCuttingLine(DataPoint<float> thetaCenterPoint, float rotationAngle, float workpieceRadius, float centerDistance, float cutY)
        {
            //计算工件圆心坐标
            DataPoint<float> workpieceCenterPoint = new DataPoint<float>(thetaCenterPoint.X, thetaCenterPoint.Y + centerDistance);
            //计算切割线与工件圆的交点
            LineSegment? line = GeometryUtils.CalculateSemicircleIntersectionLine(thetaCenterPoint, new DataPoint<float>(workpieceCenterPoint.X - thetaCenterPoint.X, workpieceCenterPoint.Y - thetaCenterPoint.Y), 
                workpieceRadius, rotationAngle, cutY);
            if (line == null)
            {
                return null;
            }
            //计算交点可能有误差，重新设置Y坐标，保证切割线起始点和结束点的Y坐标一致
            line.StartPoint.Y = cutY;
            line.EndPoint.Y = cutY;
            return line;
        }

        public static List<int> GenerateNumberList(int number)
        {
            List<int> list = new List<int>();

            for (int i = 0; i <= number; i++)
            {
                list.Add(i);
            }

            return list;
        }

        public static float GetSharpenDeep(string bladeType)
        {
            switch (bladeType)
            {
                case "A":
                case "B":
                    return 0.2f;
                case "C":
                    return 0.3f;
                case "D":
                    return 0.35f;
                default:
                    return 0.2f;
            }
        }

        public static float GetCuttingDeep(string bladeType)
        {
            switch (bladeType)
            {
                case "A":
                case "B":
                    return 0.2f;
                case "C":
                    return 0.3f;
                case "D":
                    return 0.35f;
                default:
                    return 0.2f;
            }
        }

        public static float GetBladeExposedMax(float abAverageThickness)
        {
            if (abAverageThickness < 0.013f || abAverageThickness.NearlyEquals(0.013f))
            {
                return 0.4f;
            }
            else if (abAverageThickness < 0.015f || abAverageThickness.NearlyEquals(0.015f))
            {
                return 0.45f;
            }
            else if (abAverageThickness < 0.022f || abAverageThickness.NearlyEquals(0.022f))
            {
                return 0.54f;
            }
            else
            {
                return 0.56f;
            }
        }

        /// <summary>
        /// 获取需要磨刀的次数
        /// </summary>
        /// <param name="bladeLength">刀刃长度</param>
        /// <param name="bladeExposedMax">蚀刻后刀刃暴露量范围最大值</param>
        /// <param name="singleBladeWear">磨刀一刀磨损量</param>
        /// <returns></returns>
        public static int GetNeedSharpenTimes(float bladeLength, float bladeExposedMax, float singleBladeWear)
        {
            return (int)Math.Ceiling((bladeLength - bladeExposedMax) / singleBladeWear);
        }

        /// <summary>
        /// 检查是否满足进入切割的条件
        ///1. 磨刀后当前刀刃长度 <=【刀刃蚀刻后最长暴露量范围】最大值
        ///2. 长宽比 <=【刀刃初始长宽比范围】最大值
        ///3. 刀厚 > =21，单次磨损量<=20；刀厚<21，单次磨损量<=50
        /// </summary>
        /// <param name="lunguSksj"></param>
        /// <param name="firstHeightMeasurementZ"></param>
        /// <param name="curHeightZ"></param>
        /// <returns></returns>
        public static bool CheckIsMeetsCuttingConditions(LunguSksjDTO lunguSksj, float firstHeightMeasurementZ, float curHeightZ)
        {
            return true;
            float wearAmount = Math.Abs(curHeightZ - firstHeightMeasurementZ);
            if (float.TryParse(lunguSksj.BladeOuterDiameter, out float bladeOuterDiameter))
            {
                bool isMeetCutting = true;
                //磨刀后当前刀刃长度 <=【刀刃蚀刻后最长暴露量范围】最大值
                isMeetCutting = isMeetCutting && bladeOuterDiameter - wearAmount <= lunguSksj.LongestBlade;
                //长宽比 <=【刀刃初始长宽比范围】最大值

                //刀厚 > =21，单次磨损量<=20；刀厚<21，单次磨损量<=50

                return isMeetCutting;
            }
            return false;
        }

        /// <summary>
        /// 获取当前刀刃长度
        /// </summary>
        /// <returns></returns>
        public static float GetCurrentBladeLength()
        {
            return 0.38f;
        }

        /// <summary>
        /// 检查是否达到进入切割的条件
        /// </summary>
        /// <returns></returns>
        public static bool CheckHasMeetCuttingCondition()
        {
            return true;
        }

        /// <summary>
        /// 检查刀痕状态
        /// </summary>
        /// <returns></returns>
        public static async Task<ImagesAnalysisResult?> CheckKnifeMarksStatus(LineSegment line, IEventAggregator? eventAggregator = null, CancellationToken token = default)
        {
            //工件吹气
            await WorkpieceBlowingAsync(eventAggregator, token);
            DataPoint<float> relativePos = Appsettings.CameraRelativeBladePosition;
            await PlcControl.tagControl.cutting.RunMotionAsync(line.StartPoint.X + relativePos.X, line.StartPoint.Y + relativePos.Y, token);
            //await AutoFocusAsync(eventAggregator, token);
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token);
            CancellationToken linkedToken = linkedCts.Token;
            Task slowSpeedMoveTask = PlcControl.tagControl.Xaxis.StartAbsoluteAsync(line.EndPoint.X + relativePos.X, 7f, linkedToken);
            Task<List<Mat>> grabTimerTask = Task.Run(async () =>
            {
                List<Mat> mats = new List<Mat>();
                try
                {
                    using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(150));
                    while (await timer.WaitForNextTickAsync(linkedToken))
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            WriteableBitmap? localBitmap = GrabWriteableBitmap();
                            if (localBitmap != null)
                            {
                                mats.Add(localBitmap.ToMat());
                            }
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    //正常取消任务
                }
                return mats;
            }, linkedToken);
            await Task.WhenAny(slowSpeedMoveTask, grabTimerTask);
            cts.Cancel();
            List<Mat> mats = await grabTimerTask;
            ImagesAnalysisResult? imagesAnalysisRes;
            try
            {
                imagesAnalysisRes = await ProcessImagesAnalysisAsync(mats, token);
            }
            catch(ArgumentException ex)
            {
                Tools.LogError($"图像处理异常：{ex.Message}");
                imagesAnalysisRes = null;
            }
            return imagesAnalysisRes;
        }

        public static async Task<ImagesAnalysisResult> ProcessImagesAnalysisAsync(List<Mat> mats, CancellationToken token)
        {
            var result = new ImagesAnalysisResult();
            result.IsSnakelike = await Task.Run(() =>
            {
                //拼接所有图像
                List<Mat> concatMats = ConcatMats(mats);
                foreach (Mat concatMat in concatMats)
                {
                    Mat cropConcatMat = CropHorizontalCenter(concatMat, 80);
                    Mat cropConcatMatJpg = JpegStreamToMat(MatToJpegStream(cropConcatMat));
                    var (bladeWidthMm, collapseWidthMm, bladeTop, bladeBottom, collapseTop, collapseBottom) = VisionAnalyzer.ProcessImage(cropConcatMatJpg);
                    Cv2.PutText(cropConcatMatJpg,
                        $"bladeWidthMm: {bladeWidthMm}",
                        new Point(20, bladeTop),
                        HersheyFonts.HersheySimplex,
                        1.5f,
                        Scalar.Red);
                    Cv2.PutText(cropConcatMatJpg,
                        $"collapseWidthMm:{collapseWidthMm}",
                        new Point(800, collapseTop),
                        HersheyFonts.HersheySimplex,
                        1.5f,
                        Scalar.Green);
                    Cv2.Line(
                        img: cropConcatMatJpg,
                        pt1: new Point(0, bladeTop),  // 起点
                        pt2: new Point(cropConcatMatJpg.Width, bladeTop), // 终点
                        color: Scalar.Red,         // 颜色 (B,G,R)
                        thickness: 1,             // 线宽
                        lineType: LineTypes.AntiAlias // 抗锯齿
                        );
                    Cv2.Line(
                        img: cropConcatMatJpg,
                        pt1: new Point(0, bladeBottom),  // 起点
                        pt2: new Point(cropConcatMatJpg.Width, bladeBottom), // 终点
                        color: Scalar.Red,         // 颜色 (B,G,R)
                        thickness: 1,             // 线宽
                        lineType: LineTypes.AntiAlias // 抗锯齿
                        );
                    Cv2.Line(
                        img: cropConcatMatJpg,
                        pt1: new Point(0, collapseTop),  // 起点
                        pt2: new Point(cropConcatMatJpg.Width, collapseTop), // 终点
                        color: Scalar.Green,         // 颜色 (B,G,R)
                        thickness: 1,             // 线宽
                        lineType: LineTypes.AntiAlias // 抗锯齿
                        );
                    Cv2.Line(
                        img: cropConcatMatJpg,
                        pt1: new Point(0, collapseBottom),  // 起点
                        pt2: new Point(cropConcatMatJpg.Width, collapseBottom), // 终点
                        color: Scalar.Green,         // 颜色 (B,G,R)
                        thickness: 1,             // 线宽
                        lineType: LineTypes.AntiAlias // 抗锯齿
                        );
                    string imagePath = System.IO.Path.Combine(AppContext.BaseDirectory, "image");
                    Directory.CreateDirectory(imagePath);
                    Cv2.ImWrite($"{imagePath}\\{DateTime.Now.Ticks}_cropConcatMatJpg.jpg", cropConcatMatJpg);
                    result.ConcatImages.Add(new ImageData()
                    {
                        BladeWidth = bladeWidthMm,
                        CollapseWidth = collapseWidthMm,
                        IsSnakelike = VisionAnalyzer.SnakeCase(cropConcatMatJpg).Snake,
                        Mat = cropConcatMatJpg
                    });
                }
                return result.ConcatImages.All(image => image.IsSnakelike);
            });

            // 配置参数
            const int ioMaxConcurrency = 2;
            using var semaphore = new SemaphoreSlim(ioMaxConcurrency);
            var tasks = mats.Select(async mat =>
            {
                await semaphore.WaitAsync(token);
                try
                {
                    token.ThrowIfCancellationRequested();
                    Mat cropMat = CropHorizontalCenter(mat, 80);
                    Mat cropMatJpg = JpegStreamToMat(MatToJpegStream(cropMat));
                    var (bladeWidthMm, collapseWidthMm, bladeTop, bladeBottom, collapseTop, collapseBottom) = VisionAnalyzer.ProcessImage(cropMatJpg);
                    Cv2.PutText(cropMatJpg,
                        $"bladeWidthMm: {bladeWidthMm}",
                        new Point(20, bladeTop),
                        HersheyFonts.HersheySimplex,
                        1.5f,
                        new Scalar(0, 0, 255));
                    Cv2.PutText(cropMatJpg,
                        $"collapseWidthMm:{collapseWidthMm}",
                        new Point(20, collapseTop),
                        HersheyFonts.HersheySimplex,
                        1.5f,
                        new Scalar(0, 0, 255));
                    Cv2.Line(
                       img: cropMatJpg,
                       pt1: new Point(0, bladeTop),  // 起点
                       pt2: new Point(cropMatJpg.Width, bladeTop), // 终点
                       color: Scalar.Red,         // 颜色 (B,G,R)
                       thickness: 1,             // 线宽
                       lineType: LineTypes.AntiAlias // 抗锯齿
                       );
                    Cv2.Line(
                        img: cropMatJpg,
                        pt1: new Point(0, bladeBottom),  // 起点
                        pt2: new Point(cropMatJpg.Width, bladeBottom), // 终点
                        color: Scalar.Red,         // 颜色 (B,G,R)
                        thickness: 1,             // 线宽
                        lineType: LineTypes.AntiAlias // 抗锯齿
                        );
                    Cv2.Line(
                        img: cropMatJpg,
                        pt1: new Point(0, collapseTop),  // 起点
                        pt2: new Point(cropMatJpg.Width, collapseTop), // 终点
                        color: Scalar.Green,         // 颜色 (B,G,R)
                        thickness: 1,             // 线宽
                        lineType: LineTypes.AntiAlias // 抗锯齿
                        );
                    Cv2.Line(
                        img: cropMatJpg,
                        pt1: new Point(0, collapseBottom),  // 起点
                        pt2: new Point(cropMatJpg.Width, collapseBottom), // 终点
                        color: Scalar.Green,         // 颜色 (B,G,R)
                        thickness: 1,             // 线宽
                        lineType: LineTypes.AntiAlias // 抗锯齿
                        );

                    var imageData = new ImageData
                    {
                        BladeWidth = bladeWidthMm,
                        CollapseWidth = collapseWidthMm,
                        Mat = cropMatJpg
                    };

                    result.ImageDatas.Add(imageData);

                    // 更新最大值需要线程安全操作
                    lock (result)
                    {
                        if (result.BladeWidthMaxImage.BladeWidth < bladeWidthMm)
                            result.BladeWidthMaxImage = imageData;

                        if (result.CollapseWidthMaxImage.CollapseWidth < collapseWidthMm)
                            result.CollapseWidthMaxImage = imageData;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            return result;
        }

        private static List<Mat> ConcatMats(List<Mat> mats, int maximumWidth = 65500)
        {
            // 1. 输入验证
            if (mats == null || mats.Count == 0)
                return new List<Mat>();

            // 移除空Mat并创建副本避免修改原列表
            var validMats = mats.Where(m => m != null && !m.Empty()).Select(m => m.Clone()).ToList();
            if (validMats.Count == 0)
                return new List<Mat>();

            // 2. 计算最佳分组策略
            var grouped = GroupMatsForConcat(validMats, maximumWidth);

            // 3. 并行处理各组拼接
            var result = new List<Mat>(grouped.Count);
            foreach (var group in grouped)
            {
                if (group.Count == 1)
                {
                    result.Add(group[0]);
                    continue;
                }

                using (var temp = new Mat())
                {
                    Cv2.HConcat(group, temp);
                    result.Add(temp.Clone());
                }

                // 释放组内Mat资源
                foreach (var mat in group)
                {
                    mat.Dispose();
                }
            }

            return result;
        }

        // 辅助方法：计算最优分组
        private static List<List<Mat>> GroupMatsForConcat(List<Mat> mats, int maxWidth)
        {
            var groups = new List<List<Mat>>();
            List<Mat> currentGroup = null;
            int currentWidth = 0;

            foreach (var mat in mats)
            {
                if (currentGroup == null || currentWidth + mat.Width > maxWidth)
                {
                    currentGroup = new List<Mat>();
                    groups.Add(currentGroup);
                    currentWidth = 0;
                }

                currentGroup.Add(mat);
                currentWidth += mat.Width;
            }

            return groups;
        }

        //public static async Task<ImagesAnalysisResult> ProcessImagesAnalysisAsync(List<Mat> mats, CancellationToken token)
        //{
        //    // 配置参数
        //    const int ioMaxConcurrency = 4; // 机械硬盘建议 2，SSD 可提高到 4-8
        //    Task<ImagesAnalysisResult> ioTask = Task.Run(async () =>
        //    {
        //        ImagesAnalysisResult result = new ImagesAnalysisResult();
        //        using var semaphore = new SemaphoreSlim(ioMaxConcurrency);
        //        var tasks = new List<Task>();
        //        foreach (var mat in mats)
        //        {
        //            token.ThrowIfCancellationRequested();
        //            //tasks.Add(SaveImageDataAsync($"C:\\Users\\17632\\Desktop\\image\\{DateTime.Now.Ticks}_mat.jpg", mat, semaphore, token));
        //            Mat cropMat = CropHorizontalCenter(mat, (int)(mat.Height * 0.13));
        //            //tasks.Add(SaveImageDataAsync($"C:\\Users\\17632\\Desktop\\image\\{DateTime.Now.Ticks}_cropMat.jpg", cropMat, semaphore, token));
        //            Mat cropMatJpg = JpegStreamToMat(MatToJpegStream(cropMat));
        //            //tasks.Add(SaveImageDataAsync($"C:\\Users\\17632\\Desktop\\image\\{DateTime.Now.Ticks}_cropMatJpg.jpg", cropMatJpg, semaphore, token));
        //            var (bladeWidthMm, collapseWidthMm) = VisionAnalyzer.ProcessImage(cropMatJpg);
        //            Cv2.PutText(cropMatJpg, $"bladeWidthMm: {bladeWidthMm} collapseWidthMm:{collapseWidthMm}", new OpenCvSharp.Point(10, 40), HersheyFonts.HersheySimplex, 1.5f, new Scalar(0, 0, 255));
        //            ImageData imageData = new ImageData() { BladeWidth = bladeWidthMm, CollapseWidth = collapseWidthMm, Mat = cropMat };
        //            result.ImageDatas.Add(imageData);
        //            tasks.Add(SaveImageDataAsync($"C:\\Users\\17632\\Desktop\\image\\{DateTime.Now.Ticks}_cropMatJpgText.jpg", cropMatJpg, semaphore, token));
        //            if (result.BladeWidthMaxImage.BladeWidth < bladeWidthMm)
        //            {
        //                result.BladeWidthMaxImage = imageData;
        //            }
        //            if (result.CollapseWidthMaxImage.CollapseWidth < collapseWidthMm)
        //            {
        //                result.CollapseWidthMaxImage = imageData;
        //            }
        //        }
        //        await Task.WhenAll(tasks);
        //        return result;
        //    }, token);

        //    return await ioTask;
        //}

        private static async Task SaveImageDataAsync(string filePath, Mat mat, SemaphoreSlim semaphore, CancellationToken token)
        {
            try
            {
                await semaphore.WaitAsync(token);
                await Task.Run(() => Cv2.ImWrite(filePath, mat), token);
            }
            finally
            {
                semaphore.Release();
            }
        }

        public static WriteableBitmap? GrabWriteableBitmap()
        {
            SciCam m_currentDev = CameraUtils.m_currentDev;
            nint payload = nint.Zero;
            uint nReVal = m_currentDev.Grab(ref payload);
            try
            {
                if (nReVal == SciCam.SCI_CAMERA_OK)
                {
                    if (payload != nint.Zero)
                    {
                        int result = GetConvertedInfo(payload, out WriteableBitmap localBitmap);
                        if (result == 0 && localBitmap != null)
                        {
                            return localBitmap;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                else
                {
                    Tools.LogError($"Error grabbing image: {nReVal}");
                }
                return null;
            }
            finally
            {
                // 释放负载
                if (payload != nint.Zero)
                {
                    nReVal = m_currentDev.FreePayload(payload);
                    if (nReVal != SciCam.SCI_CAMERA_OK)
                    {
                        Tools.LogError($"Error grabbing image: {nReVal}");
                    }
                    payload = nint.Zero; // 重置 payload
                }
            }
        }


        public static Mat CropHorizontalCenter(Mat sourceImage, int heightRange)
        {
            return CropHorizontalCenter(sourceImage, heightRange, heightRange);
        }

        public static Mat CropHorizontalCenter(Mat sourceImage, int topRange, int bottomRange)
        {
            int height = sourceImage.Height;
            int width = sourceImage.Width;
            int centerY = height / 2;

            int startY = Math.Max(0, centerY - topRange);
            int endY = Math.Min(height, centerY + bottomRange);

            OpenCvSharp.Rect roi = new OpenCvSharp.Rect(0, startY, width, endY - startY);
            return new Mat(sourceImage, roi).Clone();
        }

        private static int GetConvertedInfo(nint payload, out WriteableBitmap bitmap)
        {
            bitmap = null;
            if (payload == nint.Zero)
            {
                return -1;
            }
            SciCam.SCI_CAM_PAYLOAD_ATTRIBUTE payloadAttribute = new SciCam.SCI_CAM_PAYLOAD_ATTRIBUTE();
            uint nReVal = SciCam.PayloadGetAttribute(payload, ref payloadAttribute);
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                return -1;
            }
            bool imgIsComplete = payloadAttribute.isComplete;
            SciCam.SciCamPayloadMode payloadMode = payloadAttribute.payloadMode;
            SciCam.SciCamPixelType imgPixelType = payloadAttribute.imgAttr.pixelType;
            ulong imgWidth = payloadAttribute.imgAttr.width;
            ulong imgHeight = payloadAttribute.imgAttr.height;
            if (!imgIsComplete || payloadMode != SciCam.SciCamPayloadMode.SciCam_PayloadMode_2D)
            {
                return -1;
            }
            nint imgData = nint.Zero;
            nReVal = SciCam.PayloadGetImage(payload, ref imgData);
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                return -1;
            }
            long destImgSize = 0;
            nint destImg = nint.Zero; // Initialize destImg
            try
            {
                if (IsValidPixelType(imgPixelType))
                {
                    nReVal = SciCam.PayloadConvertImage(ref payloadAttribute.imgAttr, imgData, SciCam.SciCamPixelType.Mono8, nint.Zero, ref destImgSize, true);
                    if (nReVal == SciCam.SCI_CAMERA_OK)
                    {
                        destImg = Marshal.AllocHGlobal((int)destImgSize);
                        try
                        {
                            nReVal = SciCam.PayloadConvertImage(ref payloadAttribute.imgAttr, imgData, SciCam.SciCamPixelType.Mono8, destImg, ref destImgSize, true);
                            if (nReVal == SciCam.SCI_CAMERA_OK)
                            {
                                byte[] bBitmap = new byte[destImgSize];
                                Marshal.Copy(destImg, bBitmap, 0, (int)destImgSize);
                                int stride = (int)imgWidth; // Assuming 1 byte per pixel  
                                bitmap = new WriteableBitmap((int)imgWidth, (int)imgHeight, 96, 96, PixelFormats.Gray8, null);
                                bitmap.WritePixels(new System.Windows.Int32Rect(0, 0, (int)imgWidth, (int)imgHeight), bBitmap, stride, 0);
                            }
                        }
                        catch (Exception ex)
                        {
                            // 处理异常，例如记录日志
                            Console.WriteLine($"Error during image conversion: {ex.Message}");
                            return -1;
                        }
                    }
                }
            }
            finally
            {
                if (destImg != nint.Zero)
                {
                    Marshal.FreeHGlobal(destImg);
                }
            }
            return 0;
        }

        private static bool IsValidPixelType(SciCam.SciCamPixelType pixelType)
        {
            return pixelType == SciCam.SciCamPixelType.Mono1p ||
                   pixelType == SciCam.SciCamPixelType.Mono2p ||
                   pixelType == SciCam.SciCamPixelType.Mono4p ||
                   pixelType == SciCam.SciCamPixelType.Mono8s ||
                   pixelType == SciCam.SciCamPixelType.Mono8 ||
                   pixelType == SciCam.SciCamPixelType.Mono10 ||
                   pixelType == SciCam.SciCamPixelType.Mono10p ||
                   pixelType == SciCam.SciCamPixelType.Mono12 ||
                   pixelType == SciCam.SciCamPixelType.Mono12p ||
                   pixelType == SciCam.SciCamPixelType.Mono14 ||
                   pixelType == SciCam.SciCamPixelType.Mono16 ||
                   pixelType == SciCam.SciCamPixelType.Mono10Packed ||
                   pixelType == SciCam.SciCamPixelType.Mono12Packed ||
                   pixelType == SciCam.SciCamPixelType.Mono14p;
        }

        public static MemoryStream MatToJpegStream(Mat image, int quality = 95)
        {
            if (image.Empty())
                throw new ArgumentException("输入图像为空");

            // 设置 JPEG 压缩参数（质量范围：0-100）
            var parameters = new int[]
            {
                (int)ImwriteFlags.JpegQuality,
                quality
            };

            // 将 Mat 编码为 JPEG 字节数组
            byte[] jpegBytes;
            Cv2.ImEncode(".jpg", image, out jpegBytes, parameters);

            // 创建 MemoryStream
            return new MemoryStream(jpegBytes);
        }

        public static Mat JpegStreamToMat(MemoryStream jpegStream)
        {
            if (jpegStream == null || jpegStream.Length == 0)
                throw new ArgumentException("MemoryStream 无效");

            // 将 MemoryStream 转为字节数组
            byte[] jpegBytes = jpegStream.ToArray();

            // 解码字节数组为 Mat
            return Cv2.ImDecode(jpegBytes, ImreadModes.Color);
        }

        public void AddTextWithOpenCV(string imagePath, string outputPath, string text)
        {
            using (var mat = Cv2.ImRead(imagePath))
            {
                Cv2.PutText(
                    mat,
                    text,
                    new OpenCvSharp.Point(10, 50),
                    HersheyFonts.HersheyComplex,
                    1.0,
                    Scalar.Red,
                    2);

                Cv2.ImWrite(outputPath, mat);
            }
        }
    }


}
