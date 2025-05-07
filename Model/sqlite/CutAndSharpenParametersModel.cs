using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.sqlite
{
    [Table("cut_sharpen_parameters")]
    public class CutAndSharpenParametersModel
    {
        [PrimaryKey]
        [Column("blade_thickness")]
        public long BladeThickness { get; set; }

        /// <summary>
        /// 刀刃蚀刻后最长暴露
        /// </summary>
        [Column("longest_exposure")]
        public string LongestExposure { get; set; }

        /// <summary>
        /// 刀刃蚀刻后最短暴露
        /// </summary>
        [Column("shortest_exposure")]
        public string ShortestExposure { get; set; }
    }
}
