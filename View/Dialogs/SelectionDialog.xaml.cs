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
using 精密切割系统.ViewModel.Dialogs;

namespace 精密切割系统.View.Dialogs
{
    /// <summary>
    /// SelectionDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SelectionDialog : UserControl
    {
        public const string YES = "YES";
        public const string NO = "NO";

        public static SelectionDialog NewInstance(string yesBtn, string? noBtn = null, string? cancelBtn = null, string? title = null)
        {
            SelectionDialog selectionDialog = new()
            {
                DataContext = new SelectionDialogViewModel
                {
                    YesButtonContent = yesBtn,
                    YesButtonVisibility = Visibility.Visible,
                    NoButtonContent = noBtn ?? string.Empty,
                    NoButtonVisibility = noBtn is null ? Visibility.Collapsed : Visibility.Visible,
                    CancelButtonContent = cancelBtn ?? string.Empty,
                    CancelButtonVisibility = cancelBtn is null ? Visibility.Collapsed : Visibility.Visible,
                    Title = title ?? "请确认操作"
                }
            };
            return selectionDialog;
        }

        public SelectionDialog()
        {
            InitializeComponent();
        }
    }
}
