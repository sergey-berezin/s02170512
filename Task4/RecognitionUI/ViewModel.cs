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

        public static string curDir = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.FullName;

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
            imageDirectory = Path.Combine(curDir, "images");
        }

        public void Open(string selectedPath)
        {
            imageDirectory = selectedPath;
        }

        public void Start()   //http request
        {
            Clear();

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

        public async void GetStatistic(RecognitionContract selected)
        {
            List<string> statList = new List<string>() 
            { 
                 selected.Path,
                 selected.Image
            };
            var content = new StringContent(JsonConvert.SerializeObject(statList), Encoding.UTF8, "application/json");
            HttpResponseMessage httpResponse;
            try
            {
                httpResponse = await client.PostAsync(url + "/statistic", content, cts.Token);
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
                var stats = JsonConvert.DeserializeObject<string>(httpResponse.Content.ReadAsStringAsync().Result);
                Statistic = long.Parse(stats); ;
                Trace.WriteLine("RES   ");
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Statistic"));
            }
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
