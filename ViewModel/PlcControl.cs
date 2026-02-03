using MathNet.Numerics.LinearAlgebra;
using Newtonsoft.Json;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;
using 精密切割系统.Model.logs;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;

namespace 精密切割系统.ViewModel
{
    internal class PlcControl
    {
        private PlcControl()
        {
            if (GlobalParams.OnlineFlag)
            {
                //readTags = new Thread(updateAllTags);
                //readTags.IsBackground = true;
                //readTags.Start();
            }
        }

        static PlcControl()
        {
            ReadConfig();
        }

        private static readonly object _Object = new object();
        private static PlcControl? plcControl = null;

        public static PlcControl GetInstance()
        {
            lock (_Object)
            {
                if (plcControl == null)
                {
                    plcControl = new PlcControl();
                }
            }
            return plcControl;
        }

        public static KeyencePlc plc = KeyencePlc.Instance;

        // plc变量默认值是否初始化完成（tag中的default value属性会在连上plc的时候写入plc一次）
        public bool plcInit = false;

        // 循环更新所有使用到的plc变量的值，更新到tag的value属性中，如果要写入tag值，使用tag的writeValue属性
        private Thread readTags;

        // json配置文件中的结构反序列化，通过tagControl执行plc所有tag操作和运动控制
        public static PlcTags tagControl = new PlcTags();

        // 所有plc变量的字典，后台线程一直更新plc变量值，字典的key是配置文件中tag的name属性值，value是plc变量对象实例，实例中有plc地址和值等属性，方便其他地方读取使用
        public static Dictionary<string, Tag> allTags = new Dictionary<string, Tag>();

        // plc连接状态
        public static bool ConnectionStatus { get; set; } = false;

        // plc系统报警信息
        public static ObservableCollection<AlarmItem> allAlarm = new ObservableCollection<AlarmItem>();

        // PLC IO信息

        public static void ReadConfig()
        {
            // 获取相对于当前目录的配置文件路径
            // example PlcControl.tagControl.Xaxis.StartJog();
            string motionControlPath = Path.Combine(Tools.curPath, "Assets\\config\\MotionControl.json");

            // 检查配置文件是否存在
            if (File.Exists(motionControlPath))
            {
                string json = File.ReadAllText(motionControlPath);
                if (json != null)
                {
                    tagControl = JsonConvert.DeserializeObject<PlcTags>(json);
                }
                TraverseProperties(tagControl);
            }

            for (int i = 0; i < IOTags.ioTagsDI.Count; i++)
            {
                if (allTags.ContainsKey(IOTags.ioTagsDI[i].name))
                {
                    allTags[IOTags.ioTagsDI[i].name] = IOTags.ioTagsDI[i];
                    continue;
                }
                allTags.Add(IOTags.ioTagsDI[i].name, IOTags.ioTagsDI[i]);
            }

            for (int i = 0; i < IOTags.ioTagsDO.Count; i++)
            {
                if (allTags.ContainsKey(IOTags.ioTagsDO[i].name))
                {
                    allTags[IOTags.ioTagsDO[i].name] = IOTags.ioTagsDO[i];
                    continue;
                }
                allTags.Add(IOTags.ioTagsDO[i].name, IOTags.ioTagsDO[i]);
            }
            return;
        }

        private static void TraverseProperties(object? obj)
        {
            if (obj == null)
            {
                return;
            }
            Type type = obj.GetType();
            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                object propertyValue = property.GetValue(obj);
                Type propertyType = property.PropertyType;

                Console.WriteLine($"Property: {property.Name}, Value: {propertyValue}");

                // 如果属性值是一个类的实例，则递归遍历其属性
                if (propertyValue != null && !propertyType.IsPrimitive && propertyType != typeof(Tag))
                {
                    TraverseProperties(propertyValue);
                }
                else if (propertyValue != null && propertyType == typeof(Tag))
                {
                    Tag? tmpTag = propertyValue as Tag;
                    if (tmpTag != null && !allTags.ContainsKey(tmpTag.name))
                    {
                        allTags.Add(tmpTag.name, tmpTag);
                    }
                }
            }
        }

        public static async Task<float> GetCompensateAsync(Axis axis, float location)
        {
            // 获取补偿数据模型
            List<PositionCompensationModel> models = CurrentUtils.GetPositionCompensationModels();
            if (models == null || models.Count == 0)
            {
                return location;
            }
            float currLocation = await axis.GetCurrentLocationAsync() ?? 0;
            // 确定补偿方向
            PositionCompensationModel? axisModel;
            int directionType = location > currLocation ? 0 : 1;
            axisModel = models.Find(item => item.AxisType.Equals(axis.axisName + (directionType == 1 ? "-反向" : "")));
            // 无法找到对应轴的补偿信息
            if (axisModel == null)
            {
                return location;
            }
            float targetLocation = MathF.Round(location, GlobalParams.decimalPlaces);
            // 调用补偿计算函数
            float adjustedLocation = CalculateCompensation(axisModel, targetLocation, directionType);
            if (directionType == 0)
            {
                targetLocation -= adjustedLocation;
            }
            else if (directionType == 1)
            {
                targetLocation += adjustedLocation;
            }
            return targetLocation; // 返回调整后的目标位置，保留6位小数
        }

        /// <summary>
        /// 根据补偿模型计算目标位置
        /// </summary>
        /// <param name="axisModel">补偿模型</param>
        /// <param name="targetLocation">目标位置</param>
        /// <param name="directionType">方向类型：0-正向，1-反向</param>
        /// <returns>补偿后的目标位置</returns>
        public static float CalculateCompensation(PositionCompensationModel axisModel, float targetLocation, int directionType)
        {
            float compensation = 0;
            try
            {
                // 将位置和补偿数据解析为数组
                float[] positionNumbers = axisModel.AxisPosition.Split(",").Select(float.Parse).ToArray();
                float[] compensateNumbers = axisModel.AxisCompensate.Split(",").Select(float.Parse).ToArray();
                // 确保数据长度一致
                if (positionNumbers.Length != compensateNumbers.Length) return targetLocation;

                // 查找范围并进行线性插值
                for (int i = 0; i < positionNumbers.Length - 1; i++)
                {
                    if ((directionType == 0 && targetLocation <= positionNumbers[i]) ||
                        (directionType == 1 && targetLocation >= positionNumbers[i]))
                    {
                        // 判断i值，如果targetLocation == positionNumbers[i]  补偿开始就是i，如果是正向且是小于，则是补偿开始是i-1
                        int startIndex = -1;
                        int endIndex = -1;
                        if (targetLocation == positionNumbers[i])
                        {
                            startIndex = i;
                            endIndex = i;
                        }
                        else
                        {
                            startIndex = i - 1;
                            endIndex = i;
                        }
                        if (startIndex == endIndex)
                        {
                            float realPosition = compensateNumbers[startIndex];
                            float testPosition = positionNumbers[startIndex];
                            return (float)Math.Round(realPosition - testPosition, GlobalParams.decimalPlaces);
                        }
                        // 线性插值计算补偿量
                        float x1 = positionNumbers[startIndex];
                        float x2 = positionNumbers[endIndex];
                        float y1 = compensateNumbers[startIndex];
                        float y2 = compensateNumbers[endIndex];
                        if (y1 == 0 || y2 == 0)
                        {
                            break;
                        }
                        // 计算出2个位置之间的差
                        float positionComp = x2 - x1;
                        // 计算实际补偿量：插值计算出目标位置的补偿后位置
                        float interpolatedCompensate = y1 + (y2 - y1) * (targetLocation - x1) / (x2 - x1);

                        // 计算补偿量
                        compensation = interpolatedCompensate - targetLocation;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in compensation calculation: {ex.Message}");
            }

            return (float)Math.Round(compensation, GlobalParams.decimalPlaces);
        }

        /// <summary>
        /// 根据补偿模型计算目标位置
        /// </summary>
        /// <param name="axisModel">补偿模型</param>
        /// <param name="targetLocation">目标位置</param>
        /// <param name="directionType">方向类型：0-正向，1-反向</param>
        /// <returns>补偿后的目标位置</returns>
        private static float CalculateCompensation1(PositionCompensationModel axisModel, float targetLocation, int directionType)
        {
            float location = targetLocation;

            try
            {
                // 将位置和补偿数据解析为数组
                float[] positionNumbers = axisModel.AxisPosition.Split(",").Select(float.Parse).ToArray();
                float[] compensateNumbers = axisModel.AxisCompensate.Split(",").Select(float.Parse).ToArray();

                /*// 调用线性拟合方法
                var (a, b) = LinearRegression(positionNumbers, compensateNumbers);

                float compensatedPosition = GetCompensatedPosition(targetLocation, a, b);
                Debug.WriteLine($"输入位置: {targetLocation}, 补偿后光栅尺位置: {compensatedPosition}");

                // 将位置数据转为二维数组
                double[][] X = positionNumbers.Select(p => new double[] { (double)p }).ToArray();
                double[] y = compensateNumbers.Select(c => (double)c).ToArray();*/
                /*

                 // 设置高斯过程的参数（例如，核函数的尺度和噪声）
                 double sigmaF = 1.0;  // 核函数的尺度
                 double sigmaN = 1e-2; // 噪声方差

                 // 创建贝叶斯优化对象并执行优化
                 BayesianOptimization optimizer = new BayesianOptimization(X, y, sigmaF, sigmaN);
                 optimizer.Optimize(5);

                 // 传入位置9.2并预测补偿值
                 double predictedCompensation = optimizer.PredictCompensation(targetLocation);

                 Console.WriteLine($"old - 位置 {targetLocation}mm 的补偿值预测为: {predictedCompensation}");
                 */
                // 创建高斯过程优化器对象  89.9981  89.8940
                /*var optimizer_new = new GaussianProcessOptimizer(X, y);

                 // 进行超参数优化
                 optimizer_new.Optimize();

                 // 使用优化后的模型进行预测
                 double[] testPosition = new double[] { targetLocation };
                 double predictedCompensation_new = optimizer_new.Predict(testPosition);
                 Debug.WriteLine($"贝叶斯 - 预测位置 {targetLocation}mm 的补偿值为: {predictedCompensation_new}");*/

                // 确保数据长度一致
                if (positionNumbers.Length != compensateNumbers.Length) return location;

                // 查找范围并进行线性插值
                for (int i = 0; i < positionNumbers.Length - 1; i++)
                {
                    if ((directionType == 0 && targetLocation <= positionNumbers[i]) ||
                        (directionType == 1 && targetLocation >= positionNumbers[i]))
                    {
                        // 判断i值，如果targetLocation == positionNumbers[i]  补偿开始就是i，如果是正向且是小于，则是补偿开始是i-1
                        int startIndex = -1;
                        int endIndex = -1;
                        if (targetLocation == positionNumbers[i])
                        {
                            startIndex = i;
                            endIndex = i;
                        }
                        else
                        {
                            startIndex = i - 1;
                            endIndex = i;
                        }
                        if (startIndex == endIndex)
                        {
                            float realPosition = compensateNumbers[startIndex];
                            if (directionType == 0)
                            {
                                location = targetLocation + (targetLocation - realPosition);
                            }
                            else
                            {
                                location = targetLocation - (targetLocation - realPosition);
                            }

                            Debug.WriteLine($"0\t{targetLocation}\t{location}");
                            return (float)Math.Round(location, GlobalParams.decimalPlaces);
                        }
                        // 线性插值计算补偿量
                        float x1 = positionNumbers[startIndex];
                        float x2 = positionNumbers[endIndex];
                        float y1 = compensateNumbers[startIndex];
                        float y2 = compensateNumbers[endIndex];
                        if (y1 == 0 || y2 == 0)
                        {
                            break;
                        }
                        // 计算出2个位置之间的差
                        float positionComp = x2 - x1;
                        // 计算实际补偿量：插值计算出目标位置的补偿后位置
                        float interpolatedCompensate = y1 + (y2 - y1) * (targetLocation - x1) / (x2 - x1);

                        // 计算补偿量
                        float compensation = interpolatedCompensate - targetLocation;

                        // 根据补偿量调整目标位置
                        if (directionType == 0)
                        {
                            location = targetLocation - compensation;
                        }
                        else
                        {
                            location = targetLocation + compensation;
                        }
                        Debug.WriteLine($"1\t{targetLocation}\t{location}");
                        Debug.WriteLine($"线性 - 位置 {targetLocation}mm 的补偿值预测为: {location}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in compensation calculation: {ex.Message}");
            }

            return location;
        }

        public static float GetCompensation(float targetPosition, float[] positionNumbers, float[] compensateNumbers)
        {
            // 构建矩阵用于多项式拟合
            var n = positionNumbers.Length;
            var A = Matrix<double>.Build.Dense(n, 3, (i, j) => Math.Pow(positionNumbers[i], 2 - j));
            var b = Vector<double>.Build.DenseOfArray(compensateNumbers.Select(x => (double)x).ToArray());

            // 求解方程组以找到多项式系数
            var x = A.Solve(b);

            // 从向量中提取多项式系数
            double a = x[0];
            double b1 = x[1];  // 注意，这里重命名了变量以避免CS0128错误
            double c = x[2];

            // 使用多项式计算在目标位置的误差
            double errorAtTarget = a * Math.Pow(targetPosition, 2) + b1 * targetPosition + c;

            // 计算补偿值
            return (float)(targetPosition - errorAtTarget);
        }

        /// <summary>
        /// 根据拟合结果计算补偿后的光栅尺位置
        /// </summary>
        public static float GetCompensatedPosition(float position, float a, float b)
        {
            return a * position + b;
        }

        public static (float a, float b) LinearRegression(float[] x, float[] y)
        {
            if (x.Length != y.Length || x.Length == 0)
            {
                throw new ArgumentException("x 和 y 的数量必须相同，且不能为 0！");
            }

            int n = x.Length;
            float sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

            for (int i = 0; i < n; i++)
            {
                sumX += x[i];
                sumY += y[i];
                sumXY += x[i] * y[i];
                sumX2 += x[i] * x[i];
            }

            // 计算斜率 a 和截距 b
            float a = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            float b = (sumY - a * sumX) / n;

            return (a, b);
        }
    }
}