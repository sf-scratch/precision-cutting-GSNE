using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.database.db.modle;
using 精密切割系统.DTOs;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.HttpClients;
using 精密切割系统.Model.plc;
using 精密切割系统.Model.sqlite;
using 精密切割系统.Utils;
using 精密切割系统.ViewModel;

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
            GetLunguInfo("T24111102B0006");

            // 开始测高
            //float? afterHeightMeasurementZ = await ProcessMeasureHeightAsync();
            //if (afterHeightMeasurementZ == null)
            //{
            //    MaterialSnackUtils.MaterialSnack("测高失败！", MaterialSnackUtils.SnackType.ERROR);
            //    return;
            //}
            float? afterHeightMeasurementZ = 0;

            // 切割校准
            float cutCalibratTheta = await CalibratCutAsync();
            // 磨刀校准
            float sharpenCalibratTheta = await CalibratSharpenAsync();
            // 获取型号目录ID
            long deviceDataId = CurrentUtils.GetCurrentConfiguration().DeviceDataId;
            // theta轴中心点位置
            DataPoint<float> thetaCenterPoint = GlobalParams.ThetaCenterPoint;
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



        private static LunguInfoDTO? GetLunguInfo(string lunguId)
        {
            ApiRequest request = new ApiRequest
            {
                Method = RestSharp.Method.Get,
                Route = $"n2baseDev-osb/http/interface/getLunguSksj?lungu={lunguId}"
            };
            ApiResponse? response = HttpRestClient.Instance.Execute(request);
            if (response == null)
            {
                Tools.LogDebug("获取切割信息失败！");
                return null;
            }
            LunguInfoDTO? lunguInfo = null;
            if (response.IsSuccess())
            {
                lunguInfo = JsonConvert.DeserializeObject<LunguInfoDTO>(response.Data.ToString());
            }
            else
            {
                Tools.LogDebug(response.Msg);
            }
            return lunguInfo;
        }

        private static async Task<float?> ProcessMeasureHeightAsync()
        {
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
            int measureHeightTimes = 0;
            while (measureHeightTimes < retry)
            {
                int setupNumber = int.Parse(PlcControl.plc.GetPlcValueString(DeviceKey.setupNumberKey));
                // 如果不相等，则记录值
                if (setupNumber != measureHeightTimes)
                {
                    string setupValue = PlcControl.plc.GetPlcValueString(DeviceKey.setupValueKey);
                    setupValueList.Add(Tools.GetFloatStringValue(setupValue));
                    measureHeightTimes = setupNumber;

                }
                await Task.Delay(200);
            }
            if (setupValueList.Count == 0)
            {
                MaterialSnackUtils.MaterialSnack("测高失败！", MaterialSnackUtils.SnackType.SUCCESS);
                return null;
            }
            // 计算3次的平均值，为测高值
            return setupValueList.Average();
        }

        /// <summary>
        /// 校准切割
        /// </summary>
        /// <returns>theta轴角度</returns>
        private static async Task<float> CalibratCutAsync()
        {
            return 0;
        }

        /// <summary>
        /// 磨刀切割
        /// </summary>
        /// <returns>theta轴角度</returns>
        private static async Task<float> CalibratSharpenAsync()
        {
            return 0;
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
                        await PlcControl.tagControl.cutting.StartCutAsync(1);
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
                        LineSegment? cutLine = CalculateSemicircleCuttingLine(thetaCenterPoint, cutStep.ThetaDeg, 2f, workpieceRadius, centerDistance, recordCutY);
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
                            await PlcControl.tagControl.cutting.StartCutAsync(1);
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

        private static float CalculateCutY(float cutY, float cutSize, CutDirection cutDirection)
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

        private static LineSegment? CalculateRectangleCuttingLine(DataPoint<float> thetaCenterPoint, DataRectangleF rectangleF, float rotationAngle, float cutY, float margin)
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
        private static LineSegment? CalculateSemicircleCuttingLine(DataPoint<float> thetaCenterPoint, float rotationAngle, float margin, float workpieceRadius, float centerDistance, float cutY)
        {
            //计算工件圆心坐标
            DataPoint<float> workpieceCenterPoint = GeometryUtils.RotatePoint(new DataPoint<float>(thetaCenterPoint.X, thetaCenterPoint.Y + centerDistance), thetaCenterPoint, rotationAngle);
            //计算切割线与工件圆的交点
            LineSegment? line = GeometryUtils.CalculateSemicircleIntersectionLine(workpieceCenterPoint, workpieceRadius, rotationAngle, cutY);
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

        public static List<int> GenerateNumberList(int number)
        {
            List<int> list = new List<int>();

            for (int i = 0; i <= number; i++)
            {
                list.Add(i);
            }

            return list;
        }
    }
}
