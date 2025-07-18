using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using 精密切割系统.Model.common;

namespace 精密切割系统.View.Controls
{
    /// <summary>
    /// RealTimeInfoBox.xaml 的交互逻辑
    /// </summary>
    public partial class RealTimeInfoBox : GroupBox
    {
        // 定义一个依赖属性
        public static readonly DependencyProperty MessagesProperty =
            DependencyProperty.Register(
                "Messages",
                typeof(ObservableCollection<MessageModel>),
                typeof(RealTimeInfoBox),
                new PropertyMetadata(null));

        // 常规属性包装器
        public ObservableCollection<MessageModel> Messages
        {
            get { return (ObservableCollection<MessageModel>)GetValue(MessagesProperty); }
            set { SetValue(MessagesProperty, value); }
        }

        public RealTimeInfoBox()
        {
            InitializeComponent();
        }
    }
}
