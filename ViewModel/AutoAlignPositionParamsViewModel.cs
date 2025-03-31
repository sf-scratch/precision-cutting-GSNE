using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.database.db.modle;

namespace 精密切割系统.ViewModel
{
    class AutoAlignPositionParamsViewModel : INotifyPropertyChanged
    {

        public AutoAlignPositionParamsModel _model;

        public AutoAlignPositionParamsViewModel()
        {
            _model = new AutoAlignPositionParamsModel();
            _rows = new ObservableCollection<CompDataRow>();
        }

        public ObservableCollection<CompDataRow> _rows;

        public ObservableCollection<CompDataRow> Rows
        {
            get { return _rows; }
            set
            {
                if (_rows != value)
                {
                    _rows = value;
                    OnPropertyChanged("Rows");
                }
            }
        }
        public long Id
        {
            get { return _model.Id; }
            set
            {
                if (_model.Id != value)
                {
                    _model.Id = value;
                    OnPropertyChanged("Id");
                }
            }
        }

        public int SpindleRev
        {
            get { return _model.SpindleRev; }
            set
            {
                if (_model.SpindleRev != value)
                {
                    _model.SpindleRev = value;
                    OnPropertyChanged("SpindleRev");
                }
            }
        }

        public float WorkbenchCh1
        {
            get { return _model.WorkbenchCh1; }
            set
            {
                if (_model.WorkbenchCh1 != value)
                {
                    _model.WorkbenchCh1 = value;
                    OnPropertyChanged("WorkbenchCh1");
                }
            }
        }

        public float WorkbenchCh2
        {
            get { return _model.WorkbenchCh2; }
            set
            {
                if (_model.WorkbenchCh2 != value)
                {
                    _model.WorkbenchCh2 = value;
                    OnPropertyChanged("WorkbenchCh2");
                }
            }
        }

        public float SquareCh1
        {
            get { return _model.SquareCh1; }
            set
            {
                if (_model.SquareCh1 != value)
                {
                    _model.SquareCh1 = value;
                    OnPropertyChanged("SquareCh1");
                }
            }
        }

        public float SquareCh2
        {
            get { return _model.SquareCh2; }
            set
            {
                if (_model.SquareCh2 != value)
                {
                    _model.SquareCh2 = value;
                    OnPropertyChanged("SquareCh2");
                }
            }
        }

        public float BladeHeight
        {
            get { return _model.BladeHeight; }
            set
            {
                if (_model.BladeHeight != value)
                {
                    _model.BladeHeight = value;
                    OnPropertyChanged("BladeHeight");
                }
            }
        }

        public int TestCount
        {
            get { return _model.TestCount; }
            set
            {
                if (_model.TestCount != value)
                {
                    _model.TestCount = value;
                    OnPropertyChanged("TestCount");
                }
            }
        }

        public float FeedSpeed
        {
            get { return _model.FeedSpeed; }
            set
            {
                if (_model.FeedSpeed != value)
                {
                    _model.FeedSpeed = value;
                    OnPropertyChanged("FeedSpeed");
                }
            }
        }

        public float YIndex
        {
            get { return _model.YIndex; }
            set
            {
                if (_model.YIndex != value)
                {
                    _model.YIndex = value;
                    OnPropertyChanged("YIndex");
                }
            }
        }

        public float DepthSteps
        {
            get { return _model.DepthSteps; }
            set
            {
                if (_model.DepthSteps != value)
                {
                    _model.DepthSteps = value;
                    OnPropertyChanged("DepthSteps");
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
