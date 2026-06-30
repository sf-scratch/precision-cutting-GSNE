using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Helpers.GTN
{
    public class AxisConfig
    {
        public AxisType Type { get; set; }

        public float PulsePerMM { get; set; }

        public float OffsetMM { get; set; }

        public short HomingModel {  get; set; }

        public double HighSpeed { get; set; }

        public double LowSpeed { get; set; }

    }
}
