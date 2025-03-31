using NPOI.OpenXmlFormats.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.database.db.modle;

namespace 精密切割系统.ViewModel
{
    public class F5_1_1_PrecutDataViewModel : INotifyPropertyChanged
    {
        public PreCutModel precutParameter = null;

        public F5_1_1_PrecutDataViewModel()
        {
            precutParameter = new PreCutModel();
            pct = new ObservableCollection<PreCutTableClass>();
        }

        public long Id
        {
            get { return precutParameter.Id; }
            set
            {
                if (precutParameter.Id != value)
                {
                    precutParameter.Id = value;
                    OnPropertyChanged("Id");
                }
            }
        }

        public string PrecutNo
        {
            get { return precutParameter.PrecutNo; }
            set
            {
                if (precutParameter.PrecutNo != value)
                {
                    precutParameter.PrecutNo = value;
                    OnPropertyChanged("PrecutNo");
                }
            }
        }
        // PrecutID
        public string PrecutID
        {
            get { return precutParameter.PrecutID; }
            set
            {
                if (precutParameter.PrecutID != value)
                {
                    precutParameter.PrecutID = value;
                    OnPropertyChanged("PrecutID");
                }
            }
        }

        public string PrecutType
        {
            get { return precutParameter.PrecutType.ToString(); }
            set
            {
                if (precutParameter.PrecutType.ToString() != value)
                {
                    precutParameter.PrecutType = long.Parse(value);
                    OnPropertyChanged("PrecutType");
                }
            }
        }

        public int UsedBladeNo
        {
            get { return precutParameter.UsedBladeNo; }
            set
            {
                if (precutParameter.UsedBladeNo != value)
                {
                    precutParameter.UsedBladeNo = value;
                    OnPropertyChanged("UsedBladeNo");
                }
            }
        }

        public int NewBladeNo
        {
            get { return precutParameter.NewBladeNo; }
            set
            {
                if (precutParameter.NewBladeNo != value)
                {
                    precutParameter.NewBladeNo = value;
                    OnPropertyChanged("NewBladeNo");
                }
            }
        }

        public string PrecutDecrease
        {
            get { return precutParameter.PrecutDecrease; }
            set
            {
                if (precutParameter.PrecutDecrease != value)
                {
                    precutParameter.PrecutDecrease = value;
                    OnPropertyChanged("PrecutDecrease");
                }
            }
        }

        public string WorkThickness
        {
            get { return precutParameter.WorkThickness; }
            set
            {
                if (precutParameter.WorkThickness != value)
                {
                    precutParameter.WorkThickness = value;
                    OnPropertyChanged("WorkThickness");
                }
            }
        }

        public string FeedSpd
        {
            get { return precutParameter.FeedSpd; }
            set
            {
                if (precutParameter.FeedSpd != value)
                {
                    precutParameter.FeedSpd = value;
                    OnPropertyChanged("FeedSpd");
                }
            }
        }

        public string OfLines
        {
            get { return precutParameter.OfLines; }
            set
            {
                if (precutParameter.OfLines != value)
                {
                    precutParameter.OfLines = value;
                    OnPropertyChanged("OfLines");
                }
            }
        }
        public string FeedDistance
        {
            get { return precutParameter.FeedDistance; }
            set
            {
                if (precutParameter.FeedDistance != value)
                {
                    precutParameter.FeedDistance = value;
                    OnPropertyChanged("FeedDistance");
                }
            }
        }

        public string NewBladeInitialSpeed
        {
            get { return precutParameter.NewBladeInitialSpeed; }
            set
            {
                if (precutParameter.NewBladeInitialSpeed != value)
                {
                    precutParameter.NewBladeInitialSpeed = value;
                    OnPropertyChanged("NewBladeInitialSpeed");
                }
            }
        }

        public string OldBladeInitialSpeed
        {
            get { return precutParameter.OldBladeInitialSpeed; }
            set
            {
                if (precutParameter.OldBladeInitialSpeed != value)
                {
                    precutParameter.OldBladeInitialSpeed = value;
                    OnPropertyChanged("OldBladeInitialSpeed");
                }
            }
        }

        public string SpeedAtReprecut
        {
            get { return precutParameter.SpeedAtReprecut; }
            set
            {
                if (precutParameter.SpeedAtReprecut != value)
                {
                    precutParameter.SpeedAtReprecut = value;
                    OnPropertyChanged("SpeedAtReprecut");
                }
            }
        }

        public string PrecutEndSpeed
        {
            get { return precutParameter.PrecutEndSpeed; }
            set
            {
                if (precutParameter.PrecutEndSpeed != value)
                {
                    precutParameter.PrecutEndSpeed = value;
                    OnPropertyChanged("PrecutEndSpeed");
                }
            }
        }

        public string LinesOfPrecut
        {
            get { return precutParameter.LinesOfPrecut; }
            set
            {
                if (precutParameter.LinesOfPrecut != value)
                {
                    precutParameter.LinesOfPrecut = value;
                    OnPropertyChanged("LinesOfPrecut");
                }
            }
        }

        public string WorkThicknessGreater
        {
            get { return precutParameter.WorkThicknessGreater; }
            set
            {
                if (precutParameter.WorkThicknessGreater != value)
                {
                    precutParameter.WorkThicknessGreater = value;
                    OnPropertyChanged("WorkThicknessGreater");
                }
            }
        }
        public ObservableCollection<PreCutTableClass> pct;
        public ObservableCollection<PreCutTableClass> PrecutTable
        {
            get 
            {
                //ObservableCollection<PreCutTable> pct = new ObservableCollection<PreCutTable>();
/*                List<string> LinesNumber = FeedSpd.Split(",").ToList();
                List<string> LinesSpeed = OfLines.Split(",").ToList();
                pct.Clear();
                for (int i = 0;i<LinesSpeed.Count; i++)
                {
                    PreCutTableClass pTmp = new PreCutTableClass(LinesNumber[i], LinesSpeed[i]);
                    pTmp.Index = (i + 1).ToString();
                    pct.Add(pTmp);
                }*/
                return pct; 
            }
            set
            {
                /*List<string> LinesNumber = new List<string>();
                List<string> LinesSpeed = new List<string>();
                for (int i = 0; i < value.Count; i++)
                {
                    LinesNumber.Add(value[i].Number);
                    LinesSpeed.Add(value[i].Speed);
                }*/
                pct = value;
                /*OfLines = string.Join(",", LinesNumber);
                FeedSpd = string.Join(",", LinesSpeed);*/
                OnPropertyChanged("PrecutTable");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PreCutTableClass : INotifyPropertyChanged
    {
        public PreCutTableClass() { }
        public PreCutTableClass(string s, string n) 
        {
            Speed = s;
            Number = n;
        }
        private string _index, _speed, _number, _len;
        public string Index
        {
            get { return _index; }
            set
            {
                if (_index != value)
                {
                    _index = value;
                    OnPropertyChanged("Index");
                }
            }
        }

        public string Speed
        {
            get { return _speed; }
            set
            {
                if (_speed != value)
                {
                    _speed = value;
                    OnPropertyChanged("Speed");
                }
            }
        }
        public string Number
        {
            get { return _number; }
            set
            {
                if (_number != value)
                {
                    _number = value;
                    OnPropertyChanged("Number");
                }
            }
        }

        public string Len
        {
            get { return _len; }
            set
            {
                if (_len != value)
                {
                    _len = value;
                    OnPropertyChanged("Len");
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
