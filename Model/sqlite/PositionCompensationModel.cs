using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace 精密切割系统.database.db.modle
{
    [Table("position_compensation")]
    internal class PositionCompensationModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Timestamp]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("axis_type")]   //轴类型
        public string AxisType { get; set; } = "X轴";

        [Column("usage_status")]
        public string UsageStatus { get; set; } = "不启用";

        [Column("axis_position")]   //轴位置，每个位置使用英文逗号分隔，初始化0到399共400个位置
        public string AxisPosition { get; set; } = initPostion(0, 500, 1);

        [Column("axis_compensate")]   //轴位置补偿 实际位置激光，对应位置的补偿值，每个位置使用英文逗号分隔，初始化400个全0
        public string AxisCompensate { get; set; } = string.Join(",", Enumerable.Repeat(0, 500).ToList());

        [Column("axis_grating_ruler")]   //轴位置补偿 光栅尺，对应位置的补偿值，每个位置使用英文逗号分隔，初始化400个全0
        public string AxisGratingRuler { get; set; } = string.Join(",", Enumerable.Repeat(0, 500).ToList());

        public static string initPostion(float start = 0, float end = 500, float steps = 1)
        {
            string res = "";
            List<string> list = new List<string>();
            for (float i = start; i < end; i += steps)
            {
                list.Add("0.0000");
            }
            res = string.Join(",", list);
            return res;
        }
    }
}
