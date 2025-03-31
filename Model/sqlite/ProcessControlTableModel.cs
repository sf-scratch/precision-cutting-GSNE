using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//程序控制(3.1.6)
namespace 精密切割系统.database.db.modle
{
    [Table("table_process_control")]
    internal class ProcessControlTableModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("process_id")]//程序名称
        public string ProcessId { get; set; } = "ALI";

        [Column("parameter")]//参数
        public string Parameter { get; set; } = "0";

    }
}
