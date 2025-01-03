﻿using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Game.Common;
using Game.Prefabs;
using Game.UI.Tooltip;
using Game.UI.Widgets;
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

        protected override void OnCreate()
        {
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
                children = new List<IWidget>(10),
            };
            _inspectObjectTool = World.GetOrCreateSystemManaged<InspectObjectToolSystem>();
            _prefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
        }

        protected override void OnUpdate()
        {
            if (!_inspectObjectTool.Enabled || _inspectObjectTool.UIManager.CursorOverUI)
            {
                return;
            }

            Entity e = _inspectObjectTool.HoveredEntity;
            if (e == Entity.Null && _currentEntity != Entity.Null)
            {
                _tooltip.value = "= Nothing here =";
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
                    bool valid = false;
                    if (EntityManager.TryGetComponent(e, out PrefabRef refData) &&
                        EntityManager.TryGetComponent(refData.m_Prefab, out PrefabData prefabData))
                    {
                        
                        valid = prefabData.m_Index >= 0;
                        prefabName = _prefabSystem.GetPrefabName(refData.m_Prefab);
                        if (!valid)
                        {
                            prefabName = "[Missing] " + prefabName;
                        }
                    }
                    var pos = WorldToTooltipPos(_inspectObjectTool.LastPos, out _);
                    _tooltipGroup.position = new float2(pos.x + 5, pos.y + 20);
                    _tooltipGroup.children.Add(new StringTooltip
                    {
                        color = valid? TooltipColor.Success : TooltipColor.Error,
                        path = "title1",
                        value = $"Prefab: \"{prefabName}\" [{e.Index}:{e.Version}]"
                
                    });
                
                    if (EntityManager.HasComponent<Owner>(e) &&
                        EntityManager.TryGetComponent<Owner>(e, out Owner owner) &&
                        EntityManager.TryGetComponent(owner.m_Owner, out PrefabRef ownerRef) &&
                        EntityManager.TryGetComponent(ownerRef.m_Prefab, out PrefabData prefabOwnerData))
                    {
                        bool validOwner = prefabOwnerData.m_Index >= 0;
                        string name = _prefabSystem.GetPrefabName(ownerRef.m_Prefab);
                        if (!validOwner)
                        {
                            name = "[Missing] " + name;
                        }
                        _tooltipGroup.children.Add(new StringTooltip
                        {
                            color = validOwner ? TooltipColor.Success : TooltipColor.Error,
                            path = "title1_1",
                            value = $"Owner prefab: \"{name}\" [{owner.m_Owner.Index}:{owner.m_Owner.Version}]"
                        });
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
                    for (int index = 0; index < components.Length; index++)
                    {
                        ComponentType component = components[index];
                        counter++;
                        Type managedType = component.GetManagedType();
                        listOfComponentNames.Add(GetExtendedName(managedType));
                        if (counter > 3)
                        {
                            counter = 0;
                            _tooltipGroup.children.Add(new StringTooltip
                            {
                                path = $"components_group_{index}",
                                value = string.Join(", ", listOfComponentNames) + ","
                            });
                            listOfComponentNames.Clear();
                        }
                    }
                    if (listOfComponentNames.Count > 0)
                    {
                        _tooltipGroup.children.Add(new StringTooltip
                        {
                            path = "components_last",
                            value = string.Join(", ", listOfComponentNames)
                        });
                    }
                }
            }
            else
            {
            
                if (_tooltipGroup.children.Count > 0)
                {
                    var pos = WorldToTooltipPos(_inspectObjectTool.LastPos, out _);
                    _tooltipGroup.position = new float2(pos.x + 5, pos.y + 20);
                    AddGroup(_tooltipGroup);
                }
                else
                {
                    _tooltip.value = "= Nothing here =";
                    AddMouseTooltip(_tooltip);
                }
            
            }

        }

        private string GetExtendedName(Type type)
        {
            return $"{((type?.Namespace ?? "")).Split('.').Last()}.{type?.Name}";
        }
    }
}
