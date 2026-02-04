using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Entities
{
    [Table("camera_table")]
    internal class CameraEntity : IEntityWithId
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("exposure_time")]
        public string ExposureTime { get; set; }

        [Column("light_intensity")]
        public string LightIntensity { get; set; }
    }
}