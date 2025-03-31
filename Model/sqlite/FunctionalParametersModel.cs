using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using static Emgu.CV.OCR.Tesseract;

namespace 精密切割系统.database.db.modle
{
    [Table("functional_parameters")]
    public class FunctionalParametersModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("alignment")]//自动校准
        public bool Alignment { get; set; } = false;

        [Column("cut")]//切割
        public bool Cut { get; set; }

        [Column("spindle")]//旋转轴
        public bool Spindle { get; set; }

        [Column("non_contact_setup")]//非接触测高
        public bool NonContactSetup { get; set; }

        [Column("bbd")]//刀片破损检测
        public bool Bbd { get; set; }

        [Column("after_kerf_check")]//刀痕检测后再确认
        public bool AfterKerfCheck { get; set; }

        [Column("axis_maintenance")]//轴润滑维护
        public bool AxisMaintenance { get; set; }

        [Column("keep_work_wet")]//防止工件乾燥动作
        public bool KeepWorkWet { get; set; }

        [Column("tape_cut_hairline_adjust")]//膠片切割基准率校准
        public bool TapeCutHairlineAdjust { get; set; }

        //ConnectedCt
        [Column("be_connect_with_work")]//膠片切割基准率校准
        public bool ConnectedWithWork { get; set; }

        [Column("spindle_idling_time")]//回转轴空转时间
        public string SpindleIdlingTime { get; set; }

        [Column("default_unit")]//内定单位
        public string DefaultUnit { get; set; }

        [Column("layout_of_keyboard")]//键盘配置
        public string LayoutOfKeyboard { get; set; }
    }
}
