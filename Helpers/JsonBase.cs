using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Extensions;
using 精密切割系统.Model.cut;

namespace 精密切割系统.Helpers
{
    internal abstract class JsonBase
    {
        private readonly string _configPath;

        private readonly IConfigurationRoot _configuration;

        protected JsonBase(string fileName)
        {
            _configPath = Path.Combine(AppContext.BaseDirectory, $"Assets\\config\\data\\{fileName}");
            _configuration = new ConfigurationBuilder().AddJsonFile(_configPath, optional: false, reloadOnChange: true).Build();
        }

        protected bool TryGetPoint(out DataPoint<float> point, [CallerMemberName] string? prefix = null)
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

        protected void UpdatePoint(DataPoint<float> point, [CallerMemberName] string? prefix = null)
        {
            UpdateAppSettings<float>(point.X, $"{prefix}:X");
            UpdateAppSettings<float>(point.Y, $"{prefix}:Y");
        }

        protected void UpdateAppSettings(bool value, [CallerMemberName] string? key = null)
        {
            if (key is null) return;
            UpdateAppSettingsToNull(key);
            _configuration.GetSection(key).Value = value.ToString();
            string jsonView = _configuration.ToJson();
            File.WriteAllText(_configPath, jsonView);
        }

        protected void UpdateAppSettings(string value, [CallerMemberName] string? key = null)
        {
            if (key is null) return;
            UpdateAppSettingsToNull(key);
            if (value is not null)
            {
                _configuration.GetSection(key).Value = value;
                string jsonView = _configuration.ToJson();
                File.WriteAllText(_configPath, jsonView);
            }
        }

        protected void UpdateAppSettings<T>(T value, [CallerMemberName] string? key = null) where T : struct, INumber<T>
        {
            if (key is null) return;
            UpdateAppSettingsToNull(key);
            _configuration.GetSection(key).Value = ToString(value);
            string jsonView = _configuration.ToJson();
            File.WriteAllText(_configPath, jsonView);
        }

        protected void UpdateAppSettings<T>(List<T> list, [CallerMemberName] string? key = null) where T : struct, INumber<T>
        {
            if (key is null) return;
            UpdateAppSettingsToNull(key);
            if (list is not null && list.Count != 0)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    _configuration.GetSection($"{key}:{i}").Value = ToString(list[i]);
                }
                string jsonView = _configuration.ToJson();
                File.WriteAllText(_configPath, jsonView);
            }
        }

        private string ToString<T>(T value) where T : struct, INumber<T>
        {
            if (typeof(T) == typeof(float) || typeof(T) == typeof(double) || typeof(T) == typeof(decimal))
            {
                return string.Format(CultureInfo.InvariantCulture, "{0:F3}", value);
            }
            return value.ToString() ?? string.Empty;
        }

        protected void UpdateAppSettingsToNull([CallerMemberName] string? key = null)
        {
            if (key is null) return;
            var section = _configuration.GetSection(key);
            section.Value = null;
            foreach (var child in section.GetChildren())
            {
                section.GetSection(child.Key).Value = null; // 清空子键值
            }
            string jsonView = _configuration.ToJson();
            File.WriteAllText(_configPath, jsonView);
        }

        protected string GetString([CallerMemberName] string? key = null)
        {
            ArgumentNullException.ThrowIfNull(key);
            string? value = _configuration.GetSection(key).Value;
            ArgumentNullException.ThrowIfNull(value);
            return value;
        }

        protected bool GetValue([CallerMemberName] string? key = null)
        {
            ArgumentNullException.ThrowIfNull(key);
            string? value = _configuration.GetSection(key).Value;
            ArgumentNullException.ThrowIfNull(value);
            return bool.Parse(value);
        }

        protected T GetValue<T>([CallerMemberName] string? key = null) where T : struct, INumber<T>
        {
            ArgumentNullException.ThrowIfNull(key);
            string? value = _configuration.GetSection(key).Value;
            ArgumentNullException.ThrowIfNull(value);
            return T.Parse(value, CultureInfo.CurrentCulture);
        }

        protected bool TryGetValue<T>(string key, out T outValue) where T : struct, INumber<T>
        {
            outValue = default;
            string? value = _configuration.GetSection(key).Value;
            if (value is null) return false;
            if (T.TryParse(value, CultureInfo.CurrentCulture, out T number))
            {
                outValue = number;
                return true;
            }
            return false;
        }

        protected List<T> GetList<T>([CallerMemberName] string? key = null) where T : struct, INumber<T>
        {
            List<T> list = new List<T>();
            if (key is null) return list;
            IConfigurationSection section = _configuration.GetSection(key);
            var count = section.GetChildren().Count();
            for (int i = 0; i < count; i++)
            {
                var value = GetValue<T>($"{key}:{i}");
                list.Add(value);
            }
            return list;
        }

        private JToken BuildJToken(IConfigurationSection section)
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