using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using 精密切割系统.Utils;

namespace 精密切割系统.Model.cut.Workpieces
{
    public class RectangleWorkpiece : IWorkpieces
    {
        private readonly DataPoint<float> _center;
        private DataRectangleF _rect;
        private float _currentY;

        public float WorkThickness { get; set; }

        public float TapeThickness { get; set; }

        public RectangleWorkpiece(DataPoint<float> thetaCenterPoint, float width, float height, float currentY)
        {
            _center = thetaCenterPoint;
            _rect = new DataRectangleF(thetaCenterPoint.X - (width / 2), thetaCenterPoint.Y - (height / 2), width, height);
            _currentY = currentY;
        }

        public LineSegment CalculateCuttingLine()
        {
            if (_currentY >= _rect.Top && _currentY <= _rect.Bottom)
            {
                return new LineSegment(_rect.X, _currentY, _rect.X + _rect.Width, _currentY);
            }
            float halfDiagonal = MathF.Sqrt(_rect.Width * _rect.Width + _rect.Height * _rect.Height) / 2;
            return new LineSegment(_center.X - halfDiagonal, _currentY, _center.X + halfDiagonal, _currentY);
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
                if (_rect.Y - _currentY + cutSize >= -5)
                {
                    return false;
                }
                _currentY -= cutSize;
            }
            if (cutDirection == CutDirection.Forward)
            {
                if (_rect.Y + _rect.Height - _currentY - cutSize <= 5)
                {
                    return false;
                }
                _currentY += cutSize;
            }
            Tools.LogDebug($"CheckCutDistance:    {_rect.X}  {_rect.Y}  {_rect.Width}  {_rect.Height}  {_currentY}  {cutDirection}  {cutSize}");
            return true;
        }

        public void Reset(float currentY)
        {
            _currentY = currentY;
        }
    }
}