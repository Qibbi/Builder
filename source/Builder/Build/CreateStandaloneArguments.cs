namespace Builder.Build
{
    public class CreateStandaloneArguments
    {
        public string InstallPath { get; }
        public string Language { get; }
        public string Version { get; }
        public string SkuDefText { get; }

        public CreateStandaloneArguments(string installPath, string language, string version, string skuDefText)
        {
            InstallPath = installPath;
            Language = language;
            Version = version;
            SkuDefText = skuDefText;
        }
    }
}
