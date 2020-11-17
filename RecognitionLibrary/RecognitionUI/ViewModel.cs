namespace RecognitionUI
{
    using RecognitionLibrary;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using System.Windows.Threading;


    /* TODO: поиск по хэшу  +- */ 
    /* TODO: не подгружать блобу + */
    /* TODO: добавить поле просмотра статистики */



    public class ViewModel : INotifyPropertyChanged
    {
        private string selectedClass;

        internal bool isRunning;

        public long Statistic { get; set; }

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

        public ObservableCollection<RecognitionInfo> ClassesImages { get; set; }

        public ObservableCollection<RecognitionInfo> SelectedClassInfo { get; set; }

        public RecognitionLibraryContext db;
        internal bool isWriting;

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
            ClassesImages = new ObservableCollection<RecognitionInfo>();
            SelectedClassInfo = new ObservableCollection<RecognitionInfo>();
            db = new RecognitionLibraryContext();
            //db.Images = new 

        }

        public void Clear()
        {
            AvailableClasses.Clear();
            ClassesImages.Clear();
            SelectedClassInfo.Clear();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AvailableCLasses"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CLassesImages"));
            Model.CQ.Clear();
        }

        public ObservableCollection<RecognitionInfo> SelectAll(string classlabel) // add collection filter???
        {
            ObservableCollection<RecognitionInfo> res = new ObservableCollection<RecognitionInfo>();
            if (ClassesImages.Count() == 0)
                return res;
            foreach(var q in ClassesImages)
            {
                if (q.Class == classlabel)
                    res.Add(q);
            }
            return res;
        }

        public void ChangeCollectionResult(NNModel sender, ConcurrentQueue<RecognitionInfo> result) 
        {
            disp.BeginInvoke(new Action(() =>
            {
                RecognitionInfo tmp;
                result.TryDequeue(out tmp);
                lock (db)
                {
                    var sameimage = db.FindOne(tmp);
                    if (sameimage != null)
                    {
                        sameimage.Statistic++;
                        db.SaveChanges();
                        ClassesImages.Add(new RecognitionInfo(sameimage.Path, sameimage.Label.ToString(), sameimage.Confidence));    
                    }
                    else
                    {
                        Task.Run(() =>
                        {
                            Blob resBlob = new Blob { Image = tmp.Image };
                            db.Images.Add(new RecognitionImage
                            {
                                Path = tmp.Path,
                                Confidence = tmp.Confidence,
                                Statistic = 0,
                                ImageDetails = resBlob,
                                Label = int.Parse(tmp.Class)
                            });
                            db.SaveChanges();
                        });
                        ClassesImages.Add(tmp);
                        //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ClassesImages"));
                    }
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ClassesImages"));
                Pair<string, int> p;
                try
                {
                    p = AvailableClasses.Single(i => i.Item1 == tmp.Class);
                    p.Item2 += 1;
                }
                catch(InvalidOperationException)
                {
                    AvailableClasses.Add(new Pair<string, int>(tmp.Class, 1));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AvailableClasses"));
                }         
            }));
            //RecognitionStatus += ClassesInfo.Count;
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

        internal void Open(string selectedPath) 
        {
            Clear();
            Model.ImageDirectory = selectedPath;
            //StatusMax = new DirectoryInfo(selectedPath).GetFiles().Length;
            //PropertyChanged(this, new PropertyChangedEventArgs("StatusMax"));
        }

        internal void Start()
        {
            Task.Run(() =>
            {
                var res = Model.MakePrediction();
                //return res;
            }).ContinueWith(t =>
            {
                isRunning = false;
            });
        }

        internal void Stop()
        {            
            Model.StopRecognition();
            isRunning = false;
        }

    }
}
