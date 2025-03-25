using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Game.Prefabs;
using SceneExplorer.Services;
using Unity.Entities;
using UnityEngine;

namespace SceneExplorer.ToBeReplaced.Helpers
{
    public static class Extensions
    {
        private static TypeIndex _prefabRefTypeIndex;
        private static TypeIndex _prefabDataTypeIndex;

        static Extensions()
        {
            _prefabRefTypeIndex = TypeManager.GetTypeIndex(typeof(PrefabRef));
            _prefabDataTypeIndex = TypeManager.GetTypeIndex(typeof(PrefabData));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(this Entity e)
        {
            return e.Index >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ExistsIn(this Entity e, EntityManager manager)
        {
            return e != Entity.Null && e.IsValid() && manager.Exists(e);
        }

        public static object GetComponentDataByType(this Type type, EntityManager entityManager, Entity e)
        {
            MethodInfo getComponentData = typeof(EntityManager).GetMethod(nameof(EntityManager.GetComponentData), new Type[] { typeof(Entity) });
            MethodInfo genericGetComponentData = getComponentData.MakeGenericMethod(type);
            return genericGetComponentData.Invoke(entityManager, new object[] { e });
        }

        public static object GetSharedComponentDataByType(this Type type, EntityManager entityManager, Entity e)
        {
            MethodInfo getComponentData = typeof(EntityManager).GetMethod(nameof(EntityManager.GetSharedComponentManaged), new Type[] { typeof(Entity) });
            MethodInfo genericGetComponentData = getComponentData.MakeGenericMethod(type);
            return genericGetComponentData.Invoke(entityManager, new object[] { e });
        }

        public static List<object> GetComponentBufferArrayByType(this Type type, EntityManager entityManager, Entity e)
        {
            MethodInfo method = typeof(EntityManager).GetMethod(nameof(EntityManager.GetBuffer), new Type[] { typeof(Entity), typeof(bool) });
            MethodInfo generic = method.MakeGenericMethod(type);
            object bufferValue = generic.Invoke(entityManager, new object[] { e, true });
            IEnumerable data = (IEnumerable)bufferValue.GetType().GetMethod("AsNativeArray", BindingFlags.Instance | BindingFlags.Public).Invoke(bufferValue, null);
            List<object> objects = new List<object>();
            foreach (object o in data)
            {
                objects.Add(o);
            }
            IDisposable disposable = data as IDisposable;
            disposable.Dispose();
            return objects;
        }

        public static string TryGetPrefabName(this Entity e, EntityManager manager, PrefabSystem prefabSystem, out string prefabType)
        {
            if (!e.ExistsIn(manager))
            {
                prefabType = string.Empty;
                if (e != Entity.Null && SnapshotService.Instance.TryGetSnapshot(e, out SnapshotService.EntitySnapshotData data))
                {
                    if (data.TryGetData(ComponentType.FromTypeIndex(_prefabRefTypeIndex), out object value) && value != null)
                    {
                        PrefabRef prefabRef = (PrefabRef)value;
                        if (prefabSystem.TryGetPrefab(prefabRef, out PrefabBase prefab))
                        {
                            prefabType = "PrefabRef";
                            return $"[S] {prefab.name}";
                        }
                    }
                    else if (data.TryGetData(ComponentType.FromTypeIndex(_prefabDataTypeIndex), out object value2) && value2 != null)
                    {
                        PrefabData prefabData = (PrefabData)value2;
                        if (prefabSystem.TryGetPrefab(prefabData, out PrefabBase prefab))
                        {
                            prefabType = "PrefabData";
                            return $"[S] {prefab.name}";
                        }
                    }
                }
                return null;
            }

            if (manager.HasComponent<PrefabRef>(e))
            {
                PrefabRef prefabRef = manager.GetComponentData<PrefabRef>(e);
                if (prefabSystem.TryGetPrefab(prefabRef, out PrefabBase prefab))
                {
                    prefabType = "PrefabRef";
                    return prefab.name;
                }
            }
            else if (manager.HasComponent<PrefabData>(e))
            {
                PrefabData prefabData = manager.GetComponentData<PrefabData>(e);
                if (prefabSystem.TryGetPrefab(prefabData, out PrefabBase prefab))
                {
                    prefabType = "PrefabData";
                    return prefab.name;
                }
            }
            prefabType = string.Empty;
            return null;
        }

        public static bool IsNumericType(this object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsNumericType(this Type type)
        {
            if (type.Namespace == "Unity.Mathematics")
            {
                return true;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsEnumerable(object myProperty)
        {
            return typeof(IEnumerable).IsInstanceOfType(myProperty)
                || typeof(IEnumerable<>).IsInstanceOfType(myProperty);
        }

        public static bool IsCollection(Type type)
        {
            return typeof(ICollection).IsAssignableFrom(type)
                || typeof(ICollection<>).IsAssignableFrom(type);
        }

        public static bool IsList(Type type)
        {
            return typeof(IList).IsAssignableFrom(type)
                || typeof(IList<>).IsAssignableFrom(type);
        }
    }
}
