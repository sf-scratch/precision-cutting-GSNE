using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using 精密切割系统.Entities;

namespace 精密切割系统.database.db.modle
{
    [Table("operation_parameters")]
    public class OperationParametersModel : IEntityWithId
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("op_x_scan_speed")]//x轴扫描速度（mm/s）
        public string XScanSpeed { get; set; }

        [Column("op_x_scan_distance")]//x轴扫描距离（mm）
        public string XSscanDistance { get; set; }

        [Column("op_y_scan_speed")]//y轴扫描速度（mm/s）
        public string YScanSpeed { get; set; }

        [Column("op_y_scan_distance")]//y轴扫描距离（mm）
        public string YSscanDistance { get; set; }

        [Column("op_z_scan_speed")]//z轴扫描速度（mm/s）
        public string ZScanSpeed { get; set; }

        [Column("op_z_scan_distance")]//z轴扫描距离（mm）
        public string ZSscanDistance { get; set; }

        [Column("op_z2_scan_speed")]//z2轴扫描速度（mm/s）
        public string Z2ScanSpeed { get; set; }

        [Column("op_z2_scan_distance")]//z2轴扫描距离（mm）
        public string Z2SscanDistance { get; set; }

        [Column("op_r_scan_speed")]//θ轴扫描速度（°/s）
        public string RScanSpeed { get; set; }

        [Column("op_r_scan_distance")]//θ轴扫描距离（mm）
        public string RSscanDistance { get; set; }

        [Column("op_move_low_time")]//低速（s）
        public string MoveLowTime { get; set; }

        [Column("op_move_high_time")]//中速（s）
        public string MoveHighTime { get; set; }

        [Column("op_x_screen_index")]//x轴屏幕移动量（mm）
        public string XScreenIndex { get; set; }

        [Column("op_y_screen_index")]//y轴屏幕移动量（mm）
        public string YScreenIndex { get; set; }

        [Column("op_escape_rate")]//附加运动量（mm）
        public string EscapeRate { get; set; }

        [Column("op_extra_escape_rate")]//附加之上外加运动量（mm）
        public string ExtraEscapeRate { get; set; }

        [Column("op_m_stop_operation_electrical")]//主轴停止电流（A）
        public string MStopElectrical { get; set; }

        [Column("op_m_stop_operation_time")]//主轴停止时间（s）
        public string MStopTime { get; set; }

        [Column("op_z_stop_operation_electrical")]//z轴停止电流（A）
        public string ZStopElectrical { get; set; }

        [Column("op_z_stop_operation_time")]//z轴停止时间（s）
        public string ZStopTime { get; set; }

        [Column("op_z_stop_after_seq")]//z轴紧急停止后下一刀处理方式
        public string ZStopAfterSeq { get; set; } = "Next Line";

        [Column("op_x_start_clearance")]//x轴开始预留（mm）
        public string XStartClearance { get; set; }

        [Column("op_x_end_clearance")]//x轴结束预留（mm）
        public string XEndClearance { get; set; }

        [Column("op_y_clearance")]//y轴预留（mm）
        public string YClearance { get; set; }

        [Column("op_auto_focus_check_limit")]//自动对接检测范围（mm）
        public string AutoFocusCheckLimit { get; set; }

        [Column("op_air_curtain_stroke")]//通过空气屏的有效行程（mm），气帘移动距离
        public string AirCurtainStroke { get; set; }

        [Column("op_work_vaccum_lower_limit")]//工作真空下限（Kpa）
        public string VaccumWorkLowerLimit { get; set; }

        [Column("op_jig_vaccum_lower_limit_other")]//真空其他下限（Kpa），工作台本身的真空
        public string VaccumPumpLowerLimitOther { get; set; }

        [Column("op_during_cutting")]//在切割期间真空限制（Kpa）
        public string LimitDuringCutting { get; set; }

        [Column("op_vaccum_pump_lower_limit")]//真空泵下限（Kpa），低于多少会报警
        public string VaccumPumpLowerLimit { get; set; }

        // 面板寸动距离
        [Column("x_Panel_Jog_Distance")]//X轴屏幕移动速度
        public string xPanelJogDistance { get; set; }

        [Column("y_Panel_Jog_Distance")]//Y轴屏幕移动速度
        public string yPanelJogDistance { get; set; }

        [Column("z_Panel_Jog_Distance")]//Z轴屏幕移动速度
        public string zPanelJogDistance { get; set; }

        [Column("z_axis_comp_num")]// Z轴补偿-前几刀
        public int zAxisCompNum { get; set; } = 0;

        [Column("cut_x_axis_back_speed")]// 切割回程速度
        public int cutXAxisBackSpeed { get; set; } = 0;

        [Column("z_axis_comp_value")]//Z轴补偿-补偿量
        public string zAxisCompValue { get; set; } = "0";

        [Column("origin_compensation_x")]
        public string OriginCompensationX { get; set; } = "0";

        [Column("origin_compensation_y")]
        public string OriginCompensationY { get; set; } = "0";

        [Column("origin_compensation_z1")]
        public string OriginCompensationZ1 { get; set; } = "0";

        [Column("origin_compensation_z2")]
        public string OriginCompensationZ2 { get; set; } = "0";

        [Column("origin_compensation_theta")]
        public string OriginCompensationTheta { get; set; } = "0";

        [Column("is_open_cut_water_after_cutting_completed")]
        public bool IsAutoShutOffWaterWhenCuttingCompleted { get; set; }

        [Column("is_auto_shut_off_water_when_close_vacuum")]
        public bool IsAutoShutOffWaterWhenCloseVacuum { get; set; }

        [Column("is_auto_shut_off_water_when_enter_calibration")]
        public bool IsAutoShutOffWaterWhenEnterCalibration { get; set; }

        [Column("is_manually_turn_off_water")]
        public bool IsManuallyTurnOffWater { get; set; }

        [Column("is_exit_cut_clear_manual_compensation")]
        public bool IsExitCutClearManualCompensation { get; set; }

        [Column("is_update_param_clear_manual_compensation")]
        public bool IsUpdateParamClearManualCompensation { get; set; }

        [Column("is_start_pre_cutting_after_change_blade")]
        public bool IsStartPreCuttingAfterChangeBlade { get; set; }
    }
}