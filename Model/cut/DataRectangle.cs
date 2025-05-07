using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut
{
    public class DataRectangleF
    {
        private float _x;
        private float _y;
        private float _width;
        private float _height;

        /// <summary>
        /// 创建矩形
        /// </summary>
        /// <param name="x">左侧X坐标</param>
        /// <param name="y">顶部Y坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        public DataRectangleF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// 左侧X坐标
        /// </summary>
        public float X
        {
            get => _x;
            set => _x = value;
        }

        /// <summary>
        /// 顶部Y坐标
        /// </summary>
        public float Y
        {
            get => _y;
            set => _y = value;
        }

        /// <summary>
        /// 矩形宽度
        /// </summary>
        public float Width
        {
            get => _width;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "宽度必须大于0");
                _width = value;
            }
        }

        /// <summary>
        /// 矩形高度
        /// </summary>
        public float Height
        {
            get => _height;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "高度必须大于0");
                _height = value;
            }
        }

        /// <summary>
        /// 左侧边界坐标
        /// </summary>
        public float Left => X;

        /// <summary>
        /// 右侧边界坐标
        /// </summary>
        public float Right => X + Width;

        /// <summary>
        /// 顶部边界坐标
        /// </summary>
        public float Top => Y;

        /// <summary>
        /// 底部边界坐标
        /// </summary>
        public float Bottom => Y + Height;

        /// <summary>
        /// 矩形面积
        /// </summary>
        public float Area => Width * Height;

        /// <summary>
        /// 矩形周长
        /// </summary>
        public float Perimeter => 2 * (Width + Height);

        /// <summary>
        /// 中心点X坐标
        /// </summary>
        public float CenterX => X + Width / 2f;

        /// <summary>
        /// 中心点Y坐标
        /// </summary>
        public float CenterY => Y + Height / 2f;

        /// <summary>
        /// 检查点是否在矩形内
        /// </summary>
        public bool Contains(float x, float y)
        {
            return x >= Left && x <= Right &&
                   y >= Top && y <= Bottom;
        }

        /// <summary>
        /// 移动矩形位置
        /// </summary>
        public DataRectangleF Translate(float offsetX, float offsetY)
        {
            X += offsetX;
            Y += offsetY;
            return this;
        }

        /// <summary>
        /// 调整矩形大小
        /// </summary>
        public void Resize(float newWidth, float newHeight)
        {
            Width = newWidth;
            Height = newHeight;
        }

        /// <summary>
        /// 从中心点设置矩形位置
        /// </summary>
        public void SetCenter(float centerX, float centerY)
        {
            X = centerX - Width / 2f;
            Y = centerY - Height / 2f;
        }

        public override string ToString()
        {
            return $"Rectangle(Left={Left}, Top={Top}, Right={Right}, Bottom={Bottom}, Width={Width}, Height={Height})";
        }

        public DataRectangleF Clone()
        {
            return new DataRectangleF(X, Y, Width, Height);
        }
    }
}
