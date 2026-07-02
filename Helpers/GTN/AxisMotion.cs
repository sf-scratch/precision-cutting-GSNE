using DryIoc.FastExpressionCompiler.LightExpression;
using HslCommunication.Profinet.Keyence;
using NPOI.OpenXmlFormats.Spreadsheet;
using NPOI.SS.Formula.Functions;
using ScottPlot;
using ScottPlot.AxisPanels;
using ScottPlot.Colormaps;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using 精密切割系统.Driver;
using 精密切割系统.Extensions;
using 精密切割系统.Model.cut;
using 精密切割系统.Utils;
using 精密切割系统.ViewModel;
using static 精密切割系统.Helpers.GTN.mc;
using static 精密切割系统.Helpers.GTN.mc_la;

namespace 精密切割系统.Helpers.GTN
{
    public class AxisMotion
    {
        private short _core = GsneConfig.Instance.Core;
        private readonly AxisOrigin _axisOrigin = new AxisOrigin();

        /// <summary>
        /// 全局整机初始化完成标记
        /// </summary>
        public bool IsMachineInitComplete { get; set; }//是否整机初始化完成

        // 对外整机回零接口
        public async Task<bool> AllAxisHomingAsync(CancellationToken token = default, int timeoutMs = 90000)
        {
            bool rtu = await _axisOrigin.AllAxisHomingAsync(token, timeoutMs);
            if (rtu)
            {
                IsMachineInitComplete = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 轴是否准备好
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsReadyAsync(AxisType axis, params AxisStatusBits[] ignore)
        {
            return await Task.Run(() =>
            {
                GTN_GetStsEx(_core, (short)axis, out int pSts, 1, out uint pClock);
                if ((pSts & (int)AxisStatusBits.MotorEnabled) == 0)
                {
                    return false;
                }
                AxisStatusBits bitsOff = AxisStatusBits.DriverAlarm | AxisStatusBits.FollowingError | AxisStatusBits.PositiveLimit | AxisStatusBits.NegativeLimit | AxisStatusBits.SmoothStop | AxisStatusBits.EmergencyStop | AxisStatusBits.MotionActive;
                foreach (var bit in ignore)
                {
                    bitsOff &= ~bit;
                }
                return (pSts & (int)bitsOff) == 0;
            });
        }

        /// <summary>
        /// 轴到位完成
        /// </summary>
        /// <param name="axis">轴类型</param>
        /// <returns>true=到位标记置1</returns>
        public async Task<bool> IsPositionArrivedAsync(AxisType axis)
        {
            return await Task.Run(() =>
            {
                GTN_GetStsEx(_core, (short)axis, out int pSts, 1, out uint pClock);
                int bit11Mask = 1 << 11;
                return (pSts & bit11Mask) != 0;//按位与判断
            });
        }

        /// <summary>
        /// 等待轴准备好
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task WaitAxisReadyAsync(AxisType axis, CancellationToken token, params AxisStatusBits[] ignore)
        {
            await TaskUtils.WaitExpectedResultAsync(() => IsReadyAsync(axis, ignore), default, token);
        }

        /// <summary>
        /// 轴放松
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public async Task AxisOffAsync(AxisType axis)
        {
            var rtn = GTN_AxisOff(_core, (short)axis);
        }

        /// <summary>
        /// 轴上使能
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public async Task AxisOnAsync(AxisType axis)
        {
            var rtn = GTN_AxisOn(_core, (short)axis);
        }

        /// <summary>
        /// 回零点
        /// </summary>
        public async Task StartHomingAsync(AxisType axis)
        {
            // 1. 在后台线程执行初始化（同步部分）
            await Task.Run(() =>
            {
                short rtn;
                int offset = GsneConfig.Instance.Axes[axis].OffsetMM.MMToPulseF(axis);
                short model = GsneConfig.Instance.Axes[axis].HomingModel;
                double speed1 = GsneConfig.Instance.Axes[axis].HighSpeed.MMToPulse(axis);
                double speed2 = GsneConfig.Instance.Axes[axis].LowSpeed.MMToPulse(axis);
                ushort probeFunc = 0;

                rtn = GTN_SetEcatHomingPrm(_core, (short)axis, model, speed1, speed2, speed2, offset, probeFunc);
                rtn = GTN_SetHomingMode(_core, (short)axis, 6);
                Thread.Sleep(100);  // 延时 100ms
                rtn = GTN_StartEcatHoming(_core, (short)axis);
            });

            // 2. 轮询等待回零完成
            while (!await IsCompleteHomingAsync(axis))
            {
                await Task.Delay(100);  // 每 100ms 查询一次
            }

            // 3. 回零完成后设置模式 8
            await Task.Run(() =>
            {
                short rtn = GTN_SetHomingMode(_core, (short)axis, 8);
                GTN_ZeroPos(_core, (short)axis, (short)axis);
            });
        }

        /// <summary>
        /// 回零点是否完成
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public async Task<bool> IsCompleteHomingAsync(AxisType axis)
        {
            return await Task.Run(() =>
            {
                //GTN_GetStandardHomeStatus(_core, (short)axis, out TStandardHomeStatus pHomeStatus);
                //return pHomeStatus.stage == STANDARD_HOME_STAGE_END;
                GTN_GetEcatHomingStatus(_core, (short)axis, out ushort pHomeStatus);

                // Bit 0: 1 = 回零完成
                bool isComplete = (pHomeStatus & 0x01) == 1;

                // Bit 1: 1 = 回零成功完成（可选，如果需要严格判断成功）
                bool isSuccess = (pHomeStatus & 0x02) == 2;

                // Bit 2: 1 = 回零出错（需要处理错误）
                bool hasError = (pHomeStatus & 0x04) == 4;

                return isComplete;
            });
        }

        /// <summary>
        /// 设置软限位
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="positive"></param>
        /// <param name="negative"></param>
        /// <returns></returns>
        public async Task SetSoftLimit(AxisType axis, float positive, float negative)
        {
            await Task.Run(() =>
            {
                GTN_SetSoftLimit(_core, (short)axis, positive.MMToPulseF(axis), negative.MMToPulseF(axis));
            });
        }

        /// <summary>
        /// 点动JOG开始
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="speed">速度</param>
        public async Task StartJogAsync(AxisType axis, double speed)
        {
            await Task.Run(() =>
            {
                short rtn;
                rtn = GTN.mc.GTN_PrfJog(_core, (short)axis);
                rtn = GTN.mc.GTN_GetJogPrm(_core, (short)axis, out TJogPrm jog);
                jog.acc = GsneConfig.Instance.Axes[axis].HighSpeed;
                jog.dec = GsneConfig.Instance.Axes[axis].LowSpeed;
                // 设置 Jog 运动参数
                rtn = GTN.mc.GTN_SetJogPrm(_core, (short)axis, ref jog);
                rtn = GTN.mc.GTN_SetVel(_core, (short)axis, speed.MmPerSecToPulsePerMs(axis));
                rtn = GTN.mc.GTN_Update(_core, 1 << ((short)axis - 1));
            });
        }

        /// <summary>
        /// 点动停止
        /// </summary>
        /// <param name="speed">速度</param>
        /// <param name="jogDirection">方向 0 正 1 负</param>
        public async Task StopJogAsync(AxisType axis)
        {
            await Task.Run(() =>
            {
                short rtn;
                rtn = GTN.mc.GTN_Stop(_core, 1 << ((short)axis - 1), 0);//平滑停
            });
        }

        /// <summary>
        /// 开始绝对运动
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="location"></param>
        /// <param name="speed"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task StartAbsoluteAsync(AxisType axisType, float location, float speed, CancellationToken token)
        {
            short sCore = _core;
            short axis = (short)axisType;
            GTN.mc.TTrapPrm trap;
            var rtn = GTN.mc.GTN_PrfTrap(sCore, axis);
            rtn = GTN.mc.GTN_GetTrapPrm(sCore, axis, out trap);
            trap.acc = GsneConfig.Instance.Axes[axisType].HighSpeed.MMToPulse(axisType);
            trap.dec = GsneConfig.Instance.Axes[axisType].LowSpeed.MMToPulse(axisType);
            trap.smoothTime = 50;
            rtn = GTN.mc.GTN_SetTrapPrm(sCore, axis, ref trap);
            rtn = GTN.mc.GTN_SetVel(sCore, axis, speed.MmPerSecToPulsePerMs(axisType));
            rtn = GTN.mc.GTN_SetPos(sCore, axis, (int)(location.MMToPulseF(axisType)));
            rtn = GTN.mc.GTN_Update(sCore, 1 << (axis - 1));

            // 等待绝对运动完成
            //await WaitAxisStopAsync(token.WithDefaultTimeout(TimeSpan.FromSeconds(waitTime)));
        }

        /// <summary>
        /// 开始相对运动
        /// </summary>
        /// <param name="distance">相对移动距离</param>
        /// <param name="speed">速度</param>
        /// <param name="token">取消令牌</param>
        /// <returns></returns>
        public async Task StartRelativeAsync(AxisType axis, float distance, float speed, CancellationToken token)
        {
            float? curLocation = await GetCurrentLocationAsync(axis);
            if (curLocation != null)
            {
                await StartAbsoluteAsync(axis, curLocation.Value + distance, speed.MmPerSecToPulsePerMs(axis), token);
            }
        }

        /// <summary>
        /// 获取当前轴位置
        /// </summary>
        /// <returns></returns>
        public async Task<float?> GetCurrentLocationAsync(AxisType axis)
        {
            double? rawLocation = await Task.Run(() =>
            {
                short rtn = GTN_GetEncPos(_core, (short)axis, out double pos, 1, out uint status);
                return rtn != 0 ? (double?)null : pos;
            });

            if (!rawLocation.HasValue)
                return null;

            return (float)rawLocation.Value.PulseToMM(axis);
        }

        /// <summary>
        /// 执行插补运动
        /// </summary>
        /// <param name="xInterpolationMotionValue"></param>
        /// <param name="yInterpolationMotionValue"></param>
        /// <returns></returns>
        public async Task RunMotionAsync(float xInterpolationMotionValue, float yInterpolationMotionValue, CancellationToken token = default)
        {
            if (!GlobalParams.OnlineFlag) return;
            await Task.Run(() =>
            {
                short crd = 1;
                TCrdPrm crdprm1;
                crdprm1.dimension = 2;//坐标系的维数为4维
                crdprm1.synVelMax = 500;//最大合成速度为500
                crdprm1.synAccMax = 100;//最大合成加速度为100
                crdprm1.evenTime = 10;//最小匀速时间为50ms
                crdprm1.profile1 = 1;//规划器1对应X轴
                crdprm1.profile2 = 2;//规划器2对应Y轴
                crdprm1.profile3 = 0;//规划器3对应Z轴
                crdprm1.profile4 = 0;//规划器4对应A轴
                crdprm1.profile5 = 0;//规划器1对应X轴
                crdprm1.profile6 = 0;//规划器2对应Y轴
                crdprm1.profile7 = 0;//规划器3对应Z轴
                crdprm1.profile8 = 0;//规划器2对应A轴
                crdprm1.setOriginFlag = 0;//1表示需要用户指定坐标原点的规划位置
                crdprm1.originPos1 = 0;//1轴的规划位置为0
                crdprm1.originPos2 = 0;//2轴的规划位置为0
                crdprm1.originPos3 = 0;//3轴的规划位置为0
                crdprm1.originPos4 = 0;//2轴的规划位置为0
                crdprm1.originPos5 = 0;//1轴的规划位置为0
                crdprm1.originPos6 = 0;//2轴的规划位置为0
                crdprm1.originPos7 = 0;//3轴的规划位置为0
                crdprm1.originPos8 = 0;//2轴的规划位置为0
                short rtn = GTN_SetCrdPrm(_core, crd, ref crdprm1);//建立1号坐标系
                rtn = GTN.mc_la.GTN_LnXYEx(_core, crd, xInterpolationMotionValue, yInterpolationMotionValue, 10, 100, 0, 0, 0); //压入第一段位置指令
                                                                                                                                //rtn = GTN.mc_la.GTN_LnXYEx(_core, crd, 20, 20, 10, 100, 0, 0, 0);                             //压入第二段位置指令
                                                                                                                                //rtn = GTN.mc_la.GTN_LnXYEx(_core, crd, 20, 0, 10, 100, 0, 0, 0);                              //压入第三段位置指令
                                                                                                                                //rtn = GTN.mc_la.GTN_LnXYEx(_core, crd, 0, 0, 10, 100, 0, 0, 0);                               //压入第四段位置指令
                rtn = GTN.mc_la.GTN_CrdDataEx(_core, crd, IntPtr.Zero, 0);                                  //将前瞻缓存区中的数据压入控制器
                rtn = GTN.mc.GTN_CrdStart(_core, crd, 0);
            });
        }

        /// <summary>
        /// 设置切割需要的参数
        /// </summary>
        /// <param name="feedSpeedValue">切割速度</param>
        /// <param name="zEndIndex">Z轴切割位置</param>
        /// <param name="xEndLocation">X轴结束位置</param>
        /// <param name="yCutLocation">Y轴切割位置</param>
        /// <param name="spindleRev">主轴转速</param>
        public async Task SetCutParamsAsync(float feedSpeedValue, float zEndLocation, float zStartLocation, float xStartLoaction, float xEndLocation,
            float yCutLocation, float thetaDeg, int spindleRevValue, float depthEntry, CancellationToken token)
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
                $"\r\n" +
                $"切割速度: {feedSpeedValue}\r\n" +
                $"Z轴开始位置: {zStartLocation}\r\n" +
                $"Z轴结束位置: {zEndLocation}\r\n" +
                $"X轴开始位置: {xStartLoaction}\r\n" +
                $"X轴结束位置: {xEndLocation}\r\n" +
                $"Y轴切割位置: {yCutLocation}\r\n" +
                $"theta角度: {thetaDeg}\r\n" +
                $"主轴转速: {spindleRevValue}\r\n" +
                $""
                );
            TMoveAbsolutePrm prmStartZ = new()
            {
                pos = ((zStartLocation - depthEntry).MMToPulseF(AxisType.Z1)),
                vel = 50,
                acc = 0.5,
                dec = 0.5,
                percent = 10
            };
            GTN_MoveAbsolute(_core, (short)AxisType.Z1, ref prmStartZ);
            await WaitAxisReadyAsync(AxisType.Z1, token);

            TMoveAbsolutePrm prmX = new()
            {
                pos = (xStartLoaction.MMToPulseF(AxisType.X)),
                vel = 50,
                acc = 0.5,
                dec = 0.5,
                percent = 10
            };
            GTN_MoveAbsolute(_core, (short)AxisType.X, ref prmX);

            TMoveAbsolutePrm prmY = new()
            {
                pos = (yCutLocation.MMToPulseF(AxisType.Y)),
                vel = 50,
                acc = 0.5,
                dec = 0.5,
                percent = 10
            };
            GTN_MoveAbsolute(_core, (short)AxisType.Y, ref prmY);

            TMoveAbsolutePrm prmTheta = new()
            {
                pos = (thetaDeg.MMToPulseF(AxisType.Theta)),
                vel = 50,
                acc = 0.5,
                dec = 0.5,
                percent = 10
            };
            GTN_MoveAbsolute(_core, (short)AxisType.Theta, ref prmTheta);

            await Task.WhenAll(WaitAxisReadyAsync(AxisType.X, token), WaitAxisReadyAsync(AxisType.Y, token), WaitAxisReadyAsync(AxisType.Theta, token));

            TMoveAbsolutePrm prmZ = new()
            {
                pos = (zStartLocation.MMToPulseF(AxisType.Z1)),
                vel = 50,
                acc = 0.5,
                dec = 0.5,
                percent = 10
            };
            GTN_MoveAbsolute(_core, (short)AxisType.Z1, ref prmZ);
            await WaitAxisReadyAsync(AxisType.Z1, token);

            TMoveAbsolutePrm prmEndX = new()
            {
                pos = (xEndLocation.MMToPulseF(AxisType.X)),
                vel = feedSpeedValue,
                acc = 0.5,
                dec = 0.5,
                percent = 10
            };
            GTN_MoveAbsolute(_core, (short)AxisType.X, ref prmEndX);
            await WaitAxisReadyAsync(AxisType.X, token);

            await StartRelativeAsync(AxisType.Z1, depthEntry, 10, token);

            TMoveAbsolutePrm prmEndZ = new()
            {
                pos = (zEndLocation.MMToPulseF(AxisType.Z1)),
                vel = 50,
                acc = 0.5,
                dec = 0.5,
                percent = 10
            };
            GTN_MoveAbsolute(_core, (short)AxisType.Z1, ref prmEndZ);
        }
    }
}