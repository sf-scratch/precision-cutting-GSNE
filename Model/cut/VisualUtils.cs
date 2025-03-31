using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows;
using NPOI.Util;
using System.Runtime.InteropServices;

namespace 精密切割系统.Driver
{
    internal class VisualUtils
    {
        public static double CalculateTenengrad1(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                return 0;
            }

            Mat image = CvInvoke.Imread(imagePath, ImreadModes.Color);
            if (image.IsEmpty)
            {
                throw new Exception($"无法读取图像文件: {imagePath}");
            }

            Mat grayImage = new Mat();
            CvInvoke.CvtColor(image, grayImage, ColorConversion.Bgr2Gray);

            // 下采样图像以加速处理
            CvInvoke.Resize(grayImage, grayImage, new System.Drawing.Size(grayImage.Width / 2, grayImage.Height / 2), 0, 0, Inter.Linear);

            Mat sobelX = new Mat();
            Mat sobelY = new Mat();

            // 使用较低精度的数据类型
            CvInvoke.Sobel(grayImage, sobelX, DepthType.Cv32F, 1, 0, 3);
            CvInvoke.Sobel(grayImage, sobelY, DepthType.Cv32F, 0, 1, 3);

            // 手动计算梯度幅值
            Mat sobelXSquare = new Mat();
            Mat sobelYSquare = new Mat();
            CvInvoke.Multiply(sobelX, sobelX, sobelXSquare); // sobelX^2
            CvInvoke.Multiply(sobelY, sobelY, sobelYSquare); // sobelY^2

            Mat sobelMagnitude = new Mat();
            CvInvoke.Add(sobelXSquare, sobelYSquare, sobelMagnitude); // sobelX^2 + sobelY^2
            CvInvoke.Sqrt(sobelMagnitude, sobelMagnitude); // sqrt(sobelX^2 + sobelY^2)

            // 计算标准差
            var meanStdDev = new MCvScalar();
            var stddev = new MCvScalar();
            CvInvoke.MeanStdDev(sobelMagnitude, ref meanStdDev, ref stddev);

            double tenengradVar = stddev.V0 * stddev.V0;

            return tenengradVar;
        }

        public static double CalculateTenengrad2(Mat image)
        {
            if (image == null)
            {
                return 0;
            }
            /*if (!File.Exists(imagePath))
            {
                return 0;
            }

            Mat image = CvInvoke.Imread(imagePath, ImreadModes.Color);
            if (image.IsEmpty)
            {
                throw new Exception($"无法读取图像文件: {imagePath}");
            }*/

            Mat grayImage = new Mat();
            CvInvoke.CvtColor(image, grayImage, ColorConversion.Bgr2Gray);

            // 下采样图像以加速处理
            CvInvoke.Resize(grayImage, grayImage, new System.Drawing.Size(grayImage.Width / 2, grayImage.Height / 2), 0, 0, Inter.Linear);

            Mat sobelX = new Mat();
            Mat sobelY = new Mat();

            // 使用较低精度的数据类型
            CvInvoke.Sobel(grayImage, sobelX, DepthType.Cv32F, 1, 0, 3);
            CvInvoke.Sobel(grayImage, sobelY, DepthType.Cv32F, 0, 1, 3);

            // 手动计算梯度幅值
            Mat sobelXSquare = new Mat();
            Mat sobelYSquare = new Mat();
            CvInvoke.Multiply(sobelX, sobelX, sobelXSquare); // sobelX^2
            CvInvoke.Multiply(sobelY, sobelY, sobelYSquare); // sobelY^2

            Mat sobelMagnitude = new Mat();
            CvInvoke.Add(sobelXSquare, sobelYSquare, sobelMagnitude); // sobelX^2 + sobelY^2
            CvInvoke.Sqrt(sobelMagnitude, sobelMagnitude); // sqrt(sobelX^2 + sobelY^2)

            // 计算标准差
            var meanStdDev = new MCvScalar();
            var stddev = new MCvScalar();
            CvInvoke.MeanStdDev(sobelMagnitude, ref meanStdDev, ref stddev);

            double tenengradVar = stddev.V0 * stddev.V0;

            return tenengradVar;
        }

        public static double CalculateTenengrad3(WriteableBitmap writeableBitmap)
        {
            if (writeableBitmap == null)
            {
                return 0;
            }

            // 获取图像的宽度和高度
            var width = writeableBitmap.PixelWidth;
            var height = writeableBitmap.PixelHeight;

            // 创建一个 Mat，并填充 WriteableBitmap 的像素数据
            Mat image = new Mat(height, width, DepthType.Cv8U, 1); // 使用单通道（灰度）

            // 使用 CopyPixels 将 WriteableBitmap 的像素复制到 Mat 中
            writeableBitmap.CopyPixels(new Int32Rect(0, 0, width, height),
                                        image.DataPointer,
                                        image.Step,
                                        0);

            // 以下部分与原始方法相同
            Mat sobelX = new Mat();
            Mat sobelY = new Mat();
            CvInvoke.Sobel(image, sobelX, DepthType.Cv32F, 1, 0, 3);
            CvInvoke.Sobel(image, sobelY, DepthType.Cv32F, 0, 1, 3);

            Mat sobelXSquare = new Mat();
            Mat sobelYSquare = new Mat();
            CvInvoke.Multiply(sobelX, sobelX, sobelXSquare);
            CvInvoke.Multiply(sobelY, sobelY, sobelYSquare);

            Mat sobelMagnitude = new Mat();
            CvInvoke.Add(sobelXSquare, sobelYSquare, sobelMagnitude);
            CvInvoke.Sqrt(sobelMagnitude, sobelMagnitude);

            var meanStdDev = new MCvScalar();
            var stddev = new MCvScalar();
            CvInvoke.MeanStdDev(sobelMagnitude, ref meanStdDev, ref stddev);

            double tenengradVar = stddev.V0 * stddev.V0;

            return tenengradVar;
        }
        public static double CalculateTenengrad2(WriteableBitmap writeableBitmap)
        {
            if (writeableBitmap == null)
            {
                return 0;
            }

            var width = writeableBitmap.PixelWidth;
            var height = writeableBitmap.PixelHeight;

            // 检查宽度和高度是否大于零
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException("WriteableBitmap dimensions must be greater than zero.");
            }

            // 创建一个 Mat，并填充 WriteableBitmap 的像素数据
            Mat image = WriteableBitmapToMat(writeableBitmap);

            // 检查图像是否为空
            if (image.IsEmpty)
            {
                throw new InvalidOperationException("The image is empty after copying pixels.");
            }

            // 进行 Sobel 计算等后续处理
            Mat sobelX = new Mat();
            Mat sobelY = new Mat();
            CvInvoke.Sobel(image, sobelX, DepthType.Cv32F, 1, 0, 3);
            CvInvoke.Sobel(image, sobelY, DepthType.Cv32F, 0, 1, 3);

            Mat sobelXSquare = new Mat();
            Mat sobelYSquare = new Mat();
            CvInvoke.Multiply(sobelX, sobelX, sobelXSquare);
            CvInvoke.Multiply(sobelY, sobelY, sobelYSquare);

            Mat sobelMagnitude = new Mat();
            CvInvoke.Add(sobelXSquare, sobelYSquare, sobelMagnitude);
            CvInvoke.Sqrt(sobelMagnitude, sobelMagnitude);

            var meanStdDev = new MCvScalar();
            var stddev = new MCvScalar();
            CvInvoke.MeanStdDev(sobelMagnitude, ref meanStdDev, ref stddev);

            double tenengradVar = stddev.V0 * stddev.V0;

            return tenengradVar;
        }


        public static Mat WriteableBitmapToMat(WriteableBitmap writeableBitmap)
        {
            // 获取WriteableBitmap的宽度和高度
            int width = writeableBitmap.PixelWidth;
            int height = writeableBitmap.PixelHeight;

            // 创建Mat对象，单通道灰度图
            Mat mat = new Mat(height, width, DepthType.Cv8U, 1); // 1表示单通道

            // 获取像素数据
            writeableBitmap.Lock(); // 锁定写入位图以安全获取数据
            try
            {
                // 获取stride
                int stride = writeableBitmap.BackBufferStride;

                // 从BackBuffer获取数据
                // 计算字节数组的大小
                byte[] buffer = new byte[height * stride];
                Marshal.Copy(writeableBitmap.BackBuffer, buffer, 0, buffer.Length);

                // 将字节数组复制到Mat中
                Marshal.Copy(buffer, 0, mat.DataPointer, buffer.Length);
            }
            finally
            {
                writeableBitmap.Unlock(); // 解锁
            }

            return mat;
        }



        public static double CalculateTenengrad(Bitmap image)
        {
            // 将 System.Drawing.Image 转换为 Emgu.CV.Mat
            Mat matImage = BitmapToMat(image);

            if (matImage.IsEmpty)
            {
                throw new Exception("无法读取图像");
            }

            // 转换为灰度图像
            Mat grayImage = new Mat();
            CvInvoke.CvtColor(matImage, grayImage, ColorConversion.Bgr2Gray);

            // 计算 Sobel 算子
            Mat sobelX = new Mat();
            Mat sobelY = new Mat();
            CvInvoke.Sobel(grayImage, sobelX, DepthType.Cv64F, 1, 0, 3);
            CvInvoke.Sobel(grayImage, sobelY, DepthType.Cv64F, 0, 1, 3);

            // 计算梯度幅值: sqrt(sobelX^2 + sobelY^2)
            Mat sobelXSquare = new Mat();
            Mat sobelYSquare = new Mat();
            CvInvoke.Multiply(sobelX, sobelX, sobelXSquare); // sobelX^2
            CvInvoke.Multiply(sobelY, sobelY, sobelYSquare); // sobelY^2

            Mat sobelMagnitude = new Mat();
            CvInvoke.Add(sobelXSquare, sobelYSquare, sobelMagnitude); // sobelX^2 + sobelY^2
            CvInvoke.Sqrt(sobelMagnitude, sobelMagnitude); // sqrt(sobelX^2 + sobelY^2)

            // 计算方差
            var meanStdDev = new MCvScalar();
            var stddev = new MCvScalar();
            CvInvoke.MeanStdDev(sobelMagnitude, ref meanStdDev, ref stddev);

            double tenengradVar = stddev.V0 * stddev.V0; // 方差

            return tenengradVar;
        }

        public static void MainTest()
        {
            string imagePath = "Pic_2024_08_14_133452_2.jpg";  // 替换为你的图像路径
            /*double tenengradBlurriness = CalculateTenengrad(imagePath);
            Console.WriteLine($"图像的Tenengrad模糊度值: {tenengradBlurriness}");

            double threshold = 100.0;  // 根据实际情况调整
            if (tenengradBlurriness < threshold)
            {
                Console.WriteLine("图像是模糊的");
            }
            else
            {
                Console.WriteLine("图像是清晰的");
            }*/

        }
        // 辅助方法: 将 Bitmap 转换为 Mat
        private static Mat BitmapToMat(Bitmap bitmap)
        {
            // 确保图像格式是支持的格式
            if (bitmap.PixelFormat != PixelFormat.Format24bppRgb)
            {
                throw new NotSupportedException("只支持 24bpp RGB 格式的位图。");
            }

            // 创建 Mat 对象
            Mat mat = new Mat(bitmap.Height, bitmap.Width, DepthType.Cv8U, 3);

            // 锁定图像数据
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            try
            {
                // 获取图像数据的指针
                unsafe
                {
                    byte* src = (byte*)bitmapData.Scan0;
                    int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;

                    // 复制数据到 Mat
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        byte* dst = (byte*)mat.DataPointer + y * mat.Step;
                        for (int x = 0; x < bitmap.Width; x++)
                        {
                            // 24bpp RGB 格式: Blue, Green, Red
                            dst[3 * x] = src[3 * x];         // 蓝色通道
                            dst[3 * x + 1] = src[3 * x + 1]; // 绿色通道
                            dst[3 * x + 2] = src[3 * x + 2]; // 红色通道
                        }
                    }
                }
            }
            finally
            {
                // 确保解锁位图数据
                bitmap.UnlockBits(bitmapData);
            }

            return mat;
        }
    }
}
