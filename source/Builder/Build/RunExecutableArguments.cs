namespace Builder.Build
{
    public class RunExecutableArguments
    {
        public string FileName { get; set; }
        public string Args { get; set; } = string.Empty;
        public bool IsRedirectingOutput { get; set; } = true;
        public bool IsCreatingWindow { get; set; }

        public RunExecutableArguments(string fileName)
        {
            FileName = fileName;
        }
    }
}
