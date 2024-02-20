using UnityEngine;

namespace SceneExplorer.ToBeReplaced.Helpers.ContentItems
{
    public class TextItem : ISectionItem
    {
        private static GUILayoutOption[] _options = new[] { GUILayout.MaxWidth(40) };
        private readonly string _title;
        private readonly string _value;
        private readonly bool _isEnum;

        public TextItem(string title, string value, bool isEnum = false) {
        _title = $"{title}{(!string.IsNullOrEmpty(value) ? ":" : string.Empty)}";
        _value = value;
        _isEnum = isEnum;
    }

        public void Render() {
        GUILayout.BeginHorizontal(options: null);
        GUILayout.Label(_title, UIStyle.Instance.reducedPaddingLabelStyle, options: null);
        GUILayout.Label(_value, _isEnum ? UIStyle.Instance.enumValueStyle : GUI.skin.label, options: null);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }
    }
}
