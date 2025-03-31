using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace 精密切割系统.Helpers
{
    internal class LogCleaner
    {

        private static Timer _timer;

        public static void StartLogCleanup(string logDirectory, int daysThreshold, TimeSpan interval)
        {
            _timer = new Timer(_ => CleanOldLogs(logDirectory, daysThreshold), null, TimeSpan.Zero, interval);
            Console.WriteLine("日志清理定时任务已启动。");
        }

        /// <summary>
        /// 清理指定目录中超过指定天数的日志文件
        /// </summary>
        /// <param name="logDirectory">日志目录路径</param>
        /// <param name="daysThreshold">保留的日志天数</param>
        public static void CleanOldLogs(string logDirectory, int daysThreshold)
        {
            try
            {
                // 检查日志目录是否存在
                if (!Directory.Exists(logDirectory))
                {
                    Console.WriteLine($"日志目录不存在: {logDirectory}");
                    return;
                }

                // 定义日志文件命名格式的正则表达式
                var logFileRegex = new Regex(@"log\.log\.(\d{4}-\d{2}-\d{2})(?:\.\d+)?$");

                // 获取所有日志文件
                var files = Directory.GetFiles(logDirectory, "log.log.*");

                // 当前日期的清理阈值
                DateTime thresholdDate = DateTime.Now.AddDays(-daysThreshold);

                foreach (var file in files)
                {
                    try
                    {
                        // 提取文件名
                        string fileName = Path.GetFileName(file);

                        // 使用正则解析文件名中的日期
                        var match = logFileRegex.Match(fileName);
                        if (match.Success)
                        {
                            // 提取日期部分并解析为 DateTime
                            string datePart = match.Groups[1].Value;
                            if (DateTime.TryParse(datePart, out DateTime fileDate))
                            {
                                // 判断文件日期是否早于阈值日期
                                if (fileDate < thresholdDate)
                                {
                                    File.Delete(file);
                                    Console.WriteLine($"已删除日志文件: {file}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"无法解析文件日期: {fileName}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"文件名不符合日志格式: {fileName}");
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Console.WriteLine($"无权限访问文件: {file}, 错误: {ex.Message}");
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"无法删除文件: {file}, 错误: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理日志文件时发生错误: {ex.Message}");
            }
        }
    }
}
