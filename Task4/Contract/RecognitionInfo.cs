using System;
using System.IO;

namespace Contracts
{
    public class RecognitionInfo
    {
        public RecognitionInfo(string v1, string v2, float v3)
        {
            Path = v1;
            Class = v2;
            Confidence = v3;
            Image = File.ReadAllBytes(Path);
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
