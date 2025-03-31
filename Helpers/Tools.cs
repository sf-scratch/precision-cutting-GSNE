using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.ViewModel;

namespace 精密切割系统.Utils
{
    internal class Tools
    {
        public static ILog log = LogManager.GetLogger(typeof(Tools));
        //public static string curPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string curPath = AppDomain.CurrentDomain.BaseDirectory;

        public static void LogInfo(string msg)
        {
            log.Info(msg);
        }

        public static void LogError(string msg)
        {
            log.Error(msg);
        }

        public static void LogWarning(string msg)
        {
            log.Warn(msg);
        }
        // 把路径转换为BitmapImage
        public static BitmapImage BitmapImageToBitmap(string path)
        {
            BitmapImage bitmapImage = new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
            return bitmapImage;
        }
        /// <summary>
        /// 尝试将字符串转换为 decimal。
        /// </summary>
        /// <param name="input">要转换的字符串</param>
        /// <param name="result">转换后的 decimal 值</param>
        /// <returns>如果转换成功，返回 true；否则返回 false。</returns>
        public static bool TryParseDecimal(string input, out decimal result)
        {
            return decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }
        private static readonly char[] Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
        private static readonly Random Random = new Random();

        public static string GenerateRandomString(int length)
        {
            if (length <= 0)
                throw new ArgumentException("Length must be greater than zero.");

            char[] randomString = new char[length];
            for (int i = 0; i < length; i++)
            {
                randomString[i] = Characters[Random.Next(Characters.Length)];
            }
            return new string(randomString);
        }
        public static int[] StringToIntegerArray(string str)
        {
            int[] array = new int[str.Length];
            for (int i = 0; i < str.Length; i++)
            {
                // 将字符转换为整数
                array[i] = str[i] - '0';
            }
            return array;
        }
        public static string[] StringToStringArray(string str)
        {
            // 将字符串按逗号分割成字符串数组
            return str.Split(',');
        }
        public static float[] StringToFloatArray(string str)
        {
            // 将字符串按逗号分割成字符串数组
            string[] stringArray = str.Split(',');

            // 创建一个float数组，长度与分割后的字符串数组相同
            float[] floatArray = new float[stringArray.Length];

            // 将每个字符串元素转换为float
            for (int i = 0; i < stringArray.Length; i++)
            {
                if (stringArray[i].Trim() != "")
                {
                    floatArray[i] = float.Parse(stringArray[i]);
                } else
                {
                    floatArray[i] = 0.0f;
                }
            }

            return floatArray;
        }

        // 宽和高的像素
        public static int widthImg = 2448;
        public static int heightImg = 2048;
        // 宽和高的实际长度，单位微米
        public static int widthAct = 850;
        public static int heightAct = 710;
        // 根据起止位置计算实际长度
        public static float GetActualLength(int start, int end, bool isHeight = true)
        {
            float res;
            if (isHeight)
            {
                res = Math.Abs(end - start - 1) / heightImg * heightAct;
            }
            else
            {
                res = Math.Abs(end - start - 1) / widthImg * widthAct;
            }
            return res;
        }

        /// <summary>
        /// 获取符合类型的元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static List<T> GetChildrenOfType<T>(DependencyObject parent) where T : DependencyObject
        {
            List<T> children = new List<T>();

            // 递归遍历视觉树
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    children.Add(typedChild);
                }

                // 递归调用
                children.AddRange(GetChildrenOfType<T>(child));
            }

            return children;
        }

        public static List<T> GetChildrenOfTypeNew<T>(DependencyObject parent) where T : DependencyObject
        {
            List<T> children = new List<T>();
            Stack<DependencyObject> stack = new Stack<DependencyObject>();
            stack.Push(parent);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (current is T typedChild)
                {
                    children.Add(typedChild);
                }

                int childCount = VisualTreeHelper.GetChildrenCount(current);
                for (int i = 0; i < childCount; i++)
                {
                    stack.Push(VisualTreeHelper.GetChild(current, i));
                }
            }

            return children;
        }


        public static T GetChildObject<T>(DependencyObject parentObj, string name) where T : FrameworkElement
        {
            DependencyObject child = null;
            T grandChild = null;

            for (int i = 0; i <= VisualTreeHelper.GetChildrenCount(parentObj) - 1; i++)
            {
                child = VisualTreeHelper.GetChild(parentObj, i);

                if (child is T && (((T)child).Name == name | string.IsNullOrEmpty(name)))
                {
                    return (T)child;
                }
                else
                {
                    grandChild = GetChildObject<T>(child, name);
                    if (grandChild != null)
                        return grandChild;
                }
            }
            return null;
        }

        /// <summary>
        /// 等待tag的值达到目标值，timeout和interval单位为秒，超时后会返回false
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="eValue"></param>
        /// <param name="timeout"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        public static bool WaitForValue(Tag tag, string eValue, double timeout=10, double interval = 0)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed.TotalSeconds < timeout)
            {
                Task.Delay(100);
                String runValue = PlcControl.plc.GetPlcValueString(tag.name);
                if (runValue != eValue && Math.Abs(float.Parse(eValue) - float.Parse(runValue)) > 0.001)
                {
                    if (interval == 0)
                    {
                        continue;
                    }
                    else
                    {
                        Thread.Sleep(Convert.ToInt32(interval));
                    }
                }
                else
                {
                    stopwatch.Stop();
                    return true;
                }
            }
            stopwatch.Stop();
            return false;
        }
        /// <summary>
        /// 等待tag的值达到目标值，timeout和interval单位为秒，超时后会返回false
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="type">1 true 0 false</param>
        /// <returns></returns>
        public static bool WaitForValue(string tagName, int type, double timeout = 60)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            string value1 = type == 0 ? "0" : "1";
            string value2 = type == 0 ? "False" : "True";
            while (stopwatch.IsRunning)
            {
                if (timeout > 0 && stopwatch.Elapsed.TotalSeconds > timeout)
                {
                    stopwatch.Stop();
                    return false;
                }
                Task.Delay(100);
                String runValue = PlcControl.plc.GetPlcValueString(tagName);
                if (!value1.Equals(runValue) && !value2.Equals(runValue) )
                {
                    continue;
                }
                else
                {
                    stopwatch.Stop();
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 判断plc的value 是不是true
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TrueFlag(string value)
        {
            return "1".Equals(value) || "True".Equals(value);
        }


        public static int GetIntStringValue(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                // 尝试将字符串转换为双精度浮点数
                if (double.TryParse(value, out double doubleValue) && doubleValue > 0)
                {
                    // 将双精度浮点数转换为整数
                    return (int)doubleValue;
                }
            }
            return 0;
        }
        public static double GetDoubleStringValue(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (double.TryParse(value, out double moCutOneNo))
                {
                    return moCutOneNo;
                }
            }
            return 0;
        }
        public static float GetFloatStringValue(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (float.TryParse(value, out float moCutOneNo))
                {
                    return moCutOneNo;
                }
            }
            return 0;
        }
        public static string FormatDecimalString(string inputString, int decimalPlaces)
        {
            // 如果输入字符串为空或包含非法字符，直接返回原字符串
            if (string.IsNullOrEmpty(inputString) || !IsValidNumber(inputString))
            {
                return inputString;
            }

            // 如果输入字符串长度大于1，去掉前面的零
            if (inputString.Length > 1)
            {
                inputString = inputString.TrimStart('0');
            }

            // 如果去掉零后字符串为空（表示原始字符串是 "0" 或类似格式），则设置为 "0"
            if (string.IsNullOrEmpty(inputString))
            {
                return "0." + new string('0', decimalPlaces);
            }

            // 找到小数点的位置
            int dotIndex = inputString.IndexOf('.');

            if (dotIndex == -1)
            {
                // 如果没有小数点，直接在末尾加上小数点和足够的零
                if (decimalPlaces == 0)
                {
                    return inputString;
                } else
                {
                    return inputString + "." + new string('0', decimalPlaces);
                }
            }

            // 截取小数点前后的部分
            string integerPart = inputString.Substring(0, dotIndex);
            string decimalPart = inputString.Substring(dotIndex + 1);

            if (decimalPart.Length > decimalPlaces)
            {
                // 如果小数部分超过了指定的位数，则截取
                decimalPart = decimalPart.Substring(0, decimalPlaces);
            }
            else if (decimalPart.Length < decimalPlaces)
            {
                // 如果小数部分不足，则补充0
                decimalPart = decimalPart.PadRight(decimalPlaces, '0');
            }

            // 如果整数部分为空（意味着原输入是 "0.00" 这种格式），则确保整数部分为 "0"
            if (string.IsNullOrEmpty(integerPart))
            {
                integerPart = "0";
            }

            // 如果小数部分为 "0"，则只返回整数部分
            if (decimalPart == new string('0', decimalPlaces) && decimalPlaces == 0)
            {
                return integerPart;
            }

            // 如果小数部分不为 "0"，则返回整数部分 + 小数部分
            if (decimalPlaces == 0)
            {
                return integerPart;
            }

            return integerPart + "." + decimalPart;
        }

        public static string FormatDecimalString1(string inputString, int decimalPlaces)
        {
            
            // 如果输入字符串为空或包含非法字符，直接返回原字符串
            if (string.IsNullOrEmpty(inputString) || !IsValidNumber(inputString))
            {
                return inputString;
            }
            if (inputString.Length > 1)
            {
                // 去掉字符串前面的零
                inputString = inputString.TrimStart('0');
            }

            // 如果去掉零后字符串为空（表示原始字符串是 "0" 或类似格式），则设置为 "0"
            if (string.IsNullOrEmpty(inputString))
            {
                if (decimalPlaces == 0)
                {
                    return "0";
                }
                return "0." + new string('0', decimalPlaces);
            }

            // 找到小数点的位置
            int dotIndex = inputString.IndexOf('.');

            if (dotIndex == -1)
            {
                if (decimalPlaces == 0)
                {
                    return inputString;
                }
                // 如果没有小数点，直接在末尾加上小数点和足够的零
                return inputString + "." + new string('0', decimalPlaces);
            }

            // 截取小数点前后的部分
            string integerPart = inputString.Substring(0, dotIndex);
            string decimalPart = inputString.Substring(dotIndex + 1);

            if (decimalPart.Length > decimalPlaces)
            {
                // 如果小数部分超过了指定的位数，则截取
                decimalPart = decimalPart.Substring(0, decimalPlaces);
            }
            else if (decimalPart.Length < decimalPlaces)
            {
                // 如果小数部分不足，则补充0
                decimalPart = decimalPart.PadRight(decimalPlaces, '0');
            }

            // 如果整数部分为空（意味着原输入是 "0.00" 这种格式），则确保整数部分为 "0"
            if (string.IsNullOrEmpty(integerPart))
            {
                integerPart = "0";
            }

            // 返回最终格式化的数字字符串  
            if (decimalPlaces == 0) {
                return integerPart;
            }
            // 返回最终格式化的数字字符串
            return integerPart + "." + decimalPart;
        }

        // 判断字符串是否为有效数字
        static bool IsValidNumber(string inputString)
        {
            // 尝试解析字符串为数字，如果成功返回 true，失败则返回 false
            return decimal.TryParse(inputString, out _);
        }
        /// <summary>
        /// 等待状态完成后，执行某些操作，传入一个状态key和方法
        /// </summary>
        /// <param name="statusKey">状态的唯一标识</param>
        /// <param name="onFinish">状态完成后执行的方法</param>
        /// <param name="timeoutSeconds">最大等待时间，单位毫秒</param>
        /// <returns>是否成功完成任务</returns>
        public static bool AwaitForStatusFinishAsync(
            string statusKey,
            Action<bool> onFinish,
            int timeoutSeconds = 10) // 默认超时30秒
        {
            try
            {
                Task.Run(() =>
                {
                    // 监测状态完成的任务 这是一个同步任务
                    bool flag = Tools.WaitForValue(statusKey, 1, timeoutSeconds);
                    Thread.Sleep(500);
                    if (flag)
                    {
                        // 使用 Dispatcher 确保回调在主线程执行
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            onFinish?.Invoke(flag);
                        });
                    } else
                    {
                        MaterialSnackUtils.MaterialSnack("执行超时！", MaterialSnackUtils.SnackType.WARNING, 5000);
                        // 如果超时，则不执行回调方法
                        GlobalParams.globalRunFlag = false;
                    }

                });
                return true;
            }
            catch (Exception ex)
            {
                // 捕获异常并记录
                Console.WriteLine($"发生错误: {ex.Message}");
                return false;
            }
            finally
            {
                
            }
        }

        /// <summary>
        /// 将字符串写入文件，每次写入都换行。如果文件不存在，则创建文件。
        /// </summary>
        /// <param name="content">要写入的字符串内容。</param>
        /// <param name="fileName">文件名（包括路径）。</param>
        public static void WriteLineToFile(string content, string fileName)
        {
            try
            {
                // 检查文件是否存在
                if (!File.Exists(fileName))
                {
                    // 创建文件
                    using (FileStream fs = File.Create(fileName))
                    {
                        // 可选择在创建时写入文件头或初始内容
                    }
                }

                // 追加写入内容，并在末尾换行
                using (FileStream fs = new FileStream(fileName, FileMode.Append, FileAccess.Write))
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    writer.WriteLine(content); // 写入并自动换行
                }
            }
            catch (Exception ex)
            {
                Tools.LogInfo($"写入文件时发生错误: {ex.Message}");
            }
        }
        /// <summary>
        /// 删除指定路径的文件。
        /// </summary>
        /// <param name="fileName">文件的完整路径。</param>
        /// <returns>如果删除成功返回 true，否则返回 false。</returns>
        public static bool DeleteFile(string fileName)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName); // 删除文件
                    Tools.LogInfo($"文件 {fileName} 已成功删除。");
                    return true;
                }
                else
                {
                    Tools.LogInfo($"文件 {fileName} 不存在。");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Tools.LogInfo($"删除文件时发生错误: {ex.Message}");
                return false;
            }
        }

        public static decimal GetDecimalStringValue(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (decimal.TryParse(value, out decimal moCutOneNo))
                {
                    return moCutOneNo;
                }
            }
            return 0;
        }
        /// <summary>
        /// 根据光栅尺测量值和对应的激光测量值，计算当前位置绝对运动和相对运动需要的目标光栅尺位置，单位均为mm，保留4位小数
        /// </summary>
        /// <param name="measure">光栅尺显示位置数组，每个点步进为0.3mm, 递减</param>
        /// <param name="actual">激光测量位置数组，和光栅尺显示位置数组一一对应</param>
        /// <param name="curPosition">当前显示的光栅尺位置</param>
        /// <param name="targetDistance">相对运动距离, 往减小方向移动的时候，距离值为负数</param>
        /// <returns>目标光栅尺位置</returns>
        public static double GetActualDistance(double[] measure, double[] actual, double curPosition, double targetDistance = 0)
        {
            double targetGratingPosition = 0;
            if (targetDistance == 0)
            {
                return targetGratingPosition;
            }
            // 计算每个点的激光和光栅误差
            double[] misTakes = new double[measure.Length];
            for (int i = 0; i < measure.Length; i++)
            {
                misTakes[i] = actual[i] - measure[i];
            }
            double curMistake = 0;
            double targetMistake = 0;
            // 计算当前位置激光和光栅的误差
            for (int i = 0; i < measure.Count() - 1; i++)
            {
                if (measure[i] > curPosition && curPosition >= measure[i + 1])
                {
                    curMistake = misTakes[i] + (curPosition - measure[i]) / (measure[i + 1] - measure[i]) * (misTakes[i + 1] - misTakes[i]);
                    break;
                }
            }
            // 计算目标位置激光和光栅的误差
            if (targetDistance != 0)
            {
                double tp = curPosition + targetDistance;
                for (int i = 0; i < measure.Count() - 1; i++)
                {
                    if (measure[i] > tp && tp >= measure[i + 1])
                    {
                        targetMistake = misTakes[i] + (tp - measure[i]) / (measure[i + 1] - measure[i]) * (misTakes[i + 1] - misTakes[i]);
                        break;
                    }
                }
            }
            // 根据误差差值计算实际目标光栅位置
            targetGratingPosition = curPosition + targetDistance + (targetMistake - curMistake);
            return targetGratingPosition;
        }

        /// <summary>
        /// 根据光栅尺测量值和对应的激光测量值，计算当前位置绝对运动和相对运动需要的目标光栅尺位置，单位均为mm，保留4位小数
        /// </summary>
        /// <param name="measure">光栅尺显示位置数组，对应每个激光测量点的光栅尺显示位置</param>
        /// <param name="actual">激光测量位置数组，每个点步进为0.3mm, 递减</param>
        /// <param name="curPosition">当前显示的光栅尺位置</param>
        /// <param name="targetDistance">相对运动距离, 往减小方向移动的时候，距离值为负数</param>
        /// <returns>目标光栅尺位置</returns>
        public static double GetActualDistance2(double[] measure, double[] actual, double curPosition, double targetDistance = 0)
        {
            double targetGratingPosition = 0;
            if (targetDistance == 0)
            {
                return targetGratingPosition;
            }
            // 计算每个点的激光和光栅误差：单步误差
            double[] misTakes = new double[measure.Length];
            for (int i = 0; i < measure.Length; i++)
            {
                misTakes[i] = actual[i] - measure[i];
            }

            /*// 计算每个点的激光和光栅误差：累计误差
            float[] totalMisTakes = new float[measure.Length];
            totalMisTakes[0] = misTakes[0];
            for (int i = 1; i < measure.Length; i++)
            {
                totalMisTakes[i] = totalMisTakes[i-1] + actual[i] - measure[i];
            }*/

            // 起点
            int startIndex = 0;
            // 目标点
            int endIndex = 0;
            // 目标位置
            double tp = curPosition + targetDistance;
            // 计算当前位置激光和光栅的误差
            for (int i = 0; i < measure.Count() - 1; i++)
            {
                if (measure[i] > curPosition && curPosition >= measure[i + 1])
                {
                    startIndex = i;
                }
                if (measure[i] > tp && tp >= measure[i + 1])
                {
                    endIndex = i;
                }
            }

            double totalMistake = 0;
            // 计算起点到目标位置的累计误差
            for (int i = startIndex; i < endIndex; i++)
            {
                totalMistake += misTakes[i];
            }
            // 根据累计误差计算实际目标光栅位置
            targetGratingPosition = curPosition + targetDistance + totalMistake;
            return targetGratingPosition;
        }
    }
}
