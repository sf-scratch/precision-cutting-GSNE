using MathNet.Numerics.LinearAlgebra;
using Newtonsoft.Json;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Model.logs;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;

namespace 精密切割系统.ViewModel
{
    class PlcControl
    {
        private PlcControl()
        {
            readConfig();
            if (GlobalParams.onlineFlag)
            {
                readTags = new Thread(updateAllTags);
                readTags.IsBackground = true;
                readTags.Start();
            }
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
        float epsilon = 0.00001f; // 一个小的容差值
        public static string DefaultPlcIP = "192.168.10.10";
        public static KeyencePlc plc = KeyencePlc.GetInstance(DefaultPlcIP);
        // plc变量默认值是否初始化完成（tag中的default value属性会在连上plc的时候写入plc一次）
        public bool plcInit = false;
        // 循环更新所有使用到的plc变量的值，更新到tag的value属性中，如果要写入tag值，使用tag的writeValue属性
        private Thread readTags;

        // json配置文件中的结构反序列化，通过tagControl执行plc所有tag操作和运动控制
        public static PlcTags tagControl = new PlcTags();

        // 所有plc变量的字典，后台线程一直更新plc变量值，字典的key是配置文件中tag的name属性值，value是plc变量对象实例，实例中有plc地址和值等属性，方便其他地方读取使用
        public static Dictionary<string, Tag> allTags = new Dictionary<string, Tag>();

        // plc连接状态
        public static bool connectionStatus = false;

        // plc系统报警信息
        public static ObservableCollection<AlarmItem> allAlarm = new ObservableCollection<AlarmItem>();

        // PLC IO信息

        public void readConfig()
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

        private void updateAllTags()
        {
            AlarmItem connectPlc = new AlarmItem("连接plc失败");
            connectPlc.desc = "连接plc失败,请检查plc状态";
            List<string> tagKeys = new List<string>();
            tagKeys.AddRange(allTags.Keys);
            while (true)
            {
                if (connectionStatus)
                {
                    if (allAlarm.Contains(connectPlc))
                    {
                        allAlarm.Remove(connectPlc);
                    }
                    try
                    {
                        // plc变量默认值初始化写入一次
                        if (!plcInit)
                        {
                            foreach (string tKey in tagKeys)
                            {
                                if (allTags[tKey].defaultValue != "")
                                {
                                    allTags[tKey].writeValue = allTags[tKey].defaultValue;
                                    plc.writeTag(allTags[tKey]);
                                }
                            }
                            plcInit = true;
                        }
                        // plc变量循环读取和更新
                        foreach (string tKey in tagKeys)
                        {
                            plc.readTag(allTags[tKey]);
                        }
                        // 报警信息更新
                        UpdateAlarm();
                        if (allAlarm.Count > 0)
                        {
                            Helpers.MaterialSnackUtils.MaterialSnack(allAlarm[0].desc, Helpers.MaterialSnackUtils.SnackType.ERROR);
                        }
                    }
                    catch (Exception ex)
                    {
                        //sysMessage = ex.Message;
                    }
                }
                else
                {
                    connectPlc.startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    if (!allAlarm.Contains(connectPlc))
                    {
                        allAlarm.Add(connectPlc);
                    }
                }
                Thread.Sleep(500);
            }
        }

        private void UpdateAlarm()
        {
            // 更新新增报警
            foreach (var key in allTags.Keys)
            {
                var tag = allTags[key];
                if (tag.value != null && tag.value != "")
                {
                    AlarmItem alarmTmp = new AlarmItem();
                    alarmTmp.title = tag.name;
                    // 硬件传感器报警
                    if ("DM1010".Equals(tag.addr) && alarmTmp.deviceAlarm.ContainsKey(tag.value))
                    {
                        tag.errorStatus = true;
                        if (!allAlarm.Contains(alarmTmp))
                        {
                            alarmTmp.alarmTag = tag;
                            alarmTmp.startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            alarmTmp.alarmPriority = tag.alarmPriority;
                            alarmTmp.alarmTag = tag;
                            alarmTmp.desc = alarmTmp.deviceAlarm[tag.value];
                            allAlarm.Add(alarmTmp);
                            continue;
                        }
                    }
                    if ("DM1000".Equals(tag.addr) && alarmTmp.axisAlarm.ContainsKey(tag.value) && tag.invalidValue != null && tag.invalidValue.Contains(tag.value))
                    {
                        tag.errorStatus = true;
                        if (!allAlarm.Contains(alarmTmp))
                        {
                            alarmTmp.alarmTag = tag;
                            alarmTmp.startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            alarmTmp.alarmPriority = tag.alarmPriority;
                            alarmTmp.alarmTag = tag;
                            alarmTmp.desc = alarmTmp.axisAlarm[tag.value];
                            allAlarm.Add(alarmTmp);
                            continue;
                        }
                    }
                    if (tag != null && !"read data error".Equals(tag.value))
                    {
                        if (tag.upperValue != "" && tag.value != null && float.Parse(tag.value) > float.Parse(tag.upperValue))
                        {
                            tag.errorStatus = true;
                        }
                        else if (tag.lowerValue != "" && tag.value != null && float.Parse(tag.value) < float.Parse(tag.lowerValue))
                        {
                            tag.errorStatus = true;
                        }
                        else if (tag.validValue != null && !tag.validValue.Contains(tag.value))
                        {
                            tag.errorStatus = true;
                        }
                        else if (tag.invalidValue != null && tag.invalidValue.Contains(tag.value))
                        {
                            tag.errorStatus = true;
                        }
                        if (tag.errorStatus)
                        {
                            if (!allAlarm.Contains(alarmTmp))
                            {
                                alarmTmp.alarmTag = tag;
                                alarmTmp.startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                alarmTmp.alarmPriority = tag.alarmPriority;
                                alarmTmp.alarmTag = tag;
                                //alarmTmp.desc = string.Format("{0} 异常，当前其值为: {1} 地址为 {2}", tag.name, tag.value, tag.addr);
                                alarmTmp.desc = tag.describe;
                                allAlarm.Add(alarmTmp);
                            }
                        }
                    }
                }
            }
            // 自动恢复plc变量报警,由于可能删除列表中的报警，倒序遍历。其他报警需要调用RemoveAlarm方法恢复
            for (int i = allAlarm.Count - 1; i >= 0; i--)
            {
                bool clearAlarm = true;
                if (allTags.ContainsKey(allAlarm[i].title))
                {
                    var tag = allTags[allAlarm[i].title];
                    if (tag.upperValue != "" && float.Parse(tag.value) > float.Parse(tag.upperValue))
                    {
                        clearAlarm = false;
                    }
                    else if (tag.lowerValue != "" && float.Parse(tag.value) < float.Parse(tag.lowerValue))
                    {
                        clearAlarm = false;
                    }
                    else if (tag.validValue != null && !tag.validValue.Contains(tag.value))
                    {
                        clearAlarm = false;
                    }
                    else if (tag.invalidValue != null && tag.invalidValue.Contains(tag.value))
                    {
                        clearAlarm = false;
                    }
                    if (clearAlarm)
                    {
                        allTags[allAlarm[i].title].errorStatus = false;
                        allAlarm.RemoveAt(i);
                    }
                }
            }
        }
        static AlarmItem currentAxisAlarmItem = null;
        static AlarmItem currentDeviceAlarmItem = null;
        /// <summary>
        /// 记录报警日志
        /// </summary>
        public static void AddAlarmLog()
        {
            string formatStr = "yyyy年MM月dd日 HH:mm:ss";
            // 监听DM1000和DM1010的值，
            Thread thread = new Thread(() =>
            {
                // 轴报警错误
                string dm1000 = PlcControl.plc.GetPlcValueStringByAddr("DM1000", PlcDataType.Int16);
                if (!"0".Equals(dm1000) && (currentAxisAlarmItem == null || !dm1000.Equals(currentAxisAlarmItem.code)) )
                {
                    currentAxisAlarmItem = new AlarmItem();
                    currentAxisAlarmItem.code = dm1000;
                    currentAxisAlarmItem.title = currentAxisAlarmItem.axisAlarm[dm1000];
                    currentAxisAlarmItem.startTime = DateTime.Now.ToString(formatStr);
                    Debug.WriteLine($"dm1010:{dm1000}");
                    // 记录日志
                    Tools.LogError($"异常报警，代码：{dm1000}，描述：{currentAxisAlarmItem.title}");
                    // 记录日志
                    RunLogsCommon.LogEvent(LogType.Error, new List<RunLogsViewModel>
                    {
                        new RunLogsViewModel(LogType.Error, "异常"),
                        new RunLogsViewModel("时间", currentAxisAlarmItem.startTime),
                        new RunLogsViewModel("异常编码", "A-" + currentAxisAlarmItem.code),
                        new RunLogsViewModel("异常描述", currentAxisAlarmItem.title),
                    });
                } else if ("0".Equals(dm1000))
                {
                    currentAxisAlarmItem = null;
                }
                // 设备信息
                string dm1010 = PlcControl.plc.GetPlcValueStringByAddr("DM1010", PlcDataType.Int16);
                if (!"0".Equals(dm1010) && (currentDeviceAlarmItem == null || !dm1000.Equals(currentDeviceAlarmItem.code)))
                {
                    currentDeviceAlarmItem.code = dm1010;
                    currentDeviceAlarmItem = new AlarmItem();
                    currentDeviceAlarmItem.title = currentDeviceAlarmItem.axisAlarm[dm1010];
                    currentDeviceAlarmItem.startTime = DateTime.Now.ToString(formatStr);
                    Debug.WriteLine($"dm1010:{dm1010}");
                    // 记录日志
                    Tools.LogError($"异常报警，代码：{dm1010}，描述：{currentDeviceAlarmItem.title}");
                    // 记录日志
                    RunLogsCommon.LogEvent(LogType.Error, new List<RunLogsViewModel>
                    {
                        new RunLogsViewModel(LogType.Error, "异常"),
                        new RunLogsViewModel("时间", currentDeviceAlarmItem.startTime),
                        new RunLogsViewModel("异常编码", "D-" + currentDeviceAlarmItem.code),
                        new RunLogsViewModel("异常描述", currentDeviceAlarmItem.title),
                    });
                } else if ("0".Equals(dm1010))
                {
                    currentDeviceAlarmItem = null;
                }
                Thread.Sleep(200);
            });
            thread.IsBackground = true;
            thread.Start();
        }

        /// <summary>
        /// 步进补偿
        /// </summary>
        /// <param name="location"></param>
        /// <param name="axisParams"></param>
        /// <param name="directionType"></param>
        /// <returns></returns>
        public static string GetCompensateStep2(float stepIndex, string axisParams, int directionType = -1, float targetPosition = -100)
        {
            // 获取当前位置 光栅尺位置
            string currentLocationStr = GetCurrentLocation(axisParams, 0);
            // 获取补偿数据模型
            List<PositionCompensationModel> models = CurrentUtils.GetPositionCompensationModels();
            if (models == null || models.Count == 0) return null;


            if (!float.TryParse(currentLocationStr, out float currLocation))
            {
                return null; // 数据无效，直接返回原目标位置
            }
            float targetLocation = targetPosition == -100 ? currLocation + stepIndex : targetPosition;
            // 确定补偿方向
            PositionCompensationModel axisModel = null;
            if (directionType == -1) // 自动判断方向
            {
                directionType = targetLocation > currLocation ? 0 : 1;
            }
            axisModel = models.Find(item => item.AxisType.Equals(axisParams + (directionType == 1 ? "-反向" : "")));
            // 无法找到对应轴的补偿信息
            if (axisModel == null) return targetLocation.ToString();
            targetLocation = (float)Math.Round(targetLocation, GlobalParams.decimalPlaces);
            // 获取目标点位的补偿数据
            float targetLocationComp = CalculateCompensation(axisModel, targetLocation, directionType);
            float compStr = 0;
            // 获取当前点位(上一点位)的补偿数据
            float currentLocationComp = CalculateCompensation(axisModel, currLocation, directionType);
            // 判断2个位置之间的
            // 用目标点位的补偿值 - 上一目标值的补偿值 = 2个点之间的差值
            float comp = (float)Math.Round(targetLocationComp - currentLocationComp, GlobalParams.decimalPlaces);
            if (directionType == 0)
            {
                stepIndex += comp;
                compStr = comp;
            }
            else if (directionType == 1)
            {
                stepIndex -= comp;
                compStr = comp;
            }
            // 电机位置
            float yCurrentPosition = Tools.GetFloatStringValue(GetCurrentLocation(axisParams, 0));
            // 最终位置等于 电机位置 + 步进 + comp
            yCurrentPosition = yCurrentPosition + stepIndex;
            Debug.WriteLine($"targetLocation:{targetLocation} /t realPosition:{yCurrentPosition}");
            Tools.WriteLineToFile(
                   $"{DateTime.Now}\t{stepIndex}\t{targetLocation}\t{yCurrentPosition}\t{currLocation}\t{compStr.ToString("F6")}"
                   , "logs/compInfo.txt");
            return yCurrentPosition.ToString("F6"); // 返回调整后的目标位置，保留6位小数
        }
        /// <summary>
        /// 步进补偿
        /// </summary>
        /// <param name="location"></param>
        /// <param name="axisParams"></param>
        /// <param name="directionType"></param>
        /// <returns></returns>
        public static string GetCompensateStep(string stepValue, string axisParams, float targetLocationValue, int directionType = -1)
        {
            // 获取当前位置 Tools.GetFloatStringValue(GetCurrentLocation(axisParams)) 
            string currentLocationStr = GetCurrentLocation(axisParams);
            float targetLocation = (float)((GlobalParams.upPosition == -100 ? targetLocationValue
                : GlobalParams.upPosition) + Tools.GetFloatStringValue(stepValue));
            // 获取补偿数据模型
            List<PositionCompensationModel> models = CurrentUtils.GetPositionCompensationModels();
            if (models == null || models.Count == 0) return targetLocation.ToString();


            if (!float.TryParse(currentLocationStr, out float currLocation))
            {
                return targetLocation.ToString(); // 数据无效，直接返回原目标位置
            }

            // 确定补偿方向
            PositionCompensationModel axisModel = null;
            if (directionType == -1) // 自动判断方向
            {
                directionType = targetLocation > currLocation ? 0 : 1;
            }
            axisModel = models.Find(item => item.AxisType.Equals(axisParams + (directionType == 1 ? "-反向" : "")));
            // 无法找到对应轴的补偿信息
            if (axisModel == null) return targetLocation.ToString();
            targetLocation = (float)Math.Round(targetLocation, GlobalParams.decimalPlaces);
            // 获取目标点位的补偿数据
            float targetLocationComp = CalculateCompensation(axisModel, targetLocation, directionType);
            float realPosition = targetLocation; // 本次要走的位置
            string compStr = "";
            // 如果步进等于0 则取一个点的补偿就返回
            if (stepValue.Equals("0"))
            {
                if (directionType == 0)
                {
                    realPosition -= targetLocationComp;
                    compStr = $"正方向补偿-={targetLocationComp}";
                }
                else if (directionType == 1)
                {
                    realPosition += targetLocationComp;
                    compStr = $"反方向补偿-={targetLocationComp}";
                }
            } else
            {
                // 获取当前点位(上一点位)的补偿数据
                float currentLocationComp = CalculateCompensation(axisModel, GlobalParams.upPosition, directionType);
                // 判断2个位置之间的
                // 用目标点位的补偿值 - 上一目标值的补偿值 = 2个点之间的差值
                float comp = (float)Math.Round(targetLocationComp - currentLocationComp, GlobalParams.decimalPlaces);
                // 如果两次的差值小于0.0002 则不补偿，加入总补偿差值
                GlobalParams.allDeepValue += comp;
                // 如果累积的差值大于0.0002 则本次运动需要加上补偿值 0.0002
                Tools.LogInfo($"comp:{comp}\tGlobalParams.allDeepValue:{GlobalParams.allDeepValue}");
                if (MathF.Round(GlobalParams.allDeepValue, 4) >= 0.0002f || MathF.Round(GlobalParams.allDeepValue, 4) <= -0.0002f)
                {
                    if (directionType == 0)
                    {
                        realPosition -= GlobalParams.allDeepValue;
                        compStr = $"正方向补偿-={GlobalParams.allDeepValue}";
                        Tools.LogInfo(compStr);
                    }
                    else if (directionType == 1)
                    {
                        realPosition += GlobalParams.allDeepValue;
                        compStr = $"反方向补偿-={GlobalParams.allDeepValue}";
                        Tools.LogInfo(compStr);
                    }
                    GlobalParams.allDeepValue = 0;
                }
            }
            GlobalParams.upPosition = realPosition;
            Debug.WriteLine($"targetLocation:{targetLocation} /t realPosition:{realPosition}");
            Tools.WriteLineToFile(
                   $"{DateTime.Now}\t{targetLocationValue}\t{stepValue}\t{targetLocation}\t{realPosition}\t{compStr}"
                   , "logs/compInfo.txt");
            return realPosition.ToString("F4"); // 返回调整后的目标位置，保留6位小数
        }
        public static string GetCompensate(string location, string axisParams, int directionType = -1)
        {
            // 获取补偿数据模型
            List<PositionCompensationModel> models = CurrentUtils.GetPositionCompensationModels();
            if (models == null || models.Count == 0) return location;

            // 获取当前位置
            string currentLocationStr = GetCurrentLocation(axisParams);
            if (!float.TryParse(currentLocationStr, out float currLocation) || !float.TryParse(location, out float targetLocation))
            {
                return location; // 数据无效，直接返回原目标位置
            }

            // 确定补偿方向
            PositionCompensationModel axisModel = null;
            if (directionType == -1) // 自动判断方向
            {
                directionType = targetLocation > currLocation ? 0 : 1;
            }
            axisModel = models.Find(item => item.AxisType.Equals(axisParams + (directionType == 1 ? "-反向" : "")));

            // 无法找到对应轴的补偿信息
            if (axisModel == null) return location;
            targetLocation = (float)Math.Round(targetLocation, GlobalParams.decimalPlaces);
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
            return targetLocation.ToString("F6"); // 返回调整后的目标位置，保留6位小数
        }

        /// <summary>
        /// 根据轴参数获取当前位置
        /// </summary>
        public static string GetCurrentLocation(string axisParams, int type = 1)
        {
            try
            {
                if (type == 0)
                {
                    return axisParams switch
                    {
                        "X轴" => PlcControl.plc.GetPlcValueString(DeviceKey.curLocationKey),
                        "Y轴" => PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey),
                        "Z1轴" => PlcControl.plc.GetPlcValueString(DeviceKey.z1CurLocationKey),
                        _ => null // 未知轴类型
                    };
                }
                else if (type == 1)
                {
                    return axisParams switch
                    {
                        "X轴" => PlcControl.plc.GetPlcValueString(DeviceKey.curLocationKey),
                        "Y轴" => PlcControl.plc.GetPlcValueString(DeviceKey.yGratingRulerCurLocationKey),
                        "Z1轴" => PlcControl.plc.GetPlcValueString(DeviceKey.z1GratingRulerCurLocationKey),
                        _ => null // 未知轴类型
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving current location for {axisParams}: {ex.Message}");
                return null;
            }
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



        public bool ConnectPlc()
        {
            connectionStatus = plc.ConnectPlc();
            return connectionStatus;
        }

        public bool DisconnectPlc()
        {
            bool res = plc.CloseConnect();
            if (res)
            {
                connectionStatus = false;
            }
            return res;
        }
    }
}
