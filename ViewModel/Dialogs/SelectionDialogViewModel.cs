using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace 精密切割系统.ViewModel.Dialogs
{
    public class SelectionDialogViewModel : BindableBase
    {
        private string _yesButtonContent;
        public string YesButtonContent
        {
            get { return _yesButtonContent; }
            set { SetProperty(ref _yesButtonContent, value); }
        }

        private Visibility _yesButtonVisibility;
        public Visibility YesButtonVisibility
        {
            get { return _yesButtonVisibility; }
            set { SetProperty(ref _yesButtonVisibility, value); }
        }

        private string _cancelButtonContent;
        public string CancelButtonContent
        {
            get { return _cancelButtonContent; }
            set { SetProperty(ref _cancelButtonContent, value); }
        }

        private Visibility _cancelButtonVisibility;
        public Visibility CancelButtonVisibility
        {
            get { return _cancelButtonVisibility; }
            set { SetProperty(ref _cancelButtonVisibility, value); }
        }

        private string _noButtonContent;
        public string NoButtonContent
        {
            get { return _noButtonContent; }
            set { SetProperty(ref _noButtonContent, value); }
        }

        private Visibility _noButtonVisibility;
        public Visibility NoButtonVisibility
        {
            get { return _noButtonVisibility; }
            set { SetProperty(ref _noButtonVisibility, value); }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }
    }
}
