namespace Builder.Build
{
    public class MergeFilesArguments
    {
        public string Target { get; }
        public string[] Sources { get; set; }

        public MergeFilesArguments(string target)
        {
            Target = target;
        }
    }
}
