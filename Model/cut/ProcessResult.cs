using MathNet.Numerics.LinearAlgebra.Solvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut
{
    /// <summary>
    /// 执行切割或磨刀返回的结果
    /// </summary>
    public class ProcessResult
    {
        public static readonly ProcessResult FAIL = new(0, 0, false);

        private bool _isSuccess;
        /// <summary>
        /// 是否执行成功
        /// </summary>
        public bool IsSuccess
        {
            get { return _isSuccess; }
            set { _isSuccess = value; }
        }

        private int _remainTimes;
        /// <summary>
        /// 剩余次数
        /// </summary>
        public int RemainTimes
        {
            get { return _remainTimes; }
            set { _remainTimes = value; }
        }

        private float _currentY;
        /// <summary>
        /// 当前Y轴位置
        /// </summary>
        public float CurrentY
        {
            get { return _currentY; }
            set { _currentY = value; }
        }

        public ProcessResult(int remainTimes, float currentY, bool isSuccess = true)
        {
            _remainTimes = remainTimes;
            _currentY = currentY;
            _isSuccess = isSuccess;
        }
    }
}
