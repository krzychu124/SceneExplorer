using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Prefabs;
using SceneExplorer.Services;
using SceneExplorer.ToBeReplaced.Windows;
using Unity.Entities;
using UnityEngine;

namespace SceneExplorer.ToBeReplaced.Helpers.ContentSections
{
    public class ComponentSection : ISection
    {
        private readonly ComponentType _type;
        private readonly Entity _entity;

        private string _typeName;
        private bool _expanded;
        private List<ISectionItem> _items = new List<ISectionItem>();
        private GUILayoutOption[] _expandButtonOptions = new GUILayoutOption[] { GUILayout.MaxWidth(21), GUILayout.MaxHeight(22) };

        public IParentInspector ParentInspector { get; set; }

        public ComponentSection(ComponentType type, Entity entity) {
        _type = type;
        _entity = entity;
        _typeName = $"{type.GetManagedType().Name}";
    }

        public void Render() {

        bool prevExpanded = _expanded;
        if (CommonUI.CollapsibleHeader(_typeName, _expanded, Rect.zero, out bool _, prefix: "[C]", location: CommonUI.ButtonLocation.EndCenteredText, textStyle: CommonUI.CalculateTextStyle(_typeName, _expanded)))
        {
            _expanded = !_expanded;
            if (prevExpanded != _expanded && _expanded && Event.current.type == EventType.KeyDown)
            {
                UpdateBindings();
            }
        }
        CommonUI.CollapsibleList(prevExpanded, _items, 6, drawSeparator: true);
    }

        public void UpdateBindings(bool refreshOnly = false) {
        if (!_expanded)
        {
            return;
        }
        _items.Clear();
        if (!World.DefaultGameObjectInjectionWorld.EntityManager.Exists(_entity))
        {
            
        }
        Type componentType = _type.GetManagedType();
        object value = componentType.GetComponentDataByType(World.DefaultGameObjectInjectionWorld.EntityManager, _entity);

        if (_typeName.Equals(nameof(PrefabData)))
        {
            ISectionItem item = UIGenerator.GetSectionItem("m_Index", value, _typeName, 2);
            if (item is IInteractiveSectionItem interactiveSectionItem)
            {
                interactiveSectionItem.ParentInspector = ParentInspector;
            }

            _items.Add(item);

            return;
        }

        foreach (FieldInfo fieldInfo in TypeDescriptorService.Instance.GetFields(_type))
        {
            string fieldName = fieldInfo.Name;
            ISectionItem item = UIGenerator.GetSectionItem(fieldName, fieldInfo.GetValue(value), _typeName, 2);
            if (item is IInteractiveSectionItem interactiveSectionItem)
            {
                interactiveSectionItem.ParentInspector = ParentInspector;
            }
            _items.Add(item);
        }
    }
    }
}
