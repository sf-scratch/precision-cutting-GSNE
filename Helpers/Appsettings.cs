using Microsoft.Extensions.Configuration;
using 精密切割系统.Model.cut;
using System.IO;
using System.Numerics;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace 精密切割系统.Helpers
{
    public static class Appsettings
    {
        private static readonly string _configPath = Path.Combine(AppContext.BaseDirectory, "Assets\\config\\appsettings.json");

        public static IConfigurationRoot Configuration { get; }

        static Appsettings()
        {
            Configuration = new ConfigurationBuilder().AddJsonFile(_configPath, optional: false, reloadOnChange: true).Build();
        }

        /// <summary>
        /// 记录切割硅片theta角度
        /// </summary>
        public static List<float>? CutThetaDegList
        {
            get => GetList<float>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 记录磨刀板theta角度
        /// </summary>
        public static List<float>? SharpenThetaDegList
        {
            get => GetList<float>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 记录切割硅片Y轴位置
        /// </summary>
        public static float? CutY
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 记录磨刀板Y轴位置
        /// </summary>
        public static float? SharpenY
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 记录磨刀板已磨损的距离
        /// </summary>
        public static float? SharpenDistance
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 记录硅片已切割的距离
        /// </summary>
        public static float? CutDistance
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 自动聚焦位置Z1
        /// </summary>
        public static float? FocusClearZ
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 聚焦工作盘位置Z1
        /// </summary>
        public static float? FocusWorkpiecesClearZ
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 自更换刀片起刀片切了多长
        /// </summary>
        public static float? AfterReplaceBladeCutLength
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 自测高起刀片切了多长
        /// </summary>
        public static float? AfterMeasureHeightCutLength
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 自清空数据起刀片切了多长
        /// </summary>
        public static float? AfterClearDataCutLength
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 第一次测高位置
        /// </summary>
        public static float? MeasureHeightFirst
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 最后一次测高位置
        /// </summary>
        public static float? MeasureHeightLast
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 刀片外径
        /// </summary>
        public static float? BladeOuterDiameter
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 刀片厚度
        /// </summary>
        public static float? BladeThickness
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 轴心零点到工作盘距离
        /// </summary>
        public static float? AxisToWorkingDiscDistance
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 外加裕量
        /// </summary>
        public static float? AdditionalMargin
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 横向拉直行程
        /// </summary>
        public static float? HorizontalStraighteningStroke
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 竖向拉直行程
        /// </summary>
        public static float? VerticalStraighteningStroke
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        public static float? PositiveLimitPositionX
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        public static float? NegativeLimitPositionX
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        public static float? PositiveLimitPositionY
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        public static float? NegativeLimitPositionY
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        public static float? PositiveLimitPositionZ1
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        public static float? NegativeLimitPositionZ1
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        public static float? PositiveLimitPositionZ2
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        public static float? NegativeLimitPositionZ2
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        public static float? PositiveLimitPositionTheta
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        public static float? NegativeLimitPositionTheta
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        public static float? SafetyMarginZ1
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 自更换刀片起刀片切了几道
        /// </summary>
        public static int? AfterReplaceBladeCutTimes
        {
            get => GetValue<int>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 自更测高起刀片切了几道
        /// </summary>
        public static int? AfterMeasureHeightCutTimes
        {
            get => GetValue<int>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 自清空数据起刀片切了几道
        /// </summary>
        public static int? AfterClearDataCutTimes
        {
            get => GetValue<int>();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 是否需要检查基准线
        /// </summary>
        public static bool? IsNeedCheckBaseLine
        {
            get => GetValue();
            set => UpdateAppSettings(value);
        }

        /// <summary>
        /// 相机相对刀片中心点位置
        /// </summary>
        public static DataPoint<float> CameraRelativeBladePosition
        {
            get => TryGetPoint(out var point) ? point : new DataPoint<float>(0, 0);
            set => UpdatePoint(value);
        }

        /// <summary>
        /// 相机视野theta中心点位置
        /// </summary>
        public static DataPoint<float> CameraThetaCenterPoint
        {
            get => TryGetPoint(out var point) ? point : new DataPoint<float>(0, 0);
            set => UpdatePoint(value);
        }

        /// <summary>
        /// 切割theta中心点位置
        /// </summary>
        public static DataPoint<float> ThetaCenterPoint { get => new(CameraThetaCenterPoint.X.ToActualX(), CameraThetaCenterPoint.Y.ToActualY()); }

        /// <summary>
        /// 设备号
        /// </summary>
        public static string? DeviceCode
        {
            get => GetString();
            set => UpdateAppSettings(value);
        }

        private static bool TryGetPoint(out DataPoint<float> point, [CallerMemberName] string? prefix = null)
        {
            if (TryGetValue($"{prefix}:X", out float x) &&
                TryGetValue($"{prefix}:Y", out float y))
            {
                point = new(x, y);
                return true;
            }
            point = new DataPoint<float>(0, 0);
            return false;
        }

        private static void UpdatePoint(DataPoint<float> point, [CallerMemberName] string? prefix = null)
        {
            UpdateAppSettings<float>(point.X, $"{prefix}:X");
            UpdateAppSettings<float>(point.Y, $"{prefix}:Y");
        }

        public static void UpdateAppSettings(bool? value, [CallerMemberName] string? key = null)
        {
            if (key is null) return;
            UpdateAppSettingsToNull(key);
            if (value is not null)
            {
                Configuration.GetSection(key).Value = value.ToString();
                string jsonView = Configuration.ToJson();
                File.WriteAllText(_configPath, jsonView);
            }
        }

        public static void UpdateAppSettings(string? value, [CallerMemberName] string? key = null)
        {
            if (key is null) return;
            UpdateAppSettingsToNull(key);
            if (value is not null)
            {
                Configuration.GetSection(key).Value = value;
                string jsonView = Configuration.ToJson();
                File.WriteAllText(_configPath, jsonView);
            }
        }

        public static void UpdateAppSettings<T>(T? value, [CallerMemberName] string? key = null) where T : struct, INumber<T>
        {
            if (key is null) return;
            UpdateAppSettingsToNull(key);
            if (value is not null)
            {
                Configuration.GetSection(key).Value = ToString(value.Value);
                string jsonView = Configuration.ToJson();
                File.WriteAllText(_configPath, jsonView);
            }
        }

        public static void UpdateAppSettings<T>(List<T>? list, [CallerMemberName] string? key = null) where T : struct, INumber<T>
        {
            if (key is null) return;
            UpdateAppSettingsToNull(key);
            if (list is not null && list.Count != 0)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Configuration.GetSection($"{key}:{i}").Value = ToString(list[i]);
                }
                string jsonView = Configuration.ToJson();
                File.WriteAllText(_configPath, jsonView);
            }
        }

        private static string? ToString<T>(T value) where T : struct, INumber<T>
        {
            if (typeof(T) == typeof(float) || typeof(T) == typeof(double) || typeof(T) == typeof(decimal))
            {
                return string.Format(CultureInfo.InvariantCulture, "{0:F3}", value);
            }
            return value.ToString();
        }

        public static void UpdateAppSettingsToNull([CallerMemberName] string? key = null)
        {
            if (key is null) return;
            var section = Configuration.GetSection(key);
            section.Value = null;
            foreach (var child in section.GetChildren())
            {
                section.GetSection(child.Key).Value = null; // 清空子键值
            }
            string jsonView = Configuration.ToJson();
            File.WriteAllText(_configPath, jsonView);
        }

        public static string? GetString([CallerMemberName] string? key = null)
        {
            if (key is null) return null;
            return Configuration.GetSection(key).Value;
        }

        public static bool? GetValue([CallerMemberName] string? key = null)
        {
            if (key is null) return null;
            string? value = Configuration.GetSection(key).Value;
            if (value is null) return null;
            if (bool.TryParse(value, out bool res))
            {
                return res;
            }
            return null;
        }

        public static T? GetValue<T>([CallerMemberName] string? key = null) where T : struct, INumber<T>
        {
            if (key is null) return null;
            string? value = Configuration.GetSection(key).Value;
            if (value is null) return null;
            if (T.TryParse(value, CultureInfo.CurrentCulture, out T number))
            {
                return number;
            }
            return null;
        }

        public static bool TryGetValue<T>(string key, out T outValue) where T : struct, INumber<T>
        {
            outValue = default;
            string? value = Configuration.GetSection(key).Value;
            if (value is null) return false;
            if (T.TryParse(value, CultureInfo.CurrentCulture, out T number))
            {
                outValue = number;
                return true;
            }
            return false;
        }

        public static List<T> GetList<T>([CallerMemberName] string? key = null) where T : struct, INumber<T>
        {
            List<T> list = new List<T>();
            if (key is null) return list;
            IConfigurationSection section = Configuration.GetSection(key);
            var count = section.GetChildren().Count();
            for (int i = 0; i < count; i++)
            {
                var value = GetValue<T>($"{key}:{i}");
                if (value is not null) list.Add(value.Value);
            }
            return list;
        }

        private static string ToJson(this IConfiguration config)
        {
            JObject json = new JObject();
            foreach (var child in config.GetChildren())
            {
                json[child.Key] = BuildJToken(child);
            }
            return json.ToString(Formatting.Indented);
        }

        private static JToken BuildJToken(IConfigurationSection section)
        {
            if (section.GetChildren().Any())
            {
                JObject obj = new JObject();
                foreach (var child in section.GetChildren())
                {
                    obj[child.Key] = BuildJToken(child);
                }
                return obj;
            }
            return new JValue(section.Value);
        }
    }
}