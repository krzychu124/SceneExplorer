using Colossal;
using Colossal.Mathematics;
using Game.Prefabs;
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
        internal struct RenderObjectOutlinesJob : IJobFor
        {
            [ReadOnly] public ComponentLookup<Game.Objects.Transform> transformData;
            [ReadOnly] public ComponentLookup<InterpolatedTransform> interpolatedTransformData;
            [ReadOnly] public ComponentLookup<ObjectGeometryData> objectGeometryData;
            [ReadOnly] public ComponentLookup<PrefabRef> prefabRefData;
            [ReadOnly] public NativeArray<Entity> objects;
            [ReadOnly] public Color color;
            [ReadOnly] public EntityStorageInfoLookup entityInfo;
            public GizmoBatcher batcher;
            
            public void Execute(int index)
            {
                Entity entity = objects[index];
                if (entity == Entity.Null || !entityInfo.Exists(entity))
                {
                    return;
                }

                Color outlineColor = color;
                if (transformData.TryGetComponent(entity, out Game.Objects.Transform transform) &&
                    prefabRefData.TryGetComponent(entity, out PrefabRef prefabRef) &&
                    objectGeometryData.TryGetComponent(prefabRef.m_Prefab, out ObjectGeometryData objectData))
                {
                    if ((objectData.m_Flags & Game.Objects.GeometryFlags.Standing) != 0)
                    {
                        float4x4 trs = new float4x4(transform.m_Rotation, transform.m_Position);
                        if ((objectData.m_Flags & Game.Objects.GeometryFlags.CircularLeg) != 0)
                        {
                            batcher.DrawWireCylinder(trs, new float3(0f, objectData.m_LegSize.y * 0.5f, 0f), objectData.m_LegSize.x * 0.5f, objectData.m_LegSize.y, outlineColor);
                        }
                        else
                        {
                            batcher.DrawWireCube(trs, new float3(0f, objectData.m_LegSize.y * 0.5f, 0f), objectData.m_LegSize, outlineColor);
                        }
                        if ((objectData.m_Flags & Game.Objects.GeometryFlags.Circular) != 0)
                        {
                            float num = objectData.m_Size.y - objectData.m_LegSize.y;
                            batcher.DrawWireCylinder(trs, new float3(0f, objectData.m_LegSize.y + num * 0.5f, 0f), objectData.m_Size.x * 0.5f, num, outlineColor);
                            return;
                        }
                        objectData.m_Bounds.min.y = objectData.m_LegSize.y;
                        float3 center = MathUtils.Center(objectData.m_Bounds);
                        float3 size = MathUtils.Size(objectData.m_Bounds);
                        batcher.DrawWireCube(trs, center, size, outlineColor);
                    }
                    else
                    {
                        float4x4 trs2 = new float4x4(transform.m_Rotation, transform.m_Position);
                        if (interpolatedTransformData.TryGetComponent(entity, out InterpolatedTransform interpolated))
                        {
                            trs2 = new float4x4(interpolated.m_Rotation, interpolated.m_Position);
                        }
                        if ((objectData.m_Flags & Game.Objects.GeometryFlags.Circular) != 0)
                        {
                            batcher.DrawWireCylinder(trs2, new float3(0f, objectData.m_Size.y * 0.5f, 0f), objectData.m_Size.x * 0.5f, objectData.m_Size.y, outlineColor);
                            return;
                        }
                        float3 center2 = MathUtils.Center(objectData.m_Bounds);
                        float3 size2 = MathUtils.Size(objectData.m_Bounds);
                        batcher.DrawWireCube(trs2, center2, size2, outlineColor);
                    }
                } 
            }
        }
    }
}
