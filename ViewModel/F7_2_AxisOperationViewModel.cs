using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;

namespace 精密切割系统.ViewModel
{
    internal class F7_2_AxisOperationViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Tag> DataCollection { get; set; }
        Dictionary<string, Tag> allTags = PlcControl.allTags;

        public F7_2_AxisOperationViewModel() 
        {
            DataCollection = new ObservableCollection<Tag>();
            // 添加数据到集合中
            if (allTags.ContainsKey("X轴当前电机状态"))
            {
                DataCollection.Add(allTags["X轴当前电机状态"]);
            }
            if (allTags.ContainsKey("Y轴当前电机状态"))
            {
                DataCollection.Add(allTags["Y轴当前电机状态"]);
            }
            if (allTags.ContainsKey("Z1轴当前电机状态"))
            {
                DataCollection.Add(allTags["Z1轴当前电机状态"]);
            }
            if (allTags.ContainsKey("Theta轴当前状态"))
            {
                DataCollection.Add(allTags["Theta轴当前状态"]);
            }

            if (allTags.ContainsKey("X轴当前位置"))
            {
                DataCollection.Add(allTags["X轴当前位置"]);
            }
            if (allTags.ContainsKey("Y轴当前位置"))
            {
                DataCollection.Add(allTags["Y轴当前位置"]);
            }
            if (allTags.ContainsKey("Z1轴当前位置"))
            {
                DataCollection.Add(allTags["Z1轴当前位置"]);
            }
            if (allTags.ContainsKey("Theta轴当前状态"))
            {
                DataCollection.Add(allTags["Theta轴当前状态"]);
            }
        }

        public bool _xStatus;
        public bool _yStatus;
        public bool _zStatus;
        public bool _thetaStatus;

        public bool _xSwitch = true;
        /*public bool _ySwitch = true;
        public bool _zSwitch = true;
        public bool _thetaSwitch = true;
*/
        public bool xStatus
        {
            get
            {
                foreach (Tag t in DataCollection)
                {
                    if (t.name == "X轴当前电机状态" && t.Value == "2")
                    {
                        return true;
                    }
                }
                return false;
            }
            set
            {
                foreach (Tag t in DataCollection)
                {
                    if (t.name == "X轴当前电机状态")
                    {
                        t.Value = value.ToString();
                        OnPropertyChanged("xStatus");
                    }
                }  
            }
        }

        public bool yStatus
        {
            get
            {
                if(allTags.ContainsKey("Y轴当前电机状态") && allTags["Y轴当前电机状态"].value == "2")
                {
                    return true;
                }
                return false;
            }
            set
            {
                foreach (Tag t in DataCollection)
                {
                    if (t.name == "Y轴当前电机状态")
                    {
                        t.value = value.ToString();
                        OnPropertyChanged("yStatus");
                    }
                }
            }
        }

        public bool zStatus
        {
            get
            {
                foreach (Tag t in DataCollection)
                {
                    if (t.name == "X轴当前电机状态" && t.value == "2")
                    {
                        return true;
                    }
                }
                return false;
                //return _xStatus;
            }
            set
            {
                foreach (Tag t in DataCollection)
                {
                    if (t.name == "X轴当前电机状态")
                    {
                        t.value = value.ToString();
                        OnPropertyChanged("xStatus");
                    }
                }
            }
        }

        public bool thetaStatus
        {
            get
            {
                foreach (Tag t in DataCollection)
                {
                    if (t.name == "X轴当前电机状态" && t.value == "2")
                    {
                        return true;
                    }
                }
                return false;
                //return _xStatus;
            }
            set
            {
                foreach (Tag t in DataCollection)
                {
                    if (t.name == "X轴当前电机状态")
                    {
                        t.value = value.ToString();
                        OnPropertyChanged("xStatus");
                    }
                }
            }
        }

        public string xCurPosition
        {
            get
            {
                if (allTags.ContainsKey("X轴当前位置"))
                {
                    return allTags["X轴当前位置"].Value;
                }
                return "";
            }
            set
            {
                if (allTags.ContainsKey("X轴当前位置"))
                {
                    allTags["X轴当前位置"].Value = value.ToString();
                    OnPropertyChanged("xCurPosition");
                }
            }
        }

        public string yCurPosition
        {
            get
            {
                if (allTags.ContainsKey("Y轴当前位置"))
                {
                    return allTags["Y轴当前位置"].Value;
                }
                return "";
            }
            set
            {
                if (allTags.ContainsKey("Y轴当前位置"))
                {
                    allTags["Y轴当前位置"].Value = value.ToString();
                    OnPropertyChanged("yCurPosition");
                }
            }
        }

        public string zCurPosition
        {
            get
            {
                if (allTags.ContainsKey("Z1轴当前位置"))
                {
                    return allTags["Z1轴当前位置"].Value;
                }
                return "";
            }
            set
            {
                if (allTags.ContainsKey("Z1轴当前位置"))
                {
                    allTags["Z1轴当前位置"].Value = value.ToString();
                    OnPropertyChanged("zCurPosition");
                }
            }
        }

        public string thetaCurPosition
        {
            get
            {
                if (allTags.ContainsKey("Theta轴当前位置"))
                {
                    return "";
                }
                return "";
            }
            set
            {
                if (allTags.ContainsKey("Theta轴当前位置"))
                {
                    allTags["Theta轴当前位置"].Value = value.ToString();
                    OnPropertyChanged("thetaCurPosition");
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
