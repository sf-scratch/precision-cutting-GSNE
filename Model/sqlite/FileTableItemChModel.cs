using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace 精密切割系统.database.db.modle
{
    [Table("file_table_item_ch")]
    internal class FileTableItemChModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("item_id")]//上级ID
        public long ItemId { get; set; }

        [Column("ch_name")]//当前名称
        public string ChName { get; set; }

        [Column("theta_deg")]//θ角度
        public string ThetaDeg { get; set; } = "0";

        [Column("cut_mode")]//切割方式
        public string CutMode { get; set; } = "A";

        [Column("cut_dir")]//切割方向 
        public string CutDir { get; set; } = "FRONT";

        [Column("cut_line")]//切割刀数 
        public string CutLine { get; set; } = "0";

        [Column("offset_y")]//校准 
        public string OffsetY { get; set; } = "0";

        [Column("absolute_cut_position")] //绝对切割位置
        public string AbsoluteCutPosition { get; set; }

        [Column("moncut_f")]//切割停止F 
        public string MoncutF { get; set; } = "0";

        [Column("moncut_r")]//切割停止R 
        public string MoncutR { get; set; } = "0";

        [Column("blade_angle")]//刀片角度
        public string BladeAngle { get; set; } = "60";

        [Column("offset_x")]//x轴偏移量 
        public string OffsetX { get; set; } = "0";

        [Column("offset_theta")]//θ轴偏移量 
        public string OffsetTheta { get; set; } = "0";

        [Column("blade_height")]//SEQ1-刀片高度
        public string BladeHeight { get; set; } = getDefault("BladeHeight").ToString();

        [Column("feed_speed")]//SEQ1-进刀速度  整数
        public string FeedSpeed { get; set; } = getDefault("FeedSpeed").ToString();

        [Column("y_index")]//SEQ1-Y轴移动量；4位小数
        public string YIndex { get; set; } = getDefault("YIndex").ToString();

        [Column("repeat_times")]//SEQ1-切割刀数 整数
        public string RepeatTimes { get; set; } = getDefault("RepeatTimes").ToString();

        [Column("depth_steps")]//SEQ1-刀片深度
        public string DepthSteps { get; set; } = getDefault("DepthSteps").ToString();

        [Column("loop")]//SEQ1-Loop；整数字加英文，英文只能输入S
        public string Loop { get; set; } = getDefault("Loop").ToString();

        [Column("z_down_speed")]//SEQ1-Z轴下降速度
        public string ZDownSpeed { get; set; } = getDefault("ZDownSpeed").ToString();


        //设置默认值；共30个
        private static string getDefault(string name)
        {
            int count = 30;
            string[] defData = new string[count];
            for (int i = 0; i < count; i++)
            {
                if (name.Equals("Loop"))
                {
                    defData[i] = "0";
                }else if (name.Equals("YIndex"))
                {
                    defData[i] = "0.0000";
                }
                else if (name.Equals("RepeatTimes")|| name.Equals("FeedSpeed"))
                {
                    defData[i] = "0";
                }
                else
                {
                    defData[i] = "0.000";
                }
            }
            string result = string.Join(",", defData);
            return result;
        }


    }
}
