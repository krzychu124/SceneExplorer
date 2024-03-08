using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Buildings;
using Game.Prefabs;
using SceneExplorer.Services;
using SceneExplorer.ToBeReplaced.Helpers.ContentItems;
using SceneExplorer.ToBeReplaced.Windows;
using Unity.Entities;
using UnityEngine;
using SubArea = Game.Areas.SubArea;
using SubLane = Game.Net.SubLane;
using SubNet = Game.Net.SubNet;
using SubObject = Game.Objects.SubObject;

namespace SceneExplorer.ToBeReplaced.Helpers.ContentSections
{
    public class BufferSection : ISection
    {
        private static GUILayoutOption[] _expandButtonOptions = new GUILayoutOption[] { GUILayout.MaxWidth(21), GUILayout.MaxHeight(22) };
        private static GUILayoutOption[] _options = new[] { GUILayout.MaxWidth(40) };
        private string _title;
        private readonly ComponentType _type;
        private readonly Entity _entity;
        private string _typeName;
        private bool _expanded;
        private List<ISectionItem> _items = new List<ISectionItem>();

        public IParentInspector ParentInspector { get; set; }

        public BufferSection(ComponentType type, Entity entity) {
        _type = type;
        _entity = entity;
        _typeName = type.GetManagedType().Name;
        _title = $"{_typeName}";
    }

        public void Render() {
        bool prevExpanded = _expanded;

        GUI.enabled = _items.Count > 0;
        if (CommonUI.CollapsibleHeader($"{_title} ({_items.Count})", _expanded, out bool _, prefix: "[B]", location: CommonUI.ButtonLocation.EndCenteredText, textStyle: CommonUI.CalculateTextStyle(_typeName, _expanded)))
        {
            _expanded = !_expanded;
        }
        GUI.enabled = true;
        CommonUI.CollapsibleList(prevExpanded, _items, 6, drawSeparator: true);
    }

        public void UpdateBindings(bool refreshOnly = false) {
        if (refreshOnly && !_expanded)
        {
            return;
        }
        _items.Clear();
        Type managedType = _type.GetManagedType();
        List<object> bufferValues = managedType.GetComponentBufferArrayByType(World.DefaultGameObjectInjectionWorld.EntityManager, _entity);

        for (var i = 0; i < bufferValues.Count; i++)
        {
            object bufferValue = bufferValues[i];
            List<ISectionItem> items = new List<ISectionItem>();
            string prefabName = null;
            foreach (FieldInfo fieldInfo in TypeDescriptorService.Instance.GetFields(_type))
            {
                string fieldName = fieldInfo.Name;
                object fieldValue = fieldInfo.GetValue(bufferValue);
                ISectionItem item = UIGenerator.GetSectionItem(fieldName, fieldValue, _typeName, 2);
                if (item is IInteractiveSectionItem interactiveSectionItem)
                {
                    interactiveSectionItem.ParentInspector = ParentInspector;
                }

                if (prefabName == null)
                {
                    TryGetPrefabName(bufferValue, fieldValue, out prefabName);
                }

                items.Add(item);
            }
            _items.Add(new ObjectItem(_typeName, i, items, prefabName));
        }
    }

        private void TryGetPrefabName(object typeValue, object fieldValue, out string prefabName) {
        prefabName = null;
        switch (typeValue)
        {
            case SpawnLocationElement:
            case SubArea:
            case SubLane:
            case SubNet:
            case SubObject:
                if (fieldValue is Entity e && e != Entity.Null)
                {
                    prefabName = GetPrefabNameFromEntity(e);
                }
                break;

        }
    }

        private string GetPrefabNameFromEntity(Entity e) {
        if (World.DefaultGameObjectInjectionWorld.EntityManager.HasComponent<PrefabRef>(e))
        {
            if (World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>().TryGetPrefab(World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<PrefabRef>(e), out PrefabBase prefab))
            {
                return prefab.name;
            }
        }
        return null;
    }
    }
}
