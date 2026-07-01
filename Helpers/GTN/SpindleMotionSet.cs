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
        public async Task SetSpindleSpeedAsync(int spindleRevValue)
        {
           string SpindelSpeed  = spindleRevValue.ToString();
            await Task.Delay(100);
        }
        /// <summary>
        /// 主轴启动
        /// </summary>
        public async Task<bool> StartSpindleAsync(int spindleRev,bool start)
        {
           
            return await Task.FromResult(true);
        }

        /// <summary>
        /// 转速检测
        /// </summary>
        public async Task<bool> WaitSpindleSpeedReachedAsync(int spindleRev, CancellationToken token)
        {

            return await Task.FromResult(true);
        }
    }

}
