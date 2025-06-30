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
using 精密切割系统.PubSubEvent;
using 精密切割系统.Model.common;
using System.Windows.Interop;
using Prism.Events;
using 精密切割系统.View.Dialogs;
using 精密切割系统.Extensions;
using static SQLite.SQLite3;
using Point = OpenCvSharp.Point;
using System.Windows.Shapes;
using System.Diagnostics;
using 精密切割系统.Model.MeasureHeight;

namespace 精密切割系统.Model.cut
{
    public class AutoCutUtils
    {
        public const int HeightRange = 240;

        /// <summary>
        /// 换刀片
        /// </summary>
        /// <returns></returns>
        public static async Task ReplaceBladeAsync(IEventAggregator? eventAggregator = null)
        {
            MaterialSnackUtils.MaterialSnack("请准备更换刀片,轴运动中！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
            await PlcControl.tagControl.wholeDevice.StopSpindleAsync();
            Task taskZ1 = PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0);
            Task taskZ2 = PlcControl.tagControl.Z2axis.StartAbsoluteAsync(0);
            await Task.WhenAll(taskZ1, taskZ2);
            Task taskXY = PlcControl.tagControl.cutting.RunMotionAsync(0, 150);
            Task taskTheta = PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(0);
            Task speedZero = PlcControl.tagControl.wholeDevice.WaitSpindleSpeedToZeroAsync();
            await Task.WhenAll(taskXY, taskTheta, speedZero);
            Appsettings.AfterReplaceBladeCutTimes = 0;
            MaterialSnackUtils.MaterialSnack("请更换刀片！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
        }

        /// <summary>
        /// 换磨刀板
        /// </summary>
        /// <returns></returns>
        public static async Task ReplaceSharpeningBoardAsync(IEventAggregator? eventAggregator = null)
        {
            MaterialSnackUtils.MaterialSnack("请准备更换磨刀板,轴运动中！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
            await PlcControl.tagControl.wholeDevice.StopSpindleAsync();
            Task taskZ1 = PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0);
            Task taskZ2 = PlcControl.tagControl.Z2axis.StartAbsoluteAsync(0);
            await Task.WhenAll(taskZ1, taskZ2);
            Task taskXY = PlcControl.tagControl.cutting.RunMotionAsync(0, 0);
            Task taskTheta = PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(0);
            Task speedZero = PlcControl.tagControl.wholeDevice.WaitSpindleSpeedToZeroAsync();
            await Task.WhenAll(taskXY, taskTheta, speedZero);
            //清空记录
            Appsettings.SharpenY = null;
            Appsettings.SharpenThetaDegQueue = null;
            Appsettings.SharpenDistance = null;
            MaterialSnackUtils.MaterialSnack("请更换磨刀板！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
        }

        /// <summary>
        /// 换硅片
        /// </summary>
        /// <returns></returns>
        public static async Task ReplaceWaferAsync(IEventAggregator? eventAggregator = null)
        {
            MaterialSnackUtils.MaterialSnack("请准备更换硅片,轴运动中！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
            await PlcControl.tagControl.wholeDevice.StopSpindleAsync();
            Task taskZ1 = PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0);
            Task taskZ2 = PlcControl.tagControl.Z2axis.StartAbsoluteAsync(0);
            await Task.WhenAll(taskZ1, taskZ2);
            Task taskXY = PlcControl.tagControl.cutting.RunMotionAsync(0, 0);
            Task taskTheta = PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(0);
            Task speedZero = PlcControl.tagControl.wholeDevice.WaitSpindleSpeedToZeroAsync();
            await Task.WhenAll(taskXY, taskTheta, speedZero);
            //清空记录
            Appsettings.CutY = null;
            Appsettings.CutThetaDegQueue = null;
            Appsettings.CutDistance = null;
            MaterialSnackUtils.MaterialSnack("请更换硅片！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
        }

        /// <summary>
        /// 执行测高
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<CommonResult<float>> ProcessMeasureHeightAsync(HeightMeasurementMode mode, IDialogService dialogService, IEventAggregator? eventAggregator = null, CancellationToken token = default)
        {
            InitialPositionModel? initPos = await GetInitialPositionAsync();
            if (initPos is null) return CommonResult<float>.Failure("获取初始化位置信息失败！");
            switch (mode)
            {
                //接触测高
                case HeightMeasurementMode.Contact:
                    int? thetaDeg = Appsettings.ContactHeightMeasurementThetaDeg;
                    if (thetaDeg == null)
                    {
                        return CommonResult<float>.Failure("接触测高配置文件异常！");
                    }
                    //Appsettings.ContactHeightMeasurementThetaDeg = thetaDeg.Value + 1;
                    Appsettings.ContactHeightMeasurementThetaDeg = 0;
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
            for (int times = 1; times <= 10; times++)
            {
                if (mode is HeightMeasurementMode.Contact)
                {
                    // 工作盘吹气
                    await WorkpieceBlowingAsync(eventAggregator, token);
                }
                if (mode is HeightMeasurementMode.NoContact)
                {
                    //主轴旋转
                    eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("主轴开始旋转"));
                    await PlcControl.tagControl.wholeDevice.StartSpindleAsync();
                    //eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("光纤传感器开始吹水"));
                    //await PlcControl.tagControl.bladeMantance.OpenOpticalFiberSensorBlowingWaterAsync(2);
                    eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("光纤传感器开始吹气"));
                    //await PlcControl.tagControl.cutting.RunMotionAsync(128f, 50, token);
                    //await PlcControl.tagControl.bladeMantance.OpenOpticalFiberSensorBlowingAsync(15);
                    float startBlowX = 127f, endBlowX = 135f;
                    //测高前移动到初始位置，主轴旋转，开始吹水吹气
                    await PlcControl.tagControl.cutting.RunMotionAsync(startBlowX, 50, token);
                    await PlcControl.tagControl.bladeMantance.OpenOpticalFiberSensorBlowingAsync();
                    for (int count = 0; count < 5; count++)
                    {
                        await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(endBlowX, 5);
                        await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(startBlowX, 5);
                    }
                    await PlcControl.tagControl.bladeMantance.CloseOpticalFiberSensorBlowingAsync();
                    // 初始化
                    await PlcControl.tagControl.Xaxis.StartHomingAsync();
                    await PlcControl.tagControl.Xaxis.WaitAxisReadyAsync(token);
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
                    return CommonResult<float>.Failure("获取测高参数失败！");
                }
                bladeHeightModel = list[0];
                if (!int.TryParse(bladeHeightModel.Retry, out int retry))
                {
                    return CommonResult<float>.Failure("测高参数异常！");
                }
                // 发送测高开始信号到PLC
                await PlcControl.tagControl.bladeMantance.StartSetupAsync();
                List<float> setupValueList = new List<float>();
                int? measureHeightTimes = await PlcControl.tagControl.bladeMantance.GetHeightMeasureSetupNumberAsync();
                if (measureHeightTimes == null)
                {
                    return CommonResult<float>.Failure("测高次数获取失败！");
                }
                int curMeasureHeightTimes = measureHeightTimes.Value;
                while (curMeasureHeightTimes < retry)
                {
                    curMeasureHeightTimes++;
                    await PlcControl.tagControl.bladeMantance.WaitHeightMeasureSetupNumberUdatedAsync(curMeasureHeightTimes, token);
                    float? setupValue = await PlcControl.tagControl.bladeMantance.GetHeightMeasurementSetupValue();
                    if (setupValue == null)
                    {
                        return CommonResult<float>.Failure("测高值获取失败！");
                    }
                    eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"第{curMeasureHeightTimes}次测高：{setupValue.Value}"));
                    setupValueList.Add(setupValue.Value);
                    // 设置下次测高最大距离，优化流程时间
                    await PlcControl.tagControl.bladeMantance.SetZAxisMaxDistanceAsync(setupValue.Value - 0.15f);
                }
                if (setupValueList.Count == 0)
                {
                    return CommonResult<float>.Failure("没有测高数据！");
                }
                // 计算平均值，为测高值
                float measureHeightAve = setupValueList.Average();
                //等待完成测高信号
                await PlcControl.tagControl.bladeMantance.WaitHeightMeasurementCompletedAsync(token);
                eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"测高平均值：{setupValueList.Average()}"));
                float maxDeviation = setupValueList.Max() - setupValueList.Min();
                eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"测高最大偏差：{Math.Round(maxDeviation * 1000, 1)} um"));
                // 测高数据异常处理
                if (maxDeviation >= 0.01)
                {
                    eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"测高偏差过大，重新测高"));
                    if (times % 3 == 0)
                    {
                        await WaitManualBlowing(dialogService, token);
                    }
                    continue;
                }
                if (mode == HeightMeasurementMode.Contact && measureHeightAve < 17)
                {
                    eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("测高数据异常，重新测高"));
                    continue;
                }
                return CommonResult<float>.Success(measureHeightAve);
            }
            return CommonResult<float>.Failure("测高失败次数过多！");
        }

        //public static async Task<CommonResult<float>> ProcessMeasureWearAmountAsync(HeightMeasurementMode mode, bool isFirst, IDialogService dialogService, IEventAggregator? eventAggregator = null, CancellationToken token = default)
        //{
        //    InitialPositionModel? initPos = await GetInitialPositionAsync();
        //    if (initPos is null) return CommonResult<float>.Failure("获取初始化位置信息失败！");
        //    switch (mode)
        //    {
        //        //接触测高
        //        case HeightMeasurementMode.Contact:
        //            int? thetaDeg = Appsettings.ContactHeightMeasurementThetaDeg;
        //            if (thetaDeg == null)
        //            {
        //                return CommonResult<float>.Failure("接触测高配置文件异常！");
        //            }
        //            Appsettings.ContactHeightMeasurementThetaDeg = thetaDeg.Value + 1;
        //            await PlcControl.tagControl.bladeMantance.SetBladeSetuInitPositionAsync(initPos.BladeSetupInitX, initPos.BladeSetupInitY, thetaDeg.Value);
        //            await PlcControl.tagControl.bladeMantance.StartContactHeightMeasurement();
        //            break;
        //        //非接触测高
        //        case HeightMeasurementMode.NoContact:
        //            await PlcControl.tagControl.bladeMantance.SetBladeSetuInitPositionAsync(initPos.NoContactBladeSetupInitX, initPos.NoContactBladeSetupInitY);
        //            await PlcControl.tagControl.bladeMantance.StartNoContactHeightMeasurement();
        //            if (isFirst)
        //            {
        //                await PlcControl.tagControl.bladeMantance.SetFirstMeasureHight();
        //            }
        //            break;
        //        default:
        //            break;
        //    }
        //    if (mode is HeightMeasurementMode.NoContact)
        //    {
        //        //主轴旋转
        //        eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("主轴开始旋转"));
        //        await PlcControl.tagControl.wholeDevice.StartSpindleAsync();
        //        //eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("光纤传感器开始吹水"));
        //        //await PlcControl.tagControl.bladeMantance.OpenOpticalFiberSensorBlowingWaterAsync(2);
        //        eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("光纤传感器开始吹气"));
        //        await PlcControl.tagControl.cutting.RunMotionAsync(128f, 50, token);
        //        Task blowingTask = PlcControl.tagControl.bladeMantance.OpenOpticalFiberSensorBlowingAsync(15);
        //        Task z1StartHomingTask = PlcControl.tagControl.Z1axis.StartHomingAsync();
        //        Task z1WaitReadyTask = PlcControl.tagControl.Z1axis.WaitAxisReadyAsync(token);
        //        await Task.WhenAll(blowingTask, z1StartHomingTask, z1WaitReadyTask);
        //    }

        //    //等待测高准备完成信号
        //    await PlcControl.tagControl.bladeMantance.WaitReadyToMeasureHeightAsync(token);
        //    //进入测高模式
        //    await PlcControl.tagControl.bladeMantance.StartBladeSetupAsync();
        //    eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("开始测高！"));
        //    BladeHeightModel bladeHeightModel;
        //    //测高参数的数据
        //    List<BladeHeightModel> list = await SqlHelper.TableAsync<BladeHeightModel>()
        //            .Where(t => t.Id == 1).ToListAsync();
        //    //数据不存在，则初始化数据
        //    if (list == null || list.Count == 0)
        //    {
        //        return CommonResult<float>.Failure("获取测高参数失败！");
        //    }
        //    bladeHeightModel = list[0];
        //    if (!int.TryParse(bladeHeightModel.Retry, out int retry))
        //    {
        //        return CommonResult<float>.Failure("测高参数异常！");
        //    }
        //    // 发送测高开始信号到PLC
        //    await PlcControl.tagControl.bladeMantance.StartSetupAsync();
        //    List<float> setupValueList = new List<float>();
        //    int? measureHeightTimes = await PlcControl.tagControl.bladeMantance.GetHeightMeasurementSetupNumber();
        //    if (measureHeightTimes == null)
        //    {
        //        return CommonResult<float>.Failure("重复次数获取失败！");
        //    }
        //    using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
        //    while (measureHeightTimes.Value < retry && await timer.WaitForNextTickAsync(token))
        //    {
        //        int? curMeasureHeightTimes = await PlcControl.tagControl.bladeMantance.GetHeightMeasurementSetupNumber();
        //        if (curMeasureHeightTimes == null)
        //        {
        //            return CommonResult<float>.Failure("重复次数获取失败！");
        //        }
        //        // 如果不相等，则记录值
        //        if (curMeasureHeightTimes.Value != measureHeightTimes.Value)
        //        {
        //            float? setupValue = await PlcControl.tagControl.bladeMantance.GetHeightMeasurementSetupValue();
        //            if (setupValue == null)
        //            {
        //                return CommonResult<float>.Failure("磨损量获取失败！");
        //            }
        //            eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"第{curMeasureHeightTimes.Value}次获取磨损量：{setupValue.Value}"));
        //            setupValueList.Add(setupValue.Value);
        //            measureHeightTimes = curMeasureHeightTimes;
        //        }
        //    }
        //    if (setupValueList.Count == 0)
        //    {
        //        return CommonResult<float>.Failure("没有磨损量数据！");
        //    }
        //    //等待完成测高信号
        //    await PlcControl.tagControl.bladeMantance.WaitHeightMeasurementCompletedAsync(token);
        //    eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"磨损量平均值：{setupValueList.Average()}"));
        //    // 计算3次的平均值，为测高值
        //    return CommonResult<float>.Success(setupValueList.Average());
        //}

        public static async Task WaitManualBlowing(IDialogService dialogService, CancellationToken token)
        {
            await PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0, default, token);
            await PlcControl.tagControl.cutting.RunMotionAsync(100, 0, token);
            await PlcControl.tagControl.wholeDevice.OpenBuzzerAsync();
            await dialogService.ShowDialogWindowAsync(nameof(ConfirmDialog), new DialogParameters() { { "ButtonContent", "测高多次失败，请手动吹水!" } }, r => { }, nameof(ConfirmDialogWindow));
            await PlcControl.tagControl.wholeDevice.CloseBuzzerAsync();
        }

        /// <summary>
        /// 预切割序列
        /// </summary>
        public static async Task<List<float>?> GetCutListAsync(string lunguId, float sydrcd)
        {
            // 查询当前配置获取预切割开始编号
            FileTableItemModel fileTableItemModel = CurrentUtils.GetFileTableItemModel();
            // 查询当前预切割流程信息
            PreCutModel preCutModel = CurrentUtils.GetPreCutModel();
            if (preCutModel.NewBladeNo == 0)
            {
                return null;
            }
            // 获取
            float[] feedSpds = Tools.StringToFloatArray(preCutModel.FeedSpd); // 获取进刀速度
            float[] ofLinesList = Tools.StringToFloatArray(preCutModel.OfLines); // 获取切割刀数
            float cutSpeed = await CutService.GetCutSpeed(lunguId, sydrcd);
            List<float> cutSpeedList = new List<float>();
            // 从预切割开始编号开始
            for (int i = preCutModel.NewBladeNo; i <= feedSpds.Length; i++)
            {
                // 获取进刀速度
                float feedspeed = feedSpds[i - 1];
                float cutLine = ofLinesList[i - 1];
                if (feedspeed != 0 && cutLine != 0 && feedspeed <= cutSpeed)
                {
                    for (int j = 0; j < cutLine; j++)
                    {
                        cutSpeedList.Add(feedspeed);
                    }
                }
            }
            return cutSpeedList;
        }

        /// <summary>
        /// 获取总切割刀数
        /// </summary>
        public static async Task<int?> GetTotalCutTimesAsync(string lunguId, float sydrcd)
        {
            // 查询当前配置获取预切割开始编号
            FileTableItemModel fileTableItemModel = CurrentUtils.GetFileTableItemModel();
            // 查询当前预切割流程信息
            PreCutModel preCutModel = CurrentUtils.GetPreCutModel();
            if (preCutModel.NewBladeNo == 0)
            {
                return null;
            }
            // 获取
            float[] feedSpds = Tools.StringToFloatArray(preCutModel.FeedSpd); // 获取进刀速度
            float[] ofLinesList = Tools.StringToFloatArray(preCutModel.OfLines); // 获取切割刀数
            float cutSpeed = await CutService.GetCutSpeed(lunguId, sydrcd);
            int totalCutTimes = 0;
            // 从预切割开始编号开始
            for (int i = preCutModel.NewBladeNo; i <= feedSpds.Length; i++)
            {
                // 获取进刀速度
                float feedspeed = feedSpds[i - 1];
                float cutLine = ofLinesList[i - 1];
                if (feedspeed != 0 && cutLine != 0 && feedspeed <= cutSpeed)
                {
                    totalCutTimes += (int)cutLine;
                }
            }
            return totalCutTimes;
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

        public static CameraCommon? GetCameraCommon()
        {
            MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null)
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

        public static async Task GoPreCutLineAsync(CancellationToken token)
        {
            DataPoint<float> cameraCenterPoint = GlobalParams.CameraCenterPoint;
            DataPoint<float> cameraRelativeBladePosition = Appsettings.CameraRelativeBladePosition;
            float thetaDeg = Appsettings.CutThetaDegQueue is not null && Appsettings.CutThetaDegQueue.Count > 0 ? Appsettings.CutThetaDegQueue.First() : 0;
            Task focusxyTask = PlcControl.tagControl.cutting.RunMotionAsync(cameraCenterPoint.X - 10, cameraRelativeBladePosition.Y + Appsettings.CutY ?? cameraCenterPoint.Y + 30, token);
            Task focusThetaTask = PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(thetaDeg);
            await Task.WhenAll(focusxyTask, focusThetaTask);
        }

        public static async Task<CommonResult<float>> AutoFocusAsync(IEventAggregator? eventAggregator = null, CancellationToken token = default)
        {
            eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("开始相机对焦..."));
            CameraCommon? cameraCommon = GetCameraCommon();
            if (cameraCommon is null)
            {
                return CommonResult<float>.Failure("相机获取失败！");
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
                    Appsettings.IsNeedCheckBaseLine = false;
                    return CommonResult<float>.Success(focusClearZ);
                }
            }
            Appsettings.IsNeedCheckBaseLine = true;
            CommonResult<float> roughFocusPosition = await AutoFocusAsync(cameraCommon, focusClearZ, 0.5f, 0.05f, token, eventAggregator);
            if (!roughFocusPosition.IsSuccess)
            {
                return CommonResult<float>.Failure("粗调聚焦失败！");
            }
            // 进行精调聚焦
            return await AutoFocusAsync(cameraCommon, roughFocusPosition.Data, 0.05f, 0.01f, token, eventAggregator);
        }

        private static async Task<CommonResult<float>> AutoFocusAsync(CameraCommon cameraCommon, float startPositionZ2, float margin, float singleMoveDistance, CancellationToken token, IEventAggregator? eventAggregator = null)
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
                        return CommonResult<float>.Success(lastPosition);
                    }
                    lastBlurriness = tenengradBlurriness;
                    lastPosition = newPosition;
                }
                else
                {
                    eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("聚焦获取当前帧失败！"));
                }
            }
            return CommonResult<float>.Failure("聚焦失败！");
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
                await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(190, 100, token);
                await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(2, 15, token);
            }
            finally
            {
                await PlcControl.tagControl.wholeDevice.CloseWorkpieceBlowingAsync();
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
            //float wearAmount = Math.Abs(curHeightZ - firstHeightMeasurementZ);
            //if (float.TryParse(lunguSksj.BladeOuterDiameter, out float bladeOuterDiameter))
            //{
            //    bool isMeetCutting = true;
            //    //磨刀后当前刀刃长度 <=【刀刃蚀刻后最长暴露量范围】最大值
            //    isMeetCutting = isMeetCutting && bladeOuterDiameter - wearAmount <= lunguSksj.LongestBlade;
            //    //长宽比 <=【刀刃初始长宽比范围】最大值

            //    //刀厚 > =21，单次磨损量<=20；刀厚<21，单次磨损量<=50

            //    return isMeetCutting;
            //}
            //return false;
        }

        /// <summary>
        /// 检查刀痕状态
        /// </summary>
        /// <returns></returns>
        public static async Task<ImagesAnalysisResult> CheckKnifeMarksStatus(LineSegment line, IEventAggregator? eventAggregator = null, CancellationToken token = default)
        {
            ConcurrentQueue<Mat> matQueue = new ConcurrentQueue<Mat>();
            try
            {
                DataPoint<float> relativePos = Appsettings.CameraRelativeBladePosition;
                //工件吹气
                eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("开始工件吹气..."));
                float rightCheckX = line.EndPoint.X + relativePos.X - 20;
                float leftCheckX = line.StartPoint.X + relativePos.X + 20;
                float checkY = line.EndPoint.Y + relativePos.Y;
                await PlcControl.tagControl.wholeDevice.OpenWorkpieceBlowingAsync();
                await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(190, 80, token);
                await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(rightCheckX, 7f, token);
                await PlcControl.tagControl.Yaxis.StartAbsoluteAsync(checkY, 50, token);
                //await AutoFocusAsync(eventAggregator, token);
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken linkedToken = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token).Token;
                Task slowSpeedMoveTask = PlcControl.tagControl.Xaxis.StartAbsoluteAsync(leftCheckX, 7f, linkedToken);
                Task grabTimerTask = Task.Run(async () =>
                {
                    try
                    {
                        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(300));
                        while (await timer.WaitForNextTickAsync(linkedToken))
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                WriteableBitmap? localBitmap = GrabWriteableBitmap();
                                if (localBitmap != null)
                                {
                                    matQueue.Enqueue(localBitmap.ToMat());
                                }
                            });
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        //正常取消任务
                    }
                });
                CancellationTokenSource analysisCts = new CancellationTokenSource();
                Task<ImagesAnalysisResult> analysisTask = ProcessImagesAnalysisAsync(matQueue, eventAggregator, analysisCts.Token);
                await Task.WhenAny(slowSpeedMoveTask, grabTimerTask, analysisTask);
                cts.Cancel();
                await grabTimerTask;
                analysisCts.Cancel();
                return await analysisTask;
            }
            finally
            {
                //保证工作盘吹气关闭
                await PlcControl.tagControl.wholeDevice.CloseWorkpieceBlowingAsync();
            }
        }

        private static void ProcessMat(Mat mat, ImagesAnalysisResult result)
        {
            Mat cropMat = CropHorizontalCenter(mat, HeightRange);
            Mat cropMatJpg = JpegStreamToMat(MatToJpegStream(cropMat));
            Mat originMatJpg = cropMatJpg.Clone();
            var (bladeWidthMm, collapseWidthMm, bladeTop, bladeBottom, collapseTop, collapseBottom) = VisionAnalyzer.ProcessImage(cropMatJpg);
            Cv2.PutText(cropMatJpg,
                $"bladeWidthMm: {bladeWidthMm}",
                new Point(20, bladeTop),
                HersheyFonts.HersheySimplex,
                1.3f,
                Scalar.Red,
                2);
            Cv2.PutText(cropMatJpg,
                $"collapseWidthMm:{collapseWidthMm}",
                new Point(900, collapseTop),
                HersheyFonts.HersheySimplex,
                1.3f,
                Scalar.Green,
                2);
            Cv2.PutText(cropMatJpg,
                $"No: {result.ImageDatas.Count}",
                new Point(900, (bladeTop + bladeBottom) / 2),
                HersheyFonts.HersheySimplex,
                1.3f,
                Scalar.Blue,
                2);
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
                Mat = cropMatJpg,
                OriginMat = originMatJpg
            };
            result.ImageDatas.Add(imageData);
            if (result.BladeWidthMaxImage.BladeWidth < bladeWidthMm)
                result.BladeWidthMaxImage = imageData;
            if (result.CollapseWidthMaxImage.CollapseWidth < collapseWidthMm)
                result.CollapseWidthMaxImage = imageData;
        }

        public static async Task<ImagesAnalysisResult> ProcessImagesAnalysisAsync(ConcurrentQueue<Mat> matQueue, IEventAggregator? eventAggregator = null, CancellationToken token = default)
        {
            ImagesAnalysisResult result = new ImagesAnalysisResult();
            result.IsSuccess = true;
            await Task.Run(async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                List<Mat> mats = new List<Mat>();
                try
                {
                    using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
                    while (await timer.WaitForNextTickAsync(token))
                    {
                        while(matQueue.TryDequeue(out Mat? mat))
                        {
                            mats.Add(mat);
                            ProcessMat(mat, result);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // 可能剩余的图像进行处理
                    while (matQueue.TryDequeue(out Mat? mat))
                    {
                        mats.Add(mat);
                        ProcessMat(mat, result);
                    }
                }
                stopwatch.Stop();
                eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"识别所有单张图像用时: {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} 秒"));
                stopwatch = Stopwatch.StartNew();
                // 拼接所有图像
                List<Mat> concatMats = ConcatMats(mats);
                stopwatch.Stop();
                eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"拼接所有图像用时: {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} 秒"));

                #region 保存图片
                stopwatch = Stopwatch.StartNew();
                // 保存拼接图像到指定目录
                string uuid = Guid.NewGuid().ToString();
                string imagePath = System.IO.Path.Combine(AppContext.BaseDirectory, $"image\\{DateTime.Now.Ticks}");
                Directory.CreateDirectory(imagePath);
                string concatImagesPath = System.IO.Path.Combine(imagePath, "ConcatImages");
                Directory.CreateDirectory(concatImagesPath);
                if (mats.Count == result.ImageDatas.Count)
                {
                    for (int i = 0; i < mats.Count; i++)
                    {
                        Cv2.ImWrite($"{imagePath}\\{uuid}_{i}_原图_{i}.jpg", mats[i]);
                        Cv2.ImWrite($"{imagePath}\\{uuid}_{i}_裁剪识别图_{i}.jpg", result.ImageDatas[i].Mat);
                    }
                }
                else
                {
                    eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"保存原图和识别后的图片失败！"));
                }
                foreach (var image in concatMats)
                {
                    Cv2.ImWrite($"{concatImagesPath}\\{DateTime.Now.Ticks}_拼接图原图.jpg", image);
                }
                stopwatch.Stop();
                eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"保存识别后的拼接图像总用时: {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} 秒"));
                #endregion

                foreach (Mat concatMat in concatMats)
                {
                    Mat cropConcatMatJpg = JpegStreamToMat(MatToJpegStream(CropHorizontalCenter(concatMat, HeightRange)));
                    try
                    {
                        stopwatch = Stopwatch.StartNew();
                        int? centerY = VisionAnalyzer.DetectFirstHorizontalStripeCenter(cropConcatMatJpg);
                        stopwatch.Stop();
                        eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"识别有无刀痕用时: {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} 秒"));
                        if (centerY is null)
                        {
                            result.AnalysisFailMats.Add(cropConcatMatJpg);
                            result.IsSuccess = false;
                            result.Message = string.IsNullOrEmpty(result.Message) ? "未识别到刀痕，请人工检查刀痕状态！" : result.Message;
                        }
                        else
                        {
                            stopwatch = Stopwatch.StartNew();
                            var (bladeWidthMm, collapseWidthMm, bladeTop, bladeBottom, collapseTop, collapseBottom) = VisionAnalyzer.ProcessImage(cropConcatMatJpg);
                            stopwatch.Stop();
                            eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"识别刀痕宽度和崩边用时: {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} 秒"));
                            Cv2.PutText(cropConcatMatJpg,
                                $"bladeWidthMm: {bladeWidthMm}",
                                new Point(20, bladeTop),
                                HersheyFonts.HersheySimplex,
                                1.3f,
                                Scalar.Red,
                                2);
                            Cv2.PutText(cropConcatMatJpg,
                                $"collapseWidthMm:{collapseWidthMm}",
                                new Point(900, collapseTop),
                                HersheyFonts.HersheySimplex,
                                1.3f,
                                Scalar.Green,
                                2);
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
                            stopwatch = Stopwatch.StartNew();
                            result.ConcatImages.Add(new ImageData()
                            {
                                BladeWidth = bladeWidthMm,
                                CollapseWidth = collapseWidthMm,
                                IsSnakelike = VisionAnalyzer.SnakeCase(cropConcatMatJpg).Snake,
                                Mat = cropConcatMatJpg
                            });
                            stopwatch.Stop();
                            eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"识别蛇形用时: {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} 秒"));
                        }
                    }
                    catch (Exception ex)
                    {
                        result.AnalysisFailMats.Add(cropConcatMatJpg);
                        result.IsSuccess = false;
                        result.Message = string.IsNullOrEmpty(result.Message) ? "图像识别: 刀痕异常，请人工检查刀痕状态！" : result.Message;
                        eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create(ex.Message));
                    }
                    finally
                    {
                        stopwatch.Stop();
                    }
                }
                double singleCollapse =(Math.Round(result.CollapseWidthMaxImage.CollapseWidth, 3) - Math.Round(result.CollapseWidthMaxImage.BladeWidth, 3)) / 2;
                if (singleCollapse > 10)
                {
                    result.IsSuccess = false;
                    result.Message = string.IsNullOrEmpty(result.Message) ? $"图像识别: 崩边过大，单边最大为 {singleCollapse}um ！" : result.Message;
                }
                if (result.HasSnakelike())
                {
                    result.IsSuccess = false;
                    result.Message = string.IsNullOrEmpty(result.Message) ? "图像识别: 刀痕刀痕为蛇形，请人工检查刀痕状态！" : result.Message;
                }

                #region 保存图片
                if (result.AnalysisFailMats.Count != 0)
                {
                    stopwatch = Stopwatch.StartNew();
                    foreach (var image in result.ConcatImages)
                    {
                        Cv2.ImWrite($"{concatImagesPath}\\{DateTime.Now.Ticks}_拼接图识别图_{(image.IsSnakelike ? "蛇形" : "正常")}.jpg", image.Mat);
                    }
                    //保存拼接图像到指定目录
                    string analysisFailMatsPath = System.IO.Path.Combine(imagePath, "AnalysisFail");
                    Directory.CreateDirectory(analysisFailMatsPath);
                    foreach (var failMat in result.AnalysisFailMats)
                    {
                        Cv2.ImWrite($"{analysisFailMatsPath}\\{DateTime.Now.Ticks}.jpg", failMat);
                    }
                    stopwatch.Stop();
                    eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"保存识别图像总用时: {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} 秒"));
                }
                #endregion
            });
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
