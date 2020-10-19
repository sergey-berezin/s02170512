namespace RecognitionLibrary
{
    public struct RecognitionInfo
    {
        public RecognitionInfo(string v1, string v2, float v3) : this()
        {
            Path = v1;
            Class = v2;
            Confidence = v3;
        }

        public string Path { get; set; }
        public string Class { get; set; }
        public float Confidence { get; set; }

        public override string ToString()
        {
            return Path + "\t" + Class + "\t" + Confidence;
        }
    }
}
