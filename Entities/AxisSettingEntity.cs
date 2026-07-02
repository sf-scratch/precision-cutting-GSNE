using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Entities
{
    [Table("axis_setting_table")]
    internal class AxisSettingEntity : IEntityWithId
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("positive_soft_limit")]
        public string PositiveSoftLimit { get; set; }

        [Column("negative_soft_limit")]
        public string NegativeSoftLimit { get; set; }
    }
}