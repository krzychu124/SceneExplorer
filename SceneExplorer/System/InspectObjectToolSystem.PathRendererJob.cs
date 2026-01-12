using System;
using Colossal;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SceneExplorer.System
{
    public partial class InspectObjectToolSystem
    {
        [BurstCompile]
        internal struct PathRendererJob : IJob
        {
            [ReadOnly] public NativeArray<ValueTuple<Entity, float2, bool>> pathEntities;
            [ReadOnly] public ComponentLookup<Curve> curveComponentLookup;
            [ReadOnly] public ComponentLookup<Game.Routes.Waypoint> waypointComponentLookup;
            [ReadOnly] public ComponentLookup<Owner> ownerComponentLookup;
            [ReadOnly] public BufferLookup<Game.Routes.RouteSegment> routeSegmentBufferLookup;
            [ReadOnly] public BufferLookup<PathElement> pathElementBufferLookup;
            [ReadOnly] public ComponentLookup<Game.Routes.TakeoffLocation> takeoffLocationComponentLookup;
            [ReadOnly] public ComponentLookup<Game.Objects.SpawnLocation> spawnLocationComponentLookup;
            [ReadOnly] public ComponentLookup<CullingInfo> cullingInfoComponentLookup;
            public GizmoBatcher gizmoBatcher;

            public void Execute()
            {
                Game.Routes.Waypoint? startWayPoint = null;
                foreach (var pathEntity in pathEntities)
                {
                    RenderPath(pathEntity.Item1, pathEntity.Item2, pathEntity.Item3, ref startWayPoint);
                }
            }

            private void RenderPath(Entity pathEntity, float2 usedInterval, bool highlight, ref Game.Routes.Waypoint? startWayPoint)
            {
                if (curveComponentLookup.TryGetComponent(pathEntity, out Curve curve))
                {
                    // Normal lane components have a curve
                    gizmoBatcher.DrawBezier(MathUtils.Cut(curve.m_Bezier, usedInterval), highlight ? Color.white : Color.green, 6);
                }
                // Handle transit line segments
                else if (waypointComponentLookup.TryGetComponent(pathEntity, out var waypoint) && ownerComponentLookup.HasComponent(pathEntity))
                {
                    if (!startWayPoint.HasValue)
                    {
                        startWayPoint = waypoint;
                    }
                    else
                    {
                        if (ownerComponentLookup.TryGetComponent(pathEntity, out var transitLineEntity)
                            && routeSegmentBufferLookup.TryGetBuffer(transitLineEntity.m_Owner, out var routeSegments))
                        {
                            // RouteSegments are between Waypoints, e.g.:
                            // WayPoints:      0   1   2   3
                            // RouteSegments:    0   1   2   3
                            // If the StartWaypoint is 1 and the EndWaypoint is 3, then we need to render segments 1 and 2.
                            // However, the EndWaypoint might be also beyond the end of the transit line.
                            // E.g. the StartWaypoint is 2, and the EndWaypoint is 1.
                            // In this case we need to render segments 2, 3 and 0.
                            var startSegmentIndex = startWayPoint.Value.m_Index;
                            var endSegmentIndex = waypoint.m_Index == 0 ? routeSegments.Length - 1 : waypoint.m_Index - 1;

                            var i = startSegmentIndex;
                            while (true)
                            {
                                var routeSegment = routeSegments[i];
                                
                                if (pathElementBufferLookup.TryGetBuffer(routeSegment.m_Segment, out var pathElements))
                                {
                                    Game.Routes.Waypoint? innerStartWayPoint = null;
                                    foreach (var pathElement in pathElements)
                                    {
                                        RenderPath(pathElement.m_Target, pathElement.m_TargetDelta, false, ref innerStartWayPoint);
                                    }
                                }

                                if (i == endSegmentIndex)
                                {
                                    break;
                                }

                                i++;
                                if (i == routeSegments.Length)
                                {
                                    i = 0;
                                }
                            }
                        }
                        startWayPoint = null;
                    }
                }
                // Handle the others (takoff and spawn locations)
                else if ((takeoffLocationComponentLookup.HasComponent(pathEntity) || spawnLocationComponentLookup.HasComponent(pathEntity))
                    && cullingInfoComponentLookup.TryGetComponent(pathEntity, out var cullingInfo))
                {
                    var locationBounds = cullingInfo.m_Bounds;

                    gizmoBatcher.DrawWireBounds((Bounds)locationBounds, highlight ? Color.white : Color.green);
                }
                else
                {
                    // The entity has no renderable component
                }
            }
        }
    }
}
