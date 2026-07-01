using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Helpers.GTN;



namespace 精密切割系统.Model.cut
{/// <summary>
 /// 切割自动化执行程序
 /// </summary>
    public class CuttingAutomation
    {
        // ========== 切割参数==========
        public float TargetPositionX { get; set; } = 100f;      // X轴切割开始位置
        public float TargetPositionEndX { get; set; } = 100f;      // X轴切割结束位置
        public float TargetPositionY { get; set; } = 50f;       // Y轴切割开始位置
        public float TargetPositionZ1 { get; set; } = -10f;     // Z1切割位置（下降）
        public float TargetPositionZ1Safe { get; set; } = 0f;   // Z1安全/避让位置
        public float TargetPositionTheta { get; set; } = 0f;   // Theta切割位置

        public float SpeedX { get; set; } = 50f;                // X轴速度 mm/s
        public float SpeedY { get; set; } = 50f;                // Y轴速度 mm/s
        public float SpeedZ1 { get; set; } = 20f;               // Z1轴速度 mm/s
        public float SpeedTheta { get; set; } = 20f;            // Theta轴速度 mm/s
        public int SpindleRev { get; set; } = 10000;            //主轴转速转/分


        // 计数器
        public static CuttingAutomation Instance { get; } = new CuttingAutomation();
        public int CutCount { get; private set; } = 0;

        /// <summary>
        /// 执行完整切割流程
        /// </summary>
        public async Task<bool> ExecuteCuttingAsync(CuttingParameters parameters, CancellationToken token = default)
        {
            try
            {
                //"========== 切割流程开始 =========="
                // 步骤1: 接收切割参数
                if (!await ReceiveCuttingParametersAsync(parameters, token))
                {
                    return false;
                }
                
                //接收切割参数成功
                // 步骤2: Z1运动到0位置
                await GsneMotion.Instance.Axis.StartAbsoluteAsync(AxisType.Z1, 0, SpeedZ1, token);
                await WaitAxisStopAsync(AxisType.Z1);

                // 步骤3: X轴与Y轴同时运行到切割开始位置
                var xTask = GsneMotion.Instance.Axis.StartRelativeAsync(AxisType.X, TargetPositionX, SpeedX, token);
                var yTask = GsneMotion.Instance.Axis.StartAbsoluteAsync(AxisType.Y, TargetPositionY, SpeedY, token);
                var ThetaTask = GsneMotion.Instance.Axis.StartAbsoluteAsync(AxisType.Theta, TargetPositionTheta, SpeedTheta, token);
                await Task.WhenAll(xTask, yTask,ThetaTask);//同时启动
                await WaitAxisStopAsync(AxisType.X);
                await WaitAxisStopAsync(AxisType.Y);
                await WaitAxisStopAsync(AxisType.Theta);
                //X轴、Y轴到位
                // 步骤4: 启动主轴旋转
                if (!await SpindleMotionSet.Instance.StartSpindleAsync(SpindleRev,true))
                {
                    return false;
                }
                if (!await SpindleMotionSet.Instance.WaitSpindleSpeedReachedAsync(SpindleRev, token))
                {
                    //主轴转速未达到设定值
                    await StopSpindleAsync(token);
                    return false;
                }
                //主轴转速达到设定值

                // 步骤5: Z1下降到切割位置
                await GsneMotion.Instance.Axis.StartAbsoluteAsync(AxisType.Z1, TargetPositionZ1, SpeedZ1, token);
                await WaitAxisStopAsync(AxisType.Z1);

                // 步骤6: X运行到切割结束位置
                await GsneMotion.Instance.Axis.StartAbsoluteAsync(AxisType.X, TargetPositionEndX, SpeedX, token);
                await WaitAxisStopAsync(AxisType.X);

                // 步骤7: Z1上升到切割避让位
                await GsneMotion.Instance.Axis.StartAbsoluteAsync(AxisType.Z1, TargetPositionZ1Safe, SpeedZ1, token);
                await WaitAxisStopAsync(AxisType.Z1);

                // 步骤8: 计数+1
                CutCount++;

                // 步骤9: 结束流程
                await StopSpindleAsync(token);

                //========== 切割流程正常结束 ==========
                return true;
            }
            catch (OperationCanceledException)
            {
                //切割流程被取消
                await EmergencyStopAsync(token);
                throw;
            }
            catch (Exception ex)
            {
                //切割流程异常
                await EmergencyStopAsync(token);
                return false;
            }
        }

        /// <summary>
        /// 等待轴停止运动定位完成
        /// </summary>
        private async Task<bool> WaitAxisStopAsync(AxisType axis)
        {
            while (true)
            {
                bool arrived = await GsneMotion.Instance.Axis.IsPositionArrivedAsync(axis);
                if (arrived)
                {
                    return true;
                }
                // 间隔100ms轮询，降低运动卡通讯压力
                await Task.Delay(100);
            }
        }

        // ========== 预留接口==========

        /// <summary>
        /// 将外部传入参数覆盖当前自动化实例的运行参数
        /// </summary>
        /// <summary>接收外部传入切割参数，覆盖当前实例运行参数</summary>
        private async Task<bool> ReceiveCuttingParametersAsync(CuttingParameters parameters, CancellationToken token)
        {
            // 参数空校验
            if (parameters == null)
                return false;
            // 防止传入0或负数导致运动卡报错
            if (parameters.SpeedX <= 0 || parameters.SpeedY <= 0 || parameters.SpeedZ1 <= 0 || parameters.SpeedTheta <= 0)
                return false;
            // 位置参数赋值
            this.TargetPositionX = parameters.TargetPositionX;
            this.TargetPositionEndX = parameters.TargetPositionEndX;
            this.TargetPositionY = parameters.TargetPositionY;
            this.TargetPositionZ1 = parameters.TargetPositionZ1;
            this.TargetPositionZ1Safe = parameters.TargetPositionZ1Safe;
            this.TargetPositionTheta = parameters.TargetPositionTheta;

            // 速度参数赋值
            this.SpeedX = parameters.SpeedX;
            this.SpeedY = parameters.SpeedY;
            this.SpeedZ1 = parameters.SpeedZ1;
            this.SpeedTheta = parameters.SpeedTheta;
            this.SpindleRev = parameters.SpindleRev;


            return await Task.FromResult(true);
        }

        private async Task<bool> StopSpindleAsync(CancellationToken token)
        {
            // TODO: 实现主轴停止
            return await Task.FromResult(true);
        }
        /// <summary>
        /// 减速停止各轴
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task EmergencyStopAsync(CancellationToken token)
        {
            await Task.Run(() =>
            {

            }, token);
        }

        public void Clear()
        {
            CutCount = 0;
        }
    }


    
    public class CuttingParameters
    {
        /// <summary>X轴切割开始位置</summary>
        public float TargetPositionX { get; set; } = 100f;
        /// <summary>X轴切割结束位置</summary>
        public float TargetPositionEndX { get; set; } = 100f;
        /// <summary>Y轴切割开始位置</summary>
        public float TargetPositionY { get; set; } = 50f;
        /// <summary>Z1切割位置（下降）</summary>
        public float TargetPositionZ1 { get; set; } = -10f;
        /// <summary>Z1安全/避让位置</summary>
        public float TargetPositionZ1Safe { get; set; } = 0f;
        /// <summary>Theta切割位置</summary>
        public float TargetPositionTheta { get; set; } = 0f;

        // ========== 切割速度参数 ==========
        /// <summary>X轴速度 mm/s</summary>
        public float SpeedX { get; set; } = 50f;
        /// <summary>Y轴速度 mm/s</summary>
        public float SpeedY { get; set; } = 50f;
        /// <summary>Z1轴速度 mm/s</summary>
        public float SpeedZ1 { get; set; } = 20f;
        /// <summary>Theta轴速度 mm/s</summary>
        public float SpeedTheta { get; set; } = 20f;
        /// <summary>主轴转速</summary>
        public int SpindleRev { get; set; } = 10000; // 主轴转速
    }
}
