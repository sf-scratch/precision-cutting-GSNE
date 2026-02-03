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