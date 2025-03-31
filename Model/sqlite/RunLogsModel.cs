using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.sqlite
{
    [Table("run_logs")]
    class RunLogsModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }
        // 记录时间
        [Column("record_time")] 
        public string RecordTime { get; set; }
        // 记录类型
        [Column("record_type")]
        public string RecordType { get; set; }
        // 记录内容
        [Column("record_content")]
        public string RecordContent { get; set; }

        [Ignore]
        public int Index { get; set; }
        [Ignore]
        public string EventData { get; set; }
    }
}
