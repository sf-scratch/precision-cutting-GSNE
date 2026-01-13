using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut
{
    public class ChData
    {
        public string ChName { get; set; }
        public float ThetaDeg { get; set; }
        public float AfterCalibrationThetaDeg { get; set; } = 0;
        public float AfterCalibrationYPosition { get; set; } = 0;

        public ChData(string chName, float thetaDeg)
        {
            ChName = chName;
            ThetaDeg = thetaDeg;
        }
    }
}