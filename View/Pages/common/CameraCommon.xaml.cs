using OpenCvSharp.WpfExtensions;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;
using 精密切割系统.Model.cut;
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

        public event Action LineChanged;

        private Point centerLocation;
        private static List<CustomLine> _lines = new List<CustomLine>();
        private TextBlock cutWidthTextBlock;
        private TextBlock edgeWidthTextBlock;
        private bool _hasEdgeLine;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private static float _cutMarkWidth;

        /// <summary>
        /// 刀痕宽度
        /// </summary>
        public float CutMarkWidth
        {
            get { return _cutMarkWidth; }
            set { _cutMarkWidth = value; }
        }

        private static float _edgeChipWidth;

        /// <summary>
        /// 崩边
        /// </summary>
        public float EdgeChipWidth
        {
            get { return _edgeChipWidth; }
            set { _edgeChipWidth = value; }
        }

        private WriteableBitmap? _localBitmap;

        /// <summary>
        /// 当前图像
        /// </summary>
        public WriteableBitmap? LocalBitmap => _localBitmap;

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var (cutMarkWidth, edgeWidth, lightSourceBrightness) = await CurrentUtils.GetWidthAndLightAsync(GlobalParams.CH1);
            _cutMarkWidth = cutMarkWidth;
            _edgeChipWidth = edgeWidth;
            var userDefine = await SqlHelper.GetOrCreateEntityAsync(() => new UserDefineDataModel());
            if (userDefine is not null)
            {
                _hasEdgeLine = userDefine.HasEdgeLine;
            }

            centerLocation = new Point(cameraImage.Width / 2, cameraImage.Height / 2);
            SetupOverlayPanel();
            CameraUtils.PayloadReceived += CameraUtils_PayloadReceived;
            CompositionTarget.Rendering += OnCompositionTargetRendering;
            if (GlobalParams.OnlineFlag)
            {
                CameraUtils.StartGrabbing();
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            CameraUtils.PayloadReceived -= CameraUtils_PayloadReceived;
            CompositionTarget.Rendering -= OnCompositionTargetRendering;
            //CameraUtils.StopGrabbing();
        }

        private void OnCompositionTargetRendering(object? sender, EventArgs e)
        {
            if (cameraImage is not null && _localBitmap is not null)
            {
                cameraImage.Source = _localBitmap;
            }
        }

        private void CameraUtils_PayloadReceived(Model.camera.ImageData imageData)
        {
            // 确保在UI线程更新位图引用
            Application.Current.Dispatcher.Invoke(() =>
            {
                var bitmap = imageData.ToWriteableBitmap();
                _localBitmap = bitmap;
            }, DispatcherPriority.Background);
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
            SetEdgeWidthTextBlockY();
            CurrentUtils.UpdateEdgeWidth(SemiAutoCutService.Instance.CurrentChannelNum, _edgeChipWidth);
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
            _ = CurrentUtils.UpdateCutMarkWidthAsync(SemiAutoCutService.Instance.CurrentChannelNum, _cutMarkWidth);
        }

        public void UpdateLine(float baselineWidth, float edgeChipWidth)
        {
            _cutMarkWidth = baselineWidth;
            _edgeChipWidth = edgeChipWidth;
            // 根据宽度设置线条
            double cutWidth = CameraOperateUtils.ConvertToPictureBoxSize(_cutMarkWidth);
            double edgesWidth = CameraOperateUtils.ConvertToPictureBoxSize(_edgeChipWidth);
            DrawLineForWidth((float)cutWidth, (float)edgesWidth);
            AddTextToCanvas();
            SetCutWidthTextBlockY();
            SetEdgeWidthTextBlockY();
            LineChanged?.Invoke();
        }

        private void SetupOverlayPanel()
        {
            // 根据宽度设置线条
            double cutWidth = CameraOperateUtils.ConvertToPictureBoxSize(_cutMarkWidth);
            double edgesWidth = CameraOperateUtils.ConvertToPictureBoxSize(_edgeChipWidth);
            DrawLineForWidth((float)cutWidth, (float)edgesWidth);
            AddTextToCanvas();
            SetCutWidthTextBlockY();
            SetEdgeWidthTextBlockY();
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

            if (_hasEdgeLine)
            {
                edgeWidthTextBlock = new TextBlock
                {
                    Text = "",
                    FontSize = 18,
                    Foreground = new SolidColorBrush(Colors.Yellow),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B444B")),
                    FontWeight = FontWeights.Bold
                };
                Canvas.SetLeft(edgeWidthTextBlock, cameraImage.Width * 0.6);
                Canvas.SetTop(edgeWidthTextBlock, _lines[2].StartPoint.Y - 25);
                // 将TextBlock添加到Canvas
                MyCanvas.Children.Add(edgeWidthTextBlock);
            }
        }

        private void SetEdgeWidthTextBlockY()
        {
            if (_hasEdgeLine)
            {
                // 获取上刀痕的位置
                CustomLine line = _lines[2];
                // 更新TextBlock的Y轴位置
                Canvas.SetTop(edgeWidthTextBlock, line.StartPoint.Y - 25);
                edgeWidthTextBlock.Text = (_edgeChipWidth / 1000).ToString("F4") + "mm";
            }
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
                List<CustomLine> lines;
                if (_hasEdgeLine)
                {
                    lines = new List<CustomLine>
                    {
                        new CustomLine(new Point(startX, startTopCutMarkY), new Point(enxX, startTopCutMarkY), Color.FromRgb(159, 254, 0), 1, dotCollection),
                        new CustomLine(new Point(startX, startBottomCutMarkY), new Point(enxX, startBottomCutMarkY), Color.FromRgb(159, 254, 0), 1, dotCollection),
                        new CustomLine(new Point(startX, startToEdgeChipY), new Point(enxX, startToEdgeChipY), Colors.Yellow, 1, dotCollection),
                        new CustomLine(new Point(startX, startBottomEdgeChipY), new Point(enxX, startBottomEdgeChipY), Colors.Yellow, 1, dotCollection),
                        new CustomLine(new Point(0, 320), new Point(765, 320), Color.FromRgb(159, 254, 0), 1, DashStyles.Solid.Dashes),
                        // 短竖
                        new CustomLine(new Point(382.5, 307.5), new Point(382.5, 332.5), Color.FromRgb(159, 254, 0), 1, DashStyles.Solid.Dashes),
                        // 短横
                        // new CustomLine(new Point(370, 320), new Point(395, 320), Color.FromRgb(159, 254, 0), 1, DashStyles.Solid.Dashes)
                    };
                }
                else
                {
                    lines = new List<CustomLine>
                    {
                        new CustomLine(new Point(startX, startTopCutMarkY), new Point(enxX, startTopCutMarkY), Color.FromRgb(159, 254, 0), 1, dotCollection),
                        new CustomLine(new Point(startX, startBottomCutMarkY), new Point(enxX, startBottomCutMarkY), Color.FromRgb(159, 254, 0), 1, dotCollection),
                        new CustomLine(new Point(0, 320), new Point(765, 320), Color.FromRgb(159, 254, 0), 1, DashStyles.Solid.Dashes),
                        // 短竖
                        new CustomLine(new Point(382.5, 307.5), new Point(382.5, 332.5), Color.FromRgb(159, 254, 0), 1, DashStyles.Solid.Dashes),
                        // 短横
                        // new CustomLine(new Point(370, 320), new Point(395, 320), Color.FromRgb(159, 254, 0), 1, DashStyles.Solid.Dashes)
                    };
                }
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

        private async void cameraImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 获取当前点击的坐标
            var touchPoint = e.GetPosition(this);
            double x = touchPoint.X - centerLocation.X;
            double y = touchPoint.Y - centerLocation.Y;
            await CameraPictureRunAsync(x, y);
        }

        private async void cameraImage_TouchDown(object sender, TouchEventArgs e)
        {
            // 获取当前触摸的坐标
            var touchPoint = e.GetTouchPoint(this).Position;
            double x = touchPoint.X - centerLocation.X;
            double y = touchPoint.Y - centerLocation.Y;
            await CameraPictureRunAsync(x, y);
        }

        private async Task CameraPictureRunAsync(double x, double y)
        {
            if (Appsettings.PositiveLimitPositionX is null || Appsettings.NegativeLimitPositionX is null || Appsettings.PositiveLimitPositionY is null || Appsettings.NegativeLimitPositionY is null)
            {
                MaterialSnack("未设置轴极限位置！", SnackType.WARNING, 2);
                return;
            }
            if (!await _semaphore.WaitAsync(0))
            {
                return;
            }
            try
            {
                // 使用元组解构，避免数组索引
                var xPos = await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync();
                var yPos = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync();
                if (xPos is null || yPos is null) return;

                // 使用本地函数处理坐标计算
                float? newX = CalculateNewPosition(xPos.Value, x, CameraOperateUtils.ConvertPictureBoxToRealSizeWidth);
                float? newY = CalculateNewPosition(yPos.Value, y, CameraOperateUtils.ConvertPictureBoxToRealSize);

                // 或者直接修改原变量
                if (newX.HasValue) xPos = newX;
                if (newY.HasValue) yPos = newY;

                //// 判断X 和Y是否超限，超限则保留最大或者最小值
                float xUpperValue = Appsettings.PositiveLimitPositionX.Value;
                float xLowerValue = Appsettings.NegativeLimitPositionX.Value;
                float yUpperValue = Appsettings.PositiveLimitPositionY.Value;
                float yLowerValue = Appsettings.NegativeLimitPositionY.Value;
                // 使用 Math.Clamp 限制范围
                xPos = Math.Clamp(xPos.Value, xLowerValue, xUpperValue);
                yPos = Math.Clamp(yPos.Value, yLowerValue, yUpperValue);
                if (await PlcControl.tagControl.Xaxis.IsReadyAsync() && await PlcControl.tagControl.Yaxis.IsReadyAsync())
                {
                    await PlcControl.tagControl.cutting.RunMotionNoWaitAsync(xPos.Value, yPos.Value);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private static float? CalculateNewPosition(float currentPosition, double offset, Func<double, double> convertFunc)
        {
            if (offset == 0) return null;
            float distance = (float)convertFunc(Math.Abs(offset)) / 1000f;
            return currentPosition + (Math.Sign(offset) * distance);
        }

        public OpenCvSharp.Mat CaptureControl()
        {
            OpenCvSharp.Mat result = new OpenCvSharp.Mat();

            // 使用最高优先级强制同步
            CameraCommonGrid.Dispatcher.Invoke(() =>
            {
                // 强制布局更新
                CameraCommonGrid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                CameraCommonGrid.Arrange(new Rect(CameraCommonGrid.DesiredSize));

                // 等待渲染完成的事件
                var frameEvent = new ManualResetEventSlim(false);

                CompositionTarget.Rendering += OnRendering;
                void OnRendering(object sender, EventArgs e)
                {
                    CompositionTarget.Rendering -= OnRendering;
                    frameEvent.Set();
                }

                // 等待下一帧渲染
                frameEvent.Wait(TimeSpan.FromMilliseconds(100));

                // 直接渲染控件
                var rtb = new RenderTargetBitmap(
                    (int)CameraCommonGrid.ActualWidth,
                    (int)CameraCommonGrid.ActualHeight,
                    96, 96, PixelFormats.Pbgra32);

                rtb.Render(CameraCommonGrid);
                result = new FormatConvertedBitmap(rtb, PixelFormats.Bgr32, null, 0).ToMat();
            }, DispatcherPriority.Send);

            return result;
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