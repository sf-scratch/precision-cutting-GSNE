using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace 精密切割系统.DTOs
{
    public class FlowsValuesDTO
    {
        /// <summary>
        /// 字段
        /// </summary>
        [JsonProperty("field")]
        public string Field { get; set; }

        /// <summary>
        /// 字段名
        /// </summary>
        [JsonProperty("fieldLabel")]
        public string FieldLabel { get; set; }

        /// <summary>
        /// 字段ID
        /// </summary>
        [JsonProperty("fieldId")]
        public string FieldId { get; set; }

        /// <summary>
        /// 字段值
        /// </summary>
        [JsonProperty("fieldValue")]
        public string FieldValue { get; set; }

        /// <summary>
        /// 字段值ID
        /// </summary>
        [JsonProperty("fieldValueId")]
        public string FieldValueId { get; set; }

        /// <summary>
        /// 流程设置ID
        /// </summary>
        [JsonProperty("flowSetId")]
        public string FlowSetId { get; set; }

        /// <summary>
        /// 流程ID
        /// </summary>
        [JsonProperty("flowId")]
        public string FlowId { get; set; }

        /// <summary>
        /// sop主键
        /// </summary>
        [JsonProperty("sopId")]
        public string SopId { get; set; }

        /// <summary>
        /// 字段类型
        /// </summary>
        [JsonProperty("fieldType")]
        public string FieldType { get; set; } = "1";

        /// <summary>
        /// 分组编码
        /// </summary>
        [JsonProperty("groupCode")]
        public string GroupCode { get; set; }

        /// <summary>
        /// 父级ID
        /// </summary>
        [JsonProperty("parentId")]
        public string ParentId { get; set; }

        /// <summary>
        /// 组织机构
        /// </summary>
        [JsonProperty("dataAuth")]
        public string DataAuth { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        [JsonProperty("createUser")]
        public string CreateUser { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [JsonProperty("createTime")]
        public DateTime? CreateTime { get; set; }

        [JsonProperty("groupOperateId")]
        public string GroupOperateId { get; set; }

        [JsonProperty("coGroupCode")]
        public string CoGroupCode { get; set; } = "GX-QG-001";

        [JsonProperty("baseId")]
        public string BaseId { get; set; }

        /// <summary>
        /// 字段列表
        /// </summary>
        [JsonProperty("children")]
        public List<FieldChildrenDTO> Children { get; set; } = new List<FieldChildrenDTO>();

        public FlowsValuesDTO Clone()
        {
            return new FlowsValuesDTO
            {
                Field = this.Field,
                FieldLabel = this.FieldLabel,
                FieldId = this.FieldId,
                FieldValue = this.FieldValue,
                FieldValueId = this.FieldValueId,
                FlowSetId = this.FlowSetId,
                FlowId = this.FlowId,
                SopId = this.SopId,
                FieldType = this.FieldType,
                GroupCode = this.GroupCode,
                ParentId = this.ParentId,
                DataAuth = this.DataAuth,
                CreateUser = this.CreateUser,
                CreateTime = this.CreateTime,
                GroupOperateId = this.GroupOperateId,
                CoGroupCode = this.CoGroupCode,
                BaseId = this.BaseId,
                Children = this.Children == null ? new List<FieldChildrenDTO>() : this.Children.Select(c => (FieldChildrenDTO)c.Clone()).ToList()
            };
        }
    }

}
