namespace RecognitionLibrary
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ML.OnnxRuntime;
    using Microsoft.ML.OnnxRuntime.Tensors;
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.PixelFormats;
    using SixLabors.ImageSharp.Processing;
    

    public delegate void UserMessageEventHandler(NNModel sender, string Message);

    public delegate void ResultEventHandler(NNModel sender, ConcurrentQueue<RecognitionInfo> result);

    public class NNModel
    {
        private InferenceSession Session;

        private int targetWidth;

        private int targetHeight;

        private bool grayscaleMode;

        private CancellationTokenSource cancel;

        private string ModelPath { get; set; }

        public ConcurrentQueue<RecognitionInfo> CQ { get; }

        public string[] ClassLabels { get; }

        public static string DefaultImageDir = Directory.GetCurrentDirectory();

        public event UserMessageEventHandler MessageToUser;

        public event ResultEventHandler OutputResult;

        public string ImageDirectory { get; set; }

        public RecognitionLibraryContext recognitionLibraryContext;

        public NNModel(string modelPath, string labelPath, string imageDirectory = "", int size = 28, bool grayMode = false)
        {
            if (modelPath == "")
            {
                ModelPath = Path.Combine(DefaultImageDir, "mnist-8.onnx");
                labelPath = Path.Combine(DefaultImageDir, "classlabel.txt");
                Trace.WriteLine("Model = " + ModelPath);
                Trace.WriteLine("label = " + labelPath);
            }
            else
                ModelPath = modelPath;

            //DefaultImageDir = Path.Combine(DefaultImageDir, "images");
            Session = new InferenceSession(ModelPath);
            ClassLabels = File.ReadAllLines(labelPath);
            targetHeight = size;
            targetWidth = size;
            grayscaleMode = grayMode;
            CQ = new ConcurrentQueue<RecognitionInfo>();
            cancel = new CancellationTokenSource();
            ImageDirectory = imageDirectory;
            recognitionLibraryContext = new RecognitionLibraryContext();

        }

        public DenseTensor<float> PreprocessImage(string ImagePath)
        {
            var image = Image.Load<Rgb24>(ImagePath);

            image.Mutate(x => x
                .Grayscale()
                .Resize(new ResizeOptions { Size = new Size(targetWidth, targetHeight) })
            );

            if (grayscaleMode)
                image.Mutate(x => x.Grayscale());

            var input = new DenseTensor<float>(new[] { 1, 1, targetHeight, targetWidth });
            for (int y = 0; y < targetHeight; y++)
            {
                Span<Rgb24> pixelSpan = image.GetPixelRowSpan(y);
                for (int x = 0; x < targetWidth; x++)
                {
                    input[0, 0, y, x] = (pixelSpan[x].R / 255f);
                }
            }
            return input;
        }

        public DenseTensor<float> PreprocessImage(byte[] im)
        {
            var image = Image.Load<Rgb24>(im);

            image.Mutate(x => x
                .Grayscale()
                .Resize(new ResizeOptions { Size = new Size(targetWidth, targetHeight) })
            );

            if (grayscaleMode)
                image.Mutate(x => x.Grayscale());

            var input = new DenseTensor<float>(new[] { 1, 1, targetHeight, targetWidth });
            for (int y = 0; y < targetHeight; y++)
            {
                Span<Rgb24> pixelSpan = image.GetPixelRowSpan(y);
                for (int x = 0; x < targetWidth; x++)
                {
                    input[0, 0, y, x] = (pixelSpan[x].R / 255f);
                }
            }
            return input;
        }

        public RecognitionInfo ProcessImage(string img_path)
        {
            var input = PreprocessImage(img_path);
            var inputs = new List<NamedOnnxValue>
            {
                 NamedOnnxValue.CreateFromTensor(Session.InputMetadata.Keys.First(), input)
            };
            var Results = Session.Run(inputs);

            // Получаем 10 выходов и считаем для них softmax
            var output = Results.First().AsEnumerable<float>().ToArray();
            var sum = output.Sum(x => (float)Math.Exp(x));
            var softmax = output.Select(x => (float)Math.Exp(x) / sum);

            RecognitionInfo tmp = new RecognitionInfo(img_path, ClassLabels[softmax.ToList().IndexOf(softmax.Max())], softmax.Max());

            lock (recognitionLibraryContext)
            {
                Blob resBlob = new Blob { Image = tmp.Image };
                recognitionLibraryContext.Add(new RecognitionImage
                {
                    Path = tmp.Path,
                    Confidence = tmp.Confidence,
                    Statistic = 0,
                    ImageDetails = resBlob,
                    Label = int.Parse(tmp.Class)
                });
                recognitionLibraryContext.Blobs.Add(resBlob);
                recognitionLibraryContext.SaveChanges();
            }

            return tmp;
        }

        public RecognitionInfo ProcessImage(byte[] im, string path)
        {
            var input = PreprocessImage(im);
            var inputs = new List<NamedOnnxValue>
            {
                 NamedOnnxValue.CreateFromTensor(Session.InputMetadata.Keys.First(), input)
            };
            var Results = Session.Run(inputs);

            // Получаем 10 выходов и считаем для них softmax
            var output = Results.First().AsEnumerable<float>().ToArray();
            var sum = output.Sum(x => (float)Math.Exp(x));
            var softmax = output.Select(x => (float)Math.Exp(x) / sum);

            RecognitionInfo tmp = new RecognitionInfo(path, ClassLabels[softmax.ToList().IndexOf(softmax.Max())], softmax.Max());
            tmp.Image = im;
            lock (recognitionLibraryContext)
            {
                Blob resBlob = new Blob { Image = tmp.Image };
                recognitionLibraryContext.Add(new RecognitionImage
                {
                    Path = tmp.Path,
                    Confidence = tmp.Confidence,
                    Statistic = 0,
                    ImageDetails = resBlob,
                    Label = int.Parse(tmp.Class)
                });
                recognitionLibraryContext.Blobs.Add(resBlob);
                recognitionLibraryContext.SaveChanges();
            }
            Trace.WriteLine("process " + path);
            return tmp;
        }

        public ConcurrentQueue<RecognitionInfo> MakePrediction(string path)  //should check dbcontext 
        {
            MessageToUser?.Invoke(this, "If you want to stop recognition press ctrl + C");
            CancellationToken token = cancel.Token;
            try
            {
                IEnumerable<string> images;
                ParallelOptions po = new ParallelOptions();

                po.CancellationToken = token;
                po.MaxDegreeOfParallelism = Environment.ProcessorCount;


                if (path == "") // Если пользователь передал пустую директорию, меняем на дефолтную директори проекта с тестовыми изображениями
                {
                    MessageToUser?.Invoke(this, "You haven't set the path. Changed to embedded directory with images");
                    images = from file in Directory.GetFiles(DefaultImageDir)
                             where file.Contains(".jpg") ||
                                     file.Contains(".jpeg") ||
                                     file.Contains(".png")
                             select file;
                }
                images = from file in Directory.GetFiles(path) // пустой путь - throw exception
                         where file.Contains(".jpg") ||
                                 file.Contains(".jpeg") ||
                                 file.Contains(".png")
                         select file;

                if (images.Count() == 0) // Если пользователь передал пустую директорию, меняем на дефолтную директори проекта с тестовыми изображениями
                {
                    MessageToUser?.Invoke(this, "Your directory is empty. Change to embedded directory with images");
                    images = from file in Directory.GetFiles(DefaultImageDir)
                             where file.Contains(".jpg") ||
                                     file.Contains(".jpeg") ||
                                     file.Contains(".png")
                             select file;
                }
                List<string> paths = new List<string>();
                lock (recognitionLibraryContext)
                {
                    foreach (var image in images)
                    {
                        RecognitionInfo temp = new RecognitionInfo(image, "", 0);
                        var sameimage = recognitionLibraryContext.FindOne(temp);
                        if (sameimage != null)
                        {
                            sameimage.Statistic++;
                            recognitionLibraryContext.SaveChanges();
                        }
                        else
                            paths.Add(image);
                    }
                }

                var task = Task.Factory.StartNew(() =>
                {
                    Trace.WriteLine("StartNew");
                    foreach (var img in images)
                    {
                        var t = Task.Run(() =>
                        {
                                var tmp = ProcessImage(img);
                                CQ.Enqueue(tmp);
                                OutputResult?.Invoke(this, CQ);
                           
                            
                        }, cancel.Token);
                    }
                });
                task.Wait();
                Trace.WriteLine("After wait");
               
            }
            catch (OperationCanceledException)
            {
               MessageToUser?.Invoke(this, "-----------------Interrupted-----------------");
               Trace.WriteLine("-----------------Interrupted-----------------");
            }
            catch (Exception e) when (e is ArgumentException || e is IOException)
            {
                Trace.WriteLine(e.Message);
            };
            return CQ;
        }

        public ConcurrentQueue<RecognitionInfo> MakePrediction(Dictionary<string, string> info) //doesnt check in db context
        {
            MessageToUser?.Invoke(this, "If you want to stop recognition press ctrl + C");
            CancellationToken token = cancel.Token;
            try
            {
                ParallelOptions po = new ParallelOptions();

                po.CancellationToken = token;
                po.MaxDegreeOfParallelism = Environment.ProcessorCount;

                Dictionary<string, string> temp_info = new Dictionary<string, string>();
                lock (recognitionLibraryContext)
                {
                    foreach (var image in info)
                    {
                        RecognitionInfo temp = new RecognitionInfo(image.Key, "", 0);
                        temp.Image = Convert.FromBase64String(image.Value);
                        var sameimage = recognitionLibraryContext.FindOne(temp);
                        if (sameimage != null)
                        {
                            sameimage.Statistic++;
                            Trace.WriteLine("Statisticv   " + sameimage.Statistic);
                            recognitionLibraryContext.SaveChanges();
                        }
                        else
                        {
                            Trace.WriteLine("Const temp_info       "  + image.Key);
                            temp_info.Add(image.Key, image.Value);
                        }
                    }
                }


                var task = Task.Factory.StartNew(() =>
                {
                    Trace.WriteLine("StartNew");
                    foreach (var img in temp_info)
                    {
                        var t = Task.Run(() =>
                        {
                            var tmp = ProcessImage(Convert.FromBase64String(img.Value), img.Key);
                            Trace.WriteLine("Start nFactory = " + tmp.Path);
                            CQ.Enqueue(tmp);
                            OutputResult?.Invoke(this, CQ);
                        }, cancel.Token);
                        Task.WaitAll(t);
                    }
                });
                Task.WaitAll(task);

            }
            catch (OperationCanceledException)
            {
                MessageToUser?.Invoke(this, "-----------------Interrupted-----------------");
                Trace.WriteLine("-----------------Interrupted-----------------");
            }
            catch (Exception e) when (e is ArgumentException || e is IOException)
            {
                Trace.WriteLine(e.Message);
            };
            return CQ;
        }


        public void StopRecognition()
        {
            cancel.Cancel();
            cancel = new CancellationTokenSource();
        }
    }
}
