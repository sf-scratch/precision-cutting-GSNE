using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.DTOs
{
    public class QgParamsDTO
    {
        /// <summary>
        /// 切割速率
        /// </summary>
        [JsonProperty("qgSpeed")]
        public float QgSpeed { get; set; }

        /// <summary>
        /// 刀片崩边等级
        /// </summary>
        [JsonProperty("dpbbdj")]
        public string Dpbbdj { get; set; }

        /// <summary>
        /// 刀刃寿命等级
        /// </summary>
        [JsonProperty("drsmdj")]
        public string Drsmdj { get; set; }
    }
}
