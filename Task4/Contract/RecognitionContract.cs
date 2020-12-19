using System;
using System.IO;
using System.Diagnostics;

namespace Contracts
{
    public class RecognitionContract
    {
        /*TODO: Новый класс в контракты для контроллера
         
         рекогнишн инфо верни в библиотеку*/
        public RecognitionContract(string v1, string v2, float v3, string s)
        {
            Path = v1;
            Class = v2;
            Confidence = v3;
            Trace.WriteLine("Before Path");
            Trace.WriteLine(Path);
            Image = s;
        }
        public string Image { get; set; }
        public string Path { get; set; }
        public string Class { get; set; }
        public float Confidence { get; set; }

        public override string ToString()
        {
            return Path + "\t" + Class + "\t" + Confidence;
        }
    }
}

