using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Entities
{
    [Table("temperature_log_table")]
    internal class TemperatureLogEntity : BindableBase, IEntityWithId
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("sensor_id")]
        public long SensorId { get; set; }

        [Column("temperature")]
        public float Temperature { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}