using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Model.cut;
using 精密切割系统.ViewModel;
using static 精密切割系统.Helpers.GTN.mc_la;

namespace 精密切割系统.Helpers.GTN
{
    //public class OutputConfig
    //{
    //    public IoConfig GreenLight { get; set; }

    //    public IoConfig RedLight { get; set; }

    //    public IoConfig Buzzer { get; set; }
    //}
    /// <summary>
    /// EtherCAT数字输出DO配置容器
    /// 全部DO从站号100，bit0~bit15，占用2字节（字节偏移0、1）
    /// </summary>
    public class OutputConfig
    {
        private static readonly Lazy<OutputConfig> _lazy = new(() => new OutputConfig());

        public static OutputConfig Instance
        {
            get { return _lazy.Value; }
        }

        #region 全部DO点位属性（对应100.00 ~ 100.15）

        /// <summary>100.00 产品真空</summary>
        public IoConfig ProductVacuum { get; set; }

        /// <summary>100.01 工作盘真空</summary>
        public IoConfig TrayVacuum { get; set; }

        /// <summary>100.02 真空破</summary>
        public IoConfig BreakVacuum { get; set; }

        /// <summary>100.03 切割水打开</summary>
        public IoConfig CutWaterOpen { get; set; }

        /// <summary>100.04 产品吹气</summary>
        public IoConfig ProductBlow { get; set; }

        /// <summary>100.05 镜头气缸回</summary>
        public IoConfig CameraCylinderBack { get; set; }

        /// <summary>100.06 镜头气缸出</summary>
        public IoConfig CameraCylinderOut { get; set; }

        /// <summary>100.07 三色灯红</summary>
        public IoConfig LightRed { get; set; }

        /// <summary>100.08 三色灯黄</summary>
        public IoConfig LightYellow { get; set; }

        /// <summary>100.09 三色灯绿</summary>
        public IoConfig LightGreen { get; set; }

        /// <summary>100.10 蜂鸣器工作</summary>
        public IoConfig Buzzer { get; set; }

        /// <summary>100.11 测高继电器输出</summary>
        public IoConfig HeightRelayOut { get; set; }

        /// <summary>100.12 电刷继电器输出</summary>
        public IoConfig BrushRelayOut { get; set; }

        /// <summary>100.13 切割安全门锁</summary>
        public IoConfig CutSafetyLock { get; set; }

        /// <summary>100.14 复位按钮灯</summary>
        public IoConfig ResetBtnLight { get; set; }

        /// <summary>100.15 真空开按钮灯</summary>
        public IoConfig VacuumBtnLight { get; set; }

        #endregion 全部DO点位属性（对应100.00 ~ 100.15）

        private short _core = 2;

        public OutputConfig()
        {
            InitAllDoPoint();
        }

        /// <summary>初始化所有输出点位映射（从站100）</summary>
        private void InitAllDoPoint()
        {
            ushort slaveId = 5;
            ushort byteOff0 = 0;
            ushort byteOff1 = (ushort)(byteOff0 + 1);

            // 第一字节 bit0~bit7
            ProductVacuum = new IoConfig(slaveId, byteOff0, 0, "产品真空");
            TrayVacuum = new IoConfig(slaveId, byteOff0, 1, "工作盘真空");
            BreakVacuum = new IoConfig(slaveId, byteOff0, 2, "真空破");
            CutWaterOpen = new IoConfig(slaveId, byteOff0, 3, "切割水打开");
            ProductBlow = new IoConfig(slaveId, byteOff0, 4, "产品吹气");
            CameraCylinderBack = new IoConfig(slaveId, byteOff0, 5, "镜头气缸回");
            CameraCylinderOut = new IoConfig(slaveId, byteOff0, 6, "镜头气缸出");
            LightRed = new IoConfig(slaveId, byteOff0, 7, "三色灯红");

            // 第二字节 bit8~bit15
            LightYellow = new IoConfig(slaveId, byteOff1, 0, "三色灯黄");
            LightGreen = new IoConfig(slaveId, byteOff1, 1, "三色灯绿");
            Buzzer = new IoConfig(slaveId, byteOff1, 2, "蜂鸣器工作");
            HeightRelayOut = new IoConfig(slaveId, byteOff1, 3, "测高继电器输出");
            BrushRelayOut = new IoConfig(slaveId, byteOff1, 4, "电刷继电器输出");
            CutSafetyLock = new IoConfig(slaveId, byteOff1, 5, "切割安全门锁");
            ResetBtnLight = new IoConfig(slaveId, byteOff1, 6, "复位按钮灯");
            VacuumBtnLight = new IoConfig(slaveId, byteOff1, 7, "真空开按钮灯");
        }

        #region 底层私有方法：读取单路DO当前状态、写入单路DO

        /// <summary>读取单个DO当前输出电平</summary>
        private bool ReadSingleDo(IoConfig io)
        {
            byte[] buf = new byte[1];
            int ret = GTN_EcatIOReadInput(_core, io.Slave, io.ByteOffset, 1, out byte _buf);
            byte mask = (byte)(1 << io.BitIndex);
            return (buf[0] & mask) != 0;
        }

        /// <summary>异步读取DO状态</summary>
        private async Task<bool> ReadSingleDoAsync(IoConfig io)
        {
            return await Task.Run(() => ReadSingleDo(io));
        }

        /// <summary>设置单路DO电平（同步）</summary>
        /// <param name="io">点位配置</param>
        /// <param name="enable">true=输出高电平，false=输出低电平</param>
        private void SetSingleDo(IoConfig io, bool enable)
        {
            byte[] buf = new byte[2];
            int readRet = GTN_EcatIOReadOutput(_core, io.Slave, io.ByteOffset, 1, out buf[0]);

            // 第0位掩码：1 << 0 = 0b00000001
            byte bitMask = (byte)(1 << io.BitIndex);

            if (enable)
            {
                // 置1：按位或，只把目标位设1，其他位不变
                buf[0] |= bitMask;
            }
            else
            {
                // 清0：按位与取反，只把目标位清0
                buf[0] &= (byte)~bitMask;
            }

            // 写入更新后的字节
            int writeRet = GTN_EcatIOWriteOutput(_core, io.Slave, io.ByteOffset, 1, ref buf[0]);
            if (writeRet != 0)
                throw new Exception($"写入DO[{io.Name}]失败，错误码：{writeRet}");
        }

        /// <summary>异步设置DO输出</summary>
        private async Task SetSingleDoAsync(IoConfig io, bool enable)
        {
            await Task.Run(() => SetSingleDo(io, enable));
        }

        #endregion 底层私有方法：读取单路DO当前状态、写入单路DO

        /// <summary>产品真空读取</summary>
        public async Task<bool> GetProductVacuumAsync() => await ReadSingleDoAsync(ProductVacuum);

        /// <summary>设置产品真空</summary>
        /// <param name="enable">true开启，false关闭</param>
        public async Task SetProductVacuumAsync(bool enable) => await SetSingleDoAsync(ProductVacuum, enable);

        /// <summary>工作盘真空读取</summary>
        public async Task<bool> GetTrayVacuumAsync() => await ReadSingleDoAsync(TrayVacuum);

        /// <summary>设置工作盘真空</summary>
        /// <param name="enable">true开启，false关闭</param>
        public async Task SetTrayVacuumAsync(bool enable) => await SetSingleDoAsync(TrayVacuum, enable);

        /// <summary>真空破信号读取</summary>
        public async Task<bool> GetBreakVacuumAsync() => await ReadSingleDoAsync(BreakVacuum);

        /// <summary>设置破真空输出</summary>
        /// <param name="enable">true开启，false关闭</param>
        public async Task SetBreakVacuumAsync(bool enable) => await SetSingleDoAsync(BreakVacuum, enable);

        /// <summary>切割水开关状态读取</summary>
        public async Task<bool> GetCutWaterOpenAsync() => await ReadSingleDoAsync(CutWaterOpen);

        /// <summary>设置切割水输出</summary>
        /// <param name="enable">true开启，false关闭</param>
        public async Task SetCutWaterOpenAsync(bool enable) => await SetSingleDoAsync(CutWaterOpen, enable);

        /// <summary>产品吹气状态读取</summary>
        public async Task<bool> GetProductBlowAsync() => await ReadSingleDoAsync(ProductBlow);

        /// <summary>设置产品吹气输出</summary>
        /// <param name="enable">true开启，false关闭</param>
        public async Task SetProductBlowAsync(bool enable) => await SetSingleDoAsync(ProductBlow, enable);

        /// <summary>镜头气缸缩回状态读取</summary>
        public async Task<bool> GetCameraCylinderBackAsync() => await ReadSingleDoAsync(CameraCylinderBack);

        /// <summary>控制镜头气缸缩回</summary>
        /// <param name="enable">true缩回，false伸出</param>
        public async Task SetCameraCylinderBackAsync(bool enable) => await SetSingleDoAsync(CameraCylinderBack, enable);

        /// <summary>镜头气缸伸出状态读取</summary>
        public async Task<bool> GetCameraCylinderOutAsync() => await ReadSingleDoAsync(CameraCylinderOut);

        /// <summary>控制镜头气缸伸出</summary>
        /// <param name="enable">true伸出，false缩回</param>
        public async Task SetCameraCylinderOutAsync(bool enable) => await SetSingleDoAsync(CameraCylinderOut, enable);

        /// <summary>三色红灯状态读取</summary>
        public async Task<bool> GetLightRedAsync() => await ReadSingleDoAsync(LightRed);

        /// <summary>控制三色灯红灯</summary>
        /// <param name="enable">true亮，false灭</param>
        public async Task SetLightRedAsync(bool enable) => await SetSingleDoAsync(LightRed, enable);

        /// <summary>三色黄灯状态读取</summary>
        public async Task<bool> GetLightYellowAsync() => await ReadSingleDoAsync(LightYellow);

        /// <summary>控制三色灯黄灯</summary>
        /// <param name="enable">true亮，false灭</param>
        public async Task SetLightYellowAsync(bool enable) => await SetSingleDoAsync(LightYellow, enable);

        /// <summary>三色绿灯状态读取</summary>
        public async Task<bool> GetLightGreenAsync() => await ReadSingleDoAsync(LightGreen);

        /// <summary>控制三色灯绿灯</summary>
        /// <param name="enable">true亮，false灭</param>
        public async Task SetLightGreenAsync(bool enable) => await SetSingleDoAsync(LightGreen, enable);

        /// <summary>蜂鸣器状态读取</summary>
        public async Task<bool> GetBuzzerAsync() => await ReadSingleDoAsync(Buzzer);

        /// <summary>控制蜂鸣器开关</summary>
        /// <param name="enable">true开启蜂鸣，false关闭</param>
        public async Task SetBuzzerAsync(bool enable) => await SetSingleDoAsync(Buzzer, enable);

        /// <summary>测高继电器输出状态读取</summary>
        public async Task<bool> GetHeightRelayOutAsync() => await ReadSingleDoAsync(HeightRelayOut);

        /// <summary>控制测高继电器输出</summary>
        /// <param name="enable">true导通，false断开</param>
        public async Task SetHeightRelayOutAsync(bool enable) => await SetSingleDoAsync(HeightRelayOut, enable);

        /// <summary>电刷检测继电器输出状态读取</summary>
        public async Task<bool> GetBrushRelayOutAsync() => await ReadSingleDoAsync(BrushRelayOut);

        /// <summary>控制电刷检测继电器输出</summary>
        /// <param name="enable">true导通，false断开</param>
        public async Task SetBrushRelayOutAsync(bool enable) => await SetSingleDoAsync(BrushRelayOut, enable);

        /// <summary>切割安全门锁信号读取</summary>
        public async Task<bool> GetCutSafetyLockAsync() => await ReadSingleDoAsync(CutSafetyLock);

        /// <summary>控制切割安全门锁输出</summary>
        /// <param name="enable">true上锁，false解锁</param>
        public async Task SetCutSafetyLockAsync(bool enable) => await SetSingleDoAsync(CutSafetyLock, enable);

        /// <summary>复位按钮指示灯状态读取</summary>
        public async Task<bool> GetResetBtnLightAsync() => await ReadSingleDoAsync(ResetBtnLight);

        /// <summary>控制复位按钮指示灯，有报警时亮起</summary>
        /// <param name="enable">true亮灯，false熄灭</param>
        public async Task SetResetBtnLightAsync(bool enable) => await SetSingleDoAsync(ResetBtnLight, enable);

        /// <summary>真空达标按钮指示灯状态读取</summary>
        public async Task<bool> GetVacuumBtnLightAsync() => await ReadSingleDoAsync(VacuumBtnLight);

        /// <summary>控制真空达标按钮指示灯</summary>
        /// <param name="enable">true亮灯，false熄灭</param>
        public async Task SetVacuumBtnLightAsync(bool enable) => await SetSingleDoAsync(VacuumBtnLight, enable);

        #region 批量读取全部DO状态

        public AllDoState ReadAllDo()
        {
            byte[] buf = new byte[2];
            int ret = GTN_EcatIOReadInput(_core, 5, 0, 2, out buf[0]);
            if (ret != 0)
                throw new Exception("批量读取DO输出状态失败，错误码：" + ret);

            byte b0 = buf[0];
            byte b1 = buf[1];
            return new AllDoState
            {
                ProductVacuum = (b0 & 1 << 0) != 0,
                TrayVacuum = (b0 & 1 << 1) != 0,
                BreakVacuum = (b0 & 1 << 2) != 0,
                CutWaterOpen = (b0 & 1 << 3) != 0,
                ProductBlow = (b0 & 1 << 4) != 0,
                CameraCylinderBack = (b0 & 1 << 5) != 0,
                CameraCylinderOut = (b0 & 1 << 6) != 0,
                LightRed = (b0 & 1 << 7) != 0,

                LightYellow = (b1 & 1 << 0) != 0,
                LightGreen = (b1 & 1 << 1) != 0,
                Buzzer = (b1 & 1 << 2) != 0,
                HeightRelayOut = (b1 & 1 << 3) != 0,
                BrushRelayOut = (b1 & 1 << 4) != 0,
                CutSafetyLock = (b1 & 1 << 5) != 0,
                ResetBtnLight = (b1 & 1 << 6) != 0,
                VacuumBtnLight = (b1 & 1 << 7) != 0
            };
        }

        #endregion 批量读取全部DO状态

        #region 输出组合

        /// <summary>
        /// 打开切割水并确认切割水检测报警状态
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<bool> OpenCuttingWaterAndConfirmStatusAsync(CancellationToken token)
        {
            await OutputConfig.Instance.SetCutWaterOpenAsync(true);
            await Task.Delay(1000);
            return !await IoAlarm.Instance.CheckCutWaterDetectAlarmAsync();
        }

        /// <summary>
        /// 操作工件吹气
        /// </summary>
        public async Task TriggerWorkVacuumSwitchAsync()
        {
            if (await GetCameraCylinderBackAsync())
            {
                await SetCameraCylinderBackAsync(false);
            }
            else
            {
                await SetCameraCylinderBackAsync(true);
            }
        }

        #endregion 输出组合

        /// <summary>
        /// 批量读取do
        /// </summary>
        /// <returns></returns>
        public async Task<AllDoState> ReadAllDoAsync()
        {
            return await Task.Run(ReadAllDo);
        }
    }

    /// <summary>批量读取全部DO输出状态存储实体</summary>
    public class AllDoState
    {
        public bool ProductVacuum { get; set; }
        public bool TrayVacuum { get; set; }
        public bool BreakVacuum { get; set; }
        public bool CutWaterOpen { get; set; }
        public bool ProductBlow { get; set; }
        public bool CameraCylinderBack { get; set; }
        public bool CameraCylinderOut { get; set; }
        public bool LightRed { get; set; }
        public bool LightYellow { get; set; }
        public bool LightGreen { get; set; }
        public bool Buzzer { get; set; }
        public bool HeightRelayOut { get; set; }
        public bool BrushRelayOut { get; set; }
        public bool CutSafetyLock { get; set; }
        public bool ResetBtnLight { get; set; }
        public bool VacuumBtnLight { get; set; }
    }
}