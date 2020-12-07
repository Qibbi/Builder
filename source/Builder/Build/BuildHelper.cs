using System.IO;
using System.Windows.Media;

namespace Builder.Build
{
    public static class BuildHelper
    {
        private static readonly Color _colorCritical = Colors.Red;
        private static readonly Color _colorError = Colors.DarkMagenta;
        private static readonly Color _colorWarning = Colors.DarkGoldenrod;
        private static readonly string[] _emptyFolderNames = new string[0];

        private static IBuildGUI _parent;

        public static bool UseBenchmarking { get; set; } = true;
        public static bool IsAutomaticallyResettingBuildHelper { get; set; } = true;
        public static bool IsAutomaticallyResettingBuildLog { get; set; } = true;
        public static int CurrentBuildSteps { get; set; } = 0;
        public static int CurrentStep { get; private set; }
        public static string BuildTarget { get; private set; } = string.Empty;
        public static bool IsBuildComplete => CurrentStep == CurrentBuildSteps;

        public static void SetBuildGUI(IBuildGUI gui)
        {
            if (_parent != null || gui == null)
            {
                return;
            }
            _parent = gui;
            _parent.SetColors(_colorCritical, _colorError, _colorWarning);
        }

        public static void Reset()
        {
            CurrentStep = 0;
        }

        public static void StartBuild(string target)
        {
            if (IsAutomaticallyResettingBuildHelper)
            {
                Reset();
                BuildTarget = target;
            }
            if (IsAutomaticallyResettingBuildLog)
            {
                _parent.ResetBuildLog();
            }
            if (!_parent.CanBuild)
            {
                _parent.DisplayLine("Please select a project.");
            }
            _parent.Reset();
        }

        public static void OnBuildComplete()
        {
            _parent.OnBuildComplete();
        }

        public static string[] GetFolderNames(string path)
        {
            if (Directory.Exists(path))
            {
                return Directory.GetDirectories(path);
            }
            return _emptyFolderNames;
        }

        public static void DisplayLine(string text)
        {
            _parent.DisplayLine(text);
        }

        public static int GetNextExecutableStep()
        {
            while (!_parent.ScriptInterface.IsUsingStep(CurrentStep))
            {
                ++CurrentStep;
                if (IsBuildComplete)
                {
                    CurrentStep = -1;
                    break;
                }
            }
            if (IsBuildComplete)
            {
                CurrentStep = -1;
            }
            return CurrentStep;
        }

        public static void RunStep(StepType type, object args)
        {
            _parent.RunBuildStep(CurrentStep++, type, args);
        }

        public static bool RunStepPart(bool isLastPart, StepType type, object args)
        {
            if (isLastPart)
            {
                return _parent.RunBuildStepPart(CurrentStep++, isLastPart, type, args);
            }
            return _parent.RunBuildStepPart(CurrentStep, isLastPart, type, args);
        }

        public static void SelectMap(string map)
        {
            _parent.SelectMap(map);
        }

        public static void DeselectMap(string map)
        {
            _parent.DeselectMap(map);
        }
    }
}