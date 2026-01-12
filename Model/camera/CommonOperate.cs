using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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

        // 聚焦运行状态
        private bool focusRunStatus = false;

        private static CommonOperate _obj;

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
                MaterialSnack("相机获取失败！", SnackType.WARNING);
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
                MaterialSnack(modeName + "未准备好！", SnackType.WARNING);
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
                List<BmSharpenParameterModel> list = SqlHelper.Table<BmSharpenParameterModel>().Where(t => t.BladeLotID == BladeLotID).ToList();
                if (list.Count == 0)
                {
                    MaterialSnack("磨刀参数错误", SnackType.WARNING);
                    return;
                }
                bmSharpenParameterModel = list.FirstOrDefault();
                startPosition = GlobalParams.workDiscFocusPosition - float.Parse(bmSharpenParameterModel.CutThickness) - bmSharpenParameterModel.CoJiaoHeight - 0.1;
            }

            focusRunStatus = true;
            MaterialSnack("自动聚焦中....", SnackType.SUCCESS, 0);
            GlobalParams.globalRunFlag = true;
            // 往下缓慢移动 35.0
            PlcControl.tagControl.Z2axis.StartAbsolute(GlobalParams.z2DefaultSpeed, startPosition.ToString());
            // 监听Z2轴运动是否停止
            string z2CurLocation = PlcControl.plc.GetPlcValueString(DeviceKey.z2CurLocationKey);
            if (z2CurLocation != null)
            {
                Task.Run(() =>
                {
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
                            MaterialSnack("自动对焦失败!", SnackType.ERROR);
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
                            if (cameraCommon.LocalBitmap != null)
                            {
                                double tenengradBlurriness = VisualUtils.CalculateTenengrad2(cameraCommon.LocalBitmap);
                                Tools.LogInfo("当前位置：" + newPosition + " 当前模糊度：" + tenengradBlurriness);
                                if (lastBlurriness > 0 && lastBlurriness - tenengradBlurriness > 0.5)
                                {
                                    // 找到最清晰的位置，停止循环并移动到上一个位置
                                    Tools.LogInfo("最清晰的图片已找到，停止当前对焦并返回到上一个位置");

                                    // 调用plc方法，走到上一个位置
                                    PlcControl.tagControl.Z2axis.StartAbsolute(GlobalParams.z2DefaultSpeed, lastPosition.ToString());
                                    GlobalParams.lastFocusZ2Location = (float)lastPosition;
                                    // 可以弹出提示框或日志，告诉用户聚焦已完成
                                    MaterialSnack("自动对焦已完成!", SnackType.SUCCESS);
                                    GlobalParams.globalRunFlag = false;
                                    // 设置聚焦运行状态为结束
                                    focusRunStatus = false;
                                    return;
                                }
                                if (tenengradBlurriness < 10)
                                {
                                    dynamicIncrement = 0.05;
                                }
                                else if (tenengradBlurriness < 20)
                                {
                                    dynamicIncrement = 0.03;
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
                    MaterialSnack("自动对焦结束!", SnackType.SUCCESS);
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
    }

    internal class ImageDataComparer : IEqualityComparer<ImageData>
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