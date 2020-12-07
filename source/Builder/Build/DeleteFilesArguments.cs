namespace Builder.Build
{
    public class DeleteFilesArguments
    {
        public string Target { get; }

        public DeleteFilesArguments(string target)
        {
            Target = target;
        }
    }
}
