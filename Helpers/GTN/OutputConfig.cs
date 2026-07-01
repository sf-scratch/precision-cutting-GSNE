using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            public static OutputConfig Instance { get; } = new OutputConfig();
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
        #endregion

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
                if (ret != 0)
                    throw new Exception($"读取DO[{io.Name}]状态失败，错误码：{ret}");
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
            //// 1. 先读取当前输出字节（Output寄存器要用 ReadOutput 不要 ReadInput）
            //int readRet = GTN_EcatIOReadOutput(_core, io.Slave, io.ByteOffset, 1,out byte _buf);
            //if (readRet != 0)
            //    throw new Exception($"读取DO[{io.Name}]当前值失败，错误码：{readRet}");
            //byte[] bValue = BitConverter.GetBytes(Convert.ToInt32(0));
            //int writeRet = GTN_EcatIOWriteOutput(_core, io.Slave, io.ByteOffset, 1, ref bValue[0]);
            //if (writeRet != 0)
            //    throw new Exception($"写入DO[{io.Name}]失败，错误码：{writeRet}");
            byte[] buf = new byte[2];
            int readRet = GTN_EcatIOReadOutput(_core, io.Slave, io.ByteOffset, 1, out buf[0]);
            if (readRet != 0)
                throw new Exception($"读取DO[{io.Name}]当前值失败，错误码：{readRet}");

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
            int writeRet = GTN_EcatIOWriteOutput(_core, io.Slave, io.ByteOffset, 1,ref buf[0]);
            if (writeRet != 0)
                throw new Exception($"写入DO[{io.Name}]失败，错误码：{writeRet}");
        }

        /// <summary>异步设置DO输出</summary>
        private async Task SetSingleDoAsync(IoConfig io, bool enable)
            {
                await Task.Run(() => SetSingleDo(io, enable));
            }
            #endregion

            #region 对外：产品真空 读写
            public bool GetProductVacuum() => ReadSingleDo(ProductVacuum);
            public async Task<bool> GetProductVacuumAsync() => await ReadSingleDoAsync(ProductVacuum);
            public void SetProductVacuum(bool enable) => SetSingleDo(ProductVacuum, enable);
            public async Task SetProductVacuumAsync(bool enable) => await SetSingleDoAsync(ProductVacuum, enable);
            #endregion

            #region 对外：工作盘真空 读写
            public bool GetTrayVacuum() => ReadSingleDo(TrayVacuum);
            public async Task<bool> GetTrayVacuumAsync() => await ReadSingleDoAsync(TrayVacuum);
            public void SetTrayVacuum(bool enable) => SetSingleDo(TrayVacuum, enable);
            public async Task SetTrayVacuumAsync(bool enable) => await SetSingleDoAsync(TrayVacuum, enable);
            #endregion

            #region 对外：真空破 读写
            public bool GetBreakVacuum() => ReadSingleDo(BreakVacuum);
            public async Task<bool> GetBreakVacuumAsync() => await ReadSingleDoAsync(BreakVacuum);
            public void SetBreakVacuum(bool enable) => SetSingleDo(BreakVacuum, enable);
            public async Task SetBreakVacuumAsync(bool enable) => await SetSingleDoAsync(BreakVacuum, enable);
            #endregion

            #region 对外：切割水打开 读写
            public bool GetCutWaterOpen() => ReadSingleDo(CutWaterOpen);
            public async Task<bool> GetCutWaterOpenAsync() => await ReadSingleDoAsync(CutWaterOpen);
            public void SetCutWaterOpen(bool enable) => SetSingleDo(CutWaterOpen, enable);
            public async Task SetCutWaterOpenAsync(bool enable) => await SetSingleDoAsync(CutWaterOpen, enable);
            #endregion

            #region 对外：产品吹气 读写
            public bool GetProductBlow() => ReadSingleDo(ProductBlow);
            public async Task<bool> GetProductBlowAsync() => await ReadSingleDoAsync(ProductBlow);
            public void SetProductBlow(bool enable) => SetSingleDo(ProductBlow, enable);
            public async Task SetProductBlowAsync(bool enable) => await SetSingleDoAsync(ProductBlow, enable);
            #endregion

            #region 对外：镜头气缸回 读写
            public bool GetCameraCylinderBack() => ReadSingleDo(CameraCylinderBack);
            public async Task<bool> GetCameraCylinderBackAsync() => await ReadSingleDoAsync(CameraCylinderBack);
            public void SetCameraCylinderBack(bool enable) => SetSingleDo(CameraCylinderBack, enable);
            public async Task SetCameraCylinderBackAsync(bool enable) => await SetSingleDoAsync(CameraCylinderBack, enable);
            #endregion

            #region 对外：镜头气缸出 读写
            public bool GetCameraCylinderOut() => ReadSingleDo(CameraCylinderOut);
            public async Task<bool> GetCameraCylinderOutAsync() => await ReadSingleDoAsync(CameraCylinderOut);
            public void SetCameraCylinderOut(bool enable) => SetSingleDo(CameraCylinderOut, enable);
            public async Task SetCameraCylinderOutAsync(bool enable) => await SetSingleDoAsync(CameraCylinderOut, enable);
            #endregion

            #region 对外：三色灯红 读写
            public bool GetLightRed() => ReadSingleDo(LightRed);
            public async Task<bool> GetLightRedAsync() => await ReadSingleDoAsync(LightRed);
            public void SetLightRed(bool enable) => SetSingleDo(LightRed, enable);
            public async Task SetLightRedAsync(bool enable) => await SetSingleDoAsync(LightRed, enable);
            #endregion

            #region 对外：三色灯黄 读写
            public bool GetLightYellow() => ReadSingleDo(LightYellow);
            public async Task<bool> GetLightYellowAsync() => await ReadSingleDoAsync(LightYellow);
            public void SetLightYellow(bool enable) => SetSingleDo(LightYellow, enable);
            public async Task SetLightYellowAsync(bool enable) => await SetSingleDoAsync(LightYellow, enable);
            #endregion

            #region 对外：三色灯绿 读写
            public bool GetLightGreen() => ReadSingleDo(LightGreen);
            public async Task<bool> GetLightGreenAsync() => await ReadSingleDoAsync(LightGreen);
            public void SetLightGreen(bool enable) => SetSingleDo(LightGreen, enable);
            public async Task SetLightGreenAsync(bool enable) => await SetSingleDoAsync(LightGreen, enable);
            #endregion

            #region 对外：蜂鸣器 读写
            public bool GetBuzzer() => ReadSingleDo(Buzzer);
            public async Task<bool> GetBuzzerAsync() => await ReadSingleDoAsync(Buzzer);
            public void SetBuzzer(bool enable) => SetSingleDo(Buzzer, enable);
            public async Task SetBuzzerAsync(bool enable) => await SetSingleDoAsync(Buzzer, enable);
            #endregion

            #region 对外：测高继电器输出 读写
            public bool GetHeightRelayOut() => ReadSingleDo(HeightRelayOut);
            public async Task<bool> GetHeightRelayOutAsync() => await ReadSingleDoAsync(HeightRelayOut);
            public void SetHeightRelayOut(bool enable) => SetSingleDo(HeightRelayOut, enable);
            public async Task SetHeightRelayOutAsync(bool enable) => await SetSingleDoAsync(HeightRelayOut, enable);
            #endregion

            #region 对外：电刷继电器输出 读写
            public bool GetBrushRelayOut() => ReadSingleDo(BrushRelayOut);
            public async Task<bool> GetBrushRelayOutAsync() => await ReadSingleDoAsync(BrushRelayOut);
            public void SetBrushRelayOut(bool enable) => SetSingleDo(BrushRelayOut, enable);
            public async Task SetBrushRelayOutAsync(bool enable) => await SetSingleDoAsync(BrushRelayOut, enable);
            #endregion

            #region 对外：切割安全门锁 读写
            public bool GetCutSafetyLock() => ReadSingleDo(CutSafetyLock);
            public async Task<bool> GetCutSafetyLockAsync() => await ReadSingleDoAsync(CutSafetyLock);
            public void SetCutSafetyLock(bool enable) => SetSingleDo(CutSafetyLock, enable);
            public async Task SetCutSafetyLockAsync(bool enable) => await SetSingleDoAsync(CutSafetyLock, enable);
            #endregion

            #region 对外：复位按钮灯 读写
            public bool GetResetBtnLight() => ReadSingleDo(ResetBtnLight);
            public async Task<bool> GetResetBtnLightAsync() => await ReadSingleDoAsync(ResetBtnLight);
            public void SetResetBtnLight(bool enable) => SetSingleDo(ResetBtnLight, enable);
            public async Task SetResetBtnLightAsync(bool enable) => await SetSingleDoAsync(ResetBtnLight, enable);
            #endregion

            #region 对外：真空开按钮灯 读写
            public bool GetVacuumBtnLight() => ReadSingleDo(VacuumBtnLight);
            public async Task<bool> GetVacuumBtnLightAsync() => await ReadSingleDoAsync(VacuumBtnLight);
            public void SetVacuumBtnLight(bool enable) => SetSingleDo(VacuumBtnLight, enable);
            public async Task SetVacuumBtnLightAsync(bool enable) => await SetSingleDoAsync(VacuumBtnLight, enable);
            #endregion

            #region 批量读取全部DO状态
            public AllDoState ReadAllDo()
            {
                byte[] buf = new byte[2];
                int ret = GTN_EcatIOReadInput(_core,5, 0, 2, out buf[0]);
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
        /// <summary>
        /// 批量读取do
        /// </summary>
        /// <returns></returns>
            public async Task<AllDoState> ReadAllDoAsync()
            {
                return await Task.Run(ReadAllDo);
            }
            #endregion


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