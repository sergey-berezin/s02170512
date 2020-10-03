using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RecognitionLibrary
{
    public class NNModel
    {
        private InferenceSession Session;

        int TargetWidth;

        int TargetHeight;

        bool grayscaleMode;

        public readonly string[] classLabels;

        public string DefaultImageDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.Parent.FullName;

        private string ModelPath { get; set; }

        public NNModel(string modelPath, string labelPath, int size = 28, bool grayMode=false)
        {
            ModelPath = modelPath;
            DefaultImageDir = Path.Combine(DefaultImageDir, "images");
            Session = new InferenceSession(ModelPath);
            classLabels = File.ReadAllLines(labelPath);
            TargetHeight = size;
            TargetWidth = size;
            grayscaleMode = grayMode;
        }

        public DenseTensor<float> PreprocessImage(string ImagePath)
        {
            var image = Image.Load<Rgb24>(ImagePath);

            image.Mutate(x => x
                .Grayscale()
                .Resize(new ResizeOptions { Size = new Size(TargetWidth, TargetHeight) })
            );

            if (grayscaleMode)
                image.Mutate(x => x.Grayscale());

            var input = new DenseTensor<float>(new[] { 1, 1, TargetHeight, TargetWidth });
            for (int y = 0; y < TargetHeight; y++)
            {
                Span<Rgb24> pixelSpan = image.GetPixelRowSpan(y);
                for (int x = 0; x < TargetWidth; x++)
                {
                    input[0, 0, y, x] = (pixelSpan[x].R / 255f);
                }
            }
            return input;
        }

        public string ProcessImage(string img_path)
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

            return classLabels[softmax.ToList().IndexOf(softmax.Max())];
        }

        public ConcurrentQueue<string> MakePrediction(string dirpath)
        {
            Console.WriteLine("If you want to stop recognition press ESС");
            CancellationTokenSource cancel = new CancellationTokenSource();
            CancellationToken token = cancel.Token;
            ConcurrentQueue<string> CQ = new ConcurrentQueue<string>();
            try
            {
                IEnumerable<string> images;
                ParallelOptions po = new ParallelOptions();

                po.CancellationToken = token;
                po.MaxDegreeOfParallelism = Environment.ProcessorCount;

                images = from file in Directory.GetFiles(dirpath) //пустой путь - throw exception
                         where file.Contains(".jpg") ||
                                 file.Contains(".jpeg") ||
                                 file.Contains(".png")
                         select file;

                if (images.Count() == 0)            //Если пользователь передал пустуцю директорию, меняем на дефолтную директори проекта с тестовыми изображениями
                {
                    Console.WriteLine("Your directory is empty. Change to embedded directory with images");
                    images = from file in Directory.GetFiles(DefaultImageDir)
                             where file.Contains(".jpg") ||
                                     file.Contains(".jpeg") ||
                                     file.Contains(".png")
                             select file;
                }

                var interruption = Task.Run(() =>
                {
                    while (Console.ReadKey(true).Key != ConsoleKey.Escape) { }  //прерывание по нажатию ESС  
                    cancel.Cancel();
                });

                Console.WriteLine("Predicting contents of images...");

               var tasks = Parallel.ForEach<string>(images, po, img =>
               {
                    string[] name = img.Split("\\");
                   CQ.Enqueue(name[name.Length - 1] + "\t" + ProcessImage(img));
               });

            }
            catch (OperationCanceledException e)
            {
                Console.WriteLine("-----------------Interrupted-----------------");
            }
            catch (Exception e) when (e is ArgumentException || e is IOException )
            {
                Console.WriteLine(e.Message);
            };

            return CQ;
        }

    }
}
