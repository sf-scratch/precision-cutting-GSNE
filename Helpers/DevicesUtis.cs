using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace 精密切割系统.Helpers
{
    internal class DevicesUtis
    {
        //判断是否为触摸设备
        public static bool IsTouchSupported()
        {
            return Tablet.TabletDevices.Count > 0;
        }
    }
}
