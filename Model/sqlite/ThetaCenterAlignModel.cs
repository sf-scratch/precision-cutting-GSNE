using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.sqlite
{
    [Table("theta_center_align")]
    class ThetaCenterAlignModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("work_size")]
        public string workSize { get; set; }
        [Column("work_thickness")]
        public string workThickness { get; set; }
        [Column("tape_thickness")]
        public string tapeThickness { get; set; }
        [Column("blade_height")]
        public string bladeHeight { get; set; }
        [Column("cut_speed")]
        public string cutSpeed { get; set; }
        [Column("spindle_speed")]
        public string spindleSpeed { get; set; }
    }
}
