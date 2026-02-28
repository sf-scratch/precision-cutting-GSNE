using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Helpers
{
    internal class ColorUtils
    {
        /// <summary>
        /// 解析十六进制颜色（如 "#FF0000" 或 "FF0000"）
        /// </summary>
        public static (byte r, byte g, byte b) ParseHexColor(string hex)
        {
            try
            {
                hex = hex.TrimStart('#');
                if (hex.Length == 6)
                {
                    byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                    byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                    byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                    return (r, g, b);
                }
            }
            catch { }
            // 默认返回黑色
            return (0, 0, 0);
        }
    }
}