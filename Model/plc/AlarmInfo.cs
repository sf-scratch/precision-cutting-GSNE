using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.plc
{
    public class AlarmInfo
    {
        public string Address { get; set; }
        public AlarmLevel Level { get; set; }
        public string Message { get; set; }
    }
}
