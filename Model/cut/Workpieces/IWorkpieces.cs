using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut.Workpieces
{
    public interface IWorkpieces
    {
        LineSegment CalculateCuttingLine();

        float CalculateCutY();

        bool CheckCutDistance(CutDirection cutDirection, float cutSize);

        void UpdateToNextCutPosition(CutDirection cutDirection, float cutSize);

        void Reset(float currentY);

        public float WorkThickness { get; set; }

        public float TapeThickness { get; set; }
    }
}