using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using 精密切割系统.Driver;


//OptionSetting（3.1.4）
namespace 精密切割系统.database.db.modle
{
    [Table("table_function_selection")]
    internal class FunctionSelectionModel
    {

        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("air_curtain_sweep_speed")]//工作盘通过风帘的速度（mm/s）
        public string AirCurtainSweepSpeed { get; set; } = "0";

        [Column("spec_length")]//每切割长度（m）
        public string SpecLength { get; set; } = "0";

        [Column("down_length")]//补偿量（mm）
        public string DownLength { get; set; } = "0";

        [Column("auto_setup")]//AutoSetup
        public string AutoSetup { get; set; } = "During cutting";

        [Column("interval_length")]//实行间隔（距离）
        public string IntervalLength { get; set; } = "0";

        [Column("interval_lines")]//实行间隔（刀数）
        public string IntervalLines { get; set; } = "0";

        [Column("chopper_function")]//ChopperFunction
        public bool ChopperFunction { get; set; } = false;

        [Column("depth_steps_function")]//DepthStepsFunction
        public bool DepthStepsFunction { get; set; } = true;

        [Column("loop_function")]//LoopFunction
        public bool LoopFunction { get; set; } = true;

        [Column("x_axis_offset_function")]//XAxisOffsetFunction
        public bool XAxisOffsetFunction { get; set; } = false;

        [Column("theta_axis_offset_funct")]//ThetaAxisOffsetFunct
        public bool ThetaAxisOffsetFunct { get; set; } = false;

        [Column("one_channel_display")]//OneChannelDisplay
        public bool OneChannelDisplay { get; set; } = false;


        [Column("chopper_cut_x_axis_standard_position")]//ChopperCutXAxisStandardPosition
        public string ChopperCutXAxisStandardPosition { get; set; } = "ALIGNMENT";


        [Column("mixed_cleaning_fluid_nozzle_for_cutting")]//MixedCleaningFluidNozzleForCutting
        public bool MixedCleaningFluidNozzleForCutting { get; set; } = false;

        
    }
}
