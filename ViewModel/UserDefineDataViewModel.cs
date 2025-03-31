using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.database.db.modle;

namespace 精密切割系统.ViewModel
{
    public class UserDefineDataViewModel : INotifyPropertyChanged
    {
        private UserDefineDataModel _model;

        // 语言选项
        public ObservableCollection<string> Languages { get; private set; }

        // 型号改变后速度清零选项
        public ObservableCollection<string> DeviceChangeCutSpeeds { get; private set; }

        // 速度变更选项
        public ObservableCollection<string> SpeedChanges { get; private set; }

        // 高度补偿选项
        public ObservableCollection<string> HeightChanges { get; private set; }

        // 校准时是否进行过切割的检查选项
        public ObservableCollection<string> CutWorkCheckWhenAlignments { get; private set; }

        // 继续使用超过刀片使用限制错误选项
        public ObservableCollection<string> ContinueAfterBladeUserLimitErrors { get; private set; }

        // 刀片使用限制错误处理选项
        public ObservableCollection<string> ProcessingAfterBladeUserLimitErrors { get; private set; }

        // BBD定时选项
        public ObservableCollection<string> BBDTimings { get; private set; }

        // 基准线调整选项
        public ObservableCollection<string> HairlineAdjustments { get; private set; }

        // 手动调光选项
        public ObservableCollection<string> LightingAdjustments { get; private set; }

        // 刀片更换检查选项
        public ObservableCollection<string> BladeReplacementChecks { get; private set; }

        // Z轴处理数据选择选项
        public ObservableCollection<string> ZProcessingDataSelections { get; private set; }

        // 半自动切割时对齐选择选项
        public ObservableCollection<string> AlignSelectionsWhenSemiAutoCutting { get; private set; }
        public UserDefineDataViewModel()
        {
            _model = new UserDefineDataModel();
            Languages = new ObservableCollection<string> { "Chinese", "English", "Japanese" };
            DeviceChangeCutSpeeds = new ObservableCollection<string> { "clear", "keep" };
            SpeedChanges = new ObservableCollection<string> { "YES", "NO", "PAUSE", "SPEED" };
            HeightChanges = new ObservableCollection<string> { "YES", "NO", "PAUSE" };
            CutWorkCheckWhenAlignments = new ObservableCollection<string> { "YES", "NO" };
            ContinueAfterBladeUserLimitErrors = new ObservableCollection<string> { "YES", "NO" };
            ProcessingAfterBladeUserLimitErrors = new ObservableCollection<string> { "WORK", "LINE" };
            BBDTimings = new ObservableCollection<string> { "Z-EM", "RECHECK" };
            HairlineAdjustments = new ObservableCollection<string> { "AUTO", "MANUAL" };
            LightingAdjustments = new ObservableCollection<string> { "AUTO", "MANUAL" };
            BladeReplacementChecks = new ObservableCollection<string> { "YES", "NO" };
            ZProcessingDataSelections = new ObservableCollection<string> { "HEIGHT", "DEPTH" };
            AlignSelectionsWhenSemiAutoCutting = new ObservableCollection<string> { "YES", "NO" };
        }

        public long Id
        {
            get { return _model.Id; }
            set
            {
                if (_model.Id != value)
                {
                    _model.Id = value;
                    OnPropertyChanged();
                }
            }
        }

        public string MachineId
        {
            get { return _model.MachineId; }
            set
            {
                if (_model.MachineId != value)
                {
                    _model.MachineId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SystemPassword
        {
            get { return _model.SystemPassword; }
            set
            {
                if (_model.SystemPassword != value)
                {
                    _model.SystemPassword = value;
                    OnPropertyChanged();
                }
            }
        }

        public long SystemPasswordTime
        {
            get { return _model.SystemPasswordTime; }
            set
            {
                if (_model.SystemPasswordTime != value)
                {
                    _model.SystemPasswordTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AfterEdgeDressPos
        {
            get { return _model.AfterEdgeDressPos; }
            set
            {
                if (_model.AfterEdgeDressPos != value)
                {
                    _model.AfterEdgeDressPos = value;
                    OnPropertyChanged();
                }
            }
        }

        public string BladeExchangeYPos
        {
            get { return _model.BladeExchangeYPos; }
            set
            {
                if (_model.BladeExchangeYPos != value)
                {
                    _model.BladeExchangeYPos = value;
                    OnPropertyChanged();
                }
            }
        }

        public string HairlineAdjustLimit
        {
            get { return _model.HairlineAdjustLimit; }
            set
            {
                if (_model.HairlineAdjustLimit != value)
                {
                    _model.HairlineAdjustLimit = value;
                    OnPropertyChanged();
                }
            }
        }

        public string BlowTime
        {
            get { return _model.BlowTime; }
            set
            {
                if (_model.BlowTime != value)
                {
                    _model.BlowTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public string WorkVacuumCheckTime
        {
            get { return _model.WorkVacuumCheckTime; }
            set
            {
                if (_model.WorkVacuumCheckTime != value)
                {
                    _model.WorkVacuumCheckTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public string WaitTimeUntilEnergySavingMode
        {
            get { return _model.WaitTimeUntilEnergySavingMode; }
            set
            {
                if (_model.WaitTimeUntilEnergySavingMode != value)
                {
                    _model.WaitTimeUntilEnergySavingMode = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Language
        {
            get { return _model.Language; }
            set
            {
                if (_model.Language != value)
                {
                    _model.Language = value;
                    OnPropertyChanged();
                }
            }
        }



        public string DeviceChangeCutSpeed
        {
            get { return _model.DeviceChangeCutSpeed; }
            set
            {
                if (_model.DeviceChangeCutSpeed != value)
                {
                    _model.DeviceChangeCutSpeed = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SpeedChange
        {
            get { return _model.SpeedChange; }
            set
            {
                if (_model.SpeedChange != value)
                {
                    _model.SpeedChange = value;
                    OnPropertyChanged();
                }
            }
        }

        public string HeightChange
        {
            get { return _model.HeightChange; }
            set
            {
                if (_model.HeightChange != value)
                {
                    _model.HeightChange = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CutWorkCheckWhenAlignment
        {
            get { return _model.CutWorkCheckWhenAlignment; }
            set
            {
                if (_model.CutWorkCheckWhenAlignment != value)
                {
                    _model.CutWorkCheckWhenAlignment = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ContinueAfterBladeUserLimitError
        {
            get { return _model.ContinueAfterBladeUserLimitError; }
            set
            {
                if (_model.ContinueAfterBladeUserLimitError != value)
                {
                    _model.ContinueAfterBladeUserLimitError = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ProcessingAfterBladeUserLimitError
        {
            get { return _model.ProcessingAfterBladeUserLimitError; }
            set
            {
                if (_model.ProcessingAfterBladeUserLimitError != value)
                {
                    _model.ProcessingAfterBladeUserLimitError = value;
                    OnPropertyChanged();
                }
            }
        }

        public string BBDTiming
        {
            get { return _model.BBDTiming; }
            set
            {
                if (_model.BBDTiming != value)
                {
                    _model.BBDTiming = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool StopSpindleByBbd
        {
            get { return _model.StopSpindleByBbd; }
            set
            {
                if (_model.StopSpindleByBbd != value)
                {
                    _model.StopSpindleByBbd = value;
                    OnPropertyChanged();
                }
            }
        }

        public string HairlineAdjustment
        {
            get { return _model.HairlineAdjustment; }
            set
            {
                if (_model.HairlineAdjustment != value)
                {
                    _model.HairlineAdjustment = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LightingAdjustment
        {
            get { return _model.LightingAdjustment; }
            set
            {
                if (_model.LightingAdjustment != value)
                {
                    _model.LightingAdjustment = value;
                    OnPropertyChanged();
                }
            }
        }

        public string BladeReplacementCheck
        {
            get { return _model.BladeReplacementCheck; }
            set
            {
                if (_model.BladeReplacementCheck != value)
                {
                    _model.BladeReplacementCheck = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ZProcessingDataSelection
        {
            get { return _model.ZProcessingDataSelection; }
            set
            {
                if (_model.ZProcessingDataSelection != value)
                {
                    _model.ZProcessingDataSelection = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AlignSelectionWhenSemiAutoCutting
        {
            get { return _model.AlignSelectionWhenSemiAutoCutting; }
            set
            {
                if (_model.AlignSelectionWhenSemiAutoCutting != value)
                {
                    _model.AlignSelectionWhenSemiAutoCutting = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SpindleCenterPositionOffset
        {
            get { return _model.SpindleCenterPositionOffset; }
            set
            {
                if (_model.SpindleCenterPositionOffset != value)
                {
                    _model.SpindleCenterPositionOffset = value;
                    OnPropertyChanged();
                }
            }
        }

        public string WaterPumpOnTimer
        {
            get { return _model.WaterPumpOnTimer; }
            set
            {
                if (_model.WaterPumpOnTimer != value)
                {
                    _model.WaterPumpOnTimer = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AtomizingNozzlePositionX
        {
            get { return _model.AtomizingNozzlePositionX; }
            set
            {
                if (_model.AtomizingNozzlePositionX != value)
                {
                    _model.AtomizingNozzlePositionX = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AtomizingNozzlePositionY
        {
            get { return _model.AtomizingNozzlePositionY; }
            set
            {
                if (_model.AtomizingNozzlePositionY != value)
                {
                    _model.AtomizingNozzlePositionY = value;
                    OnPropertyChanged();
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
