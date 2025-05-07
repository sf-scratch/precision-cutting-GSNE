using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.DTOs
{
    public class LunguInfoDTO
    {
        /// <summary>
        /// 刀片类型
        /// </summary>
        [JsonProperty("finishType")]
        public string BladeType { get; set; }
    }
}
