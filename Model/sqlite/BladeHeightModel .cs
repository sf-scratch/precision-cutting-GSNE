using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace 精密切割系统.database.db.modle
{
    [Table("blade_height")]
    public class BladeHeightModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("unit")]//单位 mm/inch
        public string Unit { get; set; }// = "mm";

        [Column("setup_default")]   //测高方式
        public string SetupDefault { get; set; }// = "CONTACT";

        [Column("chuck_table_size")]//工作盘尺寸
        public string ChuckTableSize { get; set; }// = "3inch";

        [Column("blade_height")] // 刀片高度
        public string BladeHeight {  get; set; }

        [Column("call_operator_when_auto_setup")] //自动测高
        public string CallOperatorWhenAutoSetup { get; set; }// = "AUTO";

        [Column("precut_after_non_contact_setup")]//非接触测高后预切割
        public string PrecutAfterNonContactSetup { get; set; }// = "YES";

        [Column("spindle_rev")]//主轴转速（转/min）
        public string SpindleRev { get; set; }

        [Column("setup_z_axis_max_distance")]// 刀片测高最大距离
        public string SetupZAxisMaxDistance { get; set; }

        [Column("retry")]//自动测高的重复次数（time/s）
        public string Retry { get; set; }

        [Column("excessive_wear")]//测高刀片消耗不足消耗量（mm）
        public string ExcessiveWear { get; set; }

        [Column("insufficient_wear")]//测高刀片消耗不足消耗量（mm）
        public string InsufficientWear { get; set; }

        [Column("ct_setup_check")]//C/T测高检测回数
        public string CtSetupCheck { get; set; }

        [Column("permissible_amount_non_contact")]// 测高2次的容许值-非解除测高（mm）
        public string PermissibleAmountNonContact { get; set; }

        [Column("permissible_amount_ct")]//测高2次的容许值-C/T（mm）
        public string PermissibleAmountCt { get; set; }

        [Column("blade_blow_time_non_contact")]//刀片吹气时间-非解除测高（s）
        public string BladeBlowTimeNonContact { get; set; }

        [Column("blade_blow_time_ct")]// 刀片吹气时间-C/T（s）
        public string BladeBlowTimeCt { get; set; }

        [Column("clearance_between_flange_work_surface")]//刀刃损耗的安全量（mm）
        public string ClearanceBetweenFlangeWorkSurface { get; set; }

        [Column("waiting_time_after_non_setup_air_blow")]//非接触测高时吹风后的等待时间（s）
        public string WaitingTimeAfterNonSetupAirBlow { get; set; }

        [Column("chuck_table_shape")]//chuck_table_shape
        public string ChuckTableShape { get; set; }// = "ROUND";

        [Column("table_type")]//工作盘类型
        public string TableType { get; set; }// = "POROUS";

        [Column("blow_time_at_ncs_block")]// 非接触测高时的吹风时间（s）
        public string BlowTimeNcsBlock { get; set; }

        [Column("setup_high_speed_non_contact")]//测高高速移动速度-非接触（mm/s）
        public string HighSpeedNonContact { get; set; }

        [Column("setup_high_speed_ct")]//测高高速移动速度-C/T（mm/s）
        public string HighSpeedCt { get; set; }

        [Column("setup_low_speed_non_contact")]//测高低速移动速度-非接触（mm/s）
        public string LowSpeedNonContact { get; set; }

        [Column("setup_low_speed_ct")]//测高低速移动速度-C/T（mm/s）
        public string LowSpeedCt { get; set; }

        [Column("setup_low_speed_stroke_non_contact")]//测高低速移动范围-非接触（mm）
        public string LowSpeedStrokeNonContact { get; set; }

        [Column("setup_low_speed_stroke_ct")]//测高低速移动范围-C/T（mm）
        public string LowSpeedStrokeCt { get; set; }

        [Column("theta_rotation_for_contact_setup")]//测高时θ轴移动角度
        public string ThetaRotationForContactSetup { get; set; }

        [Column("theta_rotation_for_start_position")]//测高时θ轴开始移动位置
        public string ThetaRotationForStartPosition { get; set; }

        [Column("theta_rotation_for_end_position")]//测高时θ轴移动结束位置
        public string ThetaRotationForEndPosition { get; set; }

        [Column("theta_rotation_for_now_position")]//测高时θ轴现在的位置
        public string ThetaRotationForNowPosition { get; set; }

        [Column("chuck_table_rotation_completed")]//现在测高θ轴的位置返回次数
        public string ChuckTableRotationCompleted { get; set; }

        [Timestamp]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Timestamp]
        public DateTime UpdatedAt { get; set; }

        public void Update()
        {
            // 更新实体属性
            UpdatedAt = DateTime.Now; // 更新更新时间戳
        }

    }
}
