using System;
using System.Collections.Generic;
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

namespace 精密切割系统.View.Dialogs
{
    /// <summary>
    /// SelectDateTimeDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SelectDateTimeDialog : UserControl
    {
        public DateTime? SelectedDate
        {
            get { return (DateTime?)GetValue(SelectedDateProperty); }
            set { SetValue(SelectedDateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedDate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedDateProperty =
            DependencyProperty.Register("SelectedDate", typeof(DateTime?), typeof(SelectDateTimeDialog), new PropertyMetadata(default(DateTime)));

        public SelectDateTimeDialog()
        {
            InitializeComponent();
        }

        private void CombinedClock_TimeChanged(object sender, MaterialDesignThemes.Wpf.TimeChangedEventArgs e)
        {
            if (SelectedDate != null)
            {
                SelectedDate = SelectedDate.Value.Date + e.NewTime.TimeOfDay;
            }
        }
    }
}