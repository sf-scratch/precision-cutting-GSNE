using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Entities
{
    [Table("blade_info_table")]
    internal class BladeInfoEntity : IEntityWithId
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("tool_holder_outer_diameter")]
        public string ToolHolderOuterDiameter { get; set; }
    }
}