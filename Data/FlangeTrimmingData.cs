using Emgu.CV.Ocl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;

namespace 精密切割系统.Data
{
    internal class FlangeTrimmingData : JsonBase
    {
        private static readonly Lazy<FlangeTrimmingData> _lazy = new(() => new FlangeTrimmingData());

        public static FlangeTrimmingData Instance => _lazy.Value;

        private FlangeTrimmingData() : base("Assets\\config\\data\\flangeTrimming.json")
        {
        }

        public float XCenterPosition
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        public float YCenterPosition
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        public float ZCenterPosition
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        //主轴转数/min
        public int SpindleRev
        {
            get => GetValue<int>();
            set => UpdateAppSettings(value);
        }

        //x轴行程（mm）
        public float XAxisTravel
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        //进刀尺寸
        public float YStepDistance
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        //进刀速度
        public float CutSpeed
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        // 重复次数
        public int RepeatCount
        {
            get => GetValue<int>();
            set => UpdateAppSettings(value);
        }

        // 无火花研磨次数
        public int SparkFreeStep
        {
            get => GetValue<int>();
            set => UpdateAppSettings(value);
        }

        public float XLowSpeed
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        public float YLowSpeed
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }
    }
}