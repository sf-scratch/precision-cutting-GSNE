using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Extensions
{
    public static class ConfigurationExtensions
    {
        public static string ToJson(this IConfiguration config)
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