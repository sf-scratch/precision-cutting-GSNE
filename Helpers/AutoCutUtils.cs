using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.OpenXml4Net.OPC.Internal.Unmarshallers;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using Prism.Events;
using SciCamera.Net;
using SQLite;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using 精密切割系统.Data;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.DTOs;
using 精密切割系统.Extensions;
using 精密切割系统.HttpClients;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.MeasureHeight;
using 精密切割系统.Model.plc;
using 精密切割系统.Model.sqlite;
using 精密切割系统.PubSubEvent;
using 精密切割系统.Utils;
using 精密切割系统.View.Dialogs;
using 精密切割系统.View.Pages.common;
using 精密切割系统.ViewModel;
using static SQLite.SQLite3;
using ImageData = 精密切割系统.Model.cut.ImageData;
using LineSegment = 精密切割系统.Model.cut.LineSegment;
using Point = OpenCvSharp.Point;

namespace 精密切割系统.Helpers
{
    public class AutoCutUtils
    {
        public const int HeightRange = 240;
        public const int VisionSnakeHeightRange = 100;

        public static async Task<AxisPosition> GetAxisPositionAsync()
        {
            var positions = await Task.WhenAll(
                        PlcControl.tagControl.Xaxis.GetCurrentLocationAsync(),
                        PlcControl.tagControl.Yaxis.GetCurrentLocationAsync(),
                        PlcControl.tagControl.Z1axis.GetCurrentLocationAsync(),
                        PlcControl.tagControl.Z2axis.GetCurrentLocationAsync(),
                        PlcControl.tagControl.ThetaAxis.GetCurrentLocationAsync());
            return new AxisPosition(positions[0], positions[1], positions[2], positions[3], positions[4]);
        }

        public static async Task<AxisState> GetAxisStateAsync()
        {
            var states = await Task.WhenAll(
                        PlcControl.tagControl.Xaxis.IsReadyAsync(),
                        PlcControl.tagControl.Yaxis.IsReadyAsync(),
                        PlcControl.tagControl.Z1axis.IsReadyAsync(),
                        PlcControl.tagControl.Z2axis.IsReadyAsync(),
                        PlcControl.tagControl.ThetaAxis.IsReadyAsync());
            return new AxisState(states[0], states[1], states[2], states[3], states[4]);
        }

        /// <summary>
        /// 换刀片
        /// </summary>
        /// <returns></returns>
        public static async Task ReplaceBladeAsync(IEventAggregator? eventAggregator, CancellationToken token)
        {
            try
            {
                List<UserDefineDataModel> list = SqlHelper.Table<UserDefineDataModel>().ToList();
                if (list.Count != 1)
                {
                    MaterialSnackUtils.MaterialSnack("功能参数设定，雾化喷嘴位置设定错误！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
                    return;
                }
                UserDefineDataModel userDefineData = list.First();
                MaterialSnackUtils.MaterialSnack("请准备更换刀片,轴运动中！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
                await PlcControl.tagControl.wholeDevice.StopSpindleAsync();
                Task taskZ1 = PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0, default, token);
                Task taskZ2 = PlcControl.tagControl.Z2axis.StartAbsoluteAsync(0, default, token);
                await Task.WhenAll(taskZ1, taskZ2);
                Task taskXY = PlcControl.tagControl.cutting.RunMotionAsync(0, userDefineData.BladeExchangeYPos.ToFloat(), token);
                Task taskTheta = PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(0, default, token);
                Task speedZero = PlcControl.tagControl.wholeDevice.WaitSpindleSpeedToZeroAsync(token);
                await Task.WhenAll(taskXY, taskTheta, speedZero);
                Appsettings.AfterReplaceBladeCutTimes = 0;
                Appsettings.AfterReplaceBladeCutLength = 0;
                Appsettings.BladeOuterDiameter = null;
                Appsettings.BladeThickness = null;
                Appsettings.MeasureHeightFirst = null;
                Appsettings.MeasureHeightLast = null;
                MaterialSnackUtils.MaterialSnack("请打开切割安全门，更换刀片！", MaterialSnackUtils.SnackType.SUCCESS, 0, eventAggregator);
            }
            catch (OperationCanceledException)
            {
                MaterialSnackUtils.MaterialSnack("更换刀片操作取消！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
            }
        }

        /// <summary>
        /// 换磨刀板
        /// </summary>
        /// <returns></returns>
        public static async Task ReplaceSharpeningBoardAsync(IEventAggregator? eventAggregator, CancellationToken token)
        {
            try
            {
                MaterialSnackUtils.MaterialSnack("请准备更换磨刀板,轴运动中！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
                await PlcControl.tagControl.wholeDevice.StopSpindleAsync();
                Task taskZ1 = PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0, default, token);
                Task taskZ2 = PlcControl.tagControl.Z2axis.StartAbsoluteAsync(0, default, token);
                await Task.WhenAll(taskZ1, taskZ2);
                Task taskXY = PlcControl.tagControl.cutting.RunMotionAsync(0, 0, token);
                Task taskTheta = PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(0, default, token);
                Task speedZero = PlcControl.tagControl.wholeDevice.WaitSpindleSpeedToZeroAsync(token);
                await Task.WhenAll(taskXY, taskTheta, speedZero);
                MaterialSnackUtils.MaterialSnack("请打开相机安全门，更换磨刀板！", MaterialSnackUtils.SnackType.SUCCESS, 0, eventAggregator);
            }
            catch (OperationCanceledException)
            {
                MaterialSnackUtils.MaterialSnack("更换磨刀板操作失败！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
            }
        }

        /// <summary>
        /// 换磨刀板
        /// </summary>
        /// <returns></returns>
        public static async Task ReplaceSharpeningBoardAndResetAsync(IEventAggregator? eventAggregator, CancellationToken token)
        {
            //清空记录
            Appsettings.SharpenY = null;
            Appsettings.SharpenThetaDegList = null;
            Appsettings.SharpenDistance = null;
            await ReplaceSharpeningBoardAsync(eventAggregator, token);
        }

        /// <summary>
        /// 换硅片
        /// </summary>
        /// <returns></returns>
        public static async Task ReplaceWaferAsync(IEventAggregator? eventAggregator, CancellationToken token)
        {
            try
            {
                MaterialSnackUtils.MaterialSnack("请准备更换硅片,轴运动中！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
                //await PlcControl.tagControl.wholeDevice.StopSpindleAsync();
                Task taskZ1 = PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0, default, token);
                Task taskZ2 = PlcControl.tagControl.Z2axis.StartAbsoluteAsync(0, default, token);
                await Task.WhenAll(taskZ1, taskZ2);
                Task taskXY = PlcControl.tagControl.cutting.RunMotionAsync(0, 0, token);
                Task taskTheta = PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(0, default, token);
                //Task speedZero = PlcControl.tagControl.wholeDevice.WaitSpindleSpeedToZeroAsync(token);
                await Task.WhenAll(taskXY, taskTheta);
                MaterialSnackUtils.MaterialSnack("请打开相机安全门，更换硅片！", MaterialSnackUtils.SnackType.SUCCESS, 0, eventAggregator);
            }
            catch (OperationCanceledException)
            {
                MaterialSnackUtils.MaterialSnack("更换硅片操作失败！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
            }
        }

        /// <summary>
        /// 换硅片
        /// </summary>
        /// <returns></returns>
        public static async Task ReplaceWaferAndResetAsync(IEventAggregator? eventAggregator, CancellationToken token)
        {
            //清空记录
            Appsettings.CutY = null;
            Appsettings.CutThetaDegList = null;
            Appsettings.CutDistance = null;
            await ReplaceWaferAsync(eventAggregator, token);
        }

        public static async Task<CommonResult<float>> ProcessCombineMeasureHeightAsync(IEventAggregator? eventAggregator = null, CancellationToken token = default)
        {
            await PlcControl.tagControl.cutting.SetSpindleSpeedAsync(BmSetupData.Instance.SpindleRev);
            await PlcControl.tagControl.bladeMantance.SetSetupParamsAsync(CurrentUtils.GetBladeHeightModel());
            CommonResult<float> curHeightZ;
            if (Appsettings.MeasureHeightLast == null)
            {
                if (Appsettings.BladeOuterDiameter == null)
                {
                    return CommonResult<float>.Failure("未设置刀片外径，无法测高！");
                }
                var caculateResult = CaculateZAxisMaxDistance(Appsettings.BladeOuterDiameter.Value);
                if (!caculateResult.IsSuccess)
                {
                    return caculateResult;
                }
                await PlcControl.tagControl.bladeMantance.SetZAxisMaxDistanceAsync(caculateResult.Data - 0.15f);
                curHeightZ = await ProcessMeasureHeightAsync(HeightMeasurementMode.Contact, default, eventAggregator, token);
                Appsettings.MeasureHeightFirst = curHeightZ.IsSuccess ? curHeightZ.Data : null;
            }
            else
            {
                await PlcControl.tagControl.bladeMantance.SetZAxisMaxDistanceAsync(Appsettings.MeasureHeightLast.Value - 0.15f);
                curHeightZ = await ProcessMeasureHeightAsync(HeightMeasurementMode.Contact, default, eventAggregator, token);
            }
            Appsettings.AfterReplaceBladeCutTimes = 0;
            Appsettings.AfterReplaceBladeCutLength = 0;
            Appsettings.MeasureHeightLast = curHeightZ.IsSuccess ? curHeightZ.Data : null;
            return curHeightZ;
        }

        /// <summary>
        /// 执行测高
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<CommonResult<float>> ProcessMeasureHeightAsync(HeightMeasurementMode mode, IDialogService? dialogService, IEventAggregator? eventAggregator = null, CancellationToken token = default)
        {
            SpeedManager.IsHighSpeed = false;
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
            CancellationTokenSource repeatMeasureCts = new CancellationTokenSource();
            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(repeatMeasureCts.Token, token);
            CancellationToken useToken = linkedCts.Token;
            _ = MonitoringAlarmAsync(repeatMeasureCts.Cancel, AlarmConfig.Instance.HasConductivityAlarm, eventAggregator, repeatMeasureCts.Token);
            try
            {
                for (int times = 1; times <= 30; times++)
                {
                    try
                    {
                        if (mode is HeightMeasurementMode.Contact)
                        {
                            await PlcControl.tagControl.wholeDevice.StartSpindleAsync();
                            // 工作盘吹气
                            await WorkpieceBlowingAsync(default, eventAggregator, useToken);
                        }
                        else if (mode is HeightMeasurementMode.NoContact)
                        {
                            //主轴旋转
                            eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("主轴开始旋转"));
                            await PlcControl.tagControl.wholeDevice.StartSpindleAsync();
                            //eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("光纤传感器开始吹水"));
                            //await PlcControl.tagControl.bladeMantance.OpenOpticalFiberSensorBlowingWaterAsync(2);
                            eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("光纤传感器开始吹气"));
                            float startBlowX = 127f, endBlowX = 135f;
                            //测高前移动到初始位置，主轴旋转，开始吹水吹气
                            await PlcControl.tagControl.cutting.RunMotionAsync(startBlowX, 50, useToken);
                            await PlcControl.tagControl.bladeMantance.OpenOpticalFiberSensorBlowingAsync();
                            for (int count = 0; count < 5; count++)
                            {
                                await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(endBlowX, 5, useToken);
                                await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(startBlowX, 5, useToken);
                            }
                            await PlcControl.tagControl.bladeMantance.CloseOpticalFiberSensorBlowingAsync();
                            // 初始化
                            await PlcControl.tagControl.Xaxis.StartHomingAsync();
                            await PlcControl.tagControl.Xaxis.WaitAxisReadyAsync(useToken);
                        }
                        //等待测高准备完成信号
                        await PlcControl.tagControl.bladeMantance.WaitReadyToMeasureHeightAsync(useToken);
                        //进入测高模式
                        await PlcControl.tagControl.bladeMantance.StartBladeSetupAsync();
                        eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("开始测高！"));
                        // 发送测高开始信号到PLC
                        await PlcControl.tagControl.bladeMantance.StartSetupAsync();
                        List<float> setupValueList = [];
                        int? measureHeightTimes = await PlcControl.tagControl.bladeMantance.GetHeightMeasureSetupNumberAsync();
                        if (measureHeightTimes == null)
                        {
                            return CommonResult<float>.Failure("测高次数获取失败！");
                        }
                        int curMeasureHeightTimes = measureHeightTimes.Value;
                        int heightMeasureTimes = BmSetupData.Instance.HeightMeasureTimes;
                        while (curMeasureHeightTimes < heightMeasureTimes)
                        {
                            curMeasureHeightTimes++;
                            await PlcControl.tagControl.bladeMantance.WaitHeightMeasureSetupNumberUdatedAsync(curMeasureHeightTimes, useToken);
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
                        await PlcControl.tagControl.bladeMantance.WaitHeightMeasurementCompletedAsync(useToken);
                        eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"测高平均值：{setupValueList.Average()}"));
                        float maxDeviation = setupValueList.Max() - setupValueList.Min();
                        eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"测高最大偏差：{Math.Round(maxDeviation * 1000, 1)} um"));
                        // 测高数据异常处理
                        if (maxDeviation >= 0.01)
                        {
                            eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"测高偏差过大，重新测高"));
                            if (times % 3 == 0)
                            {
                                var res = await DialogHost.Show(SelectionDialog.NewInstance("继续测高", "结束切割", title: "测高多次失败，请确认操作"));
                                if (res is string dialogResult)
                                {
                                    if (dialogResult == SelectionDialog.NO)
                                    {
                                        return CommonResult<float>.Failure("结束切割！");
                                    }
                                }
                            }
                            continue;
                        }
                        return CommonResult<float>.Success(measureHeightAve);
                    }
                    catch (OperationCanceledException)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return CommonResult<float>.Failure("测高操作取消！");
                        }
                        eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("测高导电异常！"));
                        await PlcControl.tagControl.wholeDevice.AlarmResetAsync();
                        if (AlarmConfig.Instance.HasConductivityAlarm())
                        {
                            return CommonResult<float>.Failure("测高导电异常！");
                        }
                        repeatMeasureCts = new CancellationTokenSource();
                        linkedCts = CancellationTokenSource.CreateLinkedTokenSource(repeatMeasureCts.Token, token);
                        useToken = linkedCts.Token;
                        _ = MonitoringAlarmAsync(repeatMeasureCts.Cancel, AlarmConfig.Instance.HasConductivityAlarm, eventAggregator, repeatMeasureCts.Token);
                    }
                }
            }
            finally
            {
                repeatMeasureCts.Cancel();
                repeatMeasureCts.Dispose();
                linkedCts.Cancel();
                linkedCts.Dispose();
                // 关闭接触测高
                await PlcControl.tagControl.bladeMantance.StartNoContactHeightMeasurement();
            }
            return CommonResult<float>.Failure("测高失败次数过多！");
        }

        public static CommonResult<float> CaculateZAxisMaxDistance(float bladeOuterDiameter)
        {
            if (Appsettings.AxisToWorkingDiscDistance is null)
            {
                return CommonResult<float>.Failure("功能参数设定异常，未配置轴心零点到工作盘距离！");
            }
            return CommonResult<float>.Success(Appsettings.AxisToWorkingDiscDistance.Value - bladeOuterDiameter / 2);
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
        public static List<float>? GetCutList()
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
            List<float> cutSpeedList = [];
            // 从预切割开始编号开始
            for (int i = preCutModel.NewBladeNo; i <= feedSpds.Length; i++)
            {
                // 获取进刀速度
                float feedspeed = feedSpds[i - 1];
                float cutLine = ofLinesList[i - 1];
                if (feedspeed != 0 && cutLine != 0)
                {
                    for (int j = 0; j < cutLine; j++)
                    {
                        cutSpeedList.Add(feedspeed);
                    }
                }
            }
            return cutSpeedList;
        }

        public static CommonResult<List<float>> GetPreCutSpeedList()
        {
            PreCutModel preCutModel = CurrentUtils.GetPreCutModel();
            //起始为0，直接返回空集合
            if (preCutModel.NewBladeNo == 0)
            {
                return CommonResult<List<float>>.Success([]);
            }
            // 获取
            float[] feedSpds = Tools.StringToFloatArray(preCutModel.FeedSpd); // 获取进刀速度
            float[] ofLinesList = Tools.StringToFloatArray(preCutModel.OfLines); // 获取切割刀数
            List<float> cutSpeed = [];
            // 从预切割开始编号开始
            for (int i = preCutModel.NewBladeNo; i <= feedSpds.Length; i++)
            {
                // 获取进刀速度
                float feedspeed = feedSpds[i - 1];
                float cutLine = ofLinesList[i - 1];
                if (feedspeed != 0 && cutLine != 0)
                {
                    for (int j = 0; j < cutLine; j++)
                    {
                        cutSpeed.Add(feedspeed);
                    }
                }
            }
            return CommonResult<List<float>>.Success(cutSpeed);
        }

        /// <summary>
        /// 预切割序列
        /// </summary>
        public static async Task<CommonResult<List<float>>> GetCutListAsync(CutParamsModel cutParams)
        {
            SQLiteAsyncConnection connection = SqlHelper.SQLiteAsync;
            // 查询当前预切割流程信息
            PreCutModel? preCutModel = (await connection.Table<PreCutModel>().Where(t => t.PrecutNo == cutParams.PrecutProcessNo).ToListAsync()).FirstOrDefault();
            if (preCutModel is null)
            {
                return CommonResult<List<float>>.Failure("预切割序列异常，请检查预切割参数配置！");
            }
            //起始为0，直接返回空集合
            if (preCutModel.NewBladeNo == 0)
            {
                return CommonResult<List<float>>.Success([]);
            }
            // 获取
            float[] feedSpds = Tools.StringToFloatArray(preCutModel.FeedSpd); // 获取进刀速度
            float[] ofLinesList = Tools.StringToFloatArray(preCutModel.OfLines); // 获取切割刀数
            List<float> cutSpeedList = new List<float>();
            // 从预切割开始编号开始
            for (int i = preCutModel.NewBladeNo; i <= feedSpds.Length; i++)
            {
                // 获取进刀速度
                float feedspeed = feedSpds[i - 1];
                float cutLine = ofLinesList[i - 1];
                if (feedspeed != 0 && cutLine != 0 && feedspeed <= cutParams.HightestCutSpeed)
                {
                    for (int j = 0; j < cutLine; j++)
                    {
                        cutSpeedList.Add(feedspeed);
                    }
                }
            }
            if (cutParams.CutNum == 0)
            {
                return CommonResult<List<float>>.Success(cutSpeedList);
            }
            else if (cutParams.CutNum > cutSpeedList.Count)
            {
                return CommonResult<List<float>>.Success(
                    Enumerable.Range(0, cutParams.CutNum)
                    .Select(i => cutSpeedList[i % cutSpeedList.Count])
                    .ToList());
            }
            else
            {
                return CommonResult<List<float>>.Success(cutSpeedList.GetRange(0, cutParams.CutNum));
            }
        }

        /// <summary>
        /// 获取总切割刀数
        /// </summary>
        public static int? GetTotalCutTimes()
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
            int totalCutTimes = 0;
            // 从预切割开始编号开始
            for (int i = preCutModel.NewBladeNo; i <= feedSpds.Length; i++)
            {
                // 获取进刀速度
                float feedspeed = feedSpds[i - 1];
                float cutLine = ofLinesList[i - 1];
                if (feedspeed != 0 && cutLine != 0)
                {
                    totalCutTimes += (int)cutLine;
                }
            }
            return totalCutTimes;
        }

        public static async Task<InitialPositionModel?> GetInitialPositionAsync()
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
            if (cameraCommons.Count != 0)
            {
                return cameraCommons.FirstOrDefault();
            }

            return null;
        }

        public static async Task GoPreCutLineAsync(CancellationToken token)
        {
            DataPoint<float> cameraThetaCenterPoint = Appsettings.CameraThetaCenterPoint;
            float thetaDeg = Appsettings.CutThetaDegList?.FirstOrDefault() ?? 0f;
            Task focusxyTask = PlcControl.tagControl.cutting.RunMotionAsync(cameraThetaCenterPoint.X - 10, Appsettings.CutY?.ToCameraY() ?? (cameraThetaCenterPoint.Y + 30), token);
            Task focusThetaTask = PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(thetaDeg, default, token);
            await Task.WhenAll(focusxyTask, focusThetaTask);
        }

        public static async Task GoPreSharpenLineAsync(CancellationToken token)
        {
            DataPoint<float> cameraThetaCenterPoint = Appsettings.CameraThetaCenterPoint;
            float thetaDeg = Appsettings.SharpenThetaDegList?.FirstOrDefault() ?? 0f;
            Task focusxyTask = PlcControl.tagControl.cutting.RunMotionAsync(cameraThetaCenterPoint.X - 10, Appsettings.SharpenY?.ToCameraY() ?? (cameraThetaCenterPoint.Y - 10), token);
            Task focusThetaTask = PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(thetaDeg, default, token);
            await Task.WhenAll(focusxyTask, focusThetaTask);
        }

        public static async Task<CommonResult<float>> AutoFocusAsync(IEventAggregator? eventAggregator = null, CancellationToken token = default)
        {
            eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("开始相机精细对焦..."));
            CameraCommon? cameraCommon = GetCameraCommon();
            if (cameraCommon is null)
            {
                return CommonResult<float>.Failure("相机获取失败！");
            }
            float focusClearZ = Appsettings.FocusClearZ ?? 0;
            await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(focusClearZ, 2, token);
            // 模糊度大于200时，直接返回清晰位置
            //if (cameraCommon.localBitmap != null)
            //{
            //    double tenengradBlurriness = VisualUtils.CalculateTenengrad2(cameraCommon.localBitmap);
            //    eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"当前位置：{focusClearZ} 当前模糊度：{tenengradBlurriness}"));
            //    if (tenengradBlurriness > 200)
            //    {
            //        eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"图像已清晰无需再次聚焦"));
            //        Appsettings.IsNeedCheckBaseLine = false;
            //        return CommonResult<float>.Success(focusClearZ);
            //    }
            //}
            //Appsettings.IsNeedCheckBaseLine = true;
            CommonResult<float> roughFocusPosition = await AutoFocusAsync(cameraCommon, focusClearZ, 0.2f, 0.02f, token, eventAggregator);
            if (!roughFocusPosition.IsSuccess)
            {
                roughFocusPosition = await AutoFocusService.GlobalFocusAsync(eventAggregator, token);
                if (!roughFocusPosition.IsSuccess)
                {
                    return CommonResult<float>.Failure("聚焦失败，请检查硅片位置！");
                }
            }
            // 进行精调聚焦
            CommonResult<float> result = await AutoFocusAsync(cameraCommon, roughFocusPosition.Data, 0.018f, 0.002f, token, eventAggregator);
            if (result.IsSuccess)
            {
                Appsettings.FocusClearZ = result.Data;
            }
            return result;
        }

        private static async Task<CommonResult<float>> AutoFocusAsync(CameraCommon cameraCommon, float startPositionZ2, float margin, float singleMoveDistance, CancellationToken token, IEventAggregator? eventAggregator = null)
        {
            float lastBlurriness = 0;
            float lastPosition = 0;
            for (float newPosition = startPositionZ2 - margin; newPosition <= startPositionZ2 + margin; newPosition += singleMoveDistance)
            {
                await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(newPosition, default, token);
                if (cameraCommon.LocalBitmap != null)
                {
                    float tenengradBlurriness = (float)VisionAnalyzer.CalculateTenengrad2(cameraCommon.LocalBitmap.ToMat());
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
        public static async Task WorkpieceBlowingThenBackAsync(IEventAggregator? eventAggregator = null, CancellationToken token = default)
        {
            Task<float?> xTask = PlcControl.tagControl.Xaxis.GetCurrentLocationAsync();
            Task<float?> yTask = PlcControl.tagControl.Yaxis.GetCurrentLocationAsync();
            await Task.WhenAll(xTask, yTask);
            float curX = xTask.Result ?? 0, curY = yTask.Result ?? 0;
            await WorkpieceBlowingAsync(default, eventAggregator, token);
            await PlcControl.tagControl.cutting.RunMotionAsync(curX, curY, token);
        }

        /// <summary>
        /// 工作盘吹气
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task WorkpieceBlowingAsync(float? atomizingNozzlePositionY, IEventAggregator? eventAggregator = null, CancellationToken token = default)
        {
            List<UserDefineDataModel> list = SqlHelper.Table<UserDefineDataModel>().ToList();
            if (list.Count != 1)
            {
                eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("功能参数设定，雾化喷嘴位置设定错误！"));
                return;
            }
            UserDefineDataModel userDefineData = list.First();
            eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("开始工件吹气..."));
            try
            {
                await PlcControl.tagControl.wholeDevice.OpenWorkpieceBlowingAsync();
                await PlcControl.tagControl.cutting.RunMotionAsync(userDefineData.AtomizingNozzlePositionX.ToFloat(), atomizingNozzlePositionY ?? userDefineData.AtomizingNozzlePositionY.ToFloat(), token);
                await Task.Delay(TimeSpan.FromSeconds(userDefineData.BlowTime.ToFloat()), token);
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
                //工件吹气
                eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("开始工件吹气..."));
                float rightCheckX = line.EndPoint.X.ToCameraX() - 20;
                float leftCheckX = line.StartPoint.X.ToCameraX() + 20;
                float checkY = line.EndPoint.Y.ToCameraY();
                await PlcControl.tagControl.wholeDevice.OpenWorkpieceBlowingAsync();
                await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(190, 80, token);
                await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(rightCheckX, 7f, token);
                await PlcControl.tagControl.Yaxis.StartAbsoluteAsync(checkY, 50, token);
                await AutoFocusService.GlobalFocusAsync(eventAggregator, token);
                await FineTuneAxisYAsync();
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
                            Mat? localMat = await GetCurrentMatAsync();
                            if (localMat != null)
                            {
                                matQueue.Enqueue(localMat);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        //正常取消任务
                    }
                }, linkedToken);
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

        private static void ProcessSingleMat(Mat mat, ImagesAnalysisResult result)
        {
            Mat cropMat = CropHorizontalCenter(mat, HeightRange);
            Mat cropMatJpg = JpegStreamToMat(MatToJpegStream(cropMat));
            Mat originCropMatJpg = cropMatJpg.Clone();
            var (bladeWidthMm, collapseWidthMm, bladeTop, bladeBottom, collapseTop, collapseBottom) = VisionAnalyzer.ProcessImage(cropMatJpg);
            bladeWidthMm = Math.Round(bladeWidthMm, 4);
            collapseWidthMm = Math.Round(collapseWidthMm, 4);
            Cv2.PutText(cropMatJpg,
                $"No: {result.ImageDatas.Count}",
                new Point(900, (bladeTop + bladeBottom) / 2),
                HersheyFonts.HersheySimplex,
                1.3f,
                Scalar.Blue,
                2);
            DrawMat(cropMatJpg, bladeWidthMm, collapseWidthMm, bladeTop, bladeBottom, collapseTop, collapseBottom);
            var imageData = new ImageData
            {
                BladeWidth = bladeWidthMm,
                CollapseWidth = collapseWidthMm,
                OriginMat = mat,
                CropMatJpg = cropMatJpg,
                OriginCropMatJpg = originCropMatJpg
            };
            result.ImageDatas.Add(imageData);
            if (result.BladeWidthMaxImage.BladeWidth < bladeWidthMm)
                result.BladeWidthMaxImage = imageData;
            if (result.CollapseWidthMaxImage.CollapseWidth < collapseWidthMm)
                result.CollapseWidthMaxImage = imageData;
        }

        public static Mat DrawMat(Mat source, double bladeWidthMm, double collapseWidthMm, double bladeTop, double bladeBottom, double collapseTop, double collapseBottom)
        {
            Cv2.PutText(source,
                $"bladeWidthMm: {bladeWidthMm}",
                new Point(20, bladeTop),
                HersheyFonts.HersheySimplex,
                1.3f,
                Scalar.Green,
                2);
            Cv2.PutText(source,
                $"collapseWidthMm:{collapseWidthMm}",
                new Point(900, collapseTop),
                HersheyFonts.HersheySimplex,
                1.3f,
                Scalar.Red,
                2);
            Cv2.Line(
               img: source,
               pt1: new Point(0, bladeTop),  // 起点
               pt2: new Point(source.Width, bladeTop), // 终点
               color: Scalar.Green,         // 颜色 (B,G,R)
               thickness: 1,             // 线宽
               lineType: LineTypes.AntiAlias // 抗锯齿
               );
            Cv2.Line(
                img: source,
                pt1: new Point(0, bladeBottom),  // 起点
                pt2: new Point(source.Width, bladeBottom), // 终点
                color: Scalar.Green,         // 颜色 (B,G,R)
                thickness: 1,             // 线宽
                lineType: LineTypes.AntiAlias // 抗锯齿
                );
            Cv2.Line(
                img: source,
                pt1: new Point(0, collapseTop),  // 起点
                pt2: new Point(source.Width, collapseTop), // 终点
                color: Scalar.Red,         // 颜色 (B,G,R)
                thickness: 1,             // 线宽
                lineType: LineTypes.AntiAlias // 抗锯齿
                );
            Cv2.Line(
                img: source,
                pt1: new Point(0, collapseBottom),  // 起点
                pt2: new Point(source.Width, collapseBottom), // 终点
                color: Scalar.Red,         // 颜色 (B,G,R)
                thickness: 1,             // 线宽
                lineType: LineTypes.AntiAlias // 抗锯齿
                );
            return source;
        }

        public static async Task<ImagesAnalysisResult> ProcessImagesAnalysisAsync(ConcurrentQueue<Mat> matQueue, IEventAggregator? eventAggregator = null, CancellationToken token = default)
        {
            var result = new ImagesAnalysisResult { IsSuccess = true };
            var stopwatch = Stopwatch.StartNew();
            try
            {
                //处理单个图像
                stopwatch.Restart();
                await ProcessIndividualImagesAsync(matQueue, result, token);
                stopwatch.Stop();
                PublishTiming(eventAggregator, "处理所有单张图像", stopwatch.Elapsed);

                //拼接图像
                stopwatch.Restart();
                List<Mat> processedMats = result.ImageDatas.Select(data => data.OriginMat).ToList();
                List<Mat> concatMats = await Task.Run(() => ConcatMats(processedMats));
                stopwatch.Stop();
                PublishTiming(eventAggregator, "拼接所有图像", stopwatch.Elapsed);

                //分析拼接后的图像
                await AnalyzeConcatImagesAsync(concatMats, result, eventAggregator);

                //判断识别结果
                double singleCollapse = (Math.Round(result.CollapseWidthMaxImage.CollapseWidth, 3) - Math.Round(result.CollapseWidthMaxImage.BladeWidth, 3)) / 2;
                if (singleCollapse > 10)
                {
                    result.IsSuccess = false;
                    result.Message = string.IsNullOrEmpty(result.Message) ? $"图像识别: 崩边过大，单边最大为 {singleCollapse}um ！" : result.Message;
                }
                else if (result.HasSnakelike())
                {
                    result.IsSuccess = false;
                    result.Message = string.IsNullOrEmpty(result.Message) ? "图像识别: 刀痕刀痕为蛇形，请人工检查刀痕状态！" : result.Message;
                }

                //保存图像
                stopwatch.Restart();
                await SaveImagesAsync(concatMats, result);
                stopwatch.Stop();
                PublishTiming(eventAggregator, "保存图像", stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = $"图像识别失败: {ex.Message}";
            }

            return result;
        }

        private static async Task AnalyzeConcatImagesAsync(List<Mat> concatMats, ImagesAnalysisResult result, IEventAggregator? eventAggregator)
        {
            var stopwatch = Stopwatch.StartNew();
            foreach (Mat concatMat in concatMats)
            {
                Mat originCropConcatMatJpg = JpegStreamToMat(MatToJpegStream(CropHorizontalCenter(concatMat, VisionSnakeHeightRange)));
                Mat cropConcatMatJpg = originCropConcatMatJpg.Clone();
                try
                {
                    stopwatch.Restart();
                    int? centerY = await Task.Run(() => VisionAnalyzer.DetectFirstHorizontalStripeCenter(originCropConcatMatJpg));
                    stopwatch.Stop();
                    PublishTiming(eventAggregator, "识别有无刀痕", stopwatch.Elapsed);
                    if (centerY is null)
                    {
                        result.AnalysisFailMats.Add(originCropConcatMatJpg);
                        result.IsSuccess = false;
                        result.Message = string.IsNullOrEmpty(result.Message) ? "未识别到刀痕，请人工检查刀痕状态！" : result.Message;
                    }
                    else
                    {
                        stopwatch.Restart();
                        var (bladeWidthMm, collapseWidthMm, bladeTop, bladeBottom, collapseTop, collapseBottom) = await Task.Run(() => VisionAnalyzer.ProcessImage(originCropConcatMatJpg));
                        bladeWidthMm = Math.Round(bladeWidthMm, 4);
                        collapseWidthMm = Math.Round(collapseWidthMm, 4);
                        stopwatch.Stop();
                        PublishTiming(eventAggregator, "识别刀痕宽度和崩边", stopwatch.Elapsed);
                        DrawMat(cropConcatMatJpg, bladeWidthMm, collapseWidthMm, bladeTop, bladeBottom, collapseTop, collapseBottom);
                        stopwatch.Restart();
                        bool isSnakelike = await Task.Run(() => VisionAnalyzer.SnakeCase(originCropConcatMatJpg).Snake);
                        result.ConcatImages.Add(new ImageData()
                        {
                            BladeWidth = bladeWidthMm,
                            CollapseWidth = collapseWidthMm,
                            IsSnakelike = isSnakelike,
                            OriginMat = concatMat,
                            OriginCropMatJpg = originCropConcatMatJpg,
                            CropMatJpg = cropConcatMatJpg
                        });
                        stopwatch.Stop();
                        PublishTiming(eventAggregator, "识别蛇形", stopwatch.Elapsed);
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
        }

        private static async Task ProcessIndividualImagesAsync(ConcurrentQueue<Mat> queue, ImagesAnalysisResult result, CancellationToken token)
        {
            await Task.Run(async () =>
            {
                try
                {
                    using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
                    while (await timer.WaitForNextTickAsync(token))
                    {
                        while (queue.TryDequeue(out Mat? mat))
                        {
                            ProcessSingleMat(mat, result);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // 可能剩余的图像进行处理
                    while (queue.TryDequeue(out Mat? mat))
                    {
                        ProcessSingleMat(mat, result);
                    }
                }
            });
        }

        private static async Task SaveImagesAsync(List<Mat> concatMats, ImagesAnalysisResult result)
        {
            // 保存拼接图像到指定目录
            string imagePath = System.IO.Path.Combine(AppContext.BaseDirectory, $"image\\{DateTime.Now.Ticks}");
            Directory.CreateDirectory(imagePath);
            string concatImagesPath = System.IO.Path.Combine(imagePath, "ConcatImages");
            Directory.CreateDirectory(concatImagesPath);

            await Task.Run(() =>
            {
                if (result.ImageDatas.Count != 0)
                {
                    Parallel.For(0, result.ImageDatas.Count, i =>
                    {
                        Cv2.ImWrite($"{imagePath}\\单张原图_{i}.jpg", result.ImageDatas[i].OriginMat);
                        Cv2.ImWrite($"{imagePath}\\单张裁剪原图_{i}.jpg", result.ImageDatas[i].OriginCropMatJpg);
                        Cv2.ImWrite($"{imagePath}\\单张裁剪识别图_{i}.jpg", result.ImageDatas[i].CropMatJpg);
                    });
                }

                // 并行保存拼接图
                Parallel.ForEach(concatMats, (mat, state, index) =>
                {
                    Cv2.ImWrite($"{concatImagesPath}\\{DateTime.Now.Ticks}_拼接图原图.jpg", mat);
                });

                if (result.ConcatImages.Count != 0)
                {
                    Parallel.For(0, result.ConcatImages.Count, i =>
                    {
                        Cv2.ImWrite($"{imagePath}\\拼接原图_{i}.jpg", result.ConcatImages[i].OriginMat);
                        Cv2.ImWrite($"{imagePath}\\拼接裁剪原图_{i}.jpg", result.ConcatImages[i].OriginCropMatJpg);
                        Cv2.ImWrite($"{imagePath}\\拼接裁剪识别图_{i}_{(result.ConcatImages[i].IsSnakelike ? "蛇形" : "正常")}.jpg", result.ConcatImages[i].CropMatJpg);
                    });
                }

                if (result.AnalysisFailMats.Count != 0)
                {
                    //保存拼接图像到指定目录
                    string analysisFailMatsPath = System.IO.Path.Combine(imagePath, "AnalysisFail");
                    Directory.CreateDirectory(analysisFailMatsPath);
                    foreach (var failMat in result.AnalysisFailMats)
                    {
                        Cv2.ImWrite($"{analysisFailMatsPath}\\{DateTime.Now.Ticks}.jpg", failMat);
                    }
                }
            });
        }

        private static void PublishTiming(IEventAggregator? eventAggregator, string operation, TimeSpan elapsed)
        {
            if (eventAggregator == null) return;
            var message = MessageModel.Create($"{operation}用时: {elapsed.TotalSeconds:F2}秒");
            eventAggregator.GetEvent<AutoRuningMessageEvent>().Publish(message);
        }

        public static async Task MonitoringAlarmAsync(Action hasAlarmDoAction, Func<bool> hasActiveErrorAlarmFunc, IEventAggregator? eventAggregator, CancellationToken monitorToken)
        {
            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
                while (await timer.WaitForNextTickAsync(monitorToken))
                {
                    try
                    {
                        if (hasActiveErrorAlarmFunc.Invoke())
                        {
                            hasAlarmDoAction.Invoke();
                        }
                    }
                    catch (Exception ex)
                    {
                        eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"监控任务内异常: {ex.Message}"));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消，无需处理
            }
            catch (Exception ex)
            {
                eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"监控异常: {ex.Message}"));
            }
        }

        //public static async Task<ImagesAnalysisResult> ProcessImagesAnalysis1Async(ConcurrentQueue<Mat> matQueue, IEventAggregator? eventAggregator = null, CancellationToken token = default)
        //{
        //    ImagesAnalysisResult result = new ImagesAnalysisResult();
        //    result.IsSuccess = true;
        //    await Task.Run(async () =>
        //    {
        //        var stopwatch = Stopwatch.StartNew();
        //        List<Mat> mats = new List<Mat>();
        //        try
        //        {
        //            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
        //            while (await timer.WaitForNextTickAsync(token))
        //            {
        //                while(matQueue.TryDequeue(out Mat? mat))
        //                {
        //                    mats.Add(mat);
        //                    ProcessSingleMat(mat, result);
        //                }
        //            }
        //        }
        //        catch (OperationCanceledException)
        //        {
        //            // 可能剩余的图像进行处理
        //            while (matQueue.TryDequeue(out Mat? mat))
        //            {
        //                mats.Add(mat);
        //                ProcessSingleMat(mat, result);
        //            }
        //        }
        //        stopwatch.Stop();
        //        eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"识别所有单张图像用时: {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} 秒"));
        //        stopwatch = Stopwatch.StartNew();
        //        // 拼接所有图像
        //        List<Mat> concatMats = ConcatMats(mats);
        //        stopwatch.Stop();
        //        eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"拼接所有图像用时: {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} 秒"));

        //        #region 保存图片
        //        stopwatch = Stopwatch.StartNew();
        //        // 保存拼接图像到指定目录
        //        string uuid = Guid.NewGuid().ToString();
        //        string imagePath = System.IO.Path.Combine(AppContext.BaseDirectory, $"image\\{DateTime.Now.Ticks}");
        //        Directory.CreateDirectory(imagePath);
        //        string concatImagesPath = System.IO.Path.Combine(imagePath, "ConcatImages");
        //        Directory.CreateDirectory(concatImagesPath);
        //        if (mats.Count == result.ImageDatas.Count)
        //        {
        //            for (int i = 0; i < mats.Count; i++)
        //            {
        //                Cv2.ImWrite($"{imagePath}\\{uuid}_{i}_原图_{i}.jpg", mats[i]);
        //                Cv2.ImWrite($"{imagePath}\\{uuid}_{i}_裁剪识别图_{i}.jpg", result.ImageDatas[i].CropMatJpg);
        //            }
        //        }
        //        else
        //        {
        //            eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"保存原图和识别后的图片失败！"));
        //        }
        //        foreach (var image in concatMats)
        //        {
        //            Cv2.ImWrite($"{concatImagesPath}\\{DateTime.Now.Ticks}_拼接图原图.jpg", image);
        //        }
        //        stopwatch.Stop();
        //        eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"保存识别后的拼接图像总用时: {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} 秒"));
        //        #endregion

        //        foreach (Mat concatMat in concatMats)
        //        {
        //            Mat cropConcatMatJpg = JpegStreamToMat(MatToJpegStream(CropHorizontalCenter(concatMat, HeightRange)));
        //            try
        //            {
        //                stopwatch = Stopwatch.StartNew();
        //                int? centerY = VisionAnalyzer.DetectFirstHorizontalStripeCenter(cropConcatMatJpg);
        //                stopwatch.Stop();
        //                eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"识别有无刀痕用时: {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} 秒"));
        //                if (centerY is null)
        //                {
        //                    result.AnalysisFailMats.Add(cropConcatMatJpg);
        //                    result.IsSuccess = false;
        //                    result.Message = string.IsNullOrEmpty(result.Message) ? "未识别到刀痕，请人工检查刀痕状态！" : result.Message;
        //                }
        //                else
        //                {
        //                    stopwatch = Stopwatch.StartNew();
        //                    var (bladeWidthMm, collapseWidthMm, bladeTop, bladeBottom, collapseTop, collapseBottom) = VisionAnalyzer.ProcessImage(cropConcatMatJpg);
        //                    stopwatch.Stop();
        //                    eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"识别刀痕宽度和崩边用时: {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} 秒"));
        //                    Cv2.PutText(cropConcatMatJpg,
        //                        $"bladeWidthMm: {bladeWidthMm}",
        //                        new Point(20, bladeTop),
        //                        HersheyFonts.HersheySimplex,
        //                        1.3f,
        //                        Scalar.Red,
        //                        2);
        //                    Cv2.PutText(cropConcatMatJpg,
        //                        $"collapseWidthMm:{collapseWidthMm}",
        //                        new Point(900, collapseTop),
        //                        HersheyFonts.HersheySimplex,
        //                        1.3f,
        //                        Scalar.Green,
        //                        2);
        //                    Cv2.Line(
        //                        img: cropConcatMatJpg,
        //                        pt1: new Point(0, bladeTop),  // 起点
        //                        pt2: new Point(cropConcatMatJpg.Width, bladeTop), // 终点
        //                        color: Scalar.Red,         // 颜色 (B,G,R)
        //                        thickness: 1,             // 线宽
        //                        lineType: LineTypes.AntiAlias // 抗锯齿
        //                        );
        //                    Cv2.Line(
        //                        img: cropConcatMatJpg,
        //                        pt1: new Point(0, bladeBottom),  // 起点
        //                        pt2: new Point(cropConcatMatJpg.Width, bladeBottom), // 终点
        //                        color: Scalar.Red,         // 颜色 (B,G,R)
        //                        thickness: 1,             // 线宽
        //                        lineType: LineTypes.AntiAlias // 抗锯齿
        //                        );
        //                    Cv2.Line(
        //                        img: cropConcatMatJpg,
        //                        pt1: new Point(0, collapseTop),  // 起点
        //                        pt2: new Point(cropConcatMatJpg.Width, collapseTop), // 终点
        //                        color: Scalar.Green,         // 颜色 (B,G,R)
        //                        thickness: 1,             // 线宽
        //                        lineType: LineTypes.AntiAlias // 抗锯齿
        //                        );
        //                    Cv2.Line(
        //                        img: cropConcatMatJpg,
        //                        pt1: new Point(0, collapseBottom),  // 起点
        //                        pt2: new Point(cropConcatMatJpg.Width, collapseBottom), // 终点
        //                        color: Scalar.Green,         // 颜色 (B,G,R)
        //                        thickness: 1,             // 线宽
        //                        lineType: LineTypes.AntiAlias // 抗锯齿
        //                        );
        //                    stopwatch = Stopwatch.StartNew();
        //                    result.ConcatImages.Add(new ImageData()
        //                    {
        //                        BladeWidth = bladeWidthMm,
        //                        CollapseWidth = collapseWidthMm,
        //                        IsSnakelike = VisionAnalyzer.SnakeCase(cropConcatMatJpg).Snake,
        //                        CropMatJpg = cropConcatMatJpg
        //                    });
        //                    stopwatch.Stop();
        //                    eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"识别蛇形用时: {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} 秒"));
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                result.AnalysisFailMats.Add(cropConcatMatJpg);
        //                result.IsSuccess = false;
        //                result.Message = string.IsNullOrEmpty(result.Message) ? "图像识别: 刀痕异常，请人工检查刀痕状态！" : result.Message;
        //                eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create(ex.Message));
        //            }
        //            finally
        //            {
        //                stopwatch.Stop();
        //            }
        //        }
        //        double singleCollapse =(Math.Round(result.CollapseWidthMaxImage.CollapseWidth, 3) - Math.Round(result.CollapseWidthMaxImage.BladeWidth, 3)) / 2;
        //        if (singleCollapse > 10)
        //        {
        //            result.IsSuccess = false;
        //            result.Message = string.IsNullOrEmpty(result.Message) ? $"图像识别: 崩边过大，单边最大为 {singleCollapse}um ！" : result.Message;
        //        }
        //        if (result.HasSnakelike())
        //        {
        //            result.IsSuccess = false;
        //            result.Message = string.IsNullOrEmpty(result.Message) ? "图像识别: 刀痕刀痕为蛇形，请人工检查刀痕状态！" : result.Message;
        //        }

        //        #region 保存图片
        //        if (result.AnalysisFailMats.Count != 0)
        //        {
        //            stopwatch = Stopwatch.StartNew();
        //            foreach (var image in result.ConcatImages)
        //            {
        //                Cv2.ImWrite($"{concatImagesPath}\\{DateTime.Now.Ticks}_拼接图识别图_{(image.IsSnakelike ? "蛇形" : "正常")}.jpg", image.CropMatJpg);
        //            }
        //            //保存拼接图像到指定目录
        //            string analysisFailMatsPath = System.IO.Path.Combine(imagePath, "AnalysisFail");
        //            Directory.CreateDirectory(analysisFailMatsPath);
        //            foreach (var failMat in result.AnalysisFailMats)
        //            {
        //                Cv2.ImWrite($"{analysisFailMatsPath}\\{DateTime.Now.Ticks}.jpg", failMat);
        //            }
        //            stopwatch.Stop();
        //            eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"保存识别图像总用时: {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} 秒"));
        //        }
        //        #endregion
        //    });
        //    return result;
        //}

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

        public static async Task<Mat?> GetCurrentMatAsync()
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                WriteableBitmap? localBitmap = GrabWriteableBitmap();
                return localBitmap?.ToMat();
            });
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

        public static async Task<CommonResult> UpdateCameraCommonLineAsync()
        {
            Mat? localMat = await GetCurrentMatAsync();
            if (localMat != null)
            {
                Mat cropMatJpg = JpegStreamToMat(MatToJpegStream(CropHorizontalCenter(localMat, HeightRange)));
                return await UpdateCameraCommonLineAsync(cropMatJpg);
            }
            return CommonResult.Failure("获取相机图片失败");
        }

        public static async Task<CommonResult> UpdateCameraCommonLineAsync(Mat cropMatJpg)
        {
            CameraCommon? cameraCommon = GetCameraCommon();
            if (cameraCommon is not null)
            {
                return await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        var (bladeWidthMm, collapseWidthMm, bladeTop, bladeBottom, collapseTop, collapseBottom) = VisionAnalyzer.ProcessImage(cropMatJpg);
                        bladeWidthMm = Math.Round(bladeWidthMm, 4);
                        collapseWidthMm = Math.Round(collapseWidthMm, 4);
                        cameraCommon.UpdateLine((float)bladeWidthMm * 1000, (float)collapseWidthMm * 1000);
                        return CommonResult.Success();
                    }
                    catch (Exception e)
                    {
                        return CommonResult.Failure(e.Message);
                    }
                });
            }
            return CommonResult.Failure("获取相机失败");
        }

        public static async Task<CommonResult> FineTuneAxisYAsync()
        {
            Mat? localMat = await GetCurrentMatAsync();
            if (localMat != null)
            {
                Mat mat = new Mat();
                Cv2.Flip(localMat, mat, FlipMode.XY);  // 同时水平和垂直翻转
                Mat cropMatJpg = JpegStreamToMat(MatToJpegStream(CropHorizontalCenter(mat, HeightRange)));
                return await FineTuneAxisYAsync(cropMatJpg);
            }
            return CommonResult.Failure("获取相机图片失败");
        }

        public static async Task<CommonResult> FineTuneAxisYAsync(Mat cropMatJpg)
        {
            try
            {
                int? imageY = VisionAnalyzer.DetectFirstHorizontalStripeCenter(cropMatJpg);
                if (imageY == null)
                {
                    return CommonResult.Failure("未检测到水平条纹");
                }
                float offsetY = (float)Math.Round((imageY.Value - (cropMatJpg.Height / 2)) * VisionAnalyzer.PixelToMmRatio, 4);
                if (MathF.Abs(offsetY) >= GlobalParams.NormalStepDistance) offsetY = 0;
                await PlcControl.tagControl.Yaxis.StartRelativeAsync(offsetY, default, default);
                return CommonResult.Success();
            }
            catch (Exception ex)
            {
                return CommonResult.Failure($"未检测到水平条纹，{ex.Message}");
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

    public record class AxisPosition(float? X, float? Y, float? Z1, float? Z2, float? Theta);
    public record class AxisState(bool? X, bool? Y, bool? Z1, bool? Z2, bool? Theta);
}