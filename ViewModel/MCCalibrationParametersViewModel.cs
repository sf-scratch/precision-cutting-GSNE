using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.database.db.modle;
using 精密切割系统.Utils;

namespace 精密切割系统.ViewModel
{
    public class MCCalibrationParametersViewModel : INotifyPropertyChanged
    {
        private UserDefineDataModel _model;

        public MCCalibrationParametersViewModel()
        {
            _model = CurrentUtils.GetCurrentUserDefineDataModel();
        }

        public UserDefineDataModel UserDefineDataModel
        {
            get { return _model; }
        }

        public string BaselineWidthCh1
        {
            get { return _model.BaselineWidthCh1; }
            set
            {
                if (_model.BaselineWidthCh1 != value)
                {
                    _model.BaselineWidthCh1 = value;
                    OnPropertyChanged();
                }
            }
        }

        public string BaselineWidthCh2
        {
            get { return _model.BaselineWidthCh2; }
            set
            {
                if (_model.BaselineWidthCh2 != value)
                {
                    _model.BaselineWidthCh2 = value;
                    OnPropertyChanged();
                }
            }
        }

        public string BaselineWidthCh3
        {
            get { return _model.BaselineWidthCh3; }
            set
            {
                if (_model.BaselineWidthCh3 != value)
                {
                    _model.BaselineWidthCh3 = value;
                    OnPropertyChanged();
                }
            }
        }

        public string BaselineWidthCh4
        {
            get { return _model.BaselineWidthCh4; }
            set
            {
                if (_model.BaselineWidthCh4 != value)
                {
                    _model.BaselineWidthCh4 = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LightSourceBrightnessCh1
        {
            get { return _model.LightSourceBrightnessCh1; }
            set
            {
                if (_model.LightSourceBrightnessCh1 != value)
                {
                    _model.LightSourceBrightnessCh1 = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LightSourceBrightnessCh2
        {
            get { return _model.LightSourceBrightnessCh2; }
            set
            {
                if (_model.LightSourceBrightnessCh2 != value)
                {
                    _model.LightSourceBrightnessCh2 = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LightSourceBrightnessCh3
        {
            get { return _model.LightSourceBrightnessCh3; }
            set
            {
                if (_model.LightSourceBrightnessCh3 != value)
                {
                    _model.LightSourceBrightnessCh3 = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LightSourceBrightnessCh4
        {
            get { return _model.LightSourceBrightnessCh4; }
            set
            {
                if (_model.LightSourceBrightnessCh4 != value)
                {
                    _model.LightSourceBrightnessCh4 = value;
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
