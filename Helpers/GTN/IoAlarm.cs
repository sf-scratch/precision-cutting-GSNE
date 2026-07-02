using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using 精密切割系统.Helpers.GTN;
using 精密切割系统.Model.common;
using static Org.BouncyCastle.Math.EC.ECCurve;
using static SQLite.SQLite3;

namespace 精密切割系统.Helpers.GTN
{
    public class IoAlarm
    {
        private static readonly Lazy<IoAlarm> _lazy = new(() => new IoAlarm());

        public static IoAlarm Instance
        {
            get { return _lazy.Value; }
        }

        /// <summary>
        /// 全局表示是否存在任意IO报警，外部可直接读取该属性判断是否有报警
        /// </summary>
        public bool HasAnyAlarm { get; set; }

        private InputConfig DiInput => InputConfig.Instance;

        // 存储当前激活的报警
        public List<string> ActiveAlarmList { get; private set; } = new List<string>();

        private Dictionary<string, bool> _dic;

        public async Task StartMonitorAlarmsAsync()
        {
            var diInput = DiInput;
            var allDiConfigs = diInput.AllDiConfigs;
            _dic = diInput.AllDiConfigs.ToDictionary(p => p.ByteAddress, p => false);
            while (true)
            {
                var di = await diInput.ReadAllDiAsync();
                foreach (var config in allDiConfigs)
                {
                    _dic[config.ByteAddress] = config.IsActiviteAlarm is not null && await config.IsActiviteAlarm.Invoke(di) && _dic.ContainsKey(config.ByteAddress);
                }
                await Task.Delay(20);
            }
        }

        /// <summary>
        /// 获取所有IO报警描述
        /// </summary>
        /// <returns>true=存在任意IO报警；false=全部正常</returns>
        public async Task<List<ActiveAlarmModel>> GetAllIoAlarmDescribeAsync()
        {
            //bool shield = true;// 这里可以设置一个开关，true=屏蔽报警，false=不屏蔽报警
            //if (shield)
            //{
            //    return [];
            //}
            List<ActiveAlarmModel> result = new List<ActiveAlarmModel>();
            var allDiConfigs = InputConfig.Instance.AllDiConfigs;
            foreach (var config in allDiConfigs)
            {
                if (_dic is not null && _dic.ContainsKey(config.ByteAddress) && _dic[config.ByteAddress])
                {
                    result.Add(new ActiveAlarmModel() { Message = config.AlarmMessage });
                }
            }
            HasAnyAlarm = result.Count != 0;//读完所有IO后，更新HasAnyAlarm状态
            return result;
        }

        #region 全部改为公共异步方法，外部可单独调用，返回bool（true=报警触发）

        /// <summary>急停按钮报警</summary>
        /// <param name="di">批量读取的DI状态，不传则内部自动读取一次</param>
        /// <returns>true=急停报警触发</returns>
        public async Task<bool> CheckEmgStopAlarmAsync(AllDiState di = null)
        {
            di ??= await DiInput.ReadAllDiAsync();
            bool isAlarm = di.EmgStop;

            // 可叠加延时、过滤、逻辑判断，示例：持续200ms才算报警
            // await Task.Delay(200);
            // AllDiState newDi = await DiInput.ReadAllDiAsync();
            // isAlarm = newDi.EmgStop;

            if (isAlarm)
            {
                string msg = "设备急停按钮按下，请复位急停！";

                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

        /// <summary>紧急抬起信号</summary>
        public async Task<bool> CheckEmergencyLiftAlarmAsync(AllDiState di = null)
        {
            di ??= await DiInput.ReadAllDiAsync();
            bool isAlarm = di.EmergencyLift;
            if (isAlarm)
            {
                string msg = "收到紧急抬起信号";

                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

        /// <summary>工件真空度不足</summary>
        public async Task<bool> CheckWorkpieceVacuumDetectAlarmAsync(AllDiState di = null)
        {
            di ??= await DiInput.ReadAllDiAsync();
            bool isAlarm = !di.WorkpieceVacuumDetect;
            if (isAlarm)
            {
                string msg = "工件真空度不足，请检查真空发生器！";

                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

        /// <summary>气浮气压异常</summary>
        public async Task<bool> CheckAirFloatPressureAlarmAsync(AllDiState di = null)
        {
            di ??= await DiInput.ReadAllDiAsync();
            bool isAlarm = !di.AirFloatPressureDetect;
            if (isAlarm)
            {
                string msg = "气浮气压异常，请检查气源！";

                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

        /// <summary>主轴抱闸压力不足</summary>
        public async Task<bool> CheckSpindleBrakePressureAlarmAsync(AllDiState di = null)
        {
            di ??= await DiInput.ReadAllDiAsync();
            bool isAlarm = !di.SpindleBrakePressure;
            if (isAlarm)
            {
                string msg = "主轴抱闸压力不足！";

                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

        /// <summary>主轴工作气压不足</summary>
        public async Task<bool> CheckSpindleAirPressureAlarmAsync(AllDiState di = null)
        {
            di ??= await DiInput.ReadAllDiAsync();
            bool isAlarm = !di.SpindleAirPressure;
            if (isAlarm)
            {
                string msg = "主轴冷却气压不足！";

                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

        /// <summary>切割水缺失</summary>
        public async Task<bool> CheckCutWaterDetectAlarmAsync(AllDiState di = null)
        {
            di ??= await DiInput.ReadAllDiAsync();
            bool isAlarm = !di.CutWaterDetectNO;
            if (isAlarm)
            {
                string msg = "切割水流量不足，无法切割！";

                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

        /// <summary>主轴冷却水异常</summary>
        public async Task<bool> CheckCoolWaterDetectAlarmAsync(AllDiState di = null)
        {
            di ??= await DiInput.ReadAllDiAsync();
            bool isAlarm = !di.CoolWaterDetectNO;
            if (isAlarm)
            {
                string msg = "主轴冷却水异常！";

                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

        /// <summary>测高继电器未闭合</summary>
        public async Task<bool> CheckHeightRelayAlarmAsync(AllDiState di = null)
        {
            di ??= await DiInput.ReadAllDiAsync();
            bool isAlarm = !di.HeightRelayCloseDetect;
            if (isAlarm)
            {
                string msg = "测高继电器未闭合，请检查测高模组！";

                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

        /// <summary>主轴电刷磨损</summary>
        public async Task<bool> CheckSpindleBrushAlarmAsync(AllDiState di = null)
        {
            di ??= await DiInput.ReadAllDiAsync();
            bool isAlarm = !di.SpindleBrushCheck;
            if (isAlarm)
            {
                string msg = "主轴电刷磨损，请更换电刷！";

                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

        /// <summary>相机安全门打开</summary>
        public async Task<bool> CheckCameraSafetyDoorAlarmAsync(AllDiState di = null)
        {
            di ??= await DiInput.ReadAllDiAsync();
            bool isAlarm = !di.CameraSafetyDoor;
            if (isAlarm)
            {
                string msg = "相机安全门未关闭，禁止运行！";

                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

        /// <summary>切割仓安全门打开</summary>
        public async Task<bool> CheckCutSafetyDoorAlarmAsync(AllDiState di = null)
        {
            di ??= await DiInput.ReadAllDiAsync();
            bool isAlarm = !di.CutSafetyDoor;
            if (isAlarm)
            {
                string msg = "切割仓安全门打开，请关门后再启动！";

                ActiveAlarmList.Add(msg);
            }
            return isAlarm;
        }

        #endregion 全部改为公共异步方法，外部可单独调用，返回bool（true=报警触发）
    }
}