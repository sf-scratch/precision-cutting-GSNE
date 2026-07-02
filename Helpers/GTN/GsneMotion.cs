using HslCommunication.Profinet.Keyence;
using log4net.Util;
using ScottPlot;
using ScottPlot.Colormaps;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using 精密切割系统.Driver;
using 精密切割系统.Model.common;
using 精密切割系统.Utils;
using 精密切割系统.ViewModel;
using static System.Formats.Asn1.AsnWriter;
using static 精密切割系统.Helpers.GTN.mc;
using static 精密切割系统.Helpers.GTN.mc_la;

namespace 精密切割系统.Helpers.GTN
{
    public class GsneMotion
    {
        private static readonly Lazy<GsneMotion> _lazy = new(() => new GsneMotion());

        public static GsneMotion Instance
        {
            get { return _lazy.Value; }
        }

        public AxisMotion Axis { get; private set; } = new AxisMotion();

        private short _core = GsneConfig.Instance.Core;

        /// <summary>
        /// 连接服务器并进行必要的初始化
        /// </summary>
        /// <returns></returns>
        public async Task<CommonResult> ConnectServerAsync()
        {
            return await Task.Run(() =>
            {
                short rtn;
                //开卡
                rtn = GTN_OpenCard(5, 0, ' ');
                if (rtn != 0)
                {
                    return CommonResult.Failure("开卡失败: " + rtn);
                }
                //复位核
                rtn = GTN_Reset(_core);
                //通讯前需要将之前的通讯断开
                rtn = GTN_TerminateEcatComm(1);
                int iStatus;
                rtn = GTN_NetInit(100, "Assets\\config\\Googol.xml", 20, out iStatus);
                if (rtn != 0)
                {
                    return CommonResult.Failure("网络初始化失败: " + rtn);
                }
                rtn = GTN.mc.GTN_LoadConfig(1, "Assets\\config\\gtn_core1.cfg");
                if (rtn != 0)
                {
                    return CommonResult.Failure($"加载核1配置文件失败");
                }
                //加载cfg配置文件（Ecat卡一般可不加载 注：加载后有问题）
                //rtn = GTN.mc.GTN_LoadConfig(_core, $"Assets\\config\\gtn_core{_core}.cfg");
                //if (rtn != 0)
                //{
                //    return CommonResult.Failure($"加载核{_core}配置文件失败");
                //}
                //之后需要清除状态
                rtn = GTN.mc.GTN_ClrSts(_core, 1, 12);
                //最后清零（回零后需同步位置）
                rtn = GTN.mc.GTN_ZeroPos(_core, 1, 12);
                //加载高速指令配置
                rtn = GTN.mc.GTN_LoadReadHsConfig(1, "gtn.ini");
                //使能高速指令
                rtn = GTN.mc.GTN_ReadHsOn(1, 1, 2, 0);

                short Numberaxis = 5;
                for (short axis = 1; axis <= Numberaxis; axis++)
                {
                    GTN_LmtsOnEx(_core, (short)axis, 1, 0x03);
                    GTN_AxisOn(_core, (short)axis);
                }

                return CommonResult.Success();
            });
        }

        /// <summary>
        /// 执行系统初始化
        /// </summary>
        public async Task SystemInitAsync()
        {
            //await Axis.StartHomingAsync(AxisType.Z1);
            //await Task.WhenAll(
            //    Axis.StartHomingAsync(AxisType.X),
            //    Axis.StartHomingAsync(AxisType.Y),
            //    Axis.StartHomingAsync(AxisType.Z2),
            //    Axis.StartHomingAsync(AxisType.Theta)
            //);
        }

        public async Task<CommonResult<bool>> GetAxisStatusAsync(AxisType axisType, AxisStatusBits axisStatus)
        {
            // 在后台线程执行同步 API 调用
            var status = await Task.Run(() =>
            {
                short rtn = GTN_GetSts(_core, (short)axisType, out int pSts, 1, out uint _);
                return (rtn, pSts);
            });

            var axisAlarms = new List<string>();

            // rtn == 0 表示成功
            if (status.rtn == 0)
            {
                return CommonResult<bool>.Success((status.pSts & (int)axisStatus) != 0);
            }
            return CommonResult<bool>.Failure("状态获取失败！");
        }

        private Dictionary<AxisType, int> _dic = new Dictionary<AxisType, int>();

        public async Task StartMonitorAxisStatusAsync()
        {
            foreach (AxisType axis in Enum.GetValues(typeof(AxisType)))
            {
                _dic.Add(axis, 0);
            }
            while (true)
            {
                foreach (var axis in _dic.Keys)
                {
                    // 在后台线程执行同步 API 调用
                    var status = await Task.Run(() =>
                    {
                        short rtn = GTN_GetStsEx(_core, (short)axis, out int pSts, 1, out uint _);
                        return (rtn, pSts);
                    });
                    if (status.rtn == 0)
                    {
                        if (_dic[axis] != status.pSts)
                        {
                            _dic[axis] = status.pSts;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取所有轴的报警状态
        /// </summary>
        /// <returns>各轴的报警列表</returns>
        public List<ActiveAlarmModel> GetAllAxisAlarms()
        {
            var result = new List<ActiveAlarmModel>();
            var descriptions = GsneConfig.Instance.AxisStatusDescription;

            foreach (AxisType axis in _dic.Keys)
            {
                var axisAlarms = new List<string>();
                int pSts = _dic[axis];
                // 获取所有为 1 的位索引
                var alarmIndices = GetSetBitIndices(pSts);
                foreach (int index in alarmIndices)
                {
                    // 确保索引有效
                    if (index >= 0 && index < descriptions.Length)
                    {
                        string description = descriptions[index];
                        if (!string.IsNullOrEmpty(description) && description != "保留")
                        {
                            axisAlarms.Add($"{axis}轴{description}");
                        }
                    }
                }
                foreach (var alarm in axisAlarms)
                {
                    result.Add(new ActiveAlarmModel() { Message = alarm });
                }
            }

            return result;
        }

        public static List<int> GetSetBitIndices(int value)
        {
            List<int> indices = new List<int>();
            uint unsigned = (uint)value;  // 转为无符号处理，避免负数问题

            while (unsigned != 0)
            {
                // 获取最低位 1 的索引（从 0 开始）
                int index = BitOperations.TrailingZeroCount(unsigned);
                indices.Add(index);

                // 清除最低位 1
                unsigned &= unsigned - 1;
            }

            return indices;
        }

        /// <summary>
        /// 判断轴切割条件是否准备好
        /// 1.轴准备好了 2.轴原点ok
        /// </summary>
        /// <returns>全部轴原点完成+轴就绪返回true</returns>
        public async Task<bool> WaitReadyCuttingAsync()
        {
            AxisType[] checkAxisList = new[] { AxisType.X, AxisType.Y, AxisType.Z1, AxisType.Z2, AxisType.Theta };

            // 校验所有轴回零完成
            foreach (var axis in checkAxisList)
            {
                bool homeFinish = await GsneMotion.Instance.Axis.IsCompleteHomingAsync(axis);
                if (!homeFinish)
                {
                    return false;
                }
            }
            foreach (var axis in checkAxisList)
            {
                bool axisReady = await GsneMotion.Instance.Axis.IsReadyAsync(axis, AxisStatusBits.MotionActive);
                if (!axisReady)
                {
                    return false;
                }
            }
            // 全部轴回零完成 + 全部轴无故障就绪
            return true;
        }

        /// <summary>
        /// 轴是否有硬件报警
        /// </summary>
        /// <returns></returns>
        public async Task<bool> VerAxisAlarm()
        {
            AxisType[] checkAxisList = new[] { AxisType.X, AxisType.Y, AxisType.Z1, AxisType.Z2, AxisType.Theta };

            foreach (var axis in checkAxisList)
            {
                bool axisReady = await GsneMotion.Instance.Axis.IsReadyAsync(axis, AxisStatusBits.MotionActive);
                if (!axisReady)
                {
                    return false;
                }
            }
            //全部轴无故障就绪
            return true;
        }

        public async Task<bool> ResetAllAxisAlarmAsync()
        {
            try
            {
                // 同步硬件操作丢后台，不卡主线程
                await Task.Run(() =>
                {
                    GTN.mc.GTN_ClrSts(_core, 1, 12);
                });
                return true;
            }
            catch (Exception ex)
            {
                MaterialSnack($"轴报警复位失败：{ex.Message}", SnackType.ERROR);
                return false;
            }
        }

        public async Task<bool> AlarmResetAsync(CancellationToken token = default)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                // 1. 复位伺服轴报警
                await ResetAllAxisAlarmAsync();

                token.ThrowIfCancellationRequested();
                // 2. 复位主轴报警
                await SpindleMotionSet.Instance.ResetSpindleAlarmAsync();
                // 3. 清空IO报警缓存标志与列表
                IoAlarm.Instance.ActiveAlarmList.Clear();
                // 重置IO报警标记
                IoAlarm.Instance.HasAnyAlarm = false;

                return true;
            }
            catch (OperationCanceledException)
            {
                MaterialSnack("报警复位操作被取消", SnackType.WARNING);
                return false;
            }
            catch (Exception ex)
            {
                MaterialSnack($"报警复位失败：{ex.Message}", SnackType.ERROR);
                return false;
            }
        }

        public async Task EmergencyLiftSpindleAsync(CancellationToken token = default)
        {
            Task Z1 = GsneMotion.Instance.Axis.StartAbsoluteAsync(AxisType.Z1, 1, 20, token);
            AxisType[] checkAxisList = new[] { AxisType.X, AxisType.Y, AxisType.Theta };
            List<Task> axisTasks = new List<Task> { Z1 };
            foreach (var axis in checkAxisList)
            {
                axisTasks.Add(GsneMotion.Instance.Axis.StopJogAsync(axis));
            }
            await Task.WhenAll(axisTasks);
        }
    }
}