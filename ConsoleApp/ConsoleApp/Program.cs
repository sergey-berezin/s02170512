using RecognitionLibrary;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace ConsoleApp
{
    class Program
    {
        public static void PrintMessageToUser(NNModel m, string Message)
        {
            Console.WriteLine(Message);
        }

        public static void PrintResult(NNModel sender, ConcurrentQueue<RecognitionInfo> result)
        {
            RecognitionInfo tmp;
            result.TryDequeue(out tmp);
            Console.WriteLine(tmp);

        }

        static void Main(string[] args)
        {
            //Console.WriteLine("If you want to stop recognition press ESС");
            string img = Console.ReadLine();
            string curDir = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.Parent.FullName;

            NNModel Mnist = new NNModel(Path.Combine(curDir, "mnist-8.onnx"), Path.Combine(curDir, "classlabel.txt"));
            Mnist.MessageToUser += PrintMessageToUser;
            Mnist.OutputResult += PrintResult;
            var t = Task.Run(() => { return Mnist.MakePrediction(img); }).Result;
            
        }
    }
}
