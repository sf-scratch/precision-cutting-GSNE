using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Entities
{
    [Table("automatic_compensation_cut_height_table")]
    internal class AutomaticCompensationCutHeightEntity : IEntityWithId
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("cut_height_compensation_frequency")]
        public string CutHeightCompensationFrequency { get; set; }

        [Column("cut_height_reduction_distance")]
        public string CutHeightReductionDistance { get; set; }

        [Column("current_automatic_compensation_cutheight")]
        public string CurrentAutomaticCompensationCutHeight { get; set; }
    }
}