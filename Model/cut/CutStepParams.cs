using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Driver;

namespace 精密切割系统.Model.cut
{
    internal class CutStepParams
    {
        /// <summary>
        /// 切割深度
        /// </summary>
        public float CutDepth { get; set; }

        /// <summary>
        /// 进给速度
        /// </summary>
        public float FeedSpeed { get; set; }

        /// <summary>
        /// 切割刀高度
        /// </summary>
        public float CutBladeHeight { get; set; }

        /// <summary>
        /// Y轴索引
        /// </summary>
        public float YIndexs { get; set; }

        /// <summary>
        /// 重复次数
        /// </summary>
        public float RepeatedCuttingTimes { get; set; }
    }
}
