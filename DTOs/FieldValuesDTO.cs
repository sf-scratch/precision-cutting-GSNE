using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Converters;

namespace 精密切割系统.DTOs
{
    public class FieldValuesDTO
    {
        /// <summary>
        /// 状态
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; } = "1";

        /// <summary>
        /// SOP主键
        /// </summary>
        [JsonProperty("sopId")]
        public string SopId { get; set; } = "7c66d1fcd7e4490fb6a3fd2c4d76b70f";

        /// <summary>
        /// 流程ID
        /// </summary>
        [JsonProperty("flowId")]
        public string FlowId { get; set; }

        /// <summary>
        /// SOP编码
        /// </summary>
        [JsonProperty("sopCode")]
        public string SopCode { get; set; } = "SOP-QG-SJ-001";

        /// <summary>
        /// 业务编码
        /// </summary>
        [JsonProperty("businessCode")]
        public string BusinessCode { get; set; }

        /// <summary>
        /// 设备编码
        /// </summary>
        [JsonProperty("deviceCode")]
        public string DeviceCode { get; set; }

        /// <summary>
        /// 协同组编码
        /// </summary>
        [JsonProperty("coGroupCode")]
        public string CoGroupCode { get; set; } = "GX-QG-001";

        /// <summary>
        /// 用户ID
        /// </summary>
        [JsonProperty("userId")]
        public string UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        [JsonProperty("userName")]
        public string UserName { get; set; } = "戚太菊";

        /// <summary>
        /// 基础ID
        /// </summary>
        [JsonProperty("baseId")]
        public string BaseId { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        [JsonProperty("startTime")]
        [JsonConverter(typeof(UnixTimestampConverter))]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        [JsonProperty("endTime")]
        [JsonConverter(typeof(UnixTimestampConverter))]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 组操作ID
        /// </summary>
        [JsonProperty("groupOperateId")]
        public string GroupOperateId { get; set; }

        /// <summary>
        /// 流程值列表
        /// </summary>
        [JsonProperty("list")]
        public List<FlowsValuesDTO> List { get; set; } = new List<FlowsValuesDTO>();
    }

}
