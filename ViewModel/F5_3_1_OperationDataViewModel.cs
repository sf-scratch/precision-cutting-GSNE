using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.database.db.modle;
using 精密切割系统.Utils;

namespace 精密切割系统.ViewModel
{
    internal class F5_3_1_OperationDataViewModel : INotifyPropertyChanged
    {
        public OperationParametersModel operationParameter = null;

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
                    OnPropertyChanged("XScanSpeed");
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
                    OnPropertyChanged("XSscanDistance");
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
                    OnPropertyChanged("YScanSpeed");
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
                    OnPropertyChanged("YSscanDistance");
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
                    OnPropertyChanged("ZScanSpeed");
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
                    OnPropertyChanged("ZSscanDistance");
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
                    OnPropertyChanged("RScanSpeed");
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
                    OnPropertyChanged("RSscanDistance");
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
                    OnPropertyChanged("MoveLowTime");
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
                    OnPropertyChanged("MoveHighTime");
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
                    OnPropertyChanged("XScreenIndex");
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
                    OnPropertyChanged("YScreenIndex");
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
                    OnPropertyChanged("EscapeRate");
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
                    OnPropertyChanged("ExtraEscapeRate");
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
                    OnPropertyChanged("MStopElectrical");
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
                    OnPropertyChanged("MStopTime");
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
                    OnPropertyChanged("ZStopElectrical");
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
                    OnPropertyChanged("ZStopTime");
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
                    OnPropertyChanged("ZStopAfterSeq");
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
                    OnPropertyChanged("XStartClearance");
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
                    OnPropertyChanged("XEndClearance");
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
                    OnPropertyChanged("YClearance");
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
                    OnPropertyChanged("AutoFocusCheckLimit");
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
                    OnPropertyChanged("AirCurtainStroke");
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
                    OnPropertyChanged("VaccumWorkLowerLimit");
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
                    OnPropertyChanged("VaccumPumpLowerLimitOther");
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
                    OnPropertyChanged("LimitDuringCutting");
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
                    OnPropertyChanged("VaccumPumpLowerLimit");
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
                    OnPropertyChanged("xPanelJogDistance");
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
                    OnPropertyChanged("yPanelJogDistance");
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
                    OnPropertyChanged("zPanelJogDistance");
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
                    OnPropertyChanged("zAxisCompNum");
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
                    OnPropertyChanged("zAxisCompValue");
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
                    OnPropertyChanged("cutXAxisBackSpeed");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
