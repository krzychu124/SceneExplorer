using SceneExplorer.ToBeReplaced.Windows;
using UnityEngine;

namespace SceneExplorer.ToBeReplaced.Helpers.ContentSections
{
    public class TagSection : ISection
    {
        private static GUILayoutOption[] _options = new[] { GUILayout.MaxWidth(40) };
        private string _title;

        public IParentInspector ParentInspector { get; set; }

        public TagSection(string title) {
        _title = $"[T] {title}";
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
