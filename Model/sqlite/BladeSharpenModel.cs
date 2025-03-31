using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace 精密切割系统.database.db.modle
{
    [Table("blade_sharpen")]
    internal class BladeSharpenModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("unit")]//单位 mm/inch
        public string BladeUnit { get; set; } = "mm";

        [Column("dress_data_no")]//磨刀号
        public string BladeLotID { get; set; }

        [Column("dress_setup_after_dressing")]//是否磨刀后测高
        public bool DressSetup { get; set; }

        [Column("cut_num")]//切割刀数
        public int CutNum { get; set; }

        [Column("spindle_rev")]//主轴转数/min
        public int SpindleRev { get; set; }

        [Column("x_stroke")]//X轴行程(mm)
        public string XStroke { get; set; }

        [Column("y_stroke")]//Y轴行程(mm)
        public string YStroke { get; set; }

        [Column("cut_mode")]//切割方式(mm)
        public string CutMode { get; set; }

        [Column("tape_thickness")]//膜的厚度(mm)
        public string TapeThickness { get; set; }

        [Column("work_thickness")]//切割片厚度
        public string WorkThickness { get; set; }

        [Column("cut_index")]//进刀尺寸
        public string CutIndex { get; set; }

        [Column("blade_height")]//刀片高度
        public string BladeHeight { get; set; }

        [Column("cut_dir")]//切割方向
        public string CutDir { get; set; }

        [Column("feed_speed")]//第N次进刀速度（mm/s）数组，逗号连接成字符串存储
        public string FeedSpeed { get; set; }

        [Column("of_line")]//第N次进多少刀（刀）数组，逗号连接成字符串存储
        public string OfLine { get; set; }
    }
}
