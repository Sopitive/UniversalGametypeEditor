using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Updater
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            StateChanged += MainWindow_StateChanged;
        }

        protected override async void OnContentRendered(System.EventArgs e)
        {
            base.OnContentRendered(e);
            var args = System.Environment.GetCommandLineArgs().Skip(1).ToArray();
            // Pass LogText and LogScrollViewer to show live logs
            await UpdateRunner.RunAsync(args, StatusText, DownloadProgress, ProgressLabel, LogText, LogScrollViewer);
            // Close the updater once done
            //Close();
        }

        private void MainWindow_StateChanged(object? sender, System.EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                MaximizeButton.Visibility = Visibility.Collapsed;
                RestoreButton.Visibility = Visibility.Visible;
            }
            else
            {
                MaximizeButton.Visibility = Visibility.Visible;
                RestoreButton.Visibility = Visibility.Collapsed;
            }
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        private void CommandBinding_Executed_Minimize(object sender, ExecutedRoutedEventArgs e) => SystemCommands.MinimizeWindow(this);
        private void CommandBinding_Executed_Maximize(object sender, ExecutedRoutedEventArgs e) => SystemCommands.MaximizeWindow(this);
        private void CommandBinding_Executed_Restore(object sender, ExecutedRoutedEventArgs e) => SystemCommands.RestoreWindow(this);
        private void CommandBinding_Executed_Close(object sender, ExecutedRoutedEventArgs e) => SystemCommands.CloseWindow(this);
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}