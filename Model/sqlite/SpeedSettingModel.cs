using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.database.db.modle
{
    [Table("speed_setting")]
    internal class SpeedSettingModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("xDefaultSpeed")]//x轴默认速度
        public string XDefaultSpeed { get; set; } = "10";

        [Column("yDefaultSpeed")]//y轴默认速度
        public string YDefaultSpeed { get; set; } = "10";

        [Column("z2DefaultSpeed")]//z2相机轴默认速度
        public string Z2DefaultSpeed { get; set; } = "2";

        [Column("thetaDefaultSpeed")]//thetaθ轴默认速度
        public string ThetaDefaultSpeed { get; set; } = "20";

        [Column("xScanSpeed")]//X轴扫描速度（mm/s）
        public string XScanSpeed { get; set; } = "10";

        [Column("xScanDistance")]//X轴扫描距离（mm）
        public string XScanDistance { get; set; } = "0.001";

        [Column("yScanSpeed")]//Y轴扫描速度（mm/s）
        public string YScanSpeed { get; set; } = "5";

        [Column("yScanDistance")]//Y轴扫描距离（mm）
        public string YScanDistance { get; set; } = "0.001";

        [Column("z1ScanSpeed")]//Z1轴扫描速度（mm/s）
        public string Z1ScanSpeed { get; set; } = "5";

        [Column("z1ScanDistance")]//Z1轴扫描距离（mm）
        public string Z1ScanDistance { get; set; } = "0.001";

        [Column("z2ScanSpeed")]//Z2轴扫描速度（mm/s）
        public string Z2ScanSpeed { get; set; } = "5";

        [Column("z2ScanDistance")]//Z2轴扫描距离（mm）
        public string Z2ScanDistance { get; set; } = "0.001";

        [Column("thetaScanSpeed")]//θ轴扫描速度（°/s）
        public string ThetaScanSpeed { get; set; } = "5";

        [Column("thetaScanDistance")]//θ轴扫描距离
        public string ThetaScanDistance { get; set; } = "10";

        [Column("moveLowTime")]//低速操作时间
        public string MoveLowTime { get; set; }

        [Column("moveHighTime")]//高速操作时间
        public string MoveHighTime { get; set; }

        [Column("thetaScreenIndex")]//θ轴屏幕移动量
        public string ThetaScreenIndex { get; set; } = "90";

        [Column("xScreenIndex")]//X轴屏幕移动量
        public string XScreenIndex { get; set; } = "0.127";

        [Column("yScreenIndex")]//Y轴屏幕移动量
        public string YScreenIndex { get; set; } = "0.25";

        [Column("thetaScreenSpeed")]//θ轴屏幕移动速度
        public string ThetaScreenSpeed { get; set; } = "30";


        [Column("xScreenSpeed")]//X轴屏幕移动速度
        public string XScreenSpeed { get; set; } = "30";

        [Column("yScreenSpeed")]//Y轴屏幕移动速度
        public string YScreenSpeed { get; set; } = "5";

    }
  }
