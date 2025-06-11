using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.DTOs
{
    public class UpdateOperateStatusDTO
    {
        [JsonProperty("coGroupCode")]
        public string CoGroupCode { get; set; }

        [JsonProperty("operateId")]
        public string OperateId { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; } = "2";

        /// <summary>
        /// 切割自定义状态
        /// 0/null 正常 1 返工到蚀刻组装 2 报废
        /// </summary>
        [JsonProperty("qgCustomStatus")]
        public string QgCustomStatus { get; set; } = "0";

        [JsonProperty("deptId")]
        public string DeptId { get; set; } = "c1794d1836f94dbfa536f6deaacd131c";

        [JsonProperty("orgId")]
        public string OrgId { get; set; } = "FA174AFF136D496A87B65443D22357E3";

        [JsonProperty("updateOperateStatusHubVoList")]
        public List<UpdateOperateStatusHubDTO> UpdateOperateStatusHubVoList { get; set; }

        public UpdateOperateStatusDTO()
        {
            UpdateOperateStatusHubVoList = new List<UpdateOperateStatusHubDTO>();
        }
    }
}
