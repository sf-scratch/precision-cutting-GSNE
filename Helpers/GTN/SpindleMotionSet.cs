using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Driver;

//运动配置
namespace 精密切割系统.Helpers.GTN
{
    public class SpindleMotionSet
    {
        public static SpindleMotionSet Instance { get; } = new SpindleMotionSet();

        /// <summary>
        /// 主轴启停 启动true 停止false
        /// </summary>
        public async Task<bool> StartSpindleAsync(int spindleRev, bool start)
        {
            return await Task.FromResult(true);
        }

        /// <summary>
        /// 转速到达检测
        /// </summary>
        public async Task<bool> WaitSpindleSpeedReachedAsync(int spindleRev, CancellationToken token)
        {
            //return await Task.FromResult(true);
            return true;
        }

        /// <summary>
        /// 主轴实时转速读取
        /// </summary>
        /// <returns></returns>
        public async Task<int> SpindleSpeedDisplayAsync()
        {
            return await Task.FromResult(0);
        }

        /// <summary>
        /// 报警复位
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ResetSpindleAlarmAsync()
        {
            return await Task.FromResult(true);
        }
    }
}