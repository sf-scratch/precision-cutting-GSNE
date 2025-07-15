using SQLite;

namespace 精密切割系统.Entities
{
    [Table("knife_wear_table")]
    public class KnifeWearEntity
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("start_time")]
        public DateTime StartTime { get; set; }  // 开始时间

        [Column("end_time")]
        public DateTime EndTime { get; set; }  // 结束时间

        [Column("sharpen_count")]
        public int SharpenCount { get; set; }     // 磨刀次数

        [Column("wear_amount")]
        public float WearAmount { get; set; }    // 磨损量(mm)

        [Column("last_sharpen_count")]
        public int LastSharpenCount { get; set; }     // 最后磨刀次数

        [Column("last_wear_amount")]
        public float LastWearAmount { get; set; }    // 最后磨损量(mm)

        [Column("cut_count")]
        public int CutCount { get; set; }       // 切割刀数

        [Column("first_cut_image")]
        public string FirstCutImage { get; set; }   // 第一刀刀痕图片

        [Column("second_cut_image")]
        public string SecondCutImage { get; set; }     // 第二刀刀痕图片

        [Column("last_cut_image")]
        public string LastCutImage { get; set; }     // 最后一刀刀痕图片
    }
}
