using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.DTOs;
using 精密切割系统.Utils;

namespace 精密切割系统.HttpClients
{
    public class HttpRestClient
    {
        public static readonly string DevPreName = "n2baseDev";
        public static readonly string DevIP = "192.168.1.252";
        public static readonly string ProdPreName = "n2baseProd";
        public static readonly string ProdIP = "192.168.1.251";

        public static string PreUrl = $"{ProdPreName}-osb";
        public static string BaseUrl = $"http://{ProdIP}:8280/";
        public static string PDABaseUrl = $"http://{ProdIP}:89/";
        public static string UploadFileUrl = $"{BaseUrl}{PreUrl}/http/interface/uploadFile";

        private static readonly Lazy<HttpRestClient> _lazy = new(() => new HttpRestClient());

        private HttpRestClient() { }

        public static HttpRestClient Instance
        {
            get
            {
                return _lazy.Value;
            }
        }

        public static void UpdateDev()
        {
            PreUrl = $"{DevPreName}-osb";
            BaseUrl = $"http://{DevIP}:8280/";
            PDABaseUrl = $"http://{DevIP}:89/";
            UploadFileUrl = $"{BaseUrl}{PreUrl}/http/interface/uploadFile";
        }

        public static void UpdateProd()
        {
            PreUrl = $"{ProdPreName}-osb";
            BaseUrl = $"http://{ProdIP}:8280/";
            PDABaseUrl = $"http://{ProdIP}:89/";
            UploadFileUrl = $"{BaseUrl}{PreUrl}/http/interface/uploadFile";
        }

        /// <summary>
        /// 请求
        /// </summary>
        /// <param name="apiRequest"></param>
        /// <returns></returns>
        public async Task<ApiResponse?> ExecuteAsync(ApiRequest apiRequest)
        {
            RestRequest request = new RestRequest();
            //request.AddHeader("Authorization", "Bearer " + "");
            if (apiRequest.Method != Method.Get)
            {
                request.AddHeader("Content-Type", apiRequest.ContentType);
                string paramStr = string.Empty;
                if (apiRequest.Parameters is string parameters)
                {
                    paramStr = parameters;
                }
                else if (apiRequest.Parameters != null)
                {
                    paramStr = JsonConvert.SerializeObject(apiRequest.Parameters);
                }
                Tools.LogDebug(paramStr);
                request.AddJsonBody(paramStr);
                //request.AddParameter("param", JsonConvert.SerializeObject(apiRequest.Parameters), ParameterType.RequestBody);
            }
            RestClient client = new RestClient($"{BaseUrl}{apiRequest.Route}");
            RestResponse response = await client.ExecuteAsync(request, apiRequest.Method);
            ApiResponse? res;
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                try
                {
                    return JsonConvert.DeserializeObject<ApiResponse?>(response.Content);
                }
                catch (Exception ex)
                {
                    return null; // 反序列化失败，返回null
                }
            }
            else
            {
                res = new ApiResponse() { Code = -90, Msg = "服务器忙" };
            }
            return res;
        }

        public async Task<ApiResponse?> ExecutePdaAsync(ApiRequest apiRequest)
        {
            RestRequest request = new RestRequest();
            if (apiRequest.Method != Method.Get)
            {
                request.AddHeader("Content-Type", apiRequest.ContentType);
                string paramStr = string.Empty;
                if (apiRequest.Parameters != null)
                {
                    paramStr = JsonConvert.SerializeObject(apiRequest.Parameters);
                }
                request.AddJsonBody(paramStr);
            }
            RestClient client = new RestClient($"{PDABaseUrl}{apiRequest.Route}");
            RestResponse response = await client.ExecuteAsync(request, apiRequest.Method);
            ApiResponse? res;
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                try
                {
                    return JsonConvert.DeserializeObject<ApiResponse?>(response.Content);
                }
                catch (Exception)
                {
                    return null; // 反序列化失败，返回null
                }
            }
            else
            {
                res = new ApiResponse() { Code = -90, Msg = "服务器忙" };
            }
            return res;
        }
    }
}
