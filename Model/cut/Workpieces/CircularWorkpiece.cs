using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Helpers;

namespace 精密切割系统.Model.cut.Workpieces
{
    public class CircularWorkpiece
    {
        private readonly PointF ThetaCenterPoint;
        private readonly float WorkpieceRadius;
        private float _currentY;

        public CircularWorkpiece(PointF thetaCenterPoint, float workpieceRadius, float currentY)
        {
            ThetaCenterPoint = thetaCenterPoint;
            WorkpieceRadius = workpieceRadius;
            _currentY = currentY;
        }

        public bool CheckCutDistance(CutDirection cutDirection, float cutSize)
        {
            //切割距离达到最终位置
            if (cutDirection == CutDirection.Backward)
            {
                if (ThetaCenterPoint.Y + WorkpieceRadius - _currentY + cutSize >= WorkpieceRadius * 2 - 5)
                {
                    return false;
                }
                _currentY += cutSize;
            }
            if (cutDirection == CutDirection.Forward)
            {
                if (ThetaCenterPoint.Y + WorkpieceRadius - _currentY - cutSize <= 5)
                {
                    return false;
                }
                _currentY -= cutSize;
            }
            return true;
        }

        public void Reset(float currentY)
        {
            _currentY = currentY;
        }

        public float CalculateCutY()
        {
            return _currentY;
        }
    }
}
