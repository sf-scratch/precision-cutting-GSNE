using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using 精密切割系统.Model.sqlite;
using 精密切割系统.ViewModel;

namespace 精密切割系统.Model.logs
{
    internal class RunLogsCommon
    {
        // 配置 JSON 序列化选项
        private static JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = false, // 格式化输出
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 允许中文字符
        };

        public static void LogEvent(string logType, List<RunLogsViewModel> logItems)
        {
            string nowDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            RunLogsModel runLogs = new RunLogsModel
            {
                RecordTime = nowDate,
                RecordType = logType
            };

            // 添加动态的日志项
            List<RunLogsViewModel> runLogsViewModels = new List<RunLogsViewModel>();
            runLogsViewModels.Add(new RunLogsViewModel("事件时间", nowDate)); // 固定添加事件时间
            runLogsViewModels.AddRange(logItems); // 添加动态的日志项

            runLogs.RecordContent = System.Text.Json.JsonSerializer.Serialize(runLogsViewModels, options);
            SqlHelper.Add(runLogs);
        }

        public static void LogEvent(string logType, params RunLogsViewModel[] logItems)
        {
            string nowDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            RunLogsModel runLogs = new RunLogsModel
            {
                RecordTime = nowDate,
                RecordType = logType
            };

            // 添加动态的日志项
            List<RunLogsViewModel> runLogsViewModels = new List<RunLogsViewModel>();
            runLogsViewModels.Add(new RunLogsViewModel("事件时间", nowDate)); // 固定添加事件时间
            runLogsViewModels.AddRange(logItems); // 添加动态的日志项

            runLogs.RecordContent = System.Text.Json.JsonSerializer.Serialize(runLogsViewModels, options);
            SqlHelper.Add(runLogs);
        }
    }
}