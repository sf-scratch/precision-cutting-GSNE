using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Microsoft.Win32;
using NPOI.Util;
using SciCamera.Net;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.ViewModel;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace 精密切割系统.View.Pages.common
{
    /// <summary>
    /// CameraCommon.xaml 的交互逻辑
    /// </summary>
    public partial class CameraCommon : UserControl
    {
        public CameraCommon()
        {
            InitializeComponent();
        }

        SciCam.SCI_DEVICE_INFO_LIST m_stDevList = new SciCam.SCI_DEVICE_INFO_LIST();
        SciCam m_currentDev = new SciCam();

        WriteableBitmap bitmap;
        public static Mat? curMat;
        bool m_bDeviceReady = false;         //是否存在相机
        bool m_bDeviceOpened = false;        //相机是否打开
        bool m_bStartGrabbing = false;       //开始采集状态

        Thread m_hGrabThread = null;            //取流线程句柄
        bool m_bThreadState = false;			//线程状态  

        private readonly object bitmapLock = new object();

        private Point centerLocation;
        private float scalingRatio; // 缩放比例
        public float _cutMarkWidth = 180; // 刀痕宽度
        public float _edgeChipWidth = 200; // 崩边
        private Point[] triangle1, triangle2, triangle3, triangle4;
        private static List<CustomLine> _lines = new List<CustomLine>();
        private TextBlock cutWidthTextBlock;

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            m_currentDev = CameraUtils.m_currentDev;
            if (!m_bThreadState)
            {
                StartGrabbing();
            }
            centerLocation = new Point(cameraImage.Width / 2, cameraImage.Height / 2);
            SetupOverlayPanel();
        }
        bool changeCameraRunFlag = false;
        // 切换相机
        public void ChangeCamera()
        {
            // 如果只有1个相机，则不执行切换逻辑
            if (CameraUtils.sciCams.Count == 1)
            {
                return;
            }
            if (changeCameraRunFlag)
            {
                return;
            }
            changeCameraRunFlag = true;
            // 移动X和Y轴
            PositionAlignmentModel positionAlignmentModel = CurrentUtils.positionAlignmentModel;
            string newX = positionAlignmentModel.HighMagToLowMagCameraXOffset;
            string newY = positionAlignmentModel.HighMagToLowMagCameraYOffset;
            // 判断当前是高倍还是低倍  高》低，则X+ Y+  低》高，则X- Y-
            // 如果当前等于0 则是高倍》低倍
            if (CameraUtils.currentCameraIndex == 1)
            {
                newX = "-" + newX;
                newY = "-" + newY;
            }
            // PlcControl.tagControl.calibration.RunMotion(Tools.GetFloatStringValue(newX), Tools.GetFloatStringValue(newY));

            // 停止当前采集
            StopGrabbing();
            Thread.Sleep(50);
            // 切换相机
            CameraUtils.ChangeCamera();
            Thread.Sleep(50);
            m_currentDev = CameraUtils.m_currentDev;
            Thread.Sleep(50);
            StartGrabbing();
            changeCameraRunFlag = false;
        }
        private void StartGrabbing()
        {
            if (!GlobalParams.onlineFlag)
            {
                return;
            }
            m_bThreadState = true;
            m_hGrabThread = new Thread(GrabThreadProcess);
            m_hGrabThread.IsBackground = true;
            m_hGrabThread.Start();

            uint nReVal = m_currentDev.StartGrabbing();
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                m_bThreadState = false;
                return;
            }

            m_bStartGrabbing = true;
        }

        public void StopGrabbing()
        {
            if (!GlobalParams.onlineFlag)
            {
                return;
            }
            m_bThreadState = false;
            uint nReVal = m_currentDev.StopGrabbing();
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                return;
            }

            m_bStartGrabbing = false;
        }
        private void GrabThreadProcess()
        {
            if (!GlobalParams.onlineFlag)
            {
                return;
            }
            uint nReVal = SciCam.SCI_CAMERA_OK;
            nint payload = nint.Zero;

            while (m_bThreadState)
            {
                var startTime1 = DateTime.Now;
                nReVal = m_currentDev.Grab(ref payload);
                // Debug.WriteLine($"采集帧耗时: {(DateTime.Now - startTime1).TotalMilliseconds} 毫秒");
                
                if (nReVal == SciCam.SCI_CAMERA_OK)
                {
                    // 确保 payload 有效
                    if (payload != nint.Zero)
                    {
                        // 使用Dispatcher将更新操作封送到UI线程  
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DisplayImage(payload);
                        });
                    }
                }
                else
                {
                    // 处理错误，例如记录日志
                    Debug.WriteLine($"Error grabbing image: {nReVal}");
                }
                var startTime = DateTime.Now;
                // 释放负载
                if (payload != nint.Zero)
                {
                    nReVal = m_currentDev.FreePayload(payload);
                    if (nReVal != SciCam.SCI_CAMERA_OK)
                    {
                        // 处理错误，例如记录日志
                        Debug.WriteLine($"Error freeing payload: {nReVal}");
                    }
                    payload = nint.Zero; // 重置 payload
                }

                // 计算耗时
                var elapsed = DateTime.Now - startTime;
                // 输出耗时
                // Debug.WriteLine($"释放耗时: {elapsed.TotalMilliseconds} 毫秒");

            }
        }

        private string i4tos(uint ip)
        {
            IPAddress iPAddress = new IPAddress(ip);
            return iPAddress.ToString();
        }
        public WriteableBitmap localBitmap = null;
        public void DisplayImage(nint payload)
        {
            localBitmap = null;
            var startTime = DateTime.Now;
            int result = GetConvertedInfo(payload, out localBitmap);
            // 输出耗时
            // Debug.WriteLine($"转换耗时: {(DateTime.Now - startTime).TotalMilliseconds} 毫秒");
            if (result == 0 && localBitmap != null)
            {
                lock (bitmapLock)
                {
                    // 更新bitmap变量，确保线程安全  
                    bitmap = localBitmap;
                }
                // 使用Dispatcher将UI更新操作封送到UI线程
                if (cameraImage != null) // 确保Image_Control已初始化  
                {
                    cameraImage.Source = bitmap;
                }
            }
            else
            {
                // Handle error  
            }
        }
        private int GetConvertedInfo(nint payload, out WriteableBitmap bitmap)
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

                                bitmap.WritePixels(new Int32Rect(0, 0, (int)imgWidth, (int)imgHeight), bBitmap, stride, 0);

                                // BitmapExtension.ToMat(bitmap);
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



        private bool IsValidPixelType(SciCam.SciCamPixelType pixelType)
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
        enum ImageSaveType
        {
            Type_NONE = 0,
            Type_BMP,
            Type_JPG,
            Type_TIFF,
            Type_PNG,
        };
        ImageSaveType m_imageSaveType = ImageSaveType.Type_JPG;

        public bool SaveWriteableBitmap(string filePath)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 使用PngBitmapEncoder保存为PNG格式
                try
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(localBitmap));

                    // 将图像数据写入文件
                    using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        encoder.Save(fileStream);
                    }
                    return true;
                } catch (Exception e){
                    Tools.LogError("保存图片异常：" + e.Message);
                }
                return false;
            });
            return true;
            
        }

        private void Save_Image(string fileName)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "jpg file(*.jpg)|*.jpg|bmp file(*.bmp)|*.bmp|tiff file(*.tiff)|*.tiff|png file(*.png)|*.png|所有文件(*.*)|*.*||";       //设置文件类型                                              
            string extension; //文件拓展名
            bool? result = dialog.ShowDialog();
            if (result == true) ;
            {
                fileName = dialog.FileName.ToString();
            }
            extension = System.IO.Path.GetExtension(fileName);
            if (extension == ".jpg")
            {
                m_imageSaveType = ImageSaveType.Type_JPG;

            }
            else
            {
                MessageBox.Show("请选择相应的文件保存");
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            StopGrabbing();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="width">需要修改的宽度</param>
        /// <param name="type">类型 1 自动 2 半自动</param>
        public void SetEdgeWidth(float width, int type)
        {
            float tempCutMarkWidth = (float)CameraOperateUtils.ConvertToPictureBoxSize(_cutMarkWidth);
            float tempEdgeWidth = (float)CameraOperateUtils.ConvertToPictureBoxSize(_edgeChipWidth);
            ModifyLineY(2, -width);
            ModifyLineY(3, width);
            _cutMarkWidth = (float)CameraOperateUtils.ConvertPictureBoxToRealSize(tempCutMarkWidth);
            _edgeChipWidth = (float)CameraOperateUtils.ConvertPictureBoxToRealSize(tempEdgeWidth + (width * 2));

        }
        public void SetCutMarkWidth(float width, int type)
        {
            float tempCutMarkWidth = (float)CameraOperateUtils.ConvertToPictureBoxSize(_cutMarkWidth);
            float tempEdgeWidth = (float)CameraOperateUtils.ConvertToPictureBoxSize(_edgeChipWidth);
            ModifyLineY(0, -width);
            ModifyLineY(1, width);
            _cutMarkWidth = (float)CameraOperateUtils.ConvertPictureBoxToRealSize(tempCutMarkWidth + (width * 2));
            _edgeChipWidth = (float)CameraOperateUtils.ConvertPictureBoxToRealSize(tempEdgeWidth);
            SetCutWidthTextBlockY();
        }
        private void SetupOverlayPanel()
        {   
            // 根据宽度设置线条
            double cutWidth = CameraOperateUtils.ConvertToPictureBoxSize(_cutMarkWidth);
            double edgesWidth = CameraOperateUtils.ConvertToPictureBoxSize(_edgeChipWidth);
            DrawLineForWidth((float)cutWidth, (float)edgesWidth);
            AddTextToCanvas();
            SetCutWidthTextBlockY();
        }
        /// <summary>
        /// 更改刀痕和崩边的高度
        /// 索引 0 1 是上刀痕 下刀痕
        /// 索引 2 3 是上崩边 下崩边
        /// </summary>
        /// <param name="index"></param>
        /// <param name="deltaY"></param>
        public void ModifyLineY(int index, double deltaY)
        {
            if (index >= 0 && index < _lines.Count)
            {
                CustomLine line = _lines[index];
                double newStartPointY = line.StartPoint.Y + deltaY;
                double newEndPointY = line.EndPoint.Y + deltaY;
                line.StartPoint = new Point(line.StartPoint.X, newStartPointY);
                line.EndPoint = new Point(line.EndPoint.X, newEndPointY);
                // 重新绘制这条线
                // 这里假设 MyCanvas.Children 中的 Line 对象的顺序与 _lines 列表中的顺序一致
                Line canvasLine = (Line)MyCanvas.Children[index];
                canvasLine.Y1 = newStartPointY;
                canvasLine.Y2 = newEndPointY;
            }
            else
            {
                // 处理索引越界的情况
                Console.WriteLine("索引超出范围");
            }
        }
        private void AddTextToCanvas()
        {
            // 创建一个TextBlock控件
            cutWidthTextBlock = new TextBlock
            {
                Text = "",
                FontSize = 18,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95FD68")),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B444B")),
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(cutWidthTextBlock, cameraImage.Width * 0.8);
            Canvas.SetTop(cutWidthTextBlock, _lines[0].StartPoint.Y - 25);
            // 将TextBlock添加到Canvas
            MyCanvas.Children.Add(cutWidthTextBlock);
        }

        private void SetCutWidthTextBlockY()
        {
            // 获取上刀痕的位置
            CustomLine line = _lines[0];
            // 更新TextBlock的Y轴位置
            Canvas.SetTop(cutWidthTextBlock, line.StartPoint.Y - 25);
            cutWidthTextBlock.Text = (_cutMarkWidth / 1000).ToString("F4") + "mm";
        }

        public void DrawLineForWidth(float pictureCutMarkWidth, float pictureEdgeChipWidth)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _cutMarkWidth = (float)CameraOperateUtils.ConvertPictureBoxToRealSize(pictureCutMarkWidth);
                _edgeChipWidth = (float)CameraOperateUtils.ConvertPictureBoxToRealSize(pictureEdgeChipWidth);

                // 获取图像高度 / 2 
                float imagMideHeight = (float)(cameraImage.Height / 2);
                // 开始点位的X
                float startX = 0;
                // 结束点位Y
                float enxX = (float)cameraImage.Width;
                // 计算刀痕宽度 
                // 上刀痕 = 中间点 - （刀痕宽度 / 2）
                float resultCutMark = (float)pictureCutMarkWidth / 2;
                float startTopCutMarkY = imagMideHeight - resultCutMark;
                // 下刀痕 = 图像高度 + （刀痕宽度 / 2 ）
                float startBottomCutMarkY = imagMideHeight + resultCutMark;
                float resultEdgeChip = (float)pictureEdgeChipWidth / 2;
                // 上崩边 = 中间点 - （崩边宽度 / 2 ）
                float startToEdgeChipY = imagMideHeight - resultEdgeChip;
                // 下崩边 = 图像高度 + （崩边宽度 / 2 ）
                float startBottomEdgeChipY = imagMideHeight + resultEdgeChip;
                DoubleCollection dotCollection = new DoubleCollection { 10, 4 }; // 虚线
                List<CustomLine> lines = new List<CustomLine>
                {
                    new CustomLine(new Point(startX, startTopCutMarkY), new Point(enxX, startTopCutMarkY), Color.FromRgb(159, 254, 0), 1, dotCollection),
                    new CustomLine(new Point(startX, startBottomCutMarkY), new Point(enxX, startBottomCutMarkY), Color.FromRgb(159, 254, 0), 1, dotCollection),
                    // new CustomLine(new Point(startX, startToEdgeChipY), new Point(enxX, startToEdgeChipY), Colors.Blue, 1, dotCollection),
                    // new CustomLine(new Point(startX, startBottomEdgeChipY), new Point(enxX, startBottomEdgeChipY), Colors.Blue, 1, dotCollection),

                    new CustomLine(new Point(0, 320), new Point(765, 320), Color.FromRgb(159, 254, 0), 1, DashStyles.Solid.Dashes),
                    // 短竖
                    new CustomLine(new Point(382.5, 307.5), new Point(382.5, 332.5), Color.FromRgb(159, 254, 0), 1, DashStyles.Solid.Dashes),
                    // 短横
                    // new CustomLine(new Point(370, 320), new Point(395, 320), Color.FromRgb(159, 254, 0), 1, DashStyles.Solid.Dashes)
                };

                AddLinesAndRedraw(lines);
            });
            
        }
        
        public void AddLinesAndRedraw(List<CustomLine> lines)
        {
            _lines.Clear();
            MyCanvas.Children.Clear();
            _lines.AddRange(lines);
            foreach (CustomLine line in lines)
            {
                DrawLine(line.StartPoint, line.EndPoint, line.LineColor, line.LineThickness, line.DashStyle);
            }
        }

        private void DrawLine(Point start, Point end, Color color, double thickness, DoubleCollection dashStyle)
        {
            // 将坐标四舍五入到小数点后两位
            start = new Point(Math.Round(start.X, 2), Math.Round(start.Y, 2));
            end = new Point(Math.Round(end.X, 2), Math.Round(end.Y, 2));
            
            Line line = new Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = thickness,
                // StrokeDashArray = new DoubleCollection { 10, 4 } // 4单位线段，2单位空白
                StrokeDashArray = dashStyle // 4单位线段，2单位空白
            };
            // 将线条添加到 Canvas
            MyCanvas.Children.Add(line);
        }

        private void cameraImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (CommonCheck.CheckGlobalRunStatus())
            {
                return;
            }
            if (!CommonCheck.AxisReady(false))
            {
                return;
            }
            // 获取当前点击的坐标
            var touchPoint = e.GetPosition(this);
            double x = touchPoint.X - centerLocation.X;
            double y = touchPoint.Y - centerLocation.Y;
            cameraPictureClick(x, y);
        }


        private void cameraImage_TouchDown(object sender, TouchEventArgs e)
        {
            if (CommonCheck.CheckGlobalRunStatus())
            {
                return;
            }
            if (!CommonCheck.AxisReady(false))
            {
                return;
            }
            // 获取当前触摸的坐标
            var touchPoint = e.GetTouchPoint(this).Position;
            double x = touchPoint.X - centerLocation.X;
            double y = touchPoint.Y - centerLocation.Y;
            cameraPictureClick(x, y);
        }

        private async void cameraPictureClick(double x, double y)
        {
            string xCurPosition = PlcControl.plc.GetPlcValueString(DeviceKey.curLocationKey);
            string yCurPosition = PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey);
            double xPosition = double.Parse(xCurPosition);
            double yPosition = double.Parse(yCurPosition);
            if (x != 0)
            {
                // 根据图片像素和真实比例，计算要走的位置
                double distance = CameraOperateUtils.ConvertPictureBoxToRealSizeWidth(Math.Abs(x));
                distance = distance / 1000;
                // 如果大于0 正转 小于0 反转
                if (x > 0)
                {
                    // deviceApi.StartRelativeMotion(deviceApi.xName, "30", distance + "", 0);
                    xPosition += distance;
                }
                else
                {
                    // deviceApi.StartRelativeMotion(deviceApi.xName, "30", distance + "", 1);
                    xPosition -= distance;
                }
            }
            if (y != 0)
            {
                double distance = CameraOperateUtils.ConvertPictureBoxToRealSize(Math.Abs(y));
                distance = distance / 1000;
                // 如果大于0 正转 小于0 反转
                if (y > 0)
                {
                    // deviceApi.StartRelativeMotion(deviceApi.yName, "30", distance + "", 1);
                    yPosition += distance;
                }
                else
                {
                    // deviceApi.StartRelativeMotion(deviceApi.yName, "30", distance + "", 0);
                    yPosition -= distance;
                }
            }
            // 判断X 和Y是否超限，超限则保留最大或者最小值
            Tag xLimitUpperTag = PlcControl.allTags[DeviceKey.softUpperLimitKey];
            Tag xLimitLowerTag = PlcControl.allTags[DeviceKey.softLowerLimitKey];

            Tag yLimitUpperTag = PlcControl.allTags[DeviceKey.ySoftUpperLimitKey];
            Tag yLimitLowerTag = PlcControl.allTags[DeviceKey.ySoftLowerLimitKey];

            float xUpperValue = float.Parse(xLimitUpperTag.defaultValue);
            float xLowerValue = float.Parse(xLimitLowerTag.defaultValue);
            float yUpperValue = float.Parse(yLimitUpperTag.defaultValue);
            float yLowerValue = float.Parse(yLimitLowerTag.defaultValue);
            if(xPosition >= xUpperValue)
            {
                xPosition = xUpperValue;
            }
            if (xPosition <= xLowerValue)
            {
                xPosition = xLowerValue;
            }
            if (yPosition >= yUpperValue)
            {
                yPosition = yUpperValue;
            }
            if (yPosition <= yLowerValue)
            {
                yPosition = yLowerValue;
            }
            await PlcControl.tagControl.cutting.RunMotionAsync((float)xPosition, (float)yPosition, default);
        }
    }
    
    public class CustomLine
    {
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
        public Color LineColor { get; set; }
        public double LineThickness { get; set; }
        public DoubleCollection DashStyle { get; set; }

        public CustomLine(Point start, Point end, Color color, double thickness, DoubleCollection dashStyle)
        {
            StartPoint = start;
            EndPoint = end;
            LineColor = color;
            LineThickness = thickness;
            DashStyle = dashStyle;
        }
    }
}
