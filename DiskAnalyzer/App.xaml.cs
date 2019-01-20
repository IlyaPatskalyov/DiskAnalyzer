using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Autofac;
using DiskAnalyzer.Configuration;
using Serilog;

namespace DiskAnalyzer
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly ILogger logger = LoggingFactory.Build();
        private IContainer container;

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            Current.DispatcherUnhandledException += DispatcherOnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            container = ContainerFactory.Build(logger);
            container.Resolve<MainWindow>().Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            container.Dispose();
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            var exception = args.ExceptionObject as Exception;
            logger.Error(exception, "Unhandled app domain exception");
            HandleException(exception);
        }

        private void DispatcherOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            var exception = args.Exception;
            logger.Error(exception, "Unhandled dispatcher thread exception");
            args.Handled = true;

            HandleException(exception);
        }

        private void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
        {
            var exception = args.Exception.GetBaseException();
            logger.Error("Unhandled task exception");
            args.SetObserved();

            HandleException(exception);
        }

        private void HandleException(Exception exception)
        {
            var errorMessage = "An application error occurred.\n\n" +
                               $"Error: {exception.Message + (exception.InnerException != null ? "\n" + exception.InnerException.Message : null)}\n\n" +
                               "Do you want to continue?\n(if you click Yes you will continue with your work, if you click No the application will close)";

            if (MessageBox.Show(errorMessage, "Application Error", MessageBoxButton.YesNoCancel, MessageBoxImage.Error) == MessageBoxResult.No)
            {
                if (MessageBox.Show("WARNING: The application will close.\nDo you really want to close it?",
                                    "Close the application!", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    Current.Shutdown();
                }
            }
        }
    }
}