using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut
{
    public class SharpenStep
    {
        /// <summary>
        /// Z轴高度
        /// </summary>
        /// </summary>
        public float CutZ { get; set; }

        /// <summary>
        /// 进给速度
        /// </summary>
        public float FeedSpeed { get; set; }

        /// <summary>
        /// 进刀尺寸
        /// </summary>
        public float CutSize { get; set; }

        /// <summary>
        /// theta角度
        /// </summary>
        public float ThetaDeg { get; set; }

        /// <summary>
        /// 切割方向
        /// </summary>
        public CutDirection Direction { get; set; }

        /// <summary>
        /// 是否为该通道的第一次切割
        /// </summary>
        public bool IsChanelFirstStep { get; set; }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns></returns>
        public SharpenStep DeepCopy()
        {
            return new SharpenStep
            {
                CutZ = this.CutZ,
                FeedSpeed = this.FeedSpeed,
                CutSize = this.CutSize,
                ThetaDeg = this.ThetaDeg,
                Direction = this.Direction,
                IsChanelFirstStep = this.IsChanelFirstStep
            };
        }
    }
}
