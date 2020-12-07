namespace Builder.Build
{
    public class WriteFileArguments
    {
        public string Target { get; }
        public string Content { get; set; }

        public WriteFileArguments(string target)
        {
            Target = target;
        }
    }
}
