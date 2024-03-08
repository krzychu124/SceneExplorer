using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Game.Common;
using Game.Prefabs;
using Game.UI.Tooltip;
using Game.UI.Widgets;
using SceneExplorer.ToBeReplaced;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SceneExplorer.System
{
    public partial class InspectorTooltipSystem : TooltipSystemBase
    {
        private StringTooltip _tooltip;
        private TooltipGroup _tooltipGroup;
        private InspectObjectToolSystem _inspectObjectTool;
        private PrefabSystem _prefabSystem;
        private Entity _currentEntity;

        protected override void OnCreate() {
        base.OnCreate();

        _tooltip = new StringTooltip
        {
            path = "SceneExplorerTooltip",
            color = TooltipColor.Warning,
            value = string.Empty
        };
        _tooltipGroup = new TooltipGroup
        {
            path = "SceneExplorerTooltipGroup",
            horizontalAlignment = TooltipGroup.Alignment.Start,
            verticalAlignment = TooltipGroup.Alignment.Center,
            children = new List<IWidget>(10)
        };
        _inspectObjectTool = World.GetOrCreateSystemManaged<InspectObjectToolSystem>();
        _prefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
    }

        protected override void OnUpdate() {
        if (!_inspectObjectTool.Enabled || _inspectObjectTool.UIManager.CursorOverUI)
        {
            return;
        }

        Entity e = _inspectObjectTool.HoveredEntity;
        if (e == Entity.Null && _currentEntity != Entity.Null)
        {
            _tooltip.value = "<Nothing here>";
            _tooltipGroup.children.Clear();
            _currentEntity = Entity.Null;
            AddMouseTooltip(_tooltip);
        }
        else if (e != _currentEntity)
        {
            _currentEntity = e;
            _tooltipGroup.children.Clear();
            using (NativeArray<ComponentType> componentTypes = EntityManager.GetComponentTypes(e, Allocator.Temp))
            {
                string prefabName = EntityManager.GetName(e);
                if (EntityManager.TryGetComponent(e, out PrefabRef refData))
                {
                    PrefabBase prefab = _prefabSystem.GetPrefab<PrefabBase>(refData);
                    if (prefab != null)
                    {
                        prefabName = prefab.prefab ? prefab.prefab.name : prefab.name;
                    }
                }
                var pos = WorldToTooltipPos(_inspectObjectTool.LastPos);
                _tooltipGroup.position = new float2(pos.x + 5, pos.y + 20);
                _tooltipGroup.children.Add(new StringTooltip
                {
                    color = TooltipColor.Success,
                    path = "title1",
                    value = $"Prefab: \"{prefabName}\" [{e.Index}:{e.Version}]"

                });

                if (EntityManager.HasComponent<Owner>(e) && 
                    EntityManager.TryGetComponent<Owner>(e, out Owner owner) &&
                    EntityManager.TryGetComponent(owner.m_Owner, out PrefabRef ownerRef))
                {
                    PrefabBase ownerPrefab = _prefabSystem.GetPrefab<PrefabBase>(ownerRef);
                    if (ownerPrefab != null)
                    {
                        _tooltipGroup.children.Add(new StringTooltip
                        {
                            color = TooltipColor.Success,
                            path = "title1_1",
                            value = $"Owner prefab: \"{ownerPrefab.name}\" [{owner.m_Owner.Index}:{owner.m_Owner.Version}]"
                        });
                    }
                }

                _tooltipGroup.children.Add(new StringTooltip
                {
                    color = TooltipColor.Warning,
                    path = "title2",
                    value = $"Components ({componentTypes.Length}):"
                });
                var components = componentTypes.ToArray();
                List<string> listOfComponentNames = new List<string>(components.Length);
                int counter = 0;
                foreach (ComponentType component in components)
                {
                    counter++;
                    Type managedType = component.GetManagedType();
                    listOfComponentNames.Add(GetExtendedName(managedType));
                    if (counter > 3)
                    {
                        counter = 0;
                        _tooltipGroup.children.Add(new StringTooltip
                        {
                            value = string.Join(", ", listOfComponentNames) + ","
                        });
                        listOfComponentNames.Clear();
                    }
                }
                if (listOfComponentNames.Count > 0)
                {
                    _tooltipGroup.children.Add(new StringTooltip
                    {
                        value = string.Join(", ", listOfComponentNames)
                    });
                }
            }
        }
        else
        {

            if (_tooltipGroup.children.Count > 0)
            {
                var pos = WorldToTooltipPos(_inspectObjectTool.LastPos);
                _tooltipGroup.position = new float2(pos.x + 5, pos.y + 20);
                AddGroup(_tooltipGroup);
            }
            else
            {
                _tooltip.value = "<Nothing here>";
                AddMouseTooltip(_tooltip);
            }

        }

    }

        private string GetExtendedName(Type type) {
        return $"{((type?.Namespace ?? "")).Split('.').Last()}.{type?.Name}";
    }
    }
}