using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Input;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.View.Pages.common;
using 精密切割系统.View.Pages.F4_BladeMaintenance;
using 精密切割系统.ViewModel;


namespace 精密切割系统
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<EmptyView>();
            containerRegistry.RegisterForNavigation<BladeReplacementConfiguration, BladeReplacementConfigurationViewModel>();
            containerRegistry.RegisterForNavigation<AutoCutRuning, AutoCutRuningViewModel>();
            containerRegistry.RegisterForNavigation<AutoCutPausing, AutoCutPausingViewModel>();
        }

        //protected override void OnStartup(StartupEventArgs e)
        //                                                                                                       {
        //    DispatcherUnhandledException += App_DispatcherUnhandledException;
        //    base.OnStartup(e);
        //}

        //void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        //{
        //    //e.Exception   发生的异常
        //    //e.Handled	 是否已处理异常事件
        //    Tools.LogError("程序异常：" + e.Exception.Source + "@@" + e.Exception.Message);

        //}
    }
 }
