using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Entities;

//用户参数设置(7.4和7.4.2)
namespace 精密切割系统.database.db.modle
{
    [Table("table_user_define_data")]
    public class UserDefineDataModel : IEntityWithId
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("machine_id")]//MachineId
        public string MachineId { get; set; } = "Machine-ID";

        [Column("system_password")]//密码
        public string SystemPassword { get; set; } = "";

        [Column("system_password_time")]//录入的密码时间戳
        public long SystemPasswordTime { get; set; } = 0;

        [Column("after_edge_dress_pos")]//刀座修整时Y轴位置
        public string AfterEdgeDressPos { get; set; } = "0";

        [Column("blade_exchange_y_pos")]//换刀时Y轴位置
        public string BladeExchangeYPos { get; set; } = "0";

        [Column("hairline_adjust_limit")]//基准线调整极限
        public string HairlineAdjustLimit { get; set; } = "0";

        [Column("blow_time")]//吹气时间
        public string BlowTime { get; set; } = "0";

        [Column("baseline_width_ch1")]
        public string BaselineWidthCh1 { get; set; } = "0";

        [Column("baseline_width_ch2")]
        public string BaselineWidthCh2 { get; set; } = "0";

        [Column("baseline_width_ch3")]
        public string BaselineWidthCh3 { get; set; } = "0";

        [Column("baseline_width_ch4")]
        public string BaselineWidthCh4 { get; set; } = "0";

        [Column("edge_width_ch1")]
        public string EdgeWidthCh1 { get; set; } = "0";

        [Column("edge_width_ch2")]
        public string EdgeWidthCh2 { get; set; } = "0";

        [Column("edge_width_ch3")]
        public string EdgeWidthCh3 { get; set; } = "0";

        [Column("edge_width_ch4")]
        public string EdgeWidthCh4 { get; set; } = "0";

        [Column("light_source_brightness_ch1")]
        public string LightSourceBrightnessCh1 { get; set; } = "0";

        [Column("light_source_brightness_ch2")]
        public string LightSourceBrightnessCh2 { get; set; } = "0";

        [Column("light_source_brightness_ch3")]
        public string LightSourceBrightnessCh3 { get; set; } = "0";

        [Column("light_source_brightness_ch4")]
        public string LightSourceBrightnessCh4 { get; set; } = "0";

        [Column("has_edge_line")]
        public bool HasEdgeLine { get; set; } = false;

        [Column("warm_up_time")]//暖机时间
        public string WarmUpTime { get; set; }

        [Column("warm_up_start_x")]//暖机x开始
        public string WarmUpStartX { get; set; }

        [Column("warm_up_end_x")]//暖机x结束
        public string WarmUpEndX { get; set; }

        [Column("warm_up_start_y")]//暖机y开始
        public string WarmUpStartY { get; set; }

        [Column("warm_up_end_y")]//暖机y结束
        public string WarmUpEndY { get; set; }

        [Column("work_vacuum_check_time")]//工作真空检查时间
        public string WorkVacuumCheckTime { get; set; } = "0";

        [Column("wait_time_until_energy_saving_mode")]//等待节能模式的时间
        public string WaitTimeUntilEnergySavingMode { get; set; } = "0";

        [Column("language")]//语言
        public string Language { get; set; } = "Chinese";

        [Column("device_change_cut_speed")]//型号改变后速度清零
        public string DeviceChangeCutSpeed { get; set; } = "clear";

        [Column("speed_change")]//速度变更
        public string SpeedChange { get; set; } = "YES";

        [Column("height_change")]//高度补偿
        public string HeightChange { get; set; } = "YES";

        [Column("z_axis_cut_model")]// Z轴切割模式 高度 深度
        public string ZAxisCutModel { get; set; } = "高度";

        //校准时是否进行过切割的检查
        [Column("cut_work_check_when_alignment")]
        public string CutWorkCheckWhenAlignment { get; set; } = "YES";

        [Column("continue_after_blade_user_limit_error")]
        public string ContinueAfterBladeUserLimitError { get; set; } = "YES";

        [Column("processing_after_blade_user_limit_error")]
        public string ProcessingAfterBladeUserLimitError { get; set; } = "WORK";

        [Column("bbd_timing")]//BBD动作时刻
        public string BBDTiming { get; set; } = "Z-EM";

        //BBD被检出时主轴是否停止
        [Column("stop_spindle_by_bbd")]
        public bool StopSpindleByBbd { get; set; } = false;

        [Column("hairline_adjustment")]
        public string HairlineAdjustment { get; set; } = "AUTO";

        [Column("lighting_adjustment")]
        public string LightingAdjustment { get; set; } = "AUTO";

        [Column("blade_replacement_check")]
        public string BladeReplacementCheck { get; set; } = "YES";

        [Column("z_processing_data_selection")]
        public string ZProcessingDataSelection { get; set; } = "HEIGHT";

        [Column("align_selection_when_semi_auto_cutting")]
        public string AlignSelectionWhenSemiAutoCutting { get; set; } = "YES";

        [Column("spindle_center_position_offset")]
        public string SpindleCenterPositionOffset { get; set; } = "0";

        [Column("water_pump_on_timer")]
        public string WaterPumpOnTimer { get; set; } = "0";

        [Column("atomizing_nozzle_position_x")]
        public string AtomizingNozzlePositionX { get; set; } = "0";

        [Column("atomizing_nozzle_position_y")]
        public string AtomizingNozzlePositionY { get; set; } = "0";
    }
}