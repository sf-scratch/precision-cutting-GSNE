using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using 精密切割系统.Helpers.GTN;
using 精密切割系统.Utils;

namespace 精密切割系统.Model.cut
{
    public class AxisOrigin
    {
        /// <summary>
        /// 整机一键回零流程
        /// </summary>
        public async Task<bool> AllAxisHomingAsync(CancellationToken token, int timeoutMs)
        {
            // 创建令牌
            using var timeoutCts = new CancellationTokenSource(timeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token);
            CancellationToken runToken = linkedCts.Token;

            try
            {
                // 同时发起Z1、Z2回零
                Task z1HomeTask = GsneMotion.Instance.Axis.StartHomingAsync(AxisType.Z1);
                Task z2HomeTask = GsneMotion.Instance.Axis.StartHomingAsync(AxisType.Z2);

                // 仅等待Z1回零完成
                while (!await GsneMotion.Instance.Axis.IsCompleteHomingAsync(AxisType.Z1))
                {
                    runToken.ThrowIfCancellationRequested();
                    await Task.Delay(100, runToken);
                }

                // Z1完成后启动 X/Y/Theta
                Task xHome = GsneMotion.Instance.Axis.StartHomingAsync(AxisType.X);
                Task yHome = GsneMotion.Instance.Axis.StartHomingAsync(AxisType.Y);
                Task thetaHome = GsneMotion.Instance.Axis.StartHomingAsync(AxisType.Theta);

                await Task.WhenAll(xHome, yHome, thetaHome);
                await Task.Delay(100, runToken);

                // 等待 X,Y,Z2,Theta 全部原点到位
                AxisType[] waitAxisList = { AxisType.X, AxisType.Y, AxisType.Z2, AxisType.Theta };
                bool allFinish;
                do
                {
                    allFinish = true;
                    foreach (var axis in waitAxisList)
                    {
                        if (!await GsneMotion.Instance.Axis.IsCompleteHomingAsync(axis))
                        {
                            allFinish = false;
                            break;
                        }
                    }
                    runToken.ThrowIfCancellationRequested();
                    if (!allFinish)
                        await Task.Delay(100, runToken);
                } while (!allFinish);

                return true;
            }
            catch (OperationCanceledException)
            {
                if (timeoutCts.IsCancellationRequested)
                    MaterialSnack("整机回零操作超时", SnackType.WARNING);
                else
                    MaterialSnack("整机回零被手动取消", SnackType.WARNING);
                return false;
            }
            catch (Exception ex)
            {
                MaterialSnack("初始化异常退出", SnackType.WARNING);
                return false;
            }
        }
    }
}