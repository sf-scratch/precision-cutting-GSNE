using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Utils;
using 精密切割系统.ViewModel;

namespace 精密切割系统.Model.plc
{
    class AlarmItem
    {
        public AlarmItem()
        {
        }

        public AlarmItem(string t)
        {
            this.title = t;
        }
        public string code = "";
        // 报警标题
        public string title = "";
        // 报警优先级
        public int alarmPriority = 0;
        // 报警描述
        public string desc = "";
        // 报警发现时间
        public string startTime = "";
        // 是否手动忽略该报警
        public bool ignoreAlarm = false;
        // plc类型报警的plc变量
        public Tag? alarmTag = null;

        private KeyencePlc device = KeyencePlc.GetInstance();

        public void UpdateMsg(string msg)
        {
            desc = msg;
        }

        // dm1010  int16
        public Dictionary<string, string> deviceAlarm = new Dictionary<string, string>()
        {
            { "1", "请先解锁工件盘真空" },
            { "2", "请先解锁加工件真空" },
            { "3", "请先关闭安全门" },
            { "4", "请先关闭安全门2" },
            { "5", "紧急停止" },
            { "6", "非接触保护盖未打开" },
            { "7", "主轴冷却系统异常" },
            { "8", "电火花系统异常" },
            { "9", "主轴切削水异常" },
            { "10", "主轴冷却水异常" },
            { "11", "主轴变频器异常" },
            { "12", "真空错误" },
            { "13", "主轴清洁气源错误" },
            { "14", "安全门未关闭" },
            { "15", "安全门2未关闭" },
            { "16", "请先解锁安全门2" },
            { "17", "请先解锁安全门" },
            { "18", "z1不在安全位置" },
            { "19", "z2不在安全位置" },
            { "20", "X轴不在安全位置" },
            { "21", "丝杆润滑油液面过低" },
            { "22", "切屑水液面过低" },
            { "23", "冷却水液面过低" },
            { "24", "Z1位置设置超限" },
            { "25", "Z2位置设置超限" },
            { "26", "请先系统初始化" },
            { "27", "修刀电极状态错误" }
        };
        // dm1000  int16
        public Dictionary<string, string> axisAlarm = new Dictionary<string, string>()
        {
            { "1", "X轴伺服错误" },
            { "2", "X轴正限位" },
            { "3", "X轴负限位" },
            { "4", "Y轴伺服错误" },
            { "5", "Y轴正限位" },
            { "6", "Y轴负限位" },
            { "7", "Z1轴伺服错误" },
            { "8", "Z1轴正限位" },
            { "9", "Z1轴负限位" },
            { "10", "Z2轴伺服错误" },
            { "11", "Z2轴正限位" },
            { "12", "Z2轴负限位" },
            { "13", "θ轴伺服错误" },
            { "14", "θ轴正限位" },
            { "15", "θ轴负限位" },
            { "16", "M轴伺服错误" },
            { "17", "轴运行参数错误" }
        };

        // 手动确认报警
        public bool ClearAlarm()
        {
            bool res = false;
            if (alarmTag != null && alarmTag.addr == "DM1010")
            {
                Tag clearTag = PlcControl.allTags["确认硬件报警"];
                clearTag.writeValue = "1";
                res = PlcControl.plc.writeTag(clearTag);
            }
            else if (alarmTag != null && alarmTag.addr == "DM1000")
            {
                Tag clearTag = PlcControl.allTags["轴错误清除"];
                clearTag.writeValue = "1";
                res = PlcControl.plc.writeTag(clearTag);

                string axisName = null;
                int tempJogDirection = 1;
                switch (title)
                {
                    case "X轴正限位":
                        tempJogDirection = 1;
                        PlcControl.tagControl.Xaxis.StartRelative("20", "5.0", tempJogDirection);
                        Tools.WaitForValue(PlcControl.allTags[DeviceKey.curSpeedKey], "0");
                        axisName = DeviceKey.xName;
                        break;
                    case "X轴负限位":
                        tempJogDirection = 0;
                        PlcControl.tagControl.Xaxis.StartRelative("20", "5.0", tempJogDirection);
                        axisName = DeviceKey.xName;
                        break;
                    case "Y轴正限位":
                        tempJogDirection = 1;
                        PlcControl.tagControl.Yaxis.StartRelative("20", "5.0", tempJogDirection);
                        axisName = DeviceKey.yName;
                        break;

                    case "Y轴负限位":
                        tempJogDirection = 0;
                        PlcControl.tagControl.Yaxis.StartRelative("20", "5.0", tempJogDirection);
                        axisName = DeviceKey.yName;
                        break;

                    case "Z1轴正限位":
                        tempJogDirection = 1;
                        PlcControl.tagControl.Z1axis.StartRelative("20", "5.0", tempJogDirection);
                        axisName = DeviceKey.z1Name;
                        break;

                    case "Z1轴负限位":
                        tempJogDirection = 0;
                        PlcControl.tagControl.Z1axis.StartRelative("20", "5.0", tempJogDirection);
                        axisName = DeviceKey.z1Name;
                        break;

                    case "Z2轴正限位":
                        // 反方向相对运行
                        tempJogDirection = 1;
                        PlcControl.tagControl.Z2axis.StartRelative("20", "5.0", tempJogDirection);
                        axisName = DeviceKey.z2Name;
                        break;

                    case "Z2轴负限位":
                        tempJogDirection = 0;
                        PlcControl.tagControl.Z2axis.StartRelative("20", "5.0", tempJogDirection);
                        axisName = DeviceKey.z2Name;
                        break;

                    default:
                        axisName = null;
                        break;
                }
                if (axisName != null)
                {
                    Thread.Sleep(1000);
                    // 重新回原点
                    InitOriginPosition(axisName);
                }
            }
            MaterialSnackUtils.hideMessage();
            GlobalParams.globalRunFlag = false;
            return res;
        }
        /// <summary>
        /// 回原点
        /// </summary>
        /// <param name="axisName"></param>
        public void InitOriginPosition(string axisName)
        {
            if (DeviceKey.xName.Equals(axisName))
            {
                Tools.WaitForValue(PlcControl.allTags[DeviceKey.curSpeedKey], "0");
                PlcControl.tagControl.Xaxis.RunInitLocation();
            } else if (DeviceKey.yName.Equals(axisName))
            {
                Tools.WaitForValue(PlcControl.allTags[DeviceKey.yCurSpeedKey], "0");
                PlcControl.tagControl.Yaxis.RunInitLocation();
            }else if (DeviceKey.z1Name.Equals(axisName))
            {
                Tools.WaitForValue(PlcControl.allTags[DeviceKey.z1CurSpeedKey], "0");
                PlcControl.tagControl.Z1axis.RunInitLocation();
            }else if (DeviceKey.z2Name.Equals(axisName))
            {
                Tools.WaitForValue(PlcControl.allTags[DeviceKey.z2CurSpeedKey], "0");
                PlcControl.tagControl.Z2axis.RunInitLocation();
            }

        }

        // 重新比较函数，方便判断两个报警是否相同
        public override bool Equals(object? obj)
        {
            var other = obj as AlarmItem;
            return other != null && this.title == other.title;
        }

        public override int GetHashCode()
        {
            return title.GetHashCode();
        }
    }
}
