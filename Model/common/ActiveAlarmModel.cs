using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using 精密切割系统.Model.plc;

namespace 精密切割系统.Model.common
{
    public class ActiveAlarmModel : BindableBase
    {
        private string _address;
        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        private string _message;
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        private AlarmLevel _level;
        public AlarmLevel Level
        {
            get => _level;
            set => SetProperty(ref _level, value);
        }

        public Brush LevelBrush
        {
            get
            {
                return _level switch
                {
                    AlarmLevel.Warn => Brushes.YellowGreen,
                    AlarmLevel.Error => Brushes.Red,
                    AlarmLevel.None => Brushes.Blue,
                    _ => Brushes.Transparent
                };
            }
        }
    }
}
