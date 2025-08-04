using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut.Workpieces
{
    public class RectangleWorkpiece : IWorkpieces
    {
        private DataRectangleF _rectangle;
        private float _currentY;

        public float WorkThickness { get; set; }

        public float TapeThickness { get; set; }

        public RectangleWorkpiece(DataRectangleF rectangle, float currentY)
        {
            _rectangle = rectangle;
            _currentY = currentY;
        }

        public LineSegment? CalculateCuttingLine()
        {
            if (_currentY >= _rectangle.Top && _currentY <= _rectangle.Bottom)
            {
                return new LineSegment(_rectangle.X - _rectangle.Width, _currentY, _rectangle.X, _currentY);
            }
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
                if (_rectangle.Y - _currentY + cutSize >= - 5)
                {
                    return false;
                }
                _currentY += cutSize;
            }
            if (cutDirection == CutDirection.Forward)
            {
                if (_rectangle.Y + _rectangle.Height - _currentY - cutSize <= 5)
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
