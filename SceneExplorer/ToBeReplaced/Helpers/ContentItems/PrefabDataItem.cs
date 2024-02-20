using Game.Prefabs;
using SceneExplorer.ToBeReplaced.Windows;
using Unity.Entities;
using UnityEngine;

namespace SceneExplorer.ToBeReplaced.Helpers.ContentItems
{
    public class PrefabDataItem : IInteractiveSectionItem
    {
        private readonly string _title;
        private readonly PrefabData _value;
        private readonly PrefabBase _prefab;
        private readonly string _prefabTypeName;

        public IParentInspector ParentInspector { get; set; }

        public PrefabDataItem(string title, PrefabData value) {
        _title = $"{title}: ";
        _value = value;
        PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
        if (prefabSystem.TryGetPrefab(value, out PrefabBase prefab))
        {
            _prefab = prefab;
            _prefabTypeName = prefab.GetType().Name;
        }
    }

        public void Render() {
        GUILayout.BeginHorizontal(UIStyle.Instance.collapsibleContentStyle, options: null);

        GUILayout.Label($"{_title}", UIStyle.Instance.reducedPaddingLabelStyle, options: null);
        GUILayout.Label($"{_value.m_Index} | [{_prefabTypeName}] \"{_prefab.name}\"", UIStyle.Instance.focusedReducedPaddingLabelStyle, options: null);
        GUILayout.FlexibleSpace();

        GUI.enabled = _value.m_Index != 0 && _prefab;
        if (GUILayout.Button("Details", UIStyle.Instance.iconButton, options: null))
        {
            ParentInspector.PreviewPrefab(_prefab, $"{_prefabTypeName} ({_value.m_Index})", false);
        }
        GUI.enabled = true;

        GUILayout.EndHorizontal();
    }
    }
}
