using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.database.db.modle
{
    [Table("initial_position")]
    internal class InitialPositionModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }


        // 测高位置
        [Column("bladeSetupInitX")]//x轴初始位置
        public string BladeSetupInitX { get; set; } = "153.000";

        [Column("bladeSetupInitY")]//x轴初始位置
        public string BladeSetupInitY { get; set; } = "120.000";

        [Column("bladeSetupInitZ1")]//z1轴初始位置
        public string BladeSetupInitZ1 { get; set; } = "30.000";

        [Column("bladeSetupInitZ2")]//z2轴初始位置
        public string BladeSetupInitZ2 { get; set; } = "20.000";


        // 非接触测高位置
        [Column("noContactBladeSetupInitX")]//x轴初始位置
        public string NoContactBladeSetupInitX { get; set; } = "153.000";

        [Column("noContactBladeSetupInitY")]//x轴初始位置
        public string NoContactBladeSetupInitY { get; set; } = "120.000";

        [Column("noContactBladeSetupInitZ1")]//z1轴初始位置
        public string NoContactBladeSetupInitZ1 { get; set; } = "30.000";

        [Column("noContactBladeSetupInitZ2")]//z2轴初始位置
        public string NoContactBladeSetupInitZ2 { get; set; } = "20.000";

        // 电火花修刀位置

        // 校准位置
        [Column("alignInitX")]//x轴初始位置
        public string AlignInitX { get; set; } = "17.000";

        [Column("alignInitY")]//x轴初始位置
        public string AlignInitY { get; set; } = "127.820";

        [Column("alignInitZ1")]//z1轴初始位置
        public string AlignInitZ1 { get; set; } = "20.000";

        [Column("alignInitZ2")]//z2轴初始位置
        public string AlignInitZ2 { get; set; } = "20.000";


        // 切割位置
        [Column("cutInitX")]//x轴初始位置
        public string CutInitX { get; set; } = "21.000";

        [Column("cutInitY")]//x轴初始位置
        public string CutInitY { get; set; } = "60.000";

        [Column("cutInitZ1")]//z1轴初始位置
        public string CutInitZ1 { get; set; } = "20.000";

        [Column("cutInitZ2")]//z2轴初始位置
        public string CutInitZ2 { get; set; } = "25.000";


        // 刀片更换位置
        [Column("cutReplaceInitX")]//x轴初始位置
        public string CutReplaceInitX { get; set; } = "-40.000";

        [Column("cutReplaceInitY")]//x轴初始位置
        public string CutReplaceInitY { get; set; } = "80.000";

        [Column("cutReplaceInitZ1")]//z1轴初始位置
        public string CutReplaceInitZ1 { get; set; } = "0.000";

        [Column("cutReplaceInitZ2")]//z2轴初始位置
        public string CutReplaceInitZ2 { get; set; } = "20.000";
    }
}
