namespace RecognitionUI
{
    using Avalonia.Controls;
    using RecognitionLibrary;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reflection.Metadata.Ecma335;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Threading.Tasks;
    using System.Windows;

    //using System.Windows.Forms;

    public class ViewModel : INotifyPropertyChanged
    {
        private string selectedClass;

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

        public ObservableCollection<Pair<string, int>> Classes { get; set; }

        public ConcurrentQueue<RecognitionInfo> ClassesInfo;

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
        }

        public void Initialize() 
        {
            Model = new NNModel(modelPath, classLabels);
            Classes = new ObservableCollection<Pair<string, int>>();
           
            foreach (string line in File.ReadAllLines(classLabels))
            {
                Classes.Add(new Pair<string, int>(line, 0));
            }

            ClassesInfo = new ConcurrentQueue<RecognitionInfo>();
            Model.OutputResult += ChangeCollectionResult;
        }

        public void Clear()
        {
            foreach(var t in Classes)
            {
                t.Item2 = 0;
            }
            ClassesInfo.Clear();
            Model.CQ.Clear();
        }

        public ObservableCollection<RecognitionInfo> SelectAll(string classlabel)
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

        public void ChangeCollectionResult(NNModel sender, ConcurrentQueue<RecognitionInfo> result)
        {
            RecognitionInfo tmp;
            result.TryDequeue(out tmp);
            var p = Classes.Single(i => i.Item1 == tmp.Class);
            p.Item2 += 1;
            ClassesInfo.Enqueue(tmp);

            //RecognitionStatus += ClassesInfo.Count;s
            //SourceChanged?.Invoke(this, e: new SourceChangedEventArgs("Classes"));
            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item2"));

        }

        public void OpenDefault()
        {
            Clear();
            imageDirectory =  NNModel.DefaultImageDir;
            Model.ImageDirectory = imageDirectory;
            //StatusMax = new DirectoryInfo(imageDirectory).GetFiles().Length;
            //PropertyChanged(this, new PropertyChangedEventArgs("StatusMax"));

        }

        internal void Open(string selectedPath)
        {
            Clear();
            Model.ImageDirectory = selectedPath;
            //StatusMax = new DirectoryInfo(selectedPath).GetFiles().Length;
            //PropertyChanged(this, new PropertyChangedEventArgs("StatusMax"));
        }

        internal void Start()
        {
             Model.MakePrediction();
        }

        internal void Stop()
        {
            Model.StopRecognition();
        }

    }
}
