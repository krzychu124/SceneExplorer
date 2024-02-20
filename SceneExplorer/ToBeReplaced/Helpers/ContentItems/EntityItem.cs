using Game.Prefabs;
using SceneExplorer.ToBeReplaced.Windows;
using Unity.Entities;
using UnityEngine;

namespace SceneExplorer.ToBeReplaced.Helpers.ContentItems
{
    public class EntityItem : IInteractiveSectionItem
    {
        private static GUILayoutOption[] _options = new[] { GUILayout.MaxWidth(40) };
        private readonly string _title;
        private readonly string _titleRaw;
        private readonly Entity _value;
        private readonly string _componentName;
        private string _valueString;

        public EntityItem(string title, Entity value, string componentName) {
        _titleRaw = title;
        _title = $"{title}:";
        _value = value;
        _valueString = value.ToString();
        _componentName = componentName;

        if (title.Equals("m_Prefab") && value != Entity.Null)
        {
            PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
            if (prefabSystem.TryGetPrefab(value, out PrefabBase prefabBase))
            {
                _valueString = $"{value} [{prefabBase.name}]";
            }
        }
    }

        public void Render() {
        GUILayout.BeginHorizontal(options: null);
        GUILayout.Label(_title, UIStyle.Instance.reducedPaddingLabelStyle, options: null);
        GUILayout.Label(_valueString, UIStyle.Instance.entityValueStyle, options: null);
        GUILayout.FlexibleSpace();
        GUI.enabled = _value != Entity.Null;
        if (GUILayout.Button("Details", UIStyle.Instance.iconButton, options: null))
        {
            ParentInspector?.PreviewEntity(_value, _titleRaw, _componentName, false);
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }

        public IParentInspector ParentInspector { get; set; }
    }
}
