using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.ViewModel
{
    public class CompDataRow : INotifyPropertyChanged
    {
        private int _rowIndex;
        private string _actualValue;
        private string _axisPosition;

        public int RowIndex
        {
            get => _rowIndex;
            set
            {
                if (_rowIndex != value)
                {
                    _rowIndex = value;
                    OnPropertyChanged(nameof(RowIndex));
                }
            }
        }

        public string ActualValue
        {
            get => _actualValue;
            set
            {
                if (_actualValue != value)
                {
                    _actualValue = value;
                    OnPropertyChanged(nameof(ActualValue));
                }
            }
        }

        public string AxisPosition
        {
            get => _axisPosition;
            set
            {
                if (_axisPosition != value)
                {
                    _axisPosition = value;
                    OnPropertyChanged(nameof(AxisPosition));
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
