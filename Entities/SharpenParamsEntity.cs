using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Entities
{
    [Table("sharpen_params_table")]
    public class SharpenParamsEntity
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("spindle_rev")]
        public int SpindleRev { get; set; }

        [Column("sharpen_thickness")]
        public float SharpenThickness { get; set; }

        [Column("tape_thickness")]
        public float TapeThickness { get; set; }

        [Column("cut_height")]
        public float CutHeight { get; set; }

        [Column("cut_size")]
        public float CutSize { get; set; }

        [Column("cut_num")]
        public int CutNum { get; set; }

        [Column("offset_x")]
        public float OffsetX { get; set; }

        [Column("hightest_cut_speed")]
        public float HightestCutSpeed { get; set; }
    }
}
