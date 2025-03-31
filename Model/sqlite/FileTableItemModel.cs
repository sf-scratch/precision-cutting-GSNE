using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.database.db.modle
{
    [Table("file_table_item")]
    internal class FileTableItemModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("directory_id")]//目录ID
        public long DirectoryId { get; set; }

        [Column("device_data_no")]//型号参数编号
        public string DeviceDataNo { get; set; }

        [Column("device_data_id")]//型号参数ID
        public string DeviceDataId { get; set; }

        [Column("unit")]//单位(mm,inch)
        public string unit { get; set; } = "inch";

        [Column("spindle_rev")]//主轴转速
        public int SpindleRev { get; set; } = 0;

        [Column("precut_process_no")]//预切割流程编号
        public string PrecutProcessNo { get; set; } = "0";

        [Column("cutting_ch_seq")]//切割顺序
        public string CuttingChSeq { get; set; } = "0";

        [Column("workbench_ch1")]// 工作台长
        public float WorkbenchCh1 { get; set; } = 155;

        [Column("workbench_ch2")]// 工作台宽
        public float WorkbenchCh2 { get; set; } = 155;

        [Column("workShape")]//切割片形状(圆形/长方形)
        public int WorkShape { get; set; } = 1;

        [Column("round")]//圆形切割尺寸（直径）
        public string Round { get; set; } = "0";

        [Column("square_ch1")]//长方形-长
        public string SquareCh1 { get; set; } = "0";

        [Column("square_ch2")]//长方形-宽
        public string SquareCh2 { get; set; } = "0";

        [Column("work_thickness")]//晶片厚度
        public string WorkThickness { get; set; } = "0";

        [Column("tape_thickness")]//膜的厚度
        public string TapeThickness { get; set; } = "0";

        [Column("device_type")]//类型;目前有两种类型
        public int DeviceType { get; set; }
    }
}
