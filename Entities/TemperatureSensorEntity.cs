using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Entities
{
    [Table("temperature_sensor_table")]
    internal class TemperatureSensorEntity : BindableBase, IEntityWithId
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("sensor_name")]
        public string SensorName { get; set; }

        [Column("location")]
        public string Location { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("front_color")]
        public string FrontColor { get; set; }
    }
}