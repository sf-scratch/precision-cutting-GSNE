using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace 精密切割系统.database.db.modle
{
    [Table("replace_blade")]
    internal class ReplaceBladeModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("unit")]//单位 mm/inch
        public string BladeUnit { get; set; } = "mm";

        [Column("lot_id")]//刀片批号ID
        public string BladeLotID { get; set; } = "0";

        [Column("spec")]//更换刀片的品种名称
        public string SpecName { get; set; } = "0";

        [Column("new_or_old")]//新/旧
        public string NewOrOld { get; set; } = "New";

        [Column("blade_outside")]//刀片外径(mm)
        public string BladeOutside { get; set; } = "0";

        [Column("blade_thickness")]//刀片厚度(mm)
        public string BladeThickness { get; set; } = "0";

        [Column("blade_life")]//刀片使用寿命
        public string BladeLife { get; set; } = "0.0";

        [Column("blade_life_m")]//刀片使用寿命
        public string BladeLifeM { get; set; } = "0.0";

        [Column("replace_reason")]//刀片交换理由
        public string ReplaceReason { get; set; } = "New blade";

        [Column("blade_type")]//刀片类型 Hub、Flange
        public string BladeType { get; set; } = "Hub";

        [Column("hard_blade_length")]//硬刀刀刃长度(mm)
        public string HardBladeLength { get; set; } = "0";

        [Column("soft_blade_holder")]//软刀刀架外径(mm)
        public string SoftBladeHolder { get; set; } = "0";
    }
}
