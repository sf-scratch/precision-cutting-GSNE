using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.DTOs
{
    internal class InsertFlowValuesResponseDTO
    {
        [JsonProperty("abnormalList")]
        public List<object> AbnormalList { get; set; } = new List<object>();  // 空数组初始化
        [JsonProperty("groupOperateId")]
        public string GroupOperateId { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }

    }
}
