using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


//当前配置集合模块
namespace 精密切割系统.database.db.modle
{
    [Table("current_configuration")]
    internal class CurrentConfigurationModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("device_data_id")]//型号目录
        public long DeviceDataId { get; set; }

        [Column("channel_num")]//当前切割面
        public string ChannelNum { get; set; }

        [Column("precut_data_id")]//预切割流程
        public string PrecutDataId { get; set; }

        [Column("blade_height_data_id")]// 刀片测高
        public long BladeHeightDataId { get; set; }

        [Column("cleared_cut_all_num")]// 清零后总刀数
        public int ClearedCutAllNum { get; set; }

        [Column("cleared_cut_all_distance")]// 清零后总距离
        public float ClearedCutAllDistance { get; set; }
        
    }
}
