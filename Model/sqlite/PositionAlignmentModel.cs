using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.database.db.modle
{
    [Table("position_alignment")]
    internal class PositionAlignmentModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("thetaCenterLocationX")]//theta轴切割中心点位置 X轴
        public string ThetaCenterLocationX { get; set; } = "184.289";

        [Column("thetaCenterLocationY")]//theta轴切割中心点位置 - Y轴
        public string ThetaCenterLocationY { get; set; } = "62.386";

        [Column("thetaCameraLocationX")]//theta轴相机中心点位置 X轴
        public string ThetaCameraLocationX { get; set; } = "28.89837";

        [Column("thetaCameraLocationY")]//theta轴相机中心点位置 - Y轴
        public string ThetaCameraLocationY { get; set; } = "53.73523";

        [Column("cameraToCutXOffset")]//X轴切割位置和相机位置的偏移量
        public string CameraToCutXOffset { get; set; } = "155";

        [Column("cameraToCutYOffset")]//相机焦点和切割位置的偏移量
        public string CameraToCutYOffset { get; set; } = "10";

        [Column("cutZ1MaxLocation")]//切割Z1轴最大安全距离
        public string CutZ1MaxLocation { get; set; } = "40";

        [Column("cameraOffsetX")]//相机和切割点的X轴的偏移量
        public string CameraOffsetX { get; set; } = "10";

        [Column("cameraOffsetY")]//相机和切割点的Y轴的偏移量
        public string CameraOffsetY { get; set; } = "8.163";

        [Column("initPosition")]//聚焦初始位置
        public string InitPosition { get; set; } = "36";

        [Column("work_disc_focus_position")]// 相机焦点位置
        public float WorkDiscFocusPosition { get; set; } = 37.36f;

        [Column("light_intensity_channel")]// 光源通道
        public int LightIntensityChannel { get; set; } = 1;

        [Column("low_light_intensity_channel")]// 低倍光源通道
        public int LowLightIntensityChannel { get; set; } = 1;

        [Column("ring_light_intensity_channel")]// 环光光源通道
        public int RingLightIntensityChannel { get; set; } = 1;

        [Column("focusRatio")]//聚焦比率
        public string FocusRatio { get; set; } = "12";

        [Column("multipleNum")]//高速的倍率
        public string MultipleNum { get; set; } = "0.01";

        [Column("high_mag_to_low_mag_camera_x_offset")]//高倍和低倍相机的X轴偏移量
        public string HighMagToLowMagCameraXOffset { get; set; } = "0";

        [Column("high_mag_to_low_mag_camera_y_offset")]//高倍和低倍相机的Y轴偏移量
        public string HighMagToLowMagCameraYOffset { get; set; } = "0";

    }
}
