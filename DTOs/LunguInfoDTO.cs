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
        /// 当前车间
        /// </summary>
        [JsonProperty("currentGroup")]
        public string CurrentGroup { get; set; }
    }
}
