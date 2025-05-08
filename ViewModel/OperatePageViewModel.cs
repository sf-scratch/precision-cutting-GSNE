using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Model.common;
using 精密切割系统.View.common;

namespace 精密切割系统.ViewModel
{
    public class OperatePageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<RightButtonParams> RightButtonParams { get; set; }

        public OperatePageViewModel()
        {
            RightButtonParams = WindowLayout.OperatePageButtons;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
