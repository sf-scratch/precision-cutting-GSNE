using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using 精密切割系统.database.db.modle;

namespace 精密切割系统.ViewModel
{
    public class ReplaceBladeViewModel : INotifyPropertyChanged
    {
        private ReplaceBladeModel _replaceBladeModel;

        public ReplaceBladeViewModel()
        {
            _replaceBladeModel = new ReplaceBladeModel();
            InitializeComboBoxItems();
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
            get { return _replaceBladeModel.Id; }
            set { _replaceBladeModel.Id = value; OnPropertyChanged(); }
        }

        public string BladeUnit
        {
            get { return _replaceBladeModel.BladeUnit; }
            set { _replaceBladeModel.BladeUnit = value; OnPropertyChanged(); }
        }

        public string BladeLotID
        {
            get { return _replaceBladeModel.BladeLotID; }
            set { _replaceBladeModel.BladeLotID = value; OnPropertyChanged(); }
        }

        public string SpecName
        {
            get { return _replaceBladeModel.SpecName; }
            set { _replaceBladeModel.SpecName = value; OnPropertyChanged(); }
        }

        public string NewOrOld
        {
            get { return _replaceBladeModel.NewOrOld; }
            set { _replaceBladeModel.NewOrOld = value; OnPropertyChanged(); }
        }

        public List<string> NewOrOldComboBoxItems { get; set; }

        public string ReplaceReason
        {
            get { return _replaceBladeModel.ReplaceReason; }
            set { _replaceBladeModel.ReplaceReason = value; OnPropertyChanged(); }
        }

        public List<string> ReplaceReasonComboBoxItems { get; set; }

        public string BladeType
        {
            get { return _replaceBladeModel.BladeType; }
            set { _replaceBladeModel.BladeType = value; OnPropertyChanged(); }
        }

        public List<string> BladeTypeComboBoxItems { get; set; }

        public string BladeOutside
        {
            get { return _replaceBladeModel.BladeOutside; }
            set { _replaceBladeModel.BladeOutside = value; OnPropertyChanged(); }
        }

        public string BladeThickness
        {
            get { return _replaceBladeModel.BladeThickness; }
            set { _replaceBladeModel.BladeThickness = value; OnPropertyChanged(); }
        }

        public string BladeLife
        {
            get { return _replaceBladeModel.BladeLife; }
            set { _replaceBladeModel.BladeLife = value; OnPropertyChanged(); }
        }
        public string BladeLifeM
        {
            get { return _replaceBladeModel.BladeLifeM; }
            set { _replaceBladeModel.BladeLifeM = value; OnPropertyChanged(); }
        }
        

        public string HardBladeLength
        {
            get { return _replaceBladeModel.HardBladeLength; }
            set { _replaceBladeModel.HardBladeLength = value; OnPropertyChanged(); }
        }

        public string SoftBladeHolder
        {
            get { return _replaceBladeModel.SoftBladeHolder; }
            set { _replaceBladeModel.SoftBladeHolder = value; OnPropertyChanged(); }
        }

        private void InitializeComboBoxItems()
        {
            NewOrOldComboBoxItems = new List<string> { "新", "旧" };
            ReplaceReasonComboBoxItems = new List<string> { "新刀片装入", "破损", "正常磨损", "达切割刀数极限", "崩碎太多", "其他" };
            BladeTypeComboBoxItems = new List<string> { "硬刀", "软刀" };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}