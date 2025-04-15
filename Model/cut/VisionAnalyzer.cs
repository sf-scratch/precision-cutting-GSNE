using MathNet.Numerics.Statistics;
using MathNet.Numerics;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
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
        const int THRESHOLD_AREA = 1000;
        /// <summary>边界偏移量，用于去除边缘干扰</summary>
        const int BORDER_OFFSET = 10;

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
        public static (double bladeWidthMm, double collapseWidthMm) ProcessImage(string imagePath, double pixelToMmRatio = 1.0)
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
                Cv2.Threshold(gray, binary, 100, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

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

                contours = contours.OrderBy(c => Cv2.BoundingRect(c).X).ToArray();
                Debug.WriteLine($"检测到{contours.Length}个轮廓");

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

                        // 提取有效点
                        List<Point> validPoints = new();
                        foreach (var pt in contour)
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
        /// <summary>
        /// 蛇形判断
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ArgumentException"></exception>
        private static AnalysisResult SnakeCase(string imagePath)
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
        private static (double, double) VisualizeResults(Mat image, Dictionary<string, List<Point>> data, int imageWidth, double ratio)
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
                }
                else
                {
                    Debug.WriteLine("未能获取完整的上下边缘数据，返回默认值0");
                }

                return (bladeWidth * ratio, collapseWidth * ratio);
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

        
    }
}
