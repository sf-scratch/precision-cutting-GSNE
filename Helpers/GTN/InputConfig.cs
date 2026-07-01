using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Helpers.GTN
{/// <summary>
 /// EtherCAT数字输入点位配置
 /// slave：从站号，offset：字节偏移，bit：字节内bit位(0~7)
 /// </summary>
   
    /// <summary>
    /// 整机所有EtherCAT数字输入DI配置容器
    /// 全部DI位于从站0，字节偏移0，bit0~bit15（占用2个字节）
    /// </summary>
    public class InputConfig
    {
        private static readonly Lazy<InputConfig> _lazy = new(() => new InputConfig());

        public static InputConfig Instance
        {
            get { return _lazy.Value; }
        }
        // ========== 全部DI点位定义（对应你提供的0.00~0.15）==========
        /// <summary>0.00 工件真空按钮</summary>
        public IoConfig WorkpieceVacuumBtn { get; set; }
        /// <summary>0.01 急停按钮</summary>
        public IoConfig EmgStop { get; set; }
        /// <summary>0.02 紧急抬起</summary>
        public IoConfig EmergencyLift { get; set; }
        /// <summary>0.03 复位按钮</summary>
        public IoConfig ResetBtn { get; set; }
        /// <summary>0.05 工件真空度检测</summary>
        public IoConfig WorkpieceVacuumDetect { get; set; }
        /// <summary>0.06 气浮气压值检测</summary>
        public IoConfig AirFloatPressureDetect { get; set; }
        /// <summary>0.07 主轴抱闸压力</summary>
        public IoConfig SpindleBrakePressure { get; set; }
        /// <summary>0.08 主轴气压值</summary>
        public IoConfig SpindleAirPressure { get; set; }
        /// <summary>0.09 切割水检测开关NO</summary>
        public IoConfig CutWaterDetectNO { get; set; }
        /// <summary>0.10 冷却水检测开关NO</summary>
        public IoConfig CoolWaterDetectNO { get; set; }
        /// <summary>0.11 测高继电器闭合检测</summary>
        public IoConfig HeightRelayCloseDetect { get; set; }
        /// <summary>0.12 测高接触</summary>
        public IoConfig HeightContactDetect { get; set; }
        /// <summary>0.13 主轴电刷检查</summary>
        public IoConfig SpindleBrushCheck { get; set; }
        /// <summary>0.14 相机安全门</summary>
        public IoConfig CameraSafetyDoor { get; set; }
        /// <summary>0.15 切割安全门</summary>
        public IoConfig CutSafetyDoor { get; set; }

        private short _core = 2;

        public List<IoConfig> AllDiConfigs =>
        [
            WorkpieceVacuumBtn,
            EmgStop,
            EmergencyLift,
            ResetBtn,
            WorkpieceVacuumDetect,
            AirFloatPressureDetect,
            SpindleBrakePressure,
            SpindleAirPressure,
            CutWaterDetectNO,
            CoolWaterDetectNO,
            HeightRelayCloseDetect,
            HeightContactDetect,
            SpindleBrushCheck,
            CameraSafetyDoor,
            CutSafetyDoor
        ];

        public InputConfig()
        {
            // 构造函数自动初始化所有IO点位（从站0，字节偏移0，bit对应）
            InitAllIoPoint();
        }

        /// <summary>初始化全部DI点位映射</summary>
        private void InitAllIoPoint()
        {
            ushort slaveId = 5;
            ushort byteOff = 2;

            WorkpieceVacuumBtn = new IoConfig(slaveId, byteOff, 0, "工件真空按钮");
            EmgStop = new IoConfig(slaveId, byteOff, 1, "急停按钮", "设备急停按钮按下，请复位急停！",() => { return IoAlarm.Instance.CheckEmgStopAlarmAsync(); });
            EmergencyLift = new IoConfig(slaveId, byteOff, 2, "紧急抬起", "收到紧急抬起信号", () => { return IoAlarm.Instance.CheckEmergencyLiftAlarmAsync(); });
            ResetBtn = new IoConfig(slaveId, byteOff, 3, "复位按钮");//单独用
            WorkpieceVacuumDetect = new IoConfig(slaveId, byteOff, 5, "工件真空度", "工件真空度不足，请检查真空发生器！", () => { return IoAlarm.Instance.CheckWorkpieceVacuumDetectAlarmAsync(); });
            AirFloatPressureDetect = new IoConfig(slaveId, byteOff, 6, "气浮气压值", "气浮气压异常，请检查气源！", () => { return IoAlarm.Instance.CheckAirFloatPressureAlarmAsync(); });
            SpindleBrakePressure = new IoConfig(slaveId, byteOff, 7, "主轴抱闸压力", "主轴抱闸压力不足！", () => { return IoAlarm.Instance.CheckSpindleBrakePressureAlarmAsync(); });
            SpindleAirPressure = new IoConfig(slaveId, (ushort)(byteOff + 1), 0, "主轴气压值", "主轴冷却气压不足！", () => { return IoAlarm.Instance.CheckSpindleAirPressureAlarmAsync(); });
            CutWaterDetectNO = new IoConfig(slaveId, (ushort)(byteOff + 1), 1, "切割水检测开关NO", "切割水流量不足，无法切割！", () => { return IoAlarm.Instance.CheckCutWaterDetectAlarmAsync(); });
            CoolWaterDetectNO = new IoConfig(slaveId, (ushort)(byteOff + 1), 2, "冷却水检测开关NO", "主轴冷却水异常！", () => { return IoAlarm.Instance.CheckCoolWaterDetectAlarmAsync(); });
            HeightRelayCloseDetect = new IoConfig(slaveId, (ushort)(byteOff + 1), 3, "测高继电器闭合检测", "测高继电器未闭合，请检查测高模组！", () => { return IoAlarm.Instance.CheckHeightRelayAlarmAsync(); });
            HeightContactDetect = new IoConfig(slaveId, (ushort)(byteOff + 1), 4, "测高接触");//单独用
            SpindleBrushCheck = new IoConfig(slaveId, (ushort)(byteOff + 1), 5, "主轴电刷检查", "主轴电刷磨损，请更换电刷！", () => { return IoAlarm.Instance.CheckSpindleBrushAlarmAsync(); });
            CameraSafetyDoor = new IoConfig(slaveId, (ushort)(byteOff + 1), 6, "相机安全门", "相机安全门未关闭，禁止运行！", () => { return IoAlarm.Instance.CheckCameraSafetyDoorAlarmAsync(); });
            CutSafetyDoor = new IoConfig(slaveId, (ushort)(byteOff + 1), 7, "切割安全门", "切割仓安全门打开，请关门后再启动！", () => { return IoAlarm.Instance.CheckCutSafetyDoorAlarmAsync(); });
        }

        #region 底层封装 GTN_EcatIOReadInput 读取单个DI
        /// <summary>
        /// 同步读取单个DI点位电平
        /// 返回true：IO高电平导通；false：断开
        /// </summary>
        /// <param name="io">IO点位配置</param>
        /// <exception cref="Exception">读取IO失败抛出异常</exception>
        private bool ReadSingleDi(IoConfig io)
        {
            // 读取1字节数据（单个bit所在字节）
            byte[] buf = new byte[1];
            int ret = GTN_EcatIOReadInput(
                core: _core,
                slave: io.Slave,
                offset: io.ByteOffset,
                nSize: 2,
                pValue: buf
            );

            // 返回值1=从站异常，其他非0=指令错误
            if (ret != 0)
            {
                throw new Exception($"读取IO[{io.Name}]失败，错误码：{ret}，从站{io.Slave}");
            }

            // 按bit判断电平
            byte mask = (byte)(1 << io.BitIndex);
            return (buf[0] & mask) != 0;
        }

        /// <summary>异步读取单个DI，适配你项目async/await风格</summary>
        private async Task<bool> ReadSingleDiAsync(IoConfig io)
        {
            return await Task.Run(() => ReadSingleDi(io));
        }
        #endregion

        #region 对外暴露 单个IO读取同步/异步方法

        public bool GetWorkpieceVacuumBtn() => ReadSingleDi(WorkpieceVacuumBtn);
        public async Task<bool> GetWorkpieceVacuumBtnAsync() => await ReadSingleDiAsync(WorkpieceVacuumBtn);

        public bool GetEmgStop() => ReadSingleDi(EmgStop);
        public async Task<bool> GetEmgStopAsync() => await ReadSingleDiAsync(EmgStop);

        public bool GetEmergencyLift() => ReadSingleDi(EmergencyLift);
        public async Task<bool> GetEmergencyLiftAsync() => await ReadSingleDiAsync(EmergencyLift);

        public bool GetResetBtn() => ReadSingleDi(ResetBtn);
        public async Task<bool> GetResetBtnAsync() => await ReadSingleDiAsync(ResetBtn);
        /// <summary>
        /// 读工件真空度
        /// </summary>
        /// <returns></returns>
        public bool GetWorkpieceVacuumDetect() => ReadSingleDi(WorkpieceVacuumDetect);
        public async Task<bool> GetWorkpieceVacuumDetectAsync() => await ReadSingleDiAsync(WorkpieceVacuumDetect);

        public bool GetAirFloatPressureDetect() => ReadSingleDi(AirFloatPressureDetect);
        public async Task<bool> GetAirFloatPressureDetectAsync() => await ReadSingleDiAsync(AirFloatPressureDetect);

        public bool GetSpindleBrakePressure() => ReadSingleDi(SpindleBrakePressure);
        public async Task<bool> GetSpindleBrakePressureAsync() => await ReadSingleDiAsync(SpindleBrakePressure);

        public bool GetSpindleAirPressure() => ReadSingleDi(SpindleAirPressure);
        public async Task<bool> GetSpindleAirPressureAsync() => await ReadSingleDiAsync(SpindleAirPressure);

        public bool GetCutWaterDetectNO() => ReadSingleDi(CutWaterDetectNO);
        public async Task<bool> GetCutWaterDetectNOAsync() => await ReadSingleDiAsync(CutWaterDetectNO);

        public bool GetCoolWaterDetectNO() => ReadSingleDi(CoolWaterDetectNO);
        public async Task<bool> GetCoolWaterDetectNOAsync() => await ReadSingleDiAsync(CoolWaterDetectNO);

        public bool GetHeightRelayCloseDetect() => ReadSingleDi(HeightRelayCloseDetect);
        public async Task<bool> GetHeightRelayCloseDetectAsync() => await ReadSingleDiAsync(HeightRelayCloseDetect);

        public bool GetHeightContactDetect() => ReadSingleDi(HeightContactDetect);
        public async Task<bool> GetHeightContactDetectAsync() => await ReadSingleDiAsync(HeightContactDetect);

        public bool GetSpindleBrushCheck() => ReadSingleDi(SpindleBrushCheck);
        public async Task<bool> GetSpindleBrushCheckAsync() => await ReadSingleDiAsync(SpindleBrushCheck);

        public bool GetCameraSafetyDoor() => ReadSingleDi(CameraSafetyDoor);
        public async Task<bool> GetCameraSafetyDoorAsync() => await ReadSingleDiAsync(CameraSafetyDoor);

        public bool GetCutSafetyDoor() => ReadSingleDi(CutSafetyDoor);
        public async Task<bool> GetCutSafetyDoorAsync() => await ReadSingleDiAsync(CutSafetyDoor);
        #endregion

        #region 一次性读取全部DI（批量读取2字节，性能更高）
        /// <summary>同步批量读取全部DI（0~15bit，2字节）</summary>
        public AllDiState ReadAllDi()
        {
            byte[] buf = new byte[2];
            int ret = GTN_EcatIOReadInput(_core, 5, 2, 2, buf);
            if (ret != 0)
                throw new Exception($"批量读取DI失败，错误码：{ret}");

            byte byte0 = buf[0]; // bit0~bit7
            byte byte1 = buf[1]; // bit8~bit15

            return new AllDiState
            {
                WorkpieceVacuumBtn = (byte0 & 1 << 0) != 0,
                EmgStop = (byte0 & 1 << 1) != 0,
                EmergencyLift = (byte0 & 1 << 2) != 0,
                ResetBtn = (byte0 & 1 << 3) != 0,
                WorkpieceVacuumDetect = (byte0 & 1 << 5) != 0,
                AirFloatPressureDetect = (byte0 & 1 << 6) != 0,
                SpindleBrakePressure = (byte0 & 1 << 7) != 0,

                SpindleAirPressure = (byte1 & 1 << 0) != 0,
                CutWaterDetectNO = (byte1 & 1 << 1) != 0,
                CoolWaterDetectNO = (byte1 & 1 << 2) != 0,
                HeightRelayCloseDetect = (byte1 & 1 << 3) != 0,
                HeightContactDetect = (byte1 & 1 << 4) != 0,
                SpindleBrushCheck = (byte1 & 1 << 5) != 0,
                CameraSafetyDoor = (byte1 & 1 << 6) != 0,
                CutSafetyDoor = (byte1 & 1 << 7) != 0,
            };
        }

        public byte[] ReadAllDiBytes()
        {
            byte[] buf = new byte[2];
            int ret = GTN_EcatIOReadInput(_core, 5, 2, 2, buf);
            if (ret != 0)
                throw new Exception($"批量读取DI失败，错误码：{ret}");
            return buf;
        }

        /// <summary>异步批量读取全部DI</summary>
        public async Task<AllDiState> ReadAllDiAsync()
        {
            return await Task.Run(() => ReadAllDi());
        }
        #endregion

        #region 
        private static extern int GTN_EcatIOReadInput(
            short core,
            ushort slave,
            ushort offset,
            ushort nSize,
            byte[] pValue
        );
        #endregion
    }

    /// <summary>批量读取全部DI后存储所有IO状态的载体</summary>
    public class AllDiState
    {
        public bool WorkpieceVacuumBtn { get; set; }
        public bool EmgStop { get; set; }
        public bool EmergencyLift { get; set; }
        public bool ResetBtn { get; set; }
        public bool WorkpieceVacuumDetect { get; set; }
        public bool AirFloatPressureDetect { get; set; }
        public bool SpindleBrakePressure { get; set; }
        public bool SpindleAirPressure { get; set; }
        public bool CutWaterDetectNO { get; set; }
        public bool CoolWaterDetectNO { get; set; }
        public bool HeightRelayCloseDetect { get; set; }
        public bool HeightContactDetect { get; set; }
        public bool SpindleBrushCheck { get; set; }
        public bool CameraSafetyDoor { get; set; }
        public bool CutSafetyDoor { get; set; }
    }
}