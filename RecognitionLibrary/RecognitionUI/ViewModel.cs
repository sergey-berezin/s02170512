﻿namespace RecognitionUI
{
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;
    using RecognitionLibrary;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Reactive.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;

    public class ViewModel : INotifyPropertyChanged
    {
        private string selectedClass;

        private string modelPath;

        private string classLabels;

        private string imageDirectory;

        //private int recognitionStatus = 0;

        private static readonly HttpClient client = new HttpClient();   //instead of model

        private static readonly string url = "http://localhost:61924/recognition";

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

        public RecognitionInfo SelectedItem { get; set; }

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
            ClassesImages = new ObservableCollection<RecognitionInfo>();
            SelectedClassInfo = new ObservableCollection<RecognitionInfo>();

            var content = new StringContent(JsonConvert.SerializeObject(modelPath + "&" + classLabels), Encoding.UTF8, "application/json");
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
            }
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
            imageDirectory = NNModel.DefaultImageDir;
            //StatusMax = new DirectoryInfo(imageDirectory).GetFiles().Length;
            //PropertyChanged(this, new PropertyChangedEventArgs("StatusMax"));

        }

        public void Open(string selectedPath)
        {
            Clear();
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

        public long GetStatistic(RecognitionInfo selected) //get http add isGetting???
        {
            long res = 999999;
            Task.Run(() => 
            {
                try
                {
                    var httpResponse = client.GetAsync(url).Result;
                    var stats = JsonConvert.DeserializeObject<long>(httpResponse.Content.ReadAsStringAsync().Result);
                    if (stats != 999999)
                    {
                        disp.BeginInvoke(new Action(() =>
                        {
                            res  = stats;
                        }));
                    }
                }
                catch (AggregateException)
                {
                    disp.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show("NO CONNECTION", "Info");
                    }));
                }
            });
            Statistic = res;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Statistic"));
            return res;
        }

        private async void PostRequest()
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(imageDirectory), Encoding.UTF8, "application/json");
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
                    var items = JsonConvert.DeserializeObject<List<RecognitionInfo>>(httpResponse.Content.ReadAsStringAsync().Result);
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
