using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.ViewModel
{
    public class BMSetupDataConfViewModel : BindableBase
    {
        public ObservableCollection<BladeMeasureData> BladeMeasureList { get; set; } = new ObservableCollection<BladeMeasureData>();

        private float _currentMeasureValue;

        public float CurrentMeasureValue
        {
            get { return _currentMeasureValue; }
            set { SetProperty(ref _currentMeasureValue, value); }
        }
    }

    public class BladeMeasureData : BindableBase
    {
        private string _fieldName;

        public string FieldName
        {
            get { return _fieldName; }
            set { SetProperty(ref _fieldName, value); }
        }

        private float _fieldValue;

        public float FieldValue
        {
            get { return _fieldValue; }
            set { SetProperty(ref _fieldValue, value); }
        }
    }
}