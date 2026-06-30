using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Helpers.GTN
{
    public class DiPoint
    {
        public int GlobalIndex { get; set; }

        public int BankIndex { get; set; }

        public int BitIndex { get; set; }

        public string Name { get; set; }

        public bool CurrentState { get; set; }

        public bool PreviousState { get; set; }

        public DateTime LastChangeTime { get; set; }

        public bool RisingEdge => !PreviousState && CurrentState;

        public bool FallingEdge => PreviousState && !CurrentState;

        public override string ToString()
        {
            return $"{Name} [DI{GlobalIndex}] = {CurrentState}";
        }
    }
}