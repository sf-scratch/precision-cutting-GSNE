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
    public class MCCalibrationParametersViewModel : BindableBase
    {
        private UserDefineDataModel _model;

        public MCCalibrationParametersViewModel(UserDefineDataModel userDefineDataModel)
        {
            _model = userDefineDataModel;
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
                _model.BaselineWidthCh1 = value;
                RaisePropertyChanged();
            }
        }

        public string BaselineWidthCh2
        {
            get { return _model.BaselineWidthCh2; }
            set
            {
                _model.BaselineWidthCh2 = value;
                RaisePropertyChanged();
            }
        }

        public string BaselineWidthCh3
        {
            get { return _model.BaselineWidthCh3; }
            set
            {
                _model.BaselineWidthCh3 = value;
                RaisePropertyChanged();
            }
        }

        public string BaselineWidthCh4
        {
            get { return _model.BaselineWidthCh4; }
            set
            {
                _model.BaselineWidthCh4 = value;
                RaisePropertyChanged();
            }
        }

        public string EdgeWidthCh1
        {
            get { return _model.EdgeWidthCh1; }
            set
            {
                _model.EdgeWidthCh1 = value;
                RaisePropertyChanged();
            }
        }

        public string EdgeWidthCh2
        {
            get { return _model.EdgeWidthCh2; }
            set
            {
                _model.EdgeWidthCh2 = value;
                RaisePropertyChanged();
            }
        }

        public string EdgeWidthCh3
        {
            get { return _model.EdgeWidthCh3; }
            set
            {
                _model.EdgeWidthCh3 = value;
                RaisePropertyChanged();
            }
        }

        public string EdgeWidthCh4
        {
            get { return _model.EdgeWidthCh4; }
            set
            {
                _model.EdgeWidthCh4 = value;
                RaisePropertyChanged();
            }
        }

        public string LightSourceBrightnessCh1
        {
            get { return _model.LightSourceBrightnessCh1; }
            set
            {
                _model.LightSourceBrightnessCh1 = value;
                RaisePropertyChanged();
            }
        }

        public string LightSourceBrightnessCh2
        {
            get { return _model.LightSourceBrightnessCh2; }
            set
            {
                _model.LightSourceBrightnessCh2 = value;
                RaisePropertyChanged();
            }
        }

        public string LightSourceBrightnessCh3
        {
            get { return _model.LightSourceBrightnessCh3; }
            set
            {
                _model.LightSourceBrightnessCh3 = value;
                RaisePropertyChanged();
            }
        }

        public string LightSourceBrightnessCh4
        {
            get { return _model.LightSourceBrightnessCh4; }
            set
            {
                _model.LightSourceBrightnessCh4 = value;
                RaisePropertyChanged();
            }
        }

        public bool HasEdgeLine
        {
            get { return _model.HasEdgeLine; }
            set
            {
                _model.HasEdgeLine = value;
                RaisePropertyChanged();
            }
        }

        public bool IsAllowedCutting
        {
            get { return _model.IsAllowedCutting; }
            set
            {
                _model.IsAllowedCutting = value;
                RaisePropertyChanged();
            }
        }

        public string CutYPositiveLimit
        {
            get { return _model.CutYPositiveLimit; }
            set
            {
                _model.CutYPositiveLimit = value;
                RaisePropertyChanged();
            }
        }

        public string CutYNegativeLimit
        {
            get { return _model.CutYNegativeLimit; }
            set
            {
                _model.CutYNegativeLimit = value;
                RaisePropertyChanged();
            }
        }

        private string _horizontalStraighteningStroke;

        public string HorizontalStraighteningStroke
        {
            get { return _horizontalStraighteningStroke; }
            set { SetProperty(ref _horizontalStraighteningStroke, value); }
        }

        private string _verticalStraighteningStroke;

        public string VerticalStraighteningStroke
        {
            get { return _verticalStraighteningStroke; }
            set { SetProperty(ref _verticalStraighteningStroke, value); }
        }
    }
}