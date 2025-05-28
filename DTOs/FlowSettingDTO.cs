using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.DTOs
{
    public class FlowSettingDTO
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("flowId")]
        public string FlowId { get; set; }

        [JsonProperty("businessId")]
        public string BusinessId { get; set; }

        [JsonProperty("baseId")]
        public string BaseId { get; set; }

        [JsonProperty("defaultValue")]
        public string DefaultValue { get; set; }

        [JsonProperty("sopId")]
        public string SopId { get; set; }

        [JsonProperty("field")]
        public string Field { get; set; }

        [JsonProperty("fieldLabel")]
        public string FieldLabel { get; set; }

        [JsonProperty("fieldType")]
        public string FieldType { get; set; }

        [JsonProperty("requiredFlag")]
        public string RequiredFlag { get; set; }

        [JsonProperty("delFlag")]
        public decimal? DelFlag { get; set; }

        [JsonProperty("childrenNum")]
        public decimal? ChildrenNum { get; set; }

        [JsonProperty("fieldValue")]
        public string FieldValue { get; set; }

        [JsonProperty("fieldValueId")]
        public string FieldValueId { get; set; }

        [JsonProperty("children")]
        public List<FieldChildrenDTO> Children { get; set; } = new List<FieldChildrenDTO>();

        [JsonProperty("groupCode")]
        public string GroupCode { get; set; }

        [JsonProperty("coGroupCode")]
        public string CoGroupCode { get; set; } = "GX-QG-001";

        [JsonProperty("nextFlag")]
        public string NextFlag { get; set; }

        [JsonProperty("selectList")]
        public string SelectList { get; set; }

        [JsonProperty("llzmzp")]
        public string Llzmzp { get; set; }

        [JsonProperty("llfmzp")]
        public string Llfmzp { get; set; }

        [JsonProperty("disabledFlag")]
        public string DisabledFlag { get; set; }

        [JsonProperty("productType")]
        public string ProductType { get; set; }

        public FlowsValuesDTO ToFlowsValuesDTO()
        {
            return new FlowsValuesDTO
            {
                Field = Field,
                FieldLabel = FieldLabel,
                FieldId = Id,
                FieldValue = FieldValue,
                FieldValueId = FieldValueId,
                FieldType = FieldType,
                FlowSetId = Id,
                GroupCode = GroupCode,
                CoGroupCode = CoGroupCode,
                FlowId = FlowId,
                BaseId = BaseId,
                SopId = SopId,
                Children = this.Children == null ? new List<FieldChildrenDTO>() : Children.Select(child => child.Clone()).ToList(),
            };
        }
    }
}

