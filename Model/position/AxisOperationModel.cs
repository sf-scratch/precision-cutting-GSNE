using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Driver;

namespace 精密切割系统.Model.position
{
    public class AxisOperationModel : BindableBase
    {
        private WeakReference<Action<AxisOperationModel, bool>> _onCheckedChangedWeakRef;

        public AxisOperationModel(Axis axisObject, Action<AxisOperationModel, bool> onCheckedChanged)
        {
            AxisObject = axisObject;
            if (onCheckedChanged != null)
            {
                _onCheckedChangedWeakRef = new WeakReference<Action<AxisOperationModel, bool>>(onCheckedChanged);
            }
        }

        private bool _isChecked;

        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (SetProperty(ref _isChecked, value))
                {
                    // 使用弱引用调用，如果目标已被GC回收，则不会执行
                    if (_onCheckedChangedWeakRef != null &&
                        _onCheckedChangedWeakRef.TryGetTarget(out var callback))
                    {
                        callback(this, value);
                    }
                    else
                    {
                        // 可选：清理无效的引用
                        _onCheckedChangedWeakRef = null;
                    }
                }
            }
        }

        // 提供方法手动清理回调
        public void ClearCallback()
        {
            _onCheckedChangedWeakRef = null;
        }

        public Axis AxisObject { get; set; }

        public string AxisName
        {
            get { return AxisObject.axisName; }
        }

        private string _axisSlowSpeed;

        public string AxisSlowSpeed
        {
            get { return _axisSlowSpeed; }
            set { SetProperty(ref _axisSlowSpeed, value); }
        }

        private string _axisSpeed;

        public string AxisSpeed
        {
            get { return _axisSpeed; }
            set { SetProperty(ref _axisSpeed, value); }
        }

        private string _relativeDistance;

        public string RelativeDistance
        {
            get { return _relativeDistance; }
            set { SetProperty(ref _relativeDistance, value); }
        }

        private string _curPosition;

        public string CurPosition
        {
            get { return _curPosition; }
            set { SetProperty(ref _curPosition, value); }
        }

        private bool _isReady;

        public bool IsReady
        {
            get { return _isReady; }
            set { SetProperty(ref _isReady, value); }
        }

        private string _unit;

        public string Unit
        {
            get { return _unit; }
            set { SetProperty(ref _unit, value); }
        }

        private string _sppedUnit;

        public string SpeedUnit
        {
            get { return _sppedUnit; }
            set { SetProperty(ref _sppedUnit, value); }
        }
    }
}