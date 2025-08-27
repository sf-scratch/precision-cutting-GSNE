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
        public static readonly string DevPreName = "n2baseDev-osb";
        public static readonly string DevIP = "192.168.1.252";
        public static readonly string ProdPreName = "n2baseProd-osb";
        public static readonly string ProdIP = "192.168.1.251";

        public static string BaseUrl = $"http://{ProdIP}:8280/{ProdPreName}";

        public static string UploadFileUrl => $"{BaseUrl}/http/interface/uploadFile";

        public static string DeleteOperateValueByIdUrl => $"{BaseUrl}/http/interface/deleteOperateValueById";

        private static readonly Lazy<HttpRestClient> _lazy = new(() => new HttpRestClient());

        private HttpRestClient() { }

        public static HttpRestClient Instance
        {
            get => _lazy.Value;
        }

        public static void UpdateDev()
        {
            BaseUrl = $"http://{DevIP}:8280/{DevPreName}";
        }

        public static void UpdateProd()
        {
            BaseUrl = $"http://{ProdIP}:8280/{ProdPreName}";
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
    }
}
