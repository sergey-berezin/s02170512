namespace RecognitionUI
{
    using RecognitionLibrary;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Threading;


    /* TODO: динамическое отображение классов (по мере поступления?) */


    public class ViewModel : INotifyPropertyChanged
    {
        private string selectedClass;

        internal bool isRunning;

        readonly Dispatcher disp = Dispatcher.CurrentDispatcher;

        private string modelPath;

        private string classLabels;

        //private int recognitionStatus = 0;

        private static string curDir = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.Parent.FullName;

        private string imageDirectory;

        private NNModel Model; 

        /*public int RecognitionStatus
        {
            get
            {
                return recognitionStatus;
            }
            set
            {
                recognitionStatus = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RecognitionStatus"));
            }
        }

        public int StatusMax = 20;*/

        public string SelectedClass { 
            get
            { 
                return selectedClass; 
            } 
            set 
            {   
                selectedClass = value;
                if (value != null)
                {
                    SelectedClassInfo = SelectAll(selectedClass);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedClassInfo"));
                }
            }
        }

        public ObservableCollection<Pair<string, int>> AvailableClasses { get; set; }

        public ConcurrentQueue<RecognitionInfo> ClassesInfo;  // Model.CQ

        public ObservableCollection<RecognitionInfo> SelectedClassInfo { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ViewModel(params string[] s)
        {
            if (s != null && s.Length >=2)
            {
                modelPath = s[0];
                classLabels = s[1];
            }
            else
            {
                modelPath = Path.Combine(curDir, "mnist-8.onnx");
                classLabels = Path.Combine(curDir, "classlabel.txt");
            }
            Initialize();
            Model.OutputResult += ChangeCollectionResult;
        }

        public void Initialize() 
        {
            Model = new NNModel(modelPath, classLabels);
            AvailableClasses = new ObservableCollection<Pair<string, int>>();

            /*foreach (string line in File.ReadAllLines(classLabels))
            {
                AvailableClasses.Add(new Pair<string, int>(line, 0));
            }*/
            SelectedClassInfo = new ObservableCollection<RecognitionInfo>();
            ClassesInfo = new ConcurrentQueue<RecognitionInfo>();
        }

        public void Clear()
        {
            AvailableClasses.Clear();
            ClassesInfo.Clear();
            SelectedClassInfo.Clear();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AvailableCLasses"));
            //Model.CQ.Clear();
        }

        public ObservableCollection<RecognitionInfo> SelectAll(string classlabel) //task
        {
            ObservableCollection<RecognitionInfo> res = new ObservableCollection<RecognitionInfo>();
            if (ClassesInfo.Count() == 0)
                return res;
            foreach(var q in ClassesInfo)
            {
                if (q.Class == classlabel)
                    res.Add(q);
            }
            return res;
        }

        public void ChangeCollectionResult(NNModel sender, ConcurrentQueue<RecognitionInfo> result) //task
        {
            disp.BeginInvoke(new Action(() =>
            {
               RecognitionInfo tmp;
               result.TryDequeue(out tmp);
               Pair<string, int> p;
               try
               {
                   p = AvailableClasses.Single(i => i.Item1 == tmp.Class);
                   p.Item2 += 1;
               }
               catch(InvalidOperationException)
               {
                   AvailableClasses.Add(new Pair<string, int>(tmp.Class, 1));
                   PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AvailableClasses")); //Item2
               }         
               ClassesInfo.Enqueue(tmp);
            }));
            //RecognitionStatus += ClassesInfo.Count;s
            //SourceChanged?.Invoke(this, e: new SourceChangedEventArgs("Classes"));
            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item2"));

        }

        public void OpenDefault() //task
        {
            Clear();
            imageDirectory =  NNModel.DefaultImageDir;
            Model.ImageDirectory = imageDirectory;
            //StatusMax = new DirectoryInfo(imageDirectory).GetFiles().Length;
            //PropertyChanged(this, new PropertyChangedEventArgs("StatusMax"));

        }

        internal void Open(string selectedPath) //task
        {
            Clear();
            Model.ImageDirectory = selectedPath;
            //StatusMax = new DirectoryInfo(selectedPath).GetFiles().Length;
            //PropertyChanged(this, new PropertyChangedEventArgs("StatusMax"));
        }

        internal void Start()//task
        {
            var uis = TaskScheduler.FromCurrentSynchronizationContext();

            Task.Run(() =>
            {
                var res = Model.MakePrediction();
                //return res;
            }).ContinueWith(t =>
            {
                isRunning = false;
            }, CancellationToken.None, TaskContinuationOptions.None, uis);
        }

        internal void Stop()
        {            
            Model.StopRecognition();
            isRunning = false;
        }

    }
}
