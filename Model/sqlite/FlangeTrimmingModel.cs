using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.sqlite
{
    [Table("flange_trimming")]
    public class FlangeTrimmingModel
    {
        [PrimaryKey]
        public int Id { get; set; }

        [Column("x_center_position")]// x轴中心点位置
        public float XCenterPosition { get; set; }
        
        [Column("y_center_position")]// y轴开始位置
        public float YCenterPosition { get; set; }

        [Column("z_center_position")]// z轴开始位置
        public float ZCenterPosition { get; set; }

        [Column("spindle_rev")]//主轴转数/min
        public int SpindleRev { get; set; }

        [Column("co_x_distance")]//x轴行程（mm）
        public float CoXDistance { get; set; }

        [Column("cut_index")]//进刀尺寸
        public string CutIndex { get; set; }

        [Column("cut_speed")]//进刀速度
        public string CutSpeed { get; set; }

        // 重复次数
        [Column("repeat_count")]
        public int RepeatCount { get; set; } = 2;

        // 重复次数
        [Column("all_repeat_count")]
        public int AllRepeatCount { get; set; } = 2;
        // 重复次数
        [Column("grinding_step_interval")]
        public int GrindingStepInterval { get; set; } = 2;

        [Column("x_low_speed")]
        public string XLowSpeed {  get; set; }

        [Column("y_low_speed")]
        public string YLowSpeed { get; set; }

    }
}
