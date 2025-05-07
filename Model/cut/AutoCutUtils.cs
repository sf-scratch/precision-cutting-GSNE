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
using System.Drawing;
using 精密切割系统.View.Pages.common;
using System.IO;
using System.Windows;
using System.Drawing.Imaging;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using Emgu.CV.Reg;

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
            //发送测高值到MES
            await HttpUtils.SendMeasureHeightToMES(afterHeightMeasurementZ.Value);
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
        /// 执行测高
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<float?> ProcessMeasureHeightAsync(HeightMeasurementMode mode, CancellationToken token)
        {
            InitialPositionModel? initPos = await GetInitialPositionAsync();
            if (initPos is null) return null;
            switch (mode)
            {
                //接触测高
                case HeightMeasurementMode.Contact:
                    await PlcControl.tagControl.bladeMantance.SetBladeSetuInitPositionAsync(initPos.BladeSetupInitX, initPos.BladeSetupInitY, initPos.BladeSetupInitZ1, initPos.BladeSetupInitZ2);
                    await PlcControl.tagControl.bladeMantance.SetNoContactHeightMeasurement(1);
                    break;
                //非接触测高
                case HeightMeasurementMode.NoContact:
                    await PlcControl.tagControl.bladeMantance.SetBladeSetuInitPositionAsync(initPos.NoContactBladeSetupInitX, initPos.NoContactBladeSetupInitY, initPos.NoContactBladeSetupInitZ1, initPos.NoContactBladeSetupInitZ2);
                    await PlcControl.tagControl.bladeMantance.SetNoContactHeightMeasurement(0);
                    break;
                default:
                    break;
            }
            //关闭切割水
            await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
            //进入测高模式
            await PlcControl.tagControl.bladeMantance.RunBladeSetupAsync(1);
            //等待测高准备完成信号
            await TaskUtils.WaitExpectedResultAsync(IsReadyToMeasureHeightAsync, true, token);
            BladeHeightModel bladeHeightModel;
            //测高参数的数据
            List<BladeHeightModel> list = await SqlHelper.TableAsync<BladeHeightModel>()
                    .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list == null || list.Count == 0)
            {
                MaterialSnackUtils.MaterialSnack("获取测高参数失败！", MaterialSnackUtils.SnackType.ERROR);
                return null;
            }
            bladeHeightModel = list[0];
            if (!int.TryParse(bladeHeightModel.Retry, out int retry))
            {
                MaterialSnackUtils.MaterialSnack("测高参数异常！", MaterialSnackUtils.SnackType.ERROR);
                return null;
            }
            // 发送测高开始信号到PLC
            await PlcControl.tagControl.bladeMantance.StartSetupAsync();
            List<float> setupValueList = new List<float>();
            int measureHeightTimes = int.Parse(await PlcControl.plc.GetPlcValueStringAsync(DeviceKey.setupNumberKey));
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
            while (measureHeightTimes < retry && await timer.WaitForNextTickAsync(token))
            {
                int curMeasureHeightTimes = int.Parse(await PlcControl.plc.GetPlcValueStringAsync(DeviceKey.setupNumberKey));
                // 如果不相等，则记录值
                if (curMeasureHeightTimes != measureHeightTimes)
                {
                    string setupValue = await PlcControl.plc.GetPlcValueStringAsync(DeviceKey.setupValueKey);
                    Tools.LogDebug($"第{curMeasureHeightTimes}次测高：{setupValue}");
                    setupValueList.Add(Tools.GetFloatStringValue(setupValue));
                    measureHeightTimes = curMeasureHeightTimes;
                }
            }
            if (setupValueList.Count == 0)
            {
                return null;
            }
            //等待完成测高信号
            await TaskUtils.WaitExpectedResultAsync(IsCompletedMeasureHeightAsync, true, token);
            Tools.LogDebug($"测高平均值：{setupValueList.Average()}");
            await WaitAllAxisStopAsync(token);
            //抬起Z1轴
            await PlcControl.tagControl.Z1axis.StartAbsoluteAsync(20, 5, token);
            // 计算3次的平均值，为测高值
            return setupValueList.Average();
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

        // 判断测高是否完成
        public static async Task<bool> IsCompletedMeasureHeightAsync()
        {
            return await PlcControl.plc.ReadDataAsync(PlcControl.tagControl.bladeMantance.HeightMeasurementCompleted.addr) ?? false;
        }

        // 判断是否已准备好测高
        public static async Task<bool> IsReadyToMeasureHeightAsync()
        {
            return await PlcControl.plc.ReadDataAsync(PlcControl.tagControl.bladeMantance.bladeMantanceStatus.addr) ?? false;
        }

        /// <summary>
        /// 切割校准
        /// </summary>
        /// <returns>theta轴角度</returns>
        public static async Task<float> CalibratCutAsync(DataPoint<float> workpieceCenterPoint, float workpieceRadius, CancellationToken token)
        {
            return 0;
            await WaitAllAxisStopAsync(token);
            await PlcControl.tagControl.Yaxis.StartAbsoluteAsync(50, workpieceCenterPoint.Y + 10, token);
            await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(50, workpieceCenterPoint.X - workpieceRadius, token);
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token);
            CancellationToken linkedToken = linkedCts.Token;
            Task slowSpeedMoveTask = PlcControl.tagControl.Xaxis.StartAbsoluteAsync(7f, workpieceCenterPoint.X + workpieceRadius, linkedToken);
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
            await PlcControl.tagControl.Yaxis.StartAbsoluteAsync(50, sharpenRect.Bottom - 10, token);
            await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(50, sharpenRect.X, token);
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token);
            CancellationToken linkedToken = linkedCts.Token;
            Task slowSpeedMoveTask = PlcControl.tagControl.Xaxis.StartAbsoluteAsync(7, sharpenRect.X + sharpenRect.Width, token);
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
            Task taskX = PlcControl.tagControl.Xaxis.WatiSpeedZeroAsync(token);
            Task taskY = PlcControl.tagControl.Yaxis.WatiSpeedZeroAsync(token);
            Task taskZ1 = PlcControl.tagControl.Z1axis.WatiSpeedZeroAsync(token);
            Task taskZ2 = PlcControl.tagControl.Z2axis.WatiSpeedZeroAsync(token);
            await Task.WhenAll(taskWhole, taskX, taskY, taskZ1, taskZ2);
        }

        public static async Task<float?> AutoFocusAsync(CancellationToken token)
        {
            //await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(50, GlobalParams.CameraCenterPoint.X, token);
            //await PlcControl.tagControl.Yaxis.StartAbsoluteAsync(50, GlobalParams.CameraCenterPoint.Y + 20, token);
            MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null || !CommonCheck.AxisReady(false))
            {
                return null;
            }
            // 获取相机页面
            List<CameraCommon> cameraCommons = Tools.GetChildrenOfType<CameraCommon>(mainWindow.mainFrame);
            if (cameraCommons.Count == 0)
            { 
                MaterialSnackUtils.MaterialSnack("相机获取失败！", MaterialSnackUtils.SnackType.WARNING);
                return null;
            }
            CameraCommon cameraCommon = cameraCommons[0];
            // 获取当前配置的工作盘和膜的厚度
            FileTableItemModel fileTableItemModel = CurrentUtils.GetFileTableItemModel();
            // 获取工件的厚度和膜的厚度
            float workThickness = float.Parse(fileTableItemModel.WorkThickness);
            float tapeThickness = float.Parse(fileTableItemModel.TapeThickness);
            float z2DefaultSpeed = float.Parse(GlobalParams.z2DefaultSpeed);
            float startPositionZ2 = GlobalParams.AutoFocusStartPositionZ2;
            await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(z2DefaultSpeed, startPositionZ2, token);
            float? z2CurLocation = await PlcControl.tagControl.Z2axis.GetCurrentLocationAsync();
            if (z2CurLocation != null)
            {
                float start = 0.01f;
                float end = 0.5f;
                float increment = 0.01f;
                HashSet<ImageData> dataSet = new HashSet<ImageData>();
                float lastBlurriness = 0;
                float lastPosition = 0;
                // 增加动态调整步进增量的逻辑
                float dynamicIncrement = increment;  // 初始步进增量
                for (float i = start; i < end; i += dynamicIncrement)
                {
                    // 执行你的操作
                    float newPosition = startPositionZ2 + i;
                    await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(z2DefaultSpeed, newPosition, token);
                    if (cameraCommon.localBitmap != null)
                    {
                        float tenengradBlurriness = (float)VisualUtils.CalculateTenengrad2(cameraCommon.localBitmap);
                        Tools.LogInfo("当前位置：" + newPosition + " 当前模糊度：" + tenengradBlurriness);
                        if (lastBlurriness > 0 && lastBlurriness - tenengradBlurriness > 0.5)
                        {
                            // 找到最清晰的位置，停止循环并移动到上一个位置
                            Tools.LogInfo("最清晰的图片已找到，停止当前对焦并返回到上一个位置");
                            // 调用plc方法，走到上一个位置
                            await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(z2DefaultSpeed, lastPosition, token);
                            return lastPosition;
                        }
                        if (tenengradBlurriness < 10)
                        {
                            dynamicIncrement = 0.05f;
                        }
                        else if (tenengradBlurriness < 20)
                        {
                            dynamicIncrement = 0.03f;
                        }
                        else
                        {
                            dynamicIncrement = increment;
                        }
                        lastBlurriness = tenengradBlurriness;
                        lastPosition = newPosition;
                    }
                    else
                    {
                        Tools.LogInfo("聚焦获取当前帧失败！");
                    }
                }
            }
            return null;
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
            return 0.1f;
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

        public static float GetCuttingZ(string bladeType)
        {
            return 0.1f;
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
        public static async Task<bool> CheckKnifeMarksStatus(LineSegment line, float focusClearZ2, CancellationToken token)
        {
            //return true;
            DataPoint<float> relativePos = GlobalParams.CameraRelativeBladePosition;
            await WaitAllAxisStopAsync(token);
            await PlcControl.tagControl.Z1axis.StartAbsoluteAsync(10f, 0, token);
            await PlcControl.tagControl.Yaxis.StartAbsoluteAsync(10f, line.StartPoint.Y + relativePos.Y, token);
            await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(10f, (line.StartPoint.X + line.EndPoint.X) / 2 + relativePos.X, token);
            await AutoFocusAsync(token);
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token);
            CancellationToken linkedToken = linkedCts.Token;
            Task slowSpeedMoveTask = PlcControl.tagControl.Xaxis.StartAbsoluteAsync(7f, line.StartPoint.X + relativePos.X, linkedToken);
            Task<List<Mat>> grabTimerTask = Task.Run(async () =>
            {
                List<Mat> mats = new List<Mat>();
                try
                {
                    using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
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
            await ProcessImagesOptimizedAsync(mats, token);
            return true;
        }

        public static async Task ProcessImagesOptimizedAsync(List<Mat> bitmaps, CancellationToken token)
        {
            // 配置参数
            const int ioMaxConcurrency = 4; // 机械硬盘建议 2，SSD 可提高到 4-8
            Task ioTask = Task.Run(async () =>
            {
                using var semaphore = new SemaphoreSlim(ioMaxConcurrency);
                var tasks = new List<Task>();
                foreach (var mat in bitmaps)
                {
                    token.ThrowIfCancellationRequested();
                    //tasks.Add(SaveImageDataAsync($"C:\\Users\\17632\\Desktop\\image\\{DateTime.Now.Ticks}_mat.jpg", mat, semaphore, token));
                    Mat cropMat = CropHorizontalCenter(mat, (int)(mat.Height * 0.13));
                    //tasks.Add(SaveImageDataAsync($"C:\\Users\\17632\\Desktop\\image\\{DateTime.Now.Ticks}_cropMat.jpg", cropMat, semaphore, token));
                    Mat cropMatJpg = JpegStreamToMat(MatToJpegStream(cropMat));
                    //tasks.Add(SaveImageDataAsync($"C:\\Users\\17632\\Desktop\\image\\{DateTime.Now.Ticks}_cropMatJpg.jpg", cropMatJpg, semaphore, token));
                    var (bladeWidthMm, collapseWidthMm) = VisionAnalyzer.ProcessImage(cropMatJpg);
                    Cv2.PutText(cropMatJpg, $"bladeWidthMm: {bladeWidthMm} collapseWidthMm:{collapseWidthMm}", new OpenCvSharp.Point(10, 40), HersheyFonts.HersheySimplex, 1.5f, new Scalar(0, 0, 255));
                    tasks.Add(SaveImageDataAsync($"C:\\Users\\17632\\Desktop\\image\\{DateTime.Now.Ticks}_cropMatJpgText.jpg", cropMatJpg, semaphore, token));
                }
                await Task.WhenAll(tasks);
            }, token);

            await ioTask;
        }

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

        public static void AddTextToImage(string imagePath, string outputPath, string text)
        {
            // 1. 加载图片
            using (var image = Image.FromFile(imagePath))
            using (var graphics = Graphics.FromImage(image))
            {
                // 2. 配置文字样式
                var font = new Font("Arial", 20, System.Drawing.FontStyle.Bold);
                var brush = new SolidBrush(System.Drawing.Color.Red);
                var point = new PointF(10, 10); // 文字位置

                // 3. 绘制文字
                graphics.DrawString(text, font, brush, point);

                // 4. 保存图片
                image.Save(outputPath, ImageFormat.Jpeg);
            }
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
