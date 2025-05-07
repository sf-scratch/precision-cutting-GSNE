using Emgu.CV.Ocl;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;
using 精密切割系统.Model.cut;
using System.IO;
using System.Numerics;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace 精密切割系统.Helpers
{
    public static class Appsettings
    {
        private static readonly string _configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        public static readonly string RecordSharpenY = "SharpenRecord:RecordSharpenY";
        public static readonly string ThetaDegQueue = "SharpenRecord:ThetaDegQueue";

        public static IConfigurationRoot Configuration { get; }

        static Appsettings()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        }

        public static void UpdateAppSettings<T>(string key, T value) where T : struct, INumber<T>
        {
            Configuration.GetSection(key).Value = value.ToString();
            string jsonView = Configuration.ToJson();
            File.WriteAllText(_configPath, jsonView);
        }

        public static void UpdateAppSettings<T>(string key, List<T> list) where T : struct, INumber<T>
        {
            for (int i = 0; i < list.Count; i++)
            {
                Configuration.GetSection($"{key}:{i}").Value = list[i].ToString();
            }
            string jsonView = Configuration.ToJson();
            File.WriteAllText(_configPath, jsonView);
        }

        public static void UpdateAppSettingsToNull(string key)
        {
            Configuration.GetSection(key).Value = null;
            string jsonView = Configuration.ToJson();
            File.WriteAllText(_configPath, jsonView);
        }

        public static T? GetValue<T>(string key) where T : struct, INumber<T>
        {
            string? value = Configuration.GetSection(key).Value;
            if (value is null) return null;
            if (T.TryParse(value, CultureInfo.CurrentCulture, out T number))
            {
                return number;
            }
            return null;
        }

        public static List<T> GetList<T>(string key) where T : struct, INumber<T>
        {
            List<T> list = new List<T>();
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
