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

        public static string DefaultImageDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.Parent.FullName;

        public event UserMessageEventHandler MessageToUser;

        public event ResultEventHandler OutputResult;

        public string ImageDirectory { get; set; }

        public NNModel(string modelPath, string labelPath, string imageDirectory = "", int size = 28, bool grayMode = false)
        {
            ModelPath = modelPath;
            DefaultImageDir = Path.Combine(DefaultImageDir, "images");
            Session = new InferenceSession(ModelPath);
            ClassLabels = File.ReadAllLines(labelPath);
            targetHeight = size;
            targetWidth = size;
            grayscaleMode = grayMode;
            CQ = new ConcurrentQueue<RecognitionInfo>();
            cancel = new CancellationTokenSource();
            ImageDirectory = imageDirectory;
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

            RecognitionInfo recognitionResult = new RecognitionInfo(img_path, ClassLabels[softmax.ToList().IndexOf(softmax.Max())], softmax.Max());

            return recognitionResult;
        }

        public ConcurrentQueue<RecognitionInfo> MakePrediction()
        {
            MessageToUser?.Invoke(this, "If you want to stop recognition press ctrl + C");
            CancellationToken token = cancel.Token;
            try
            {
                IEnumerable<string> images;
                ParallelOptions po = new ParallelOptions();

                po.CancellationToken = token;
                po.MaxDegreeOfParallelism = Environment.ProcessorCount;


                if (ImageDirectory == "") // Если пользователь передал пустую директорию, меняем на дефолтную директори проекта с тестовыми изображениями
                {
                    MessageToUser?.Invoke(this, "You haven't set the path. Changed to embedded directory with images");
                    images = from file in Directory.GetFiles(DefaultImageDir)
                             where file.Contains(".jpg") ||
                                     file.Contains(".jpeg") ||
                                     file.Contains(".png")
                             select file;
                }
                images = from file in Directory.GetFiles(ImageDirectory) // пустой путь - throw exception
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
                var tasks = Parallel.ForEach<string>(images, po, img =>
                    {
                        CQ.Enqueue(ProcessImage(img));
                        Thread.Sleep(1000);
                        OutputResult?.Invoke(this, CQ);
                    });
            
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
