namespace RecognitionUI
{
    using Newtonsoft.Json;
    using Contracts;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
    using System.Diagnostics;

    public class ViewModel : INotifyPropertyChanged
    {
        private string selectedClass;

        private string modelPath;

        private string classLabels;

        private string imageDirectory;

        //private int recognitionStatus = 0;

        private static readonly HttpClient client = new HttpClient();   //instead of model

        private static readonly string url = "https://localhost:44353/recognition";

        private static CancellationTokenSource cts = new CancellationTokenSource();

        private Dispatcher disp = Dispatcher.CurrentDispatcher;

        internal bool isRunning;

        public long Statistic { get; set; }

        public static string curDir = Directory.GetParent(Environment.CurrentDirectory).Parent.FullName;

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

        public string SelectedClass
        {
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

        public ObservableCollection<RecognitionContract> ClassesImages { get; set; }

        public ObservableCollection<RecognitionContract> SelectedClassInfo { get; set; }

        public RecognitionContract SelectedItem { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ViewModel(params string[] s)
        {
            if (s != null && s.Length >= 2)
            {
                modelPath = s[0];
                classLabels = s[1];
            }
            else
            {
                modelPath = Path.Combine(curDir, "mnist-8.onnx");
                classLabels = Path.Combine(curDir, "classlabel.txt");
            }
            try
            {
                Initialize();
            }
            catch (HttpRequestException)
            {
                disp.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show("Couldn't create model\nChoose correct files", "Info");
                }));
                return;
            }
        }

        public void Initialize() //http put (init model)
        {
            AvailableClasses = new ObservableCollection<Pair<string, int>>();
            ClassesImages = new ObservableCollection<RecognitionContract>();
            SelectedClassInfo = new ObservableCollection<RecognitionContract>();

            /*var content = new StringContent(JsonConvert.SerializeObject(modelPath + "&" + classLabels), Encoding.UTF8, "application/json");
            HttpResponseMessage httpResponse;
            try
            {
                httpResponse = client.PutAsync(url, content, cts.Token).Result;
            }
            catch (HttpRequestException)
            {
                disp.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show("NO CONNECTION", "Couldn't create model");
                }));
                return;
            }
            if (httpResponse.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<string>(httpResponse.Content.ReadAsStringAsync().Result);
                if (result == "SUCCESSFULLY CREATED MODEL: OK")
                    return;
                throw new HttpRequestException();
            }*/
        }

        public void Clear()
        {
            AvailableClasses.Clear();
            ClassesImages.Clear();
            SelectedClassInfo.Clear();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AvailableCLasses"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CLassesImages"));

        }

        public void ClearDataBase()
        {
            Task.Run(() =>
            {
                try
                {
                    var httpResponse = client.DeleteAsync(url).Result;
                }
                catch (AggregateException)
                {
                    disp.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show("NO CONNECTION", "Info");
                    }));
                }
            });
            Statistic = 0;
        }

        public void OpenDefault()
        {
            //Clear();
            imageDirectory = "images";
            //StatusMax = new DirectoryInfo(imageDirectory).GetFiles().Length;
            //PropertyChanged(this, new PropertyChangedEventArgs("StatusMax"));

        }

        public void Open(string selectedPath)
        {
            //Clear();
            imageDirectory = selectedPath;
            //StatusMax = new DirectoryInfo(selectedPath).GetFiles().Length;
            //PropertyChanged(this, new PropertyChangedEventArgs("StatusMax"));
        }

        public void Start()   //http request
        {
            isRunning = true;
            Task.Run(() =>
            {
                PostRequest();
            }).ContinueWith(t =>
            {
                isRunning = false;
            });
        }

        public void Stop()
        {
            cts.Cancel();
            cts.Dispose();
            cts = new CancellationTokenSource();
            isRunning = false;
        }

        /* public void ChangeCollectionResult(NNModel sender, ConcurrentQueue<RecognitionInfo> result) 
         {
             disp.BeginInvoke(new Action(() =>
             {
                 RecognitionInfo tmp;
                 result.TryDequeue(out tmp);
                 lock (db)
                 {
                     ClassesImages.Add(tmp);
                     PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ClassesImages"));
                     Task.Run(() =>
                     {
                         lock (db)
                         {
                             isWriting = true;
                             Blob resBlob = new Blob { Image = tmp.Image };
                             db.Add(new RecognitionImage
                             {
                                 Path = tmp.Path,
                                 Confidence = tmp.Confidence,
                                 Statistic = 0,
                                 ImageDetails = resBlob,
                                 Label = int.Parse(tmp.Class)
                             });
                             db.Blobs.Add(resBlob);
                             db.SaveChanges();
                         }
                     });
                 }

                 isWriting = false;
                 //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ClassesImages"));
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
         }*/

        public ObservableCollection<RecognitionContract> SelectAll(string classlabel) // add collection filter???
        {
            ObservableCollection<RecognitionContract> res = new ObservableCollection<RecognitionContract>();
            if (ClassesImages.Count() == 0)
                return res;
            foreach (var q in ClassesImages)
            {
                if (q.Class == classlabel)
                    res.Add(q);
            }
            return res;
        }

        public long GetStatistic(RecognitionContract selected) //get http add isGetting???
        {
            long res = 999999;
            try
            {
                string UrlStat = url + "/" + selected.Path;
                Trace.WriteLine(UrlStat);
                /*var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(UrlStat),
                    Content = new StringContent(JsonConvert.SerializeObject(selected), Encoding.UTF8, "application/json")
                };

                var httpResponse = client.SendAsync(request);
                var responseInfo = httpResponse.GetAwaiter().GetResult();*/
                var httpResponse = client.GetAsync(UrlStat).Result;
                var stats = JsonConvert.DeserializeObject<string>(httpResponse.Content.ReadAsStringAsync().Result);
                if (stats != null)
                {
                    //disp.BeginInvoke(new Action(() =>
                    //{
                        res = long.Parse(stats);
                        Statistic = res;
                        Trace.WriteLine("RES   " + res);
                    //}));
                }
                else
                {
                    Trace.WriteLine("stat == null");
                }
                
            }
            catch (AggregateException)
            {
                disp.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show("NO CONNECTION", "Info");
                }));
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Statistic"));
            return res;
        }

        private async void PostRequest()
        {
            try
            {
                //MessageBox.Show(imageDirectory);
                var images = from file in Directory.GetFiles(imageDirectory) // пустой путь - throw exception
                         where file.Contains(".jpg") ||
                                 file.Contains(".jpeg") ||
                                 file.Contains(".png")
                         select file;
                Dictionary<string, string> ToServer = new Dictionary<string, string>();
                foreach(var i in images)
                {
                    ToServer.Add(i, Convert.ToBase64String(File.ReadAllBytes(i)));
                }

                var content = new StringContent(JsonConvert.SerializeObject(ToServer), Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponse;
                try
                {
                    httpResponse = await client.PostAsync(url, content, cts.Token);
                }
                catch (HttpRequestException)
                {
                    await disp.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show("NO CONNECTION", "Warning");
                        Stop();
                    }));
                    return;
                }

                if (httpResponse.IsSuccessStatusCode)
                {
                    var items = JsonConvert.DeserializeObject<List<RecognitionContract>>(httpResponse.Content.ReadAsStringAsync().Result);
                    foreach (var item in items)
                    {
                        await disp.BeginInvoke(new Action(() =>  //await ???
                        {
                            ClassesImages.Add(item);
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ClassesImages"));
                            Pair<string, int> p;
                            try
                            {
                                p = AvailableClasses.Single(i => i.Item1 == item.Class);
                                p.Item2 += 1;
                            }
                            catch (InvalidOperationException)
                            {
                                AvailableClasses.Add(new Pair<string, int>(item.Class, 1));
                                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AvailableClasses"));
                            }
                        }));
                    }
                    isRunning = false;
                }
            }
            catch (OperationCanceledException)
            {
                await disp.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show("RECOGNITION STOPPED", "Warning");
                }));
            }
        }
    }
}
