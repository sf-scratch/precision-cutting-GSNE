using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.sqlite
{
    [Table("axis_idling_maintenance_conf")]
    internal class AxisIdlingMaintenanceConfModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("XAxis")]//X-Axis 是否选择  1选择 0未选择
        public string XAxis { get; set; } = "0";

        [Column("YAxis")]//Y-Axis 是否选择  1选择 0未选择
        public string YAxis { get; set; } = "0";

        [Column("ZAxis")]//Z-Axis 是否选择  1选择 0未选择
        public string ZAxis { get; set; } = "0";

    }
}
