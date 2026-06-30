using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using 精密切割系统.Driver;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace 精密切割系统.Helpers.GTN
{
    public class GsneConfig
    {
        private static readonly Lazy<GsneConfig> _lazy = new(GetGsneConfig);

        public static GsneConfig Instance
        {
            get { return _lazy.Value; }
        }

        public static GsneConfig GetGsneConfig()
        {
            string alarmConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets\\config\\GsneConfig.json");
            string allText = File.ReadAllText(alarmConfigPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,  // 可选：忽略大小写
                Converters = { new JsonStringEnumConverter() }  // 可选：处理枚举
            };
            var config = JsonSerializer.Deserialize<GsneConfig>(allText, options);
            if (config == null)
            {
                Tools.LogDebug("GsneConfig.json 反序列化失败，返回默认配置");
                return new GsneConfig();
            }
            List<AxisConfig> axisConfigs = [config.X, config.Y, config.Z1, config.Z2, config.Theta];
            config.Axes = axisConfigs.ToDictionary(p => p.Type, p => p);
            return config ?? new GsneConfig();
        }

        public short Core { get; set; }

        public AxisConfig X { get; set; }
        public AxisConfig Y { get; set; }
        public AxisConfig Z1 { get; set; }
        public AxisConfig Z2 { get; set; }
        public AxisConfig Theta { get; set; }

        public Dictionary<AxisType, AxisConfig> Axes { get; set; } 

        public string[] AxisStatusDescription { get; set; }

        public InputConfig Inputs { get; set; }

        public OutputConfig Outputs { get; set; }
    }
}