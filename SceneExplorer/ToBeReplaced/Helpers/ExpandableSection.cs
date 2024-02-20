using Game.Prefabs;
using SceneExplorer.ToBeReplaced.Windows;
using Unity.Entities;
using UnityEngine;
using Object = System.Object;

namespace SceneExplorer.ToBeReplaced.Helpers
{
    public interface ISection
    {
        void Render();
        void UpdateBindings(bool refreshOnly = false);

        IParentInspector ParentInspector { get; set; }
    }

    public interface ISectionItem
    {
        void Render();
    }

    public interface IRenderableControl
    {
        void Render();
    }

    public interface IInteractiveSectionItem : ISectionItem
    {
        IParentInspector ParentInspector { get; set; }
    }

    public class ExpandableSection : ISection
    {
        private readonly string _title;
        private bool _expanded;
        private ValueBinding<string> _contentBinding;

        public IParentInspector ParentInspector { get; set; }

        public ExpandableSection(string title, ValueBinding<string> contentBinding, bool expanded = false) {
        _title = title;
        _contentBinding = contentBinding;
        _expanded = expanded;
    }

        public void Render() {
        GUILayout.BeginHorizontal();
        bool prevExpanded = _expanded;
        if (GUILayout.Button(_expanded ? "▼" : "▶", UIStyle.Instance.iconButton, new GUILayoutOption[] { GUILayout.MaxWidth(22), GUILayout.MaxHeight(22) }))
        {
            _expanded = !_expanded;
        }
        GUILayout.Space(4);
        GUILayout.Label(_title, options: null);
        GUILayout.EndHorizontal();

        if (prevExpanded)
        {
            // GUI.enabled = false;
            bool temp = GUI.skin.label.wordWrap;
            GUI.skin.label.wordWrap = true;
            GUILayout.Label(_contentBinding.Value, options: null);
            GUI.skin.label.wordWrap = temp;
            // GUILayout.TextArea(_contentBinding.Value);
            // GUI.enabled = true;
        }
    }

        public void UpdateBindings(bool refreshOnly) {
        _contentBinding.Update();
    }
    }
}