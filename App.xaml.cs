using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Input;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.View.Dialogs;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.View.Pages.common;
using 精密切割系统.View.Pages.F2_ManualOperation;
using 精密切割系统.View.Pages.F4_BladeMaintenance;
using 精密切割系统.ViewModel;
using 精密切割系统.ViewModel.Dialogs;


namespace 精密切割系统
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        //用于控制应用程序单开
        public static readonly Mutex MUTEX = new Mutex(true, "精密切割系统");

        protected override void Initialize()
        {
            base.Initialize();
            if (!HslCommunication.Authorization.SetAuthorizationCode("c6f33910-f831-44c2-8cbb-99b96a80f432"))
            {
                MessageBox.Show("HSL授权码错误，请联系作者获取最新授权码！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
        }

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
            containerRegistry.RegisterForNavigation<AutoCut, AutoCutViewModel>();
            containerRegistry.RegisterForNavigation<AutoCutSelectConfig, AutoCutSelectConfigViewModel>();
            containerRegistry.RegisterForNavigation<AutoCutConfig, AutoCutConfigViewModel>();
            containerRegistry.RegisterForNavigation<AutoCutHistory, AutoCutHistoryViewModel>();
            containerRegistry.RegisterForNavigation<AutoCutSetCutPosition, AutoCutSetCutPositionViewModel>();
            containerRegistry.RegisterForNavigation<EmptyRun, EmptyRunViewModel>();
            containerRegistry.RegisterForNavigation<MQSemiAutomaticCuttingRun, MQSemiAutomaticCuttingRunViewModel>();
            containerRegistry.RegisterForNavigation<MQSemiAutomaticCuttingStop, MQSemiAutomaticCuttingStopViewModel>();
            containerRegistry.RegisterDialog<ConfirmDialog, ConfirmDialogViewModel>();
            containerRegistry.Register<IDialogWindow, ConfirmDialogWindow>(nameof(ConfirmDialogWindow));
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
