using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Helpers.GTN
{
    /// <summary>
    /// 轴状态位定义（使用 Flags 特性支持位运算）
    /// </summary>
    [Flags]
    public enum AxisStatusBits : int
    {
        None = 0,

        /// <summary>驱动器报警 (Bit 1)</summary>
        DriverAlarm = 1 << 1,

        /// <summary>跟随误差越限 (Bit 4)</summary>
        FollowingError = 1 << 4,

        /// <summary>正限位触发 (Bit 5)</summary>
        PositiveLimit = 1 << 5,

        /// <summary>负限位触发 (Bit 6)</summary>
        NegativeLimit = 1 << 6,

        /// <summary>IO平滑停止触发 (Bit 7)</summary>
        SmoothStop = 1 << 7,

        /// <summary>IO急停触发 (Bit 8)</summary>
        EmergencyStop = 1 << 8,

        /// <summary>电机使能 (Bit 9)</summary>
        MotorEnabled = 1 << 9,

        /// <summary>规划运动 (Bit 10)</summary>
        MotionActive = 1 << 10,

        /// <summary>电机到位 (Bit 11)</summary>
        InPosition = 1 << 11,

        /// <summary>驱动器到位 (Bit 12, EtherCAT only)</summary>
        DriverInPosition = 1 << 12,
    }
}