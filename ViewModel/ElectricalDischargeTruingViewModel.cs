using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.database.db.modle;

namespace 精密切割系统.ViewModel
{
    internal class ElectricalDischargeTruingViewModel : INotifyPropertyChanged
    {
        private ElectricalDischargeTruingModel _model;

        public ElectricalDischargeTruingViewModel()
        {
            _model = new ElectricalDischargeTruingModel();
        }

        // X轴初始位置
        public string XInitLocation
        {
            get { return _model.XInitLocation; }
            set
            {
                if (_model.XInitLocation != value)
                {
                    _model.XInitLocation = value;
                    OnPropertyChanged("XInitLocation");
                }
            }
        }

        // Y轴刀片前端位置
        public string YBladeFrontLocation
        {
            get { return _model.YBladeFrontLocation; }
            set
            {
                if (_model.YBladeFrontLocation != value)
                {
                    _model.YBladeFrontLocation = value;
                    OnPropertyChanged("YBladeFrontLocation");
                }
            }
        }

        // Y轴刀片后端位置
        public string YBladeBackLocation
        {
            get { return _model.YBladeBackLocation; }
            set
            {
                /*if (_model.YBladeBackLocation != value)
                {*/
                    _model.YBladeBackLocation = value;
                    OnPropertyChanged("YBladeBackLocation");
                /*}*/
            }
        }

        // Z轴设定位置
        public string ZSetPosition
        {
            get { return _model.ZSetPosition; }
            set
            {
                if (_model.ZSetPosition != value)
                {
                    _model.ZSetPosition = value;
                    OnPropertyChanged("ZSetPosition");
                }
            }
        }

        // 刀片角度
        public string BladeAngle
        {
            get { return _model.BladeAngle; }
            set
            {
                if (_model.BladeAngle != value)
                {
                    _model.BladeAngle = value;
                    OnPropertyChanged("BladeAngle");
                }
            }
        }

        // X0轴基准位置
        public string X0BasePosition
        {
            get { return _model.X0BasePosition; }
            set
            {
                if (_model.X0BasePosition != value)
                {
                    _model.X0BasePosition = value;
                    OnPropertyChanged();
                }
            }
        }

        // Y0轴基准位置
        public string Y0BasePosition
        {
            get { return _model.Y0BasePosition; }
            set
            {
                if (_model.Y0BasePosition != value)
                {
                    _model.Y0BasePosition = value;
                    OnPropertyChanged();
                }
            }
        }

        // Z0轴基准位置
        public string Z0BasePosition
        {
            get { return _model.Z0BasePosition; }
            set
            {
                if (_model.Z0BasePosition != value)
                {
                    _model.Z0BasePosition = value;
                    OnPropertyChanged();
                }
            }
        }

        // Z轴切割量
        public string ZCuttingAmount
        {
            get { return _model.ZCuttingAmount; }
            set
            {
                if (_model.ZCuttingAmount != value)
                {
                    _model.ZCuttingAmount = value;
                    OnPropertyChanged();
                }
            }
        }

        // 重复次数
        public int RepeatCount
        {
            get { return _model.RepeatCount; }
            set
            {
                if (_model.RepeatCount != value)
                {
                    _model.RepeatCount = value;
                    OnPropertyChanged();
                }
            }
        }

        // 电极极性设置
        public string ElectrodePolaritySetting
        {
            get { return _model.ElectrodePolaritySetting; }
            set
            {
                if (_model.ElectrodePolaritySetting != value)
                {
                    _model.ElectrodePolaritySetting = value;
                    OnPropertyChanged();
                }
            }
        }

        // 刀片修正速度
        public string BladeCorrectionSpeed
        {
            get { return _model.BladeCorrectionSpeed; }
            set
            {
                if (_model.BladeCorrectionSpeed != value)
                {
                    _model.BladeCorrectionSpeed = value;
                    OnPropertyChanged();
                }
            }
        }

        // 主轴速度
        public string SpindleSpeed
        {
            get { return _model.SpindleSpeed; }
            set
            {
                if (_model.SpindleSpeed != value)
                {
                    _model.SpindleSpeed = value;
                    OnPropertyChanged();
                }
            }
        }

        // 刀片厚度
        public string BladeThickness
        {
            get { return _model.BladeThickness; }
            set
            {
                if (_model.BladeThickness != value)
                {
                    _model.BladeThickness = value;
                    OnPropertyChanged();
                }
            }
        }

        // 电极厚度
        public string ElectrodeThickness
        {
            get { return _model.ElectrodeThickness; }
            set
            {
                if (_model.ElectrodeThickness != value)
                {
                    _model.ElectrodeThickness = value;
                    OnPropertyChanged();
                }
            }
        }

        // Y轴偏移量
        public string YOffsetAmount
        {
            get { return _model.YOffsetAmount; }
            set
            {
                if (_model.YOffsetAmount != value)
                {
                    _model.YOffsetAmount = value;
                    OnPropertyChanged();
                }
            }
        }

        // Y轴浮动量
        public string YFloatingAmount
        {
            get { return _model.YFloatingAmount; }
            set
            {
                if (_model.YFloatingAmount != value)
                {
                    _model.YFloatingAmount = value;
                    OnPropertyChanged();
                }
            }
        }

        // Z轴极限位置
        public string ZLimitPosition
        {
            get { return _model.ZLimitPosition; }
            set
            {
                if (_model.ZLimitPosition != value)
                {
                    _model.ZLimitPosition = value;
                    OnPropertyChanged();
                }
            }
        }

        // 电极角度
        public string ElectrodeAngle
        {
            get { return _model.ElectrodeAngle; }
            set
            {
                if (_model.ElectrodeAngle != value)
                {
                    _model.ElectrodeAngle = value;
                    OnPropertyChanged("ElectrodeAngle");
                }
            }
        }

        // 当前修刀进度
        public int CurrentRepairNum
        {
            get { return _model.CurrentRepairNum; }
            set
            {
                if (_model.CurrentRepairNum != value)
                {
                    _model.CurrentRepairNum = value;
                    OnPropertyChanged("CurrentRepairNum");
                }
            }
        }
        public int AllDressersNum
        {
            get { return _model.AllDressersNum; }
            set
            {
                if (_model.AllDressersNum != value)
                {
                    _model.AllDressersNum = value;
                    OnPropertyChanged("AllDressersNum");
                }
            }
        }
        public int ClearDressersNum
        {
            get { return _model.ClearDressersNum; }
            set
            {
                if (_model.ClearDressersNum != value)
                {
                    _model.ClearDressersNum = value;
                    OnPropertyChanged("ClearDressersNum");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
