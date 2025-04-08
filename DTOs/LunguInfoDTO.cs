using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.DTOs
{
    internal class LunguInfoDTO
    {
        /// <summary>
        /// AB平均厚度
        /// </summary>
        [JsonProperty("abhdpj")]
        public double ABAverageThickness { get; set; }

        /// <summary>
        /// 最长刀刃
        /// </summary>
        [JsonProperty("zcskdrcd")]
        public double LongestBlade { get; set; }
    }
}
