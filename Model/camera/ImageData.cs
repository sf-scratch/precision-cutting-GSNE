using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace 精密切割系统.Model.camera
{
    public class ImageData
    {
        public byte[] BytesBitmap { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public ImageData(byte[] bytesBitmap, int width, int height)
        {
            BytesBitmap = bytesBitmap;
            Width = width;
            Height = height;
        }

        public WriteableBitmap ToWriteableBitmap()
        {
            int stride = Width;
            WriteableBitmap writeableBitmap = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Gray8, null);
            writeableBitmap.WritePixels(new System.Windows.Int32Rect(0, 0, Width, Height), BytesBitmap, stride, 0);
            return writeableBitmap;
        }

        public Mat ToMat(int channels = 3)
        {
            // 根据通道数确定 MatType
            MatType matType = channels switch
            {
                1 => MatType.CV_8UC1,  // 灰度图
                3 => MatType.CV_8UC3,  // BGR彩色图
                4 => MatType.CV_8UC4,  // BGRA带透明度
                _ => throw new ArgumentException($"不支持的通道数: {channels}")
            };

            // 创建 Mat 对象
            Mat mat = new Mat(Height, Width, matType);

            // 将 byte[] 数据复制到 Mat 中
            Marshal.Copy(BytesBitmap, 0, mat.Data, BytesBitmap.Length);

            return mat;
        }
    }
}