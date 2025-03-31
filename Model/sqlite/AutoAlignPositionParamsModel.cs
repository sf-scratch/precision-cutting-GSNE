using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.ViewModel
{
    [Table("auto_align_position_params")]
    internal class AutoAlignPositionParamsModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("spindle_rev")]//主轴转速
        public int SpindleRev { get; set; } = 12000;

        [Column("workbench_ch1")]// 工作台长
        public float WorkbenchCh1 { get; set; } = 155;

        [Column("workbench_ch2")]// 工作台宽
        public float WorkbenchCh2 { get; set; } = 155;

        [Column("square_ch1")]//长方形-长
        public float SquareCh1 { get; set; } = 100;

        [Column("square_ch2")]//长方形-宽
        public float SquareCh2 { get; set; } = 100;

        [Column("blade_height")]//SEQ1-刀片高度
        public float BladeHeight { get; set; } = 0.0000f;

        [Column("test_count")] // 测量次数
        public int TestCount { get; set; } = 10;

        [Column("feed_speed")]//SEQ1-进刀速度  整数
        public float FeedSpeed { get; set; } = 10;

        [Column("y_index")]//SEQ1-Y轴移动量；4位小数
        public float YIndex { get; set; } = 0.5f;

        [Column("depth_steps")]//SEQ1-刀片深度
        public float DepthSteps { get; set; } = 0.155f;

    }
}
