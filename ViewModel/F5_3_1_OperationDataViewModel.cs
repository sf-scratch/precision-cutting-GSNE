using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.database.db.modle;
using 精密切割系统.Helpers;
using 精密切割系统.Utils;

namespace 精密切割系统.ViewModel
{
    internal class F5_3_1_OperationDataViewModel : BindableBase
    {
        public OperationParametersModel operationParameter { get; set; }

        public F5_3_1_OperationDataViewModel()
        {
            operationParameter = new OperationParametersModel();
        }

        public string XScanSpeed
        {
            get { return operationParameter.XScanSpeed; }
            set
            {
                if (operationParameter.XScanSpeed != value)
                {
                    operationParameter.XScanSpeed = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string XSscanDistance
        {
            get { return operationParameter.XSscanDistance; }
            set
            {
                if (operationParameter.XSscanDistance != value)
                {
                    operationParameter.XSscanDistance = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string YScanSpeed
        {
            get { return operationParameter.YScanSpeed; }
            set
            {
                if (operationParameter.YScanSpeed != value)
                {
                    operationParameter.YScanSpeed = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string YSscanDistance
        {
            get { return operationParameter.YSscanDistance; }
            set
            {
                if (operationParameter.YSscanDistance != value)
                {
                    operationParameter.YSscanDistance = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string ZScanSpeed
        {
            get { return operationParameter.ZScanSpeed; }
            set
            {
                if (operationParameter.ZScanSpeed != value)
                {
                    operationParameter.ZScanSpeed = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string ZSscanDistance
        {
            get { return operationParameter.ZSscanDistance; }
            set
            {
                if (operationParameter.ZSscanDistance != value)
                {
                    operationParameter.ZSscanDistance = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string Z2ScanSpeed
        {
            get { return operationParameter.Z2ScanSpeed; }
            set
            {
                if (operationParameter.Z2ScanSpeed != value)
                {
                    operationParameter.Z2ScanSpeed = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string Z2SscanDistance
        {
            get { return operationParameter.Z2SscanDistance; }
            set
            {
                if (operationParameter.Z2SscanDistance != value)
                {
                    operationParameter.Z2SscanDistance = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string RScanSpeed
        {
            get { return operationParameter.RScanSpeed; }
            set
            {
                if (operationParameter.RScanSpeed != value)
                {
                    operationParameter.RScanSpeed = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string RSscanDistance
        {
            get { return operationParameter.RSscanDistance; }
            set
            {
                if (operationParameter.RSscanDistance != value)
                {
                    operationParameter.RSscanDistance = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string MoveLowTime
        {
            get { return operationParameter.MoveLowTime; }
            set
            {
                if (operationParameter.MoveLowTime != value)
                {
                    operationParameter.MoveLowTime = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string MoveHighTime
        {
            get { return operationParameter.MoveHighTime; }
            set
            {
                if (operationParameter.MoveHighTime != value)
                {
                    operationParameter.MoveHighTime = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string XScreenIndex
        {
            get { return operationParameter.XScreenIndex; }
            set
            {
                if (operationParameter.XScreenIndex != value)
                {
                    operationParameter.XScreenIndex = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string YScreenIndex
        {
            get { return operationParameter.YScreenIndex; }
            set
            {
                if (operationParameter.YScreenIndex != value)
                {
                    operationParameter.YScreenIndex = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string EscapeRate
        {
            get { return operationParameter.EscapeRate; }
            set
            {
                if (operationParameter.EscapeRate != value)
                {
                    operationParameter.EscapeRate = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string ExtraEscapeRate
        {
            get { return operationParameter.ExtraEscapeRate; }
            set
            {
                if (operationParameter.ExtraEscapeRate != value)
                {
                    operationParameter.ExtraEscapeRate = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string MStopElectrical
        {
            get { return operationParameter.MStopElectrical; }
            set
            {
                if (operationParameter.MStopElectrical != value)
                {
                    operationParameter.MStopElectrical = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string MStopTime
        {
            get { return operationParameter.MStopTime; }
            set
            {
                if (operationParameter.MStopTime != value)
                {
                    operationParameter.MStopTime = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string ZStopElectrical
        {
            get { return operationParameter.ZStopElectrical; }
            set
            {
                if (operationParameter.ZStopElectrical != value)
                {
                    operationParameter.ZStopElectrical = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string ZStopTime
        {
            get { return operationParameter.ZStopTime; }
            set
            {
                if (operationParameter.ZStopTime != value)
                {
                    operationParameter.ZStopTime = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string ZStopAfterSeq
        {
            get { return operationParameter.ZStopAfterSeq; }
            set
            {
                if (operationParameter.ZStopAfterSeq != value)
                {
                    operationParameter.ZStopAfterSeq = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string XStartClearance
        {
            get { return operationParameter.XStartClearance; }
            set
            {
                if (operationParameter.XStartClearance != value)
                {
                    operationParameter.XStartClearance = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string XEndClearance
        {
            get { return operationParameter.XEndClearance; }
            set
            {
                if (operationParameter.XEndClearance != value)
                {
                    operationParameter.XEndClearance = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string YClearance
        {
            get { return operationParameter.YClearance; }
            set
            {
                if (operationParameter.YClearance != value)
                {
                    operationParameter.YClearance = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string AutoFocusCheckLimit
        {
            get { return operationParameter.AutoFocusCheckLimit; }
            set
            {
                if (operationParameter.AutoFocusCheckLimit != value)
                {
                    operationParameter.AutoFocusCheckLimit = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string AirCurtainStroke
        {
            get { return operationParameter.AirCurtainStroke; }
            set
            {
                if (operationParameter.AirCurtainStroke != value)
                {
                    operationParameter.AirCurtainStroke = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string VaccumWorkLowerLimit
        {
            get { return operationParameter.VaccumWorkLowerLimit; }
            set
            {
                if (operationParameter.VaccumWorkLowerLimit != value)
                {
                    operationParameter.VaccumWorkLowerLimit = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string VaccumPumpLowerLimitOther
        {
            get { return operationParameter.VaccumPumpLowerLimitOther; }
            set
            {
                if (operationParameter.VaccumPumpLowerLimitOther != value)
                {
                    operationParameter.VaccumPumpLowerLimitOther = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string LimitDuringCutting
        {
            get { return operationParameter.LimitDuringCutting; }
            set
            {
                if (operationParameter.LimitDuringCutting != value)
                {
                    operationParameter.LimitDuringCutting = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string VaccumPumpLowerLimit
        {
            get { return operationParameter.VaccumPumpLowerLimit; }
            set
            {
                if (operationParameter.VaccumPumpLowerLimit != value)
                {
                    operationParameter.VaccumPumpLowerLimit = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string xPanelJogDistance
        {
            get { return operationParameter.xPanelJogDistance; }
            set
            {
                if (operationParameter.xPanelJogDistance != value)
                {
                    operationParameter.xPanelJogDistance = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string yPanelJogDistance
        {
            get { return operationParameter.yPanelJogDistance; }
            set
            {
                if (operationParameter.yPanelJogDistance != value)
                {
                    operationParameter.yPanelJogDistance = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string zPanelJogDistance
        {
            get { return operationParameter.zPanelJogDistance; }
            set
            {
                if (operationParameter.zPanelJogDistance != value)
                {
                    operationParameter.zPanelJogDistance = value;
                    RaisePropertyChanged();
                }
            }
        }

        public int zAxisCompNum
        {
            get { return operationParameter.zAxisCompNum; }
            set
            {
                if (operationParameter.zAxisCompNum != value)
                {
                    operationParameter.zAxisCompNum = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string zAxisCompValue
        {
            get { return operationParameter.zAxisCompValue; }
            set
            {
                if (operationParameter.zAxisCompValue != value)
                {
                    operationParameter.zAxisCompValue = value;
                    RaisePropertyChanged();
                }
            }
        }

        public int cutXAxisBackSpeed
        {
            get { return operationParameter.cutXAxisBackSpeed; }
            set
            {
                if (operationParameter.cutXAxisBackSpeed != value)
                {
                    operationParameter.cutXAxisBackSpeed = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _positiveLimitPositionX;

        public string PositiveLimitPositionX
        {
            get { return _positiveLimitPositionX; }
            set { SetProperty(ref _positiveLimitPositionX, value); }
        }

        private string _negativeLimitPositionX;

        public string NegativeLimitPositionX
        {
            get { return _negativeLimitPositionX; }
            set { SetProperty(ref _negativeLimitPositionX, value); }
        }

        private string _positiveLimitPositionY;

        public string PositiveLimitPositionY
        {
            get { return _positiveLimitPositionY; }
            set { SetProperty(ref _positiveLimitPositionY, value); }
        }

        private string _negativeLimitPositionY;

        public string NegativeLimitPositionY
        {
            get { return _negativeLimitPositionY; }
            set { SetProperty(ref _negativeLimitPositionY, value); }
        }

        private string _positiveLimitPositionZ1;

        public string PositiveLimitPositionZ1
        {
            get { return _positiveLimitPositionZ1; }
            set { SetProperty(ref _positiveLimitPositionZ1, value); }
        }

        private string _negativeLimitPositionZ1;

        public string NegativeLimitPositionZ1
        {
            get { return _negativeLimitPositionZ1; }
            set { SetProperty(ref _negativeLimitPositionZ1, value); }
        }

        private string _positiveLimitPositionZ2;

        public string PositiveLimitPositionZ2
        {
            get { return _positiveLimitPositionZ2; }
            set { SetProperty(ref _positiveLimitPositionZ2, value); }
        }

        private string _negativeLimitPositionZ2;

        public string NegativeLimitPositionZ2
        {
            get { return _negativeLimitPositionZ2; }
            set { SetProperty(ref _negativeLimitPositionZ2, value); }
        }

        private string _positiveLimitPositionTheta;

        public string PositiveLimitPositionTheta
        {
            get { return _positiveLimitPositionTheta; }
            set { SetProperty(ref _positiveLimitPositionTheta, value); }
        }

        private string _negativeLimitPositionTheta;

        public string NegativeLimitPositionTheta
        {
            get { return _negativeLimitPositionTheta; }
            set { SetProperty(ref _negativeLimitPositionTheta, value); }
        }

        public string OriginCompensationX
        {
            get { return operationParameter.OriginCompensationX; }
            set
            {
                operationParameter.OriginCompensationX = value;
                RaisePropertyChanged();
            }
        }

        public string OriginCompensationY
        {
            get { return operationParameter.OriginCompensationY; }
            set
            {
                operationParameter.OriginCompensationY = value;
                RaisePropertyChanged();
            }
        }

        public string OriginCompensationZ1
        {
            get { return operationParameter.OriginCompensationZ1; }
            set
            {
                operationParameter.OriginCompensationZ1 = value;
                RaisePropertyChanged();
            }
        }

        public string OriginCompensationZ2
        {
            get { return operationParameter.OriginCompensationZ2; }
            set
            {
                operationParameter.OriginCompensationZ2 = value;
                RaisePropertyChanged();
            }
        }

        public string OriginCompensationTheta
        {
            get { return operationParameter.OriginCompensationTheta; }
            set
            {
                operationParameter.OriginCompensationTheta = value;
                RaisePropertyChanged();
            }
        }

        public bool IsAutoShutOffWaterWhenCuttingCompleted
        {
            get { return operationParameter.IsAutoShutOffWaterWhenCuttingCompleted; }
            set
            {
                operationParameter.IsAutoShutOffWaterWhenCuttingCompleted = value;
                RaisePropertyChanged();
            }
        }

        public bool IsAutoShutOffWaterWhenCloseVacuum
        {
            get { return operationParameter.IsAutoShutOffWaterWhenCloseVacuum; }
            set
            {
                operationParameter.IsAutoShutOffWaterWhenCloseVacuum = value;
                RaisePropertyChanged();
            }
        }

        public bool IsAutoShutOffWaterWhenEnterCalibration
        {
            get { return operationParameter.IsAutoShutOffWaterWhenEnterCalibration; }
            set
            {
                operationParameter.IsAutoShutOffWaterWhenEnterCalibration = value;
                RaisePropertyChanged();
            }
        }

        public bool IsManuallyTurnOffWater
        {
            get { return operationParameter.IsManuallyTurnOffWater; }
            set
            {
                operationParameter.IsManuallyTurnOffWater = value;
                RaisePropertyChanged();
            }
        }

        public bool IsExitCutClearManualCompensation
        {
            get { return operationParameter.IsExitCutClearManualCompensation; }
            set
            {
                operationParameter.IsExitCutClearManualCompensation = value;
                RaisePropertyChanged();
            }
        }

        public bool IsUpdateParamClearManualCompensation
        {
            get { return operationParameter.IsUpdateParamClearManualCompensation; }
            set
            {
                operationParameter.IsUpdateParamClearManualCompensation = value;
                RaisePropertyChanged();
            }
        }
    }
}