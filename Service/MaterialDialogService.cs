using DryIoc;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace 精密切割系统.Service
{
    public class MaterialDialogService : IMaterialDialogService
    {
        private readonly IContainerExtension _container;

        public MaterialDialogService(IContainerExtension container)
        {
            _container = container;
        }

        public async Task<IDialogResult> ShowMaterialDialog<TView>(IDialogParameters parameters, string dialogHostIdentifier = "RootDialog")
            where TView : FrameworkElement
        {
            var view = _container.Resolve<TView>();
            return await ShowMaterialDialog(view, parameters, dialogHostIdentifier);
        }

        private async Task<IDialogResult> ShowMaterialDialog(object content, IDialogParameters parameters, string dialogHostIdentifier = "RootDialog")
        {
            var parameter = await DialogHost.Show(content, dialogHostIdentifier);
            if (parameter is IDialogResult result)
            {
                return result;
            }
            else
            {
                DialogResult res = new DialogResult(ButtonResult.OK);
                res.Parameters = new DialogParameters { { "Result", parameter } };
                return res;
            }
        }

        public void ShowDialog(string name, IDialogParameters parameters, DialogCallback callback)
        {
            
        }

        // 实现Prism原生IDialogService的其他方法...
    }
}
