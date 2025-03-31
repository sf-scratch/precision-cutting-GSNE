using Emgu.CV;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace 精密切割系统.Helpers
{
    internal class QueryUtils
    {

        //获取URl中传递的参数
        public static Dictionary<string, string> getQuery(Page page)
        {
            var url = page.NavigationService.CurrentSource.OriginalString;
            string[] query = url.Split("?");
            Dictionary<string, string> dictQuery = new Dictionary<string, string>();
            if (query.Length>1)
            {
              string  queryData = query[1];
                string[] data = queryData.Split("&");
                foreach (var q in data)
                {
                    var keyValue = q.Split('=');
                    dictQuery[keyValue[0]] = keyValue[1];
                }
            }
            return dictQuery;
        }
        
        public static string GetValueFromQueryParams(Page page, string key)
        {
            Dictionary<string, string> queryParams = getQuery(page);
    
            if (queryParams.TryGetValue(key, out string value))
            {
                return value; // 如果找到了值，则返回
            }
            else
            {
                return null; // 如果没有找到，返回 null
            }
        }

    }
}
