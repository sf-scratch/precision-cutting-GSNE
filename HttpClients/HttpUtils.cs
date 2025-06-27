using HslCommunication;
using Newtonsoft.Json;
using OpenCvSharp;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
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
        public static async Task<HttpUtilsResult<LunguInfoDTO>> GetLunguInfoAsync(string lunguId)
        {
            ApiRequest request = new ApiRequest
            {
                Method = RestSharp.Method.Post,
                Route = $"{HttpRestClient.PreUrl}/http/interface/getHubInfoById?hubNumber={lunguId}"
            };
            ApiResponse? response = await HttpRestClient.Instance.ExecuteAsync(request);
            if (response == null)
            {
                return HttpUtilsResult<LunguInfoDTO>.Fail("获取轮毂信息失败！");
            }
            if (response.IsSuccess())
            {
                try
                {
                    var data = JsonConvert.DeserializeObject<LunguInfoDTO>(response.Data.ToString());
                    return HttpUtilsResult<LunguInfoDTO>.Success(data);
                }
                catch (Exception ex)
                {
                    return HttpUtilsResult<LunguInfoDTO>.Fail(ex.Message);
                }
            }
            return HttpUtilsResult<LunguInfoDTO>.Fail(response.Msg);
        }

        /// <summary>
        /// 获取轮毂蚀刻数据
        /// </summary>
        /// <param name="lunguId"></param>
        /// <returns></returns>
        public static async Task<HttpUtilsResult<LunguSksjDTO>> GetLunguSksjAsync(string lunguId)
        {
            ApiRequest request = new ApiRequest
            {
                Method = RestSharp.Method.Get,
                Route = $"{HttpRestClient.PreUrl}/http/interface/getLunguSksj?lungu={lunguId}"
            };
            ApiResponse? response = await HttpRestClient.Instance.ExecuteAsync(request);
            if (response == null)
            {
                return HttpUtilsResult<LunguSksjDTO>.Fail("获取轮毂蚀刻数据失败！");
            }
            if (response.IsSuccess())
            {
                try
                {
                    var data = JsonConvert.DeserializeObject<LunguSksjDTO>(response.Data.ToString());
                    return HttpUtilsResult<LunguSksjDTO>.Success(data);
                }
                catch (Exception ex)
                {
                    return HttpUtilsResult<LunguSksjDTO>.Fail(ex.Message);
                }
            }
            return HttpUtilsResult<LunguSksjDTO>.Fail(response.Msg);
        }

        public static async Task<HttpUtilsResult<string>> InsertFlowValuesAsync(FieldValuesDTO fieldValues)
        {
            ApiRequest request = new ApiRequest
            {
                Method = RestSharp.Method.Post,
                Route = $"{HttpRestClient.PreUrl}/http/interface/insertFlowValues",
                Parameters = fieldValues
            };
            ApiResponse? response = await HttpRestClient.Instance.ExecuteAsync(request);
            if (response == null)
            {
                return HttpUtilsResult<string>.Fail("InsertFlowValues失败！");
            }
            if (response.IsSuccess())
            {
                try
                {
                    var data = JsonConvert.DeserializeObject<InsertFlowValuesResponseDTO>(response.Data.ToString()).GroupOperateId;
                    return HttpUtilsResult<string>.Success(data);
                }
                catch (Exception ex)
                {
                    return HttpUtilsResult<string>.Fail(ex.Message);
                }
            }
            return HttpUtilsResult<string>.Fail(response.Msg);
        }

        public static async Task<HttpUtilsResult<string>> UpdateGroupStatusAsync(UpdateOperateStatusDTO updateOperateStatus)
        {
            ApiRequest request = new ApiRequest
            {
                Method = RestSharp.Method.Post,
                Route = $"{HttpRestClient.PreUrl}/http/interface/pda/sop/updateGroupStatus",
                Parameters = updateOperateStatus
            };
            ApiResponse? response = await HttpRestClient.Instance.ExecuteAsync(request);
            if (response == null)
            {
                return HttpUtilsResult<string>.Fail("通信异常！");
            }
            if (response.IsSuccess())
            {
                try
                {
                    var data = response.Data.ToString();
                    return HttpUtilsResult<string>.Success(data);
                }
                catch (Exception ex)
                {
                    return HttpUtilsResult<string>.Fail(ex.Message);
                }
            }
            return HttpUtilsResult<string>.Fail(response.Msg);
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
                Route = $"{HttpRestClient.PreUrl}/http/interface/queryFlowSettingById",
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
                try
                {
                    return JsonConvert.DeserializeObject<List<FlowSettingDTO>>(response.Data.ToString());
                }
                catch (Exception)
                {
                    return null; // 反序列化失败，返回null
                }
            }
            Tools.LogDebug(response.Msg);
            return null;
        }

        /// <summary>
        /// 获取QgParams
        /// </summary>
        /// <param name="hubNumber">轮毂号 </param>
        /// <param name="sydrcd">剩余刀刃长度</param>
        /// <param name="dbbjdx">单边崩角大小</param>
        /// <returns></returns>
        public static async Task<QgParamsDTO?> GetQgParamsByHub(string hubNumber, float? sydrcd = null, float? dbbjdx = null, float? zyhddmsl = null)
        {
            ApiRequest request = new ApiRequest
            {
                Method = RestSharp.Method.Post,
                Route = $"{HttpRestClient.PreUrl}/http/interface/getQgParamsByHub",
                Parameters = new JsonObject
                {
                    [nameof(hubNumber)] = hubNumber,
                    [nameof(sydrcd)] = sydrcd,
                    [nameof(dbbjdx)] = dbbjdx,
                    [nameof(zyhddmsl)] = zyhddmsl,
                }.ToString()
            };
            Tools.LogDebug($"GetQgParamsByHub:   {request.Parameters.ToString()}");
            ApiResponse? response = await HttpRestClient.Instance.ExecuteAsync(request);
            if (response == null)
            {
                Tools.LogDebug("InsertFlowValues失败！");
                return null;
            }
            if (response.IsSuccess())
            {
                try
                {
                    return JsonConvert.DeserializeObject<QgParamsDTO>(response.Data.ToString());
                }
                catch (Exception ex)
                {
                    return null; // 反序列化失败，返回null
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
