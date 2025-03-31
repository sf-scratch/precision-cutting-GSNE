using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


//校准参数（3.1.3）
namespace 精密切割系统.database.db.modle
{
    [Table("table_calibration_parameter")]
    class CalibrationParameterModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("alignment_mode")]//校准方式
        public string AlignmentMode { get; set; } = "SPECIAL";

        [Column("alignment_pattern")]//校准模式
        public string AlignmentPattern { get; set; } = "A";

        [Column("time_out")]//时间有效
        public string TimeOut { get; set; } = "10";

        [Column("retry_count")]//重新校准次数
        public string RetryCount { get; set; } = "2";


        [Column("permissible_y_adjust")]//Y校准容许值
        public string PermissibleYAdjust { get; set; } = "0";

        [Column("permissible_0_adjust")]//校准容许值
        public string Permissible0Adjust { get; set; } = "0";

        [Column("adjust_stroke")]//0水平校整时的行程
        public string AdjustStroke { get; set; } = "0";

        //跳格检查
        [Column("ic_x_posintion")]//X方向
        public string IcXPosintion { get; set; } = "0";

        [Column("ic_y_posintion")]//Y方向
        public string IcYPosintion { get; set; } = "0";


        [Column("ic_permissible")]//容许值
        public string IcPermissible { get; set; } = "0";

        [Column("escape_data_auto_adjust")]//校准点自动跳随//0：未选中；1选中
        public bool EscapeDataAutoAdjust { get; set; } = false;


        [Column("focus_timing")]//对焦时机
        public string FocusTiming { get; set; } = "BY_POINT";

        [Column("focus_mode")]//对焦方式
        public string FocusMode { get; set; } = "WORK";

        [Column("focus_stroke")]//对焦行程
        public string FocusStroke { get; set; } = "0";

        [Column("focus_step")]//对焦步距
        public string FocusStep { get; set; } = "0";

        [Column("by_point_distance")]//ByPoint对焦的距离
        public string ByPointDistance { get; set; } = "0";

        [Column("focus_pos")]//焦点向上
        public string FocusPos { get; set; } = "0";

        //多模块数据显示

        //Q相似比值
        [Column("q_level_macro")]//粗调
        public string QLevelMacro { get; set; } = "0";

        [Column("q_level_ch_1")]//CH1
        public string QLevelCH1 { get; set; } = "0";

        [Column("q_level_ch_2")]//CH2
        public string QLevelCH2 { get; set; } = "0";

        [Column("q_level_ch_3")]//CH3
        public string QLevelCH3 { get; set; } = "0";

        [Column("q_level_ch_4")]//CH4
        public string QLevelCH4 { get; set; } = "0";

        //窗视尺寸X
        [Column("window_size_x_macro")]//粗调
        public string WindowSizeXMacro { get; set; } = "0";

        [Column("window_size_x_ch_1")]//CH1
        public string WindowSizeXCH1 { get; set; } = "0";

        [Column("window_size_x_ch_2")]//CH2
        public string WindowSizeXCH2 { get; set; } = "0";

        [Column("window_size_x_ch_3")]//CH3
        public string WindowSizeXCH3 { get; set; } = "0";

        [Column("window_size_x_ch_4")]//CH4
        public string WindowSizeXCH4 { get; set; } = "0";

        //窗视尺寸Y
        [Column("window_size_y_macro")]//粗调
        public string WindowSizeYMacro { get; set; } = "0";

        [Column("window_size_y_ch_1")]//CH1
        public string WindowSizeYCH1 { get; set; } = "0";

        [Column("window_size_y_ch_2")]//CH2
        public string WindowSizeYCH2 { get; set; } = "0";

        [Column("window_size_y_ch_3")]//CH3
        public string WindowSizeYCH3 { get; set; } = "0";

        [Column("window_size_y_ch_4")]//CH4
        public string WindowSizeYCH4 { get; set; } = "0";

        //光源（直射）
        [Column("light_level_dir_macro")]//粗调
        public string LightLevelDirMacro { get; set; } = "0";

        [Column("light_level_dir_ch_1")]//CH1
        public string LightLevelDirCH1 { get; set; } = "0";

        [Column("light_level_dir_ch_2")]//CH2
        public string LightLevelDirCH2 { get; set; } = "0";

        [Column("light_level_dir_ch_3")]//CH3
        public string LightLevelDirCH3 { get; set; } = "0";

        [Column("light_level_dir_ch_4")]//CH4
        public string LightLevelDirCH4 { get; set; } = "0";

        //光源（斜射）
        [Column("light_level_obl_macro")]//粗调
        public string LightLevelOblMacro { get; set; } = "0";

        [Column("light_level_obl_ch_1")]//CH1
        public string LightLevelOblCH1 { get; set; } = "0";

        [Column("light_level_obl_ch_2")]//CH2
        public string LightLevelOblCH2 { get; set; } = "0";

        [Column("light_level_obl_ch_3")]//CH3
        public string LightLevelOblCH3 { get; set; } = "0";

        [Column("light_level_obl_ch_4")]//CH4
        public string LightLevelOblCH4 { get; set; } = "0";

        //切割调整
        [Column("street_adjust_ch_1")]//CH1
        public string StreetAdjustCH1 { get; set; } = "0";

        [Column("street_adjust_ch_2")]//CH2
        public string StreetAdjustCH2 { get; set; } = "0";

        [Column("street_adjust_ch_3")]//CH3
        public string StreetAdjustCH3 { get; set; } = "0";

        [Column("street_adjust_ch_4")]//CH4
        public string StreetAdjustCH4 { get; set; } = "0";

        //基准线宽度
        [Column("hairline_width_ch_1")]//CH1
        public string HairlineWidthCH1 { get; set; } = "0";

        [Column("hairline_width_ch_2")]//CH2
        public string HairlineWidthCH2 { get; set; } = "0";

        [Column("hairline_width_ch_3")]//CH3
        public string HairlineWidthCH3 { get; set; } = "0";

        [Column("hairline_width_ch_4")]//CH4
        public string HairlineWidthCH4 { get; set; } = "0";
    }
}
