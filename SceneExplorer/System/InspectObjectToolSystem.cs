﻿using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game;
using Game.Areas;
using Game.Common;
using Game.Net;
using Game.Notifications;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Game.UI.Editor;
using SceneExplorer.ToBeReplaced;
using SceneExplorer.ToBeReplaced.Helpers;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using CarLane = Game.Net.CarLane;
using EditorContainer = Game.Tools.EditorContainer;
using Node = Game.Net.Node;
using SubArea = Game.Areas.SubArea;
using SubLane = Game.Net.SubLane;
using SubNet = Game.Net.SubNet;

namespace SceneExplorer.System
{
    public struct InspectedObject : IComponentData
    {
        public Entity entity;
        public bool entityChanged;
        public bool isDirty;
    }

    public partial class InspectObjectToolSystem : ToolBaseSystem
    {
        public Entity HoveredEntity;
        public float3 LastPos;
        public Entity Selected;

        public override string toolID => "Object Inspector Tool";
        public int Mode { get; set; }
        public bool Underground { get; set; }

        public override bool allowUnderground => true;

        private InspectorToolPanelSystem _panel;
        private UIManager _uiManager;

        public UIManager UIManager => _uiManager;
        private List<Action> _actions = new List<Action>();
        private ComponentType[] _selectedEntityComponents = Array.Empty<ComponentType>();
        private PrefabToolPanelSystem _prefabToolPanelSystem;
        private OverlayRenderSystem _overlayRenderSystem;
        private bool _eventRegistered;
        public ComponentDataRenderer.HoverData HoverData;

        protected override void OnCreate()
        {
            base.OnCreate();
            EntityManager.AddComponent<InspectedObject>(SystemHandle);
            _panel = World.GetOrCreateSystemManaged<InspectorToolPanelSystem>();
            Enabled = false;
            _prefabToolPanelSystem = World.GetExistingSystemManaged<PrefabToolPanelSystem>();
            _overlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
            TryRegisterPrefabSelectedEvent();
        }

        private void TryRegisterPrefabSelectedEvent()
        {
            if (_prefabToolPanelSystem == null)
            {
                _prefabToolPanelSystem = World.GetExistingSystemManaged<PrefabToolPanelSystem>();
            }
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            if (mode == GameMode.Editor)
            {
                _uiManager = World.GetOrCreateSystemManaged<SceneExplorerUISystem>().UiManager;
                for (var i = 0; i < _actions.Count; i++)
                {
                    _actions[i].Invoke();
                }
                _actions.Clear();
            }
            if (mode == GameMode.Game)
            {
                _uiManager = World.GetOrCreateSystemManaged<SceneExplorerUISystem>().UiManager;
            }
        }

        public void ToggleTool()
        {
            if (m_ToolSystem.activeTool != this && m_ToolSystem.activeTool == m_DefaultToolSystem)
            {
                m_ToolSystem.selected = Entity.Null;
                m_ToolSystem.activeTool = this;
            }
            else if (m_ToolSystem.activeTool == this)
            {
                m_ToolSystem.selected = Entity.Null;
                m_ToolSystem.activeTool = m_DefaultToolSystem;
            }
        }

        public void ChangeToolMode()
        {
            if (m_ToolSystem.activeTool == this)
            {
                Mode = GetNextMode();
            }
        }

        public override void SetUnderground(bool isUnderground)
        {
            Underground = isUnderground;
        }

        public override void ElevationUp()
        {
            Underground = false;
        }

        public override void ElevationDown()
        {
            Underground = true;
        }

        public override void ElevationScroll()
        {
            Underground = !Underground;
        }


        public override PrefabBase GetPrefab()
        {
            return null;
        }

        public override bool TrySetPrefab(PrefabBase prefab)
        {
            return false;
        }

        public void RegisterOnUIReadyAction(Action action)
        {
            _actions.Add(action);
        }

        public void RequestDisable()
        {
            m_ToolSystem.activeTool = m_DefaultToolSystem;
        }

        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            if (UIManager.CursorOverUI)
            {
                return;
            }

            if (Underground)
            {
                m_ToolRaycastSystem.collisionMask = CollisionMask.Underground;
            }
            else
            {
                m_ToolRaycastSystem.collisionMask = (CollisionMask.OnGround | CollisionMask.Overground);
            }

            switch (Mode)
            {
                case 0:
                    m_ToolRaycastSystem.typeMask = (TypeMask.Lanes | TypeMask.Net | TypeMask.MovingObjects | TypeMask.StaticObjects | TypeMask.MovingObjects);
                    m_ToolRaycastSystem.raycastFlags = RaycastFlags.SubElements | RaycastFlags.Decals | RaycastFlags.Placeholders | RaycastFlags.Outside |
                        RaycastFlags.Cargo | RaycastFlags.Passenger | RaycastFlags.BuildingLots | RaycastFlags.IgnoreSecondary;
                    m_ToolRaycastSystem.netLayerMask = Layer.All;
                    m_ToolRaycastSystem.iconLayerMask = IconLayerMask.None;
                    break;
                case 1:
                    m_ToolRaycastSystem.typeMask = (TypeMask.Lanes | TypeMask.Net);
                    m_ToolRaycastSystem.raycastFlags = RaycastFlags.Markers | RaycastFlags.ElevateOffset | RaycastFlags.SubElements | RaycastFlags.Cargo | RaycastFlags.Passenger;
                    m_ToolRaycastSystem.netLayerMask = Layer.All;
                    m_ToolRaycastSystem.iconLayerMask = IconLayerMask.None;
                    m_ToolRaycastSystem.utilityTypeMask = UtilityTypes.WaterPipe | UtilityTypes.SewagePipe | UtilityTypes.StormwaterPipe | UtilityTypes.LowVoltageLine | UtilityTypes.Fence | UtilityTypes.Catenary | UtilityTypes.HighVoltageLine;
                    break;
                case 2:
                    m_ToolRaycastSystem.typeMask = (TypeMask.MovingObjects | TypeMask.StaticObjects | TypeMask.MovingObjects);
                    m_ToolRaycastSystem.raycastFlags = (RaycastFlags.SubElements | RaycastFlags.SubBuildings | RaycastFlags.Decals | RaycastFlags.Markers | RaycastFlags.Outside | RaycastFlags.Placeholders | RaycastFlags.UpgradeIsMain);
                    m_ToolRaycastSystem.netLayerMask = Layer.None;
                    m_ToolRaycastSystem.iconLayerMask = IconLayerMask.None;
                    break;
                case 3:
                    m_ToolRaycastSystem.areaTypeMask = (AreaTypeMask.Lots | AreaTypeMask.Spaces | AreaTypeMask.Surfaces);
                    m_ToolRaycastSystem.typeMask = (TypeMask.Areas);
                    m_ToolRaycastSystem.raycastFlags = RaycastFlags.SubElements | RaycastFlags.EditorContainers;
                    m_ToolRaycastSystem.netLayerMask = Layer.None;
                    m_ToolRaycastSystem.iconLayerMask = IconLayerMask.None;
                    m_ToolRaycastSystem.utilityTypeMask = UtilityTypes.None;
                    break;
                default:
                    Mode = 0;
                    break;
            }
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            requireUnderground = false;
            _uiManager.ShowUI();
            TryRegisterPrefabSelectedEvent();
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            _uiManager.HideUI();
#if DEBUG_PP
            _panel.SelectEntity(Entity.Null);
#endif
            EntityManager.ChangeHighlighting_MainThread(Selected, Utils.ChangeMode.RemoveHighlight);
            EntityManager.ChangeHighlighting_MainThread(HoveredEntity, Utils.ChangeMode.RemoveHighlight);
            Selected = Entity.Null;
            HoveredEntity = Entity.Null;
            LastPos = float3.zero;
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            bool prevRequireUnderground = requireUnderground;
            requireUnderground = Underground;
            if (prevRequireUnderground != Underground)
            {
                EntityManager.SetComponentData<InspectedObject>(SystemHandle, new InspectedObject()
                {
                    entity = Entity.Null,
                    entityChanged = true,
                    isDirty = false,
                });

                EntityManager.ChangeHighlighting_MainThread(Selected, Utils.ChangeMode.RemoveHighlight);
                EntityManager.ChangeHighlighting_MainThread(HoveredEntity, Utils.ChangeMode.RemoveHighlight);
                Selected = Entity.Null;
                HoveredEntity = Entity.Null;
                LastPos = float3.zero;
                _panel.SelectEntity(Entity.Null);
                return inputDeps;
            }

            if (GetRaycastResult(out Entity e, out RaycastHit rc))
            {
                Entity prev = HoveredEntity;
                HoveredEntity = e;
                LastPos = rc.m_HitPosition;

                if (Mouse.current.leftButton.wasReleasedThisFrame && e != Selected)
                {
                    Logging.Debug($"Selected entity: {e}");
                    EntityManager.SetComponentData<InspectedObject>(SystemHandle, new InspectedObject()
                    {
                        entity = e,
                        entityChanged = true,
                        isDirty = false,
                    });

                    EntityManager.ChangeHighlighting_MainThread(Selected, Utils.ChangeMode.RemoveHighlight);
                    EntityManager.ChangeHighlighting_MainThread(e, Utils.ChangeMode.AddHighlight);
                    Selected = e;
                    _panel.SelectEntity(e);
                }
                else if (prev != HoveredEntity)
                {
                    if (prev != Selected)
                    {
                        EntityManager.ChangeHighlighting_MainThread(prev, Utils.ChangeMode.RemoveHighlight);
                    }
                    EntityManager.ChangeHighlighting_MainThread(HoveredEntity, Utils.ChangeMode.AddHighlight);
                }
            }
            else if (Mouse.current.rightButton.wasReleasedThisFrame || (Selected != Entity.Null && !EntityManager.Exists(Selected)))
            {
                EntityManager.SetComponentData<InspectedObject>(SystemHandle, new InspectedObject()
                {
                    entity = Entity.Null,
                    entityChanged = true,
                    isDirty = false,
                });

                EntityManager.ChangeHighlighting_MainThread(Selected, Utils.ChangeMode.RemoveHighlight);
                EntityManager.ChangeHighlighting_MainThread(HoveredEntity, Utils.ChangeMode.RemoveHighlight);
                Selected = Entity.Null;
                HoveredEntity = Entity.Null;
                LastPos = float3.zero;
                _panel.SelectEntity(Entity.Null);
            }
            else if (HoveredEntity != Entity.Null)
            {
                if (HoveredEntity != Selected)
                {
                    EntityManager.ChangeHighlighting_MainThread(HoveredEntity, Utils.ChangeMode.RemoveHighlight);
                }
                HoveredEntity = Entity.Null;
                LastPos = float3.zero;
            }

            //TODO Move to custom overlay render system, handle via singleton component with hovered entity data

            JobHandle deps = inputDeps;
            var hovered = HoverData;
            if (hovered.entity != Entity.Null && hovered.DataType != ComponentDataRenderer.HoverData.HoverType.None)
            {
                ComponentType type = (ComponentType)hovered.Type;
                Type managedType = type.GetManagedType();
                if (RenderByManagedType(managedType, hovered, inputDeps, out JobHandle resultDeps))
                {
                    return resultDeps;
                }
            }
            else if (HoveredEntity != Entity.Null || Selected != Entity.Null)
            {
                JobHandle resultDeps = inputDeps;
                if (HoveredEntity != Entity.Null && HoveredEntity != Selected && RequireManualHighlight(HoveredEntity, out ComponentType componentType, out bool isBuffer))
                {
                    Type managedType = componentType.GetManagedType();
                    hovered = new ComponentDataRenderer.HoverData()
                    {
                        entity = HoveredEntity,
                        DataType = isBuffer ? ComponentDataRenderer.HoverData.HoverType.Buffer : ComponentDataRenderer.HoverData.HoverType.Component,
                        Type = managedType
                    };
                    if (RenderByManagedType(managedType, hovered, resultDeps, out resultDeps, true) && HoveredEntity == Selected)
                    {
                        return resultDeps;
                    }
                }

                if (Selected != Entity.Null && RequireManualHighlight(Selected, out ComponentType componentType2, out bool isBuffer2))
                {
                    Type managedType = componentType2.GetManagedType();
                    hovered = new ComponentDataRenderer.HoverData()
                    {
                        entity = Selected,
                        DataType = isBuffer2 ? ComponentDataRenderer.HoverData.HoverType.Buffer : ComponentDataRenderer.HoverData.HoverType.Component,
                        Type = managedType
                    };
                    if (RenderByManagedType(managedType, hovered, resultDeps, out resultDeps))
                    {
                        return resultDeps;
                    }
                }
            }

            return deps;
        }

        private bool RequireManualHighlight(Entity entity, out ComponentType componentType, out bool buffer)
        {
            buffer = false;
            if (EntityManager.HasComponent<EditorContainer>(entity))
            {
                if (EntityManager.HasComponent<Edge>(entity) && EntityManager.HasBuffer<SubLane>(entity))
                {
                    componentType = ComponentType.ReadOnly<SubLane>();
                    buffer = true;
                    return true;
                }
                if (EntityManager.HasComponent<Node>(entity))
                {
                    componentType = ComponentType.ReadOnly<Node>();
                    buffer = false;
                    return true;
                }
            }
            else if (EntityManager.HasComponent<Game.Areas.Area>(entity) && EntityManager.HasBuffer<Game.Areas.Triangle>(entity))
            {
                componentType = ComponentType.ReadOnly<Triangle>();
                buffer = true;
                return true;
            }
            componentType = default;
            return false;
        }

        private bool RenderByManagedType(Type managedType, ComponentDataRenderer.HoverData hovered, JobHandle inputDeps, out JobHandle deps, bool highlight = false)
        {
            if (managedType == typeof(SubLane))
            {
                var buffer = _overlayRenderSystem.GetBuffer(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderSubLanes(hovered, buffer);
                return true;
            }
            if (managedType == typeof(SubNet))
            {
                var buffer = _overlayRenderSystem.GetBuffer(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderSubNets(hovered.entity, hovered.DataType == ComponentDataRenderer.HoverData.HoverType.Buffer, hovered, buffer);
                return true;
            }
            if (managedType == typeof(ConnectedEdge))
            {
                var buffer = _overlayRenderSystem.GetBuffer(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderConnectedEdges(hovered.entity, hovered, buffer);
                return true;
            }
            if (managedType == typeof(ConnectedNode))
            {
                var buffer = _overlayRenderSystem.GetBuffer(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderConnectedNodes(hovered.entity, hovered, buffer);
                return true;
            }
            if (managedType == typeof(Node))
            {
                var buffer = _overlayRenderSystem.GetBuffer(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderNode(hovered.entity, buffer, highlight);
                return true;
            }
            if (managedType == typeof(SubArea))
            {
                var buffer = _overlayRenderSystem.GetBuffer(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderSubAreas(hovered.entity, hovered, buffer, highlight);
                return true;
            }
            if (managedType == typeof(Game.Areas.Node))
            {
                var buffer = _overlayRenderSystem.GetBuffer(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderAreaNodes(hovered.entity, hovered, buffer);
                return true;
            }
            if (managedType == typeof(Game.Areas.Triangle))
            {
                var buffer = _overlayRenderSystem.GetBuffer(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderAreaTriangles(hovered.entity, hovered, buffer, highlight);
                return true;
            }
            if (managedType == typeof(Edge))
            {
                switch (hovered.DataType)
                {
                    case ComponentDataRenderer.HoverData.HoverType.Component:
                        var buffer = _overlayRenderSystem.GetBuffer(out JobHandle dependencies);
                        deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                        RenderEdgeNode(hovered.entity, null, buffer);
                        return true;
                    case ComponentDataRenderer.HoverData.HoverType.ComponentItem:
                        var buffer2 = _overlayRenderSystem.GetBuffer(out JobHandle dependencies2);
                        deps = JobHandle.CombineDependencies(inputDeps, dependencies2);
                        RenderEdgeNode(hovered.entity, hovered.Index == 0, buffer2);
                        return true;
                }
            }
            deps = new JobHandle();
            return false;
        }

        private void RenderSubNets(Entity entity, bool isBufferType, ComponentDataRenderer.HoverData hovered, OverlayRenderSystem.Buffer buffer)
        {
            DynamicBuffer<SubNet> subNets = EntityManager.GetBuffer<SubNet>(entity);
            bool isElevated = false;
            if (EntityManager.TryGetComponent(entity, out Elevation elevation))
            {
                isElevated = math.any(elevation.m_Elevation > 0.5f);
            }
            if (isBufferType && !subNets.IsEmpty)
            {
                foreach (SubNet subNet in subNets)
                {
                    if (subNet.m_SubNet != Entity.Null)
                    {
                        RenderSubNetLanes(subNet, hovered, buffer, isElevated);
                    }
                }
            }
            else
            {
                SubNet subNet = subNets[hovered.Index];
                RenderSubNetLanes(subNet, hovered, buffer, isElevated);
            }
        }

        private void RenderSubNetLanes(SubNet subNet, ComponentDataRenderer.HoverData hovered, OverlayRenderSystem.Buffer buffer, bool isElevated)
        {
            if (subNet.m_SubNet != Entity.Null && EntityManager.HasBuffer<SubLane>(subNet.m_SubNet))
            {
                DynamicBuffer<SubLane> subLanes = EntityManager.GetBuffer<SubLane>(subNet.m_SubNet);
                foreach (SubLane subLane in subLanes)
                {
                    if (subLane.m_SubLane != Entity.Null)
                    {
                        RenderNet(subLane.m_SubLane, hovered, buffer, false, isElevated);
                    }
                }
            }
        }

        private void RenderSubLanes(ComponentDataRenderer.HoverData hovered, OverlayRenderSystem.Buffer buffer)
        {
            DynamicBuffer<SubLane> subLanes = EntityManager.GetBuffer<SubLane>(hovered.entity);
            bool isElevated = false;
            if (EntityManager.TryGetComponent(hovered.entity, out Elevation elevation))
            {
                isElevated = math.any(elevation.m_Elevation > 0.5f);
            }
            if (hovered.DataType == ComponentDataRenderer.HoverData.HoverType.Buffer)
            {
                foreach (SubLane subLane in subLanes)
                {
                    if (subLane.m_SubLane != Entity.Null)
                    {
                        RenderNet(subLane.m_SubLane, hovered, buffer, false, isElevated);
                    }
                }
            }
            else
            {
                SubLane subLane = subLanes[hovered.Index];
                if (subLane.m_SubLane != Entity.Null)
                {
                    RenderNet(subLane.m_SubLane, hovered, buffer, false, isElevated);
                }
            }
        }

        private void RenderNet(Entity netEntity, ComponentDataRenderer.HoverData hovered, OverlayRenderSystem.Buffer buffer, bool isNode, bool isElevated)
        {

            if (EntityManager.HasComponent<Curve>(netEntity))
            {
                PrefabRef prefabRef = EntityManager.GetComponentData<PrefabRef>(netEntity);
                if (!EntityManager.TryGetComponent(prefabRef, out NetLaneData netLaneData))
                {
                    return;
                }

                Curve curve = EntityManager.GetComponentData<Curve>(netEntity);
                float width = netLaneData.m_Width > 0 ? netLaneData.m_Width : 0.5f;

                OverlayColor colors = GetColor(netLaneData.m_Flags);
                var style = (netLaneData.m_Flags & (LaneFlags.Underground | LaneFlags.Utility)) != 0 || isElevated ? OverlayRenderSystem.StyleFlags.Grid : OverlayRenderSystem.StyleFlags.Projected;

                if (EntityManager.TryGetComponent(netEntity, out CarLane carLane))
                {
                    bool isUnsafe = (carLane.m_Flags & (CarLaneFlags.Unsafe | CarLaneFlags.UTurnLeft | CarLaneFlags.UTurnRight)) != 0;
                    bool isMaster = EntityManager.HasComponent<MasterLane>(netEntity);
                    bool isSideConnection = (carLane.m_Flags & CarLaneFlags.SideConnection) != 0;
                    if (isUnsafe && !isSideConnection)
                    {
                        Color outlineColor = new Color(1f, 0.92f, 0.02f, 0.65f);
                        Color fillColor = new Color(1f, 0.92f, 0.02f, 0.08f);
                        if (isMaster)
                        {
                            outlineColor = new Color(0.57f, 0.49f, 0.02f, 0.65f);
                            fillColor = new Color(1f, 0.92f, 0.02f, 0.08f);
                            isNode = true;
                        }

                        buffer.DrawDashedCurve(outlineColor,
                            fillColor,
                            0.1f,
                            style,
                            curve.m_Bezier,
                            isNode ? 1f : width,
                            width / 2f,
                            width / 3f);
                    }
                    else
                    {
                        if (isMaster)
                        {
                            colors.outlineColor = new Color(0f, 0.08f, 1f, 0.65f);
                            colors.fillColor = new Color(0.03f, 0.04f, 1f, 0.08f);
                            isNode = true;
                        }
                        else if (isSideConnection)
                        {
                            colors.outlineColor = new Color(0.65f, 0f, 1f, 0.65f);
                            colors.fillColor = new Color(0.41f, 0f, 1f, 0.08f);
                            isNode = true;
                        }

                        buffer.DrawCurve(colors.outlineColor,
                            colors.fillColor,
                            0.15f,
                            style,
                            curve.m_Bezier,
                            isNode ? 1f : width,
                            float2.zero);
                    }
                }
                else
                {
                    buffer.DrawCurve(colors.outlineColor,
                        colors.fillColor,
                        0.1f,
                        style,
                        curve.m_Bezier,
                        isNode ? 0.5f : width,
                        float2.zero);
                }
            }
        }

        private void RenderConnectedEdges(Entity entity, ComponentDataRenderer.HoverData hovered, OverlayRenderSystem.Buffer buffer)
        {
            if (entity == Entity.Null)
                return;

            DynamicBuffer<ConnectedEdge> connectedEdges = EntityManager.GetBuffer<ConnectedEdge>(entity);
            if (connectedEdges.Length == 0)
            {
                return;
            }

            if (hovered.DataType == ComponentDataRenderer.HoverData.HoverType.Buffer)
            {
                foreach (ConnectedEdge connectedEdge in connectedEdges)
                {
                    if (connectedEdge.m_Edge != Entity.Null)
                    {
                        RenderEdge(connectedEdge.m_Edge, buffer);
                    }
                }
            }
            else if (hovered.Index >= 0 && hovered.Index < connectedEdges.Length)
            {
                ConnectedEdge connectedEdge = connectedEdges[hovered.Index];
                if (connectedEdge.m_Edge != Entity.Null)
                {
                    RenderEdge(connectedEdge.m_Edge, buffer);
                }
            }
        }

        private void RenderEdge(Entity entity, OverlayRenderSystem.Buffer buffer)
        {
            if (EntityManager.HasComponent<Curve>(entity))
            {
                bool isElevated = false;
                if (EntityManager.TryGetComponent(entity, out Elevation elevation))
                {
                    isElevated = math.any(elevation.m_Elevation > 0.5f);
                }

                float defaultWidth = 0;
                float elevatedWidth = 0;
                if (EntityManager.HasComponent<EdgeGeometry>(entity))
                {
                    PrefabRef prefabRef = EntityManager.GetComponentData<PrefabRef>(entity);
                    if (!EntityManager.TryGetComponent(prefabRef, out NetGeometryData netGeometryData))
                    {
                        return;
                    }
                    defaultWidth = netGeometryData.m_DefaultWidth;
                    elevatedWidth = netGeometryData.m_ElevatedWidth;
                }
                else if (EntityManager.TryGetComponent<EditorContainer>(entity, out EditorContainer editorContainer) &&
                    editorContainer.m_Prefab != Entity.Null &&
                    EntityManager.TryGetComponent(editorContainer.m_Prefab, out NetLaneGeometryData laneGeometryData))
                {
                    defaultWidth = laneGeometryData.m_Size.x * 0.5f;
                }
                else
                {
                    return;
                }

                Curve curve = EntityManager.GetComponentData<Curve>(entity);
                float baseWidth = isElevated ? elevatedWidth : defaultWidth;
                float width = baseWidth > 0.01f ? baseWidth : 0.5f;
                buffer.DrawCurve(Color.white,
                    new Color(0.79f, 0.79f, 0.79f, 0.1f),
                    math.select(0.25f, 0.1f, width <= 1f),
                    OverlayRenderSystem.StyleFlags.Projected,
                    curve.m_Bezier,
                    width,
                    float2.zero);
            }
        }

        private void RenderConnectedNodes(Entity entity, ComponentDataRenderer.HoverData hovered, OverlayRenderSystem.Buffer buffer)
        {
            if (entity == Entity.Null)
                return;

            DynamicBuffer<ConnectedNode> connectedNodes = EntityManager.GetBuffer<ConnectedNode>(entity);
            if (connectedNodes.Length == 0)
            {
                return;
            }

            if (hovered.DataType == ComponentDataRenderer.HoverData.HoverType.Buffer)
            {
                foreach (ConnectedNode connectedNode in connectedNodes)
                {
                    if (connectedNode.m_Node != Entity.Null)
                    {
                        RenderNode(connectedNode.m_Node, buffer);
                    }
                }
            }
            else if (hovered.Index >= 0 && hovered.Index < connectedNodes.Length)
            {
                ConnectedNode connectedNode = connectedNodes[hovered.Index];
                if (connectedNode.m_Node != Entity.Null)
                {
                    RenderNode(connectedNode.m_Node, buffer);
                }
            }
        }

        private void RenderNode(Entity entity, OverlayRenderSystem.Buffer buffer, bool highlight = false)
        {
            if (EntityManager.HasComponent<Node>(entity))
            {
                float diameter = 1f;
                Node node = EntityManager.GetComponentData<Node>(entity);
                float3 position = node.m_Position;
                if (EntityManager.TryGetComponent(entity, out NodeGeometry nodeGeometry))
                {
                    diameter = MathUtils.Size(nodeGeometry.m_Bounds).x + 1f;
                }
                else if (EntityManager.TryGetComponent<EditorContainer>(entity, out EditorContainer editorContainer) &&
                    editorContainer.m_Prefab != Entity.Null &&
                    EntityManager.TryGetComponent(editorContainer.m_Prefab, out NetLaneGeometryData laneGeometryData))
                {
                    diameter = laneGeometryData.m_Size.x * 0.75f;
                    if (highlight && EntityManager.HasBuffer<ConnectedEdge>(entity))
                    {
                        position += new float3(0, 0.2f, 0);
                        ComponentDataRenderer.HoverData data = new ComponentDataRenderer.HoverData()
                        {
                            entity = entity,
                            DataType = ComponentDataRenderer.HoverData.HoverType.Buffer,
                        };
                        RenderConnectedEdges(entity, data, buffer);
                    }
                }

                buffer.DrawCircle(
                    new Color(0f, 0.43f, 1f),
                    new Color(0f, 0.09f, 0.85f, 0.24f),
                    math.select(0.25f, 0.1f, diameter <= 1f),
                    0,
                    new float2(0, 1),
                    position,
                    diameter);
            }
        }

        private void RenderSubAreas(Entity entity, ComponentDataRenderer.HoverData hovered, OverlayRenderSystem.Buffer buffer, bool highlight = false) 
        {
            if (entity == Entity.Null)
                return;

            DynamicBuffer<Game.Areas.SubArea> areas = EntityManager.GetBuffer<Game.Areas.SubArea>(entity);
            if (areas.Length == 0)
            {
                return;
            }
            if (hovered.DataType == ComponentDataRenderer.HoverData.HoverType.Buffer)
            {
                foreach (SubArea subArea in areas)
                {
                    RenderAreaTriangles(subArea.m_Area, hovered, buffer, highlight);
                }
            } 
            else if (hovered.Index >= 0 && hovered.Index < areas.Length)
            {
                ComponentDataRenderer.HoverData data = hovered;
                data.DataType = ComponentDataRenderer.HoverData.HoverType.Buffer;
                RenderAreaTriangles(areas[hovered.Index].m_Area, data, buffer, highlight);
            }
        }

        private void RenderAreaNodes(Entity entity, ComponentDataRenderer.HoverData hovered, OverlayRenderSystem.Buffer buffer)
        {
            if (entity == Entity.Null)
                return;

            DynamicBuffer<Game.Areas.Node> areaNodes = EntityManager.GetBuffer<Game.Areas.Node>(entity);
            if (areaNodes.Length == 0)
            {
                return;
            }

            if (hovered.DataType == ComponentDataRenderer.HoverData.HoverType.Buffer)
            {
                for (int index = 0; index < areaNodes.Length - 1; index++)
                {
                    Game.Areas.Node node = areaNodes[index];
                    RenderAreaNode(node, buffer);
                    Game.Areas.Node node2 = areaNodes[index + 1];
                    RenderLine(node.m_Position, node2.m_Position, buffer, new Color(0f, 0.58f, 1f),  new Color(0f, 0.09f, 0.85f, 0.24f));
                }
                if (areaNodes.Length - 1 > 0)
                {
                    Game.Areas.Node node = areaNodes[0];
                    Game.Areas.Node node2 = areaNodes[areaNodes.Length - 1];
                    RenderAreaNode(node2, buffer);
                    RenderLine(node.m_Position, node2.m_Position, buffer, new Color(0f, 0.58f, 1f),  new Color(0f, 0.09f, 0.85f, 0.24f));
                }
            }
            else if (hovered.Index >= 0 && hovered.Index < areaNodes.Length)
            {
                RenderAreaNode(areaNodes[hovered.Index], buffer);
            }
        }

        private void RenderAreaTriangles(Entity entity, ComponentDataRenderer.HoverData hovered, OverlayRenderSystem.Buffer buffer, bool highlight = false)
        {
            if (entity == Entity.Null || !EntityManager.HasBuffer<Triangle>(entity) || !EntityManager.HasBuffer<Game.Areas.Node>(entity))
                return;

            DynamicBuffer<Game.Areas.Node> areaNodes = EntityManager.GetBuffer<Game.Areas.Node>(entity);
            DynamicBuffer<Game.Areas.Triangle> areaTriangles = EntityManager.GetBuffer<Game.Areas.Triangle>(entity);
            if (areaNodes.IsEmpty || areaTriangles.IsEmpty)
            {
                return;
            }

            if (hovered.DataType == ComponentDataRenderer.HoverData.HoverType.Buffer)
            {
                foreach (Game.Areas.Triangle triangle in areaTriangles)
                {
                    RenderAreaTriangle(triangle, ref areaNodes, buffer, highlight);
                }
            }
            else if (hovered.Index >= 0 && hovered.Index < areaNodes.Length)
            {
                RenderAreaTriangle(areaTriangles[hovered.Index], ref areaNodes, buffer, highlight);
            }
        }

        private void RenderAreaTriangle(Game.Areas.Triangle triangle, ref DynamicBuffer<Game.Areas.Node> nodes, OverlayRenderSystem.Buffer buffer, bool highlight = false)
        {
            if (math.any(triangle.m_Indices > new int3(nodes.Length)))
            {
                return;
            }
            Color outline = highlight ? new Color(0.21f, 1f, 0.36f) : new Color(0f, 0.58f, 1f);
            Color fill = highlight ? new Color(0.21f, 1f, 0.36f, 0.24f) : new Color(0f, 0.09f, 0.85f, 0.24f);
            RenderLine(nodes[triangle.m_Indices.x].m_Position, nodes[triangle.m_Indices.y].m_Position, buffer, outline, fill);
            RenderLine(nodes[triangle.m_Indices.y].m_Position, nodes[triangle.m_Indices.z].m_Position, buffer, outline, fill);
            RenderLine(nodes[triangle.m_Indices.z].m_Position, nodes[triangle.m_Indices.x].m_Position, buffer, outline, fill);
        }

        private void RenderLine(float3 from, float3 to, OverlayRenderSystem.Buffer buffer, Color outline, Color fill, float width = 0.1f)
        {
            Line3.Segment line= new Line3.Segment(from, to);
            buffer.DrawLine( outline, fill, 0f, 0f, line, width, new float2(1f));
        }
        
        private void RenderAreaNode(Game.Areas.Node node, OverlayRenderSystem.Buffer buffer)
        {
                buffer.DrawCircle(
                    new Color(0.01f, 0.31f, 0.85f),
                    new Color(0f, 0.09f, 0.85f, 0.24f),
                    0.1f,
                    0,
                    new float2(0,1),
                    node.m_Position,
                    0.4f);
        }

        private void RenderEdgeNode(Entity entity, bool? start, OverlayRenderSystem.Buffer buffer)
        {
            if (EntityManager.HasComponent<Edge>(entity))
            {
                Edge edge = EntityManager.GetComponentData<Edge>(entity);
                if (start.HasValue)
                {
                    RenderNode(start.Value ? edge.m_Start: edge.m_End, buffer);
                }
                else
                {
                    RenderNode(edge.m_Start, buffer);
                    RenderNode(edge.m_End, buffer);
                }
            }
        }

        private OverlayColor GetColor(LaneFlags flags)
        {
            if ((flags & LaneFlags.Master) != 0)
            {
                return new OverlayColor() { outlineColor = new Color(0f, 1f, 0.66f, 0.65f), fillColor = new Color(0.02f, 1f, 0.42f, 0.08f) };
            }
            if ((flags & LaneFlags.Road) != 0)
            {
                return new OverlayColor() { outlineColor = new Color(0f, 0.83f, 1f, 0.65f), fillColor = new Color(0f, 0.8f, 1f, 0.08f) };
            }
            if ((flags & LaneFlags.Utility) != 0)
            {
                return new OverlayColor() { outlineColor = new Color(0f, 0.4f, 1f, 0.65f), fillColor = new Color(0f, 0.25f, 1f, 0.08f) };
            }
            if ((flags & LaneFlags.Pedestrian) != 0)
            {
                return new OverlayColor() { outlineColor = new Color(0f, 1f, 0.25f, 0.65f), fillColor = new Color(0f, 1f, 0.46f, 0.08f) };
            }
            if ((flags & (LaneFlags.Virtual | LaneFlags.Parking | LaneFlags.ParkingLeft | LaneFlags.ParkingRight)) != 0)
            {
                return new OverlayColor() { outlineColor = new Color(0.24f, 0.26f, 1f, 0.65f), fillColor = new Color(0.03f, 0.04f, 1f, 0.08f) };
            }
            if ((flags & LaneFlags.Secondary) != 0)
            {
                return new OverlayColor() { outlineColor = new Color(0.82f, 1f, 0f, 0.65f), fillColor = new Color(0.97f, 1f, 0.01f, 0.08f) };
            }
            if ((flags & LaneFlags.Track) != 0)
            {
                return new OverlayColor() { outlineColor = new Color(1f, 0.91f, 0.81f, 0.65f), fillColor = new Color(0.96f, 1f, 0.89f, 0.08f) };
            }
            if ((flags & LaneFlags.OnWater | LaneFlags.HasAuxiliary) != 0)
            {
                return new OverlayColor() { outlineColor = new Color(1f, 0f, 0.97f, 0.65f), fillColor = new Color(0.95f, 0f, 1f, 0.08f) };
            }

            return new OverlayColor() { fillColor = new Color(0f, 1f, 0.73f, 0.05f), outlineColor = new Color(0.01f, 1f, 0.42f, 0.65f) };
        }

        private struct OverlayColor
        {
            public Color fillColor;
            public Color outlineColor;
        }

        private bool IsNearEnd(Entity edge, Curve curve, float3 position, bool invert)
        {
            EdgeGeometry edgeGeometry;
            if (EntityManager.TryGetComponent(edge, out edgeGeometry))
            {
                Bezier4x3 startBezier = MathUtils.Lerp(edgeGeometry.m_Start.m_Left, edgeGeometry.m_Start.m_Right, 0.5f);
                Bezier4x3 endBezier = MathUtils.Lerp(edgeGeometry.m_End.m_Left, edgeGeometry.m_End.m_Right, 0.5f);
                float startBezierT;
                float distanceToStart = MathUtils.Distance(startBezier.xz, position.xz, out startBezierT);
                float endBezierT;
                float distanceToEnd = MathUtils.Distance(endBezier.xz, position.xz, out endBezierT);
                float middleLengthStart = edgeGeometry.m_Start.middleLength;
                float middleLengthEnd = edgeGeometry.m_End.middleLength;
                return math.select(startBezierT * middleLengthStart, middleLengthStart + endBezierT * middleLengthEnd, distanceToEnd < distanceToStart) > (middleLengthStart + middleLengthEnd) * 0.5f != invert;
            }
            float curveBezierT;
            MathUtils.Distance(curve.m_Bezier.xz, position.xz, out curveBezierT);
            return curveBezierT > 0.5f;
        }

        private int GetNextMode()
        {
            int current = Mode;
            return current > 2 ? 0 : current + 1;
        }

        private string HitToString(RaycastHit hit)
        {
            return
                $"m_HitEntity: {hit.m_HitEntity} m_Position: {hit.m_Position} m_HitPosition: {hit.m_HitPosition} m_HitDirection: {hit.m_HitDirection} m_CellIndex: {hit.m_CellIndex} m_NormalizedDistance: {hit.m_NormalizedDistance} m_CurvePosition: {hit.m_CurvePosition} ";
        }
    }
}
