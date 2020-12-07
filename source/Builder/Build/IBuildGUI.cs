using System.Windows.Media;

namespace Builder.Build
{
    public interface IBuildGUI
    {
        IBuildScript ScriptInterface { get; }
        bool CanBuild { get; }

        void RunBuildStep(int step, StepType stepType, object args);

        bool RunBuildStepPart(int step, bool isLastPart, StepType stepType, object args);

        void Reset();

        void DisplayLine(string text);

        void ResetBuildLog();

        void OnBuildComplete();

        void SetColors(Color critical, Color error, Color warning);

        void SelectMap(string map);

        void DeselectMap(string map);
    }
}
