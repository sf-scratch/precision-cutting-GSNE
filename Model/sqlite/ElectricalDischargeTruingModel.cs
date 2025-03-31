using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace 精密切割系统.database.db.modle
{
    [Table("electrical_discharge_truing")]
    public class ElectricalDischargeTruingModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        // X轴初始位置
        [Column("x_init_location")]
        public string XInitLocation { get; set; } = "10";

        // Y轴刀片前端位置
        [Column("y_blade_front_location")]
        public string YBladeFrontLocation { get; set; } = "31.1078";

        // Y轴刀片后端位置
        [Column("y_blade_back_location")]
        public string YBladeBackLocation { get; set; } = "29.8078";

        // Z轴设定位置
        [Column("z_set_position")]
        public string ZSetPosition { get; set; } = "30.538";

        // 刀片角度
        [Column("blade_angle")]
        public string BladeAngle { get; set; } = "60";

        // X0轴基准位置
        [Column("x0_base_position")]
        public string X0BasePosition { get; set; } = "17";

        // Y0轴基准位置
        [Column("y0_base_position")]
        public string Y0BasePosition { get; set; } = "7";

        // Z0轴基准位置
        [Column("z0_base_position")]
        public string Z0BasePosition { get; set; } = "32";

        // Z轴切割量
        [Column("z_cutting_amount")]
        public string ZCuttingAmount { get; set; } = "0.0005";

        // 重复次数
        [Column("repeat_count")]
        public int RepeatCount { get; set; } = 2;

        // 电极极性设置
        [Column("electrode_polarity_setting")]
        public string ElectrodePolaritySetting { get; set; } = "1.0000";

        // 刀片修正速度
        [Column("blade_correction_speed")]
        public string BladeCorrectionSpeed { get; set; } = "0.3333";

        // 主轴速度
        [Column("spindle_speed")]
        public string SpindleSpeed { get; set; } = "3000";

        // 刀片厚度
        [Column("blade_thickness")]
        public string BladeThickness { get; set; } = "0.025";

        // 电极厚度
        [Column("electrode_thickness")]
        public string ElectrodeThickness { get; set; } = "0.025";

        // Y轴偏移量
        [Column("y_offset_amount")]
        public string YOffsetAmount { get; set; } = "0.3";

        // Y轴浮动量
        [Column("y_floating_amount")]
        public string YFloatingAmount { get; set; } = "0.07";

        // Z轴极限位置
        [Column("z_limit_position")]
        public string ZLimitPosition { get; set; } = "50";

        // 电极角度
        [Column("electrode_angle")]
        public string ElectrodeAngle { get; set; } = "60";

        // 当前修刀数量
        [Column("current_repair_num")]
        public int CurrentRepairNum { get; set; } = 0;

        // 当前修刀数量
        [Column("all_dressers_num")]
        public int AllDressersNum { get; set; } = 0;
        // 当前修刀数量
        [Column("clear_dressers_num")]
        public int ClearDressersNum { get; set; } = 0;

    }
}
