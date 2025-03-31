using Emgu.CV;
using MathNet.Numerics;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.ViewModel
{
    internal class BladeImage
    {
        public BladeImage()
        {
        }

        // 刀痕内径,上内径左点，下内径左点
        public List<System.Drawing.Point> lineInnerPoint = new List<System.Drawing.Point>();
        // 刀痕外径,上外径左点，下外径左点
        public List<System.Drawing.Point> lineExternalPoint = new List<System.Drawing.Point>();
        // 最大崩角中心点
        //public System.Drawing.Point circleCenter = new System.Drawing.Point();

        // 识别图像后的返回对象
        public ImageResult imageResult = new ImageResult();
        // 最大崩角半径
        public int radius = 0;
        // 原始图像对象
        public OpenCvSharp.Mat mat = new OpenCvSharp.Mat();
        // 预处理后的图像对象
        public OpenCvSharp.Mat preBinary = new OpenCvSharp.Mat();
        // 原始图像绝对路径
        public string srcImagePath = "";
        public int width, height;
        public BladeImage(string path)
        {
            srcImagePath = path;
            ReadMat(srcImagePath);
        }

        public BladeImage(Emgu.CV.Mat s_mat)
        {
            mat = ConvertEmguCVMatToOpenCvSharpMat(s_mat);
        }

        public OpenCvSharp.Mat ConvertEmguCVMatToOpenCvSharpMat(Emgu.CV.Mat emguMat)
        {
            // Emgu.CV.Mat 转 OpenCvSharp.Mat
            OpenCvSharp.Mat openCvMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(emguMat.ToBitmap());
            return openCvMat;
        }

        private OpenCvSharp.Mat ReadMat(string? mat_path)
        {
            if (mat_path != null)
            {
                srcImagePath = mat_path;
            }
            mat = Cv2.ImRead(srcImagePath, OpenCvSharp.ImreadModes.Color);
            width = mat.Width;
            height = mat.Height;
            return mat;
        }

        public OpenCvSharp.Mat PreProcess(string? imgPath)
        {
            ReadMat(srcImagePath);
            // 灰度化
            OpenCvSharp.Mat gray = new();
            Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
            /*Cv2.ImShow("gray Image", gray);
            Cv2.WaitKey(0);*/

            // 方法1: 使用高斯模糊去噪
            OpenCvSharp.Mat gaussianBlurOutput = new();
            Cv2.GaussianBlur(gray, gaussianBlurOutput, new OpenCvSharp.Size(9, 9), 0);

            // 方法2: 使用中值滤波去噪
            OpenCvSharp.Mat medianBlurOutput = new();
            Cv2.MedianBlur(gaussianBlurOutput, medianBlurOutput, 9);
            for (int i = 0; i < 2; i++)
            {
                Cv2.MedianBlur(medianBlurOutput, medianBlurOutput, 9);
            }
            //Cv2.MedianBlur(medianBlurOutput, medianBlurOutput, 9);

            double median = GetMedianValue(medianBlurOutput);
            /*Cv2.ImShow("median Image", medianBlurOutput);
            Cv2.WaitKey(0);*/

            // 二值化
            OpenCvSharp.Mat binary = new();
            //Cv2.Threshold(medianBlurOutput, binary, 127, 255, ThresholdTypes.Binary);
            Cv2.Threshold(medianBlurOutput, binary, median, 255, ThresholdTypes.Binary);
            // 显示图像
            /*Cv2.ImShow("binary Image", binary);
            // 等待任意键按下
            Cv2.WaitKey(0);*/

            // 竖直刀痕补为白色
            List<int> myColSumList = new List<int>();
            for (int y = 0; y < binary.Cols; y++)
            {
                int colValue = 0;
                for (int x = 0; x < binary.Rows; x++)
                {
                    // 获取(x, y)位置的像素值
                    byte pixelValue = binary.At<byte>(x, y);
                    // 使用像素值 255为白色
                    if (pixelValue == 0)
                    {
                        colValue += 1;
                    }
                }
                myColSumList.Add(colValue);
                if (colValue > binary.Rows * 0.8)
                {
                    for (int x = 0; x < binary.Rows; x++)
                    {
                        // 设置(x, y)位置的像素值
                        binary.At<byte>(x, y) = 255;
                    }
                }
            }

            /*Cv2.ImShow("binary Image", binary);
            // 等待任意键按下
            Cv2.WaitKey(0);*/

            // 保存结果
            // Cv2.ImWrite("D:\\project\\image\\ZH\\medianBlurOutput_image_path.png", binary);
            preBinary = binary;
            return binary;
        }

        public static double GetMedianValue(OpenCvSharp.Mat imageMat)
        {
            // Calculate median value
            SortedSet<double> pixelValues = new SortedSet<double>();
            for (int y = 0; y < imageMat.Rows; y++)
            {
                for (int x = 0; x < imageMat.Cols; x++)
                {
                    pixelValues.Add(imageMat.At<byte>(y, x));
                }
            }

            return pixelValues.ElementAt((int)(pixelValues.Count * 0.4));
        }

        public List<int> myRowSumList = new List<int>();

        // 查找刀痕
        /// <summary>
        /// 
        /// </summary>
        /// <param name="startRow"></param>
        /// 
        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public ImageResult FindLinePoint(string? imgPath)
        {
            if (imgPath == null)
            {
                imgPath = srcImagePath;
            }
            OpenCvSharp.Mat mat = PreProcess(imgPath);
            imageResult = new ImageResult();
            imageResult.width = width;
            imageResult.height = height;
            OpenCvSharp.Mat.Indexer<Vec3b> indexer = mat.GetGenericIndexer<Vec3b>();
            // 遍历图像的每个像素
            for (int row = 0; row < mat.Rows; row++)
            {
                for (int col = 0; col < mat.Cols; col++)
                {
                    // 获取像素值
                    Vec3b pixelValue = indexer[row, col];

                    // 处理像素值
                    byte blue = pixelValue.Item0;
                    byte green = pixelValue.Item1;
                    byte red = pixelValue.Item2;

                    // 你可以在这里添加你的处理代码

                    // 设置新的像素值（如果需要）
                    indexer[row, col] = new Vec3b(blue, green, red);
                }
            }


            List<int> myRowSumList = new List<int>();
            // 遍历图片的每个像素,按行遍历，累加每行
            for (int y = 0; y < mat.Rows; y++)
            {
                int rowValue = 0;
                for (int x = 0; x < mat.Cols; x++)
                {
                    // 获取(x, y)位置的像素值
                    byte pixelValue = mat.At<byte>(y, x);
                    // 使用像素值
                    if (pixelValue == 255)
                    {
                        rowValue += 1;
                    }
                }
                myRowSumList.Add(rowValue);
            }

            /*Cv2.NamedWindow("mat Image", 0);
            Cv2.ResizeWindow("mat Image", width / 2, height / 2);
            Cv2.ImShow("mat Image", mat);
            // 等待任意键按下
            Cv2.WaitKey(0);*/

            int begin1 = 0;
            int d_1 = 0;
            int d_2 = 0;
            int d_3 = 0;
            int d_4 = 0;
            List<int> copiedRowList = myRowSumList.ToList();
            copiedRowList.Sort();
            // int tagValueHigh = copiedRowList[Convert.ToInt32(0.55 * width)];
            int tagValueHigh = Convert.ToInt32(0.88 * width);
            // int tagValueLow = copiedRowList[Convert.ToInt32(0.05 * height)];
            int tagValueLow = 20;
            // 找到上刀痕内径的一行d_2
            for (int i = 0; i < myRowSumList.Count; i++)
            {
                if ((myRowSumList[i] >= tagValueHigh) && (begin1 == 0))
                {
                    begin1 = 1;
                }
                if ((myRowSumList[i] <= tagValueLow) && (begin1 == 1))
                {

                    begin1 = 2;
                    d_2 = i;
                    break;
                }
            }
            // 找到上刀痕外径的一行d_1
            if (begin1 == 2)
            {


                for (int i = d_2; i > 10; i--)
                {

                    byte pixelValue1 = 255;
                    byte pixelValue2 = 255;
                    byte pixelValue3 = 255;
                    byte pixelValue4 = 255;
                    byte pixelValue5 = 255;
                    byte pixelValue6 = 255;
                    int sumTmp = 255;
                    // 遍历图片一行的每个像素  255为白色
                    for (int x = 0; x < mat.Cols - 3; x++)
                    {
                        // 获取(行, 列)位置的像素值 向下向右+
                        pixelValue1 = mat.At<byte>(i, x);
                        pixelValue2 = mat.At<byte>(i, x + 1);
                        pixelValue3 = mat.At<byte>(i, x + 2);
                        pixelValue4 = mat.At<byte>(i - 1, x + 1);
                        pixelValue5 = mat.At<byte>(i - 2, x + 1);
                        pixelValue6 = mat.At<byte>(i - 3, x + 1);
                        sumTmp = pixelValue1 + pixelValue2 + pixelValue3 + pixelValue4 + pixelValue5;
                        if (sumTmp == 0)
                        {
                            break;
                        }
                    }
                    if (sumTmp == 0)
                    {
                        continue;
                    }

                    if (myRowSumList[i] >= tagValueHigh)
                    {
                        d_1 = i;
                        begin1 = 3;
                        break;
                    }
                }
            }
            if (begin1 == 3)
            {
                for (int i = d_2 + 1; i < height - 10; i++)
                {
                    if (myRowSumList[i] >= tagValueLow)
                    {
                        List<int> ints = new List<int>();
                        for (int x = 0; x < mat.Width; x++)
                        {
                            int sum_i = 0;
                            // 遍历图片一行的每个像素  255为白色
                            for (int y = i; y < i + 10; y++)
                            {
                                // 获取(行, 列)位置的像素值 向下向右+
                                sum_i += mat.At<byte>(y, x);
                            }
                            ints.Add(sum_i);
                        }
                        int increaseCount = 0;
                        int decreaseCount = 0;
                        bool stat = false;
                        for (int x = 1; x < ints.Count; x++)
                        {
                            if (ints[x] > ints[x - 1] && !stat)
                            {
                                increaseCount++;
                                stat = true;
                            }
                            if (ints[x] < ints[x - 1] && stat)
                            {
                                decreaseCount++;
                                stat = false;
                            }
                        }
                        if (decreaseCount < 30)
                        {
                            continue;
                        }
                        /*// 计算方差
                        double variance = Math.Sqrt(Statistics.PopulationVariance(ints));
                        if (variance < 3000)
                        {
                            continue;
                        }*/

                        d_3 = i;
                        begin1 = 4;
                        break;
                    }
                }
            }
            if (begin1 == 4)
            {
                for (int i = d_3; i < height - 3; i++)
                {
                    byte pixelValue1 = 255;
                    byte pixelValue2 = 255;
                    byte pixelValue3 = 255;
                    byte pixelValue4 = 255;
                    byte pixelValue5 = 255;
                    byte pixelValue6 = 255;
                    int sumTmp = 255;
                    // 遍历图片一行的每个像素
                    for (int x = 0; x < mat.Cols - 3; x++)
                    {
                        // 获取(行, 列)位置的像素值
                        pixelValue1 = mat.At<byte>(i, x);
                        pixelValue2 = mat.At<byte>(i, x + 1);
                        pixelValue3 = mat.At<byte>(i, x + 2);
                        pixelValue4 = mat.At<byte>(i + 1, x + 1);
                        pixelValue5 = mat.At<byte>(i + 2, x + 1);
                        pixelValue6 = mat.At<byte>(i + 3, x + 1);
                        sumTmp = pixelValue1 + pixelValue2 + pixelValue3 + pixelValue4;
                        if (sumTmp == 0)
                        {
                            break;
                        }
                    }
                    if (sumTmp == 0)
                    {
                        continue;
                    }

                    if (myRowSumList[i] >= tagValueHigh)
                    {
                        d_4 = i;
                        begin1 = 5;
                        break;
                    }
                }
            }
            if (begin1 != 5)
            {
                imageResult.returnCode = 1;
                return imageResult;
            }
            imageResult.knifeMarkPoint.Clear();
            int max_sub = (d_2 - d_1) - (d_4 - d_3);
            if (max_sub >= 0)
            {
                d_4 = d_4 + max_sub;
            }
            else
            {
                d_1 = d_1 + max_sub;
            }
            // 定义线条的起点和终点
            imageResult.knifeMarkPoint.Add(new System.Drawing.Point(0, d_1));
            lineInnerPoint.Add(new System.Drawing.Point(width - 1, d_1));

            imageResult.knifeMarkPoint.Add(new System.Drawing.Point(0, d_2));
            lineExternalPoint.Add(new System.Drawing.Point(width - 1, d_2));

            imageResult.knifeMarkPoint.Add(new System.Drawing.Point(0, d_3));
            lineInnerPoint.Add(new System.Drawing.Point(width - 1, d_3));

            imageResult.knifeMarkPoint.Add(new System.Drawing.Point(0, d_4));
            lineExternalPoint.Add(new System.Drawing.Point(width - 1, d_4));

            imageResult.maxChipping = Math.Max((d_2 - d_1), (d_4 - d_3));
            imageResult.innerBlade = d_3 - d_2;
            imageResult.outerBlade = imageResult.innerBlade + imageResult.maxChipping * 2;

            // 遍历上崩边求斜率
            /*int d_5 = 0;
            int d_6 = 0;
            double[] dataX = new double[width];
            double[] myColUpperSumList = new double[width];
            for (int x = 0; x < mat.Cols; x++)
            {
                int colValue = 0;
                for (int y = d_1; y < d_2; y++)
                {
                    // 获取(x, y)位置的像素值
                    byte pixelValue = mat.At<byte>(y, x);
                    // 使用像素值
                    if (pixelValue == 255)
                    {
                        colValue += 1;
                    }
                }
                myColUpperSumList[x] = colValue;
                dataX[x] = x;
            }
            var fitResult = Fit.Line(dataX, myColUpperSumList);
            d_5 = Convert.ToInt32(fitResult.Item1);
            d_6 = Convert.ToInt32(fitResult.Item2 * (width - 1) + fitResult.Item1);
            imageResult.knifeSlopePoint.Add(new System.Drawing.Point(0, d_1 + d_5));
            imageResult.knifeSlopePoint.Add(new System.Drawing.Point(width - 1, d_1 + d_6));
            imageResult.upperSlope = fitResult.Item2;

            // 遍历下崩边求斜率
            int d_7 = 0;
            int d_8 = 0;
            double[] dataX1 = new double[width];
            double[] myColLowerSumList1 = new double[width];

            if (mat == null || mat.Empty())
            {
                throw new InvalidOperationException("Mat is null or empty.");
            }

            for (int x = 0; x < mat.Cols; x++)
            {
                int colValue = 0;
                for (int y = d_3; y < d_4; y++)
                {
                    if (y < 0 || y >= mat.Rows)
                    {
                        throw new IndexOutOfRangeException($"Invalid y index: y={y}, Rows={mat.Rows}");
                    }
                    // 获取(x, y)位置的像素值 这段可能要出错
                    try
                    {
                        // 有潜在崩溃的代码
                        byte pixelValue = mat.At<byte>(y, x);
                        // 使用像素值
                        if (pixelValue == 0)
                        {
                            colValue += 1;
                        }
                    }
                    catch (AccessViolationException ex)
                    {
                        Console.WriteLine($"Caught an AccessViolationException: {ex.Message}");
                    }
                    
                }
                myColLowerSumList1[x] = colValue;
                dataX1[x] = x;
            }
            var fitResult1 = Fit.Line(dataX1, myColLowerSumList1);
            d_7 = Convert.ToInt32(fitResult1.Item1);
            d_8 = Convert.ToInt32(fitResult1.Item2 * (width - 1) + fitResult1.Item1);
            imageResult.knifeSlopePoint.Add(new System.Drawing.Point(0, d_3 + d_7));
            imageResult.knifeSlopePoint.Add(new System.Drawing.Point(width - 1, d_3 + d_8));
            imageResult.lowerSlope = fitResult.Item2;*/
            //// 在PictureBox上画线
            //g.DrawLine(pen, startPoint, endPoint);
            //g.DrawLine(pen, startPoint1, endPoint1);

            //// 释放Graphics对象占用的资源
            //g.Dispose();
            //double ac_inner = (d_3 - d_2) / (double)h * double.Parse(tbxHeight.Text);
            //tbxInnerLength.Text = ac_inner.ToString();
            if (Path.Exists(imgPath))
            {
                File.Delete(imgPath);
            }
            return imageResult;
        }


        public void FitLine(double[] x, double[] y)
        {
            if (x.Length != y.Length)
                throw new ArgumentException("x and y must have the same length");

            int n = x.Length;
            double sumX = 0;
            double sumY = 0;
            double sumX2 = 0;
            double sumXY = 0;

            for (int i = 0; i < n; i++)
            {
                sumX += x[i];
                sumY += y[i];
                sumX2 += x[i] * x[i];
                sumXY += x[i] * y[i];
            }

            double delta = sumX * sumX - n * sumX2;
            double Slope = (n * sumXY - sumX * sumY) / delta;
            double Intercept = (sumY * sumX2 - sumX * sumXY) / delta;

            // Console.WriteLine($"Line equation: y = {lineFitter.Slope} * x + {lineFitter.Intercept}");

            // 方法二
            // 示例数据点
            /*var dataX = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
            var dataY = new[] { 2.0, 4.0, 6.0, 8.0, 10.0 };

            // 拟合直线
            var fitResult = Fit.Line(dataX, dataY);

            // 输出拟合的直线参数：斜率和截距
            Console.WriteLine("斜率: " + fitResult.Item1);
            Console.WriteLine("截距: " + fitResult.Item2);*/
        }

        public static double[] CalculateCutWidthAndEdges(string imagePath)
        {
            // 读取图像
            OpenCvSharp.Mat image = Cv2.ImRead(imagePath);

            // 获取图像的宽高
            int height = image.Height;
            int width = image.Width;

            // 转换为灰度图像
            OpenCvSharp.Mat grayImage = new OpenCvSharp.Mat();
            Cv2.CvtColor(image, grayImage, ColorConversionCodes.BGR2GRAY);

            // 应用高斯模糊去噪
            OpenCvSharp.Mat blurredImage = new OpenCvSharp.Mat();
            Cv2.GaussianBlur(grayImage, blurredImage, new OpenCvSharp.Size(5, 5), 0);

            // 使用阈值方法提取黑色区域
            OpenCvSharp.Mat thresh = new OpenCvSharp.Mat();
            Cv2.Threshold(blurredImage, thresh, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

            // 寻找轮廓
            Cv2.FindContours(thresh, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            // 初始化返回值
            int cutWidth = 0;
            int cutEdges = 0;

            // 遍历轮廓以绘制符合条件的点并获取y轴的最大值和最小值
            foreach (var contour in contours)
            {
                // 计算轮廓面积
                double area = Cv2.ContourArea(contour);
                if (area > 1000)
                {
                    // 计算轮廓的平均y坐标
                    int yMean = (int)contour.Select(point => point.Y).Average();

                    // 初始化上下半部分的y轴最大值和最小值
                    int upperYMin = int.MaxValue;
                    int upperYMax = int.MinValue;
                    int lowerYMin = int.MaxValue;
                    int lowerYMax = int.MinValue;

                    foreach (var point in contour)
                    {
                        int x = point.X;
                        int y = point.Y;

                        // 排除x=0和x=width的点
                        if (x != 0 && x != width - 1)
                        {
                            if (y < yMean)
                            {
                                // 上半部分更新y最大值和最小值
                                upperYMin = Math.Min(upperYMin, y);
                                upperYMax = Math.Max(upperYMax, y);
                            }
                            else
                            {
                                // 下半部分更新y最大值和最小值
                                lowerYMin = Math.Min(lowerYMin, y);
                                lowerYMax = Math.Max(lowerYMax, y);
                            }
                        }
                    }

                    int upperEdges = upperYMax - upperYMin;
                    int lowerEdges = lowerYMax - lowerYMin;

                    // 如果上崩边大于下崩边，则把下崩边改为和上崩边一样
                    if (upperEdges > lowerEdges)
                    {
                        lowerYMax = lowerYMin + upperEdges;
                    }
                    else if (upperEdges < lowerEdges)
                    {
                        upperYMin = upperYMax - lowerEdges;
                    }

                    if (cutWidth == 0)
                    {
                        // 计算cutWidth和cutEdges
                        cutWidth = lowerYMin - upperYMax;
                        cutEdges = lowerYMax - upperYMin;
                    }
                }
            }

            // 返回结果
            return [cutWidth, cutEdges];
        }

        // 计算模糊度
        public double CalculateBlur()
        {
            // 计算拉普拉斯变换
            OpenCvSharp.Mat laplacian = new OpenCvSharp.Mat();
            Cv2.Laplacian(mat, laplacian, MatType.CV_64F);

            // 计算方差
            Scalar mean, stddev;
            Cv2.MeanStdDev(laplacian, out mean, out stddev);
            double variance = stddev.Val0 * stddev.Val0;
            return variance;


            //// 将图像转换为灰度图，因为拉普拉斯算子通常在灰度图像上操作
            //Mat grayImage = new Mat();
            //Cv2.CvtColor(mat, grayImage, ColorConversionCodes.BGR2GRAY);

            //// 应用拉普拉斯算子
            //Mat laplacianImage = new Mat();
            //Cv2.Laplacian(grayImage, laplacianImage, MatType.CV_64F);

            //// 计算拉普拉斯图像的绝对值
            //Mat absLaplacianImage = new Mat();
            //Cv2.ConvertScaleAbs(laplacianImage, absLaplacianImage);

            //// 计算所有像素值的总和，这可以作为清晰度的一个指标
            //double totalSum = Cv2.Sum(absLaplacianImage).Val0;

            //// 为了得到一个标准化的模糊度值，可以除以像素总数
            //double numPixels = grayImage.Rows * grayImage.Cols;
            //double averageValue = totalSum / numPixels;
            //return averageValue;
        }
    }

    public class ImageResult
    {
        public ImageResult()
        {
        }
        // 图像识别返回码：0为正常识别
        public int returnCode = 0;
        // 刀痕识别四个定位点：最左边的上外径点，上内径点，下内径点，下外径点
        public List<System.Drawing.Point> knifeMarkPoint = new List<System.Drawing.Point>();
        // 图像的宽度和高度像素分辨率
        public int width, height;
        // 图像的宽度和高度范围实际长度值
        public float widthSpace, heightSpace;
        // 最大崩边像素值
        public int maxChipping;
        // 刀痕内径像素值
        public int innerBlade;
        // 刀痕外径像素值
        public int outerBlade;
        // 刀痕斜率四个定位点：最左边的上刀痕点，最右边的上刀痕点，最左边的下刀痕点，最右边的下刀痕点
        public List<System.Drawing.Point> knifeSlopePoint = new List<System.Drawing.Point>();
        // 上崩边斜率，下崩边斜率
        public double upperSlope, lowerSlope;
    }
}
