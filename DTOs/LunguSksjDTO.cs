using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.DTOs
{
    public class LunguSksjDTO
    {
        /// <summary>
        /// AB平均厚度
        /// </summary>
        [JsonProperty("abhdpj")]
        public float ABAverageThickness { get; set; }

        /// <summary>
        /// 最长刀刃
        /// </summary>
        [JsonProperty("zcskdrcd")]
        public float LongestBlade { get; set; }

        /// <summary>
        /// 刀片类型
        /// </summary>
        [JsonProperty("finishType")]
        public string BladeType { get; set; }

        /// <summary>
        /// 订单类型
        /// </summary>
        [JsonProperty("itemSpec")]
        public string OrderType { get; set; }

        /// <summary>
        /// 刀刃规格
        /// </summary>
        [JsonProperty("ggxh")]
        public string BladeEdgeType { get; set; }

        /// <summary>
        /// 刀片外径
        /// </summary>
        [JsonProperty("dpwj")]
        public string BladeOuterDiameter { get; set; }
    }
}
