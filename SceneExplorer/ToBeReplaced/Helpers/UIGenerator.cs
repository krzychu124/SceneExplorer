using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Colossal.IO;
using Colossal.Logging.Utils;
using Game.Net;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
using SceneExplorer.Services;
using SceneExplorer.ToBeReplaced.Helpers.ContentItems;
using SceneExplorer.ToBeReplaced.Helpers.ContentSections;
using SceneExplorer.ToBeReplaced.Windows;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SceneExplorer.ToBeReplaced.Helpers
{
    public static class UIGenerator
    {
        internal const int MAX_GENERATOR_DEPTH = 10;
        // private static Dictionary<ComponentType, List<FieldInfo>> _componentFields = new Dictionary<ComponentType, List<FieldInfo>>();
        // private static Dictionary<Type, List<FieldInfo>> _otherFields = new Dictionary<Type, List<FieldInfo>>();
        private static FieldInfo _pathNode_SearchKeyField = typeof(PathNode).GetField("m_SearchKey", BindingFlags.Instance | BindingFlags.NonPublic);
        // private static HashSet<Type> _skipTypes = new HashSet<Type>();
        private static TypeDescriptorService _descriptorService;

        static UIGenerator()
        {
            _descriptorService = TypeDescriptorService.Instance;
            // _skipTypes.Add(typeof(EntityArchetype));
            // _skipTypes.Add(typeof(FixedString32Bytes));
            // _skipTypes.Add(typeof(FixedString64Bytes));
            // _skipTypes.Add(typeof(FixedString128Bytes));
            // _skipTypes.Add(typeof(FixedString512Bytes));
            // _skipTypes.Add(typeof(FixedString4096Bytes));
            // _skipTypes.Add(typeof(FixedString4096Bytes));
            // _skipTypes.Add(typeof(GCHandle));
        }

        // public static List<FieldInfo> GetFields(ComponentType type) {
        //     return _descriptorService.GetFields(type);
        // }
        //
        // public static List<FieldInfo> GetFields(Type type) {
        //     return _descriptorService.GetFields(type);
        // }

        public static ISectionItem GetSectionItem(string fieldName, object o, string sectionName, int depth)
        {
            Logging.DebugEvaluation($"[{depth}] {sectionName} | {fieldName}: {o?.ToString()}");
            if (o == null)
            {
                return new TextItem(fieldName, "null");
            }
            if (depth >= MAX_GENERATOR_DEPTH)
            {
                return new TextItem(fieldName, "<reached_max_depth>");
            }

            if (fieldName.Equals("m_SurfaceAssets") || fieldName.Equals("m_GeometryAsset"))
            {
                return new TextItem(fieldName, "<not supported>");
            }

            switch (o)
            {
                case string s:
                    return new TextItem(fieldName, s);
                case Entity e:
                    return new EntityItem(fieldName, e, sectionName);
                case PrefabData prefabData:
                    return new PrefabDataItem(fieldName, prefabData);
                case PathfindCosts pathfindCosts:
                    return new TextItem(fieldName, $"(Behaviour, Comfort, Money, Time): {pathfindCosts.m_Value}", true);
                case PathfindCostInfo pathfindCostInfo:
                    return new TextItem(fieldName, $"(Behaviour, Comfort, Money, Time), {pathfindCostInfo.ToPathfindCosts().m_Value}", true);

                case EntityArchetype eat:
                    using (NativeArray<ComponentType> componentTypes = eat.GetComponentTypes())
                    {
                        string value = string.Join(", ", componentTypes.Select(c => c.GetManagedType().Name));
                        componentTypes.Dispose();
                        return new TextItem(fieldName, $"({value})", true);
                    }

                case CompositionFlags cFlags:
                    List<ISectionItem> itemsFlags = new List<ISectionItem>()
                    {
                        new TextItem(nameof(CompositionFlags.m_General), cFlags.m_General.ToString(), true),
                        new TextItem(nameof(CompositionFlags.m_Left), cFlags.m_Left.ToString(), true),
                        new TextItem(nameof(CompositionFlags.m_Right), cFlags.m_Right.ToString(), true),
                    };
                    return new ObjectItem($"{fieldName}", itemsFlags);
                case StaticObjectPrefab staticObject:
                    return new NotSupportedData($"{fieldName}: {o.GetType().Name} [{staticObject.name}]", true);
                /* TODO type gone?
                 case UnityGuid guid:
                    return new NotSupportedData($"{fieldName}: {o.GetType().Name} [{guid.Guid.ToString()}]", true);*/
                case FixedString32Bytes:
                case FixedString64Bytes:
                case FixedString128Bytes:
                case FixedString512Bytes:
                case FixedString4096Bytes:
                    return new TextItem(fieldName, $"\"{o.ToString()}\" ({o.GetType().Name})");
                case GCHandle handle:
                    return new TextItem(fieldName, StringifyGcHandle(handle));
                case FeaturePrefab featurePrefab:
                    return new TextItem(fieldName, $"\"{featurePrefab.name}\", prefabBase: {featurePrefab.prefab?.name} ({o.GetType().Name})");
                case Unlockable unlockable:
                    // Locks[{unlockable.m_Locks.Length}]
                    return new TextItem(fieldName, $"Name: {unlockable.name}, RequireAll[{unlockable.m_RequireAll.Length}], RequireAny[{unlockable.m_RequireAny.Length}], ignoreDeps: {unlockable.m_IgnoreDependencies}");
                case List<EmissiveProperties.LightProperties> lightProperties:
                    List<ISectionItem> propertyItems = lightProperties.Select(item => {
                        List<ISectionItem> items = new List<ISectionItem>();
                        ObjectItem.PrepareItems(item.GetType(), item, item.GetType().Name, items, ++depth);
                        return new ObjectItem(item.GetType().Name, items);
                    }).ToList<ISectionItem>();
                    return new ObjectItem($"{fieldName} {lightProperties.GetType().GetElementType()?.Name}({lightProperties.Count})", propertyItems);

                case List<ComponentBase> list:
                    if (list.Count == 0)
                    {
                        return new TextItem(fieldName, " (0)");
                    }
                    List<ISectionItem> items2 = list.Select(item => {
                        List<ISectionItem> items = new List<ISectionItem>();
                        ObjectItem.PrepareItems(item.GetType(), item, item.GetType().Name, items, ++depth);
                        return new ObjectItem(item.GetType().Name, items);
                    }).ToList<ISectionItem>();
                    return new ObjectItem($"{fieldName} ({items2.Count})", items2);
                case NetPieceRequirements[] requirements:
                    return new TextItem($"{fieldName} | {nameof(NetPieceRequirements)}[{requirements.Length}]", $"{string.Join(", ", requirements)}", true);
                case object[] array:
                    if (array.Length == 0)
                    {
                        return new TextItem($"{fieldName} {array.GetType().GetElementType()?.Name}[0]", string.Empty);
                    }
                    if (sectionName.Equals("Unlockable"))
                    {
                        Logging.DebugEvaluation($"Unlockable 1 {fieldName}: {array.Length}");
                        depth = MAX_GENERATOR_DEPTH-2;
                    }
                    List<ISectionItem> items4 = array.Where(item => item != null).Select(item => {
                        List<ISectionItem> items = new List<ISectionItem>();
                        ObjectItem.PrepareItems(item.GetType(), item, item.GetType().Name, items, ++depth);
                        return new ObjectItem(item.GetType().Name, items);
                    }).ToList<ISectionItem>();
                    return new ObjectItem($"{fieldName} {array.GetType().GetElementType()?.Name}[{array.Length}]", items4);
                case Segment:
                case EdgeNodeGeometry:
                case ColorSet:
                case SignalAnimation:
                case PrefabBase:
                    if (sectionName.Equals("Unlockable"))
                    {
                        Logging.DebugEvaluation($"Unlockable 2 {fieldName} {o.GetType()}");
                        depth = MAX_GENERATOR_DEPTH-2;
                    }
                    List<ISectionItem> items = new List<ISectionItem>();
                    ObjectItem.PrepareItems(o.GetType(), o, fieldName, items, ++depth);
                    return new ObjectItem(fieldName, items);
            }

            return new TextItem(fieldName, o.ToString(), o is Enum);
        }

        public static ISection GetUIForComponent(ComponentType type, Entity entity)
        {

            if (_descriptorService.IsTagComponent(type))
            {
                return new TagSection(type.GetManagedType().Name);
            }

            if (type.IsComponent)
            {
                return new ComponentSection(type, entity);
            }

            if (type.IsBuffer)
            {
                return new BufferSection(type, entity);
            }

            return new NotSupportedData(type.GetManagedType().GetTypeName() + $"({type.TypeIndex})");
        }

        public static IInspectableComponent CalculateComponentInfo(ComponentType type, Entity entity, bool isSnapshot)
        {

            if (type.IsZeroSized /*IsTagComponent(type)*/)
            {
                if (type.IsSharedComponent)
                {
                    return new SharedComponentInfo(type, type.GetManagedType().GetTypeName(), _descriptorService.GetFields(type), isSnapshot);
                }
                return new EntityTagComponentInfo(type, type.GetManagedType().GetTypeName(), new List<FieldInfo>(), isSnapshot);
            }
            if (type.IsComponent)
            {
                if (type.GetManagedType() == typeof(PrefabRef))
                {
                    return new PrefabRefComponentInfo(type, type.GetManagedType().GetTypeName(), _descriptorService.GetFields(type), isSnapshot);
                }
                if (type.GetManagedType() == typeof(PrefabData))
                {
                    return new PrefabDataComponentInfo(type, type.GetManagedType().GetTypeName(), _descriptorService.GetFields(type), isSnapshot);
                }
                if (type.IsManagedComponent || type.GetManagedType().IsClass)
                {
                    return new ManagedComponentInfo(type, type.GetManagedType().GetTypeName(), _descriptorService.GetFields(type), isSnapshot);
                }
                if (!type.GetManagedType().IsClass)
                {
                    return new UnmanagedComponentInfo(type, type.GetManagedType().GetTypeName(), _descriptorService.GetFields(type), isSnapshot);
                }

                return new CommonComponentInfo(type, type.GetManagedType().GetTypeName(), _descriptorService.GetFields(type), isSnapshot);

            }
            if (type.IsBuffer)
            {
                return new EntityBufferComponentInfo(type, type.GetManagedType().GetTypeName(), _descriptorService.GetFields(type), isSnapshot);
            }

            return new EntityNotSupportedComponent(type, type.GetManagedType().GetTypeName(), new List<FieldInfo>(), isSnapshot);
        }

        public static List<IInspectableObject> CalculateComponentInspectableInfo(ComponentType type, List<FieldInfo> fields, IInspectableObject parent, bool isSnapshot)
        {
            List<IInspectableObject> result = new List<IInspectableObject>(fields.Count);

            Logging.DebugEvaluation($"Reading types of: {type.GetManagedType().FullName}");
            bool isPrefabData = type.GetManagedType() == typeof(PrefabData);
            foreach (FieldInfo fieldInfo in fields)
            {
                IInspectableObject obj = GetInspectableObjectData(fieldInfo, parent, isSnapshot);
                Logging.DebugEvaluation($"Field: {fieldInfo.Name}({fieldInfo.FieldType.FullName})");
                if (isPrefabData && fieldInfo.Name.Equals("m_Index"))
                {
                    obj.CanInspectValue = true;
                }
                result.Add(obj);
            }

            return result;
        }

        public static IInspectableObject GetInspectableObjectData(FieldInfo fieldInfo, IInspectableObject parent, bool isSnapshot)
        {

            if (typeof(IFormattable).IsAssignableFrom(fieldInfo.FieldType))
            {
                return new CommonInspectableObject(fieldInfo, (info, o) => info.GetValue(o), isSnapshot);
            }

            if (fieldInfo.FieldType == typeof(Entity))
            {
                return new InspectableEntity(fieldInfo, parent, isSnapshot);
            }

            if (fieldInfo.FieldType == typeof(string) || fieldInfo.FieldType.IsPrimitive)
            {
                return new CommonInspectableObject(fieldInfo, (info, o) => info.GetValue(o), isSnapshot);
            }

            if (fieldInfo.FieldType.IsArray)
            {
                Logging.DebugEvaluation($"Is Array: {fieldInfo.FieldType.FullName} | {fieldInfo.FieldType.GetElementType()?.FullName}");
                var elType = fieldInfo.FieldType.GetElementType();
                return new ArrayIterableObject(fieldInfo, elType, _descriptorService.GetFields(elType), parent, isSnapshot);
            }

            if (Extensions.IsList(fieldInfo.FieldType) && fieldInfo.Name.Equals("components") && fieldInfo.FieldType == typeof(List<ComponentBase>))
            {
                Logging.DebugEvaluation($"Is List: {fieldInfo.FieldType.FullName}");
                return new PrefabComponentsIterableObject(fieldInfo, parent, isSnapshot);
            }

            if (Extensions.IsList(fieldInfo.FieldType))
            {
                Logging.DebugEvaluation($"Is Generic List: {fieldInfo.FieldType.FullName} {string.Join(", ", fieldInfo.FieldType.GenericTypeArguments.Select(t => t.FullName))}");
                return new GenericListObject(fieldInfo, parent, isSnapshot);
            }

            List<FieldInfo> fields = _descriptorService.GetFields(fieldInfo.FieldType);
            if (fields.Count > 0 && fieldInfo.FieldType != typeof(PathNode))
            {
                return new ComplexObject(fieldInfo, fields, isSnapshot);
            }

            return new CommonInspectableObject(fieldInfo, (info, o) => {
                if (o == null) return "null";
                object v = info.GetValue(o);
                switch (v)
                {
                    case PathNode pn:
                        // COMPILATION ERROR NOTE: Requires assembly publicizing and manual modification of field visibility
                        return $"PathNode(lane index: {pn.GetLaneIndex() & 255} (raw: {pn.GetLaneIndex()}, ownerIndex: {((ulong)_pathNode_SearchKeyField.GetValue(pn)) >> 32}), curve pos: {pn.GetCurvePos()} isSecondary: {pn.IsSecondary()})";
                    case IFormattable f:
                        return f;

                    case EntityArchetype archetype:
                        using (NativeArray<ComponentType> componentTypes = archetype.GetComponentTypes())
                        {
                            return string.Join(", ", componentTypes.Select(c => c.GetManagedType().Name));
                        }

                    case FixedString32Bytes:
                    case FixedString64Bytes:
                    case FixedString128Bytes:
                    case FixedString512Bytes:
                    case FixedString4096Bytes:
                        return $"\"{v.ToString()}\" ({v.GetType().Name})";
                    case GCHandle handle:
                        return StringifyGcHandle(handle);
                    default:
                        return info.FieldType.FullName;
                }
            }, isSnapshot);
        }

        public static string StringifyGcHandle(GCHandle handle)
        {
            return !handle.IsAllocated ? "<GC_HANDLE NOT-ALLOCATED>" : $"{(handle.Target != null ? $"\"{handle.Target}\" ({handle.Target.GetType().FullName})" : "")}";
        }
    }
}
