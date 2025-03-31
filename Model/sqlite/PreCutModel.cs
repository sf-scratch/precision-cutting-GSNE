using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace 精密切割系统.database.db.modle
{
    [Table("pre_cut")]
    public class PreCutModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("precut_process_no")]//预切割编号
        public string PrecutNo { get; set; }

        [Column("precut_process_ID")]//预切割名称
        public string PrecutID { get; set; } = "id 0";

        [Column("precut_process_type")]//预切割方式（1、2、3表示3种模式，现在默认第2种）
        public long PrecutType { get; set; } = 2;

        [Column("used_blade_seq_no")]//旧刀片预切割开始序号
        public int UsedBladeNo { get; set; }

        [Column("new_blade_seq_no")]//新刀片预切割开始序号
        public int NewBladeNo { get; set; }

        [Column("precut_set_during_precut_decrease")]//切割中减速切割还原量
        public string PrecutDecrease { get; set; } = "0.000";

        [Column("set_for_work_thickness_greater")]//工件厚度大于多少需要做预切割
        public string WorkThickness { get; set; }

        [Column("feed_spd")]//顺序1-进刀速度（mm/s）  30个，数组
        public string FeedSpd { get; set; }= getDefault(1).ToString();

        [Column("of_lines")]//顺序1-切割刀数（刀）  30个，数组
        public string OfLines { get; set; }= getDefault(0).ToString();

        [Column("feed_distance")]//顺序1-长度 m  30个，数组
        public string FeedDistance { get; set; } = getDefault(1).ToString();

        [Column("new_blade_initial_feed_speed")]//新刀初始进刀速度
        public string NewBladeInitialSpeed { get; set; }

        [Column("old_blade_initial_feed_speed")]//旧刀初始进刀速度
        public string OldBladeInitialSpeed { get; set; }

        [Column("reduced_speed_at_re_precut")]//再次切割归零的时候的速度
        public string SpeedAtReprecut { get; set; }

        [Column("precut_end_speed")]//最终结束速度
        public string PrecutEndSpeed { get; set; }

        [Column("lines_of_precut")]//预切割次数
        public string LinesOfPrecut { get; set; }

        [Column("work_thickness_greater")]//每次加多少距离
        public string WorkThicknessGreater { get; set; }

        [Column("is_delete")]//该条数据是否被删除
        public string IsDelete { get; set; } = "False";


        /// <summary>
        /// 设置默认值；共30个
        /// </summary>
        /// <param name="type">1 有3位小数 0 没有小数</param>
        /// <returns></returns>
        private static string getDefault(int type)
        {
            int count = 30;
            string[] defData = new string[count];
            for (int i = 0; i < count; i++)
            {
                defData[i] = 0 + (type == 1 ? ".000" : "");
            }
            string result = string.Join(",", defData);
            return result;
        }
    }
}
