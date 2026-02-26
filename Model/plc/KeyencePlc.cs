using Emgu.CV.Dnn;
using HslCommunication;
using HslCommunication.Profinet.Keyence;
using Newtonsoft.Json.Linq;
using NPOI.OpenXmlFormats.Dml.Diagram;
using NPOI.SS.Formula.Functions;
using Prism.Events;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Numerics;
using 精密切割系统.database.db.modle;
using 精密切割系统.Extensions;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.plc;
using 精密切割系统.Model.sqlite;
using 精密切割系统.PubSubEvent;
using 精密切割系统.Utils;
using 精密切割系统.ViewModel;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace 精密切割系统.Driver
{
    //基恩士PLC各个点位和变量的读取及写入
    public class KeyencePlc
    {
        private static readonly Lazy<KeyencePlc> _lazy = new(() => new KeyencePlc());

        public static KeyencePlc Instance
        {
            get { return _lazy.Value; }
        }

        private const int MinDelayMs = 100; // 基础延迟
        public static string plcIP = "192.168.10.10";
        private static KeyenceMcNet keyence_net = new KeyenceMcNet(plcIP, 5000);
        private static KeyenceMcNet _keyence_async_net = new KeyenceMcNet(plcIP, 5000);
        public static OperateResult connect;
        public Dictionary<string, PlcDataType> typeMap = new Dictionary<string, PlcDataType>();
        public static readonly SemaphoreSlim PlcSemaphore = new(10, 10);
        private readonly TimeSpan TimeOutSpan = TimeSpan.FromSeconds(3);

        static KeyencePlc()
        {
            keyence_net.ConnectTimeOut = 1000;
            keyence_net.ReceiveTimeOut = MinDelayMs;
            _keyence_async_net.ConnectTimeOut = 1000;
            _keyence_async_net.ReceiveTimeOut = MinDelayMs;
        }

        private KeyencePlc()
        {
            typeMap.Add("bool", PlcDataType.Bool);
            typeMap.Add("int16", PlcDataType.Int16);
            typeMap.Add("uint16", PlcDataType.UInt16);
            typeMap.Add("int32", PlcDataType.Int32);
            typeMap.Add("uint32", PlcDataType.UInt32);
            typeMap.Add("int64", PlcDataType.Int64);
            typeMap.Add("uint64", PlcDataType.UInt64);
            typeMap.Add("float", PlcDataType.Float);
            typeMap.Add("double", PlcDataType.Double);
        }

        public async Task<CommonResult> ConnectServerAsync()
        {
            var connect = await keyence_net.ConnectServerAsync();
            if (!connect.IsSuccess)
            {
                return CommonResult.Failure($"PLC连接失败: {connect.Message}");
            }
            connect = await _keyence_async_net.ConnectServerAsync();
            if (!connect.IsSuccess)
            {
                return CommonResult.Failure($"PLC连接失败: {connect.Message}");
            }
            return CommonResult.Success();
        }

        public string readData(string plcAddr, PlcDataType dataType = PlcDataType.Int32, ushort dataNumber = 1)
        {
            if (!GlobalParams.OnlineFlag) return "";
            if (!PlcSemaphore.Wait(TimeOutSpan))
            {
                return null;
            }
            try
            {
                const int maxRetries = 3;
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        OperateResult<object[]> read = null;
                        switch (dataType)
                        {
                            case PlcDataType.Bool:
                                var boolResult = keyence_net.ReadBool(plcAddr, dataNumber);
                                read = boolResult.IsSuccess ?
                                    OperateResult.CreateSuccessResult(boolResult.Content.Cast<object>().ToArray()) :
                                    OperateResult.CreateFailedResult<object[]>(boolResult);
                                break;

                            case PlcDataType.Int16:
                                var int16Result = keyence_net.ReadInt16(plcAddr, dataNumber);
                                if (int16Result.IsSuccess)
                                {
                                    var convertedContent = int16Result.Content.Select(x => (object)((int)x)).ToArray();
                                    read = OperateResult.CreateSuccessResult(convertedContent);
                                }
                                else
                                {
                                    read = OperateResult.CreateFailedResult<object[]>(int16Result);
                                }
                                break;

                            case PlcDataType.UInt16:
                                var uint16Result = keyence_net.ReadUInt16(plcAddr, dataNumber);
                                if (uint16Result.IsSuccess)
                                {
                                    var convertedContent = uint16Result.Content.Select(x => (object)((uint)x)).ToArray();
                                    read = OperateResult.CreateSuccessResult(convertedContent);
                                }
                                else
                                {
                                    read = OperateResult.CreateFailedResult<object[]>(uint16Result);
                                }
                                break;

                            case PlcDataType.Int32:
                                var int32Result = keyence_net.ReadInt32(plcAddr, dataNumber);
                                if (int32Result.IsSuccess)
                                {
                                    var convertedContent = int32Result.Content.Select(x => (object)((int)x)).ToArray();
                                    read = OperateResult.CreateSuccessResult(convertedContent);
                                }
                                else
                                {
                                    read = OperateResult.CreateFailedResult<object[]>(int32Result);
                                }
                                break;

                            case PlcDataType.UInt32:
                                var uint32Result = keyence_net.ReadUInt32(plcAddr, dataNumber);
                                if (uint32Result.IsSuccess)
                                {
                                    var convertedContent = uint32Result.Content.Select(x => (object)((uint)x)).ToArray();
                                    read = OperateResult.CreateSuccessResult(convertedContent);
                                }
                                else
                                {
                                    read = OperateResult.CreateFailedResult<object[]>(uint32Result);
                                }
                                break;

                            case PlcDataType.Int64:
                                var int64Result = keyence_net.ReadInt64(plcAddr, dataNumber);
                                if (int64Result.IsSuccess)
                                {
                                    var convertedContent = int64Result.Content.Select(x => (object)((long)x)).ToArray();
                                    read = OperateResult.CreateSuccessResult(convertedContent);
                                }
                                else
                                {
                                    read = OperateResult.CreateFailedResult<object[]>(int64Result);
                                }
                                break;

                            case PlcDataType.UInt64:
                                var uint64Result = keyence_net.ReadUInt64(plcAddr, dataNumber);
                                if (uint64Result.IsSuccess)
                                {
                                    var convertedContent = uint64Result.Content.Select(x => (object)((ulong)x)).ToArray();
                                    read = OperateResult.CreateSuccessResult(convertedContent);
                                }
                                else
                                {
                                    read = OperateResult.CreateFailedResult<object[]>(uint64Result);
                                }
                                break;

                            case PlcDataType.Float:
                                var floatResult = keyence_net.ReadFloat(plcAddr, dataNumber);
                                if (floatResult.IsSuccess)
                                {
                                    var convertedContent = floatResult.Content.Select(x => (object)((float)x)).ToArray();
                                    read = OperateResult.CreateSuccessResult(convertedContent);
                                }
                                else
                                {
                                    read = OperateResult.CreateFailedResult<object[]>(floatResult);
                                }
                                break;

                            case PlcDataType.Double:
                                var doubleResult = keyence_net.ReadDouble(plcAddr, dataNumber);
                                if (doubleResult.IsSuccess)
                                {
                                    var convertedContent = doubleResult.Content.Select(x => (object)((double)x)).ToArray();
                                    read = OperateResult.CreateSuccessResult(convertedContent);
                                }
                                else
                                {
                                    read = OperateResult.CreateFailedResult<object[]>(doubleResult);
                                }
                                break;

                            default:
                                throw new ArgumentException($"不支持的数据类型：{dataType}");
                        }

                        if (read != null && read.IsSuccess && read.Content != null && read.Content.Length > 0)
                        {
                            return read.Content[0].ToString();
                        }

                        if (attempt < maxRetries)
                        {
                            System.Threading.Thread.Sleep(MinDelayMs);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (attempt < maxRetries)
                        {
                            System.Threading.Thread.Sleep(MinDelayMs);
                        }
                    }
                }
                Tools.Monitor($"同步 读取PLC失败：地址 {plcAddr}，类型 {dataType}，所有尝试均失败");
            }
            finally
            {
                PlcSemaphore.Release();
            }
            return null;
        }

        public async Task<bool?> ReadDataAsync(string plcAddr)
        {
            bool[]? bools = await ReadDataAsync(plcAddr, 1);
            if (bools != null && bools.Length == 1)
            {
                return bools[0];
            }
            return null;
        }

        public async Task<bool[]?> ReadDataAsync(string plcAddr, ushort dataNumber)
        {
            if (!GlobalParams.OnlineFlag) return null;
            if (!await PlcSemaphore.WaitAsync(TimeOutSpan))
            {
                return null;
            }
            try
            {
                const int maxRetries = 3;
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        var int16Result = await _keyence_async_net.ReadBoolAsync(plcAddr, dataNumber);
                        if (int16Result.IsSuccess)
                        {
                            return int16Result.Content;
                        }
                        if (attempt < maxRetries)
                        {
                            //Tools.LogWarning($"读取PLC数据失败，地址：{plcAddr}，类型：{typeof(bool)}，尝试第{attempt}次重试");
                            await Task.Delay(MinDelayMs);
                        }
                    }
                    catch (Exception ex)
                    {
                        //Tools.LogError($"读取PLC异常：地址 {plcAddr}，类型 {typeof(bool)}，尝试 {attempt}/{maxRetries}，异常：{ex.Message}");
                        if (attempt < maxRetries)
                        {
                            await Task.Delay(MinDelayMs);
                        }
                    }
                }
                Tools.Monitor($"异步 读取PLC失败：地址 {plcAddr}，类型 {typeof(bool)}，所有尝试均失败");
            }
            finally
            {
                PlcSemaphore.Release();
            }
            return null;
        }

        public async Task<T?> ReadDataAsync<T>(string plcAddr) where T : struct, INumber<T>
        {
            T[]? datas = await ReadDataAsync<T>(plcAddr, 1);
            if (datas != null && datas.Length == 1)
            {
                return datas[0];
            }
            return null;
        }

        public async Task<T[]?> ReadDataAsync<T>(string plcAddr, ushort dataNumber) where T : INumber<T>
        {
            Type type = typeof(T);
            if (!type.IsPrimitive) return null;
            if (!await PlcSemaphore.WaitAsync(TimeOutSpan))
            {
                return null;
            }
            try
            {
                const int maxRetries = 3;
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        if (type == typeof(short))
                        {
                            var int16Result = await _keyence_async_net.ReadInt16Async(plcAddr, dataNumber);
                            if (int16Result.IsSuccess)
                            {
                                return int16Result.Content.Select(p => T.CreateChecked(p)).ToArray();
                            }
                        }
                        else if (type == typeof(ushort))
                        {
                            var uint16Result = await _keyence_async_net.ReadUInt16Async(plcAddr, dataNumber);
                            if (uint16Result.IsSuccess)
                            {
                                return uint16Result.Content.Select(p => T.CreateChecked(p)).ToArray();
                            }
                        }
                        else if (type == typeof(int))
                        {
                            var int32Result = await _keyence_async_net.ReadInt32Async(plcAddr, dataNumber);
                            if (int32Result.IsSuccess)
                            {
                                return int32Result.Content.Select(p => T.CreateChecked(p)).ToArray();
                            }
                        }
                        else if (type == typeof(uint))
                        {
                            var uint32Result = await _keyence_async_net.ReadUInt32Async(plcAddr, dataNumber);
                            if (uint32Result.IsSuccess)
                            {
                                return uint32Result.Content.Select(p => T.CreateChecked(p)).ToArray();
                            }
                        }
                        else if (type == typeof(long))
                        {
                            var int64Result = await _keyence_async_net.ReadInt64Async(plcAddr, dataNumber);
                            if (int64Result.IsSuccess)
                            {
                                return int64Result.Content.Select(p => T.CreateChecked(p)).ToArray();
                            }
                        }
                        else if (type == typeof(ulong))
                        {
                            var uint64Result = await _keyence_async_net.ReadUInt64Async(plcAddr, dataNumber);
                            if (uint64Result.IsSuccess)
                            {
                                return uint64Result.Content.Select(p => T.CreateChecked(p)).ToArray();
                            }
                        }
                        else if (type == typeof(float))
                        {
                            var floatResult = await _keyence_async_net.ReadFloatAsync(plcAddr, dataNumber);
                            if (floatResult.IsSuccess)
                            {
                                return floatResult.Content.Select(p => T.CreateChecked(p)).ToArray();
                            }
                        }
                        else if (type == typeof(double))
                        {
                            var doubleResult = await _keyence_async_net.ReadDoubleAsync(plcAddr, dataNumber);
                            if (doubleResult.IsSuccess)
                            {
                                return doubleResult.Content.Select(p => T.CreateChecked(p)).ToArray();
                            }
                        }
                        else
                        {
                            throw new ArgumentException($"不支持的数据类型：{type.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(MinDelayMs);
                    }
                }
                Tools.Monitor($"异步 读取PLC失败：地址 {plcAddr}，类型 {type.Name}，所有尝试均失败");
            }
            finally
            {
                PlcSemaphore.Release();
            }
            return null;
        }

        public OperateResult? WriteData(string plcAddr, object pData, PlcDataType dataType = PlcDataType.Int32)
        {
            if (!PlcSemaphore.Wait(TimeOutSpan))
            {
                return null;
            }
            OperateResult write = new OperateResult();

            try
            {
                switch (dataType)
                {
                    case PlcDataType.Bool:
                        bool boolRes = pData.ToString().Equals("1") || pData.ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
                        write = keyence_net.Write(plcAddr, boolRes);
                        break;

                    case PlcDataType.Int16:
                        if (short.TryParse(pData.ToString(), out short int16Res))
                            write = keyence_net.Write(plcAddr, int16Res);
                        else
                            throw new ArgumentException("Invalid Int16 value.");
                        break;

                    case PlcDataType.UInt16:
                        if (ushort.TryParse(pData.ToString(), out ushort uint16Res))
                            write = keyence_net.Write(plcAddr, uint16Res);
                        else
                            throw new ArgumentException("Invalid UInt16 value.");
                        break;

                    case PlcDataType.Int32:
                        if (int.TryParse(pData.ToString(), out int int32Res))
                            write = keyence_net.Write(plcAddr, int32Res);
                        else
                            throw new ArgumentException("Invalid Int32 value.");
                        break;

                    case PlcDataType.UInt32:
                        if (uint.TryParse(pData.ToString(), out uint uint32Res))
                            write = keyence_net.Write(plcAddr, uint32Res);
                        else
                            throw new ArgumentException("Invalid UInt32 value.");
                        break;

                    case PlcDataType.Int64:
                        if (long.TryParse(pData.ToString(), out long int64Res))
                            write = keyence_net.Write(plcAddr, int64Res);
                        else
                            throw new ArgumentException("Invalid Int64 value.");
                        break;

                    case PlcDataType.UInt64:
                        if (ulong.TryParse(pData.ToString(), out ulong uint64Res))
                            write = keyence_net.Write(plcAddr, uint64Res);
                        else
                            throw new ArgumentException("Invalid UInt64 value.");
                        break;

                    case PlcDataType.Float:
                        if (float.TryParse(pData.ToString(), out float floatRes))
                            write = keyence_net.Write(plcAddr, floatRes);
                        else
                            throw new ArgumentException("Invalid Float value.");
                        break;

                    case PlcDataType.Double:
                        if (double.TryParse(pData.ToString(), out double doubleRes))
                            write = keyence_net.Write(plcAddr, doubleRes);
                        else
                            throw new ArgumentException("Invalid Double value.");
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(dataType), "Unsupported PlcDataType.");
                }
            }
            catch (Exception ex)
            {
                write = new OperateResult { IsSuccess = false, Message = ex.Message };
            }
            finally
            {
                PlcSemaphore.Release();
            }

            return write;
        }

        public async Task<OperateResult?> WriteDataAsync(string plcAddr, object pData, PlcDataType dataType = PlcDataType.Int32)
        {
            if (!await PlcSemaphore.WaitAsync(TimeOutSpan))
            {
                return null;
            }
            OperateResult write;
            try
            {
                if (pData is not string data)
                {
                    throw new ArgumentException("不是string类型", nameof(pData));
                }
                switch (dataType)
                {
                    case PlcDataType.Bool:
                        bool boolRes = data.Equals("1") || data.Equals("true", StringComparison.OrdinalIgnoreCase);
                        write = await _keyence_async_net.WriteAsync(plcAddr, boolRes);
                        break;

                    case PlcDataType.Int16:
                        if (short.TryParse(data, out short int16Res))
                            write = await _keyence_async_net.WriteAsync(plcAddr, int16Res);
                        else
                            throw new ArgumentException("Invalid Int16 value.");
                        break;

                    case PlcDataType.UInt16:
                        if (ushort.TryParse(data, out ushort uint16Res))
                            write = await _keyence_async_net.WriteAsync(plcAddr, uint16Res);
                        else
                            throw new ArgumentException("Invalid UInt16 value.");
                        break;

                    case PlcDataType.Int32:
                        if (int.TryParse(data, out int int32Res))
                            write = await _keyence_async_net.WriteAsync(plcAddr, int32Res);
                        else
                            throw new ArgumentException("Invalid Int32 value.");
                        break;

                    case PlcDataType.UInt32:
                        if (uint.TryParse(data, out uint uint32Res))
                            write = await _keyence_async_net.WriteAsync(plcAddr, uint32Res);
                        else
                            throw new ArgumentException("Invalid UInt32 value.");
                        break;

                    case PlcDataType.Int64:
                        if (long.TryParse(data, out long int64Res))
                            write = await _keyence_async_net.WriteAsync(plcAddr, int64Res);
                        else
                            throw new ArgumentException("Invalid Int64 value.");
                        break;

                    case PlcDataType.UInt64:
                        if (ulong.TryParse(data, out ulong uint64Res))
                            write = await _keyence_async_net.WriteAsync(plcAddr, uint64Res);
                        else
                            throw new ArgumentException("Invalid UInt64 value.");
                        break;

                    case PlcDataType.Float:
                        if (float.TryParse(data, out float floatRes))
                            write = await _keyence_async_net.WriteAsync(plcAddr, floatRes);
                        else
                            throw new ArgumentException("Invalid Float value.");
                        break;

                    case PlcDataType.Double:
                        if (double.TryParse(data, out double doubleRes))
                            write = await _keyence_async_net.WriteAsync(plcAddr, doubleRes);
                        else
                            throw new ArgumentException("Invalid Double value.");
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(dataType), "Unsupported PlcDataType.");
                }
            }
            catch (Exception ex)
            {
                write = new OperateResult { IsSuccess = false, Message = ex.Message };
            }
            finally
            {
                PlcSemaphore.Release();
            }

            return write;
        }

        /// <summary>
        /// 退出所有模式
        /// </summary>
        public void exitAllModel()
        {
            PlcControl.tagControl.bladeMantance.RunBladeSetup(0);
            PlcControl.tagControl.bladeMantance.RunBladeReplace(0);
            PlcControl.tagControl.cutting.EnterFullAutoInit(0);
            PlcControl.tagControl.sparkRepairKnife.EnterElectrical(0);
            PlcControl.tagControl.wholeDevice.IoModelSet(0);
            PlcControl.tagControl.flange.JoinTrimming(0);
        }

        public bool writeTag(Tag tag)
        {
            if (!GlobalParams.OnlineFlag)
            {
                return true;
            }
            // 检查输入参数有效性
            if (tag == null || string.IsNullOrEmpty(tag.writeValue))
            {
                Tools.Monitor("同步 写入PLC失败：tag或tag.writeValue为空");
                return false;
            }

            string writeValue = tag.writeValue;

            // 检查上下限值
            writeValue = GetValidatedWriteValue(tag);

            // 尝试写入数据，最多重试3次
            const int retryCount = 3;
            for (int attempt = 1; attempt <= retryCount; attempt++)
            {
                try
                {
                    OperateResult? res = WriteData(tag.addr, writeValue, typeMap[tag.valueType]);
                    if (res != null && res.IsSuccess)
                    {
                        // Tools.LogInfo($"写入PLC成功：地址 {tag.addr}，值 {writeValue}");
                        return true;
                    }
                    //Tools.LogWarning($"写入PLC失败：地址 {tag.addr}，值 {writeValue}，尝试次数 {attempt}");
                }
                catch (Exception ex)
                {
                    //Tools.LogError($"写入PLC异常：地址 {tag.addr}，值 {writeValue}，尝试次数 {attempt}，异常信息：{ex.Message}");
                }
            }

            Tools.Monitor($"同步 写入PLC失败：地址 {tag.addr}，值 {writeValue}，所有尝试均失败");
            return false;
        }

        public async Task<bool> WriteTagAsync(Tag tag)
        {
            if (!GlobalParams.OnlineFlag)
            {
                return true;
            }
            string writeValue = tag.writeValue;

            // 检查输入参数有效性
            if (tag == null || string.IsNullOrEmpty(writeValue))
            {
                Tools.Monitor("写入PLC失败：tag或tag.writeValue为空");
                return false;
            }

            // 检查上下限值
            writeValue = GetValidatedWriteValue(tag);

            // 尝试写入数据，最多重试3次
            const int retryCount = 3;
            for (int attempt = 1; attempt <= retryCount; attempt++)
            {
                try
                {
                    OperateResult? res = await WriteDataAsync(tag.addr, writeValue, typeMap[tag.valueType]);
                    if (res != null && res.IsSuccess)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                }
                if (attempt < retryCount)
                {
                    await Task.Delay(MinDelayMs);
                }
            }
            Tools.Monitor($"写入PLC失败：地址 {tag.addr}，值 {writeValue}，所有尝试均失败");
            //Tools.LogDebug($"地址：{tag.addr}  写入：{writeValue}  状态：失败");
            return false;
        }

        /// <summary>
        /// 验证并返回符合上下限约束的写入值。
        /// </summary>
        private string GetValidatedWriteValue(Tag tag)
        {
            string writeValue = tag.writeValue;

            // 检查上限值
            if (!string.IsNullOrEmpty(tag.maxValue) &&
                float.TryParse(tag.Value, out float value) && // 首次解析
                float.TryParse(tag.maxValue, out float upperValue))
            {
                if (value > upperValue)
                {
                    Tools.LogWarning($"{tag.addr}写入值：{tag.writeValue}超过上限{upperValue}，调整为{tag.maxValue}");
                    writeValue = tag.maxValue;
                }
            }

            // 检查下限值
            if (!string.IsNullOrEmpty(tag.minValue) &&
                float.TryParse(tag.Value, out value) && // 再次解析
                float.TryParse(tag.minValue, out float lowerValue))
            {
                if (value < lowerValue)
                {
                    Tools.LogWarning($"{tag.addr}写入值：{tag.writeValue}低于下限{lowerValue}，调整为{tag.minValue}");
                    writeValue = tag.minValue;
                }
            }

            return writeValue;
        }
    }

    public enum PlcDataType
    {
        Bool,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Float,
        Double,
    }

    public class PlcTags
    {
        public PlcTags()
        { }

        public Axis Xaxis { get; set; } = new Axis(AxisNameType.X);
        public Axis Yaxis { get; set; } = new Axis(AxisNameType.Y);
        public Axis Z1axis { get; set; } = new Axis(AxisNameType.Z1);
        public Axis Z2axis { get; set; } = new Axis(AxisNameType.Z2);
        public Axis ThetaAxis { get; set; } = new Axis(AxisNameType.Theta);

        public BladeMantance bladeMantance { get; set; }

        public WholeDevice wholeDevice { get; set; }

        public Calibration calibration { get; set; }

        public Cutting cutting { get; set; }

        public SparkRepairKnife sparkRepairKnife { get; set; }
        public Flange flange { get; set; }
    }

    public class Axis
    {
        public Axis()
        {
        }

        public Axis(string name)
        {
            this.axisName = name;
        }

        private KeyencePlc keyencePlc = KeyencePlc.Instance;

        // 运动轴名称
        public string axisName;

        // 运动轴是否定位到原点
        public Tag hasOriginPoint { get; set; }

        // 运动轴当前位置
        public Tag curLocation { get; set; }

        // 光栅尺当前位置
        public Tag gratingRulerCurLocation { get; set; }

        // 运动轴当前速度curMotion
        public Tag curSpeed { get; set; }

        // 运动轴当前状态 =1  axis ready
        public Tag curStatus { get; set; }

        // 电机当前状态 =1 busy，=2 done
        public Tag curMotion { get; set; }

        // 运动类型 0 点动 1 相对运动
        public Tag runType { get; set; }

        // 回原点启动
        public Tag startHoming { get; set; }

        public Tag relaxAxis { get; set; }

        // 模式正转开始
        public Tag jogStart { get; set; }

        // 模式反转开始
        public Tag jogAntiStart { get; set; }

        // 点动/相对模式速度
        public Tag jogRelativeSpeed { get; set; }

        // 高速-点动/相对模式速度
        public Tag jogRelativeHighSpeed { get; set; }

        // 相对运动目标位置
        public Tag relativeDistance { get; set; }

        // 绝对运动开始
        public Tag absoluteStart { get; set; }

        // 绝对运动速度
        public Tag absoluteSpeed { get; set; }

        // 绝对运动目标位置
        public Tag absoluteLocation { get; set; }

        // 面板按钮运动距离
        public Tag panelRelativeDistance { get; set; }

        // 高速运动 0 低速 1 高速
        public Tag highSpeed { get; set; }

        public Tag isReady { get; set; }
        public Tag isCompleteAbsoluteMotion { get; set; }

        // 轴软正限位
        public Tag softUpperLimit { get; set; }

        // 轴软负限位F
        public Tag softLowerLimit { get; set; }

        /// <summary>
        /// 原点补偿
        /// </summary>
        public Tag originCompensation { get; set; }

        /// <summary>
        /// 设置软正限位
        /// </summary>
        /// <param name="limitPosition"></param>
        /// <returns></returns>
        public async Task SetSoftUpperLimit(float limitPosition)
        {
            softUpperLimit.writeValue = limitPosition.ToString("F3");
            await keyencePlc.WriteTagAsync(softUpperLimit);
        }

        /// <summary>
        /// 设置软负限位
        /// </summary>
        /// <param name="limitPosition"></param>
        /// <returns></returns>
        public async Task SetSoftLowerLimit(float limitPosition)
        {
            softLowerLimit.writeValue = limitPosition.ToString("F3");
            await keyencePlc.WriteTagAsync(softLowerLimit);
        }

        /// <summary>
        /// 设置原点补偿
        /// </summary>
        /// <param name="compenstation"></param>
        /// <returns></returns>
        public async Task SetOriginCompensation(float compenstation)
        {
            originCompensation.writeValue = compenstation.ToString("F3");
            await keyencePlc.WriteTagAsync(originCompensation);
        }

        public bool IsReady()
        {
            if (AlarmConfig.Instance.HasActiveErrorAlarm())
            {
                return false;
            }
            return IsReadyAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 轴是否准备好
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsReadyAsync()
        {
            return await PlcControl.plc.ReadDataAsync(isReady.addr) == true;
        }

        /// <summary>
        /// 等待轴准备好
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task WaitAxisReadyAsync(CancellationToken token)
        {
            await TaskUtils.WaitExpectedResultAsync(IsReadyAsync, default, token);
        }

        /// <summary>
        /// 轴是否停止
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsStopedAxisAsync()
        {
            return await PlcControl.plc.ReadDataAsync(isReady.addr) == true;
        }

        /// <summary>
        /// 等待轴停止
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task WaitAxisStopAsync(CancellationToken token)
        {
            await TaskUtils.WaitExpectedResultAsync(IsStopedAxisAsync, default, token);
        }

        public async Task<bool> IsCompleteAbsoluteMotionAsync()
        {
            return await PlcControl.plc.ReadDataAsync(isCompleteAbsoluteMotion.addr) == true;
        }

        public async Task<bool> IsSpeedZeroAsync()
        {
            float? speepd = await PlcControl.plc.ReadDataAsync<float>(curSpeed.addr);
            return speepd != null && speepd.Value.NearlyEquals(0, 0.1f);
        }

        private async Task<bool> IsNearlyPosition(float targetPosition)
        {
            float? curLoacation = await GetCurrentLocationAsync();
            return curLoacation != null && curLoacation.Value.NearlyEquals(targetPosition, 0.1f);
        }

        public async Task<float?> GetCurrentLocationAsync()
        {
            return await keyencePlc.ReadDataAsync<float>(curLocation.addr);
        }

        public async Task<float?> GetCurrentLocationWaitAsync(CancellationToken token)
        {
            if (!GlobalParams.OnlineFlag) return null;
            await WaitAxisStopAsync(token);
            return await GetCurrentLocationAsync();
        }

        /// <summary>
        /// 回零点
        /// </summary>
        public async Task StartHomingAsync()
        {
            startHoming.writeValue = "0";
            await keyencePlc.WriteTagAsync(startHoming);
            startHoming.writeValue = "1";
            await keyencePlc.WriteTagAsync(startHoming);
        }

        /// <summary>
        /// 轴放松
        /// </summary>
        /// <returns></returns>
        public async Task RelaxAxisAsync()
        {
            relaxAxis.writeValue = "0";
            await keyencePlc.WriteTagAsync(relaxAxis);
            relaxAxis.writeValue = "1";
            await keyencePlc.WriteTagAsync(relaxAxis);
        }

        /// <summary>
        /// 点动开始
        /// </summary>
        /// <param name="speed">速度</param>
        /// <param name="jogDirection">方向 0 正 1 负</param>
        public async Task StartJogAsync(int jogDirection)
        {
            if (jogDirection == 0)
            {
                // 开启正转
                jogStart.writeValue = "1";
                await keyencePlc.WriteTagAsync(jogStart);
            }
            else
            {
                // 开启反转
                jogAntiStart.writeValue = "1";
                await keyencePlc.WriteTagAsync(jogAntiStart);
            }
        }

        /// <summary>
        /// 点动结束
        /// </summary>
        /// <param name="jogDirection">方向 0 正 1 负</param>
        public async Task WaitStopJogAsync(CancellationToken token)
        {
            jogStart.writeValue = "0";
            await keyencePlc.WriteTagAsync(jogStart);
            jogAntiStart.writeValue = "0";
            await keyencePlc.WriteTagAsync(jogAntiStart);
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
            while (await timer.WaitForNextTickAsync(token) && (await PlcControl.plc.ReadDataAsync(jogStart.addr) != false || await PlcControl.plc.ReadDataAsync(jogAntiStart.addr) != false))
            {
                jogStart.writeValue = "0";
                await keyencePlc.WriteTagAsync(jogStart);
                jogAntiStart.writeValue = "0";
                await keyencePlc.WriteTagAsync(jogAntiStart);
            }
        }

        /// <summary>
        /// 点动结束
        /// </summary>
        /// <param name="jogDirection">方向 0 正 1 负</param>
        public async Task StopJogAsync()
        {
            jogStart.writeValue = "0";
            await keyencePlc.WriteTagAsync(jogStart);
            jogAntiStart.writeValue = "0";
            await keyencePlc.WriteTagAsync(jogAntiStart);
        }

        /// <summary>
        /// 设置点动和寸动的移动速度
        /// </summary>
        /// <param name="speed"></param>
        public async Task SetJogRelativeSpeedAsync(float speed)
        {
            jogRelativeSpeed.writeValue = speed.ToString();
            await keyencePlc.WriteTagAsync(jogRelativeSpeed);
        }

        public async Task SetAbsoluteSpeedAsync(float speed)
        {
            absoluteSpeed.writeValue = speed.ToString();
            await keyencePlc.WriteTagAsync(absoluteSpeed);
        }

        /// <summary>
        /// 开始绝对运动
        /// </summary>
        /// <param name="location"></param>
        /// <param name="token"></param>
        /// <param name="speed"></param>
        /// <returns></returns>
        public async Task StartAbsoluteAsync(float location, float? speed, CancellationToken token)
        {
            if (!GlobalParams.OnlineFlag) return;
            if (!GlobalParams.HasTheta && axisName == AxisNameType.Theta) return;
            await WaitStopJogAsync(token.WithDefaultTimeout());
            // 等待轴准备好
            await WaitAxisReadyAsync(token.WithDefaultTimeout());
            // 设置绝对运动位置
            absoluteLocation.writeValue = location.ToString();
            await keyencePlc.WriteTagAsync(absoluteLocation);
            // 最大移动距离
            int maxDistance = 400;
            int waitTime = 1;
            if (speed != null)
            {
                // 设置绝对运动速度
                await SetAbsoluteSpeedAsync(speed.Value);
                waitTime += (int)MathF.Ceiling(maxDistance / speed.Value);
            }
            else
            {
                float defaultSpeed = axisName switch
                {
                    AxisNameType.X => GlobalParams.XDefaultSpeed,
                    AxisNameType.Y => GlobalParams.YDefaultSpeed,
                    AxisNameType.Z1 => GlobalParams.Z1DefaultSpeed,
                    AxisNameType.Z2 => GlobalParams.Z2DefaultSpeed,
                    AxisNameType.Theta => GlobalParams.ThetaDefaultSpeed,
                    _ => 1f // 默认值
                };
                await SetAbsoluteSpeedAsync(defaultSpeed);
                waitTime += (int)MathF.Ceiling(maxDistance / defaultSpeed);
            }
            absoluteStart.writeValue = "0";
            await keyencePlc.WriteTagAsync(absoluteStart);
            // 设置绝对运功开始
            absoluteStart.writeValue = "1";
            await keyencePlc.WriteTagAsync(absoluteStart);
            // 等待绝对运动完成
            await WaitAxisStopAsync(token.WithDefaultTimeout(TimeSpan.FromSeconds(waitTime)));
        }

        public async Task StartAbsoluteUseToJogAsync(float location, float? speed, CancellationToken token)
        {
            // 设置绝对运动位置
            absoluteLocation.writeValue = location.ToString();
            await keyencePlc.WriteTagAsync(absoluteLocation);
            if (speed != null)
            {
                // 设置绝对运动速度
                await SetAbsoluteSpeedAsync(speed.Value);
            }
            else
            {
                float defaultSpeed = axisName switch
                {
                    AxisNameType.X => GlobalParams.XDefaultSpeed,
                    AxisNameType.Y => GlobalParams.YDefaultSpeed,
                    AxisNameType.Z1 => GlobalParams.Z1DefaultSpeed,
                    AxisNameType.Z2 => GlobalParams.Z2DefaultSpeed,
                    AxisNameType.Theta => GlobalParams.ThetaDefaultSpeed,
                    _ => 1f // 默认值
                };
                await SetAbsoluteSpeedAsync(defaultSpeed);
            }
            absoluteStart.writeValue = "0";
            await keyencePlc.WriteTagAsync(absoluteStart);
            // 设置绝对运功开始
            absoluteStart.writeValue = "1";
            await keyencePlc.WriteTagAsync(absoluteStart);
            // 等待绝对运动完成
            await WaitStopJogAsync(token);
            await WaitAxisStopAsync(token);
        }

        /// <summary>
        /// 开始相对运动
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="speed"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task StartRelativeAsync(float distance, float? speed, CancellationToken token)
        {
            float? curLocation = await GetCurrentLocationAsync();
            if (curLocation != null)
            {
                await StartAbsoluteAsync(curLocation.Value + distance, speed, token);
            }
        }

        /// <summary>
        /// 开始相对运动
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="speed"></param>
        /// <returns></returns>
        public async Task StartRelativeUseToJogAsync(float distance, float? speed, CancellationToken token)
        {
            float? curLocation = await GetCurrentLocationAsync();
            if (curLocation != null)
            {
                await StartAbsoluteUseToJogAsync(curLocation.Value + distance, speed, token);
            }
        }

        /// <summary>
        /// 设置是否高速 0 低速 1 高速
        /// </summary>
        /// <param name="highFlag"></param>
        public async Task SetHighSpeedAsync(int highFlag)
        {
            highSpeed.writeValue = highFlag.ToString();
            await keyencePlc.WriteTagAsync(highSpeed);
        }
    }

    public class AxisNameType
    {
        public const string X = "X轴";
        public const string Y = "Y轴";
        public const string Z1 = "Z1轴";
        public const string Z2 = "Z2轴";
        public const string Theta = "Theta轴";
    }

    public class BladeMantance
    {
        public BladeMantance()
        {
        }

        private KeyencePlc keyencePlc = KeyencePlc.Instance;

        // ============刀片维护相关
        public Tag initReplaceLocation { get; set; }

        public Tag firstMeasureHight { get; set; }
        public Tag heightMeasurementEarlyEnd { get; set; }
        public Tag NoContactHeightMeasurement { get; set; }
        public Tag HeightMeasurementCompleted { get; set; }
        public Tag opticalFiberSensorBlowing { get; set; }
        public Tag opticalFiberSensorBlowingWater { get; set; }
        public Tag xReplaceLocation { get; set; }
        public Tag yReplaceLocation { get; set; }
        public Tag z1ReplaceLocation { get; set; }
        public Tag z2ReplaceLocation { get; set; }
        public Tag bladeSetup { get; set; }

        // 设置参数确认
        public Tag confirmParams { get; set; }

        // 是否已准备好测高
        public Tag bladeMantanceStatus { get; set; }

        // x轴测高位置设置
        public Tag xHeightSet { get; set; }

        // Y轴测高位置设置
        public Tag yHeightSet { get; set; }

        // Y轴测高位置设置
        public Tag z1HeightSet { get; set; }

        // Y轴测高位置设置
        public Tag z2HeightSet { get; set; }

        public Tag thetaSet { get; set; }

        // x轴基准线测量位置设置
        public Tag xBaseMeasurePos { get; set; }

        // Y轴基准线测量位置设置
        public Tag yBaseMeasurePos { get; set; }

        public Tag z1BaseMeasurePos { get; set; }
        public Tag z2BaseMeasurePos { get; set; }
        public Tag setupStart { get; set; }
        public Tag setupZAxisHighSpeed { get; set; }
        public Tag setupZAxisLowSpeed { get; set; }
        public Tag setupZAxisLowDistance { get; set; }
        public Tag setupSetNumber { get; set; }
        public Tag setupValue { get; set; }
        public Tag setupNumber { get; set; }
        public Tag measureHeightHighSpeed { get; set; }
        public Tag measureHeightSlowSpeed { get; set; }
        public Tag measureHeightSlowSpeedRanged { get; set; }

        // ============刀片维护相关 END

        public async Task SetMeasureHeightParams(float hightspeed, float slowspeed, float slowspeedranged)
        {
            // 设置测高高速
            measureHeightHighSpeed.writeValue = hightspeed.ToString();
            await keyencePlc.WriteTagAsync(measureHeightHighSpeed);
            // 设置测高低速
            measureHeightSlowSpeed.writeValue = slowspeed.ToString();
            await keyencePlc.WriteTagAsync(measureHeightSlowSpeed);
            // 设置测高低速距离
            measureHeightSlowSpeedRanged.writeValue = slowspeedranged.ToString();
            await keyencePlc.WriteTagAsync(measureHeightSlowSpeedRanged);
        }

        public async Task SetHeightMeasureTimes(int times)
        {
            setupSetNumber.writeValue = times.ToString();
            await keyencePlc.WriteTagAsync(setupSetNumber);
        }

        /// <summary>
        /// 开始测高
        /// </summary>
        public void StartSetup()
        {
            setupStart.writeValue = "1";
            keyencePlc.writeTag(setupStart);
        }

        /// <summary>
        /// 测高提前结束
        /// </summary>
        public async Task HeightMeasurementEarlyEndAsync()
        {
            heightMeasurementEarlyEnd.writeValue = "1";
            await keyencePlc.WriteTagAsync(heightMeasurementEarlyEnd);
        }

        /// <summary>
        /// 开始测高
        /// </summary>
        public async Task StartSetupAsync()
        {
            setupStart.writeValue = "1";
            await keyencePlc.WriteTagAsync(setupStart);
        }

        public async Task StartNoContactHeightMeasurement()
        {
            NoContactHeightMeasurement.writeValue = "0";
            await keyencePlc.WriteTagAsync(NoContactHeightMeasurement);
        }

        public async Task StartContactHeightMeasurement()
        {
            NoContactHeightMeasurement.writeValue = "1";
            await keyencePlc.WriteTagAsync(NoContactHeightMeasurement);
        }

        // 判断是否已准备好测高
        public static async Task<bool> IsReadyToMeasureHeightAsync()
        {
            return await PlcControl.plc.ReadDataAsync(PlcControl.tagControl.bladeMantance.bladeMantanceStatus.addr) ?? false;
        }

        /// <summary>
        /// 等待测高准备完成
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task WaitReadyToMeasureHeightAsync(CancellationToken token)
        {
            await TaskUtils.WaitExpectedResultAsync(IsReadyToMeasureHeightAsync, default, token);
        }

        /// <summary>
        /// 获取测高次数
        /// </summary>
        /// <returns></returns>
        public async Task<int?> GetHeightMeasureSetupNumberAsync()
        {
            return await PlcControl.plc.ReadDataAsync<int>(setupNumber.addr);
        }

        /// <summary>
        /// 等待测高次数更新
        /// </summary>
        /// <param name="preNum"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task WaitHeightMeasureSetupNumberUdatedAsync(int preNum, CancellationToken token)
        {
            await TaskUtils.WaitExpectedResultAsync(GetHeightMeasureSetupNumberAsync, preNum, default, token);
        }

        /// <summary>
        /// 获取测高值
        /// </summary>
        /// <returns></returns>
        public async Task<float?> GetHeightMeasurementSetupValue()
        {
            return await PlcControl.plc.ReadDataAsync<float>(setupValue.addr);
        }

        /// <summary>
        /// 测高是否完成
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsCompletedHeightMeasurementAsync()
        {
            return await PlcControl.plc.ReadDataAsync(HeightMeasurementCompleted.addr) == true;
        }

        /// <summary>
        /// 等待测高完成
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task WaitHeightMeasurementCompletedAsync(CancellationToken token)
        {
            await TaskUtils.WaitExpectedResultAsync(IsCompletedHeightMeasurementAsync, default, token);
        }

        /// <summary>
        /// 开启光纤传感器吹气
        /// </summary>
        /// <returns></returns>
        public async Task OpenOpticalFiberSensorBlowingAsync(int blowingseconds)
        {
            await OpenOpticalFiberSensorBlowingAsync();
            await Task.Delay(TimeSpan.FromSeconds(blowingseconds));
            await CloseOpticalFiberSensorBlowingAsync();
        }

        public async Task<bool> GetOpticalFiberSensorBlowingAsync()
        {
            return await PlcControl.plc.ReadDataAsync(opticalFiberSensorBlowing.addr) == true;
        }

        /// <summary>
        /// 开启光纤传感器吹气
        /// </summary>
        /// <returns></returns>
        public async Task OpenOpticalFiberSensorBlowingAsync()
        {
            opticalFiberSensorBlowing.writeValue = "1";
            await keyencePlc.WriteTagAsync(opticalFiberSensorBlowing);
        }

        /// <summary>
        /// 关闭光纤传感器吹气
        /// </summary>
        /// <returns></returns>
        public async Task CloseOpticalFiberSensorBlowingAsync()
        {
            opticalFiberSensorBlowing.writeValue = "0";
            await keyencePlc.WriteTagAsync(opticalFiberSensorBlowing);
        }

        /// <summary>
        /// 开启光纤传感器吹水
        /// </summary>
        /// <returns></returns>
        public async Task OpenOpticalFiberSensorBlowingWaterAsync(int blowingWaterSeconds)
        {
            await OpenOpticalFiberSensorBlowingWaterAsync();
            await Task.Delay(TimeSpan.FromSeconds(blowingWaterSeconds));
            await CloseOpticalFiberSensorBlowingWaterAsync();
        }

        public async Task<bool> GetOpticalFiberSensorBlowingWaterAsync()
        {
            return await PlcControl.plc.ReadDataAsync(opticalFiberSensorBlowingWater.addr) == true;
        }

        /// <summary>
        /// 开启光纤传感器吹水
        /// </summary>
        /// <returns></returns>
        public async Task OpenOpticalFiberSensorBlowingWaterAsync()
        {
            opticalFiberSensorBlowingWater.writeValue = "1";
            await keyencePlc.WriteTagAsync(opticalFiberSensorBlowingWater);
        }

        /// <summary>
        /// 关闭光纤传感器吹水
        /// </summary>
        /// <returns></returns>
        public async Task CloseOpticalFiberSensorBlowingWaterAsync()
        {
            opticalFiberSensorBlowingWater.writeValue = "0";
            await keyencePlc.WriteTagAsync(opticalFiberSensorBlowingWater);
        }

        /// <summary>
        /// 执行换刀
        /// </summary>
        public void RunBladeReplace(int status)
        {
            initReplaceLocation.writeValue = status + "";
            keyencePlc.writeTag(initReplaceLocation);
        }

        /// <summary>
        /// 执行换刀
        /// </summary>
        public async Task RunBladeReplaceAsync(int status)
        {
            initReplaceLocation.writeValue = status + "";
            await keyencePlc.WriteTagAsync(initReplaceLocation);
        }

        /// <summary>
        /// 第一次测高（用于获取初始电压值）
        /// </summary>
        public async Task SetFirstMeasureHight()
        {
            firstMeasureHight.writeValue = "0";
            await keyencePlc.WriteTagAsync(firstMeasureHight);
            firstMeasureHight.writeValue = "1";
            await keyencePlc.WriteTagAsync(firstMeasureHight);
        }

        /// <summary>
        /// 执行测高
        /// </summary>
        public void RunBladeSetup(int status)
        {
            bladeSetup.writeValue = status + "";
            keyencePlc.writeTag(bladeSetup);
        }

        /// <summary>
        /// 开始测高
        /// </summary>
        public async Task StartBladeSetupAsync()
        {
            //关闭切割水
            await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
            bladeSetup.writeValue = "0";
            await keyencePlc.WriteTagAsync(bladeSetup);
            bladeSetup.writeValue = "1";
            await keyencePlc.WriteTagAsync(bladeSetup);
        }

        /// <summary>
        /// 设置测高初始位置
        /// </summary>
        /// <param name="initX"></param>
        /// <param name="initY"></param>
        /// <param name="initZ1"></param>
        /// <param name="initZ2"></param>
        public void SetBladeSetuInitPosition(string initX, string initY, string initZ1, string initZ2)
        {
            xHeightSet.writeValue = initX.ToString();
            yHeightSet.writeValue = initY.ToString();
            z1HeightSet.writeValue = initZ1.ToString();
            z2HeightSet.writeValue = initZ2.ToString();
            keyencePlc.writeTag(xHeightSet);
            Thread.Sleep(10);
            keyencePlc.writeTag(yHeightSet);
            Thread.Sleep(10);
            keyencePlc.writeTag(z1HeightSet);
            Thread.Sleep(10);
            keyencePlc.writeTag(z2HeightSet);
            Thread.Sleep(10);
            // 确认设置参数
            ConfirmParams();
        }

        public async Task SetBladeSetuInitPositionAsync(string initX, string initY, string initZ1, float? initTheta = null)
        {
            xHeightSet.writeValue = initX.ToString();
            await keyencePlc.WriteTagAsync(xHeightSet);
            yHeightSet.writeValue = initY.ToString();
            await keyencePlc.WriteTagAsync(yHeightSet);
            z1HeightSet.writeValue = initZ1.ToString();
            await keyencePlc.WriteTagAsync(z1HeightSet);
            if (initTheta is not null)
            {
                thetaSet.writeValue = initTheta.Value.ToString();
                await keyencePlc.WriteTagAsync(thetaSet);
            }
        }

        /// <summary>
        /// 设置基准线校准初始位置
        /// </summary>
        /// <param name="initX"></param>
        /// <param name="initY"></param>
        /// <param name="initZ1"></param>
        /// <param name="initZ2"></param>
        public void SetBaseMeasureInitPosition(string initX, string initY, string initZ1, string initZ2)
        {
            xBaseMeasurePos.writeValue = initX.ToString();
            yBaseMeasurePos.writeValue = initY.ToString();
            z1BaseMeasurePos.writeValue = initZ1.ToString();
            z2BaseMeasurePos.writeValue = initZ2.ToString();
            keyencePlc.writeTag(xBaseMeasurePos);
            Thread.Sleep(10);
            keyencePlc.writeTag(yBaseMeasurePos);
            Thread.Sleep(10);
            keyencePlc.writeTag(z1BaseMeasurePos);
            Thread.Sleep(10);
            keyencePlc.writeTag(z2BaseMeasurePos);
            Thread.Sleep(10);
            // 确认设置参数
            ConfirmParams();
        }

        /// <summary>
        /// 设置刀片更换初始位置
        /// </summary>
        /// <param name="initX"></param>
        /// <param name="initY"></param>
        /// <param name="initZ1"></param>
        /// <param name="initZ2"></param>
        public void SetCutReplaceInitPosition(string initX, string initY, string initZ1, string initZ2)
        {
            xReplaceLocation.writeValue = initX.ToString();
            yReplaceLocation.writeValue = initY.ToString();
            z1ReplaceLocation.writeValue = initZ1.ToString();
            z2ReplaceLocation.writeValue = initZ2.ToString();
            keyencePlc.writeTag(xReplaceLocation);
            Thread.Sleep(10);
            keyencePlc.writeTag(yReplaceLocation);
            Thread.Sleep(10);
            keyencePlc.writeTag(z1ReplaceLocation);
            Thread.Sleep(10);
            keyencePlc.writeTag(z2ReplaceLocation);// 确认设置参数
            Thread.Sleep(10);
            ConfirmParams();
        }

        /// <summary>
        /// 确认参数
        /// </summary>
        public void ConfirmParams()
        {
            confirmParams.writeValue = "1";
            keyencePlc.writeTag(confirmParams);
        }
    }

    public class WholeDevice
    {
        // ============整机相关==========
        public WholeDevice()
        {
        }

        private KeyencePlc keyencePlc = KeyencePlc.Instance;

        public Tag canSystemInit { get; set; }
        public Tag debugMode { get; set; }
        public Tag systemInit { get; set; }
        public Tag urgentRaise { get; set; }
        public Tag isSystemIniting { get; set; }
        public Tag systemReset { get; set; }
        public Tag systemInitStatus { get; set; }
        public Tag systemStop { get; set; }
        public Tag clearAxisAlarm { get; set; }
        public Tag cutSecurityDoor { get; set; }
        public Tag alarmReset { get; set; }
        public Tag cameraSecurityDoor { get; set; }
        public Tag cutSecurityDoorStatus { get; set; }
        public Tag cameraSecurityDoorStatus { get; set; }
        public Tag vacuumState { get; set; }
        public Tag spindleAir { get; set; }
        public Tag spindleCuttingWater { get; set; }
        public Tag spindleCoolingWater { get; set; }
        public Tag screwOil { get; set; }
        public Tag spindleSpeedStatus { get; set; }
        public Tag vacuumSwitch { get; set; }
        public Tag vacuumSwitchStatus { get; set; }

        // 工作盘真空
        public Tag workVacuumSwitch { get; set; }

        // 主轴切割水
        public Tag cuttingWater { get; set; }

        // 工件吹气
        public Tag workpieceBlowing { get; set; }

        // 工件吹气状态
        public Tag workpieceBlowingStatus { get; set; }

        // 确认系统报警
        public Tag clearSystemAlarm { get; set; }

        public Tag systemErrorClear { get; set; }

        // 手动运行轴
        public Tag spindleManuallyRun { get; set; }

        public Tag spindleManuallyRunStatus { get; set; }

        // 轴错误报警已解除
        public Tag systemErrorReset { get; set; }

        // 面板按钮
        public Tag panelButtons { get; set; }

        // 是否开启插补 1 开启 0 关闭
        public Tag interpositionStatus { get; set; }

        // 油泵计数
        public Tag refuelingPumpCount { get; set; }

        // 油泵计数清零
        public Tag refuelingPumpReset { get; set; }

        // 蜂鸣
        public Tag buzzer { get; set; }

        // 绿灯闪
        public Tag greenLightFlash { get; set; }

        // 黄灯闪
        public Tag yellowLightFlash { get; set; }

        // 红灯闪
        public Tag redLightFlash { get; set; }

        // IO检测模式
        public Tag ioModel { get; set; }

        public Tag temperatureSensor1 { get; set; }

        public Tag temperatureSensor2 { get; set; }

        public Tag temperatureSensor3 { get; set; }

        public Tag temperatureSensor4 { get; set; }

        public Tag temperatureSensor5 { get; set; }

        public Tag temperatureSensor6 { get; set; }

        public Tag temperatureSensor7 { get; set; }

        public Tag spindleDirectionSwitch { get; set; }

        // ============整机相关 END==========

        public async Task TriggerSpindleDirection()
        {
            if (await PlcControl.tagControl.wholeDevice.GetSpindleSpeedAsync() == 0)
            {
                bool result = await PlcControl.plc.ReadDataAsync(spindleDirectionSwitch.addr) == true;
                spindleDirectionSwitch.writeValue = result ? "0" : "1";
                await keyencePlc.WriteTagAsync(spindleDirectionSwitch);
            }
        }

        /// <summary>
        /// 读取多个温度传感器
        /// </summary>
        /// <returns></returns>
        public async Task<float[]?> GetTemperatureSensorsAsync()
        {
            var result = await PlcControl.plc.ReadDataAsync<short>(temperatureSensor1.addr, 5);
            if (result != null && result.Length == 5)
            {
                return result.Select(p => p / 10.0f).ToArray();
            }
            return null;
        }

        /// <summary>
        /// 能否执行系统初始化
        /// </summary>
        public async Task<bool> CanSystemInitAsync()
        {
            return await PlcControl.plc.ReadDataAsync(canSystemInit.addr) == true;
        }

        /// <summary>
        /// 执行系统初始化
        /// </summary>
        public async Task SystemInitAsync()
        {
            systemInit.writeValue = "0";
            await keyencePlc.WriteTagAsync(systemInit);
            systemInit.writeValue = "1";
            await keyencePlc.WriteTagAsync(systemInit);
        }

        public async Task OpenDebugModeAsync()
        {
            debugMode.writeValue = "1";
            await keyencePlc.WriteTagAsync(debugMode);
        }

        public async Task CloseDebugModeAsync()
        {
            debugMode.writeValue = "0";
            await keyencePlc.WriteTagAsync(debugMode);
        }

        /// <summary>
        /// 紧急抬起主轴
        /// </summary>
        public async Task UrgentRaise()
        {
            urgentRaise.writeValue = "0";
            await keyencePlc.WriteTagAsync(urgentRaise);
            urgentRaise.writeValue = "1";
            await keyencePlc.WriteTagAsync(urgentRaise);
        }

        /// <summary>
        /// 是否系统初始化中
        /// </summary>
        public async Task<bool> IsSystemInitingAsync()
        {
            return await PlcControl.plc.ReadDataAsync(isSystemIniting.addr) == true;
        }

        /// <summary>
        /// 系统初始化是否完成
        /// </summary>
        public async Task<bool> IsCompletedSystemInitAsync()
        {
            return await PlcControl.plc.ReadDataAsync(systemInitStatus.addr) == true;
        }

        /// <summary>
        /// 等待系统初始化完成
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task WaitSystemInitCompletedAsync(CancellationToken token)
        {
            await TaskUtils.WaitExpectedResultAsync(IsCompletedSystemInitAsync, default, token);
        }

        public async Task AlarmResetAsync()
        {
            alarmReset.writeValue = "0";
            await keyencePlc.WriteTagAsync(alarmReset);
            alarmReset.writeValue = "1";
            await keyencePlc.WriteTagAsync(alarmReset);
        }

        /// <summary>
        /// 获取主轴转速
        /// </summary>
        /// <returns></returns>
        public async Task<int?> GetSpindleSpeedAsync()
        {
            return await keyencePlc.ReadDataAsync<int>(spindleSpeedStatus.addr);
        }

        /// <summary>
        /// 等待主轴转速为0
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task WaitSpindleSpeedToZeroAsync(CancellationToken token = default)
        {
            await TaskUtils.WaitExpectedResultAsync(GetSpindleSpeedAsync, 0, default, token);
        }

        /// <summary>
        /// IO模式设置
        /// </summary>
        public void IoModelSet(int status)
        {
            ioModel.writeValue = status.ToString();
            keyencePlc.writeTag(ioModel);
        }

        /// <summary>
        /// 是否开启插补运动
        /// </summary>
        /// <param name="status">0 关闭 1 开启</param>
        public void SetInterpositionStatus(int status)
        {
            interpositionStatus.writeValue = status + "";
            keyencePlc.writeTag(interpositionStatus);
        }

        /// <summary>
        /// 整机复位
        /// </summary>
        public void SystemReset()
        {
            systemReset.writeValue = "1";
            keyencePlc.writeTag(systemReset);
        }

        /// <summary>
        /// 启动主轴
        /// </summary>
        /// <returns></returns>
        public async Task StartSpindleAsync()
        {
            if (await PlcControl.tagControl.wholeDevice.GetSpindleSpeedAsync() == 0)
            {
                await PlcControl.tagControl.wholeDevice.TriggerSpindleManuallyRunAsync();
            }
        }

        /// <summary>
        /// 停止主轴
        /// </summary>
        /// <returns></returns>
        public async Task StopSpindleAsync()
        {
            if (await PlcControl.tagControl.wholeDevice.GetSpindleSpeedAsync() != 0)
            {
                await PlcControl.tagControl.wholeDevice.TriggerSpindleManuallyRunAsync();
            }
        }

        /// <summary>
        /// 主轴触发手动运行
        /// </summary>
        public async Task TriggerSpindleManuallyRunAsync()
        {
            spindleManuallyRun.writeValue = "0";
            await keyencePlc.WriteTagAsync(spindleManuallyRun);
            spindleManuallyRun.writeValue = "1";
            await keyencePlc.WriteTagAsync(spindleManuallyRun);
        }

        public async Task<bool> IsSpindleStopAsync()
        {
            bool? isRun = await PlcControl.plc.ReadDataAsync(spindleManuallyRunStatus.addr);
            return isRun == false;
        }

        /// <summary>
        /// 设置面板按钮状态
        /// </summary>
        public void SetPanelButtonsStauts(int status)
        {
            panelButtons.writeValue = status + "";
            keyencePlc.writeTag(panelButtons);
        }

        /// <summary>
        /// 异常清除
        /// </summary>
        public void SystemErrorClear()
        {
            clearSystemAlarm.writeValue = "1";
            keyencePlc.writeTag(clearSystemAlarm);
            // 清除后 读取哪个轴报警，则往范围内走一点
        }

        /// <summary>
        /// 是否开启切割水
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsOpenSpindleCuttingWaterAsync()
        {
            return await keyencePlc.ReadDataAsync(spindleCuttingWater.addr) == true;
        }

        public async Task OpenCuttingWaterAndConfirmStatusAsync(CancellationToken token)
        {
            await TaskUtils.WaitExpectedResultAsync(async () =>
            {
                await OpenCuttingWaterAsync();
                await Task.Delay(1000);
                return await IsOpenSpindleCuttingWaterAsync();
            }, default, token);
        }

        /// <summary>
        /// 打开切割水
        /// </summary>
        public async Task OpenCuttingWaterAsync()
        {
            cuttingWater.writeValue = "1";
            await keyencePlc.WriteTagAsync(cuttingWater);
        }

        /// <summary>
        /// 关闭切割水
        /// </summary>
        public async Task CloseCuttingWaterAsync()
        {
            cuttingWater.writeValue = "0";
            await keyencePlc.WriteTagAsync(cuttingWater);
        }

        public async Task<bool> IsOpenCuttingWaterAsync()
        {
            return await keyencePlc.ReadDataAsync(cuttingWater.addr) == true;
        }

        /// <summary>
        /// 切割水
        /// </summary>
        public async Task TriggerCuttingWaterAsync()
        {
            if (await PlcControl.tagControl.wholeDevice.IsOpenCuttingWaterAsync())
            {
                await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
            }
            else
            {
                await PlcControl.tagControl.wholeDevice.OpenCuttingWaterAsync();
            }
        }

        /// <summary>
        /// 打开或关闭工作盘真空
        /// </summary>
        public void SetWorkVacuumSwitch()
        {
            workVacuumSwitch.writeValue = "1";
            keyencePlc.writeTag(workVacuumSwitch);
        }

        /// <summary>
        /// 打开工作盘真空
        /// </summary>
        public async Task OpenWorkVacuumSwitchAsync()
        {
            workVacuumSwitch.writeValue = "1";
            await keyencePlc.WriteTagAsync(workVacuumSwitch);
        }

        /// <summary>
        /// 关闭工作盘真空
        /// </summary>
        public async Task CloseWorkVacuumSwitchAsync()
        {
            workVacuumSwitch.writeValue = "0";
            await keyencePlc.WriteTagAsync(workVacuumSwitch);
        }

        /// <summary>
        /// 工作盘是否打开真空
        /// </summary>
        public async Task<bool> IsOpenWorkVacuumSwitchAsync()
        {
            return await keyencePlc.ReadDataAsync(workVacuumSwitch.addr) == true;
        }

        /// <summary>
        /// 操作工件吹气
        /// </summary>
        public async Task TriggerWorkVacuumSwitchAsync()
        {
            if (await PlcControl.tagControl.wholeDevice.IsOpenWorkVacuumSwitchAsync())
            {
                await PlcControl.tagControl.wholeDevice.CloseWorkVacuumSwitchAsync();
            }
            else
            {
                await PlcControl.tagControl.wholeDevice.OpenWorkVacuumSwitchAsync();
            }
        }

        /// <summary>
        /// 设置工件吹气打开
        /// </summary>
        public async Task OpenWorkpieceBlowingAsync()
        {
            workpieceBlowing.writeValue = "1";
            await keyencePlc.WriteTagAsync(workpieceBlowing);
        }

        /// <summary>
        /// 设置工件吹气关闭
        /// </summary>
        public async Task CloseWorkpieceBlowingAsync()
        {
            workpieceBlowing.writeValue = "0";
            await keyencePlc.WriteTagAsync(workpieceBlowing);
        }

        /// <summary>
        /// 是否打开工件吹气
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsOpenWorkpieceBlowingAsync()
        {
            return await keyencePlc.ReadDataAsync(workpieceBlowingStatus.addr) == true;
        }

        /// <summary>
        /// 是否打开切割安全门
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsOpenCutSecurityDoorAsync()
        {
            return await keyencePlc.ReadDataAsync(cutSecurityDoorStatus.addr) == false;
        }

        /// <summary>
        /// 获取切割安全门地址信息
        /// </summary>
        /// <returns></returns>
        public async Task<bool> GetCutSecurityDoorAddressAsync()
        {
            return await keyencePlc.ReadDataAsync(cutSecurityDoor.addr) == false;
        }

        public async Task OpenCutSecurityDoorAsync()
        {
            if (await GetSpindleSpeedAsync() != 0)
            {
                MaterialSnack("打开安全门失败，主轴未停止！", SnackType.WARNING, 0);
                return;
            }
            cutSecurityDoor.writeValue = "0";
            await keyencePlc.WriteTagAsync(cutSecurityDoor);
        }

        public async Task CloseCutSecurityDoorAsync()
        {
            cutSecurityDoor.writeValue = "1";
            await keyencePlc.WriteTagAsync(cutSecurityDoor);
        }

        public async Task OpenCameraLensCapAsync()
        {
            cutSecurityDoor.writeValue = "1";
            await keyencePlc.WriteTagAsync(cutSecurityDoor);
        }

        public async Task CloseCameraLensCapAsync()
        {
            cutSecurityDoor.writeValue = "0";
            await keyencePlc.WriteTagAsync(cutSecurityDoor);
        }

        /// <summary>
        /// 是否打开相机安全门
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsOpenCameraSecurityDoorAsync()
        {
            return await keyencePlc.ReadDataAsync(cameraSecurityDoorStatus.addr) == false;
        }

        /// <summary>
        /// 获取相机安全门地址信息
        /// </summary>
        /// <returns></returns>
        public async Task<bool> GetCameraSecurityDoorAddressAsync()
        {
            return await keyencePlc.ReadDataAsync(cameraSecurityDoor.addr) == false;
        }

        public async Task UnlockCameraSecurityDoorAsync()
        {
            cameraSecurityDoor.writeValue = "1";
            await keyencePlc.WriteTagAsync(cameraSecurityDoor);
        }

        public async Task LockCameraSecurityDoorAsync()
        {
            cameraSecurityDoor.writeValue = "0";
            await keyencePlc.WriteTagAsync(cameraSecurityDoor);
        }

        /// <summary>
        /// 工件真空状态
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsOpenVacuumSwitchAsync()
        {
            return await keyencePlc.ReadDataAsync(vacuumState.addr) == true;
        }

        /// <summary>
        /// 主轴气源
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsOpenSpindleAirAsync()
        {
            return await keyencePlc.ReadDataAsync(spindleAir.addr) == true;
        }

        /// <summary>
        /// 主轴冷却水
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsOpenSpindleCoolingWaterAsync()
        {
            return await keyencePlc.ReadDataAsync(spindleCoolingWater.addr) == true;
        }

        //public async Task OpenVacuumSwitchAsync()
        //{
        //    vacuumSwitch.writeValue = "1";
        //    await keyencePlc.WriteTagAsync(vacuumSwitch);
        //}

        //public async Task CloseVacuumSwitchAsync()
        //{
        //    vacuumSwitch.writeValue = "0";
        //    await keyencePlc.WriteTagAsync(vacuumSwitch);
        //}

        public async Task TriggerVacuumSwitchAsync()
        {
            vacuumSwitch.writeValue = "0";
            await keyencePlc.WriteTagAsync(vacuumSwitch);
            vacuumSwitch.writeValue = "1";
            await keyencePlc.WriteTagAsync(vacuumSwitch);
        }

        /// <summary>
        /// 读取所有报警状态
        /// </summary>
        /// <returns></returns>
        public async Task<bool[]?> ReadTotalAlarmsAsync()
        {
            return await PlcControl.plc.ReadDataAsync(AlarmConfig.Instance.StartAddress, (ushort)AlarmConfig.Instance.TotalAlarmCount);
        }

        /// <summary>
        /// 设置油泵计数清零
        /// </summary>
        /// <param name="status">0 关 1 开</param>
        public void SetRefuelingPumpReset(int status)
        {
            refuelingPumpReset.writeValue = status + "";
            keyencePlc.writeTag(refuelingPumpReset);
        }

        /// <summary>
        /// 设置蜂鸣
        /// </summary>
        /// <param name="status">0 关 1 开</param>
        public void SetBuzzerStatus(int status)
        {
            buzzer.writeValue = status + "";
            keyencePlc.writeTag(buzzer);
        }

        /// <summary>
        /// 打开蜂鸣器
        /// </summary>
        /// <returns></returns>
        public async Task OpenBuzzerAsync(int seconds)
        {
            await OpenBuzzerAsync();
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            await CloseBuzzerAsync();
        }

        /// <summary>
        /// 打开蜂鸣器
        /// </summary>
        /// <returns></returns>
        public async Task OpenBuzzerAsync()
        {
            buzzer.writeValue = "1";
            await keyencePlc.WriteTagAsync(buzzer);
        }

        /// <summary>
        /// 关闭蜂鸣器
        /// </summary>
        /// <returns></returns>
        public async Task CloseBuzzerAsync()
        {
            buzzer.writeValue = "0";
            await keyencePlc.WriteTagAsync(buzzer);
        }

        private SemaphoreSlim _lightSemaph = new SemaphoreSlim(1, 1);

        /// <summary>
        /// 打开绿灯闪烁
        /// </summary>
        /// <returns></returns>
        public async Task OpenGreenLightAsync()
        {
            await _lightSemaph.ExecuteAsync(async () =>
            {
                greenLightFlash.writeValue = "1";
                await keyencePlc.WriteTagAsync(greenLightFlash);
                yellowLightFlash.writeValue = "0";
                await keyencePlc.WriteTagAsync(yellowLightFlash);
                redLightFlash.writeValue = "0";
                await keyencePlc.WriteTagAsync(redLightFlash);
            });
        }

        /// <summary>
        /// 打开黄灯闪烁
        /// </summary>
        /// <returns></returns>
        public async Task OpenYellowLightAsync()
        {
            await _lightSemaph.ExecuteAsync(async () =>
            {
                greenLightFlash.writeValue = "0";
                await keyencePlc.WriteTagAsync(greenLightFlash);
                yellowLightFlash.writeValue = "1";
                await keyencePlc.WriteTagAsync(yellowLightFlash);
                redLightFlash.writeValue = "0";
                await keyencePlc.WriteTagAsync(redLightFlash);
            });
        }

        /// <summary>
        /// 打开红灯闪烁
        /// </summary>
        /// <returns></returns>
        public async Task OpenRedLightAsync()
        {
            await _lightSemaph.ExecuteAsync(async () =>
            {
                greenLightFlash.writeValue = "0";
                await keyencePlc.WriteTagAsync(greenLightFlash);
                yellowLightFlash.writeValue = "0";
                await keyencePlc.WriteTagAsync(yellowLightFlash);
                redLightFlash.writeValue = "1";
                await keyencePlc.WriteTagAsync(redLightFlash);
            });
        }

        /// <summary>
        /// 关闭所有灯闪烁
        /// </summary>
        /// <returns></returns>
        public async Task CloseAllLightAsync()
        {
            greenLightFlash.writeValue = "0";
            await keyencePlc.WriteTagAsync(greenLightFlash);
            yellowLightFlash.writeValue = "0";
            await keyencePlc.WriteTagAsync(yellowLightFlash);
            redLightFlash.writeValue = "0";
            await keyencePlc.WriteTagAsync(redLightFlash);
        }
    }

    public class Calibration
    {
        public Calibration()
        {
        }

        private KeyencePlc keyencePlc = KeyencePlc.Instance;

        // ============校准 ===============
        // 进入校准画面
        public Tag alignInit { get; set; }

        public Tag alignStatus { get; set; }

        // x轴校准初始设置
        public Tag alignInitX { get; set; }

        // y轴校准初始设置
        public Tag alignInitY { get; set; }

        // z1轴校准初始设置
        public Tag alignInitZ1 { get; set; }

        // z2轴校准初始设置
        public Tag alignInitZ2 { get; set; }

        // 设置参数确认
        public Tag confirmParams { get; set; }

        // ============校准结束===========

        /// <summary>
        /// 设置校准初始位置
        /// </summary>
        /// <param name="initX"></param>
        /// <param name="initY"></param>
        /// <param name="initZ1"></param>
        /// <param name="initZ2"></param>
        public void SetAlignInitPosition(string initX, string initY, string initZ1, string initZ2)
        {
            alignInitX.writeValue = initX.ToString();
            alignInitY.writeValue = initY.ToString();
            alignInitZ1.writeValue = initZ1.ToString();
            alignInitZ2.writeValue = initZ2.ToString();
            keyencePlc.writeTag(alignInitX);
            Thread.Sleep(10);
            keyencePlc.writeTag(alignInitY);
            Thread.Sleep(10);
            keyencePlc.writeTag(alignInitZ1);
            Thread.Sleep(10);
            keyencePlc.writeTag(alignInitZ2);
            Thread.Sleep(10);
            // 确认设置参数
            ConfirmParams();
        }

        /// <summary>
        /// 确认参数
        /// </summary>
        public void ConfirmParams()
        {
            confirmParams.writeValue = "1";
            keyencePlc.writeTag(confirmParams);
        }
    }

    public class Cutting
    {
        public Cutting()
        { }

        private KeyencePlc keyencePlc = KeyencePlc.Instance;

        // ================切割相关
        // 自动切割画面进入
        public Tag fullAutoInit { get; set; }

        // 是否已准备好进入切割模式
        public Tag isReadyToCutting { get; set; }

        // 切割设置参数确认
        public Tag confirmParams { get; set; }

        // 切割设置参数确认状态
        public Tag confirmParamsStatus { get; set; }

        // 切割方向-前切
        public Tag cutDirectionAgo { get; set; }

        // 切割方向-后切
        public Tag cutDirectionAfter { get; set; }

        // 切割方式 A-0 从左往右切（默认）  B_ZKEEP-1
        public Tag cutMethod { get; set; }

        // Y 轴是否开启光栅尺补偿 False 补偿 True 不补偿
        public Tag yAxisCompStatus { get; set; }

        // Z1 轴是否开启光栅尺补偿 False 补偿 True 不补偿
        public Tag z1AxisCompStatus { get; set; }

        // 切割开始
        public Tag cutStart { get; set; }

        // 切割停止
        public Tag cutStop { get; set; }

        // X轴切割开始位置
        public Tag xStartPosition { get; set; }

        // Y轴切割开始位置
        public Tag yStartPosition { get; set; }

        // z1轴切割开始位置
        public Tag z1StartPosition { get; set; }

        // 主轴速度
        public Tag spindleRev { get; set; }

        // 切割次数
        public Tag cutNum { get; set; }

        // 自动切割结束
        public Tag fullAutoCutEnd { get; set; }

        // 是否退出切割模式
        public Tag isExitCuttingMode { get; set; }

        // 停机检查
        public Tag shutdownCheck { get; set; }

        // Z1轴结束位置（切割位置）
        public Tag z1EndPosition { get; set; }

        // 自动当前切割面角度
        public Tag cutFaceAngle { get; set; }

        // 是否进入切割模式
        public Tag isEnterCuttingMode { get; set; }

        // X轴切割结束位置
        public Tag xLength { get; set; }

        // X轴初始位置
        public Tag xInitPosition { get; set; }

        // Y轴初始位置
        public Tag yInitPosition { get; set; }

        // Z1轴初始位置
        public Tag z1InitPosition { get; set; }

        // x轴停机检查位置
        public Tag xStopLocation { get; set; }

        // y轴停机检查位置
        public Tag yStopLocation { get; set; }

        // z2轴停机检查位置
        public Tag z2StopLocation { get; set; }

        // 切割返回速度
        public Tag returnSpeed { get; set; }

        // 切割停止延时检查
        public Tag cutStopDelayTime { get; set; }

        // 差步运动开始
        public Tag startInterpolationMotion { get; set; }

        // 差步运动是否完成
        public Tag isCompleteInterpolationMotion { get; set; }

        // 差步X运动位置
        public Tag xInterpolationMotion { get; set; }

        // 差步Y轴运动位置
        public Tag yInterpolationMotion { get; set; }

        // 当前切入深度
        public Tag currentDepthEntry { get; set; }

        public Tag flangeRepairBegins { get; set; }
        public Tag detectedSparks { get; set; }
        public Tag spindleRevReach { get; set; }
        public Tag instructionPositionY { get; set; }
        public Tag averagePositionY { get; set; }
        public Tag instructionPositionZ1 { get; set; }
        public Tag averagePositionZ1 { get; set; }
        public Tag isReadyCuttingData { get; set; }

        private async Task<bool> IsReadyCuttingDataAsync()
        {
            return await keyencePlc.ReadDataAsync(isReadyCuttingData.addr) == true;
        }

        public async Task SetIsReadyCuttingDataAsync(bool isReady)
        {
            isReadyCuttingData.writeValue = (isReady ? 1 : 0).ToString();
            await keyencePlc.WriteTagAsync(isReadyCuttingData);
        }

        public async Task WaitReadyCuttingDataAsyncAsync(CancellationToken token)
        {
            await TaskUtils.WaitExpectedResultAsync(IsReadyCuttingDataAsync, default, token);
        }

        public async Task<(double instructionPositionY, double averagePositionY, double instructionPositionZ1, double averagePositionZ1)> GetCuttingDataAsync()
        {
            var insY = await keyencePlc.ReadDataAsync<double>(instructionPositionY.addr) ?? 0;
            var aveY = await keyencePlc.ReadDataAsync<double>(averagePositionY.addr) ?? 0;
            var insZ1 = await keyencePlc.ReadDataAsync<double>(instructionPositionZ1.addr) ?? 0;
            var aveZ1 = await keyencePlc.ReadDataAsync<double>(averagePositionZ1.addr) ?? 0;
            return (insY, aveY, insZ1, aveZ1);
        }

        /// <summary>
        /// 执行插补运动
        /// </summary>
        /// <param name="xInterpolationMotionValue"></param>
        /// <param name="yInterpolationMotionValue"></param>
        /// <returns></returns>
        public async Task RunMotionAsync(float xInterpolationMotionValue, float yInterpolationMotionValue, CancellationToken token)
        {
            if (!GlobalParams.OnlineFlag) return;
            CancellationToken useToken = token.WithDefaultTimeout();
            Task waitX = PlcControl.tagControl.Xaxis.WaitAxisStopAsync(useToken);
            Task waitY = PlcControl.tagControl.Yaxis.WaitAxisStopAsync(useToken);
            await Task.WhenAll(waitX, waitY);
            xInterpolationMotion.writeValue = xInterpolationMotionValue.ToString();
            await keyencePlc.WriteTagAsync(xInterpolationMotion);
            yInterpolationMotion.writeValue = yInterpolationMotionValue.ToString();
            await keyencePlc.WriteTagAsync(yInterpolationMotion);
            startInterpolationMotion.writeValue = "0";
            await keyencePlc.WriteTagAsync(startInterpolationMotion);
            startInterpolationMotion.writeValue = "1";
            await keyencePlc.WriteTagAsync(startInterpolationMotion);
            await WaitInterpolationMotionCompletedAsync(token.WithDefaultTimeout(TimeSpan.FromSeconds(5)));
        }

        public async Task RunMotionNoWaitAsync(float xInterpolationMotionValue, float yInterpolationMotionValue)
        {
            xInterpolationMotion.writeValue = xInterpolationMotionValue.ToString();
            await keyencePlc.WriteTagAsync(xInterpolationMotion);
            yInterpolationMotion.writeValue = yInterpolationMotionValue.ToString();
            await keyencePlc.WriteTagAsync(yInterpolationMotion);
            startInterpolationMotion.writeValue = "0";
            await keyencePlc.WriteTagAsync(startInterpolationMotion);
            startInterpolationMotion.writeValue = "1";
            await keyencePlc.WriteTagAsync(startInterpolationMotion);
        }

        /// <summary>
        /// xy插补运动是否完成
        /// </summary>
        /// <returns></returns>
        private async Task<bool> IsCompleteInterpolationMotionAsync()
        {
            return await keyencePlc.ReadDataAsync(isCompleteInterpolationMotion.addr) == true;
        }

        /// <summary>
        /// 等待xy插补运动完成
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task WaitInterpolationMotionCompletedAsync(CancellationToken token)
        {
            await TaskUtils.WaitExpectedResultAsync(IsCompleteInterpolationMotionAsync, default, token);
        }

        /// <summary>
        /// 是否已准备好进入切割模式
        /// </summary>
        public async Task<bool> IsReadyToCuttingAsync()
        {
            return await keyencePlc.ReadDataAsync(isReadyToCutting.addr) == true;
        }

        public async Task WaitReadyToCuttingAsync(CancellationToken token)
        {
            await TaskUtils.WaitExpectedResultAsync(IsReadyToCuttingAsync, default, token);
        }

        /// <summary>
        /// 进入全自动切割
        /// </summary>
        public void EnterFullAutoInit(int status)
        {
            fullAutoInit.writeValue = status.ToString();
            keyencePlc.writeTag(fullAutoInit);
        }

        /// <summary>
        /// 进入全自动切割异步
        /// </summary>
        public async Task EnterCuttingModeAsync(CancellationToken token)
        {
            await WaitReadyToCuttingAsync(token);
            fullAutoInit.writeValue = "0";
            await keyencePlc.WriteTagAsync(fullAutoInit);
            fullAutoInit.writeValue = "1";
            await keyencePlc.WriteTagAsync(fullAutoInit);
            await WaitEnterCuttingModeAsync(token);
        }

        /// <summary>
        /// 退出全自动切割异步
        /// </summary>
        public async Task ExitCuttingModeAsync(CancellationToken token)
        {
            fullAutoCutEnd.writeValue = "0";
            await keyencePlc.WriteTagAsync(fullAutoCutEnd);
            fullAutoCutEnd.writeValue = "1";
            await keyencePlc.WriteTagAsync(fullAutoCutEnd);
            await WaitExitCuttingModeAsync(token);
        }

        /// <summary>
        /// 是否退出切割模式
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsExitCuttingModeAsync()
        {
            return await keyencePlc.ReadDataAsync(isExitCuttingMode.addr) ?? true;
        }

        /// <summary>
        /// 等待退出切割模式
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task WaitExitCuttingModeAsync(CancellationToken token)
        {
            await TaskUtils.WaitExpectedResultAsync(IsExitCuttingModeAsync, default, token);
        }

        /// <summary>
        /// 是否进入切割模式
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsEnterCuttingModeAsync()
        {
            return await keyencePlc.ReadDataAsync(isEnterCuttingMode.addr) ?? true;
        }

        /// <summary>
        /// 等待进入切割模式
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task WaitEnterCuttingModeAsync(CancellationToken token)
        {
            await TaskUtils.WaitExpectedResultAsync(IsEnterCuttingModeAsync, default, token);
        }

        /// <summary>
        /// 开始切割
        /// </summary>
        /// <param name="status">0 停止 1 开始</param>
        public void StartCut(int status)
        {
            cutStart.writeValue = status.ToString();
            keyencePlc.writeTag(cutStart);
        }

        /// <summary>
        /// 开始切割
        /// </summary>
        public async Task StartCutAsync()
        {
            cutStart.writeValue = "0";
            await keyencePlc.WriteTagAsync(cutStart);
            cutStart.writeValue = "1";
            await keyencePlc.WriteTagAsync(cutStart);
        }

        /// <summary>
        /// 获取切割次数
        /// </summary>
        /// <returns></returns>
        public async Task<int?> GetCutNumAsync()
        {
            return await keyencePlc.ReadDataAsync<int>(cutNum.addr);
        }

        /// <summary>
        /// 等待切割次数更新
        /// </summary>
        /// <param name="preCutNum"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task WaitCutNumUdatedAsync(int preCutNum, CancellationToken token)
        {
            await TaskUtils.WaitExpectedResultAsync(GetCutNumAsync, preCutNum, default, token);
        }

        //public async Task WaitCutNumUdatedAsync(int preCutNum, int timeoutSeconds = 30)
        //{
        //    await TaskUtils.WaitExpectedResultAsync(GetCutNumAsync, preCutNum, TimeSpan.FromSeconds(timeoutSeconds));
        //}

        /// <summary>
        /// 切割方法
        /// </summary>
        /// <param name="cutMethod">0 默认-从左往右切  1 B_ZKEEP</param>
        public void StartCutMethod(int cutMethodValue)
        {
            cutMethod.writeValue = cutMethodValue.ToString();
            keyencePlc.writeTag(cutMethod);
        }

        /// <summary>
        /// Y轴光栅尺补偿
        /// </summary>
        /// <param name="cutMethod">0 默认-开启补偿  1 不开</param>
        public void SetYAxisCompStatus(int status)
        {
            yAxisCompStatus.writeValue = status.ToString();
            keyencePlc.writeTag(yAxisCompStatus);
        }

        /// <summary>
        /// Z轴光栅尺补偿
        /// </summary>
        /// <param name="cutMethod">0 默认-开启补偿  1 不开</param>
        public void SetZ1AxisCompStatus(int status)
        {
            z1AxisCompStatus.writeValue = status.ToString();
            keyencePlc.writeTag(z1AxisCompStatus);
        }

        /// <summary>
        /// 停止切割
        /// </summary>
        public void StopCut(int status)
        {
            cutStop.writeValue = status + "";
            keyencePlc.writeTag(cutStop);
        }

        /// <summary>
        /// 停止切割
        /// </summary>
        public async Task StopCutAsync(int status)
        {
            cutStop.writeValue = status + "";
            await keyencePlc.WriteTagAsync(cutStop);
        }

        /// <summary>
        /// 切割结束
        /// </summary>
        public void EndFullAutoCut()
        {
            fullAutoCutEnd.writeValue = "1";
            keyencePlc.writeTag(fullAutoCutEnd);
        }

        /// <summary>
        /// 切割结束
        /// </summary>
        public async Task EndFullAutoCutAsync()
        {
            fullAutoCutEnd.writeValue = "1";
            await keyencePlc.WriteTagAsync(fullAutoCutEnd);
        }

        /// <summary>
        /// 设置Y轴开始位置
        /// </summary>
        public void SetYStartPosition(string position)
        {
            yStartPosition.writeValue = position.ToString();
            keyencePlc.writeTag(yStartPosition);
        }

        /// <summary>
        /// 设置切割方向
        /// </summary>
        /// <param name="cutDirection">切割方向 0 前切 1 后切</param>
        public void SetCutDirection(int cutDirection)
        {
            if (cutDirection == 0)
            {
                cutDirectionAfter.writeValue = "0";
                cutDirectionAgo.writeValue = "1";
                keyencePlc.writeTag(cutDirectionAfter);
                keyencePlc.writeTag(cutDirectionAgo);
            }
            else
            {
                cutDirectionAfter.writeValue = "1";
                cutDirectionAgo.writeValue = "0";
                keyencePlc.writeTag(cutDirectionAfter);
                keyencePlc.writeTag(cutDirectionAgo);
            }
        }

        /// <summary>
        /// 设置切割刀数
        /// </summary>
        /// <param name="count">刀数</param>
        public void SetCutNum(int count)
        {
            cutNum.writeValue = count.ToString();
            keyencePlc.writeTag(cutNum);
        }

        /// <summary>
        /// 确认参数
        /// </summary>
        public void ConfirmParams()
        {
            confirmParams.writeValue = "1";
            keyencePlc.writeTag(confirmParams);
        }

        /// <summary>
        /// 主轴转速设置
        /// </summary>
        /// <param name="spindleRevValue"></param>
        /// <returns></returns>
        public async Task SetSpindleSpeedAsync(int spindleRevValue)
        {
            spindleRev.writeValue = spindleRevValue.ToString();
            await keyencePlc.WriteTagAsync(spindleRev);
        }

        /// <summary>
        /// 等待主轴转速达到预期
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task WaitSpindleRevReachAsync(CancellationToken token)
        {
            await TaskUtils.WaitExpectedResultAsync(IsSpindleRevReachAsync, default, token);
        }

        /// <summary>
        /// 主轴转速是否达到预期
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsSpindleRevReachAsync()
        {
            return await keyencePlc.ReadDataAsync(spindleRevReach.addr) == true;
        }

        public async Task FlangeRepairBeginsAsync()
        {
            flangeRepairBegins.writeValue = "1";
            await keyencePlc.WriteTagAsync(flangeRepairBegins);
        }

        public async Task FlangeRepairEndAsync()
        {
            flangeRepairBegins.writeValue = "0";
            await keyencePlc.WriteTagAsync(flangeRepairBegins);
        }

        /// <summary>
        /// 是否检测到电火花
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsDetectedSparksAsync()
        {
            return await keyencePlc.ReadDataAsync(detectedSparks.addr) == true;
        }

        /// <summary>
        /// 设置切割需要的参数
        /// </summary>
        /// <param name="feedSpeedValue">切割速度</param>
        /// <param name="zEndIndex">Z轴切割位置</param>
        /// <param name="xEndLocation">X轴结束位置</param>
        /// <param name="yCutLocation">Y轴切割位置</param>
        /// <param name="spindleRev">主轴转速</param>
        public async Task<bool> SetCutParamsAsync(float feedSpeedValue, float zEndLocation, float zStartLocation, float xStartLoaction, float xEndLocation,
            float yCutLocation, string checkStatus, float thetaDeg, int spindleRevValue, float depthEntry)
        {
            float xSoftUpperLimit = Appsettings.PositiveLimitPositionX ?? 0;
            if (xEndLocation > xSoftUpperLimit)
            {
                xEndLocation = xSoftUpperLimit;
            }
            float ySoftUpperLimit = Appsettings.PositiveLimitPositionY ?? 0;
            if (yCutLocation > ySoftUpperLimit)
            {
                yCutLocation = ySoftUpperLimit;
            }
            Tools.LogDebug(
                $"\r\n切割速度: {feedSpeedValue}\r\n" +
                $"Z轴开始位置: {zStartLocation}\r\n" +
                $"Z轴结束位置: {zEndLocation}\r\n" +
                $"X轴开始位置: {xStartLoaction}\r\n" +
                $"X轴结束位置: {xEndLocation}\r\n" +
                $"Y轴切割位置: {yCutLocation}\r\n" +
                $"状态检查: {checkStatus}\r\n" +
                $"theta角度: {thetaDeg}\r\n" +
                $"主轴转速: {spindleRevValue}\r\n" +
                $""
                );
            bool success = true;
            // 切割速度
            await PlcControl.tagControl.Xaxis.SetAbsoluteSpeedAsync(feedSpeedValue);
            // x轴开始位置
            xStartPosition.writeValue = xStartLoaction.ToString();
            success &= await keyencePlc.WriteTagAsync(xStartPosition);
            // x结束位置
            xLength.writeValue = xEndLocation.ToString();
            success &= await keyencePlc.WriteTagAsync(xLength);
            // Y轴切割开始位置
            yStartPosition.writeValue = yCutLocation.ToString();
            success &= await keyencePlc.WriteTagAsync(yStartPosition);
            // z轴开始位置
            z1StartPosition.writeValue = zStartLocation.ToString();
            success &= await keyencePlc.WriteTagAsync(z1StartPosition);
            // z轴结束位置
            z1EndPosition.writeValue = zEndLocation.ToString();
            success &= await keyencePlc.WriteTagAsync(z1EndPosition);
            // 停机检查
            shutdownCheck.writeValue = checkStatus;
            success &= await keyencePlc.WriteTagAsync(shutdownCheck);
            // 切割角度
            cutFaceAngle.writeValue = thetaDeg.ToString();
            success &= await keyencePlc.WriteTagAsync(cutFaceAngle);
            // 主轴转速
            spindleRev.writeValue = spindleRevValue.ToString();
            success &= await keyencePlc.WriteTagAsync(spindleRev);
            //确认切割参数
            confirmParams.writeValue = "1";
            success &= await keyencePlc.WriteTagAsync(confirmParams);
            // 当前切入深度
            currentDepthEntry.writeValue = depthEntry.ToString();
            success &= await keyencePlc.WriteTagAsync(currentDepthEntry);
            return success;
        }

        /// <summary>
        /// 设置切割停止位置
        /// </summary>
        /// <param name="xLocation"></param>
        /// <param name="yLocation"></param>
        /// <param name="z2Location"></param>
        public void SetStopLocation(float xLocation, float yLocation, float z2Location)
        {
            xStopLocation.writeValue = xLocation.ToString();
            keyencePlc.writeTag(xStopLocation);
            yStopLocation.writeValue = yLocation.ToString();
            keyencePlc.writeTag(yStopLocation);
            z2StopLocation.writeValue = z2Location.ToString();
            keyencePlc.writeTag(z2StopLocation);
            // 确认切割参数
            ConfirmParams();
        }

        /// <summary>
        /// 设置切割停止位置
        /// </summary>
        /// <param name="xLocation"></param>
        /// <param name="yLocation"></param>
        /// <param name="z2Location"></param>
        public async Task SetStopLocationAsync(float xLocation, float yLocation, float z2Location)
        {
            float xSoftUpperLimit = float.Parse(PlcControl.tagControl.Xaxis.softUpperLimit.defaultValue);
            if (xLocation > xSoftUpperLimit) xLocation = xSoftUpperLimit;

            float ySoftUpperLimit = float.Parse(PlcControl.tagControl.Yaxis.softUpperLimit.defaultValue);
            if (yLocation > ySoftUpperLimit) yLocation = ySoftUpperLimit;

            xStopLocation.writeValue = xLocation.ToString();
            await keyencePlc.WriteTagAsync(xStopLocation);
            yStopLocation.writeValue = yLocation.ToString();
            await keyencePlc.WriteTagAsync(yStopLocation);
            z2StopLocation.writeValue = z2Location.ToString();
            await keyencePlc.WriteTagAsync(z2StopLocation);
            // 确认切割参数
            confirmParams.writeValue = "1";
            await keyencePlc.WriteTagAsync(confirmParams);
        }

        /// <summary>
        /// 设置返回速度
        /// </summary>
        /// <param name="returnSpeedParams"></param>
        public void SetReturnSpeed(int returnSpeedParams)
        {
            returnSpeed.writeValue = returnSpeed.ToString();
            keyencePlc.writeTag(returnSpeed);
            // 确认切割参数
            ConfirmParams();
        }

        /// <summary>
        /// 设置切割延时时间
        /// </summary>
        /// <param name="second"></param>
        public void SetCutStopDelayTime(int second)
        {
            cutStopDelayTime.writeValue = second.ToString();
            keyencePlc.writeTag(cutStopDelayTime);
            // 确认切割参数
            ConfirmParams();
        }

        /// <summary>
        /// 设置切割初始位置
        /// </summary>
        /// <param name="initX"></param>
        /// <param name="initY"></param>
        /// <param name="initZ1"></param>
        public void SetCutInitPosition(string initX, string initY, string initZ1)
        {
            xInitPosition.writeValue = initX.ToString();
            keyencePlc.writeTag(xInitPosition);
            Thread.Sleep(10);
            yInitPosition.writeValue = initY.ToString();
            keyencePlc.writeTag(yInitPosition);
            Thread.Sleep(10);
            z1InitPosition.writeValue = initZ1.ToString();
            keyencePlc.writeTag(z1InitPosition);
            Thread.Sleep(10);
            // 确认切割参数
            ConfirmParams();
        }
    }

    public class SparkRepairKnife
    {
        private KeyencePlc keyencePlc = KeyencePlc.Instance;

        public SparkRepairKnife()
        { }

        // 进入修刀画面
        public Tag enterElectrical { get; set; }

        // 参数设置确认
        public Tag confirmParams { get; set; }

        public Tag axisParamsConfirm { get; set; }

        // 修刀开始
        public Tag sharpenStart { get; set; }

        // 修刀停止
        public Tag sharpenEnd { get; set; }

        // 修刀停止
        public Tag sharpenStop { get; set; }

        // 修刀模式状态
        public Tag sharpenStatus { get; set; }

        // z1轴修刀开始位置
        public Tag z1StartPos { get; set; }

        // Y轴正面修刀开始位置
        public Tag yFrontStartPos { get; set; }

        // Y轴反面修刀开始位置
        public Tag yBackStartPos { get; set; }

        // z1轴修刀结束位置
        public Tag z1EndPos { get; set; }

        // x轴修刀开始位置
        public Tag xStartPos { get; set; }

        // z轴每次修刀下降距离
        public Tag zStepAmount { get; set; }

        // 重复次数
        public Tag repeatCount { get; set; }

        // 修刀速度
        public Tag sharpenSpeed { get; set; }

        // 主轴速度
        public Tag spindleSpeed { get; set; }

        // Z轴极限位置
        public Tag zLimitPos { get; set; }

        // 当前修刀次数
        public Tag currentCount { get; set; }

        // 修刀工作中
        public Tag electricalStatus { get; set; }

        // ====================切割相关结束

        /// <summary>
        /// 进入修刀
        /// </summary>
        public void EnterElectrical(int status)
        {
            enterElectrical.writeValue = "0";
            keyencePlc.writeTag(enterElectrical);
            if (status == 1)
            {
                Thread.Sleep(10);
                enterElectrical.writeValue = "1";
                keyencePlc.writeTag(enterElectrical);
            }
        }

        /// <summary>
        /// 启动/停止修刀
        /// </summary>
        /// <param name="type">0 开始修刀 1 暂停修刀 2 结束修刀</param>
        public void ToggleKnifeSharpening(int type)
        {
            if (type == 0)
            {
                sharpenStart.writeValue = "0";
                keyencePlc.writeTag(sharpenStart);
                Thread.Sleep(10);
                sharpenStart.writeValue = "1";
                keyencePlc.writeTag(sharpenStart);
            }
            else if (type == 1)
            {
                sharpenStop.writeValue = "0";
                keyencePlc.writeTag(sharpenStop);
                Thread.Sleep(10);
                sharpenStop.writeValue = "1";
                keyencePlc.writeTag(sharpenStop);
            }
            else if (type == 2)
            {
                sharpenEnd.writeValue = "0";
                keyencePlc.writeTag(sharpenEnd);
                Thread.Sleep(10);
                sharpenEnd.writeValue = "1";
                keyencePlc.writeTag(sharpenEnd);
            }
        }

        /// <summary>
        /// 设置修刀参数
        /// </summary>
        /// <param name="z1StartPosValue"></param>
        /// <param name="z1EndPosValue"></param>
        /// <param name="yFrontStartPosValue"></param>
        /// <param name="yBackStartPosValue"></param>
        /// <param name="xStartPosValue"></param>
        /// <param name="zStepAmountValue"></param>
        /// <param name="repeatCountValue"></param>
        /// <param name="bladeCorrctionSpeedValue"></param>
        /// <param name="spindleSpeedValue"></param>
        /// <param name="zLimitPosValue"></param>
        public void SetParams(double z1StartPosValue, double z1EndPosValue, double yFrontStartPosValue
            , double yBackStartPosValue, string xStartPosValue, string zStepAmountValue
            , int repeatCountValue, string bladeCorrctionSpeedValue, string spindleSpeedValue, string zLimitPosValue)
        {
            // 设置相关参数
            // 设置z1轴修刀开始位置
            z1StartPos.writeValue = z1StartPosValue.ToString();
            keyencePlc.writeTag(z1StartPos);
            Thread.Sleep(2);
            // Y轴正面修刀开始位置
            yFrontStartPos.writeValue = yFrontStartPosValue.ToString();
            keyencePlc.writeTag(yFrontStartPos);
            Thread.Sleep(2);
            // Y轴反面修刀开始位置
            yBackStartPos.writeValue = yBackStartPosValue.ToString();
            keyencePlc.writeTag(yBackStartPos);
            Thread.Sleep(2);
            // z1轴修刀结束位置
            z1EndPos.writeValue = Math.Round(z1EndPosValue, 4).ToString();
            keyencePlc.writeTag(z1EndPos);
            Thread.Sleep(2);
            // x轴修刀开始位置
            xStartPos.writeValue = xStartPosValue.ToString();
            keyencePlc.writeTag(xStartPos);
            Thread.Sleep(2);
            // z轴每次修刀下降距离
            zStepAmount.writeValue = zStepAmountValue;
            keyencePlc.writeTag(zStepAmount);
            Thread.Sleep(2);
            // 重复次数
            repeatCount.writeValue = repeatCountValue + "";
            keyencePlc.writeTag(repeatCount);
            Thread.Sleep(2);
            // 修刀速度
            sharpenSpeed.writeValue = bladeCorrctionSpeedValue;
            keyencePlc.writeTag(sharpenSpeed);
            Thread.Sleep(2);
            // 主轴速度
            spindleSpeed.writeValue = spindleSpeedValue;
            keyencePlc.writeTag(spindleSpeed);
            Thread.Sleep(2);
            // Z轴极限位置
            zLimitPos.writeValue = zLimitPosValue;
            keyencePlc.writeTag(zLimitPos);
            Thread.Sleep(2);
            // 参数确认
            confirmParams.writeValue = "1";
            keyencePlc.writeTag(confirmParams);
            Thread.Sleep(10);
        }
    }

    public class Flange
    {
        private KeyencePlc keyencePlc = KeyencePlc.Instance;

        public Flange()
        { }

        /// <summary>
        /// 自动法兰研磨画面进入
        /// </summary>
        public Tag AutoFlangeGrindingScreenEnter { get; set; }

        /// <summary>
        /// 研磨开始
        /// </summary>
        public Tag GrindingStart { get; set; }

        /// <summary>
        /// 研磨停止
        /// </summary>
        public Tag GrindingStop { get; set; }

        /// <summary>
        /// 研磨结束
        /// </summary>
        public Tag EndTrimming { get; set; }

        /// <summary>
        /// 研磨参数确认
        /// </summary>
        public Tag GrindingParameterConfirm { get; set; }

        /// <summary>
        /// 法兰研磨准备好
        /// </summary>
        public Tag FlangeGrindingReady { get; set; }

        /// <summary>
        /// X 轴开始位置
        /// </summary>
        public Tag XAxisStartPosition { get; set; }

        /// <summary>
        /// X 轴结束位置
        /// </summary>
        public Tag XAxisEndPosition { get; set; }

        /// <summary>
        /// Y 轴开始位置
        /// </summary>
        public Tag YAxisStartPosition { get; set; }

        /// <summary>
        /// Y 轴步进距离
        /// </summary>
        public Tag YAxisStepDistance { get; set; }

        /// <summary>
        /// Z 轴研磨位置
        /// </summary>
        public Tag ZAxisGrindingPosition { get; set; }

        /// <summary>
        /// 研磨次数
        /// </summary>
        public Tag GrindingCount { get; set; }

        /// <summary>
        /// 主轴研磨速度
        /// </summary>
        public Tag SpindleGrindingSpeed { get; set; }

        /// <summary>
        /// X 轴研磨速度
        /// </summary>
        public Tag XAxisGrindingSpeed { get; set; }

        /// <summary>
        /// 研磨步进间隔
        /// </summary>
        public Tag GrindingStepInterval { get; set; }

        /// <summary>
        /// 当前研磨次数
        /// </summary>
        public Tag TrimmingCurrentCount { get; set; }

        /// <summary>
        /// 开始修整
        /// </summary>
        public void StartTrimming()
        {
            GrindingStart.writeValue = "1";
            keyencePlc.writeTag(GrindingStart);
        }

        /// <summary>
        /// 暂停修整
        /// </summary>
        public void StopTrimming()
        {
            GrindingStop.writeValue = "1";
            keyencePlc.writeTag(GrindingStop);
        }

        /// <summary>
        /// 结束修整
        /// </summary>
        public void SetEndTrimming()
        {
            EndTrimming.writeValue = "1";
            keyencePlc.writeTag(EndTrimming);
        }

        /// <summary>
        /// 进入修整
        /// </summary>
        public void JoinTrimming(int status)
        {
            AutoFlangeGrindingScreenEnter.writeValue = status.ToString();
            keyencePlc.writeTag(AutoFlangeGrindingScreenEnter);
        }

        /// <summary>
        /// 设置法兰修整参数
        /// </summary>
        /// <param name="xAxisStartPositionValue">x轴开始位置的值</param>
        /// <param name="xAxisEndPositionValue">x轴结束位置的值</param>
        /// <param name="yAxisStartPositionValue">Y轴开始位置的值</param>
        /// <param name="yAxisStepDistanceValue">Y轴步进距离的值</param>
        /// <param name="zAxisGrindingPositionValue">z轴研磨位置的值</param>
        /// <param name="grindingCountValue">研磨次数的值</param>
        /// <param name="spindleGrindingSpeedValue">主轴研磨速度的值</param>
        /// <param name="xAxisGrindingSpeedValue">x轴研磨速度的值</param>
        /// <param name="grindingStepIntervalValue">研磨步进间隔的值</param>
        public void SetFlangeParams(
            string xAxisStartPositionValue,
            string xAxisEndPositionValue,
            string yAxisStartPositionValue,
            string yAxisStepDistanceValue,
            string zAxisGrindingPositionValue,
            string grindingCountValue,
            string spindleGrindingSpeedValue,
            string xAxisGrindingSpeedValue,
            string grindingStepIntervalValue)
        {
            // x轴开始位置
            XAxisStartPosition.writeValue = xAxisStartPositionValue;
            keyencePlc.writeTag(XAxisStartPosition);
            Thread.Sleep(5);

            // x轴结束位置
            XAxisEndPosition.writeValue = xAxisEndPositionValue;
            keyencePlc.writeTag(XAxisEndPosition);
            Thread.Sleep(5);

            // Y轴开始位置
            YAxisStartPosition.writeValue = yAxisStartPositionValue;
            keyencePlc.writeTag(YAxisStartPosition);
            Thread.Sleep(5);

            // Y轴步进距离
            YAxisStepDistance.writeValue = yAxisStepDistanceValue;
            keyencePlc.writeTag(YAxisStepDistance);
            Thread.Sleep(5);

            // z轴研磨位置
            ZAxisGrindingPosition.writeValue = zAxisGrindingPositionValue;
            keyencePlc.writeTag(ZAxisGrindingPosition);
            Thread.Sleep(5);

            // 研磨次数
            GrindingCount.writeValue = grindingCountValue;
            keyencePlc.writeTag(GrindingCount);
            Thread.Sleep(5);

            // 主轴研磨速度
            SpindleGrindingSpeed.writeValue = spindleGrindingSpeedValue;
            keyencePlc.writeTag(SpindleGrindingSpeed);
            Thread.Sleep(5);

            // x轴研磨速度
            XAxisGrindingSpeed.writeValue = xAxisGrindingSpeedValue;
            keyencePlc.writeTag(XAxisGrindingSpeed);
            Thread.Sleep(5);

            // 研磨步进间隔
            GrindingStepInterval.writeValue = grindingStepIntervalValue;
            keyencePlc.writeTag(GrindingStepInterval);
            Thread.Sleep(5);
            // 参数确认
            GrindingParameterConfirm.writeValue = "1";
            keyencePlc.writeTag(GrindingParameterConfirm);
        }
    }

    public class Tag : ICloneable, INotifyPropertyChanged
    {
        public Tag()
        {
        }

        public Tag(string addr)
        {
            this.addr = addr;
        }

        public Tag(string addr, string name, string valueType)
        {
            this.addr = addr;
            this.name = name;
            this.valueType = valueType;
        }

        public Tag(string addr, string writeAddr, string name, string valueType, string desc)
        {
            this.addr = addr;
            this.name = name;
            this.writeAddr = writeAddr;
            this.valueType = valueType;
        }

        public Tag(string addr, string name, string valueType, string desc)
        {
            this.addr = addr;
            this.name = name;
            this.valueType = valueType;
            this.describe = desc;
        }

        // plc变量名称
        public string name = "";

        // plc变量地址
        public string addr = "";

        // plc变量写入地址
        public string writeAddr = "";

        // plc变量默认值
        public string defaultValue = "";

        // plc变量类型
        public string valueType = "";

        // plc变量实际值
        public string value = "";

        // 最大值
        public string maxValue = "";

        // 最小值
        public string minValue = "";

        // plc变量写入值
        public string writeValue = "";

        // 变量描述
        public string describe = "";

        // 变量上限值（不为空字符串的情况下，超过则报警）
        public string upperValue = "";

        // 变量下限值（不为空字符串的情况下，低于则报警）
        public string lowerValue = "";

        // 变量有效值（不为null或空的情况下，变量值不在这个范围则报警，用于离散量校验和报警）
        public List<string>? validValue = null;

        // 变量无效值（不为null或空的情况下，变量值在这个范围则报警，用于离散量校验和报警）
        public List<string>? invalidValue = null;

        // 变量报警优先级
        public int alarmPriority = 0;

        // 变量状态：是否处于异常报警状态
        public bool errorStatus = false;

        public string Value
        {
            get
            {
                return value;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    this.value = value.ToString();
                }
                OnPropertyChanged("Value");
            }
        }

        public object Clone()
        {
            return (Tag)this.MemberwiseClone();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}