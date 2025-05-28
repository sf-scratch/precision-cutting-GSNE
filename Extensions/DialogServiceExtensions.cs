using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace 精密切割系统.Extensions
{
    public static class DialogServiceExtensions
    {
        public static async Task ShowDialogWindowAsync(this IDialogService dialogService, string name, IDialogParameters parameters, Action<IDialogResult> callback, string windowName)
        {
            TaskCompletionSource continueTcs = new TaskCompletionSource();
            Window mainWindow = Application.Current.MainWindow;
            mainWindow.IsEnabled = false;
            dialogService.Show(name, parameters, r =>
            {
                if (r.Result == ButtonResult.OK)
                {
                    mainWindow.IsEnabled = true;
                    callback?.Invoke(r);
                    continueTcs.SetResult();
                }
            }, windowName);
            await continueTcs.Task;
        }
    }
}
