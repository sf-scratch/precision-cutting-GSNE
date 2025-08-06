using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace 精密切割系统.Helpers
{
    public static class FloatExtensions
    {
        public static bool NearlyEquals(this float a, float b, float epsilon = 1e-3f)
        {
            return Math.Abs(a - b) < epsilon;
        }

        public static bool NearlyEquals(this double a, double b, double epsilon = 1e-10)
        {
            return Math.Abs(a - b) < epsilon;
        }

        /// <summary>
        /// 将字符串转换为 float，转换失败时返回默认值。
        /// </summary>
        /// <param name="str">要转换的字符串</param>
        /// <param name="defaultValue">转换失败时的默认值（默认为 0f）</param>
        /// <param name="cultureInfo">文化信息（默认为 CultureInfo.InvariantCulture）</param>
        /// <returns>转换成功返回对应的 float 值，失败返回 defaultValue</returns>
        public static float ToFloat(this string str, float defaultValue = 0f, CultureInfo? cultureInfo = null)
        {
            if (string.IsNullOrWhiteSpace(str))
                return defaultValue;
            cultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;
            bool success = float.TryParse(
                str,
                NumberStyles.Float | NumberStyles.AllowThousands,
                cultureInfo,
                out float result
            );
            return success ? result : defaultValue;
        }

        /// <summary>
        /// 将字符串转换为 int，转换失败时返回默认值。
        /// </summary>
        /// <param name="str">要转换的字符串</param>
        /// <param name="defaultValue">转换失败时的默认值（默认为 0）</param>
        /// <param name="numberStyle">数字格式（默认为 NumberStyles.Integer）</param>
        /// <param name="cultureInfo">文化信息（默认为 CultureInfo.InvariantCulture）</param>
        /// <returns>转换成功返回对应的 int 值，失败返回 defaultValue</returns>
        public static int ToInt(this string str,
                              int defaultValue = 0,
                              NumberStyles numberStyle = NumberStyles.Integer,
                              CultureInfo? cultureInfo = null)
        {
            if (string.IsNullOrWhiteSpace(str))
                return defaultValue;
            cultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;
            bool success = int.TryParse(
                str,
                numberStyle,
                cultureInfo,
                out int result
            );
            return success ? result : defaultValue;
        }

        public static float ToActualX(this float cameraX)
        {
            return cameraX - Appsettings.CameraRelativeBladePosition.X;
        }

        public static float ToActualY(this float cameraY)
        {
            return cameraY - Appsettings.CameraRelativeBladePosition.Y;
        }

        public static float ToCameraX(this float actualX)
        {
            return actualX + Appsettings.CameraRelativeBladePosition.X;
        }

        public static float ToCameraY(this float actualY)
        {
            return actualY + Appsettings.CameraRelativeBladePosition.Y;
        }
    }
}
