using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.DTOs
{
    public class FieldChildrenDTO
    {
        [JsonProperty("fieldList")]
        public List<FlowSettingDTO> FieldList { get; set; } = new List<FlowSettingDTO>();

        public FieldChildrenDTO Clone()
        {
            return new FieldChildrenDTO
            {
                FieldList = this.FieldList.Select(field => new FlowSettingDTO
                {
                    Id = field.Id,
                    FlowId = field.FlowId,
                    BusinessId = field.BusinessId,
                    BaseId = field.BaseId,
                    DefaultValue = field.DefaultValue,
                    SopId = field.SopId,
                    Field = field.Field,
                    FieldLabel = field.FieldLabel,
                    FieldType = field.FieldType,
                    RequiredFlag = field.RequiredFlag,
                    DelFlag = field.DelFlag,
                    ChildrenNum = field.ChildrenNum,
                    FieldValue = field.FieldValue,
                    FieldValueId = field.FieldValueId,
                    Children = field.Children == null ? new List<FieldChildrenDTO>() : field.Children.Select(child => child.Clone()).ToList(),
                    GroupCode = field.GroupCode,
                    CoGroupCode = "GX-QG-001"
                }).ToList()
            };
        }
    }
}
