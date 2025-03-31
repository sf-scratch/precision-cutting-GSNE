using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.sqlite
{
    [Table("auto_align_position")]
    class AutoAlignPositionModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("row_index")] 
        public int RowIndex { get; set; } = 1;
        [Column("actual_value")]
        public string ActualValue { get; set; }
        [Column("axis_position")]
        public string AxisPosition { get; set; }
    }
}
