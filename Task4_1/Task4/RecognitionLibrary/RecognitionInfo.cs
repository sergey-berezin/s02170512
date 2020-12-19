using System;
using System.IO;
using System.Diagnostics;

namespace RecognitionLibrary
{
    public class RecognitionInfo
    {
        /*TODO: Новый класс в контракты для контроллера
         
         рекогнишн инфо верни в библиотеку*/
        public RecognitionInfo(string v1, string v2, float v3)
        {
            Path = v1;
            Class = v2;
            Confidence = v3;
            Trace.WriteLine("Before Path");
            Trace.WriteLine(Path);
            try { Image = File.ReadAllBytes(v1); }
            catch (Exception e)
            { Trace.WriteLine(e.Message); }
        }
        public byte[] Image { get; set; }
        public string Path { get; set; }
        public string Class { get; set; }
        public float Confidence { get; set; }

        public override string ToString()
        {
            return Path + "\t" + Class + "\t" + Confidence;
        }
    }
}
