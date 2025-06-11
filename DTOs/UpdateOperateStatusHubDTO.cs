using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.DTOs
{
    using Newtonsoft.Json;

    public class UpdateOperateStatusHubDTO
    {
        /// <summary>
        /// 轮毂编号
        /// </summary>
        [JsonProperty("hubNumber")]
        public string HubNumber { get; set; }

        /// <summary>
        /// 是否合格
        /// </summary>
        [JsonProperty("qualityValue")]
        public string QualityValue { get; set; }

        /// <summary>
        /// 质检图片
        /// </summary>
        [JsonProperty("qualityImgUrl")]
        public string QualityImgUrl { get; set; }

        /// <summary>
        /// 质检备注
        /// </summary>
        [JsonProperty("qualityRemark")]
        public string QualityRemark { get; set; }

        [JsonProperty("qualityResult")]
        public string QualityResult { get; set; }

        /// <summary>
        /// 报废类型编码
        /// </summary>
        [JsonProperty("scrapTypeValue")]
        public string ScrapTypeValue { get; set; }

        /// <summary>
        /// 报废类型文本
        /// </summary>
        [JsonProperty("scrapTypeName")]
        public string ScrapTypeName { get; set; }

        /// <summary>
        /// 报废原因编码
        /// </summary>
        [JsonProperty("scrapYxValue")]
        public string ScrapYxValue { get; set; }

        /// <summary>
        /// 报废原因文本
        /// </summary>
        [JsonProperty("scrapYxName")]
        public string ScrapYxName { get; set; }

        /// <summary>
        /// 报废原因id
        /// </summary>
        [JsonProperty("scrapYxId")]
        public string ScrapYxId { get; set; }

        /// <summary>
        /// 报废环节编码
        /// </summary>
        [JsonProperty("badCoGroupCode")]
        public string BadCoGroupCode { get; set; }

        /// <summary>
        /// 报废环节文本
        /// </summary>
        [JsonProperty("badCoGroupName")]
        public string BadCoGroupName { get; set; }

        /// <summary>
        /// 用户id
        /// </summary>
        [JsonProperty("userId")]
        public string UserId { get; set; }

        /// <summary>
        /// 机构id
        /// </summary>
        [JsonProperty("orgId")]
        public string OrgId { get; set; }

        /// <summary>
        /// 部门id
        /// </summary>
        [JsonProperty("deptId")]
        public string DeptId { get; set; }

        /// <summary>
        /// 报废标志
        /// </summary>
        [JsonProperty("scrapFlag")]
        public string ScrapFlag { get; set; }

        [JsonProperty("cleanImageUrl")]
        public string CleanImageUrl { get; set; }

        /// <summary>
        /// 您新添加的属性示例
        /// </summary>
        [JsonProperty("sjSpec")]
        public string SjSpec { get; set; }
    }
}
