using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Model.common;

namespace 精密切割系统.View.common
{
    public class WindowLayout
    {
        public static ObservableCollection<RightButtonParams> RightPageButtons => _lazyRightPageButtons.Value;
        private static readonly Lazy<ObservableCollection<RightButtonParams>> _lazyRightPageButtons = new(() => []);
        public static ObservableCollection<RightButtonParams> OperatePageButtons => _lazyOperatePageButtons.Value;
        private static readonly Lazy<ObservableCollection<RightButtonParams>> _lazyOperatePageButtons = new(() => []);
    }
}
