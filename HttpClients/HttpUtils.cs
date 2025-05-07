using HslCommunication;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.DTOs;
using 精密切割系统.Utils;

namespace 精密切割系统.HttpClients
{
    public class HttpUtils
    {
        private static Tuple<FieldValuesDTO, Dictionary<string, FlowsValuesDTO>> _tuple;

        public static async Task SendMeasureHeightToMES(float afterHeightMeasurementZ)
        {

        }

        public static async Task SendSharpenDataToMES()
        {

        }

        /// <summary>
        /// 获取轮毂信息
        /// </summary>
        /// <param name="lunguId"></param>
        /// <returns></returns>
        public static async Task<LunguInfoDTO?> GetLunguInfoAsync(string lunguId)
        {
            ApiRequest request = new ApiRequest
            {
                Method = RestSharp.Method.Post,
                Route = $"n2baseDev-osb/http/interface/getHubInfoById?hubNumber={lunguId}"
            };
            ApiResponse? response = await HttpRestClient.Instance.ExecuteAsync(request);
            if (response == null)
            {
                Tools.LogDebug("获取轮毂信息失败！");
                return null;
            }
            LunguInfoDTO? lunguInfo = null;
            if (response.IsSuccess())
            {
                lunguInfo = JsonConvert.DeserializeObject<LunguInfoDTO>(response.Data.ToString());
            }
            else
            {
                Tools.LogDebug(response.Msg);
            }
            return lunguInfo;
        }

        /// <summary>
        /// 获取轮毂蚀刻数据
        /// </summary>
        /// <param name="lunguId"></param>
        /// <returns></returns>
        public static async Task<LunguSksjDTO?> GetLunguSksjAsync(string lunguId)
        {
            ApiRequest request = new ApiRequest
            {
                Method = RestSharp.Method.Get,
                Route = $"n2baseDev-osb/http/interface/getLunguSksj?lungu={lunguId}"
            };
            ApiResponse? response = await HttpRestClient.Instance.ExecuteAsync(request);
            if (response == null)
            {
                Tools.LogDebug("获取轮毂蚀刻数据失败！");
                return null;
            }
            LunguSksjDTO? lunguInfo = null;
            if (response.IsSuccess())
            {
                lunguInfo = JsonConvert.DeserializeObject<LunguSksjDTO>(response.Data.ToString());
            }
            else
            {
                Tools.LogDebug(response.Msg);
            }
            return lunguInfo;
        }

        public static async Task<string?> InsertFlowValuesAsync(FieldValuesDTO fieldValues)
        {
            ApiRequest request = new ApiRequest
            {
                Method = RestSharp.Method.Post,
                Route = $"n2baseDev-osb/http/interface/insertFlowValues",
                Parameters = fieldValues
            };
            ApiResponse? response = await HttpRestClient.Instance.ExecuteAsync(request);
            if (response == null)
            {
                Tools.LogDebug("InsertFlowValues失败！");
                return null;
            }
            if (response.IsSuccess())
            {
                InsertFlowValuesResponseDTO? flowValuesResponseDTO = JsonConvert.DeserializeObject<InsertFlowValuesResponseDTO>(response.Data.ToString());
                if (flowValuesResponseDTO != null)
                {
                    return flowValuesResponseDTO.GroupOperateId;
                }
            }
            Tools.LogDebug(response.Msg);
            return null;
        }

        public static async Task<List<FlowSettingDTO>?> QueryFlowSettingByIdAsync(string? businessId = null)
        {
            var data = new
            {
                businessId,
                coGroupCode = "GX-QG-001",
                flowId = "5079dbcb49044012afcba2b036b55869"
            };
            ApiRequest request = new ApiRequest
            {
                Method = RestSharp.Method.Post,
                Route = $"n2baseDev-osb/http/interface/queryFlowSettingById",
                Parameters = data
            };
            ApiResponse? response = await HttpRestClient.Instance.ExecuteAsync(request);
            if (response == null)
            {
                Tools.LogDebug("InsertFlowValues失败！");
                return null;
            }
            if (response.IsSuccess())
            {
                List<FlowSettingDTO> fieldValues = JsonConvert.DeserializeObject<List<FlowSettingDTO>>(response.Data.ToString());
                if (fieldValues != null)
                {
                    return fieldValues;
                }
            }
            Tools.LogDebug(response.Msg);
            return null;
        }

        public static string? UploadImage(string imagePath)
        {
            // 1. 验证文件存在
            if (!File.Exists(imagePath))
                throw new FileNotFoundException("图片文件不存在", imagePath);

            // 2. 创建 RestClient（基地址为Blob服务URL）
            var client = new RestClient(HttpRestClient.UploadFileUrl);

            // 3. 创建请求（POST方法）
            var request = new RestRequest();
            request.AddHeader("Content-Type", "multipart/form-data");

            // 4. 添加文件流
            byte[] fileBytes = File.ReadAllBytes(imagePath);
            request.AddFile("file", fileBytes, Path.GetFileName(imagePath), "image/jpeg");

            // 5. 执行请求
            RestResponse response = client.Execute(request, Method.Post);

            if (response.IsSuccessful)
            {
                if (response.Content == null)
                {
                    return null;
                }
                ApiResponse? apiResponse = JsonConvert.DeserializeObject<ApiResponse?>(response.Content);
                if (apiResponse != null)
                {
                    return apiResponse.Data.ToString();
                }
            }
            return null;
        }

        public static async Task<bool> ComputerPracticeAsync()
        {
            List<FlowSettingDTO>? allFieldValues = await HttpUtils.QueryFlowSettingByIdAsync();
            if (allFieldValues == null)
            {
                return false;
            }
            Dictionary<string, FlowsValuesDTO> flowsDic = allFieldValues.Select(x => x.ToFlowsValuesDTO()).ToDictionary(x => x.FieldLabel);
            FieldValuesDTO fieldValues = GetFieldValuesDTO(flowsDic, "QG-01", "T24111102B0005");
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

        public static bool AddSharpen(float num1, int count1, float num2, int count2)
        {
            if (_tuple is null) return false;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;
            FlowsValuesDTO mslValues = fieldDic["总磨损量(um)"].Clone();
            mslValues.FieldValue = (num1 + num2).ToString();
            mslValues.GroupOperateId = fieldValues.GroupOperateId;
            FlowsValuesDTO mosunliang = mslValues.Children[0].FieldList[0].ToFlowsValuesDTO();
            mosunliang.ParentId = mslValues.FieldId;
            mosunliang.GroupOperateId = fieldValues.GroupOperateId;
            mosunliang.FieldValue = num1.ToString();
            FlowsValuesDTO mdsl = mslValues.Children[0].FieldList[1].ToFlowsValuesDTO();
            mdsl.ParentId = mslValues.FieldId;
            mdsl.GroupOperateId = fieldValues.GroupOperateId;
            mdsl.FieldValue = count1.ToString();
            FlowsValuesDTO mosunliang2 = mslValues.Children[0].FieldList[0].ToFlowsValuesDTO();
            mosunliang2.ParentId = mslValues.FieldId;
            mosunliang2.GroupOperateId = fieldValues.GroupOperateId;
            mosunliang2.FieldValue = num2.ToString();
            mosunliang2.GroupCode = "2";
            FlowsValuesDTO mdsl2 = mslValues.Children[0].FieldList[1].ToFlowsValuesDTO();
            mdsl2.ParentId = mslValues.FieldId;
            mdsl2.GroupOperateId = fieldValues.GroupOperateId;
            mdsl2.FieldValue = count2.ToString();
            mdsl2.GroupCode = "2";
            fieldValues.List.Add(mslValues);
            fieldValues.List.Add(mosunliang);
            fieldValues.List.Add(mdsl);
            fieldValues.List.Add(mosunliang2);
            fieldValues.List.Add(mdsl2);
            return true;
        }

        public static bool AddToolMarks(string toolMarkWidth, string toolMarkActualWidth, string firstToolMarkWidth,
            string firstToolMarkImagePath, string secondToolMarkImagePath, string maximumCollapseAngle, string maxCutSpeed)
        {
            if (_tuple is null) return false;
            FieldValuesDTO fieldValues = _tuple.Item1;
            Dictionary<string, FlowsValuesDTO> fieldDic = _tuple.Item2;

            List<FlowsValuesDTO> flows = new List<FlowsValuesDTO>();

            // 刀痕宽度(um)
            FlowsValuesDTO dhkd = fieldDic["刀痕宽度(um)"].Clone();
            dhkd.FieldValue = toolMarkWidth;
            flows.Add(dhkd);

            // 刀痕实际宽度
            FlowsValuesDTO dhsjkd = fieldDic["刀痕实际宽度"].Clone();
            dhsjkd.FieldValue = toolMarkActualWidth;
            flows.Add(dhsjkd);

            // 第一刀刀痕宽度
            FlowsValuesDTO dyddhkd = fieldDic["第一刀刀痕宽度"].Clone();
            dyddhkd.FieldValue = firstToolMarkWidth;
            flows.Add(dyddhkd);

            // 上传第一刀图片
            string? firstUrl = HttpUtils.UploadImage(firstToolMarkImagePath);
            if (firstUrl == null)
            {
                return false;
            }

            // 第一刀图片
            FlowsValuesDTO dhpz = fieldDic["第一刀（10mm/s）"].Clone();
            dhpz.FieldValue = firstUrl;
            flows.Add(dhpz);

            // 上传第二刀图片
            string? secondUrl = HttpUtils.UploadImage(secondToolMarkImagePath);
            if (secondUrl == null)
            {
                return false;
            }

            // 第二刀图片
            FlowsValuesDTO dhpz2 = fieldDic["第二刀(20mm/s)"].Clone();
            dhpz2.FieldValue = secondUrl;
            flows.Add(dhpz2);

            // 崩角最大值
            FlowsValuesDTO qgbjzdz = fieldDic["崩角最大值"].Clone();
            qgbjzdz.FieldValue = maximumCollapseAngle;
            flows.Add(qgbjzdz);

            // 最高切割速度
            FlowsValuesDTO zgqgsd = fieldDic["最高切割速度(mm/s)"].Clone();
            zgqgsd.FieldValue = maxCutSpeed;
            flows.Add(zgqgsd);

            // 设置GroupOperateId
            foreach (var flow in flows)
            {
                flow.GroupOperateId = fieldValues.GroupOperateId;
            }

            fieldValues.List.AddRange(flows);
            return true;
        }

        public static async Task UpdateFlowValues()
        {
            if (_tuple is null) return;
            FieldValuesDTO fieldValues = _tuple.Item1;
            await InsertFlowValuesAsync(fieldValues);
        }
    }
}
