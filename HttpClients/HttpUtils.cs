using HslCommunication;
using Newtonsoft.Json;
using OpenCvSharp;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.DTOs;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Utils;

namespace 精密切割系统.HttpClients
{
    public class HttpUtils
    {
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

        public static string? UploadImage(Mat mat)
        {
            RestClient client = new RestClient(HttpRestClient.UploadFileUrl);

            RestRequest request = new RestRequest();
            request.AddHeader("Content-Type", "multipart/form-data");

            string imageSuffix = ".jpg";
            byte[] fileBytes = mat.ToBytes(imageSuffix);
            request.AddFile("file", fileBytes, $"{DateTime.Now.Ticks}{imageSuffix}", "image/jpeg");

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
    }
}
