using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Model.sqlite;
using 精密切割系统.Utils;
using 精密切割系统.ViewModel;

namespace 精密切割系统.Model.bunkering
{
    internal class BunkeringHandler
    {
        private static string bunkeringNum = "0";

        /// <summary>
        /// 添加加油记录 1分钟看一次计数是否有变化
        /// </summary>
        public static void AddBunkeringRecord()
        {
        }
    }
}