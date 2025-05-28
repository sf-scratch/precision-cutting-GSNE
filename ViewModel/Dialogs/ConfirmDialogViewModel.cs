using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.ViewModel.Dialogs
{
    public class ConfirmDialogViewModel : BindableBase, IDialogAware
    {
        public DialogCloseListener RequestClose { get; set; }

        private string _buttonContent;
        public string ButtonContent
        {
            get { return _buttonContent; }
            set { SetProperty(ref _buttonContent, value); }
        }

        private DelegateCommand _confirmCommand;
        public DelegateCommand ConfirmCommand =>
            _confirmCommand ?? (_confirmCommand = new DelegateCommand(ExecuteConfirmCommand));

        void ExecuteConfirmCommand()
        {
            RequestClose.Invoke(new DialogResult(ButtonResult.OK));
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            // 关闭对话框时的处理逻辑
            // 例如，释放资源、保存状态等
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            ButtonContent = parameters.GetValue<string>(nameof(ButtonContent));
        }
    }
}
