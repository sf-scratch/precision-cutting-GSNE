using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Printing;
using System.Windows.Media.Imaging;
using 精密切割系统.FrmWindow.common;
using static 精密切割系统.Model.cut.EdgeAnalyzer;

namespace 精密切割系统.Model.cut
{
    /// <summary>
    /// 视觉分析器类，用于处理图像识别、测量和分析相关的功能
    /// 主要功能包括：
    /// 1. 刀痕和崩边的识别与测量
    /// 2. 图像轮廓分析和处理
    /// 3. 蛇形识别
    /// 4. 图像拼接
    /// 5. 模板匹配
    /// </summary>
    public class VisionAnalyzer
    {
        // 常量定义
        /// <summary>轮廓面积阈值，用于过滤小面积噪点</summary>
        const int THRESHOLD_AREA = 5000;
        /// <summary>边界偏移量，用于去除边缘干扰</summary>
        const int BORDER_OFFSET = 10;
        // 默认像素到毫米的转换比例
        public const double PixelToMmRatio = 0.0004075;

        /// <summary>
        /// 传入图片，识别刀痕和崩边
        /// </summary>
        /// <param name="imagePath">识别图片路径</param>
        /// <param name="pixelToMmRatio">单像素精度mm</param>
        /// <returns>返回一个元组 (刀痕宽度mm, 崩边宽度mm)</returns>
        /// <exception cref="ArgumentNullException">当图像路径为空时抛出</exception>
        /// <exception cref="ArgumentException">当像素比例小于等于0时抛出</exception>
        /// <exception cref="FileNotFoundException">当图像文件不存在时抛出</exception>
        /// <exception cref="Exception">当图像无法读取时抛出</exception>
        public static (double bladeWidthMm, double collapseWidthMm, double bladeTop, double bladeBottom, double collapseTop, double collapseBottom) ProcessImage(string imagePath, double pixelToMmRatio = PixelToMmRatio)
        //public static (double bladeWidthMm, double collapseWidthMm, double bladeTop, double bladeBottom, double collapseTop, double collapseBottom) ProcessImage(string imagePath, double pixelToMmRatio = 0.0004575)
        {
            if (string.IsNullOrEmpty(imagePath))
                throw new ArgumentNullException(nameof(imagePath), "图像路径不能为空");
            if (pixelToMmRatio <= 0)
                throw new ArgumentException("像素比例必须大于0", nameof(pixelToMmRatio));
            if (!File.Exists(imagePath))
                throw new FileNotFoundException($"找不到图像文件: {imagePath}");

            Mat image = null;
            try
            {
                image = Cv2.ImRead(imagePath);
                if (image.Empty())
                    throw new Exception($"无法读取图像文件: {imagePath}");

                int imageWidth = image.Cols;
                var result = VisualizeResults(image, GetContourData(image), imageWidth, pixelToMmRatio);
                Debug.WriteLine($"图像处理完成 - 刀痕宽度: {result.Item1}mm, 崩边宽度: {result.Item2}mm");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"图像处理失败: {ex.Message}");
                throw;
            }
            finally
            {
                image?.Dispose();
            }
        }

        //public static (double bladeWidthMm, double collapseWidthMm) ProcessImage(Mat image, double pixelToMmRatio = 0.00074)
        public static (double bladeWidthMm, double collapseWidthMm, double bladeTop, double bladeBottom, double collapseTop, double collapseBottom) ProcessImage(Mat image, double pixelToMmRatio = PixelToMmRatio)
        {
            try
            {
                if (image.Empty())
                    throw new Exception($"无法读取图像文件");
                int imageWidth = image.Cols;
                var result = VisualizeResults(image, GetContourData(image), imageWidth, pixelToMmRatio);
                Debug.WriteLine($"图像处理完成 - 刀痕宽度: {result.Item1}mm, 崩边宽度: {result.Item2}mm");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"图像处理失败: {ex.Message}");
                throw;
            }
            finally
            {
                //image?.Dispose();
            }
        }

        /// <summary>
        /// 获取图片有效轮廓
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        /// <summary>
        /// 获取图片有效轮廓数据
        /// </summary>
        /// <param name="image">输入的OpenCV图像对象</param>
        /// <returns>返回字典，包含top和bottom两组轮廓点数据</returns>
        /// <exception cref="ArgumentNullException">当输入图像为空时抛出</exception>
        /// <remarks>
        /// 处理步骤：
        /// 1. 图像预处理：灰度转换、高斯模糊、二值化、开运算去噪
        /// 2. 轮廓检测：使用外轮廓检测方法
        /// 3. 轮廓筛选：去除小面积噪点
        /// 4. 点分类：将轮廓点分为上下两组
        /// 5. 边缘优化：去除边界干扰，提取有效边缘点
        /// </remarks>
        private static Dictionary<string, List<Point>> GetContourData(Mat image)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image), "输入图像不能为空");

            int imageHeight = image.Rows;
            int imageCenterY = imageHeight / 2;
            Mat gray = null;
            Mat binary = null;
            Mat kernel = null;

            try
            {
                // 转灰度图并高斯模糊
                gray = new Mat();
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, gray, new Size(5, 5), 0);

                // Otsu阈值
                binary = new Mat();
                Cv2.Threshold(gray, binary, 80, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

                // 开运算去噪
                kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
                Cv2.MorphologyEx(binary, binary, MorphTypes.Open, kernel);

                Debug.WriteLine("图像预处理完成：灰度转换、高斯模糊、二值化、开运算");

                // 查找轮廓
                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(binary, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours == null || contours.Length == 0)
                {
                    Debug.WriteLine("未检测到有效轮廓");
                    return new Dictionary<string, List<Point>>
                    {
                        ["top"] = new List<Point>(),
                        ["bottom"] = new List<Point>()
                    };
                }
                // 过滤掉面积小于阈值的轮廓
                contours = contours.Where(contour => Cv2.ContourArea(contour) >= THRESHOLD_AREA).ToArray();
                contours = contours.OrderBy(c => Cv2.BoundingRect(c).X).ToArray();
                Debug.WriteLine($"检测到{contours.Length}个轮廓");

                // 创建一个空白图像用于绘制轮廓
                Mat mask = new Mat(binary.Size(), MatType.CV_8UC1, Scalar.All(0));

                // 绘制过滤后的轮廓
                foreach (var contour in contours)
                {
                    Cv2.DrawContours(mask, new[] { contour }, -1, new Scalar(255), -1); // 填充轮廓
                }
                Cv2.ImWrite("mask.jpg", mask);
                Cv2.FindContours(mask, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours == null || contours.Length == 0)
                {
                    Debug.WriteLine("未检测到有效轮廓");
                    return new Dictionary<string, List<Point>>
                    {
                        ["top"] = new List<Point>(),
                        ["bottom"] = new List<Point>()
                    };
                }

                // 分类点数据容器
                var contourData = new Dictionary<string, List<Point>>
                {
                    ["top"] = new List<Point>(),
                    ["bottom"] = new List<Point>()
                };

                try
                {
                    foreach (var contour in contours)
                    {
                        if (contour == null || contour.Length == 0) continue;

                        double area = Cv2.ContourArea(contour);
                        if (area < THRESHOLD_AREA)
                        {
                            Debug.WriteLine($"轮廓面积{area}小于阈值{THRESHOLD_AREA}，跳过");
                            continue;
                        }

                        Rect rect = Cv2.BoundingRect(contour);
                        int xMin = rect.X + BORDER_OFFSET;
                        int xMax = rect.X + rect.Width - BORDER_OFFSET;
                        int yMin = rect.Y;
                        int yMax = rect.Y + rect.Height;

                        bool isTop = (yMin + yMax) / 2 < imageCenterY;
                        double adjustedYMin = isTop ? yMin + rect.Height / 2.0 : yMin;
                        double adjustedYMax = isTop ? yMax : yMax - rect.Height / 2.0;

                        Point leftPos, rightPos, centerPos;
                        GetTrianglePoints(contour, isTop, out leftPos, out rightPos, out centerPos);

                        Point[] filtered = FilterPointsInTriangle(contour, leftPos, rightPos, centerPos);


                        // 提取有效点
                        List<Point> validPoints = new();
                        foreach (var pt in filtered)
                        {
                            if (pt.X >= xMin && pt.X <= xMax && pt.Y >= adjustedYMin && pt.Y <= adjustedYMax)
                                validPoints.Add(pt);
                        }

                        string category = isTop ? "top" : "bottom";
                        contourData[category].AddRange(validPoints);
                        Debug.WriteLine($"添加{validPoints.Count}个{category}点");
                    }
                    return contourData;
                }
                finally
                {
                    gray?.Dispose();
                    binary?.Dispose();
                    kernel?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"轮廓处理失败: {ex.Message}");
                throw;
            }
        }
        public static Point[] FilterPointsInTriangle(Point[] points, Point v1, Point v2, Point v3)
        {
            List<Point> filterPoints = new List<Point>();

            foreach (var point in points)
            {
                if (IsPointInTriangle(point, v1, v2, v3))
                {
                    filterPoints.Add(point);
                }
            }

            return filterPoints.ToArray(); // 等价于 np.array(filter_points)
        }
        public static bool IsPointInTriangle(Point pt, Point v1, Point v2, Point v3, double eps = 1e-10)
        {
            double Sign(Point p1, Point p2, Point p3)
            {
                return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
            }

            double d1 = Sign(pt, v1, v2);
            double d2 = Sign(pt, v2, v3);
            double d3 = Sign(pt, v3, v1);

            bool hasNeg = (d1 < -eps) || (d2 < -eps) || (d3 < -eps);
            bool hasPos = (d1 > eps) || (d2 > eps) || (d3 > eps);

            return !(hasNeg && hasPos);
        }
        public static void GetTrianglePoints(Point[] contour, bool isTop, out Point leftPos, out Point rightPos, out Point centerPos)
        {
            // 拉平成二维数组
            var pts = contour;
            Rect rect = Cv2.BoundingRect(contour);
            int yMin = rect.Y;
            int yMax = rect.Y + rect.Height;
            if (isTop)
            {
                // 获取左下角点：最大 y - x
                Point leftBottom = pts.OrderByDescending(p => p.Y - p.X).First();
                // 获取右下角点：最大 y + x
                Point rightBottom = pts.OrderByDescending(p => p.Y + p.X).First();
                // 最小 y 值（最上方）
                int minY = pts.Min(p => p.Y);
                // 中点
                Point midBottom = new Point((leftBottom.X + rightBottom.X) / 2, minY + (rect.Height / 2.0));

                // 三个顶点
                leftPos = new Point(leftBottom.X, yMax);
                rightPos = new Point(rightBottom.X, yMax);
                centerPos = midBottom;
            }
            else
            {
                // 获取左上角点：最小 x + y
                Point leftTop = pts.OrderBy(p => p.X + p.Y).First();
                // 获取右上角点：最大 x - y
                Point rightTop = pts.OrderByDescending(p => p.X - p.Y).First();
                // 最大 y 值（最下方）
                int maxY = pts.Max(p => p.Y);
                // 中点
                Point midTop = new Point((leftTop.X + rightTop.X) / 2, maxY - (rect.Height / 2.0));

                // 三个顶点
                leftPos = new Point(leftTop.X, yMin);
                rightPos = new Point(rightTop.X, yMin);
                centerPos = midTop;
            }
        }

        /// <summary>
        /// 蛇形判断
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static AnalysisResult SnakeCase(string imagePath)
        {
            Mat image = Cv2.ImRead(imagePath);
            if (image.Empty())
                throw new Exception($"无法读取图像文件: {imagePath}");

            Dictionary<string, List<Point>> contour_data = GetContourData(image);

            if (!contour_data.TryGetValue("top", out var topPoints) || !contour_data.TryGetValue("bottom", out var bottomPoints))
            {
                throw new ArgumentException("contour_data 中缺少 top 或 bottom 数据");
            }

            if (topPoints.Count == 0 || bottomPoints.Count == 0)
            {
                throw new ArgumentException("未能检测到有效的上下边缘点");
            }

            var points = new List<(int x, int y)>();
            foreach (var point in topPoints)
            {
                points.Add((point.X, point.Y));
            }
            var topEdgePoints = points.ToList();

            points.Clear();
            foreach (var point in bottomPoints)
            {
                points.Add((point.X, point.Y));
            }
            var bottomEdgePoints = points.ToList();

            AnalysisResult result = EdgeAnalyzer.AnalyzeEdgeShape(topEdgePoints, bottomEdgePoints);

            // 打印分析结果
            Debug.WriteLine("\n边缘形状分析结果:");
            Debug.WriteLine($"最大偏差: {result.MaxDevUm} μm");
            Debug.WriteLine($"标准偏差: {result.StdDevUm} μm");
            Debug.WriteLine($"角度: {result.AngleDeg}°");
            Debug.WriteLine($"是否为蛇形: {result.Snake}");

            return result;
        }

        public static AnalysisResult SnakeCase(Mat image)
        {
            Dictionary<string, List<Point>> contour_data = GetContourData(image);

            if (!contour_data.TryGetValue("top", out var topPoints) || !contour_data.TryGetValue("bottom", out var bottomPoints))
            {
                throw new ArgumentException("contour_data 中缺少 top 或 bottom 数据");
            }

            if (topPoints.Count == 0 || bottomPoints.Count == 0)
            {
                throw new ArgumentException("未能检测到有效的上下边缘点");
            }

            var points = new List<(int x, int y)>();
            foreach (var point in topPoints)
            {
                points.Add((point.X, point.Y));
            }
            var topEdgePoints = points.ToList();

            points.Clear();
            foreach (var point in bottomPoints)
            {
                points.Add((point.X, point.Y));
            }
            var bottomEdgePoints = points.ToList();

            AnalysisResult result = EdgeAnalyzer.AnalyzeEdgeShape(topEdgePoints, bottomEdgePoints);

            // 打印分析结果
            Debug.WriteLine("\n边缘形状分析结果:");
            Debug.WriteLine($"最大偏差: {result.MaxDevUm} μm");
            Debug.WriteLine($"标准偏差: {result.StdDevUm} μm");
            Debug.WriteLine($"角度: {result.AngleDeg}°");
            Debug.WriteLine($"是否为蛇形: {result.Snake}");

            return result;
        }

        /// <summary>
        /// 获取刀痕和崩边宽度
        /// </summary>
        /// <param name="image">输入的OpenCV图像对象</param>
        /// <param name="data">包含top和bottom两组轮廓点数据的字典</param>
        /// <param name="imageWidth">图像宽度</param>
        /// <param name="ratio">像素到毫米的转换比例</param>
        /// <returns>返回元组 (刀痕宽度mm, 崩边宽度mm)</returns>
        /// <exception cref="ArgumentNullException">当输入图像或轮廓数据为空时抛出</exception>
        /// <exception cref="ArgumentException">当轮廓数据不完整或参数无效时抛出</exception>
        /// <remarks>
        /// 处理步骤：
        /// 1. 分别计算上下边缘的最大最小Y值
        /// 2. 计算刀痕宽度：下边缘最小Y - 上边缘最大Y
        /// 3. 计算崩边宽度：下边缘最大Y - 上边缘最小Y
        /// 4. 将像素单位转换为毫米
        /// </remarks>
        private static (double, double, double, double, double, double) VisualizeResults(Mat image, Dictionary<string, List<Point>> data, int imageWidth, double ratio)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image), "输入图像不能为空");
            if (data == null)
                throw new ArgumentNullException(nameof(data), "轮廓数据不能为空");
            if (!data.ContainsKey("top") || !data.ContainsKey("bottom"))
                throw new ArgumentException("轮廓数据必须包含top和bottom类别");
            if (imageWidth <= 0)
                throw new ArgumentException("图像宽度必须大于0", nameof(imageWidth));
            if (ratio <= 0)
                throw new ArgumentException("比例因子必须大于0", nameof(ratio));

            try
            {
                var yValues = new Dictionary<string, (int minY, int maxY)>();
                var yDiffs = new Dictionary<string, int>();

                foreach (var category in new[] { "top", "bottom" })
                {
                    if (data[category].Count >= 2)
                    {
                        var ys = data[category].Select(p => p.Y).ToList();
                        int minY = ys.Min();
                        int maxY = ys.Max();
                        yValues[category] = (minY, maxY);
                        yDiffs[category] = maxY - minY;
                        Debug.WriteLine($"{category}边缘点: 最小Y={minY}, 最大Y={maxY}, 差值={maxY - minY}");
                    }
                    else
                    {
                        Debug.WriteLine($"{category}边缘点数量不足: {data[category].Count}");
                    }
                }

                double bladeWidth = 0;
                double collapseWidth = 0;

                if (yValues.Count == 2)
                {
                    bladeWidth = yValues["bottom"].minY - yValues["top"].maxY;
                    collapseWidth = yValues["bottom"].maxY - yValues["top"].minY;
                    Debug.WriteLine($"计算结果 - 刀痕宽度: {bladeWidth * ratio}mm, 崩边宽度: {collapseWidth * ratio}mm");
                    return (bladeWidth * ratio, collapseWidth * ratio, yValues["bottom"].minY, yValues["top"].maxY, yValues["bottom"].maxY, yValues["top"].minY);
                }
                else
                {
                    Debug.WriteLine("未能获取完整的上下边缘数据，返回默认值0");
                    return (0, 0, 0, 0, 0, 0);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"结果可视化失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 将指定目录下的图片进行水平拼接
        /// </summary>
        /// <param name="inputDir">输入图片目录的路径</param>
        /// <param name="reverse">是否按文件名逆序排序（从右到左拼接），默认为True</param>
        /// <returns>拼接后的图片保存路径，处理失败则返回null</returns>
        /// <remarks>
        /// 处理步骤：
        /// 1. 读取目录下所有支持的图片文件（jpg、jpeg、png）
        /// 2. 将所有图片调整至相同高度
        /// 3. 按指定顺序水平拼接
        /// 4. 保存结果到指定目录
        /// </remarks>
        public static string? SpliceImages(string inputDir, bool reverse = true)
        {
            try
            {
                // 检查目录是否存在
                if (!Directory.Exists(inputDir))
                {
                    Console.WriteLine($"错误：输入目录 '{inputDir}' 不存在或不是一个有效的目录");
                    return null;
                }

                // 获取所有图像文件
                string[] extensions = new[] { ".jpg", ".jpeg", ".png" };
                var imgFiles = Directory.GetFiles(inputDir)
                    .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()))
                    .ToList();

                if (imgFiles.Count == 0)
                {
                    Console.WriteLine($"错误：目录 '{inputDir}' 中没有找到任何图片文件");
                    return null;
                }

                // 按文件名排序
                imgFiles.Sort();
                if (reverse)
                    imgFiles.Reverse();

                List<Mat> images = new List<Mat>();
                int minHeight = int.MaxValue;

                foreach (var file in imgFiles)
                {
                    var img = Cv2.ImRead(file);
                    if (img.Empty())
                    {
                        Console.WriteLine($"警告：无法读取图片 '{Path.GetFileName(file)}'，已跳过");
                        continue;
                    }
                    images.Add(img);
                    minHeight = Math.Min(minHeight, img.Rows);
                }

                if (images.Count == 0)
                {
                    Console.WriteLine("错误：没有成功读取任何图片");
                    return null;
                }

                // Resize 所有图片到相同高度
                List<Mat> resizedImages = new List<Mat>();
                foreach (var img in images)
                {
                    double scale = (double)minHeight / img.Rows;
                    int newWidth = (int)(img.Cols * scale);
                    Mat resized = new Mat();
                    Cv2.Resize(img, resized, new Size(newWidth, minHeight));
                    resizedImages.Add(resized);
                }

                // 拼接图片
                Mat result = new Mat();
                Cv2.HConcat(resizedImages, result);

                // 保存结果
                string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "file", "images", "spliced", DateTime.Now.ToString("yyyy-MM-dd"));
                Directory.CreateDirectory(baseDir);
                string fileName = $"spliced_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}_{Guid.NewGuid().ToString().Substring(0, 8)}.jpg";
                string outputPath = Path.Combine(baseDir, fileName);
                Cv2.ImWrite(outputPath, result);
                Console.WriteLine($"拼接完成，结果已保存到: {outputPath}");

                return outputPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误：图片拼接过程中发生异常: {ex.Message}");
                return null;
            }
        }

        public static string? SpliceImages(List<Mat> mats)
        {
            try
            {
                // 拼接图片
                Mat result = new Mat();
                Cv2.HConcat(mats, result);
                // 保存结果
                string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "file", "images", "spliced", DateTime.Now.ToString("yyyy-MM-dd"));
                Directory.CreateDirectory(baseDir);
                string fileName = $"spliced_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}_{Guid.NewGuid().ToString().Substring(0, 8)}.jpg";
                string outputPath = Path.Combine(baseDir, fileName);
                Cv2.ImWrite(outputPath, result);
                Console.WriteLine($"拼接完成，结果已保存到: {outputPath}");
                return outputPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误：图片拼接过程中发生异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 在大图中查找模板图片的位置
        /// </summary>
        /// <param name="searchType">搜索类型：1-磨刀，2-切割</param>
        /// <param name="searchImagePath">待搜索的大图路径</param>
        /// <returns>返回元组 (匹配位置的中心坐标, 匹配置信度)，未找到则返回 (null, 0)</returns>
        /// <exception cref="ArgumentException">当图片无法加载时抛出</exception>
        /// <remarks>
        /// 使用模板匹配算法查找目标位置，匹配方法为归一化相关系数
        /// </remarks>
        public static Tuple<Point?, double> FindTemplate(int searchType, string searchImagePath)
        {
            string templateImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "templateImage", searchType == 1 ? "sharpening_template.png" : "cut_template.png");
            // 加载图像（灰度模式）
            using var template = Cv2.ImRead(templateImagePath, ImreadModes.Grayscale);
            using var image = Cv2.ImRead(searchImagePath, ImreadModes.Grayscale);

            if (template.Empty() || image.Empty())
            {
                throw new ArgumentException("无法加载图片，请检查图片路径是否正确");
            }

            // 模板大小
            int w = template.Cols;
            int h = template.Rows;

            // 执行模板匹配
            using var result = new Mat();
            Cv2.MatchTemplate(image, template, result, TemplateMatchModes.CCoeffNormed);

            // 查找最大匹配位置
            Cv2.MinMaxLoc(result, out double minVal, out double maxVal, out Point minLoc, out Point maxLoc);

            // 中心点坐标
            Point center = new Point(maxLoc.X + w / 2, maxLoc.Y + h / 2);

            return Tuple.Create<Point?, double>(center, maxVal);
        }

        /// <summary>
        /// 检测图像中的多条横向条纹，只返回第一条有效条纹的中心线位置
        /// </summary>
        /// <param name="imagePath">图像路径</param>
        /// <param name="minAreaThreshold">最小有效条纹区域面积阈值（默认1000）</param>
        /// <returns>第一条有效条纹的中心 y 坐标，如果未检测到则返回 null</returns>
        public static int? DetectFirstHorizontalStripeCenter(string imagePath, double minAreaThreshold = 1000.0)
        {
            // 读取灰度图像
            Mat img = Cv2.ImRead(imagePath, ImreadModes.Grayscale);
            if (img.Empty())
            {
                throw new FileNotFoundException("图像读取失败，请检查路径");
            }

            // 使用 CLAHE 进行对比度自适应增强（局部对比度提升）
            CLAHE clahe = Cv2.CreateCLAHE(clipLimit: 3.0, tileGridSize: new Size(8, 8));
            Mat imgEq = new Mat();
            clahe.Apply(img, imgEq);

            // OTSU 自动阈值 + 反转，使图像中条纹为白（像素值255）
            Mat binary = new Mat();
            Cv2.Threshold(imgEq, binary, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

            int imageWidth = binary.Cols;
            int imageHeight = binary.Rows;

            // 水平投影：统计每一行中非零（白色）像素数量
            int[] projection = new int[imageHeight];
            for (int i = 0; i < imageHeight; i++)
            {
                projection[i] = Cv2.CountNonZero(binary.Row(i));
            }

            // 根据投影最大值设定阈值，判定是否为“可能条纹”行
            int maxProj = projection.Max();
            double threshold = 0.5 * maxProj;

            // 构建 bool 数组，标记每一行是否为条纹
            bool[] isLine = new bool[imageHeight];
            for (int i = 0; i < imageHeight; i++)
            {
                isLine[i] = projection[i] > threshold;
            }

            // 条纹区域检测：根据 isLine 连续值构建区段，并进行多重判断
            List<(int top, int bottom)> validLines = new List<(int, int)>();
            int? start = null;

            for (int i = 0; i < isLine.Length; i++)
            {
                int y = i;

                if (isLine[i] && start == null)
                {
                    // 检测到新条纹起始
                    start = y;
                }
                else if (!isLine[i] && start != null)
                {
                    // 条纹结束，计算条纹特征
                    int top = start.Value;
                    int bottom = y - 1;
                    int lineHeight = bottom - top + 1;
                    double lineArea = lineHeight * imageWidth;

                    // 高度 + 面积初步过滤
                    if (lineHeight > 2 && lineArea > minAreaThreshold)
                    {
                        // 计算实际区域中“白色像素”（条纹）占比
                        Mat lineRegion = binary.SubMat(top, bottom + 1, 0, imageWidth);
                        int actualWhitePixels = Cv2.CountNonZero(lineRegion);
                        double areaRatio = (double)actualWhitePixels / lineArea;

                        if (areaRatio > 0.5)
                        {
                            // 符合要求，加入结果
                            Console.WriteLine($"检测到有效横线: Y={top}-{bottom}, 高度={lineHeight}, 面积={lineArea}, 黑色像素比例={areaRatio:F2}");
                            validLines.Add((top, bottom));
                        }
                        else
                        {
                            Console.WriteLine($"跳过低密度区域: Y={top}-{bottom}, 黑色像素比例={areaRatio:F2}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"跳过小区域: Y={top}-{bottom}, 高度={lineHeight}, 面积={lineArea}");
                    }

                    // 结束当前条纹记录
                    start = null;
                }
            }

            // 补处理：若最后一行仍处于条纹区域
            if (start != null)
            {
                int top = start.Value;
                int bottom = imageHeight - 1;
                int lineHeight = bottom - top + 1;
                double lineArea = lineHeight * imageWidth;

                if (lineHeight > 2 && lineArea > minAreaThreshold)
                {
                    Mat lineRegion = binary.SubMat(top, bottom + 1, 0, imageWidth);
                    int actualWhitePixels = Cv2.CountNonZero(lineRegion);
                    double areaRatio = (double)actualWhitePixels / lineArea;

                    if (areaRatio > 0.5)
                    {
                        Console.WriteLine($"检测到有效横线(末尾): Y={top}-{bottom}, 高度={lineHeight}, 面积={lineArea}, 黑色像素比例={areaRatio:F2}");
                        validLines.Add((top, bottom));
                    }
                }
            }

            // 输出结果
            Console.WriteLine($"\n总共检测到 {validLines.Count} 条有效横线");

            // 返回第一条有效横线的中心 y 坐标
            if (validLines.Count > 0)
            {
                var (top, bottom) = validLines[0];
                return (top + bottom) / 2;
            }
            else
            {
                return null;
            }
        }

        public static int? DetectFirstHorizontalStripeCenter(Mat img, double minAreaThreshold = 1000.0)
        {
            // 转换为灰度图
            Mat gray = new Mat();
            Cv2.CvtColor(img, gray, ColorConversionCodes.BGR2GRAY);

            // 创建CLAHE并应用
            CLAHE clahe = Cv2.CreateCLAHE(clipLimit: 3.0, tileGridSize: new Size(8, 8));
            Mat imgEq = new Mat();
            clahe.Apply(gray, imgEq);

            // OTSU 自动阈值 + 反转，使图像中条纹为白（像素值255）
            Mat binary = new Mat();
            Cv2.Threshold(imgEq, binary, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

            int imageWidth = binary.Cols;
            int imageHeight = binary.Rows;

            // 水平投影：统计每一行中非零（白色）像素数量
            int[] projection = new int[imageHeight];
            for (int i = 0; i < imageHeight; i++)
            {
                projection[i] = Cv2.CountNonZero(binary.Row(i));
            }

            // 根据投影最大值设定阈值，判定是否为“可能条纹”行
            int maxProj = projection.Max();
            double threshold = 0.5 * maxProj;

            // 构建 bool 数组，标记每一行是否为条纹
            bool[] isLine = new bool[imageHeight];
            for (int i = 0; i < imageHeight; i++)
            {
                isLine[i] = projection[i] > threshold;
            }

            // 条纹区域检测：根据 isLine 连续值构建区段，并进行多重判断
            List<(int top, int bottom)> validLines = new List<(int, int)>();
            int? start = null;

            for (int i = 0; i < isLine.Length; i++)
            {
                int y = i;

                if (isLine[i] && start == null)
                {
                    // 检测到新条纹起始
                    start = y;
                }
                else if (!isLine[i] && start != null)
                {
                    // 条纹结束，计算条纹特征
                    int top = start.Value;
                    int bottom = y - 1;
                    int lineHeight = bottom - top + 1;
                    double lineArea = lineHeight * imageWidth;

                    // 高度 + 面积初步过滤
                    if (lineHeight > 2 && lineArea > minAreaThreshold)
                    {
                        // 计算实际区域中“白色像素”（条纹）占比
                        Mat lineRegion = binary.SubMat(top, bottom + 1, 0, imageWidth);
                        int actualWhitePixels = Cv2.CountNonZero(lineRegion);
                        double areaRatio = (double)actualWhitePixels / lineArea;

                        if (areaRatio > 0.5)
                        {
                            // 符合要求，加入结果
                            Console.WriteLine($"检测到有效横线: Y={top}-{bottom}, 高度={lineHeight}, 面积={lineArea}, 黑色像素比例={areaRatio:F2}");
                            validLines.Add((top, bottom));
                        }
                        else
                        {
                            Console.WriteLine($"跳过低密度区域: Y={top}-{bottom}, 黑色像素比例={areaRatio:F2}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"跳过小区域: Y={top}-{bottom}, 高度={lineHeight}, 面积={lineArea}");
                    }

                    // 结束当前条纹记录
                    start = null;
                }
            }

            // 补处理：若最后一行仍处于条纹区域
            if (start != null)
            {
                int top = start.Value;
                int bottom = imageHeight - 1;
                int lineHeight = bottom - top + 1;
                double lineArea = lineHeight * imageWidth;

                if (lineHeight > 2 && lineArea > minAreaThreshold)
                {
                    Mat lineRegion = binary.SubMat(top, bottom + 1, 0, imageWidth);
                    int actualWhitePixels = Cv2.CountNonZero(lineRegion);
                    double areaRatio = (double)actualWhitePixels / lineArea;

                    if (areaRatio > 0.5)
                    {
                        Console.WriteLine($"检测到有效横线(末尾): Y={top}-{bottom}, 高度={lineHeight}, 面积={lineArea}, 黑色像素比例={areaRatio:F2}");
                        validLines.Add((top, bottom));
                    }
                }
            }

            // 输出结果
            Console.WriteLine($"\n总共检测到 {validLines.Count} 条有效横线");

            // 返回第一条有效横线的中心 y 坐标
            if (validLines.Count > 0)
            {
                var (top, bottom) = validLines[0];
                return (top + bottom) / 2;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 获取模糊度评分
        /// > 100：较清晰 < 50：较模糊
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static double GetBlurrinessScore(Mat image)
        {
            // 转换为灰度图（如果输入是彩色图）
            Mat gray = new Mat();
            if (image.Channels() == 3)
            {
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                gray = image.Clone();
            }

            // 计算拉普拉斯算子
            Mat laplacian = new Mat();
            Cv2.Laplacian(gray, laplacian, MatType.CV_64F);

            // 计算方差（方差越高，图像越清晰）
            Scalar mean, stddev;
            Cv2.MeanStdDev(laplacian, out mean, out stddev);
            double variance = stddev.Val0 * stddev.Val0;

            return variance;
        }
    }
}
