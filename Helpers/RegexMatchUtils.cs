using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace 精密切割系统.Helpers
{
    public class RegexMatchUtils
    {
        public static int? ExtractChNumber(string input)
        {
            // 匹配特定模式 "Ch {数字}"
            Match match = Regex.Match(input, @"Ch\s*(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count == 2 && int.TryParse(match.Groups[1].Value, out int output))
            {
                // match.Groups[0] 是整个匹配 "Ch 1"
                // match.Groups[1] 是第一个捕获组 "1"
                return output;
            }
            // 如果找不到数字，返回null
            return null;
        }
    }
}