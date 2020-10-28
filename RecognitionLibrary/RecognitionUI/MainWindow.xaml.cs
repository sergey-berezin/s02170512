namespace RecognitionUI
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    public partial class MainWindow : Window
    {
        
        public static RoutedCommand OpenDefault = new RoutedCommand("OpenDefault", typeof(MainWindow));
        public static RoutedCommand Start = new RoutedCommand("Start", typeof(MainWindow));
        public static RoutedCommand Stop = new RoutedCommand("Stop", typeof(MainWindow));
        

        public ViewModel VM;

        /* TODO: отдельное окно для отображения все изображений */
        /* TODO: два текстбокса для ввода пути и меток к произвольной сетке */
        /* TODO: fix ProgressBar */
        /* TODO: Вид кнопок */
        /* TODO: fix Stop */


        public MainWindow()
        {
            InitializeComponent();
            VM = new ViewModel();
            CommandBinding OpenCmdBinding = new CommandBinding(ApplicationCommands.Open, OpenCommand, CanOpenCommand);
            this.CommandBindings.Add(OpenCmdBinding);
            this.DataContext = VM;
            VM.isRunning = false;
        }


        private void CanOpenCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = VM != null && !VM.isRunning;
        }

        public void OpenCommand(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                VM.Open(fbd.SelectedPath);
                VM.isRunning = false;
            }
        }

        private void OpenDefaultCommand(object sender, ExecutedRoutedEventArgs e)
        {
            VM.OpenDefault();         //открываем дефолтную директорию через ViewModel -> Model
        }

        private void CanStartCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = VM != null && !VM.isRunning;
        }

        private void StartCommand(object sender, ExecutedRoutedEventArgs e)
        {
            VM.isRunning = true;
            VM.Start();
        }

        private void CanStopCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = VM != null && VM.isRunning;
        }

        private void StopCommand(object sender, ExecutedRoutedEventArgs e)
        {
            VM.Stop();
            //VM.isRunning = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Classes.SelectedItem != null && Classes.SelectedItem.GetType().Equals(typeof(Pair<string, int>)))
                VM.SelectedClass = ((Pair<string, int>)Classes.SelectedItem).Item1;
        }


    }
}
