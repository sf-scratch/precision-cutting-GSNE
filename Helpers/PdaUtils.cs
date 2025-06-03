using NPOI.SS.Formula.Functions;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.DTOs;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.HttpClients;

namespace 精密切割系统.Helpers
{
    public class PdaUtils
    {
        private static Tuple<FieldValuesDTO, Dictionary<string, FlowsValuesDTO>> _tuple;

        public static async Task<bool> ComputerPracticeAsync(string lunguId)
        {
            if (!GlobalParams.OnlineMES) return true;
            List<FlowSettingDTO>? allFieldValues = await HttpUtils.QueryFlowSettingByIdAsync();
            if (allFieldValues == null)
            {
                return false;
            }
            Dictionary<string, FlowsValuesDTO> flowsDic = allFieldValues.Select(x => x.ToFlowsValuesDTO()).ToDictionary(x => x.FieldLabel);
            FieldValuesDTO fieldValues = GetFieldValuesDTO(flowsDic, "QG-03", lunguId);
            string? groupOperateId = await HttpUtils.InsertFlowValuesAsync(fieldValues);
            if (groupOperateId == null)
            {
                return false;
            }
            fieldValues.GroupOperateId = groupOperateId;
            fieldValues.List[0].GroupOperateId = groupOperateId;
            fieldValues.List.RemoveAt(1);
            _tuple = new Tuple<FieldValuesDTO, Dictionary<string, FlowsValuesDTO>>(fieldValues, flowsDic);
            return true;
        }

        private static FieldValuesDTO GetFieldValuesDTO(Dictionary<string, FlowsValuesDTO> flowsDic, string deviceCode, string lungu)
        {
            FieldValuesDTO fieldValuesDTO = new FieldValuesDTO();
            var flowsValuesDto1 = flowsDic["设备编码"].Clone();
            var flowsValuesDto2 = flowsDic["轮毂"].Clone();
            flowsValuesDto1.FieldValue = deviceCode;
            flowsValuesDto2.FieldValue = lungu;
            fieldValuesDTO.List.Add(flowsValuesDto1);
            fieldValuesDTO.List.Add(flowsValuesDto2);
            return fieldValuesDTO;
        }

        private static FlowsValuesDTO? MslValues = null;
        private static int SharpenCount = 1;


        public static void AddStandardBlade(string blade)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            if (fieldDic.TryGetValue("标准刃长", out FlowsValuesDTO? value))
            {
                FlowsValuesDTO dto = value.Clone();
                dto.GroupOperateId = fieldValues.GroupOperateId;
                dto.FieldValue = blade;
                fieldValues.List.Add(dto);
            }
        }

        public static void AddStandardAspectRatio(double aspectRatio)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            if (fieldDic.TryGetValue("标准刃长", out FlowsValuesDTO? value))
            {
                FlowsValuesDTO dto = value.Clone();
                dto.GroupOperateId = fieldValues.GroupOperateId;
                dto.FieldValue = aspectRatio.ToString();
                fieldValues.List.Add(dto);
            }
        }

        public static void AddStandardCutSpeed(float cutSpeed)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            if (fieldDic.TryGetValue("标准切割速率(mm/s)", out FlowsValuesDTO? value))
            {
                FlowsValuesDTO dto = value.Clone();
                dto.GroupOperateId = fieldValues.GroupOperateId;
                dto.FieldValue = cutSpeed.ToString();
                fieldValues.List.Add(dto);
            }
        }

        public static void AddStandardSharpenSpeed(string sharpenSpeed)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            if (fieldDic.TryGetValue("标准磨刀速率(mm/s)", out FlowsValuesDTO? value))
            {
                FlowsValuesDTO dto = value.Clone();
                dto.GroupOperateId = fieldValues.GroupOperateId;
                dto.FieldValue = sharpenSpeed;
                fieldValues.List.Add(dto);
            }
        }

        public static void AddResidueSharpenTimes(int residueSharpenTiems)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            if (fieldDic.TryGetValue("剩余磨刀数", out FlowsValuesDTO? value))
            {
                FlowsValuesDTO dto = value.Clone();
                dto.GroupOperateId = fieldValues.GroupOperateId;
                dto.FieldValue = residueSharpenTiems.ToString();
                fieldValues.List.Add(dto);
            }
        }

        public static void AddTotalSharpenTimes(int totalSharpenTiems)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            if (fieldDic.TryGetValue("总磨刀数", out FlowsValuesDTO? value))
            {
                FlowsValuesDTO dto = value.Clone();
                dto.GroupOperateId = fieldValues.GroupOperateId;
                dto.FieldValue = totalSharpenTiems.ToString();
                fieldValues.List.Add(dto);
            }
        }

        public static void AddSharpen(float wearAmount, int count)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            if (MslValues is null)
            {
                MslValues = fieldDic["总磨损量(um)"].Clone();
                MslValues.FieldValue = "0";
                MslValues.GroupOperateId = fieldValues.GroupOperateId;
                fieldValues.List.Add(MslValues);
            }
            MslValues.FieldValue = (float.Parse(MslValues.FieldValue) + wearAmount).ToString();
            FlowsValuesDTO mosunliang = MslValues.Children[0].FieldList[0].ToFlowsValuesDTO();
            mosunliang.ParentId = MslValues.FieldId;
            mosunliang.GroupOperateId = fieldValues.GroupOperateId;
            mosunliang.FieldValue = wearAmount.ToString();
            mosunliang.GroupCode = SharpenCount.ToString();
            FlowsValuesDTO mdsl = MslValues.Children[0].FieldList[1].ToFlowsValuesDTO();
            mdsl.ParentId = MslValues.FieldId;
            mdsl.GroupOperateId = fieldValues.GroupOperateId;
            mdsl.FieldValue = count.ToString();
            mdsl.GroupCode = SharpenCount.ToString();
            fieldValues.List.Add(mosunliang);
            fieldValues.List.Add(mdsl);
        }

        public static void AddToolMarkWidth(double toolMarkWidth)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            // 刀痕宽度(um)
            FlowsValuesDTO dto = fieldDic["刀痕宽度(um)"].Clone();
            dto.GroupOperateId = fieldValues.GroupOperateId;
            dto.FieldValue = Math.Round(toolMarkWidth * 1000).ToString();
            fieldValues.List.Add(dto);
        }

        public static void AddToolMarkActualWidth(double toolMarkActualWidth)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            // 刀痕宽度(um)
            FlowsValuesDTO dto = fieldDic["刀痕实际宽度"].Clone();
            dto.GroupOperateId = fieldValues.GroupOperateId;
            dto.FieldValue = Math.Round(toolMarkActualWidth * 1000).ToString();
            fieldValues.List.Add(dto);
        }

        public static void AddFirstToolMarkWidth(double firstToolMarkWidth)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            // 刀痕宽度(um)
            FlowsValuesDTO dto = fieldDic["第一刀刀痕宽度"].Clone();
            dto.GroupOperateId = fieldValues.GroupOperateId;
            dto.FieldValue = Math.Round(firstToolMarkWidth * 1000).ToString();
            fieldValues.List.Add(dto);
        }

        public static void AddFirstToolMarkImage(Mat mat)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            // 上传第一刀图片
            string? firstUrl = HttpUtils.UploadImage(mat);
            if (firstUrl == null)
            {
                return;
            }
            // 第一刀图片
            if (fieldDic.ContainsKey("第一刀（10mm/s）"))
            {
                FlowsValuesDTO dhpz = fieldDic["第一刀（10mm/s）"].Clone();
                dhpz.GroupOperateId = fieldValues.GroupOperateId;
                dhpz.FieldValue = firstUrl;
                fieldValues.List.Add(dhpz);
            }
        }

        public static void AddSecondToolMarkImage(Mat Mat)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            // 上传第二刀图片
            string? secondUrl = HttpUtils.UploadImage(Mat);
            if (secondUrl == null)
            {
                return;
            }
            // 第二刀图片
            if (fieldDic.ContainsKey("第二刀(20mm/s)"))
            {
                FlowsValuesDTO dhpz2 = fieldDic["第二刀(20mm/s)"].Clone();
                dhpz2.GroupOperateId = fieldValues.GroupOperateId;
                dhpz2.FieldValue = secondUrl;
                fieldValues.List.Add(dhpz2);
            }
            else if (fieldDic.ContainsKey("第二刀（60mm/s）"))
            {
                FlowsValuesDTO dhpz2 = fieldDic["第二刀（0mm/s）"].Clone();
                dhpz2.GroupOperateId = fieldValues.GroupOperateId;
                dhpz2.FieldValue = secondUrl;
                fieldValues.List.Add(dhpz2);
            }
        }

        public static void AddMaximumCollapseAngleImage(Mat Mat)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            string? imageUrl = HttpUtils.UploadImage(Mat);
            if (imageUrl == null)
            {
                return;
            }
            if (fieldDic.TryGetValue("崩角拍照", out FlowsValuesDTO? value))
            {
                FlowsValuesDTO dto = value.Clone();
                dto.GroupOperateId = fieldValues.GroupOperateId;
                dto.FieldValue = imageUrl;
                fieldValues.List.Add(dto);
            }
        }

        public static void AddMaximumCollapseAngle(double maximumCollapseAngle)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            if (fieldDic.TryGetValue("崩角最大值", out FlowsValuesDTO? value))
            {
                FlowsValuesDTO dto = value.Clone();
                dto.GroupOperateId = fieldValues.GroupOperateId;
                dto.FieldValue = Math.Round(maximumCollapseAngle * 1000).ToString();
                fieldValues.List.Add(dto);
            }
        }

        public static void AddMaxCutSpeed(float maxCutSpeed)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            if (fieldDic.TryGetValue("最高切割速度(mm/s)", out FlowsValuesDTO? value))
            {
                FlowsValuesDTO dto = value.Clone();
                dto.GroupOperateId = fieldValues.GroupOperateId;
                dto.FieldValue = maxCutSpeed.ToString();
                fieldValues.List.Add(dto);
            }
        }

        public static void AddSingleCollapseAngle(double singleCollapseAngle)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            if (fieldDic.TryGetValue("单边崩角大小", out FlowsValuesDTO? value))
            {
                FlowsValuesDTO dto = value.Clone();
                dto.GroupOperateId = fieldValues.GroupOperateId;
                dto.FieldValue = Math.Round(singleCollapseAngle * 1000, 1).ToString();
                fieldValues.List.Add(dto);
            }
        }



        public static void AddBladeEdgeBreakageGrade(string bladeEdgeBreakageGrade)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            if (fieldDic.TryGetValue("刀片崩边等级", out FlowsValuesDTO? value))
            {
                FlowsValuesDTO dto = value.Clone();
                dto.GroupOperateId = fieldValues.GroupOperateId;
                dto.FieldValue = bladeEdgeBreakageGrade;
                fieldValues.List.Add(dto);
            }
        }

        public static void AddWearAmountBeforeCircle(float wearAmountBeforeCircle)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            if (fieldDic.TryGetValue("真圆前磨损量", out FlowsValuesDTO? value))
            {
                FlowsValuesDTO dto = value.Clone();
                dto.GroupOperateId = fieldValues.GroupOperateId;
                dto.FieldValue = Math.Round(wearAmountBeforeCircle * 1000).ToString();
                fieldValues.List.Add(dto);
            }
        }

        public static void AddSingleWearAmount(float singleWearAmount)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            if (fieldDic.TryGetValue("单刀磨损量(修真圆后)", out FlowsValuesDTO? value))
            {
                FlowsValuesDTO dto = value.Clone();
                dto.GroupOperateId = fieldValues.GroupOperateId;
                dto.FieldValue = Math.Round(singleWearAmount * 1000).ToString();
                fieldValues.List.Add(dto);
            }
        }

        public static void AddWearAmountAfterCircle(float wearAmountAfterCircle)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            if (fieldDic.TryGetValue("真圆后磨损量", out FlowsValuesDTO? value))
            {
                FlowsValuesDTO dto = value.Clone();
                dto.GroupOperateId = fieldValues.GroupOperateId;
                dto.FieldValue = Math.Round(wearAmountAfterCircle * 1000).ToString();
                fieldValues.List.Add(dto);
            }
        }

        public static void AddBladeLifeGrade(string bladeLifeGrade)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            if (fieldDic.TryGetValue("刀片寿命等级", out FlowsValuesDTO? value))
            {
                FlowsValuesDTO dto = value.Clone();
                dto.GroupOperateId = fieldValues.GroupOperateId;
                dto.FieldValue = bladeLifeGrade;
                fieldValues.List.Add(dto);
            }
        }

        public static async Task UpdateFlowValuesAsync()
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            await HttpUtils.InsertFlowValuesAsync(fieldValues);
        }

        public static async Task SetCompletedAsync()
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            fieldValues.Status = "3";
            await HttpUtils.InsertFlowValuesAsync(fieldValues);
        }
    }
}
