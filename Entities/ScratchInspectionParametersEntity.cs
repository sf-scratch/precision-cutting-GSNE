using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Entities
{
    [Table("automatic_compensation_cut_height_table")]
    internal class ScratchInspectionParametersEntity : BindableBase, IEntityWithId
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("first_check_ch1")]
        public string FirstCheckCh1 { get; set; }

        [Column("first_check_ch2")]
        public string FirstCheckCh2 { get; set; }

        [Column("first_check_ch3")]
        public string FirstCheckCh3 { get; set; }

        [Column("first_check_ch4")]
        public string FirstCheckCh4 { get; set; }

        [Column("next_check_ch1")]
        public string NextCheckCh1 { get; set; }

        [Column("next_check_ch2")]
        public string NextCheckCh2 { get; set; }

        [Column("next_check_ch3")]
        public string NextCheckCh3 { get; set; }

        [Column("next_check_ch4")]
        public string NextCheckCh4 { get; set; }
    }
}