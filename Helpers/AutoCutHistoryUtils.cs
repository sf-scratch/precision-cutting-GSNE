using Microsoft.Xaml.Behaviors.Media;
using OpenCvSharp;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Entities;

namespace 精密切割系统.Helpers
{
    public class AutoCutHistoryUtils
    {
        private static readonly string ImageFolder = "AutoCutHistoryImages";
        private static KnifeWearEntity _knifeWearEntity;

        static AutoCutHistoryUtils()
        {
            _knifeWearEntity = new KnifeWearEntity();
        }

        public static void SetStartTime()
        {
            _knifeWearEntity = new KnifeWearEntity();
            _knifeWearEntity.StartTime = DateTime.Now;
        }

        public static void SetEndTime()
        {
            _knifeWearEntity.EndTime = DateTime.Now;
            SQLiteAsyncConnection connection = SqlHelper.SQLiteAsync;
            connection.InsertAsync(_knifeWearEntity);
        }

        public static void SetSharpen(float wearAmount, int sharpenCount)
        {
            _knifeWearEntity.WearAmount = wearAmount;
            _knifeWearEntity.SharpenCount = sharpenCount;
        }

        public static void SetLastSharpen(float wearAmount, int sharpenCount)
        {
            _knifeWearEntity.LastWearAmount = wearAmount;
            _knifeWearEntity.LastSharpenCount= sharpenCount;
        }

        public static void SetCutCount(int cutCount)
        {
            _knifeWearEntity.CutCount = cutCount;
        }

        public static void SetFirstCutImage(Mat mat)
        {
            _knifeWearEntity.FirstCutImage = SaveImage(mat);
        }

        public static void SetSecondCutImage(Mat mat)
        {
            _knifeWearEntity.SecondCutImage = SaveImage(mat);
        }

        public static void SetLastCutImage(Mat mat)
        {
            _knifeWearEntity.LastCutImage = SaveImage(mat);
        }

        private static string SaveImage(Mat mat)
        {
            string fileName = $"{DateTime.Now.Ticks}_{Guid.NewGuid()}.jpg";
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, ImageFolder));
            string filePath = Path.Combine(AppContext.BaseDirectory, ImageFolder, fileName);
            Cv2.ImWrite(filePath, mat);
            return Path.Combine(ImageFolder, fileName);
        }
    }
}
