using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//OptionSetting（3.1.5）
namespace 精密切割系统.database.db.modle
{


    [Table("table_option_setting")]
    internal class OptionSettingModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("blade_width")]//Blade width
        public string BladeWidth { get; set; } = "0";

        [Column("alignment_t_p_y_ch1")]//Alignment teach pos. Y
        public string AlignmentTPYCH1 { get; set; } = "0";

        [Column("alignment_t_p_y_ch2")]//Alignment teach pos. Y
        public string AlignmentTPYCH2 { get; set; } = "0";

        [Column("alignment_t_p_0_ch1")]//Alignment teach pos. 0
        public string AlignmentTP0CH1 { get; set; } = "0";

        [Column("alignment_t_p_0_ch2")]//Alignment teach pos. 0
        public string AlignmentTP0CH2 { get; set; } = "0";

        [Column("alignment_t_p_sw_ch1")]//Alignment teach pos. Measurement slot width（mm）
        public string AlignmentTPSWCH1 { get; set; } = "0";

        [Column("alignment_t_p_sw_ch2")]//Alignment teach pos. Measurement slot width（mm）
        public string AlignmentTPSWCH2 { get; set; } = "0";

    }
}
