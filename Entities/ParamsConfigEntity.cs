using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Entities
{
    [Table("params_config_table")]
    public class ParamsConfigEntity
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("sharpen_params_id")]
        public long SharpenParamsId { get; set; }

        [Column("cut_params_id")]
        public long CutParamsId { get; set; }

        [Column("describe")]
        public string Describe { get; set; }
    }
}
