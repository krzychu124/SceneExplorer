using SceneExplorer.ToBeReplaced.Windows;
using UnityEngine;

namespace SceneExplorer.ToBeReplaced.Helpers.ContentSections
{
    public class NotSupportedData : ISection, ISectionItem
    {
        private readonly string _title;

        public IParentInspector ParentInspector { get; set; }

        public NotSupportedData(string title, bool isItem = false) {
        _title = $"{(!isItem ? "[N] ": string.Empty)}{title}";
    }

        public void Render() {
        GUILayout.BeginHorizontal();
        GUILayout.Label(_title, UIStyle.Instance.reducedPaddingLabelStyle, options: null);
        GUILayout.EndHorizontal();
    }

        public void UpdateBindings(bool refreshOnly = false) {
    }
    }
}
