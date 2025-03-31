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
    //测高参数4.7
    public class BladeHeightViewModel : INotifyPropertyChanged
    {
        public BladeHeightModel _bladeHeightModel;

        //下拉框数据源 
        //工作盘尺寸
        public List<string> ChuckTableSizeItems { get; set; } = new List<string> { "3inch", "4inch", "5inch", "6inch", "8inch" };

        //chuck_table_shape
        public List<string> ChuckTableShapeItems { get; set; } = new List<string> { "ROUND", "SQUARE" };
        //TableType
        public List<string> TableTypeItems { get; set; } = new List<string> { "POROUS", "UNIVERSAL", "SPECIAL" };
        public List<string> SetupDefaultItems { get; set; } = new List<string> { "CONTACT" }; //"CONTACT", "NONCONTACT", "ALL"
        public List<string> CallOperatorWhenAutoSetupItems { get; set; } = new List<string> { "NO" }; // "AUTO", "NO" 
        public List<string> PrecutAfterNonContactSetupItems { get; set; } = new List<string> { "NO" }; //"YES", "NO"

        public List<string> ReplaceReasonComboBoxItems { get; set; } = new List<string> { "ROUND", "破损", "正常磨损", "达切割刀数极限", "崩碎太多", "其他" };

        //
        public BladeHeightViewModel()
        {
            _bladeHeightModel = new BladeHeightModel(); 
        }
        private bool isBladeUnitMm = true; // 默认单位为mm
        public bool IsBladeUnitMm
        {
            get { return isBladeUnitMm; }
            set
            {
                if (isBladeUnitMm != value)
                {
                    isBladeUnitMm = value;
                    OnPropertyChanged(nameof(IsBladeUnitMm));
                    if (value)
                        IsBladeUnitInch = false; // 当mm被选中时，确保inch不被选中
                }
            }
        }

        private bool isBladeUnitInch = false;
        public bool IsBladeUnitInch
        {
            get { return isBladeUnitInch; }
            set
            {
                if (isBladeUnitInch != value)
                {
                    isBladeUnitInch = value;
                    OnPropertyChanged(nameof(IsBladeUnitInch));
                    if (value)
                        IsBladeUnitMm = false; // 当inch被选中时，确保mm不被选中
                }
            }
        }
        public long Id
        {
            get { return _bladeHeightModel.Id; }
            set { _bladeHeightModel.Id = value; OnPropertyChanged(); }
        }

        //单位 mm/inch 
        public string Unit
        {
            get { return _bladeHeightModel.Unit; }
            set { _bladeHeightModel.Unit = value; OnPropertyChanged(); }
        }
        //测高方式

        public string SetupDefault
        {
            get { return _bladeHeightModel.SetupDefault; }
            set { _bladeHeightModel.SetupDefault = value; OnPropertyChanged(); }
        }

        //工作盘尺寸
        public string ChuckTableSize
        {
            get { return _bladeHeightModel.ChuckTableSize; }
            set { _bladeHeightModel.ChuckTableSize = value; OnPropertyChanged(); }
        }
        // 刀片高度
        public string BladeHeight
        {
            get { return _bladeHeightModel.BladeHeight; }
            set { _bladeHeightModel.BladeHeight = value; OnPropertyChanged(); }
        }

       
        public List<string> NewOrOldComboBoxItems { get; set; }

        //自动测高
        public string CallOperatorWhenAutoSetup
        {
            get { return _bladeHeightModel.CallOperatorWhenAutoSetup; }
            set { _bladeHeightModel.CallOperatorWhenAutoSetup = value; OnPropertyChanged(); }
        }

        //非接触测高后预切割
        public string PrecutAfterNonContactSetup
        {
            get { return _bladeHeightModel.PrecutAfterNonContactSetup; }
            set { _bladeHeightModel.PrecutAfterNonContactSetup = value; OnPropertyChanged(); }
        }

        public List<string> BladeTypeComboBoxItems { get; set; }

        //主轴转速（转/min）
        public string SpindleRev
        {
            get { return _bladeHeightModel.SpindleRev; }
            set { _bladeHeightModel.SpindleRev = value; OnPropertyChanged(); }
        }
        //主轴转速（转/min）
        public string SetupZAxisMaxDistance
        {
            get { return _bladeHeightModel.SetupZAxisMaxDistance; }
            set { _bladeHeightModel.SetupZAxisMaxDistance = value; OnPropertyChanged(); }
        }
        //自动测高的重复次数（time/s）
        public string Retry
        {
            get { return _bladeHeightModel.Retry; }
            set { _bladeHeightModel.Retry = value; OnPropertyChanged(); }
        }
        //测高刀片消耗不足消耗量（mm）
        public string ExcessiveWear
        {
            get { return _bladeHeightModel.ExcessiveWear; }
            set { _bladeHeightModel.ExcessiveWear = value; OnPropertyChanged(); }
        }

        //测高刀片消耗不足消耗量（mm）
        public string InsufficientWear
        {
            get { return _bladeHeightModel.InsufficientWear; }
            set { _bladeHeightModel.InsufficientWear = value; OnPropertyChanged(); }
        }

        //C/T测高检测回数
        public string CtSetupCheck
        {
            get { return _bladeHeightModel.CtSetupCheck; }
            set { _bladeHeightModel.CtSetupCheck = value; OnPropertyChanged(); }
        }
        // 测高2次的容许值-非解除测高（mm）
        public string PermissibleAmountNonContact
        {
            get { return _bladeHeightModel.PermissibleAmountNonContact; }
            set { _bladeHeightModel.PermissibleAmountNonContact = value; OnPropertyChanged(); }
        }
        //测高2次的容许值-C/T（mm）
        public string PermissibleAmountCt
        {
            get { return _bladeHeightModel.PermissibleAmountCt; }
            set { _bladeHeightModel.PermissibleAmountCt = value; OnPropertyChanged(); }
        }
        //刀片吹气时间-非解除测高（s）
        public string BladeBlowTimeNonContact
        {
            get { return _bladeHeightModel.BladeBlowTimeNonContact; }
            set { _bladeHeightModel.BladeBlowTimeNonContact = value; OnPropertyChanged(); }
        }
        // 刀片吹气时间-C/T（s）
        public string BladeBlowTimeCt
        {
            get { return _bladeHeightModel.BladeBlowTimeCt; }
            set { _bladeHeightModel.BladeBlowTimeCt = value; OnPropertyChanged(); }
        }
        //刀刃损耗的安全量（mm）
        public string ClearanceBetweenFlangeWorkSurface
        {
            get { return _bladeHeightModel.ClearanceBetweenFlangeWorkSurface; }
            set { _bladeHeightModel.ClearanceBetweenFlangeWorkSurface = value; OnPropertyChanged(); }
        }
        //非接触测高时吹风后的等待时间（s）
        public string WaitingTimeAfterNonSetupAirBlow
        {
            get { return _bladeHeightModel.WaitingTimeAfterNonSetupAirBlow; }
            set { _bladeHeightModel.WaitingTimeAfterNonSetupAirBlow = value; OnPropertyChanged(); }
        }

        //chuck_table_shape
        public string ChuckTableShape
        {
            get { return _bladeHeightModel.ChuckTableShape; }
            set { _bladeHeightModel.ChuckTableShape = value; OnPropertyChanged(); }
        }

        //工作盘类型
        public string TableType
        {
            get { return _bladeHeightModel.TableType; }
            set { _bladeHeightModel.TableType = value; OnPropertyChanged(); }
        }

        // 非接触测高时的吹风时间（s）
        public string BlowTimeNcsBlock
        {
            get { return _bladeHeightModel.BlowTimeNcsBlock; }
            set { _bladeHeightModel.BlowTimeNcsBlock = value; OnPropertyChanged(); }
        }

        //测高高速移动速度-非接触（mm/s）
        public string HighSpeedNonContact
        {
            get { return _bladeHeightModel.HighSpeedNonContact; }
            set { _bladeHeightModel.HighSpeedNonContact = value; OnPropertyChanged(); }
        }

        //测高高速移动速度-C/T（mm/s）
        public string HighSpeedCt
        {
            get { return _bladeHeightModel.HighSpeedCt; }
            set { _bladeHeightModel.HighSpeedCt = value; OnPropertyChanged(); }
        }

        //测高低速移动速度-非接触（mm/s）
        public string LowSpeedNonContact
        {
            get { return _bladeHeightModel.LowSpeedNonContact; }
            set { _bladeHeightModel.LowSpeedNonContact = value; OnPropertyChanged(); }
        }
        //测高低速移动速度-C/T（mm/s）
        public string LowSpeedCt
        {
            get { return _bladeHeightModel.LowSpeedCt; }
            set { _bladeHeightModel.LowSpeedCt = value; OnPropertyChanged(); }
        }
        //测高低速移动范围-非接触（mm）
        public string LowSpeedStrokeNonContact
        {
            get { return _bladeHeightModel.LowSpeedStrokeNonContact; }
            set { _bladeHeightModel.LowSpeedStrokeNonContact = value; OnPropertyChanged(); }
        }
        //测高低速移动范围-C/T（mm）
        public string LowSpeedStrokeCt
        {
            get { return _bladeHeightModel.LowSpeedStrokeCt; }
            set { _bladeHeightModel.LowSpeedStrokeCt = value; OnPropertyChanged(); }
        }
        //测高时θ轴移动角度
        public string ThetaRotationForContactSetup
        {
            get { return _bladeHeightModel.ThetaRotationForContactSetup; }
            set { _bladeHeightModel.ThetaRotationForContactSetup = value; OnPropertyChanged(); }
        }
        //测高时θ轴开始移动位置
        public string ThetaRotationForStartPosition
        {
            get { return _bladeHeightModel.ThetaRotationForStartPosition; }
            set { _bladeHeightModel.ThetaRotationForStartPosition = value; OnPropertyChanged(); }
        }
        //测高时θ轴移动结束位置
        public string ThetaRotationForEndPosition
        {
            get { return _bladeHeightModel.ThetaRotationForEndPosition; }
            set { _bladeHeightModel.ThetaRotationForEndPosition = value; OnPropertyChanged(); }
        }
        //测高时θ轴现在的位置
        public string ThetaRotationForNowPosition
        {
            get { return _bladeHeightModel.ThetaRotationForNowPosition; }
            set { _bladeHeightModel.ThetaRotationForNowPosition = value; OnPropertyChanged(); }
        }
        //现在测高θ轴的位置返回次数
        public string ChuckTableRotationCompleted
        {
            get { return _bladeHeightModel.ChuckTableRotationCompleted; }
            set { _bladeHeightModel.ChuckTableRotationCompleted = value; OnPropertyChanged(); }
        }

         
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}
