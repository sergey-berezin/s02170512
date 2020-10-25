namespace RecognitionUI
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    public partial class MainWindow : Window
    {

        public static RoutedCommand OpenDefault = new RoutedCommand("OpenDefault", typeof(MainWindow));
        public static RoutedCommand Start = new RoutedCommand("Start", typeof(MainWindow));
        public static RoutedCommand Stop = new RoutedCommand("Stop", typeof(MainWindow));
        public ViewModel VM; 


        public MainWindow()
        {
            InitializeComponent();
            VM = new ViewModel();
            this.DataContext = VM;

        }


        public void OpenCommand(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                VM.Open(fbd.SelectedPath);
            }
        }

        private void CanOpenDefaultCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenDefaultCommand(object sender, ExecutedRoutedEventArgs e)
        {
            VM.OpenDefault();
                //открываем дефолтную директорию через ViewModel -> Model
        }

        private void CanStartCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = VM != null;
        }

        private void StartCommand(object sender, ExecutedRoutedEventArgs e)
        {
            VM.Start();
        }

        private void CanStopCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = VM != null;
        }

        private void StopCommand(object sender, ExecutedRoutedEventArgs e)
        {
            VM.Stop();
            //куда результат??? 
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //останавливаем все таски
            //закрываем все файлы
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Classes.SelectedItem.GetType().Equals(typeof(Pair<string, int>)))
                VM.SelectedClass = ((Pair<string, int>)Classes.SelectedItem).Item1;
        }


    }
}
