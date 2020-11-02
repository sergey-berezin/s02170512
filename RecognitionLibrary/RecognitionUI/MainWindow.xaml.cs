namespace RecognitionUI
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Forms;
    using System.Windows.Input;

    public partial class MainWindow : Window
    {
        
        public static RoutedCommand OpenDefault = new RoutedCommand("OpenDefault", typeof(MainWindow));
        public static RoutedCommand CustomModel = new RoutedCommand("CustomModel", typeof(MainWindow));
        public static RoutedCommand Start = new RoutedCommand("Start", typeof(MainWindow));
        public static RoutedCommand Stop = new RoutedCommand("Stop", typeof(MainWindow));
        

        public ViewModel VM;

        /* TODO: fix ProgressBar */
       


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
            FolderBrowserDialog fbd = new FolderBrowserDialog();

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

        private void CanCustomCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        public void CustomModelCommand(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                List<string> paths = new List<string>(ofd.FileNames);
                string modelPath = paths.Find(s => s.Contains(".onnx"));
                string labelsPath = paths.Find(s => s.Contains("txt") || s.Contains("csv") || s.Contains("label"));
               // System.Windows.MessageBox.Show(modelPath);
               // System.Windows.MessageBox.Show( labelsPath);
                 VM = new ViewModel(modelPath, labelsPath);
                this.DataContext = VM;
                VM.isRunning = false;
            }
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
