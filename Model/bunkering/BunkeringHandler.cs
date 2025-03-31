using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Model.plc;
using 精密切割系统.Model.sqlite;
using 精密切割系统.Utils;
using 精密切割系统.ViewModel;

namespace 精密切割系统.Model.bunkering
{
    internal class BunkeringHandler
    {
        static string bunkeringNum = "0";
        /// <summary>
        /// 添加加油记录 1分钟看一次计数是否有变化
        /// </summary>
        public static void AddBunkeringRecord()
        {
            Task.Run(() => {
                bunkeringNum = PlcControl.plc.GetPlcValueString(DeviceKey.bunkeringNumKey);
                while (true)
                {
                    string tempStr = PlcControl.plc.GetPlcValueString(DeviceKey.bunkeringNumKey);
                    if (tempStr != null && !bunkeringNum.Equals(tempStr))
                    {
                        if (!bunkeringNum.Equals("0"))
                        {// 记录一条加油记录
                            BunkeringRecordModel model = new BunkeringRecordModel();
                            model.CreateTime = DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss");
                            SqlHelper.Add(model);
                            Tools.LogInfo($"轨道加油一次 {model.CreateTime}");
                        }
                        bunkeringNum = tempStr;
                    }
                    Thread.Sleep(60000);
                }
            });
        }
    }
}
