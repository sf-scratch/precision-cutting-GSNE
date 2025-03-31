using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.sqlite
{
    [Table("bunkering_record_model")]
    internal class BunkeringRecordModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("create_time")]
        public string CreateTime { get; set; }

        [Ignore]
        public int BunkeringIndex { get; set; }

    }
}
