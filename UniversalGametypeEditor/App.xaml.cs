using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading; // Missing namespace for DispatcherUnhandledExceptionEventArgs

namespace UniversalGametypeEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // Handle exceptions from all threads
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Handle exceptions from UI thread
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Handle exceptions from tasks
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            HandleException("UI Thread Exception", e.Exception);
            e.Handled = true; // Prevent application from crashing
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException("AppDomain Unhandled Exception", e.ExceptionObject as Exception);
            // Can't set Handled = true here, application might still terminate
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleException("Task Exception", e.Exception);
            e.SetObserved(); // Prevent application from crashing
        }

        private void HandleException(string source, Exception exception)
        {
            try
            {
                // Log the exception
                Logger.LogError($"{source}: An error occurred", exception);

                // Show friendly message to user
                string message = "An unexpected error has occurred in the application.\n\n";
                message += "The error has been logged. You can continue using the application, but some functionality may not work properly.\n\n";
                message += "Would you like to see the details of the error?";

                var result = System.Windows.MessageBox.Show(message, "Application Error",
                    MessageBoxButton.YesNo, MessageBoxImage.Error);

                if (result == MessageBoxResult.Yes)
                {
                    System.Windows.MessageBox.Show($"Error: {exception.Message}\n\nSource: {source}",
                        "Error Details", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                // Last resort if logging itself fails
                System.Windows.MessageBox.Show("A critical error occurred in the error handler: " + ex.Message,
                    "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
