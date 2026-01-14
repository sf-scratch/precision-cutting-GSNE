using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Entities
{
    [Table("baseline_calibration_table")]
    internal class BaselineCalibrationEntity : BindableBase, IEntityWithId
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("rectangular_length")]
        public string RectangularLength { get; set; }

        [Column("rectangular_width")]
        public string RectangularWidth { get; set; }

        [Column("circular_radius")]
        public string CircularRadius { get; set; }

        [Column("spindle_rev")]
        public string SpindleRev { get; set; }

        [Column("work_thickness")]
        public string WorkThickness { get; set; }

        [Column("tape_thickness")]
        public string TapeThickness { get; set; }

        [Column("blade_height")]
        public string BladeHeight { get; set; }

        [Column("cut_speed")]
        public string CutSpeed { get; set; }
    }
}