using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using 精密切割系统.Helpers.GTN;

namespace 精密切割系统.Helpers.GTN
{
    public class IoAlarm
    {
        public static IoAlarm Instance { get; } = new IoAlarm();
        private readonly InputConfig _diInput = InputConfig.Instance;

        // 存储当前激活的报警
        public List<string> ActiveAlarmList { get; private set; } = new List<string>();

        /// <summary>
        /// 检测所有IO报警，一次性批量读取DI
        /// </summary>
        /// <returns>true=存在任意IO报警；false=全部正常</returns>
        public async Task<bool> ScanAllIoAlarmAsync()
        {   bool shield = true;// 这里可以设置一个开关，true=屏蔽报警，false=不屏蔽报警
            if (shield)
            {
                return false;
            }
            ActiveAlarmList.Clear();
            bool hasAnyAlarm = false;
            AllDiState diState = await _diInput.ReadAllDiAsync();

            // 逐个校验，存在报警则标记
            if (await CheckEmgStopAlarmAsync(diState)) hasAnyAlarm = true;
            if (await CheckEmergencyLiftAlarmAsync(diState)) hasAnyAlarm = true;
            if (await CheckWorkpieceVacuumDetectAlarmAsync(diState)) hasAnyAlarm = true;
            if (await CheckAirFloatPressureAlarmAsync(diState)) hasAnyAlarm = true;
            if (await CheckSpindleBrakePressureAlarmAsync(diState)) hasAnyAlarm = true;
            if (await CheckSpindleAirPressureAlarmAsync(diState)) hasAnyAlarm = true;
            if (await CheckCutWaterDetectAlarmAsync(diState)) hasAnyAlarm = true;
            if (await CheckCoolWaterDetectAlarmAsync(diState)) hasAnyAlarm = true;
            if (await CheckHeightRelayAlarmAsync(diState)) hasAnyAlarm = true;
            if (await CheckSpindleBrushAlarmAsync(diState)) hasAnyAlarm = true;
            if (await CheckCameraSafetyDoorAlarmAsync(diState)) hasAnyAlarm = true;
            if (await CheckCutSafetyDoorAlarmAsync(diState)) hasAnyAlarm = true;

            return hasAnyAlarm;
        }

        #region 全部改为公共异步方法，外部可单独调用，返回bool（true=报警触发）
        /// <summary>急停按钮报警</summary>
        /// <param name="di">批量读取的DI状态，不传则内部自动读取一次</param>
        /// <returns>true=急停报警触发</returns>
        public async Task<bool> CheckEmgStopAlarmAsync(AllDiState di = null)
        {
            di ??= await _diInput.ReadAllDiAsync();
            bool isAlarm = di.EmgStop;

            // 可叠加延时、过滤、逻辑判断，示例：持续200ms才算报警
            // await Task.Delay(200);
            // AllDiState newDi = await _diInput.ReadAllDiAsync();
            // isAlarm = newDi.EmgStop;

            if (isAlarm)
            {
                string msg = "设备急停按钮按下，请复位急停！";
                MaterialSnack(msg, SnackType.WARNING, 0, null);
                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }



        /// <summary>紧急抬起信号</summary>
        public async Task<bool> CheckEmergencyLiftAlarmAsync(AllDiState di = null)
        {
            di ??= await _diInput.ReadAllDiAsync();
            bool isAlarm = di.EmergencyLift;
            if (isAlarm)
            {
                string msg = "收到紧急抬起信号";
                MaterialSnack(msg, SnackType.WARNING, 0, null);
                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

   

        /// <summary>工件真空度不足</summary>
        public async Task<bool> CheckWorkpieceVacuumDetectAlarmAsync(AllDiState di = null)
        {
            di ??= await _diInput.ReadAllDiAsync();
            bool isAlarm = !di.WorkpieceVacuumDetect;
            if (isAlarm)
            {
                string msg = "工件真空度不足，请检查真空发生器！";
                MaterialSnack(msg, SnackType.WARNING, 0, null);
                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

        /// <summary>气浮气压异常</summary>
        public async Task<bool> CheckAirFloatPressureAlarmAsync(AllDiState di = null)
        {
            di ??= await _diInput.ReadAllDiAsync();
            bool isAlarm = !di.AirFloatPressureDetect;
            if (isAlarm)
            {
                string msg = "气浮气压异常，请检查气源！";
                MaterialSnack(msg, SnackType.WARNING, 0, null);
                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

        /// <summary>主轴抱闸压力不足</summary>
        public async Task<bool> CheckSpindleBrakePressureAlarmAsync(AllDiState di = null)
        {
            di ??= await _diInput.ReadAllDiAsync();
            bool isAlarm = !di.SpindleBrakePressure;
            if (isAlarm)
            {
                string msg = "主轴抱闸压力不足！";
                MaterialSnack(msg, SnackType.WARNING, 0, null);
                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

        /// <summary>主轴工作气压不足</summary>
        public async Task<bool> CheckSpindleAirPressureAlarmAsync(AllDiState di = null)
        {
            di ??= await _diInput.ReadAllDiAsync();
            bool isAlarm = !di.SpindleAirPressure;
            if (isAlarm)
            {
                string msg = "主轴冷却气压不足！";
                MaterialSnack(msg, SnackType.WARNING, 0, null);
                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

        /// <summary>切割水缺失</summary>
        public async Task<bool> CheckCutWaterDetectAlarmAsync(AllDiState di = null)
        {
            di ??= await _diInput.ReadAllDiAsync();
            bool isAlarm = !di.CutWaterDetectNO;
            if (isAlarm)
            {
                string msg = "切割水流量不足，无法切割！";
                MaterialSnack(msg, SnackType.WARNING, 0, null);
                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

        /// <summary>主轴冷却水异常</summary>
        public async Task<bool> CheckCoolWaterDetectAlarmAsync(AllDiState di = null)
        {
            di ??= await _diInput.ReadAllDiAsync();
            bool isAlarm = !di.CoolWaterDetectNO;
            if (isAlarm)
            {
                string msg = "主轴冷却水异常！";
                MaterialSnack(msg, SnackType.WARNING, 0, null);
                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

        /// <summary>测高继电器未闭合</summary>
        public async Task<bool> CheckHeightRelayAlarmAsync(AllDiState di = null)
        {
            di ??= await _diInput.ReadAllDiAsync();
            bool isAlarm = !di.HeightRelayCloseDetect;
            if (isAlarm)
            {
                string msg = "测高继电器未闭合，请检查测高模组！";
                MaterialSnack(msg, SnackType.WARNING, 0, null);
                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

        /// <summary>主轴电刷磨损</summary>
        public async Task<bool> CheckSpindleBrushAlarmAsync(AllDiState di = null)
        {
            di ??= await _diInput.ReadAllDiAsync();
            bool isAlarm = !di.SpindleBrushCheck;
            if (isAlarm)
            {
                string msg = "主轴电刷磨损，请更换电刷！";
                MaterialSnack(msg, SnackType.WARNING, 0, null);
                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

        /// <summary>相机安全门未关</summary>
        public async Task<bool> CheckCameraSafetyDoorAlarmAsync(AllDiState di = null)
        {
            di ??= await _diInput.ReadAllDiAsync();
            bool isAlarm = !di.CameraSafetyDoor;
            if (isAlarm)
            {
                string msg = "相机安全门未关闭，禁止运行！";
                MaterialSnack(msg, SnackType.WARNING, 0, null);
                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

        /// <summary>切割仓安全门打开</summary>
        public async Task<bool> CheckCutSafetyDoorAlarmAsync(AllDiState di = null)
        {
            di ??= await _diInput.ReadAllDiAsync();
            bool isAlarm = !di.CutSafetyDoor;
            if (isAlarm)
            {
                string msg = "切割仓安全门打开，请关门后再启动！";
                MaterialSnack(msg, SnackType.WARNING, 0, null);
                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }
        #endregion
        
    }
}