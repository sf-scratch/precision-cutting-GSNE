using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.HttpClients
{
    public class HttpRestClient
    {
        private readonly string BaseUrl = "http://192.168.1.252:8280/";
        private static readonly Lazy<HttpRestClient> _lazy = new(() => new HttpRestClient());

        private HttpRestClient() { }

        public static HttpRestClient Instance
        {
            get
            {
                return _lazy.Value;
            }
        }

        /// <summary>
        /// 请求
        /// </summary>
        /// <param name="apiRequest"></param>
        /// <returns></returns>
        public ApiResponse? Execute(ApiRequest apiRequest)
        {
            RestRequest request = new RestRequest();
            //request.AddHeader("Authorization", "Bearer " + "");
            if (apiRequest.Method != Method.Get)
            {
                request.AddHeader("Content-Type", apiRequest.ContentType);
                string paramStr = string.Empty;
                if (apiRequest.Parameters != null)
                {
                    paramStr = JsonConvert.SerializeObject(apiRequest.Parameters);
                }
                request.AddJsonBody(paramStr);
                //request.AddParameter("param", JsonConvert.SerializeObject(apiRequest.Parameters), ParameterType.RequestBody);
            }
            RestClient client = new RestClient($"{BaseUrl}{apiRequest.Route}");
            RestResponse response = client.Execute(request, apiRequest.Method);
            ApiResponse? res;
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                if (response.Content == null)
                {
                    return null;
                }
                res = JsonConvert.DeserializeObject<ApiResponse?>(response.Content);
            }
            else
            {
                res = new ApiResponse() { Code = -90, Msg = "服务器忙" };
            }
            return res;
        }
    }
}
