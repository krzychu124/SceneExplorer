using System;
using System.Linq;
using Colossal;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Routes;
using Game.Vehicles;
using Game.Zones;
using SceneExplorer.ToBeReplaced.Helpers;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using CarLane = Game.Net.CarLane;
using CarLaneFlags = Game.Net.CarLaneFlags;
using EditorContainer = Game.Tools.EditorContainer;
using Color = UnityEngine.Color;
using Elevation = Game.Net.Elevation;
using Node = Game.Net.Node;
using Edge = Game.Net.Edge;
using PathElement = Game.Pathfind.PathElement;
using Segment = Game.Net.Segment;
using SpawnLocation = Game.Objects.SpawnLocation;
using SubArea = Game.Areas.SubArea;
using SubLane = Game.Net.SubLane;
using SubNet = Game.Net.SubNet;
using SubObject = Game.Objects.SubObject;
using TakeoffLocation = Game.Routes.TakeoffLocation;
using Transform = Game.Objects.Transform;

namespace SceneExplorer.System
{
    public partial class ObjectHighlightSystem : GameSystemBase
    {
        private GizmosSystem _gizmosSystem;
        private OverlayRenderSystem _overlayRenderSystem;
        private NativeParallelMultiHashMap<Entity, ComponentHighlight> _highlightsMap;
        private EntityQuery _tempHighlightquery;
        private const float CellSize = 8f;
        public int Stats => _highlightsMap.GetKeyArray(Allocator.Temp).Select(k => _highlightsMap.CountValuesForKey(k)).Sum();
        public EntityArchetype ComponentHighlightArchetype;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            _gizmosSystem = World.GetExistingSystemManaged<GizmosSystem>();
            _overlayRenderSystem = World.GetExistingSystemManaged<OverlayRenderSystem>();
            _highlightsMap = new NativeParallelMultiHashMap<Entity, ComponentHighlight>(16, Allocator.Persistent);
            
            _tempHighlightquery = SystemAPI.QueryBuilder().WithAll<EntityHighlight>().WithAny<Updated, Deleted>().Build();
            ComponentHighlightArchetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<EntityHighlight>());
        }

        protected override void OnUpdate()
        {
            if (!_tempHighlightquery.IsEmptyIgnoreFilter)
            {
                UpdateHighlightsMap();
            }

            NativeHashSet<Entity> toRemove = new NativeHashSet<Entity>(2, Allocator.Temp);
            using (NativeParallelMultiHashMap<Entity, ComponentHighlight>.KeyValueEnumerator keyValueEnumerator = _highlightsMap.GetEnumerator())
            {
                JobHandle input = Dependency;
                while (keyValueEnumerator.MoveNext())
                {
                    KeyValue<Entity, ComponentHighlight> pair = keyValueEnumerator.Current;
                    if (pair.Key != Entity.Null && EntityManager.Exists(pair.Key))
                    {
                        HighlightByManagedType(pair.Value, null, pair.Key, input, out input);
                    }
                    else
                    {
                        toRemove.Add(pair.Key);
                    }
                }
                Dependency = input;
            }
            if (!toRemove.IsEmpty)
            {
                foreach (Entity entity in toRemove)
                {
                    _highlightsMap.Remove(entity);
                }
                toRemove.Dispose();
            }
        }

        private void UpdateHighlightsMap()
        {
            NativeArray<ArchetypeChunk> chunks = _tempHighlightquery.ToArchetypeChunkArray(Allocator.Temp);
            var highlightsType = SystemAPI.GetComponentTypeHandle<EntityHighlight>(true);
            var deletedType = SystemAPI.GetComponentTypeHandle<Deleted>(true);
            _highlightsMap.Clear();
            foreach (ArchetypeChunk chunk in chunks)
            {
                NativeArray<EntityHighlight> highlights = chunk.GetNativeArray(ref highlightsType);
                if (!chunk.Has(ref deletedType))
                {
                    foreach (EntityHighlight highlight in highlights)
                    {
                        TryAddUniqueValue(highlight.entity, highlight.highlight);
                    }
                }
            }
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            _highlightsMap.Clear();
        }

        public void AddHighlight(Entity e, ComponentType type, int index = -1)
        {
            var h = new ComponentHighlight(type, index);
            TryAddUniqueValue(e, h);
        }

        public void RemoveHighlight(Entity e)
        {
            if (_highlightsMap.ContainsKey(e))
            {
                _highlightsMap.Remove(e);
            }
        }
        
        public void RemoveHighlight(Entity e, ComponentType type, int index = -1)
        {
            ComponentHighlight highlight = new ComponentHighlight(type, index);
            if (_highlightsMap.TryGetFirstValue(e, out ComponentHighlight item, out NativeParallelMultiHashMapIterator<Entity> it ))
            {
                do
                {
                    if (item.Equals(highlight))
                    {
                        _highlightsMap.Remove(it);
                    }
                    
                } while (_highlightsMap.TryGetNextValue(out item, ref it));

                if (_highlightsMap.CountValuesForKey(e) == 0)
                {
                    _highlightsMap.Remove(e);
                }
            }
        }

        private void TryAddUniqueValue(Entity key, ComponentHighlight value)
        {
            foreach (var item in _highlightsMap.GetValuesForKey(key))
            {
                if (item.Equals(value))
                {
                    return;
                }
            }
            _highlightsMap.Add(key, value);
        }

        public bool IsHiglightable(ComponentType type)
        {
            Type t = type.GetManagedType();
            return t == typeof(Node) ||
                t == typeof(Edge) ||
                t == typeof(Segment) ||
                t == typeof(Cell) ||
                t == typeof(SubBlock) ||
                t == typeof(SubLane) ||
                t == typeof(SubNet) ||
                t == typeof(SubArea) ||
                t == typeof(SubObject) ||
                t == typeof(ConnectedEdge) ||
                t == typeof(ConnectedNode) ||
                t == typeof(Game.Areas.Node) ||
                t == typeof(Triangle) ||
                t == typeof(Bone) ||
                t == typeof(SpawnLocationElement) ||
                t == typeof(ConnectedBuilding) ||
                t == typeof(WoodResource) ||
                t == typeof(GuestVehicle) ||
                t == typeof(OwnedVehicle) ||
                t == typeof(LayoutElement) ||
                t == typeof(PathElement) ||
                t == typeof(CarNavigationLane) ||
                t == typeof(TrainNavigationLane) ||
                t == typeof(WatercraftNavigationLane) ||
                t == typeof(AircraftNavigationLane);
        } 
        
        private bool HighlightByManagedType(ComponentHighlight componentHighlight, object value, Entity entity, JobHandle inputDeps, out JobHandle deps, bool highlight = false)
        {
            Type managedType = componentHighlight.type.GetManagedType();
            if (managedType == typeof(Bezier4x3))
            {
                var gizmoBatcher = _gizmosSystem.GetGizmosBatcher(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderCurve((Bezier4x3)value, gizmoBatcher);
                return true;
            }
            if (managedType == typeof(Bounds3))
            {
                var gizmoBatcher = _gizmosSystem.GetGizmosBatcher(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderBounds((Bounds3)value, gizmoBatcher);
                return true;
            }
            if (managedType == typeof(float3))
            {
                var gizmoBatcher = _gizmosSystem.GetGizmosBatcher(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderPoint((float3)value, gizmoBatcher);
                return true;
            }
            if (managedType == typeof(Segment))
            {
                var gizmoBatcher = _gizmosSystem.GetGizmosBatcher(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderSegment((Segment)value, gizmoBatcher);
                return true;
            }
            if (managedType == typeof(PathElement))
            {
                if (EntityManager.TryGetBuffer(entity, true, out DynamicBuffer<PathElement> pathElements))
                {
                    deps = StartPathRenderingJob(inputDeps, pathElements, math.max(0, componentHighlight.index), !componentHighlight.IsMain, (pa) => pa.m_Target, (pa) => pa.m_TargetDelta);
                    return true;
                }
                deps = inputDeps;
                return false;
            }
            if (managedType == typeof(CarNavigationLane))
            {
                if (EntityManager.TryGetBuffer(entity, true, out DynamicBuffer<CarNavigationLane> carNavigationLanes))
                {
                    deps = StartPathRenderingJob(inputDeps, carNavigationLanes, math.max(0, componentHighlight.index), !componentHighlight.IsMain, (cnl) => cnl.m_Lane, (cnl) => cnl.m_CurvePosition);
                    return true;
                }
                deps = inputDeps;
                return false;
            }
            if (managedType == typeof(TrainNavigationLane))
            {
                if (EntityManager.TryGetBuffer(entity, true, out DynamicBuffer<TrainNavigationLane> trainNavigationLanes))
                {
                    deps = StartPathRenderingJob(inputDeps, trainNavigationLanes, math.max(0, componentHighlight.index), !componentHighlight.IsMain, (tnl) => tnl.m_Lane, (tnl) => tnl.m_CurvePosition);
                }
                deps = inputDeps;
                return false;
            }
            if (managedType == typeof(WatercraftNavigationLane))
            {
                if (EntityManager.TryGetBuffer(entity, true, out DynamicBuffer<WatercraftNavigationLane> watercraftNavigationLanes))
                {
                    deps = StartPathRenderingJob(inputDeps, watercraftNavigationLanes, math.max(0, componentHighlight.index), !componentHighlight.IsMain, (wnl) => wnl.m_Lane, (wnl) => wnl.m_CurvePosition);
                }
                deps = inputDeps;
                return false;
            }
            if (managedType == typeof(AircraftNavigationLane))
            {
                if (EntityManager.TryGetBuffer(entity, true, out DynamicBuffer<AircraftNavigationLane> aircraftNavigationLanes))
                {
                    deps = StartPathRenderingJob(inputDeps, aircraftNavigationLanes, math.max(0, componentHighlight.index), !componentHighlight.IsMain, (anl) => anl.m_Lane, (anl) => anl.m_CurvePosition);
                }
                deps = inputDeps;
                return false;
            }
            if (managedType == typeof(SubLane))
            {
                var buffer = _overlayRenderSystem.GetBuffer(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderSubLanes(entity, componentHighlight, buffer);
                return true;
            }
            if (managedType == typeof(SubNet))
            {
                var buffer = _overlayRenderSystem.GetBuffer(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderSubNets(entity, componentHighlight, buffer);
                return true;
            }
            if (managedType == typeof(ConnectedEdge))
            {
                var buffer = _overlayRenderSystem.GetBuffer(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderConnectedEdges(entity, componentHighlight, buffer);
                return true;
            }
            if (managedType == typeof(ConnectedNode))
            {
                var buffer = _overlayRenderSystem.GetBuffer(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderConnectedNodes(entity, componentHighlight, buffer);
                return true;
            }
            if (managedType == typeof(Node))
            {
                var buffer = _overlayRenderSystem.GetBuffer(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderNode(entity, componentHighlight, buffer, highlight);
                return true;
            }
            if (managedType == typeof(SubArea))
            {
                var buffer = _overlayRenderSystem.GetBuffer(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderSubAreas(entity, componentHighlight, buffer, highlight);
                return true;
            }
            if (managedType == typeof(SubObject))
            {
                deps = RenderSubObjects(entity, componentHighlight, inputDeps, highlight);
                return true;
            }
            if (managedType == typeof(Game.Areas.Node))
            {
                var buffer = _overlayRenderSystem.GetBuffer(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderAreaNodes(entity, componentHighlight, buffer);
                return true;
            }
            if (managedType == typeof(Triangle))
            {
                var buffer = _overlayRenderSystem.GetBuffer(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderAreaTriangles(entity, componentHighlight, buffer, highlight);
                return true;
            }
            if (managedType == typeof(Bone))
            {
                var gizmoBatcher = _gizmosSystem.GetGizmosBatcher(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderBone(entity, componentHighlight, gizmoBatcher);
                return true;
            }
            if (managedType == typeof(SpawnLocationElement))
            {
                deps = RenderSpawnLocations(entity, componentHighlight, inputDeps);
                return true;
            }
            if (managedType == typeof(ConnectedBuilding))
            {
                deps = RenderGeometryObjectsGeneric<ConnectedBuilding>(entity, componentHighlight, inputDeps, (data) => data.m_Building);
                return true;
            }
            if (managedType == typeof(WoodResource))
            {
                deps = RenderGeometryObjectsGeneric<WoodResource>(entity, componentHighlight, inputDeps, (data) => data.m_Tree);
                return true;
            }
            if (managedType == typeof(GuestVehicle))
            {
                deps = RenderGeometryObjectsGeneric<GuestVehicle>(entity, componentHighlight, inputDeps, (data) => data.m_Vehicle);
                return true;
            }
            if (managedType == typeof(OwnedVehicle))
            {
                deps = RenderGeometryObjectsGeneric<OwnedVehicle>(entity, componentHighlight, inputDeps, (data) => data.m_Vehicle);
                return true;
            }
            if (managedType == typeof(LayoutElement))
            {
                deps = RenderGeometryObjectsGeneric<LayoutElement>(entity, componentHighlight, inputDeps, (data) => data.m_Vehicle);
                return true;
            }
            if (managedType == typeof(Edge))
            {
                if (componentHighlight.IsMain)
                {
                    var buffer = _overlayRenderSystem.GetBuffer(out JobHandle dependencies);
                    deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                    RenderEdgeNode(entity, null, componentHighlight, buffer);
                    return true;
                }
                if (!componentHighlight.IsMain)
                {
                    var buffer2 = _overlayRenderSystem.GetBuffer(out JobHandle dependencies2);
                    deps = JobHandle.CombineDependencies(inputDeps, dependencies2);
                    RenderEdgeNode(entity, componentHighlight.index == 0, componentHighlight, buffer2);
                    return true;
                }
            }
            if (managedType == typeof(SubBlock)) {
                var gizmoBatcher = _gizmosSystem.GetGizmosBatcher(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderSubBlock(entity, componentHighlight, gizmoBatcher);
                return true;
            }
            if (managedType == typeof(Cell)) {
                var gizmoBatcher = _gizmosSystem.GetGizmosBatcher(out JobHandle dependencies);
                deps = JobHandle.CombineDependencies(inputDeps, dependencies);
                RenderCellBuffer(entity, componentHighlight, gizmoBatcher);
                return true;
            }
            deps = inputDeps;
            return false;
        }
        
        private JobHandle StartPathRenderingJob<T>(JobHandle inputDeps, DynamicBuffer<T> pathEntities, int startIndex, bool highlight, Func<T, Entity> getTargetEntity, Func<T, float2> getDelta)
            where T : unmanaged
        {
            int count = pathEntities.Length - startIndex;
            if (count <= 0)
            {
                return inputDeps;
            }
            
            var pathEntitiesAndDeltas = new NativeList<ValueTuple<Entity, float2, bool>>(count, Allocator.TempJob);

            for (int i = startIndex; i < pathEntities.Length; i++)
            {
                var targetAndDelta = new ValueTuple<Entity, float2, bool>();
                targetAndDelta.Item1 = getTargetEntity(pathEntities[i]);
                targetAndDelta.Item2 = getDelta(pathEntities[i]);
                targetAndDelta.Item3 = highlight && i == startIndex;
                pathEntitiesAndDeltas.Add(targetAndDelta);
            }

            var jobHandle = new InspectObjectToolSystem.PathRendererJob
            {
                gizmoBatcher = _gizmosSystem.GetGizmosBatcher(out JobHandle dependencies),
                pathEntities = pathEntitiesAndDeltas.AsArray(),
                curveComponentLookup = SystemAPI.GetComponentLookup<Curve>(true),
                waypointComponentLookup = SystemAPI.GetComponentLookup<Waypoint>(true),
                ownerComponentLookup = SystemAPI.GetComponentLookup<Owner>(true),
                routeSegmentBufferLookup = SystemAPI.GetBufferLookup<RouteSegment>(true),
                pathElementBufferLookup = SystemAPI.GetBufferLookup<PathElement>(true),
                takeoffLocationComponentLookup = SystemAPI.GetComponentLookup<TakeoffLocation>(true),
                spawnLocationComponentLookup = SystemAPI.GetComponentLookup<SpawnLocation>(true),
                cullingInfoComponentLookup = SystemAPI.GetComponentLookup<CullingInfo>(true),
            }.Schedule(JobHandle.CombineDependencies(inputDeps, dependencies));

            _gizmosSystem.AddGizmosBatcherWriter(jobHandle);

            pathEntitiesAndDeltas.Dispose(jobHandle);

            return jobHandle;
        }

        private void RenderSubNets(Entity entity, ComponentHighlight highlight, OverlayRenderSystem.Buffer buffer)
        {
            DynamicBuffer<SubNet> subNets = EntityManager.GetBuffer<SubNet>(entity);
            bool isElevated = false;
            if (EntityManager.TryGetComponent(entity, out Elevation elevation))
            {
                isElevated = math.any(elevation.m_Elevation > 0.5f);
            }
            if (highlight.IsMain && !subNets.IsEmpty)
            {
                foreach (SubNet subNet in subNets)
                {
                    if (subNet.m_SubNet != Entity.Null)
                    {
                        RenderSubNetLanes(subNet, buffer, isElevated);
                    }
                }
            }
            else
            {
                SubNet subNet = subNets[highlight.index];
                RenderSubNetLanes(subNet, buffer, isElevated);
            }
        }

        private void RenderSubNetLanes(SubNet subNet, OverlayRenderSystem.Buffer buffer, bool isElevated)
        {
            if (subNet.m_SubNet != Entity.Null && EntityManager.HasBuffer<SubLane>(subNet.m_SubNet))
            {
                DynamicBuffer<SubLane> subLanes = EntityManager.GetBuffer<SubLane>(subNet.m_SubNet);
                foreach (SubLane subLane in subLanes)
                {
                    if (subLane.m_SubLane != Entity.Null)
                    {
                        RenderNet(subLane.m_SubLane, buffer, false, isElevated);
                    }
                }
            }
        }

        private void RenderSubLanes(Entity entity, ComponentHighlight highlight, OverlayRenderSystem.Buffer buffer)
        {
            DynamicBuffer<SubLane> subLanes = EntityManager.GetBuffer<SubLane>(entity);
            bool isElevated = false;
            if (EntityManager.TryGetComponent(entity, out Elevation elevation))
            {
                isElevated = math.any(elevation.m_Elevation > 0.5f);
            }
            if (highlight.IsMain)
            {
                foreach (SubLane subLane in subLanes)
                {
                    if (subLane.m_SubLane != Entity.Null)
                    {
                        RenderNet(subLane.m_SubLane, buffer, false, isElevated);
                    }
                }
            }
            else
            {
                SubLane subLane = subLanes[highlight.index];
                if (subLane.m_SubLane != Entity.Null)
                {
                    RenderNet(subLane.m_SubLane, buffer, false, isElevated);
                }
            }
        }

        private void RenderNet(Entity netEntity, OverlayRenderSystem.Buffer buffer, bool isNode, bool isElevated)
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

        private void RenderConnectedEdges(Entity entity, ComponentHighlight highlight, OverlayRenderSystem.Buffer buffer)
        {
            if (entity == Entity.Null)
                return;

            DynamicBuffer<ConnectedEdge> connectedEdges = EntityManager.GetBuffer<ConnectedEdge>(entity);
            if (connectedEdges.Length == 0)
            {
                return;
            }

            if (highlight.IsMain)
            {
                foreach (ConnectedEdge connectedEdge in connectedEdges)
                {
                    if (connectedEdge.m_Edge != Entity.Null)
                    {
                        RenderEdge(connectedEdge.m_Edge, buffer);
                    }
                }
            }
            else if (highlight.index >= 0 && highlight.index < connectedEdges.Length)
            {
                ConnectedEdge connectedEdge = connectedEdges[highlight.index];
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

        private void RenderConnectedNodes(Entity entity, ComponentHighlight componentHighlight, OverlayRenderSystem.Buffer buffer)
        {
            if (entity == Entity.Null)
                return;

            DynamicBuffer<ConnectedNode> connectedNodes = EntityManager.GetBuffer<ConnectedNode>(entity);
            if (connectedNodes.Length == 0)
            {
                return;
            }

            if (componentHighlight.IsMain)
            {
                foreach (ConnectedNode connectedNode in connectedNodes)
                {
                    if (connectedNode.m_Node != Entity.Null)
                    {
                        RenderNode(connectedNode.m_Node, componentHighlight, buffer);
                    }
                }
            }
            else if (componentHighlight.index >= 0 && componentHighlight.index < connectedNodes.Length)
            {
                ConnectedNode connectedNode = connectedNodes[componentHighlight.index];
                if (connectedNode.m_Node != Entity.Null)
                {
                    RenderNode(connectedNode.m_Node, componentHighlight, buffer);
                }
            }
        }

        private void RenderNode(Entity entity, ComponentHighlight componentHighlight, OverlayRenderSystem.Buffer buffer, bool highlight = false)
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
                    editorContainer.m_Prefab.ExistsIn(EntityManager) &&
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
                        RenderConnectedEdges(entity, componentHighlight, buffer);
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

        private void RenderSubAreas(Entity entity, ComponentHighlight componentHighlight, OverlayRenderSystem.Buffer buffer, bool highlight = false) 
        {
            if (entity == Entity.Null)
                return;

            DynamicBuffer<SubArea> areas = EntityManager.GetBuffer<SubArea>(entity);
            if (areas.Length == 0)
            {
                return;
            }
            if (componentHighlight.IsMain)
            {
                foreach (SubArea subArea in areas)
                {
                    RenderAreaTriangles(subArea.m_Area, componentHighlight, buffer, highlight);
                }
            } 
            else if (componentHighlight.index >= 0 && componentHighlight.index < areas.Length)
            {
                ComponentHighlight data = new ComponentHighlight(componentHighlight.type);
                RenderAreaTriangles(areas[componentHighlight.index].m_Area, data, buffer, highlight);
            }
        }

        private void RenderAreaNodes(Entity entity, ComponentHighlight componentHighlight, OverlayRenderSystem.Buffer buffer)
        {
            if (entity == Entity.Null)
                return;

            DynamicBuffer<Game.Areas.Node> areaNodes = EntityManager.GetBuffer<Game.Areas.Node>(entity);
            if (areaNodes.Length == 0)
            {
                return;
            }

            if (componentHighlight.IsMain)
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
            else if (componentHighlight.index >= 0 && componentHighlight.index < areaNodes.Length)
            {
                RenderAreaNode(areaNodes[componentHighlight.index], buffer);
            }
        }

        private void RenderAreaTriangles(Entity entity, ComponentHighlight componentHighlight, OverlayRenderSystem.Buffer buffer, bool highlight = false)
        {
            if (entity == Entity.Null || !EntityManager.HasBuffer<Triangle>(entity) || !EntityManager.HasBuffer<Game.Areas.Node>(entity))
                return;

            DynamicBuffer<Game.Areas.Node> areaNodes = EntityManager.GetBuffer<Game.Areas.Node>(entity);
            DynamicBuffer<Triangle> areaTriangles = EntityManager.GetBuffer<Triangle>(entity);
            if (areaNodes.IsEmpty || areaTriangles.IsEmpty)
            {
                return;
            }

            if (componentHighlight.IsMain)
            {
                foreach (Triangle triangle in areaTriangles)
                {
                    RenderAreaTriangle(triangle, ref areaNodes, buffer, highlight);
                }
            }
            else if (componentHighlight.index >= 0 && componentHighlight.index < areaNodes.Length)
            {
                RenderAreaTriangle(areaTriangles[componentHighlight.index], ref areaNodes, buffer, highlight);
            }
        }

        private void RenderAreaTriangle(Triangle triangle, ref DynamicBuffer<Game.Areas.Node> nodes, OverlayRenderSystem.Buffer buffer, bool highlight = false)
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

        private void RenderCurve(Bezier4x3 bezierCurve, GizmoBatcher gizmoBatcher)
        {
            gizmoBatcher.DrawBezier(bezierCurve, Color.red);
        }

        private void RenderPoint(float3 point, GizmoBatcher gizmoBatcher)
        {
            var color = Color.red;
            gizmoBatcher.DrawLine(new float3(point.x - 1, point.y, point.z), new float3(point.x + 1, point.y, point.z), color);
            gizmoBatcher.DrawLine(new float3(point.x, point.y, point.z - 1), new float3(point.x, point.y, point.z + 1), color);
            gizmoBatcher.DrawLine(new float3(point.x, point.y - 1, point.z), new float3(point.x, point.y + 1, point.z), color);
        }

        private void RenderBounds(Bounds3 bounds, GizmoBatcher gizmoBatcher)
        {
            gizmoBatcher.DrawWireBounds((Bounds)bounds, Color.red);
        }

        private void RenderSegment(Segment segment , GizmoBatcher gizmoBatcher)
        {
            var color = new Color(0.9f, 0.1f, 0.1f, 0.8f);
            gizmoBatcher.DrawBezier(segment.m_Left, color);
            gizmoBatcher.DrawBezier(segment.m_Right, color);
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

        private void RenderSubBlock(Entity entity, ComponentHighlight highlight, GizmoBatcher gizmoBatcher) {
            var subBlocks = EntityManager.GetBuffer<SubBlock>(entity);

            if (highlight.IsMain) {
                foreach (var subBlock in subBlocks) {
                    if (subBlock.m_SubBlock != Entity.Null) {
                        RenderBlock(subBlock.m_SubBlock, gizmoBatcher);
                    }
                }
            } else {
                var subBlock = subBlocks[highlight.index];
                if (subBlock.m_SubBlock != Entity.Null) {
                    RenderBlock(subBlock.m_SubBlock, gizmoBatcher);
                }
            }
        }

        private void RenderCellBuffer(Entity entity, ComponentHighlight highlight, GizmoBatcher gizmoBatcher) {
            var cells = EntityManager.GetBuffer<Cell>(entity);

            if (highlight.IsMain) {
                // Highlight block instead of all cells
                RenderBlock(entity, gizmoBatcher);
            } else {
                if (cells.Length < highlight.index) {
                    return;
                }

                RenderCell(entity, highlight.index, gizmoBatcher);
            }
        }

        private void RenderBlock(Entity entity, GizmoBatcher gizmoBatcher) {
            var block = EntityManager.GetComponentData<Block>(entity);

            var forward = new float3(block.m_Direction.x, 0f, block.m_Direction.y);
            var right = new float3(-block.m_Direction.y, 0f, block.m_Direction.x);
            var rotationMatrix = new float4x4(
                new float4(right.x, right.y, right.z, 0f),
                new float4(0f, 1f, 0f, 0f),
                new float4(forward.x, forward.y, forward.z, 0f),
                new float4(0f, 0f, 0f, 1f)
            );
            
            var transform = math.mul(float4x4.Translate(block.m_Position), rotationMatrix);
            var size = new float3(block.m_Size.x * CellSize, CellSize / 2, block.m_Size.y * CellSize);
            
            gizmoBatcher.DrawWireCube(transform, new float3(0f, 2f, 0f), size, Color.white);
        }

        private void RenderCell(Entity entity, int cellIndex, GizmoBatcher gizmoBatcher) {
            var block = EntityManager.GetComponentData<Block>(entity);
            
            var cellX   = cellIndex % block.m_Size.x;
            var cellY   = cellIndex / block.m_Size.x;
            var forward = new float3(block.m_Direction.x,  0f, block.m_Direction.y);
            var right   = new float3(-block.m_Direction.y, 0f, block.m_Direction.x);
            var rotationMatrix = new float4x4(
                new float4(right.x, right.y, right.z, 0f),
                new float4(0f, 1f, 0f, 0f),
                new float4(forward.x, forward.y, forward.z, 0f),
                new float4(0f, 0f, 0f, 1f)
            );
            var offsetX      = (cellX - block.m_Size.x / 2.0f + 0.5f) * CellSize;
            var offsetY      = -(cellY - block.m_Size.y / 2.0f + 0.5f) * CellSize;
            var cellPosition = block.m_Position + right * offsetX + forward * offsetY;
            var transform    = math.mul(float4x4.Translate(cellPosition), rotationMatrix);
            var size         = new float3(CellSize, CellSize / 2, CellSize);
            
            gizmoBatcher.DrawWireCube(transform, new float3(0f, 2f, 0f), size, Color.white);
        }

        private void RenderEdgeNode(Entity entity, bool? start, ComponentHighlight componentHighlight, OverlayRenderSystem.Buffer buffer)
        {
            if (EntityManager.HasComponent<Edge>(entity))
            {
                Edge edge = EntityManager.GetComponentData<Edge>(entity);
                if (start.HasValue)
                {
                    RenderNode(start.Value ? edge.m_Start: edge.m_End, componentHighlight, buffer);
                }
                else
                {
                    RenderNode(edge.m_Start, componentHighlight, buffer);
                    RenderNode(edge.m_End, componentHighlight, buffer);
                }
            }
        }

        private JobHandle RenderSubObjects(Entity hoveredEntity, ComponentHighlight componentHighlight, JobHandle inputDeps, bool ignoreBuffer = false, bool highlight = false)
        {
            if (ignoreBuffer)
            {
                NativeArray<Entity> objects = new NativeArray<Entity>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                objects[0] = hoveredEntity;
                return RenderGeometryObjects(objects, inputDeps, highlight);
            }
            
            DynamicBuffer<SubObject> subObjects = EntityManager.GetBuffer<SubObject>(hoveredEntity, true);
            if (subObjects.IsEmpty)
            {
                return inputDeps;
            }

            if (componentHighlight.IsMain)
            {
                NativeArray<Entity> objects = new NativeArray<Entity>(subObjects.Length, Allocator.TempJob);
                for (int index = 0; index < subObjects.Length; index++)
                {
                    SubObject subObject = subObjects[index];
                    if (subObject.m_SubObject != Entity.Null)
                    {
                        objects[index] = subObject.m_SubObject;
                    }
                }
                
                return RenderGeometryObjects(objects, inputDeps, highlight);
            }
            else
            {
                SubObject subObject = subObjects[componentHighlight.index];
                NativeArray<Entity> objects = new NativeArray<Entity>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                objects[0] = subObject.m_SubObject;
                return RenderGeometryObjects(objects, inputDeps, highlight);
            }
        }
        
        private void RenderBone(Entity entity, ComponentHighlight componentHighlight, GizmoBatcher gizmoBatcher)
        {
            if (entity == Entity.Null)
                return;

            Transform transform;
            if (!EntityManager.TryGetBuffer<Bone>(entity, true, out DynamicBuffer<Bone> bones) || bones.IsEmpty ||
                !EntityManager.TryGetComponent(entity, out transform))
            {
                return;
            }

            if (EntityManager.TryGetComponent(entity, out InterpolatedTransform interpolatedTransform))
            {
                transform = new Transform(interpolatedTransform.m_Position, interpolatedTransform.m_Rotation);
            }

            DynamicBuffer<ProceduralBone> proceduralBones = default;
            if (EntityManager.TryGetComponent(entity, out PrefabRef prefabRef) &&
                EntityManager.TryGetBuffer(prefabRef, true, out DynamicBuffer<SubMesh> subMeshes) &&
                EntityManager.TryGetBuffer(subMeshes[0].m_SubMesh, true, out DynamicBuffer<ProceduralBone> procBones))
            {
                proceduralBones = procBones;
            }

            if (componentHighlight.IsMain)
            {
                for (int index = 0; index < bones.Length; index++)
                {
                    if (!proceduralBones.IsEmpty && index < proceduralBones.Length)
                    {
                        Transform t = GetBoneTransform(index, bones, proceduralBones);
                        Transform worldPos = ObjectUtils.LocalToWorld(transform, t); 
                        RenderWireBone(ref worldPos, 1.5f, gizmoBatcher);
                    } else {
                        Bone bone = bones[index];
                        RenderWireBone(ref transform, bone, gizmoBatcher);
                    }
                }
            }
            else if (componentHighlight.index >= 0 && componentHighlight.index < bones.Length)
            {
                Transform parentTransform = transform;
                if (!proceduralBones.IsEmpty && componentHighlight.index < proceduralBones.Length)
                {
                    Transform t = GetBoneTransform(componentHighlight.index, bones, proceduralBones);
                    Transform worldPos = ObjectUtils.LocalToWorld(parentTransform, t); 
                    RenderWireBone(ref worldPos, 1.5f, gizmoBatcher);
                }
                else
                {
                    RenderWireBone(ref parentTransform, bones[componentHighlight.index], gizmoBatcher);
                }
            }
        }

        private Transform GetBoneTransform(int index, DynamicBuffer<Bone> bones, DynamicBuffer<ProceduralBone> proceduralBones)
        {
            int i = index;
            float4x4 world = float4x4.identity;
            while (i != -1)
            {
                Bone b = bones[i];
                ProceduralBone pBone = proceduralBones[i];
                float4x4 local = float4x4.TRS(b.m_Position, b.m_Rotation, new float3(1));
                world = math.mul(local, world);
                i = pBone.m_ParentIndex;
            }
            return new Transform(world.c3.xyz, new quaternion(world));
        }

        private void RenderWireBone(ref Transform parentTransform, Bone bone, GizmoBatcher gizmoBatcher)
        {
            Transform transform = ObjectUtils.LocalToWorld(parentTransform, bone.m_Position, bone.m_Rotation);
            float3 tip = transform.m_Position + math.mul(transform.m_Rotation, math.forward()) * math.length(bone.m_Position.yz);
            gizmoBatcher.DrawWireNode(transform.m_Position, 0.1f, Color.green);
            gizmoBatcher.DrawWireNode(tip, 0.12f, new Color(0f, 0.73f, 0f));
            gizmoBatcher.DrawWireCone(transform.m_Position, 0.11f, tip, 0.06f, new Color(0.69f, 0.93f, 0f));
        }

        private void RenderWireBone(ref Transform transform, float length, GizmoBatcher gizmoBatcher)
        {
            float3 tip = transform.m_Position + math.mul(transform.m_Rotation, math.forward()) * length;
            gizmoBatcher.DrawWireNode(transform.m_Position, 0.1f, Color.green);
            gizmoBatcher.DrawWireNode(tip, 0.12f, new Color(0f, 1f, 1f));
            gizmoBatcher.DrawWireCone(transform.m_Position, 0.11f, tip, 0.06f, new Color(0.93f, 0.73f, 0f));
            
            float3 rhs1 = math.rotate(transform.m_Rotation, math.right());
            float3 rhs2 = math.rotate(transform.m_Rotation, math.up());
            float3 rhs3 = math.rotate(transform.m_Rotation, math.forward());
            gizmoBatcher.DrawArrow(transform.m_Position, transform.m_Position + (rhs1 * 0.5f), Color.red, 0.2f, 20f);
            gizmoBatcher.DrawArrow(transform.m_Position, transform.m_Position + (rhs2 * 0.5f), Color.green, 0.2f, 20f);
            gizmoBatcher.DrawArrow(transform.m_Position, transform.m_Position + (rhs3 * 0.5f), Color.blue, 0.2f, 20f);
        }

        private JobHandle RenderSpawnLocations(Entity hoveredEntity, ComponentHighlight componentHighlight, JobHandle inputDeps)
        {
            DynamicBuffer<SpawnLocationElement> spawnLocations = EntityManager.GetBuffer<SpawnLocationElement>(hoveredEntity, true);
            if (spawnLocations.IsEmpty)
            {
                return inputDeps;
            }

            if (componentHighlight.IsMain)
            {
                NativeList<SpawnLocationElement> objects = new NativeList<SpawnLocationElement>(spawnLocations.Length, Allocator.Temp);
                for (int index = 0; index < spawnLocations.Length; index++)
                {
                    SpawnLocationElement location = spawnLocations[index];
                    if (location.m_Type != SpawnLocationType.None &&
                        location.m_SpawnLocation != Entity.Null &&
                        EntityManager.Exists(location.m_SpawnLocation))
                    {
                        objects.Add(location);
                    }
                }
                
                return RenderSpawnLocations(objects.AsArray(), componentHighlight, inputDeps);
            }
            NativeArray<SpawnLocationElement> locations = new NativeArray<SpawnLocationElement>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            locations[0] = spawnLocations[componentHighlight.index];
            return RenderSpawnLocations(locations, componentHighlight, inputDeps);
        }

        private JobHandle RenderSpawnLocations(NativeArray<SpawnLocationElement> locations, ComponentHighlight highlight, JobHandle inputDeps)
        {
            NativeList<Entity> points = new NativeList<Entity>(2, Allocator.Temp);
            NativeList<SpawnLocationElement> curves = new NativeList<SpawnLocationElement>(2, Allocator.Temp);
            NativeList<SpawnLocationElement> areas = new NativeList<SpawnLocationElement>(2, Allocator.Temp);
            foreach (SpawnLocationElement location in locations)
            {
                if (location.m_Type == SpawnLocationType.SpawnLocation)
                {
                    points.Add(location.m_SpawnLocation);
                }
                else if (location.m_Type == SpawnLocationType.HangaroundLocation)
                {
                    areas.Add(location);
                }
                else if (location.m_Type == SpawnLocationType.ParkingLane)
                {
                    curves.Add(location);
                }
            }
            if (!points.IsEmpty)
            {
               inputDeps = RenderGeometryObjects(points.ToArray(Allocator.TempJob), inputDeps, true);
            }
            if (!curves.IsEmpty || !areas.IsEmpty)
            {
                var buffer = _overlayRenderSystem.GetBuffer(out JobHandle dependencies);
                inputDeps = JobHandle.CombineDependencies(inputDeps, dependencies);
                foreach (SpawnLocationElement curveLocation in curves)
                {
                    RenderNet(curveLocation.m_SpawnLocation, buffer, false, true);
                }
                foreach (SpawnLocationElement areaLocation in areas)
                {
                    ComponentHighlight componentHighlight = new ComponentHighlight(highlight.type);
                    RenderAreaTriangles(areaLocation.m_SpawnLocation, componentHighlight, buffer, true);
                }
            }

            return inputDeps;
        }

        private JobHandle RenderGeometryObjectsGeneric<T>(Entity entity, ComponentHighlight componentHighlight, JobHandle inputDeps, Func<T, Entity> getEntity) where T : unmanaged, IBufferElementData
        {
            DynamicBuffer<T> objects = EntityManager.GetBuffer<T>(entity, true);
            if (objects.IsEmpty)
            {
                return inputDeps;
            }

            if (componentHighlight.IsMain)
            {
                NativeList<Entity> datas = new NativeList<Entity>(objects.Length, Allocator.Temp);
                for (int index = 0; index < objects.Length; index++)
                {
                    Entity e = getEntity(objects[index]);
                    if (e != Entity.Null &&
                        EntityManager.Exists(e))
                    {
                        datas.Add(e);
                    }
                }
                NativeArray<Entity> array = datas.ToArray(Allocator.TempJob);
                return RenderGeometryObjects(array, inputDeps, false);
            }
            NativeArray<Entity> data = new NativeArray<Entity>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            data[0] = getEntity(objects[componentHighlight.index]);
            return RenderGeometryObjects(data, inputDeps, true);
        }

        private JobHandle RenderGeometryObjects(NativeArray<Entity> objects, JobHandle inputDeps, bool highlight)
        {
            GizmoBatcher batcher = _gizmosSystem.GetGizmosBatcher(out JobHandle dependencies);
            JobHandle deps = JobHandle.CombineDependencies(inputDeps, dependencies);
            JobHandle handle =  new InspectObjectToolSystem.RenderObjectOutlinesJob()
            {
                objects = objects,
                objectGeometryData = SystemAPI.GetComponentLookup<ObjectGeometryData>(true),
                transformData = SystemAPI.GetComponentLookup<Transform>(true),
                interpolatedTransformData = SystemAPI.GetComponentLookup<InterpolatedTransform>(true),
                prefabRefData = SystemAPI.GetComponentLookup<PrefabRef>(true),
                color = highlight ? Color.cyan : Color.white,
                entityInfo = SystemAPI.GetEntityStorageInfoLookup(),
                batcher = batcher,
            }.ScheduleParallel(objects.Length, 2, deps);
            objects.Dispose(handle);
            return handle;
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
                return new OverlayColor() { outlineColor = new Color(0.24f, 0.26f, 1f, 0.85f), fillColor = new Color(0.03f, 0.04f, 1f, 0.08f) };
            }
            if ((flags & LaneFlags.Secondary) != 0)
            {
                return new OverlayColor() { outlineColor = new Color(0.82f, 1f, 0f, 0.65f), fillColor = new Color(0.97f, 1f, 0.01f, 0.08f) };
            }
            if ((flags & LaneFlags.Track) != 0)
            {
                return new OverlayColor() { outlineColor = new Color(1f, 0.91f, 0.81f, 0.65f), fillColor = new Color(0.96f, 1f, 0.89f, 0.08f) };
            }
            if ((flags & (LaneFlags.OnWater | LaneFlags.HasAuxiliary)) != 0)
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

        internal struct EntityHighlight : IComponentData
        {
            public Entity entity;
            public ComponentHighlight highlight;
        }

        internal struct ComponentHighlight : IEquatable<ComponentHighlight>
        {
            public ComponentType type;
            public int index;

            public bool IsMain => index == -1;
            
            public ComponentHighlight(ComponentType t, int i)
            {
                type = t;
                index = i;
            }
            
            public ComponentHighlight(ComponentType t)
            {
                type = t;
                index = -1;
            }

            public bool Equals(ComponentHighlight other)
            {
                return type.Equals(other.type) && index == other.index;
            }

            public bool EqualsIgnoreIndex(ComponentHighlight other)
            {
                return type.Equals(other.type);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (type.GetHashCode() * 397) ^ index;
                }
            }
        }
    }
}
