using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut
{
    public enum AutoRunStatus
    {
        None,
        /// <summary>
        /// 测高模式中
        /// </summary>
        HeightMeasurementInProgress,
        /// <summary>
        /// 自动聚焦中
        /// </summary>
        AutoFocus,
        /// <summary>
        /// 磨刀校准中
        /// </summary>
        SharpenCalibrat,
        /// <summary>
        /// 切割校准中
        /// </summary>
        CutingCalibrat,
        /// <summary>
        /// 磨刀中
        /// </summary>
        SharpeningInProgress,
        /// <summary>
        /// 切割中
        /// </summary>
        CutingInProgress,
        /// <summary>
        /// 替换磨刀板
        /// </summary>
        ReplaceSharpenBoard,
        /// <summary>
        /// 替换硅片
        /// </summary>
        ReplaceWafer,
        /// <summary>
        /// 结束
        /// </summary>
        End
    }
}
