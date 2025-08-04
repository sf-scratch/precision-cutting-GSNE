using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Extensions
{
    internal static class EnumExtension
    {
        // 获取枚举值的描述字符串的辅助方法
        public static string GetEnumDescription(this Enum value)
        {
            FieldInfo? field = value.GetType().GetField(value.ToString());
            if (field == null) return value.ToString();
            DescriptionAttribute[]? attribute = field.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
            if (attribute == null) return value.ToString();
            return attribute.Length > 0 ? attribute[0].Description : value.ToString();
        }
    }
}
