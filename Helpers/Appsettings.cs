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
            get { return GetList<float>(); }
            set
            {
                UpdateAppSettingsToNull();
                if (value is not null)
                {
                    UpdateAppSettings(value);
                }
            }
        }

        /// <summary>
        /// 记录切割硅片Y轴位置
        /// </summary>
        public static float? CutY
        {
            get { return GetValue<float>(); }
            set
            {
                UpdateAppSettingsToNull();
                if (value is not null)
                {
                    UpdateAppSettings(value.Value);
                }
            }
        }

        /// <summary>
        /// 记录磨刀板theta角度
        /// </summary>
        public static List<float>? SharpenThetaDegList
        {
            get { return GetList<float>(); }
            set
            {
                UpdateAppSettingsToNull();
                if (value is not null)
                {
                    UpdateAppSettings(value);
                }
            }
        }

        /// <summary>
        /// 记录磨刀板Y轴位置
        /// </summary>
        public static float? SharpenY
        {
            get { return GetValue<float>(); }
            set
            {
                UpdateAppSettingsToNull();
                if (value is not null)
                {
                    UpdateAppSettings(value.Value);
                }
            }
        }

        /// <summary>
        /// 记录磨刀板已磨损的距离
        /// </summary>
        public static float? SharpenDistance
        {
            get { return GetValue<float>(); }
            set
            {
                UpdateAppSettingsToNull();
                if (value is not null)
                {
                    UpdateAppSettings(value.Value);
                }
            }
        }

        /// <summary>
        /// 记录硅片已切割的距离
        /// </summary>
        public static float? CutDistance
        {
            get { return GetValue<float>(); }
            set
            {
                UpdateAppSettingsToNull();
                if (value is not null)
                {
                    UpdateAppSettings(value.Value);
                }
            }
        }

        /// <summary>
        /// 测高位置theta角度
        /// </summary>
        public static int? ContactHeightMeasurementThetaDeg
        {
            get { return GetValue<int>(); }
            set
            {
                UpdateAppSettingsToNull();
                if (value is not null)
                {
                    UpdateAppSettings(value.Value);
                }
            }
        }

        /// <summary>
        /// 自动聚焦位置Z1
        /// </summary>
        public static float? FocusClearZ
        {
            get { return GetValue<float>(); }
            set
            {
                UpdateAppSettingsToNull();
                if (value is not null)
                {
                    UpdateAppSettings(value.Value);
                }
            }
        }

        /// <summary>
        /// 自更换刀片起刀片切了几道
        /// </summary>
        public static int? AfterReplaceBladeCutTimes
        {
            get { return GetValue<int>(); }
            set
            {
                UpdateAppSettingsToNull();
                if (value is not null)
                {
                    UpdateAppSettings(value.Value);
                }
            }
        }

        /// <summary>
        /// 自更换刀片起刀片切了多长
        /// </summary>
        public static float? AfterReplaceBladeCutLength
        {
            get { return GetValue<float>(); }
            set
            {
                UpdateAppSettingsToNull();
                if (value is not null)
                {
                    UpdateAppSettings(value.Value);
                }
            }
        }

        /// <summary>
        /// 是否需要检查基准线
        /// </summary>
        public static bool? IsNeedCheckBaseLine
        {
            get { return GetValue(); }
            set
            {
                UpdateAppSettingsToNull();
                if (value is not null)
                {
                    UpdateAppSettings(value.Value);
                }
            }
        }

        /// <summary>
        /// 相机相对刀片中心点位置
        /// </summary>
        public static DataPoint<float> CameraRelativeBladePosition
        {
            get => TryGetPoint(nameof(CameraRelativeBladePosition), out var point) ? point : new DataPoint<float>(0, 0);
            set => UpdatePoint(nameof(CameraRelativeBladePosition), value);
        }

        /// <summary>
        /// 相机视野theta中心点位置
        /// </summary>
        public static DataPoint<float> CameraThetaCenterPoint
        {
            get => TryGetPoint(nameof(CameraThetaCenterPoint), out var point) ? point : new DataPoint<float>(0, 0);
            set => UpdatePoint(nameof(CameraThetaCenterPoint), value);
        }

        private static bool TryGetPoint(string prefix, out DataPoint<float> point)
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

        private static void UpdatePoint(string prefix, DataPoint<float> point)
        {
            UpdateAppSettings(point.X, $"{prefix}:X");
            UpdateAppSettings(point.Y, $"{prefix}:Y");
        }

        public static void UpdateAppSettings<T>(T value, [CallerMemberName] string? key = null) where T : struct, INumber<T>
        {
            if (key is null) return;
            Configuration.GetSection(key).Value = value.ToString();
            string jsonView = Configuration.ToJson();
            File.WriteAllText(_configPath, jsonView);
        }

        public static void UpdateAppSettings(bool value, [CallerMemberName] string? key = null)
        {
            if (key is null) return;
            Configuration.GetSection(key).Value = value.ToString();
            string jsonView = Configuration.ToJson();
            File.WriteAllText(_configPath, jsonView);
        }

        public static void UpdateAppSettings<T>(List<T> list, [CallerMemberName] string? key = null) where T : struct, INumber<T>
        {
            if (key is null) return;
            for (int i = 0; i < list.Count; i++)
            {
                Configuration.GetSection($"{key}:{i}").Value = list[i].ToString();
            }
            string jsonView = Configuration.ToJson();
            File.WriteAllText(_configPath, jsonView);
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
