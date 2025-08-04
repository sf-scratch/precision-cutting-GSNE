using RestSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Helpers;

namespace 精密切割系统.Model.cut.Workpieces
{
    public class CircularWorkpiece : IWorkpieces
    {
        private readonly DataPoint<float> ThetaCenterPoint;
        private readonly float WorkpieceRadius;
        private float _currentY;

        public float WorkThickness { get; set; }

        public float TapeThickness { get; set; }


        public CircularWorkpiece(DataPoint<float> thetaCenterPoint, float workpieceRadius, float currentY)
        {
            ThetaCenterPoint = thetaCenterPoint;
            WorkpieceRadius = workpieceRadius;
            _currentY = currentY;
        }

        public LineSegment? CalculateCuttingLine()
        {
            float x0 = ThetaCenterPoint.X;
            float y0 = ThetaCenterPoint.Y;
            float r = WorkpieceRadius;
            float discriminant = r * r - (_currentY - y0) * (_currentY - y0);
            if (discriminant > 0)
            {
                // 两个交点（按 x 坐标从小到大排序）
                float sqrtDiscriminant = MathF.Sqrt(discriminant);
                float x1 = x0 - sqrtDiscriminant; // 较小的 x
                float x2 = x0 + sqrtDiscriminant; // 较大的 x
                return new LineSegment(x1, _currentY, x2, _currentY);
            }
            else if (discriminant == 0)
            {
                // 一个交点（相切）
                return new LineSegment(x0, _currentY, x0, _currentY);
            }
            // 无交点
            return null;
        }

        public float CalculateCutY()
        {
            return _currentY;
        }

        public bool CheckCutDistance(CutDirection cutDirection, float cutSize)
        {
            //切割距离达到最终位置
            if (cutDirection == CutDirection.Backward)
            {
                if (ThetaCenterPoint.Y - WorkpieceRadius - _currentY + cutSize >= - 5)
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
    }
}
