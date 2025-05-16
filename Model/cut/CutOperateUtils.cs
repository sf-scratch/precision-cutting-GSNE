using Emgu.CV.Dnn;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using 精密切割系统.database.db.modle;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.logs;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.ViewModel;

namespace 精密切割系统.Driver
{
    internal class CutOperateUtils
    {
        // true 运行中 false 空闲
        public static bool _disposed = false;
        // 检查状态 false 未检查完成 true 检查完成
        public static bool checkStatus = false;
        // 是否停机检查
        public static bool stopCheckFlag = true;
        // 如果切割方式是Z_KEEP 则是来回重复切 0 A(从左往右切)  1 Z_KEEP 左右来回切
        public static int cutMethod = 0;
        // 是否暂停
        public static bool pauseFlag = false;
        // 交换x轴的开始和结束位置
        public static bool exchangeXPosition = false;
        // 默认检查刀数
        public static int defaultCheckCutNum = 0;
        // 当前刀数
        public static int currentCutLine = 0;
        // 当前面刀数
        public static int chCurrentCutLine = 0;
        // 当前刀数
        public static string tempCurrentCutLine = "0";
        // 切割模式 0 全自动 1 半自动
        public static int cutType = 0;
        // 切割方向 0 前切 1 后切
        public static int cutDirection = -1;
        // x轴停止位置
        public static float xStopLocation = 0;
        // y轴停止位置
        public static float yStopLocation = 0;
        // 预切割信息
        static List<float> preSpeeds = new List<float>();
        // 是否启用预切割
        public static bool precutFlag = false;
        // 当前切割深度
        public static float _cutDepth;
        // 自定义切割刀数
        public static int _cutLineNum = 0;
        // 当前面总切割刀数
        public static int allRunCutLine = 0;
        // 刀片高度补偿
        // public static float bladeHeightComp = 0;
        // 进刀速度补偿
        public static float feedSpeedComp = 0;
        // 当前进刀速度
        public static float currentFeedSpeed = 0;
        // 是否一直重复循环
        public static bool repeatedFlag = false;
        // 循环次数
        public static int repeatedCount = 0;
        static float lastYCurrentPosition = -100;
        // 切割信息文件名
        static string cutInfoFileName = "logs/cutInfo.txt";
        // 切割光栅尺信息文件名
        static string cutRulerFileName = "logs/cutRulerInfo.txt";
        // theta轴是否校准
        public static bool thetaAlignFlag = false;
        // 是否蜂鸣提示
        public static bool buzzerTipFlag = true;
        // z轴开始位置
        static float zStartLocation = 0;
        // 暂停超时时间
        public static int stopDelayTime = 90;

        public static float globalXCutStartPosition = 0;
        public static float globalXCutEndPosition = 0;
        public static float globalYCutPosition = 0;
        public static float globalZCutPosition = 0;

        static bool absoluteCutFlag = false;
        static RightButton _startBtn;
        static MainWindow _mainWindow;
        static PositionCompensationModel axisModel = null;
        static CancellationTokenSource cts = new CancellationTokenSource();
        // 进行过程中是否校验异常 如果有，则不提示切割完成
        static bool errorFlag = false;
        // 重新初始化参数
        public static void InitParams(int _cutType, MainWindow mainWindow)
        {
            exchangeXPosition = false;
            precutFlag = false;
            _cutLineNum = 0;
            feedSpeedComp = 0;
            currentFeedSpeed = 0;
            repeatedCount = 0;
            cutMethod = 0;
            _disposed = false;
            checkStatus = false;
            pauseFlag = false;
            repeatedFlag = false;
            stopCheckFlag = true;
            buzzerTipFlag = true;
            defaultCheckCutNum = 0;
            currentCutLine = 0;
            allRunCutLine = 0;
            feedSpeedComp = 0;
            zStartLocation = 0;
            cutDirection = errorFlag ? cutDirection : - 1;
            cutType = _cutType;
            errorFlag = false;
            globalErrorFlag = false;
            GlobalParams.upPosition = -100;
            GlobalParams.upRealPosition = -100;
            lastYCurrentPosition = -100;
            yIndex = 0;
            absoluteCutFlag = false;
            stopDelayTime = 90;
            _mainWindow = mainWindow;
        }
        /// <summary>
        /// 执行切割逻辑
        /// </summary>
        public static async void runCut(int cutLineNum)
        {
            // 判断是否已准备好切割
            if (!IsReadyToCut())
            {
                MaterialSnackUtils.MaterialSnack("切割未准备好！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }

            BladeHeightModel bladeHeightModel = CurrentUtils.GetBladeHeightModel();

            // 判断是否已测高
            if (string.IsNullOrEmpty(bladeHeightModel.BladeHeight) || bladeHeightModel.BladeHeight.Equals("0"))
            {
                MaterialSnackUtils.MaterialSnack("请先测高！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }

            float bladeHeight = float.Parse(bladeHeightModel.BladeHeight); // 设置刀具高度，单位毫米
            long id = CurrentUtils.GetCurrentConfiguration().DeviceDataId;

            // 判断是否确认配置信息
            if (id == 0)
            {
                MaterialSnackUtils.MaterialSnack("未确认配置信息！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }

            // 判断切割方向
            if (cutDirection == -1)
            {
                MaterialSnackUtils.MaterialSnack("请设置切割方向！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }

            // 查询配置信息
            var listConf = await SqlHelper.TableAsync<FileTableItemModel>().Where(t => t.Id == id).ToListAsync();
            if (listConf.Count == 0)
            {
                MaterialSnackUtils.MaterialSnack("未确认配置信息！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            errorFlag = false;
            FileTableItemModel _model = listConf[0];
            string cuttingChSeq = _model.CuttingChSeq;
            float workbenchCh1 = _model.WorkbenchCh1;
            float tapeThickness = float.Parse(_model.TapeThickness); // 胶带厚度
            float workThickness = float.Parse(_model.WorkThickness); // 工件厚度
            float squareCh1 = float.Parse(_model.SquareCh1);
            float z2StopLocation = GlobalParams.lastFocusZ2Location;
            // 获取补偿数据模型
            List<PositionCompensationModel> models = CurrentUtils.GetPositionCompensationModels();
            axisModel = models.Find(item => item.AxisType.Equals(DeviceKey.yName + (cutDirection == 1 ? "-反向" : "")));
            _cutLineNum = cutLineNum;
            // 参数校验
            if (_model.SpindleRev == 0 || _model.SpindleRev > 30000)
            {
                Tools.LogError("切割参数配置错误！");
                MaterialSnackUtils.MaterialSnack("切割参数配置错误！", MaterialSnackUtils.SnackType.WARNING);
                errorFlag = true;
                return;
            }

            // 查询通道信息
            List<FileTableItemChModel> chModels = await SqlHelper.TableAsync<FileTableItemChModel>()
                .Where(t => t.ItemId == _model.Id).ToListAsync();
            int[] chSeqs = Tools.StringToIntegerArray(cuttingChSeq);
            // 初始化相关位置和偏移量
            float thetaCenterLocationX = GlobalParams.thetaCenterLocationX;
            xStopLocation = float.Parse(PlcControl.plc.GetPlcValueString(DeviceKey.curLocationKey));
            
            PlcControl.tagControl.cutting.StopCut(0); // 修改停止信号，防止中途错误触发
            cts = new CancellationTokenSource();
            tempCurrentCutLine = "0";
            // 各状态检查正确后，设置开始状态
            _disposed = true;
            CheckError();
            preSpeeds = [];
            var startTime = DateTime.Now;
            float tempCutAllDistance = GlobalParams.cutAllDistance;
            try
            {
                await Task.Run(() =>
                {
                    bool flag = true;
                    do
                    {
                        repeatedCount++;
                        disposeCut(chSeqs, chModels, flag, workbenchCh1, squareCh1
                             , bladeHeight, tapeThickness, workThickness, thetaCenterLocationX, z2StopLocation, _model);
                    } while (repeatedFlag && _disposed);
                    // 处理循环
                    Debug.WriteLine("结束切割!");
                    // 结束后，回到停止位置
                    Thread.Sleep(10);
                    PlcControl.tagControl.cutting.EndFullAutoCut();
                    Thread.Sleep(100);
                    StopCut();
                    // 记录日志
                    RunLogsCommon.LogEvent(LogType.Cut, new List<RunLogsViewModel>
                    {
                        new RunLogsViewModel(LogType.Cut, "切割"),
                        new RunLogsViewModel("型号参数ID", _model.DeviceDataId),
                        new RunLogsViewModel("耗时", (DateTime.Now - startTime).TotalSeconds.ToString("F2") + "sec"),
                        new RunLogsViewModel("切割长度", (GlobalParams.cutAllDistance - tempCutAllDistance).ToString("F2") + "mm"),
                        new RunLogsViewModel("刀数", currentCutLine.ToString()),
                        new RunLogsViewModel("结果", "OK")
                    });
                });
            }
            catch (Exception ex)
            {
                // 捕获异常并提示错误消息
                MaterialSnackUtils.MaterialSnack($"切割过程中出现错误: {ex.Message}", MaterialSnackUtils.SnackType.ERROR);
                Tools.LogError($"切割过程中出现错误: {ex.Message}");
                errorFlag = true;
                StopCut();
            }
        }

        public static void disposeCut(int[] chSeqs, List<FileTableItemChModel> chModels, bool flag, float workbenchCh1, float squareCh1
            , float bladeHeight, float tapeThickness, float workThickness, float thetaCenterLocationX, float z2StopLocation, FileTableItemModel _model)
        {
            // float yCurrentPosition = float.Parse(PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey));
            // bool setStartCutFlag = repeatedCount == 1 ? true : false;
            bool setStartCutFlag = true;
            foreach (int chIndex in chSeqs)
            {
                int tempChIndex = chIndex - 1;
                // 判断是否取消任务
                if (cts.Token.IsCancellationRequested)
                    return;
                float yStartCutPosition = 0;
                // 获取当前子ch信息 
                FileTableItemChModel ch = chModels[tempChIndex];
                if (string.IsNullOrEmpty(ch.AbsoluteCutPosition))
                {
                    absoluteCutFlag = false;
                    // 获取当前切割面的开始位置
                    switch (ch.ChName)
                    {
                        case "Ch 1":
                            yStartCutPosition = GlobalParams.ch1CutStartPosition;
                            break;
                        case "Ch 2":
                            yStartCutPosition = GlobalParams.ch2CutStartPosition;
                            break;
                        case "Ch 3":
                            yStartCutPosition = GlobalParams.ch3CutStartPosition;
                            break;
                        case "Ch 4":
                            yStartCutPosition = GlobalParams.ch4CutStartPosition;
                            break;
                        default:
                            break;
                    }
                } else
                {
                    absoluteCutFlag = true;
                    // 设置绝对切割位置
                    yStartCutPosition = Tools.GetFloatStringValue(ch.AbsoluteCutPosition);
                }
                
                if (ch.CutLine == null || ch.CutLine.Equals("0"))
                {
                    MaterialSnackUtils.MaterialSnack($"{ch.ChName} 参数异常！", MaterialSnackUtils.SnackType.WARNING);
                    Tools.LogError($"{ch.ChName} 参数异常！");
                    errorFlag = true;
                    break;
                }
                // 设置当前切割面
                CurrentUtils.UpdateCurrentCh(ch.ChName);
                // 处理每个通道的切割序列
                flag = ProcessCutSequence(ch, workbenchCh1, squareCh1, bladeHeight, tapeThickness, workThickness,
                                   thetaCenterLocationX, ref yStartCutPosition, GlobalParams.cameraOffsetX
                                   , ref z2StopLocation, _model.SpindleRev.ToString(), setStartCutFlag);
                setStartCutFlag = false;
                if (!flag)
                {
                    break;
                }
            }
        }
        /// <summary>
        /// 预切割
        /// </summary>
        public static void preCut()
        {
            // 查询当前配置获取预切割开始编号
            FileTableItemModel fileTableItemModel = CurrentUtils.GetFileTableItemModel();
            // 查询当前预切割流程信息
            PreCutModel preCutModel = CurrentUtils.GetPreCutModel();
            if (preCutModel.NewBladeNo == 0)
            {
                return;
            }
            // 获取
            float[] feedSpds = Tools.StringToFloatArray(preCutModel.FeedSpd); // 获取进刀速度
            float[] ofLinesList = Tools.StringToFloatArray(preCutModel.OfLines); // 获取切割刀数

            int num = 0;
            // 从预切割开始编号开始
            for (int i = preCutModel.NewBladeNo; i <= feedSpds.Length; i++)
            {
                // 获取进刀速度
                float feedspeed = feedSpds[i - 1];
                float cutLine = ofLinesList[i - 1];
                // 循环刀数
                for (int j = 0; j < cutLine; j++)
                {
                    preSpeeds.Add(feedspeed);
                    num++;
                }
            }
        }

        /// <summary>
        /// 处理切割序列的逻辑
        /// </summary>
        private static bool ProcessCutSequence(FileTableItemChModel ch, float workbenchCh1, float squareCh1, float bladeHeight,
                                               float tapeThickness, float workThickness, float thetaCenterLocationX,
                                               ref float yCurrentPosition, float cameraOffsetX, ref float z2StopLocation
                                                , string spindleRev, bool setStartCutFlag)
        {
            float[] cutDepths = Tools.StringToFloatArray(ch.DepthSteps); // 获取切割深度
            float[] feedSpeeds = Tools.StringToFloatArray(ch.FeedSpeed); // 获取进给速度
            float[] yIndexs = Tools.StringToFloatArray(ch.YIndex);       // 获取Y轴偏移
            float[] cutLines = Tools.StringToFloatArray(ch.RepeatTimes); // 获取重复次数
            string[] loops = Tools.StringToStringArray(ch.Loop);         // 获取循环控制信息
            float[] setBladeHeight = Tools.StringToFloatArray(ch.BladeHeight);         // 设置的刀片高度
            string thetaDeg = ch.ThetaDeg; // 面的角度
            Tools.LogInfo($"ch.ThetaDeg:{ch.ThetaDeg}");
            // 检查索引是否连续
            int maxIndex = CutUtils.AreIndexesContinuous(setBladeHeight, feedSpeeds, yIndexs, cutLines);
            if (maxIndex == 0)
            {
                MaterialSnackUtils.MaterialSnack("切割参数错误！", MaterialSnackUtils.SnackType.ERROR);
                Tools.LogError("切割参数错误！");
                errorFlag = true;
                return false;
            }
            int chCutLines = Tools.GetIntStringValue(ch.CutLine);
            // 生成子序列
            string[] subArray = new string[maxIndex];
            Array.Copy(loops, 0, subArray, 0, maxIndex);
            List<string> repetitions = new List<string>(subArray);
            List<int> sequences = CutUtils.GenerateNumberList(maxIndex);
            List<int> newSeq = CutUtils.CombineSequences(sequences, repetitions);
            // 生成与切割相关数据
            preCut();
            bool positionLimitFlag = PositionCheck(newSeq, yIndexs, cutLines, setBladeHeight, yCurrentPosition);
            // 如果位置超限，则提示
            if (!positionLimitFlag)
            {
                MaterialSnackUtils.MaterialSnack("切割参数错误，结束位置超限！", MaterialSnackUtils.SnackType.ERROR);
                Tools.LogError("切割参数错误，结束位置超限！");
                errorFlag = true;
                return false;
            }
            chCurrentCutLine = 0;
            yIndex = 0;
            allRunCutLine = _cutLineNum > 0 ? (_cutLineNum > chCutLines ? chCutLines : _cutLineNum) : chCutLines;
            Tools.LogInfo($"currentCutLine:{currentCutLine}  chCutLines: {chCutLines} allRunCutLine:{allRunCutLine}");
            // 执行切割步骤
            do
            {
                seqFirstFlag = true;
                for (int num = 0; num < newSeq.Count; num++)
                {
                    if (cts.Token.IsCancellationRequested)
                        return false;
                    if (chCurrentCutLine >= chCutLines)
                    {
                        break;
                    }
                    int i = newSeq[num] - 1;
                    if (i + 1 > setBladeHeight.Length)
                        break;

                    // 执行每一步的切割
                    bool errorFlag = ExecuteCutStep(i, cutDepths, feedSpeeds, yIndexs, cutLines, thetaCenterLocationX, ref yCurrentPosition,
                                   workbenchCh1, squareCh1, bladeHeight, tapeThickness, workThickness, cameraOffsetX,
                                   ref z2StopLocation, ref setStartCutFlag, thetaDeg, spindleRev, chCutLines, ref chCurrentCutLine, setBladeHeight);
                    if (!errorFlag)
                    {
                        return false;
                    }
                }
                Debug.WriteLine($"currentCutLine:{currentCutLine} chCurrentCutLine:{chCurrentCutLine}  chCutLines: {chCutLines}");
            } while (chCurrentCutLine < chCutLines);
            
            return true;
        }

        static bool seqFirstFlag = false;
        static int seqFirstCount = 0;

        /// <summary>
        /// 根据当前位置和所有切割刀数的步进总和，判断是否超过Y轴限位
        /// </summary>
        /// <param name="newSeq"></param>
        /// <param name="yIndexs"></param>
        /// <param name="cutLines"></param>
        /// <param name="bladeHeight"></param>
        /// <param name="yCurrentPosition"></param>
        /// <returns></returns>
        private static bool PositionCheck(List<int> newSeq, float[] yIndexs, float[] cutLines, float[] bladeHeight, float yCurrentPosition)
        {
            int checkCutNum = 0;
            foreach (int seq in newSeq)
            {
                int index = seq - 1;
                if (index < 0 || index >= bladeHeight.Length)
                    break;

                float yIndex = yIndexs[index];   // 当前Y轴索引
                int cutLine = (int)cutLines[index];  // 当前重复次数

                if (cutLine <= 0)
                    continue;

                // 获取上下限值
                float upperLimit = Tools.GetFloatStringValue(PlcControl.allTags[DeviceKey.ySoftUpperLimitKey].defaultValue);
                float lowerLimit = Tools.GetFloatStringValue(PlcControl.allTags[DeviceKey.ySoftLowerLimitKey].defaultValue);

                for (int k = 0; k < cutLine; k++)
                {
                    // 根据切割方向调整Y轴位置并判断是否超限
                    if (cutDirection == 0) // 前切
                    {
                        yCurrentPosition += (k == 0 ? 0 : yIndex);
                        if (yCurrentPosition > upperLimit)
                            return false;
                    }
                    else if (cutDirection == 1) // 后切
                    {
                        yCurrentPosition -= (k == 0 ? 0 : yIndex);
                        if (yCurrentPosition < lowerLimit)
                            return false;
                    }
                    // 如果当前刀数等于设置的切割刀数，则结束切割
                    if (checkCutNum == _cutLineNum)
                    {
                        return true;
                    }
                    checkCutNum++;
                }
            }

            return true;
        }

        static int timeout = 90;
        static float yIndex = 0;
        /// <summary>
        /// 执行切割步骤
        /// </summary>
        private static bool ExecuteCutStep(int i, float[] cutDepths, float[] feedSpeeds, float[] yIndexs, float[] cutLines,
                                           float thetaCenterLocationX, ref float yCurrentPosition, float workbenchCh1, float squareCh1,
                                           float bladeHeight, float tapeThickness, float workThickness,
                                           float cameraOffsetX, ref float z2StopLocation
            , ref bool setStartCutFlag, string thetaDeg, string spindleRev, int chCutLines, ref int chCurrentCutLine, float[] setBladeHeights)
        {
            float cutDepth = cutDepths[i];    // 当前切割深度

            float feedSpeed = feedSpeeds[i];  // 当前进给速度
            float cutBladeHeight = setBladeHeights[i];  // 当前切割高度

            // float yIndex = yIndexs[i];        // 当前Y轴索引
            float cutLine = cutLines[i];      // 当前重复次数
            // 加上设置角度
            float tempThetaDeg = Tools.GetFloatStringValue(thetaDeg) + GlobalParams.calibrationAngle;
            if (cutLine == 0)
                return true;
            Debug.WriteLine($"i {i} yIndex {yIndex} cutDepth {cutDepth} cutBladeHeight {cutBladeHeight}");
            float targetDepth = 0;
            float nextDepth = 0;
            // 根据重复次数循环切割, 如果循环完了后
            int steps = 0;
            bool cleanSteps = false; 
            for (int k = 0; k < cutLine; k++)
            {
                if (cts.Token.IsCancellationRequested)
                    return false;

                // 如果是手动切割，则获取页面的深度补偿数据
                if (cutType == 1)
                {
                    /*MQSemiAutomaticCuttingConf control = MainForm.Instance.GetControlOfType<MQSemiAutomaticCuttingConf>
                    (typeof(MQSemiAutomaticCuttingConf), MainForm.PanelType.Main);
                    GlobalParams.cutDepthOffset = control.GetDepthCompensation();*/
                }
                // cutDepth大于0 说明设置了setep 深度模式
                if (cutDepth > 0)
                {
                    // 计算目标切割深度
                    float startCutPosition = bladeHeight - tapeThickness - workThickness;
                    targetDepth = (targetDepth == 0 ? startCutPosition : targetDepth) + cutDepth;

                    // 校正目标深度，确保不超过最大允许深度
                    if (targetDepth >= (bladeHeight - cutBladeHeight))
                    {
                        targetDepth = bladeHeight - cutBladeHeight;
                        cleanSteps = true;
                    }
                    else
                    {
                        cleanSteps = false;
                        k--;  // 如果未达到最大深度，则调整k的值
                    }
                    steps++;
                }
                else
                {
                    // 如果cutDepth <= 0，直接设置目标深度为固定值
                    targetDepth = bladeHeight - cutBladeHeight;
                    cleanSteps = true;
                }

                _cutDepth = cutDepth;
                // Z轴下降位置 = 刀片测高高度 - 设置刀片高度 - 补偿高度
                float zEndIndex = targetDepth - GlobalParams.depthComp;
                // 如果是每个通达的第一刀和第二刀，则抬起来0.5微米
                if (seqFirstFlag && GlobalParams.zAxisCompNum > 0)
                {
                    zEndIndex += GlobalParams.zAxisCompValue;
                    seqFirstCount++;
                    if (seqFirstCount == GlobalParams.zAxisCompNum)
                    {
                        seqFirstFlag = false;
                        seqFirstCount = 0;
                    }
                    Tools.LogInfo($"zEndIndex:{zEndIndex} GlobalParams.zAxisCompValue:{GlobalParams.zAxisCompValue} GlobalParams.zAxisCompNum:{GlobalParams.zAxisCompNum}");
                }
                if (zEndIndex >= bladeHeight)
                {
                    MaterialSnackUtils.MaterialSnack("Z1轴位置超限！", MaterialSnackUtils.SnackType.ERROR);
                    Tools.LogError("Z1轴位置超限！");
                    errorFlag = true;
                    return false;
                }
                // 设置/计算切割相关参数
                float xOffset = 10f;
                float avgWorkbenchCh1 = workbenchCh1 / 2;

                // Z轴开始位置 = Z轴下降位置 - 2
                // float zStartLocation = zEndIndex - 2;
                if (zStartLocation < 1)
                {
                    zStartLocation = zEndIndex - GlobalParams.zCutRaisedHeight;
                }
                // X轴开始位置
                float xStartLocation = thetaCenterLocationX - avgWorkbenchCh1 - xOffset;
                // X轴结束位置
                float xEndLocation = xStartLocation + squareCh1 + xOffset;
                // 如果是第一刀，则加上和相机的偏移量
                if (chCurrentCutLine == 0 && !absoluteCutFlag)
                {
                    yCurrentPosition += GlobalParams.cameraOffsetY;
                }
                // 设置切割方向 0 前切 1 后切
                if (cutDirection == 0)
                {
                    // Y轴切割位置
                    // yCurrentPosition += (currentCutLine == 0 ? 0 : yIndex);
                    if (steps < 2)
                    {
                        yCurrentPosition += yIndex;
                    }
                }
                else if (cutDirection == 1)
                {
                    // Y轴切割位置
                    // yCurrentPosition -= (currentCutLine == 0 ? 0 : yIndex);
                    if (steps < 2)
                    {
                        yCurrentPosition -= yIndex;
                    }
                }
                float setFeedSpeed = feedSpeed;
                // 如果启用预切割，则读取预切割配置
                if (precutFlag)
                {
                    if (preSpeeds.Count > currentCutLine)
                    {
                        float tempFeedSpeed = preSpeeds[currentCutLine];
                        // 如果预切割速度大于设置进刀速度，则不生效
                        if (tempFeedSpeed <= feedSpeed && tempFeedSpeed > 0)
                        {
                            setFeedSpeed = tempFeedSpeed;
                        }
                        Debug.WriteLine("当前进刀速度：" + setFeedSpeed);
                    }
                }
                // 加上高度补偿
                setFeedSpeed += feedSpeedComp;
                if (setFeedSpeed > 150)
                {
                    MaterialSnackUtils.MaterialSnack("切割速度超限！", MaterialSnackUtils.SnackType.ERROR);
                    Tools.LogError("切割速度超限！");
                    errorFlag = true;
                    return false;
                }
                float tempXStartPosition = xStartLocation;
                float tempXEndPosition = xEndLocation;
                float tempSetFeedSpeed = setFeedSpeed;
                // 如果切割模式是 1 则交替x的开始和结束位置
                if (cutMethod == 1)
                {
                    // 如果是要交换，则替换X开始和结束位置
                    if (exchangeXPosition)
                    {
                        xStartLocation = tempXEndPosition;
                        xEndLocation = tempXStartPosition;
                        tempSetFeedSpeed = 5;
                        exchangeXPosition = false;
                    } else
                    {
                        exchangeXPosition = true;
                    }
                }
                // 设置切割参数并调用API执行切割
                float setYPosition = SetCutParams(tempSetFeedSpeed, zEndIndex, zStartLocation, xStartLocation, xEndLocation
                    ,ref yCurrentPosition, "0", tempThetaDeg.ToString(), spindleRev, cutDirection, yIndex);

                yStopLocation = setYPosition - GlobalParams.cameraOffsetY;
                Debug.WriteLine($"setYPosition:{setYPosition} GlobalParams.cameraOffsetY: {GlobalParams.cameraOffsetY}");
                PlcControl.tagControl.cutting.SetStopLocation(xStopLocation, yStopLocation, z2StopLocation);
                Tools.LogInfo($"xStopLocation:{xStopLocation} yStopLocation:{yStopLocation} z2StopLocation:{z2StopLocation}");
                Thread.Sleep(10);  // 等待设备准备
                // 开始切割
                if (setStartCutFlag)
                {
                    // 如果是开始切割，则先往Y轴切割位置前面一点，消除回程误差
                    string clearYErrorPosition = (setYPosition + 2).ToString();
                    PlcControl.tagControl.Yaxis.StartAbsolute("10", clearYErrorPosition);
                    Thread.Sleep(300);
                    Tools.WaitForValue(PlcControl.allTags[DeviceKey.yCurLocationKey], clearYErrorPosition);
                    Tools.WaitForValue(PlcControl.allTags[DeviceKey.yCurSpeedKey], "0");
                    Thread.Sleep(500);
                    PlcControl.tagControl.Yaxis.StartAbsolute("10", (setYPosition + 1).ToString());
                    Thread.Sleep(1000);
                    PlcControl.tagControl.cutting.StartCut(0);
                    Thread.Sleep(10);
                    PlcControl.tagControl.cutting.StartCut(1);
                    setStartCutFlag = false;
                    Tools.LogInfo("发送开始切割信号！");
                    if (MonitorCutStatus())
                    {
                        _mainWindow.NavigateToPage("Pages/F2_ManualOperation/MQSemiAutomaticCuttingRun");
                        MaterialSnackUtils.MaterialSnack("切割中....", MaterialSnackUtils.SnackType.SUCCESS, 0);
                    }
                }

                // 增加切割距离
                float cutDistance = (float)Math.Round(Math.Abs(xEndLocation - xStartLocation) / 1000, 3);
                Thread.Sleep(100);
                string currentCount = "0";
                do
                {
                    // 定期检查切割进度
                    currentCount = PlcControl.plc.GetPlcValueString(DeviceKey.cutNumKey);
                    Thread.Sleep(50);
                    // 再检查是否有报警信息，有报警则暂停
                    if (cts.Token.IsCancellationRequested)
                        return false;
                } while (tempCurrentCutLine.Equals(currentCount));
                Thread.Sleep(500);
                lastYCurrentPosition = Tools.GetFloatStringValue(PlcControl.plc.GetPlcDefaultValueString(DeviceKey.yCurLocationKey));
                Debug.WriteLine($"当前切割刀数-PLC：{currentCount}  : lastYCurrentPosition:{lastYCurrentPosition}");
                Tools.LogInfo("Y轴光栅尺位置：" + PlcControl.plc.GetPlcDefaultValueString(DeviceKey.yGratingRulerCurLocationKey));
                Tools.LogInfo("Z1轴光栅尺位置：" + PlcControl.plc.GetPlcDefaultValueString(DeviceKey.z1GratingRulerCurLocationKey));
                Tools.WriteLineToFile(
                    $"{DateTime.Now}\t第{currentCutLine}刀" +
                    $"\t{PlcControl.plc.GetPlcDefaultValueString(DeviceKey.yCurLocationKey)}" +
                    $"\t{PlcControl.plc.GetPlcDefaultValueString(DeviceKey.yGratingRulerCurLocationKey)}" +
                    $"\t{zEndIndex}" +
                    $"\t{PlcControl.plc.GetPlcDefaultValueString(DeviceKey.z1GratingRulerCurLocationKey)}" +
                    $"\t{PlcControl.plc.GetPlcDefaultValueString(DeviceKey.z1CurLocationKey)}" + $"\t实时位置"
                    , cutRulerFileName);
                tempCurrentCutLine = currentCount;
                currentFeedSpeed = setFeedSpeed;
                SetCutRecord(cutDistance);
                currentCutLine++;  // 增加已完成的刀数
                if (steps < 2)
                {
                    chCurrentCutLine++; // 增加当前ch切割刀数
                }
                if (cleanSteps)
                {
                    steps = 0;
                    targetDepth = 0;
                    cleanSteps = false;
                }
                yIndex = yIndexs[i];
                // 如果当前刀数等于设置的切割刀数，则结束切割
                if (_cutLineNum > 0 && tempCurrentCutLine.Equals(_cutLineNum.ToString()))
                {
                    return false;
                }
                if ((chCurrentCutLine >= chCutLines) && cleanSteps)
                {
                    break;
                }
                // 监听Z轴是否上升，如果上升，则表面当前刀已完成 20.58 21
                Stopwatch stopwatch = Stopwatch.StartNew();
                while (stopwatch.Elapsed.TotalSeconds < timeout)
                {
                    String runValue = PlcControl.plc.GetPlcValueString(DeviceKey.z1CurLocationKey);
                    if (float.Parse(zStartLocation.ToString()) - float.Parse(runValue) < -0.01)
                    {
                        continue;
                    }
                    else
                    {
                        stopwatch.Stop();
                        break;
                    }
                }
                stopwatch.Stop();

                Debug.WriteLine($"checkStatus:{checkStatus}  chCutLines:{chCutLines}");
                if (checkStatus && k < chCutLines - 1)
                {
                    pauseFlag = true;
                    if (MonitorCutStatusFalse("False", stopDelayTime * 1000))
                    {
                        /*PlcControl.tagControl.wholeDevice.SetYellowLightFlash(1);
                        PlcControl.tagControl.wholeDevice.SetBuzzerStatus(1);

                        _mainWindow.NavigateToPage("Pages/F2_ManualOperation/MQSemiAutomaticCuttingStop");*/
                        MaterialSnackUtils.MaterialSnack("暂停中...", MaterialSnackUtils.SnackType.WARNING, 0);
                    }
                    else
                    {
                        MaterialSnackUtils.MaterialSnack("暂停失败！强行退出切割状态！", MaterialSnackUtils.SnackType.WARNING, 0);
                        // 如果停止失败，则强行结束切割
                        Tools.LogError("暂停失败！强行退出切割状态！");
                    }

                    PlcControl.tagControl.wholeDevice.SetYellowLightFlash(1);
                    // PlcControl.tagControl.wholeDevice.SetBuzzerStatus(1);
                    // 关水
                    CloseCutWater();
                    _mainWindow.NavigateToPage("Pages/F2_ManualOperation/MQSemiAutomaticCuttingStop");
                    setStartCutFlag = true;
                }
                // 如果是停机中，则暂停运行
                while (pauseFlag)
                {
                    if (cts.Token.IsCancellationRequested)
                        return false;
                    Thread.Sleep(50);
                }
            }
            return true;
        }
        /// <summary>
        /// 设置清零后的刀数和长度信息
        /// </summary>
        /// <param name="cutDistance">切割长度</param>
        public static void SetClearedCutInfo(float cutDistance)
        {
            CurrentConfigurationModel currentConfigurationModel = CurrentUtils.GetCurrentConfiguration();
            currentConfigurationModel.ClearedCutAllNum++;
            currentConfigurationModel.ClearedCutAllDistance += cutDistance;
            CurrentUtils.UpdateCurrentConfiguration(currentConfigurationModel);
            CurrentUtils.UpdateParams();
        }
        public static void SetCutRecord(float cutDistance)
        {
            // 增加总共切割刀数
            GlobalParams.cutAllNum++;
            GlobalParams.heightCutAllNum++;
            GlobalParams.cutAllDistance += cutDistance;
            GlobalParams.heightCutAllDistance += cutDistance;
            SetClearedCutInfo(cutDistance);
        }
        public static int GetCutMethod(string cutMethod)
        {
            return cutMethod switch
            {
                "A" => 0,
                "B_ZKEEP" => 1,
                _ => 0 // 默认值
            };
        }

        /// <summary>
        /// 设置切割参数
        /// </summary>
        public static float SetCutParams(float feedSpeed, float zEndIndex, float zStartLocation, float xStartLocation,
                                         float xEndLocation, ref float yCutLocation, string checkFlag, string thetaDeg, string spindleRev
            ,int cutDirectionValue, float stepIndex, bool compFlag = true)
        {
            string tempYCutLocation = yCutLocation.ToString();
            // 增加Y轴补偿
            if (currentCutLine > 0 && compFlag)
            {
                // 绝对位置线性插补
                tempYCutLocation = PlcControl.GetCompensate(yCutLocation + "", DeviceKey.yName, cutDirection);
            }
            Tools.LogInfo("切割数据：电机位置：" + yCutLocation + "  补偿位置：" + tempYCutLocation);
            // 记录切割数据
            Tools.LogInfo("第" + currentCutLine + "刀切割数据：feedSpeed:" + feedSpeed + " zEndIndex:" + zEndIndex + " tempZEndIndex:" + " zStartLocation:" + zStartLocation
                + " xStartLocation:" + xStartLocation + " xEndLocation:" + xEndLocation 
                + " yCutLocation:" + yCutLocation + " tempYCutLocation:" + tempYCutLocation + " checkFlag:"
                + checkFlag + " thetaDeg:" + thetaDeg + " spindleRev:" + spindleRev);
            Tools.WriteLineToFile($"绝对定位\t{DateTime.Now}\t第{currentCutLine}刀\t{yCutLocation}\t{tempYCutLocation}", cutInfoFileName);
            // yCutLocation = Tools.GetFloatStringValue(tempYCutLocation);
            PlcControl.tagControl.cutting.SetCutParams(feedSpeed, zEndIndex.ToString(), zStartLocation, xStartLocation.ToString()
                , xEndLocation.ToString(), tempYCutLocation, checkFlag, thetaDeg, spindleRev, cutDirectionValue);
            globalXCutStartPosition = xStartLocation;
            globalXCutEndPosition = xEndLocation;
            globalYCutPosition = Tools.GetFloatStringValue(tempYCutLocation);
            globalZCutPosition = zEndIndex;
            return Tools.GetFloatStringValue(tempYCutLocation);
        }
        /// <summary>
        /// 设置切割参数
        /// </summary>
        public static void SetCutParams1(float feedSpeed, float zEndIndex, float zStartLocation, float xStartLocation,
                                         float xEndLocation, ref float yCutLocation, string checkFlag, string thetaDeg, string spindleRev
            ,int cutDirectionValue, float stepIndex, bool compFlag = true)
        {
            // 增加X轴补偿
            // string tempXStartLocation = PlcControl.GetCompensate(xStartLocation + "", DeviceKey.xName, cutDirection);
            // string tempXEndLocation = PlcControl.GetCompensate(xEndLocation + "", DeviceKey.xName, cutDirection);
            string tempYCutLocation = yCutLocation.ToString();
            // 增加Y轴补偿
            if (currentCutLine > 0 && compFlag)
            {
                tempYCutLocation = GetAxisCompensate(yCutLocation, cutDirection, stepIndex);
            }
            // 增加Z轴补偿
            // string tempZEndIndex = PlcControl.GetCompensate(zEndIndex + "", DeviceKey.z1Name, 0);

            Tools.LogInfo("切割数据：电机位置：" + yCutLocation + "  补偿位置：" + tempYCutLocation);
            // 记录切割数据
            Tools.LogInfo("第" + currentCutLine + "刀切割数据：feedSpeed:" + feedSpeed + " zEndIndex:" + zEndIndex + " tempZEndIndex:" + " zStartLocation:" + zStartLocation
                + " xStartLocation:" + xStartLocation + " xEndLocation:" + xEndLocation 
                + " yCutLocation:" + yCutLocation + " tempYCutLocation:" + tempYCutLocation + " checkFlag:"
                + checkFlag + " thetaDeg:" + thetaDeg + " spindleRev:" + spindleRev);
            Tools.WriteLineToFile($"{DateTime.Now}\t第{currentCutLine}刀\t{yCutLocation}\t{tempYCutLocation}", cutInfoFileName);
            yCutLocation = Tools.GetFloatStringValue(tempYCutLocation);
            PlcControl.tagControl.cutting.SetCutParams(feedSpeed, zEndIndex.ToString(), zStartLocation, xStartLocation.ToString()
                , xEndLocation.ToString(), tempYCutLocation, checkFlag, thetaDeg, spindleRev, cutDirectionValue);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetLocation">目标切割位置（电机）</param>
        /// <param name="directionType">方向 0 正向 1 反向</param>
        public static string GetAxisCompensate(float targetLocation, int directionType, float stepIndex)
        {
            // 获取当前点位(上一点位)的补偿数据
            float lastLocationComp = PlcControl.CalculateCompensation(axisModel, lastYCurrentPosition, directionType);
            float compTargetLocation = lastYCurrentPosition + (directionType == 0 ? stepIndex : -stepIndex);
            float targetLocationComp = PlcControl.CalculateCompensation(axisModel, compTargetLocation, directionType);
            // 用目标点位的补偿值 - 上一目标值的补偿值 = 2个点之间的差值
            float tempComp = (float)Math.Round(targetLocationComp - lastLocationComp, GlobalParams.decimalPlaces);
            float comp = tempComp / 2;
            float tempTargetLocation = targetLocation;
            if (directionType == 0)
            {
                tempTargetLocation += comp;
            }
            else if (directionType == 1)
            {
                tempTargetLocation -= comp;
            }
            Tools.WriteLineToFile($"{DateTime.Now}\t{lastYCurrentPosition}\t{compTargetLocation}\t{lastLocationComp}\t{targetLocationComp}" +
                $"\t{targetLocation}\t{tempTargetLocation}\t{tempComp}\t{comp}\t{directionType}", "logs/compInfo.txt");
            return tempTargetLocation.ToString("F6");
        }

        /// <summary>
        /// 检查异常状态 急停后，要全部重新标定一次
        /// </summary>
        /// <returns></returns>
        private static void CheckError()
        {
            Thread thread = new Thread(() =>
            {
                while (_disposed)
                {
                    if (AlarmConfig.Instance.HasActiveAlarm())
                    {
                        Debug.WriteLine("异常报警！");
                        globalErrorFlag = true;
                        _disposed = false;
                        cts.Cancel();
                    }
                    Thread.Sleep(50);
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        // 异常标识
        static bool globalErrorFlag = false;

        public static void exitCut()
        {
            MaterialSnackUtils.MaterialSnack("正在停止切割...", MaterialSnackUtils.SnackType.WARNING, 0);
            // 发送结束信号
            PlcControl.tagControl.cutting.EndFullAutoCut();
            cts.Cancel();
            _disposed = false;
        }

        public static void StopCut()
        {
            InitParams(cutType, _mainWindow);
            // 如果异常，则直接退出切割
            if (globalErrorFlag)
            {
                PlcControl.tagControl.cutting.EnterFullAutoInit(0);
                MaterialSnackUtils.MaterialSnack("异常退出", MaterialSnackUtils.SnackType.ERROR, 0);
                Tools.LogInfo("异常退出...");
                Debug.WriteLine("异常退出...");
                GlobalParams.globalRunFlag = false;
                _mainWindow.NavigateToPage("Pages/F2_ManualOperation/MQSemiAutomaticCuttingConf", "type=1");
            } else
            {
                if (errorFlag)
                {
                    // 切割完成
                    GlobalParams.globalRunFlag = false;
                    Tools.LogInfo("参数异常退出...");
                    Debug.WriteLine("参数异常退出...");
                    _mainWindow.NavigateToPage("Pages/F2_ManualOperation/MQSemiAutomaticCuttingConf", "type=1");
                    return;
                }
                Tools.LogInfo("正常退出...");
                Debug.WriteLine("正常退出...");
                Task.Run(() => {
                    if (MonitorCutStatusFalse())
                    {
                        /*if (CommonCheck.GetParamsStatus(DeviceKey.workpieceBlowingStatusKey))
                        {
                            // 吹气4秒
                            Thread.Sleep(4000);
                            PlcControl.tagControl.wholeDevice.SetWorkpieceBlowing();
                        }*/
                        Debug.WriteLine("正常退出成功...");
                        _mainWindow.NavigateToPage("Pages/F2_ManualOperation/MQSemiAutomaticCuttingConf", "type=1");
                        MaterialSnackUtils.MaterialSnack("切割完成！", MaterialSnackUtils.SnackType.SUCCESS);
                        GlobalParams.globalRunFlag = false;
                        if (buzzerTipFlag)
                        {
                            // 蜂鸣+闪黄灯
                            PlcControl.tagControl.wholeDevice.SetYellowLightFlash(1);
                            PlcControl.tagControl.wholeDevice.SetBuzzerStatus(1);
                            Task.Run(() =>
                            {
                                Thread.Sleep(3000);
                                PlcControl.tagControl.wholeDevice.SetBuzzerStatus(0);
                            });
                        }
                    }
                });
            }
            // CloseCutWater();
            Task.Run(async () => {
                await Task.Delay(1000);
                await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
            });
        }

        /// <summary>
        /// 关闭切割水
        /// </summary>
        public static async void CloseCutWater()
        {
            bool tempSpindleCuttingWater = CommonCheck.GetParamsStatus(DeviceKey.spindleCuttingWaterKey);
            if (tempSpindleCuttingWater)
            {
                // 关水
                await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
            }
        }

        /// <summary>
        /// 设置高度补偿数据
        /// </summary>
        /// <param name="bladeHeightCompValue"></param>
        public static void SetBladeHeightComp(float bladeHeightCompValue)
        {
            if (bladeHeightCompValue != 0)
            {
                // bladeHeightComp = bladeHeightCompValue;
            }
        }
        /// <summary>
        /// 设置进刀速度补偿数据
        /// </summary>
        /// <param name="bladeHeightCompValue"></param>
        public static void SetFeedSpeedComp(float feedSpeedCompValue)
        {
            feedSpeedComp = feedSpeedCompValue;
        }

        // 监听切割状态
        public static bool MonitorCutStatusFalse(string flag = "False", int timeout = 0)
        {
            bool value;
            int elapsed = 0; // 用于记录已经等待的时间
            do
            {
                string runValue = PlcControl.plc.GetPlcValueString(DeviceKey.cutStatusKey);
                value = flag.Equals(runValue) && !globalErrorFlag;
                Thread.Sleep(50);
                elapsed += 50; // 每次循环增加 50 毫秒

                // 如果设置了超时时间且已超时
                if (timeout > 0 && elapsed >= timeout)
                {
                    return false;
                }
            } while (value);

            if (globalErrorFlag)
            {
                return false;
            }

            return !value;
        }

        /// <summary>
        /// 校验切割深度是否有效
        /// </summary>
        /// <param name="widthInfo">刀痕宽度信息</param>
        /// <param name="angle">刀片角度</param>
        /// <param name="cueDepth">切割深度</param>
        /// <returns></returns>
        public static bool IsCuttingDepthValid(double[] widthInfo, double angle, double cutDepth)
        {
            /*double cutWidth = Camera.ConvertToPictureBoxSize(widthInfo[0]);
            // 实现切割深度校验逻辑
            double depth = TriangleAngles.CalculateDepth(angle, cutWidth);
            // 如果切割深度小于要求深度，则补偿
            if (depth < cutDepth)
            {
                GlobalParams.cutDepthOffset += float.Parse((cutDepth - depth) + "");
                return false;
            }*/
            return true;
        }

        // 判断是否已准备好切割
        public static bool IsReadyToCut()
        {
            // 判断切割是否准备好
            string runValue = PlcControl.plc.GetPlcValueString(DeviceKey.cutStatusKey);
            return "True".Equals(runValue);
        }

        // 监听切割状态
        public static bool MonitorCutStatus()
        {
            bool value;
            do
            {
                string runValue = PlcControl.plc.GetPlcValueString(DeviceKey.cutStatusKey);
                value = "True".Equals(runValue) && !globalErrorFlag;
                Thread.Sleep(50);
            } while (value);
            if (globalErrorFlag)
            {
                return false;
            }
            return !value;
        }
    }


    public class PrecutItem{
        private int cutNum;
        private float cutSpeed;

        public PrecutItem(int _cutNum, float _cutSpeed)
        {
            cutNum = _cutNum;
            cutSpeed = _cutSpeed;
        }
    }
}
