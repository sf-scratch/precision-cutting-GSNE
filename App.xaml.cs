using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Input;
using 精密切割系统.Utils;


namespace 精密切割系统
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
                                                                                                               {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            base.OnStartup(e);
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            //e.Exception   发生的异常
            //e.Handled	 是否已处理异常事件
            Tools.LogError("程序异常：" + e.Exception.Source + "@@" + e.Exception.Message);

        }
    }
 }
