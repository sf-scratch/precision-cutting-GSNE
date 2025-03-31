using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


//刀痕检查参数(3.1.8)
namespace 精密切割系统.database.db.modle
{

    [Table("table_kerf_check_data")]
    internal class KerfCheckDataModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        //检查频率（每）
        [Column("workpiece")]//片
        public string Workpiece { get; set; } = "0";

        [Column("lines")]//刀检查一次
        public string Lines { get; set; } = "0";

        //每枚切割片内检查频率（刀）
        [Column("first_ch1")]//首次检查
        public string FirstCh1 { get; set; } = "0";

        [Column("first_ch2")]//首次检查
        public string FirstCh2 { get; set; } = "0";

        [Column("first_ch3")]//首次检查
        public string FirstCh3 { get; set; } = "0";

        [Column("first_ch4")]//首次检查
        public string FirstCh4 { get; set; } = "0";

        [Column("every_ch1")]//下次检查
        public string EveryCh1 { get; set; } = "0";

        [Column("every_ch2")]//下次检查
        public string EveryCh2 { get; set; } = "0";

        [Column("every_ch3")]//下次检查
        public string EveryCh3 { get; set; } = "0";

        [Column("every_ch4")]//下次检查
        public string EveryCh4 { get; set; } = "0";


        [Column("check_mode")]//检查方式
        public string CheckMode { get; set; } = "KERF";

        [Column("window_width")]//检查范围
        public string WindowWidth { get; set; } = "0";

        [Column("sensitivity")]//检查敏感度
        public string Sensitivity { get; set; } = "0";

        [Column("retry_times")]//重测次数
        public string RetryTimes { get; set; } = "0";

        [Column("air_blow_timer")]//吹气时间
        public string AirBlowTimer { get; set; } = "0";

        [Column("check_object")]//检查对象
        public string CheckObject { get; set; } = "CENTER";

        [Column("auto_focus")]//自动对焦
        public bool AutoFocus { get; set; } = false;

        [Column("auto_light_retry")]//自动调光（重测时）
        public bool AutoLightRetry { get; set; } = false;


        //光源
        [Column("dir")]//直射
        public string Dir { get; set; } = "0";

        [Column("obl")]//斜射
        public string Obl { get; set; } = "0";

        //容许值
        [Column("kerf_score")]//刀痕点数
        public string KerfScore { get; set; } = "0";

        [Column("kerf_score_error_countermeas")]//处理对策
        public string KerfScoreErrorCountermeas { get; set; } = "CALL";

        [Column("off_center_call")]//偏离中心线（呼叫）
        public string OffCenterCall { get; set; } = "0";

        [Column("auto_adjust")]//（自动调整）
        public string AutoAdjust { get; set; } = "0";

        [Column("kerf_idth_vithout_chipping_max")]//（刀痕宽度（不含崩碎）上限）
        public string AutoAKerfIdthVithoutChippingMax { get; set; } = "0";

        [Column("kerf_idth_vithout_chipping_max_error_countermeas")]//处理对策
        public string AutoAKerfIdthVithoutChippingMaxErrorCountermeas { get; set; } = "CALL";

        [Column("kerf_idth_vithout_chipping_mix")]//（刀痕宽度（不含崩碎）下限）
        public string AutoAKerfIdthVithoutChippingMix { get; set; } = "0";

        [Column("kerf_idth_vithout_chipping_mix_error_countermeas")]//处理对策
        public string AutoAKerfIdthVithoutChippingMixErrorCountermeas { get; set; } = "CALL";

        [Column("include_chipping")]//（含崩碎）
        public string IncludeChipping { get; set; } = "0";

        [Column("include_chipping_error_countermeas")]//处理对策
        public string IncludeChippingErrorCountermeas { get; set; } = "CALL";


        [Column("center_chipping")]//（中心线 ~崩碎）
        public string CenterChipping { get; set; } = "0";

        [Column("center_chipping_error_countermeas")]//处理对策
        public string CenterChippingErrorCountermeas { get; set; } = "CALL";
        


        [Column("chipping_size")]//容许崩碎尺寸
        public string ChippingSize { get; set; } = "0";

        [Column("chipping_size_error_countermeas")]//处理对策
        public string ChippingSizeErrorCountermeas { get; set; } = "CALL";

        [Column("chipping_area")]//崩碎面积
        public string ChippingArea { get; set; } = "0";

        [Column("chipping_area_error_countermeas")]//处理对策
        public string ChippingAreaErrorCountermeas { get; set; } = "CALL";

        [Column("per_issible_y_from_target)")]//Y误差容许值（由目标决定的）
        public string PerIssibleYFromTarget { get; set; } = "0";

        //刀痕检查区域设定
        [Column("center)")]//中心
        public string Center { get; set; } = "0";

        [Column("outside)")]//外侧
        public string Outside { get; set; } = "0";
    }
}
