using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Entities
{
    [Table("cut_params_table")]
    public class CutParamsEntity
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("tape_thickness")]
        public float TapeThickness { get; set; }

        [Column("spindle_rev")]
        public int SpindleRev { get; set; }

        [Column("cut_height")]
        public float CutHeight { get; set; }

        [Column("precut_process_no")]
        public string PrecutProcessNo { get; set; }

        [Column("hightest_cut_speed")]
        public float HightestCutSpeed { get; set; }

        [Column("cut_num")]
        public int CutNum { get; set; }

        [Column("cut_size")]
        public float CutSize { get; set; }

        [Column("work_thickness")]
        public float WorkThickness { get; set; }

        [Column("offset_x")]
        public float OffsetX { get; set; }

        [Column("check_marks_cut_times")]
        public int CheckMarksCutTimes { get; set; }
    }
}
