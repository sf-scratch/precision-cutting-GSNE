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
        private static FlowsValuesDTO? MslValues = null;
        private static FlowsValuesDTO? AfterCircleMslValues = null;
        private static int GroupCode = 0;
        private static string _lunguId;

        private static void InitParams()
        {
            MslValues = null;
            AfterCircleMslValues = null;
            GroupCode = 0;
        }

        public static async Task<bool> ComputerPracticeAsync(string lunguId)
        {
            if (!GlobalParams.OnlineMES) return true;
            _lunguId = lunguId;
            List<FlowSettingDTO>? allFieldValues = await HttpUtils.QueryFlowSettingByIdAsync();
            if (allFieldValues == null)
            {
                MaterialSnackUtils.MaterialSnack("QueryFlowSettingByIdAsync失败", MaterialSnackUtils.SnackType.WARNING, 0);
                return false;
            }
            Dictionary<string, FlowsValuesDTO> flowsDic = allFieldValues.Select(x => x.ToFlowsValuesDTO()).ToDictionary(x => x.FieldLabel);
            FieldValuesDTO fieldValues = GetFieldValuesDTO(flowsDic, "QG-03", lunguId);
            HttpUtilsResult<string> groupOperateIdRes = await HttpUtils.InsertFlowValuesAsync(fieldValues);
            if (groupOperateIdRes.Data == null)
            {
                MaterialSnackUtils.MaterialSnack(groupOperateIdRes.Msg, MaterialSnackUtils.SnackType.WARNING, 0);
                return false;
            }
            fieldValues.GroupOperateId = groupOperateIdRes.Data;
            fieldValues.List[0].GroupOperateId = groupOperateIdRes.Data;
            fieldValues.List.RemoveAt(1);
            _tuple = new Tuple<FieldValuesDTO, Dictionary<string, FlowsValuesDTO>>(fieldValues, flowsDic);
            InitParams();
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
                dto.FieldValue = Math.Round(cutSpeed).ToString();
                fieldValues.List.Add(dto);
            }
        }

        public static void AddStandardSharpenSpeed(float sharpenSpeed)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            if (fieldDic.TryGetValue("标准磨刀速率(mm/s)", out FlowsValuesDTO? value))
            {
                FlowsValuesDTO dto = value.Clone();
                dto.GroupOperateId = fieldValues.GroupOperateId;
                dto.FieldValue = Math.Round(sharpenSpeed).ToString();
                fieldValues.List.Add(dto);
            }
        }

        public static void AddResidueBlade(float blade)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            if (fieldDic.TryGetValue("剩余刀刃长度", out FlowsValuesDTO? value))
            {
                FlowsValuesDTO dto = value.Clone();
                dto.GroupOperateId = fieldValues.GroupOperateId;
                dto.FieldValue = Math.Round(blade).ToString();
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
            int wearAmountInt = (int)Math.Round(wearAmount * 1000);
            int childrenIndex = GroupCode;
            GroupCode++;
            if (childrenIndex == 0)
            {
                MslValues.FieldValue = Math.Round(float.Parse(MslValues.FieldValue) + wearAmountInt).ToString();
                FlowsValuesDTO mosunliang = MslValues.Children[0].FieldList[0].ToFlowsValuesDTO();
                mosunliang.ParentId = MslValues.FieldId;
                mosunliang.GroupOperateId = fieldValues.GroupOperateId;
                mosunliang.FieldValue = wearAmountInt.ToString();
                mosunliang.GroupCode = GroupCode.ToString();
                fieldValues.List.Add(mosunliang);
                FlowsValuesDTO mdsl = MslValues.Children[0].FieldList[1].ToFlowsValuesDTO();
                mdsl.ParentId = MslValues.FieldId;
                mdsl.GroupOperateId = fieldValues.GroupOperateId;
                mdsl.FieldValue = count.ToString();
                mdsl.GroupCode = GroupCode.ToString();
                fieldValues.List.Add(mdsl);
                FlowsValuesDTO ddmsl = MslValues.Children[0].FieldList[2].ToFlowsValuesDTO();
                ddmsl.ParentId = MslValues.FieldId;
                ddmsl.GroupOperateId = fieldValues.GroupOperateId;
                ddmsl.FieldValue = Math.Round((float)wearAmountInt / count, 2).ToString();
                ddmsl.GroupCode = GroupCode.ToString();
                fieldValues.List.Add(ddmsl);
            }
            else
            {
                MslValues.FieldValue = Math.Round(float.Parse(MslValues.FieldValue) + wearAmount * 1000).ToString();
                var fieldList = MslValues.Children[0].Clone();
                MslValues.Children.Add(fieldList);
                FlowsValuesDTO mosunliang = fieldList.FieldList[0].ToFlowsValuesDTO();
                mosunliang.ParentId = MslValues.FieldId;
                mosunliang.GroupOperateId = fieldValues.GroupOperateId;
                mosunliang.FieldValue = Math.Round(wearAmount * 1000).ToString();
                mosunliang.GroupCode = GroupCode.ToString();
                fieldValues.List.Add(mosunliang);
                FlowsValuesDTO mdsl = fieldList.FieldList[1].ToFlowsValuesDTO();
                mdsl.ParentId = MslValues.FieldId;
                mdsl.GroupOperateId = fieldValues.GroupOperateId;
                mdsl.FieldValue = count.ToString();
                mdsl.GroupCode = GroupCode.ToString();
                fieldValues.List.Add(mdsl);
                FlowsValuesDTO ddmsl = fieldList.FieldList[2].ToFlowsValuesDTO();
                ddmsl.ParentId = MslValues.FieldId;
                ddmsl.GroupOperateId = fieldValues.GroupOperateId;
                ddmsl.FieldValue = Math.Round(wearAmount / count * 1000).ToString();
                ddmsl.GroupCode = GroupCode.ToString();
                fieldValues.List.Add(ddmsl);
            }
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

        public static void AddSecondToolMarkImage(Mat mat)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            // 上传第二刀图片
            string? secondUrl = HttpUtils.UploadImage(mat);
            if (secondUrl == null)
            {
                return;
            }
            // 第二刀图片
            if (fieldDic.ContainsKey("第二刀（60mm/s）"))
            {
                FlowsValuesDTO dhpz2 = fieldDic["第二刀（60mm/s）"].Clone();
                dhpz2.GroupOperateId = fieldValues.GroupOperateId;
                dhpz2.FieldValue = secondUrl;
                fieldValues.List.Add(dhpz2);
            }
            else if (fieldDic.ContainsKey("第二刀(20mm/s)"))
            {
                FlowsValuesDTO dhpz2 = fieldDic["第二刀(20mm/s)"].Clone();
                dhpz2.GroupOperateId = fieldValues.GroupOperateId;
                dhpz2.FieldValue = secondUrl;
                fieldValues.List.Add(dhpz2);
            }
        }

        public static void AddMaximumCollapseAngleImage(Mat mat)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            string? imageUrl = HttpUtils.UploadImage(mat);
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

        public static void AddWearAmountAfterCircle(float wearAmount, int count)
        {
            //if (!GlobalParams.OnlineMES || _tuple is null) return;
            //FieldValuesDTO fieldValues = _tuple.Item1;
            //Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            //if (fieldDic.TryGetValue("真圆后总磨损量", out FlowsValuesDTO? value))
            //{
            //    FlowsValuesDTO dto = value.Clone();
            //    dto.GroupOperateId = fieldValues.GroupOperateId;
            //    dto.FieldValue = Math.Round(wearAmountAfterCircle * 1000).ToString();
            //    fieldValues.List.Add(dto);
            //}
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            if (AfterCircleMslValues is null)
            {
                AfterCircleMslValues = fieldDic["真圆后总磨损量"].Clone();
                AfterCircleMslValues.FieldValue = "0";
                AfterCircleMslValues.GroupOperateId = fieldValues.GroupOperateId;
                fieldValues.List.Add(AfterCircleMslValues);
            }
            int wearAmountInt = (int)Math.Round(wearAmount * 1000);
            GroupCode++;
            AfterCircleMslValues.FieldValue = Math.Round(float.Parse(AfterCircleMslValues.FieldValue) + wearAmountInt).ToString();
            if (MslValues is not null)
                MslValues.FieldValue = Math.Round(float.Parse(MslValues.FieldValue) + wearAmountInt).ToString();
            FlowsValuesDTO mosunliang = AfterCircleMslValues.Children[0].FieldList[0].ToFlowsValuesDTO();
            mosunliang.ParentId = AfterCircleMslValues.FieldId;
            mosunliang.GroupOperateId = fieldValues.GroupOperateId;
            mosunliang.FieldValue = wearAmountInt.ToString();
            mosunliang.GroupCode = "1";
            fieldValues.List.Add(mosunliang);
            FlowsValuesDTO mdsl = AfterCircleMslValues.Children[0].FieldList[1].ToFlowsValuesDTO();
            mdsl.ParentId = AfterCircleMslValues.FieldId;
            mdsl.GroupOperateId = fieldValues.GroupOperateId;
            mdsl.FieldValue = count.ToString();
            mdsl.GroupCode = "1";
            fieldValues.List.Add(mdsl);
            FlowsValuesDTO ddmsl = AfterCircleMslValues.Children[0].FieldList[2].ToFlowsValuesDTO();
            ddmsl.ParentId = AfterCircleMslValues.FieldId;
            ddmsl.GroupOperateId = fieldValues.GroupOperateId;
            ddmsl.FieldValue = Math.Round((float)wearAmountInt / count, 2).ToString();
            ddmsl.GroupCode = "1";
            fieldValues.List.Add(ddmsl);
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

        public static async Task ScrapAsync(Mat mat)
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            UpdateOperateStatusDTO updateOperateStatus = new UpdateOperateStatusDTO
            {
                CoGroupCode = fieldValues.CoGroupCode,
                OperateId = fieldValues.GroupOperateId,
                UserId = fieldValues.UserId
            };
            string? imageUrl = HttpUtils.UploadImage(mat);
            updateOperateStatus.UpdateOperateStatusHubVoList.Add(new UpdateOperateStatusHubDTO
            {
                BadCoGroupCode = fieldValues.CoGroupCode,
                UserId = fieldValues.UserId,
                HubNumber = _lunguId,
                QualityImgUrl = imageUrl ?? string.Empty,
                ScrapFlag = "1",
                BadCoGroupName = "切割车间",
                ScrapYxId = "f7ef7df636624a209ef69acf77bbfec2",
                ScrapYxName = "蛇形",
                ScrapYxValue = "F",
                ScrapTypeName = "生产报废",
                ScrapTypeValue = "C",
                QualityResult = "2",
            });
            await HttpUtils.UpdateGroupStatusAsync(updateOperateStatus);
        }

        /// <summary>
        /// 合格
        /// </summary>
        /// <returns></returns>
        public static async Task QualifiedAsync()
        {
            if (!GlobalParams.OnlineMES || _tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            UpdateOperateStatusDTO updateOperateStatus = new UpdateOperateStatusDTO
            {
                CoGroupCode = fieldValues.CoGroupCode,
                OperateId = fieldValues.GroupOperateId,
                UserId = fieldValues.UserId
            };
            updateOperateStatus.UpdateOperateStatusHubVoList.Add(new UpdateOperateStatusHubDTO
            {
                BadCoGroupCode = fieldValues.CoGroupCode,
                UserId = fieldValues.UserId,
                HubNumber = _lunguId,
                QualityResult = "0",
                ScrapFlag = "0",
            });
            await HttpUtils.UpdateGroupStatusAsync(updateOperateStatus);
        }
    }
}
