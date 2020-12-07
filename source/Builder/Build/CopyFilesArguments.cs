namespace Builder.Build
{
    public class CopyFilesArguments
    {
        public string Target { get; }
        public string Source { get; }
        public string Include { get; set; }
        public string Exclude { get; set; }

        public CopyFilesArguments(string target, string source)
        {
            Target = target;
            Source = source;
        }
    }
}
