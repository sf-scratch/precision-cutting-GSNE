using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace 精密切割系统.Service
{
    public interface IMaterialDialogService : IDialogService
    {
        Task<IDialogResult> ShowMaterialDialog<TView>(IDialogParameters parameters, string dialogHostIdentifier = "RootDialog")
            where TView : FrameworkElement;
    }
}
