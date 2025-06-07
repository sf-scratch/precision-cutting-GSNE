using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emgu.CV;
using NPOI.POIFS.Crypt.Dsig;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Model.sqlite;
using 精密切割系统.Utils;
using 精密切割系统.View.Pages.common;
using 精密切割系统.ViewModel;

namespace 精密切割系统.FrmWindow.common
{
    internal class CommonOperate
    {
        
        public static int xLocation = 0; // 横向拉直x轴的位置 0 左边 1 右边 2 已完成
        public static int xVerticalLocation = 0; // 竖向拉直x轴的位置 0 左边 1 右边 2 已完成
        private float x = 0;
        private float locationAX = 0; // 校准A点X坐标
        private float locationAY = 0; // 校准A点Y坐标
        private float locationBX = 0; // 校准B点
        private float locationBX1 = 0; // 竖向校准B点
        private float locationBY = 0; // 校准B点Y轴距离
        private float locationBY1 = 0; // 竖向校准B点Y轴距离
        private float locationC = 0; // 校准C点
        private string xTagKey = "X轴当前位置";
        private string yTagKey = "Y轴当前位置";
        private string yAbsTagKey = "Y轴绝对运动开始";
        private string xPosKey = "X轴正转开始";
        private string thetaPosKey = "Theta轴正转开始";
        private string thetaAntiKey = "Theta轴反转开始";
        private string xSpeed = "80";
        private string ySpeed = "30";
        // Theta轴拉直运行状态
        bool thetaAlignRunStatus = false;
        // 聚焦运行状态
        bool focusRunStatus = false;
        

        static CommonOperate _obj;

        public static CommonOperate GetInstance()
        {
            if (_obj == null)
            {
                _obj = new CommonOperate();
            }
            return _obj;
        }
        
        /// <summary>
        /// 自动聚焦
        /// </summary>
        /// <param name="type">1、校准模式 2、半自动切割 3、自动切割 4、磨刀</param>
        public void AutoFocus(int type, MainWindow mainWindow, string BladeLotID = "")
        {
            if (!CommonCheck.AxisReady(false))
            {
                return;
            }
            if (focusRunStatus)
            {
                return;
            }
            // 获取相机页面
            List<CameraCommon> cameraCommons = Tools.GetChildrenOfType<CameraCommon>(mainWindow.mainFrame);
            if (cameraCommons.Count == 0)
            {
                MaterialSnackUtils.MaterialSnack("相机获取失败！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            CameraCommon cameraCommon = cameraCommons[0];
            // 根据进入的哪个模式，来判断模式是否已经准备好
            bool readyFlag = false;
            string modeName = "";
            switch (type)
            {
                case 1:
                    modeName = "校准模式";
                    readyFlag = CommonCheck.GetParamsStatus(DeviceKey.alignStatusKey);
                    break;
                case 2:
                    modeName = "半自动模式";
                    readyFlag = CommonCheck.GetParamsStatus(DeviceKey.cutStatusKey);
                    break;
                case 3:
                    modeName = "自动模式";
                    readyFlag = CommonCheck.GetParamsStatus(DeviceKey.cutStatusKey);
                    break;
                default:
                    break;
            }
            if (!readyFlag)
            {
                MaterialSnackUtils.MaterialSnack(modeName + "未准备好！", MaterialSnackUtils.SnackType.WARNING);
                // return;
            }
            // 获取当前配置的工作盘和膜的厚度
            FileTableItemModel fileTableItemModel = CurrentUtils.GetFileTableItemModel();
            // 获取工件的厚度和膜的厚度
            double workThickness = double.Parse(fileTableItemModel.WorkThickness);
            double tapeThickness = double.Parse(fileTableItemModel.TapeThickness);
            double startPosition = GlobalParams.workDiscFocusPosition - workThickness - tapeThickness - 0.1;
            if (type == 4)
            {
                BmSharpenParameterModel bmSharpenParameterModel = new BmSharpenParameterModel();
                BladeLotID = string.IsNullOrEmpty(BladeLotID) ? "0" : BladeLotID;
                // 如果是磨刀，则选择磨刀参数
                List < BmSharpenParameterModel > list = SqlHelper.Table<BmSharpenParameterModel>().Where(t => t.BladeLotID == BladeLotID).ToList();
                if (list.Count == 0)
                {
                    MaterialSnackUtils.MaterialSnack("磨刀参数错误", MaterialSnackUtils.SnackType.WARNING);
                    return;
                }
                bmSharpenParameterModel = list.FirstOrDefault();
                startPosition = GlobalParams.workDiscFocusPosition - float.Parse(bmSharpenParameterModel.CutThickness) - bmSharpenParameterModel.CoJiaoHeight - 0.1;
            }


            focusRunStatus = true;
            MaterialSnackUtils.MaterialSnack("自动聚焦中....", MaterialSnackUtils.SnackType.SUCCESS, 0);
            GlobalParams.globalRunFlag = true;
            // 往下缓慢移动 35.0
            PlcControl.tagControl.Z2axis.StartAbsolute(GlobalParams.z2DefaultSpeed, startPosition.ToString());
            // 监听Z2轴运动是否停止
            string z2CurLocation = PlcControl.plc.GetPlcValueString(DeviceKey.z2CurLocationKey);
            if (z2CurLocation != null)
            {
                Task.Run(() => {
                    Tools.WaitForValue(PlcControl.allTags[DeviceKey.z2CurLocationKey], startPosition.ToString());
                    Tools.WaitForValue(PlcControl.allTags[DeviceKey.z2CurSpeedKey], "0");
                    Thread.Sleep(500);
                    double start = 0.01;
                    double end = 0.5;
                    double increment = 0.01;
                    string focusImgName = "focusImg/";
                    HashSet<ImageData> dataSet = new HashSet<ImageData>();
                    double lastBlurriness = 0;
                    double lastPosition = 0;
                    // 增加动态调整步进增量的逻辑
                    double dynamicIncrement = increment;  // 初始步进增量
                    for (double i = start; i < end; i += dynamicIncrement)
                    {
                        // 执行你的操作
                        double newPosition = startPosition + i;
                        PlcControl.tagControl.Z2axis.StartAbsolute(GlobalParams.z2DefaultSpeed, newPosition.ToString());
                        Thread.Sleep(10);
                        // bool res = Tools.WaitForValue(PlcControl.allTags[DeviceKey.z2CurLocationKey], newPosition.ToString());
                        bool res = Tools.WaitForValue(PlcControl.allTags[DeviceKey.z2CurSpeedKey], "0");
                        // bool res = Tools.WaitForValue(DeviceKey.z2CurMotionStatusKey, 1);
                        // 走到位置后，获取当前图片 调用模糊度判断
                        if (!res)
                        {
                            Tools.LogInfo("自动对焦失败");
                            MaterialSnackUtils.MaterialSnack("自动对焦失败!", MaterialSnackUtils.SnackType.ERROR);
                            focusRunStatus = false;
                            GlobalParams.globalRunFlag = false;
                            return;
                        }

                        ImageData imgData = new ImageData();
                        DateTime currentTime = DateTime.UtcNow;
                        long timestamp = (long)(currentTime.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
                        imgData.ImagePath = focusImgName + newPosition + "_" + timestamp.ToString() + ".png";
                        Thread.Sleep(50);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (cameraCommon.localBitmap != null)
                            {
                                double tenengradBlurriness = VisualUtils.CalculateTenengrad2(cameraCommon.localBitmap);
                                Tools.LogInfo("当前位置：" + newPosition + " 当前模糊度：" + tenengradBlurriness);
                                if (lastBlurriness > 0 && lastBlurriness - tenengradBlurriness > 0.5)
                                {
                                    // 找到最清晰的位置，停止循环并移动到上一个位置
                                    Tools.LogInfo("最清晰的图片已找到，停止当前对焦并返回到上一个位置");

                                    // 调用plc方法，走到上一个位置
                                    PlcControl.tagControl.Z2axis.StartAbsolute(GlobalParams.z2DefaultSpeed, lastPosition.ToString());
                                    GlobalParams.lastFocusZ2Location = (float)lastPosition;
                                    // 可以弹出提示框或日志，告诉用户聚焦已完成
                                    MaterialSnackUtils.MaterialSnack("自动对焦已完成!", MaterialSnackUtils.SnackType.SUCCESS);
                                    GlobalParams.globalRunFlag = false;
                                    // 设置聚焦运行状态为结束
                                    focusRunStatus = false;
                                    return;
                                }
                                if (tenengradBlurriness < 10)
                                {
                                    dynamicIncrement = 0.05;
                                } else if (tenengradBlurriness < 20)
                                {
                                    dynamicIncrement = 0.03;
                                }
                                else
                                {
                                    dynamicIncrement = increment;
                                }
                                lastBlurriness = tenengradBlurriness;
                                lastPosition = newPosition;
                            } else
                            {
                                focusRunStatus = false;
                                GlobalParams.globalRunFlag = false;
                                Tools.LogInfo("聚焦获取当前帧失败！");
                                return;
                            }
                        });
                        if (!focusRunStatus)
                        {
                            break;
                        }
                    }

                    GlobalParams.globalRunFlag = false;
                    // 设置聚焦运行状态为结束
                    focusRunStatus = false;
                    MaterialSnackUtils.MaterialSnack("自动对焦结束!", MaterialSnackUtils.SnackType.SUCCESS);
                });
            }
        }
        public WriteableBitmap CloneWriteableBitmap(WriteableBitmap source)
        {
            // 确保源的格式是 Gray8
            if (source.Format != PixelFormats.Gray8)
            {
                throw new ArgumentException("Source WriteableBitmap must be in Gray8 format.");
            }

            // 创建一个新的 WriteableBitmap
            var clone = new WriteableBitmap(source.PixelWidth, source.PixelHeight,
                                             source.DpiX, source.DpiY,
                                             source.Format, null);

            // 创建一个数组来存储源的像素数据
            byte[] pixels = new byte[source.PixelWidth * source.PixelHeight];

            // 复制像素数据到数组
            source.CopyPixels(pixels, source.PixelWidth, 0);

            // 将数组中的像素数据复制到克隆的 WriteableBitmap
            clone.WritePixels(new Int32Rect(0, 0, source.PixelWidth, source.PixelHeight), pixels, source.PixelWidth, 0);

            return clone;
        }

        /// <summary>
        /// 获取当前刀痕得宽度和崩边宽度
        /// </summary>
        /// <returns></returns>
        /// 
        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public static double[] GetCutEdgeWidth(string fileName)
        {
            
            double[] cutEdgeWidths = [0, 0];
            // CameraUtils.SaveImagePng(fileName);
            while (!File.Exists(fileName))
            {
                continue;
            }
            Thread.Sleep(200);
            try
            {
                BladeImage imageUtils = new BladeImage(fileName);
                ViewModel.ImageResult result = imageUtils.FindLinePoint(fileName);
                Debug.WriteLine("returnCode：" + result.returnCode);
                double[] temps = [0, 0];
                if (result.returnCode != 0)
                {
                    return temps;
                }

                temps = [result.innerBlade, result.outerBlade];
                if (temps[0] != 0 && temps[1] != 0)
                {
                    double nj = CameraOperateUtils.ConvertToRealSize(temps[0]);
                    double wj = CameraOperateUtils.ConvertToRealSize(temps[1]);
                    cutEdgeWidths = [nj, wj];
                }
                
            }
            catch (AccessViolationException ex)
            {
                Tools.LogError($"获取刀痕异常：{ex.Message}");
                return cutEdgeWidths;
            }
            return cutEdgeWidths;
        }

        /// <summary>
        /// theta 横向拉直
        /// </summary>
        public void ThetaAlign1()
        {
            if (!CommonCheck.AxisReady(false))
            {
                return;
            }
            // 如果在运行中，则不执行
            if (thetaAlignRunStatus)
            {
                return;
            }
            if (xVerticalLocation == 1)
            {
                MaterialSnackUtils.MaterialSnack("请先完成竖向拉直。", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            int xRunDistance = 60;
            GlobalParams.globalRunFlag = true;
            thetaAlignRunStatus = true;
            // 设置当前位置：0 左边 1 右边 
            // 当xLocation 为0的时候点击，设置locationA
            if (xLocation == 0 || xLocation == 2)
            {
                locationAX = parseFloat(PlcControl.plc.GetPlcValueString(xTagKey));
                float tempLocationAX = locationAX + 1;
                // 解决回程误差的问题，先往正方向移动1mm
                // PlcControl.tagControl.Xaxis.StartAbsolute(xSpeed, tempLocationAX + "", false);
                // 设置locationA
                locationAX = tempLocationAX;
                locationAY = parseFloat(PlcControl.plc.GetPlcValueString(yTagKey));
                locationBY = locationAY;
                // X轴向右移动30mm
                string xCurrentPosition = PlcControl.plc.GetPlcValueString(DeviceKey.curLocationKey);
                float newPosition = float.Parse(xCurrentPosition) + xRunDistance;
                locationBX = (locationBX == 0 ? newPosition : locationBX);
                PlcControl.tagControl.Xaxis.StartAbsolute(xSpeed, locationBX + "", false);
                // 监听是否移动到位
                Thread _thread = new Thread(() =>
                {
                    Thread.Sleep(500);
                    bool flag = Tools.WaitForValue(PlcControl.allTags[DeviceKey.curSpeedKey], "0");
                    if (flag)
                    {
                        // 到位后设置locationB的位置
                        xLocation = 1;
                    } else
                    {
                        MaterialSnackUtils.MaterialSnack("Theta轴校准失败！请重新开始。", MaterialSnackUtils.SnackType.WARNING);
                        xLocation = 0;
                    }
                    thetaAlignRunStatus = false;
                    GlobalParams.globalRunFlag = false;
                    MaterialSnackUtils.MaterialSnack("请再次点击Theta轴校准，完成校准！", MaterialSnackUtils.SnackType.WARNING);
                });
                _thread.IsBackground = true;
                _thread.Start();
            }
            else if (xLocation == 1)
            {
                // 如果是第二次点击，则设置locationC的定位 = 当前y轴位置 - locationYB的值
                // locationBX = parseFloat(PlcControl.plc.GetPlcValueString(xTagKey));
                Thread.Sleep(5);
                float _tempLocationBY = parseFloat(PlcControl.plc.GetPlcValueString(yTagKey));
                Thread.Sleep(5);
                locationC = _tempLocationBY - locationBY;

                // 构建计算角度需要的参数
                // 三角形的顶点坐标
                double x1 = 0, y1 = 0;
                double dis = locationBX - locationAX;
                double x2 = dis, y2 = 0;
                double x3 = dis, y3 = Math.Abs(locationC);
                Tools.LogInfo($"x1:{x1}, y1:{y1}, x2:{x2}, y2:{y2}, x3:{x3}, y3:{y3}");
                // 计算并输出角度
                double angle = TriangleAngles.GetTriangleAngles(x1, y1, x2, y2, x3, y3);
                Thread.Sleep(5);
                Tools.LogInfo("angle:" + angle);
                // 旋转Theta轴角度
                // 如果Y轴是正数，则theta正转
                int jogDirection = 0;
                string thetaKey = thetaPosKey;// 默认正转
                if (locationC > 0)
                {
                    jogDirection = 1;
                    thetaKey = thetaAntiKey;
                }
                // 旋转theta轴
                PlcControl.tagControl.ThetaAxis.StartRelative(GlobalParams.thetaDefaultSpeed, angle.ToString(), jogDirection);
                // 复位Y轴
                // 计算Y轴移动后的位置
                string xCurrLocation = PlcControl.plc.GetPlcValueString(DeviceKey.curLocationKey);
                // 计算原点X轴位置 = 

                // 定义直线端点
                TriangleAngles.Point A = new TriangleAngles.Point(locationAX, locationAY);
                TriangleAngles.Point B = new TriangleAngles.Point(locationBX, _tempLocationBY);

                // 圆心位置 (不是原点)
                TriangleAngles.Point center = new TriangleAngles.Point(GlobalParams.thetaCameraLocationX
                    , GlobalParams.thetaCameraLocationY);

                double computingAngle = jogDirection == 0 ? angle : -angle;
                // 调用 RotateLine 方法
                var (A_rotated, B_rotated) = TriangleAngles.RotateLine(A, B, computingAngle, center);

                Tools.LogInfo($"当前Y轴位置：{PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey)}" +
                    $"，拉直后的Y位置：{A_rotated.Y} A:{A} B:{B} computingAngle:{computingAngle} center:{center}");
                Debug.WriteLine($"拉直后的Y位置：{A_rotated.Y} A:{A} B:{B} computingAngle:{computingAngle} center:{center}");

                if (A_rotated.Y > 0)
                {
                    Tools.LogInfo("进入Theta轴拉直后Y轴回位");
                    PlcControl.tagControl.Yaxis.StartAbsolute(GlobalParams.yDefaultSpeed, A_rotated.Y + "", false);
                }
                // 监听是否移动到位
                Thread _thread = new Thread(() =>
                {
                    // X轴向右移动回原位
                    PlcControl.tagControl.Xaxis.StartAbsolute(xSpeed, locationAX + "", false);
                    Thread.Sleep(300);
                    // 等待X轴移动完成后，可以操作
                    bool flag = Tools.WaitForValue(PlcControl.allTags[DeviceKey.curSpeedKey], "0");
                    if (flag)
                    {
                        SetCalibrationAngle();
                        CutOperateUtils.thetaAlignFlag = true;
                        MaterialSnackUtils.MaterialSnack("Theta轴完成校准!", MaterialSnackUtils.SnackType.SUCCESS);
                    }
                    else
                    {
                        MaterialSnackUtils.MaterialSnack("Theta轴校准失败,请重新开始！", MaterialSnackUtils.SnackType.ERROR);
                    }
                    thetaAlignRunStatus = false;
                    GlobalParams.globalRunFlag = false;
                    xLocation = 2;
                });
                _thread.IsBackground = true;
                _thread.Start();
            }

        }
        /// <summary>
        /// theta 竖向拉直
        /// </summary>
        public void ThetaAlign()
        {
            if (!CommonCheck.AxisReady(false))
            {
                return;
            }
            // 如果在运行中，则不执行
            if (thetaAlignRunStatus)
            {
                return;
            }
            if (xLocation == 1) 
            {
                MaterialSnackUtils.MaterialSnack("请先完成横向拉直。", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            int yRunDistance = 60;
            GlobalParams.globalRunFlag = true;
            thetaAlignRunStatus = true;
            // 设置当前位置：0 左边 1 右边 
            // 当xLocation 为0的时候点击，设置locationA
            if (xVerticalLocation == 0 || xVerticalLocation == 2)
            {
                // 设置locationA
                locationAX = parseFloat(PlcControl.plc.GetPlcValueString(xTagKey));
                locationAY = parseFloat(PlcControl.plc.GetPlcValueString(yTagKey));
                locationBX1 = locationAX;
                // Y轴向右移动30mm
                string yCurrentPosition = PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey);
                float newPosition = float.Parse(yCurrentPosition) - yRunDistance;
                locationBY1 = (locationBY1 == 0 ? newPosition : locationBY1);
                PlcControl.tagControl.Yaxis.StartAbsolute(ySpeed, locationBY1 + "", false);
                // 监听是否移动到位
                Thread _thread = new Thread(() =>
                {
                    Thread.Sleep(500);
                    bool flag = Tools.WaitForValue(PlcControl.allTags[DeviceKey.curSpeedKey], "0");
                    if (flag)
                    {
                        // 到位后设置locationB的位置
                        xVerticalLocation = 1;
                    } else
                    {
                        MaterialSnackUtils.MaterialSnack("Theta轴校准失败！请重新开始。", MaterialSnackUtils.SnackType.WARNING);
                        xVerticalLocation = 0;
                    }
                    thetaAlignRunStatus = false;
                    GlobalParams.globalRunFlag = false;
                    MaterialSnackUtils.MaterialSnack("请再次点击Theta轴校准，完成校准！", MaterialSnackUtils.SnackType.WARNING);
                });
                _thread.IsBackground = true;
                _thread.Start();
            }
            else if (xVerticalLocation == 1)
            {
                // 如果是第二次点击，则设置locationC的定位 = 当前y轴位置 - locationYB的值
                // locationBY1 = parseFloat(PlcControl.plc.GetPlcValueString(yTagKey));
                Thread.Sleep(5);
                float _tempLocationBX = parseFloat(PlcControl.plc.GetPlcValueString(xTagKey));
                Thread.Sleep(5);
                locationC = _tempLocationBX - locationBX1;

                // 构建计算角度需要的参数
                // 三角形的顶点坐标
                double x1 = 0, y1 = 0;
                double dis = locationBY1 - locationAY;
               
                double x2 = 0, y2 = dis;
                double x3 = Math.Abs(locationC), y3 = dis;
                Tools.LogInfo($"x1:{x1}, y1:{y1}, x2:{x2}, y2:{y2}, x3:{x3}, y3:{y3}");
                // 计算并输出角度
                double angle = TriangleAngles.GetTriangleAngles(x1, y1, x2, y2, x3, y3);
                Thread.Sleep(5);
                Tools.LogInfo("angle:" + angle);
                // 旋转Theta轴角度
                // 如果Y轴是正数，则theta正转
                int jogDirection = 0;
                string thetaKey = thetaPosKey;// 默认正转
                if (locationC > 0)
                {
                    jogDirection = 1;
                    thetaKey = thetaAntiKey;
                }
                // 旋转theta轴
                PlcControl.tagControl.ThetaAxis.StartRelative(GlobalParams.thetaDefaultSpeed, angle.ToString(), jogDirection);
                // 复位Y轴
                // 计算Y轴移动后的位置
                string xCurrLocation = PlcControl.plc.GetPlcValueString(DeviceKey.curLocationKey);
                // 计算原点X轴位置 = 

                // 定义直线端点
                TriangleAngles.Point A = new TriangleAngles.Point(locationAX, locationAY);
                TriangleAngles.Point B = new TriangleAngles.Point(locationBY1, _tempLocationBX);

                // 圆心位置 (不是原点)
                TriangleAngles.Point center = new TriangleAngles.Point(GlobalParams.thetaCameraLocationX
                    , GlobalParams.thetaCameraLocationY);

                double computingAngle = jogDirection == 0 ? angle : -angle;
                // 调用 RotateLine 方法
                var (A_rotated, B_rotated) = TriangleAngles.RotateLine(A, B, computingAngle, center);

                Tools.LogInfo($"拉直后的Y位置：{A_rotated.Y} A:{A} B:{B} computingAngle:{computingAngle} center:{center}");
                Debug.WriteLine($"拉直后的Y位置：{A_rotated.Y} A:{A} B:{B} computingAngle:{computingAngle} center:{center}");
                if (A_rotated.X > 0)
                {
                    PlcControl.tagControl.Xaxis.StartAbsolute(GlobalParams.xDefaultSpeed, A_rotated.X + "", false);
                }
                // 监听是否移动到位
                Thread _thread = new Thread(() =>
                {
                    // X轴向右移动回原位
                    PlcControl.tagControl.Yaxis.StartAbsolute(ySpeed, locationAY + "", false);
                    Thread.Sleep(200);
                    // 等待X轴移动完成后，可以操作
                    bool flag = Tools.WaitForValue(PlcControl.allTags[DeviceKey.yCurSpeedKey], "0");
                    if (flag)
                    {
                        SetCalibrationAngle();
                        CutOperateUtils.thetaAlignFlag = true;
                        MaterialSnackUtils.MaterialSnack("Theta轴完成校准!", MaterialSnackUtils.SnackType.SUCCESS);
                    }
                    else
                    {
                        MaterialSnackUtils.MaterialSnack("Theta轴校准失败,请重新开始！", MaterialSnackUtils.SnackType.ERROR);
                    }
                    thetaAlignRunStatus = false;
                    GlobalParams.globalRunFlag = false;
                    xVerticalLocation = 2;
                });
                _thread.IsBackground = true;
                _thread.Start();
            }

        }

        private float parseFloat(string value)
        {
            if (float.TryParse(value, out float result))
            {
                Console.WriteLine(result); // 输出 3.14
            }
            else
            {
                Console.WriteLine("转换失败");
            }
            return (float)Math.Round(result, 5);
        }

        void SetCalibrationAngle()
        {
            // 获取当前面的切割角度，然后用当前角度减去切割角度 等于拉直误差角度
            FileTableItemChModel chModel = CurrentUtils.GetFileTableItemChModel();
            float tempCh = Tools.GetFloatStringValue(chModel.ThetaDeg);
            float thetaCurrentDeg = Tools.GetFloatStringValue(PlcControl.plc.GetPlcValueString(DeviceKey.thetaCurLocationKey));
            GlobalParams.calibrationAngle = thetaCurrentDeg - tempCh;
            Tools.LogInfo($"GlobalParams.calibrationAngle:{GlobalParams.calibrationAngle}");
            Tools.LogInfo($"thetaCurrentDeg:{thetaCurrentDeg}");
            Tools.LogInfo($"tempCh:{tempCh}");
        }
    }
    class ImageDataComparer : IEqualityComparer<ImageData>
    {
        public bool Equals(ImageData x, ImageData y)
        {
            if (x == null || y == null)
                return false;

            return x.TenengradBlurriness == y.TenengradBlurriness && x.Position == y.Position;
        }

        public int GetHashCode(ImageData obj)
        {
            return obj.TenengradBlurriness.GetHashCode() ^ obj.Position.GetHashCode();
        }
    }
    public class ImageData
    {
        public double TenengradBlurriness { get; set; }
        public string ImagePath { get; set; }
        public double Position { get; set; }
        public long DestImgSize { get; set; }

        public WriteableBitmap ImageMat { get; set; }
    }
}
