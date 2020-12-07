using System.Collections.Generic;

namespace Builder.Build
{
    public interface IBuildScript
    {
        List<GUIElementData> GuiData { get; }

        void Initialize();

        void OnPartSuccess();

        void OnStepSuccess();

        void OnStepFailure();

        void SetGUIData(Dictionary<string, object> results);

        void Build(string name);

        bool IsUsingStep(int step);
    }
}