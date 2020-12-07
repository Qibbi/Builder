using Builder.Build;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Builder
{
    public class BuildScript : IBuildScript
    {
        private Dictionary<string, object> _currentGuiData;

        public List<GUIElementData> GuiData { get; }
        private string this[string key] => (string)_currentGuiData[key];

        public BuildScript()
        {
            GuiData = new List<GUIElementData>
            {
                new GUIElementData("project", "Project", GUIElementDataType.ComboBox),
                new GUIElementData("", "Language:", GUIElementDataType.Label),
                new GUIElementData("language", "Language", GUIElementDataType.ComboBox),
                new GUIElementData("", "", GUIElementDataType.Separator),
                new GUIElementData("", "Debug", GUIElementDataType.Label),
                new GUIElementData("linked", "Do not link stream", GUIElementDataType.CheckBox),
                new GUIElementData("", "", GUIElementDataType.Separator),
                new GUIElementData("step0", "Clear output Folder", GUIElementDataType.CheckBox, ClearOutputFolder),
                new GUIElementData("", "", GUIElementDataType.Separator),
                new GUIElementData("step1", "Build Global Data", GUIElementDataType.CheckBox, BuildGlobal),
                new GUIElementData("step2", "Build Static Data", GUIElementDataType.CheckBox, BuildStatic),
                new GUIElementData("step3", "Build Static Low LOD Data", GUIElementDataType.CheckBox, BuildStaticLow),
                new GUIElementData("", "", GUIElementDataType.Separator),
                new GUIElementData("step4", "Copy other Data", GUIElementDataType.CheckBox, CopyOtherData),
                new GUIElementData("step5", "Copy language Data", GUIElementDataType.CheckBox, CopyLanguageData),
                new GUIElementData("step6", "Create standalone", GUIElementDataType.CheckBox, CreateStandalone),
                new GUIElementData("", "", GUIElementDataType.Separator),
                new GUIElementData("", "Game Path:", GUIElementDataType.Label),
                new GUIElementData("game", StaticPaths.GetDefaultGamePath(), GUIElementDataType.Input),
                new GUIElementData("", "Version:", GUIElementDataType.Label),
                new GUIElementData("version", "TSRAlpha", GUIElementDataType.Input)
            };
        }

        private void BuildGlobal(GUIElementData stepData)
        {
            RunExecutableArguments args = new RunExecutableArguments(StaticPaths.WrathEdPath)
            {
                Args = $"\"{StaticPaths.ConvertNameToGlobalXmlPath(BuildHelper.BuildTarget)}\" /dr:\"{StaticPaths.ConvertNameToXmlDataPath(BuildHelper.BuildTarget)}\" /iod:\"{StaticPaths.BuiltIntermediatePath}\" /od:\"{StaticPaths.ConvertNameToBuiltOutputDataPath(this["game"], BuildHelper.BuildTarget)}\" /ls:{!(bool)_currentGuiData["linked"]} /gui:false /pc:true /vf:true /ss:true /tl:9 /el:0"
            };
            BuildHelper.RunStep(StepType.RunExecutable, args);
        }

        private void BuildStatic(GUIElementData stepData)
        {
            RunExecutableArguments args = new RunExecutableArguments(StaticPaths.WrathEdPath)
            {
                // TODO:
                Args = $"\"{StaticPaths.ConvertNameToStaticXmlPath(BuildHelper.BuildTarget)}\" /dr:\"{StaticPaths.ConvertNameToXmlDataPath(BuildHelper.BuildTarget)}\" /iod:\"{StaticPaths.BuiltIntermediatePath}\" /od:\"{StaticPaths.ConvertNameToBuiltOutputDataPath(this["game"], BuildHelper.BuildTarget)}\" /ls:{!(bool)_currentGuiData["linked"]} /gui:false /pc:true /vf:true /ss:true /tl:9 /el:0 /art:\".\\Art\" /data:\".;.\\Mods\""
            };
            BuildHelper.RunStep(StepType.RunExecutable, args);
        }

        private void BuildStaticLow(GUIElementData stepData)
        {
            RunExecutableArguments args = new RunExecutableArguments(StaticPaths.WrathEdPath)
            {
                // TODO:
                Args = $"\"{StaticPaths.ConvertNameToStaticXmlPath(BuildHelper.BuildTarget)}\" /dr:\"{StaticPaths.ConvertNameToXmlDataPath(BuildHelper.BuildTarget)}\" /iod:\"{StaticPaths.BuiltIntermediatePath}\" /od:\"{StaticPaths.ConvertNameToBuiltOutputDataPath(this["game"], BuildHelper.BuildTarget)}\" /ls:{!(bool)_currentGuiData["linked"]} /gui:false /pc:true /vf:false /ss:true /tl:9 /el:0 /bcn:LowLOD /bps:\"{StaticPaths.BuiltPath}\\static.manifest\" /art:\".\\Art\" /data:\".;.\\Mods\""
            };
            BuildHelper.RunStep(StepType.RunExecutable, args);
        }

        private void ClearOutputFolder(GUIElementData stepData)
        {
            DeleteFilesArguments args = new DeleteFilesArguments(Path.Combine(StaticPaths.ConvertNameToBuiltOutputPath(this["game"], BuildHelper.BuildTarget), ".."));
            BuildHelper.RunStep(StepType.DeleteFiles, args);
        }

        private void CopyOtherData(GUIElementData stepData)
        {
            CopyFilesArguments args = new CopyFilesArguments(StaticPaths.ConvertNameToBuiltOutputPath(this["game"], BuildHelper.BuildTarget), StaticPaths.ConvertNameToMainPath(BuildHelper.BuildTarget))
            {
                Include = "*",
                Exclude = $"{Path.DirectorySeparatorChar}xml{Path.DirectorySeparatorChar}"
            };
            BuildHelper.RunStep(StepType.CopyFiles, args);
        }

        private void CopyLanguageData(GUIElementData stepData)
        {
            if (_currentGuiData.ContainsKey("language"))
            {
                CopyFilesArguments args = new CopyFilesArguments(StaticPaths.ConvertNameToBuiltOutputPath(this["game"], BuildHelper.BuildTarget, this["language"]), StaticPaths.ConvertNameToMainPath(BuildHelper.BuildTarget, this["language"]))
                {
                    Include = "*",
                    Exclude = $"{Path.DirectorySeparatorChar}xml{Path.DirectorySeparatorChar}"
                };
                BuildHelper.RunStep(StepType.CopyFiles, args);
            }
            else
            {
                BuildHelper.DisplayLine("No language selected.");
                BuildHelper.RunStep(StepType.StepOver, null);
            }
        }

        private void CreateStandalone(GUIElementData stepData)
        {
            if (_currentGuiData.ContainsKey("language"))
            {
                StringBuilder skuDef = new StringBuilder();
                skuDef.AppendLine($"set-exe {Path.Combine(BuildHelper.BuildTarget, "RetailExe", "cnc3game.dat")}");
                skuDef.AppendLine($"add-search-path {StaticPaths.ConvertNameToRelativePath(BuildHelper.BuildTarget)}");
                skuDef.AppendLine($"add-search-path {StaticPaths.ConvertNameToRelativePath(BuildHelper.BuildTarget, this["language"])}");
                CreateStandaloneArguments args = new CreateStandaloneArguments(this["game"], this["language"], this["version"], skuDef.ToString());
                BuildHelper.RunStep(StepType.CreateStandalone, args);
            }
            else
            {
                BuildHelper.DisplayLine("No language selected.");
                BuildHelper.RunStep(StepType.StepOver, null);
            }
        }

        private void BuildStep(int step)
        {
            if (step == -1)
            {
                BuildHelper.DisplayLine("No steps selected.");
                OnStepSuccess();
                return;
            }
            GUIElementData stepData = GuiData.Where(x => x.DataKey == "step" + step).FirstOrDefault();
            BuildHelper.DisplayLine($"{stepData.Label}...");
            stepData.ExecuteAction(stepData);
        }

        public void Initialize()
        {
            BuildHelper.IsAutomaticallyResettingBuildHelper = true;
            BuildHelper.UseBenchmarking = true;
        }

        public void OnPartSuccess()
        {
        }

        public void OnStepSuccess()
        {
            if (BuildHelper.GetNextExecutableStep() != -1)
            {
                BuildStep(BuildHelper.CurrentStep);
            }
            else
            {
                BuildHelper.OnBuildComplete();
            }
        }

        public void OnStepFailure()
        {
            BuildHelper.DisplayLine($"Build failed on step {BuildHelper.CurrentStep}.");
        }

        public void SetGUIData(Dictionary<string, object> results)
        {
            _currentGuiData = results;
        }

        public void Build(string name)
        {
            BuildHelper.CurrentBuildSteps = _currentGuiData.Where(x => x.Key.StartsWith("step")).Count();
            BuildStep(BuildHelper.GetNextExecutableStep());
        }

        public bool IsUsingStep(int step)
        {
            if (_currentGuiData.TryGetValue("step" + step, out object value))
            {
                return (bool)value;
            }
            return true;
        }
    }
}
