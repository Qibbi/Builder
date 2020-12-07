using System;

namespace Builder.Build
{
    public enum GUIElementDataType
    {
        CheckBox,
        Input,
        ComboBox,
        Button,
        Separator,
        Label
    }

    public class GUIElementData
    {
        private GUIElementDataType _controlType;

        public string DataKey { get; }
        public string Label { get; set; }
        public GUIElementDataType ControlType
        {
            get => _controlType;
            set
            {
                if (value != GUIElementDataType.CheckBox &&
                    value != GUIElementDataType.Input &&
                    value != GUIElementDataType.ComboBox &&
                    value != GUIElementDataType.Button &&
                    value != GUIElementDataType.Separator &&
                    value != GUIElementDataType.Label) throw new ArgumentException($"Invalid type '{value}'.", nameof(value));
                _controlType = value;
            }
        }
        public Action<GUIElementData> ExecuteAction { get; }
        public Func<bool> IsEnabled { get; set; }

        public GUIElementData(string key, string label, GUIElementDataType type)
        {
            DataKey = key;
            Label = label;
            ControlType = type;
            ExecuteAction = Nop;
            IsEnabled = IsTrue;
        }

        public GUIElementData(string key, string label, GUIElementDataType type, Action<GUIElementData> executeAction)
        {
            DataKey = key;
            Label = label;
            ControlType = type;
            ExecuteAction = executeAction;
            IsEnabled = IsTrue;
        }

        public GUIElementData(string key, string label, GUIElementDataType type, Action<GUIElementData> executeAction, Func<bool> isEnabled)
        {
            DataKey = key;
            Label = label;
            ControlType = type;
            ExecuteAction = executeAction;
            IsEnabled = isEnabled;
        }

        private void Nop(GUIElementData elementData)
        {
            BuildHelper.GetNextExecutableStep();
        }

        private bool IsTrue()
        {
            return true;
        }
    }
}