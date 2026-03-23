using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using 精密切割系统.ViewModel;

namespace 精密切割系统.Entities
{
    [Table("bm_parameter_maintenance_table")]
    public class BMParameterMaintenanceEntity : BindableBase, IEntityWithId
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        private string _spindleRev;

        [Column("spindle_rev")]
        public string SpindleRev
        {
            get { return _spindleRev; }
            set { SetProperty(ref _spindleRev, value); }
        }

        private string _heightMeasureTimes;

        [Column("height_measure_times")]
        public string HeightMeasureTimes
        {
            get { return _heightMeasureTimes; }
            set { SetProperty(ref _heightMeasureTimes, value); }
        }

        private string _thetaMovementAngle;

        [Column("theta_movement_angle")]
        public string ThetaMovementAngle
        {
            get { return _thetaMovementAngle; }
            set { SetProperty(ref _thetaMovementAngle, value); }
        }

        private string _thetaStartingToMovePosition;

        [Column("theta_starting_to_move_position")]
        public string ThetaStartingToMovePosition
        {
            get { return _thetaStartingToMovePosition; }
            set { SetProperty(ref _thetaStartingToMovePosition, value); }
        }

        private string _thetaEndingToMovePosition;

        [Column("theta_ending_to_move_position")]
        public string ThetaEndingToMovePosition
        {
            get { return _thetaEndingToMovePosition; }
            set { SetProperty(ref _thetaEndingToMovePosition, value); }
        }

        private string _thetaCurrentLocation;

        [Column("theta_current_location")]
        public string ThetaCurrentLocation
        {
            get { return _thetaCurrentLocation; }
            set { SetProperty(ref _thetaCurrentLocation, value); }
        }

        private string _heightMeasurementMaxZ1Pos;

        [Column("height_measurement_max_z1_pos")]
        public string HeightMeasurementMaxZ1Pos
        {
            get { return _heightMeasurementMaxZ1Pos; }
            set { SetProperty(ref _heightMeasurementMaxZ1Pos, value); }
        }

        private bool _isAutomHeightMeasureBeforeCutting;

        [Column("is_autom_height_measure_before_cutting")]
        public bool IsAutomHeightMeasureBeforeCutting
        {
            get { return _isAutomHeightMeasureBeforeCutting; }
            set { SetProperty(ref _isAutomHeightMeasureBeforeCutting, value); }
        }

        private string _deficiencyWearAmount;

        [Column("deficiency_wear_amount")]
        public string DeficiencyWearAmount
        {
            get { return _deficiencyWearAmount; }
            set { SetProperty(ref _deficiencyWearAmount, value); }
        }

        private string _maximumWearAmount;

        [Column("maximum_wear_amount")]
        public string MaximumWearAmount
        {
            get { return _maximumWearAmount; }
            set { SetProperty(ref _maximumWearAmount, value); }
        }

        private string _reserveWearAmount;

        [Column("reserve_wear_amount")]
        public string ReserveWearAmount
        {
            get { return _reserveWearAmount; }
            set { SetProperty(ref _reserveWearAmount, value); }
        }

        private string _cuttingLifeLength;

        [Column("cutting_life_length")]
        public string CuttingLifeLength
        {
            get { return _cuttingLifeLength; }
            set { SetProperty(ref _cuttingLifeLength, value); }
        }

        private string _cuttingLifeCutNumber;

        [Column("cutting_life_cut_number")]
        public string CuttingLifeCutNumber
        {
            get { return _cuttingLifeCutNumber; }
            set { SetProperty(ref _cuttingLifeCutNumber, value); }
        }

        private string _measureHeightHighSpeedPreset;

        [Column("measure_height_high_speed_preset")]
        public string MeasureHeightHighSpeedPreset
        {
            get { return _measureHeightHighSpeedPreset; }
            set { SetProperty(ref _measureHeightHighSpeedPreset, value); }
        }

        private string _measureHeightSlowSpeedPreset;

        [Column("measure_height_slow_speed_preset")]
        public string MeasureHeightSlowSpeedPreset
        {
            get { return _measureHeightSlowSpeedPreset; }
            set { SetProperty(ref _measureHeightSlowSpeedPreset, value); }
        }

        private string _measureHeightSlowSpeedRangePreset;

        [Column("measure_height_slow_speed_range_preset")]
        public string MeasureHeightSlowSpeedRangePreset
        {
            get { return _measureHeightSlowSpeedRangePreset; }
            set { SetProperty(ref _measureHeightSlowSpeedRangePreset, value); }
        }

        private string _measureHeightHighSpeed;

        [Column("measure_height_high_speed")]
        public string MeasureHeightHighSpeed
        {
            get { return _measureHeightHighSpeed; }
            set { SetProperty(ref _measureHeightHighSpeed, value); }
        }

        private string _measureHeightSlowSpeed;

        [Column("measure_height_slow_speed")]
        public string MeasureHeightSlowSpeed
        {
            get { return _measureHeightSlowSpeed; }
            set { SetProperty(ref _measureHeightSlowSpeed, value); }
        }

        private string _measureHeightSlowSpeedRange;

        [Column("measure_height_slow_speed_range")]
        public string MeasureHeightSlowSpeedRange
        {
            get { return _measureHeightSlowSpeedRange; }
            set { SetProperty(ref _measureHeightSlowSpeedRange, value); }
        }

        private string _bladeBlowingTime;

        [Column("blade_blowing_time")]
        public string BladeBlowingTime
        {
            get { return _bladeBlowingTime; }
            set { SetProperty(ref _bladeBlowingTime, value); }
        }

        private string _ctBlowingTime;

        [Column("ct_blowing_time")]
        public string CtBlowingTime
        {
            get { return _ctBlowingTime; }
            set { SetProperty(ref _ctBlowingTime, value); }
        }

        private string _measureHeightAllowableDeviationValue;

        [Column("measure_height_allowable_deviation_value")]
        public string MeasureHeightAllowableDeviationValue
        {
            get { return _measureHeightAllowableDeviationValue; }
            set { SetProperty(ref _measureHeightAllowableDeviationValue, value); }
        }

        private string _measureHeightHistory;

        [Column("measure_height_history")]
        public string MeasureHeightHistory
        {
            get { return _measureHeightHistory; }
            set { SetProperty(ref _measureHeightHistory, value); }
        }
    }
}