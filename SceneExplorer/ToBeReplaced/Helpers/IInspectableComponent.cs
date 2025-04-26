using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Colossal.Entities;
using Game.Prefabs;
using SceneExplorer.Services;
using Unity.Entities;

namespace SceneExplorer.ToBeReplaced.Helpers
{
    public interface IInspectableComponent
    {
        ComponentType Type { get; }
        SpecialComponentType SpecialType { get; }
        string Name { get; }
        List<FieldInfo> DataFields { get; }
        List<IInspectableObject> Objects { get; }
        bool UpdateBindings(Entity entity);
        bool RefreshValues(Entity entity);
        void Dispose();
        void ShowDetails();
        void HideDetails();
        bool DetailedView { get; }
        bool IsSnapshot { get; }
        bool IsDisabled { get; }
    }

    public interface IEntityComponent : IInspectableComponent
    {
    }

    public interface IEntityBufferComponent : IInspectableComponent
    {
        int CurrentPage { get; }
        int PageCount { get; }
        int ItemCount { get; }
        List<IInspectableObject> DataArray { get; }
        void PreviousPage();
        void NextPage();
    }

    public interface IEntityTagComponent : IInspectableComponent
    {
    }

    public interface IEntityNotSupportedComponent : IInspectableComponent
    {
    }

    public abstract class ComponentInfoBase : IInspectableComponent
    {
        private bool _disposed;
        protected HashSet<object> visitedObjects = new HashSet<object>();

        protected ComponentInfoBase(ComponentType type, string name, List<FieldInfo> fields, bool isSnapshot) {
            Type = type;
            Name = name;
            DataFields = fields;
            IsSnapshot = isSnapshot;
            Objects = UIGenerator.CalculateComponentInspectableInfo(type, fields, null, isSnapshot);
        }

        public ComponentType Type { get; }
        public SpecialComponentType SpecialType { get; protected set; }

        public string Name { get; }
        public List<FieldInfo> DataFields { get; private set; }
        public List<IInspectableObject> Objects { get; private set; }

        public bool DetailedView { get; private set; }
        public bool IsSnapshot { get; }
        public bool IsDisabled { get; set; }
        protected object _componentData;

        public bool UpdateBindings(Entity entity) {
            if (!_disposed)
            {
                Type managedType = Type.GetManagedType();
                if ((entity.IsValid() && (IsSnapshot || entity.ExistsIn(World.DefaultGameObjectInjectionWorld.EntityManager))) && managedType != null)
                {
                    try
                    {
                        _componentData = UpdateBindingsInternal(entity);
                    }
                    catch
                    {
                        Logging.DebugEvaluation($"Failed for: E: {entity}, type: {managedType.FullName}");
                    }
                }
                else
                {
                    _componentData = null;
                }
            }
            else
            {
                _componentData = null;
            }
            return !_disposed;
        }

        public bool RefreshValues(Entity entity) {
            if (_componentData != null)
            {
                RefreshValuesInternal(entity, _componentData);
            }
            return true;
        }

        public virtual object UpdateBindingsInternal(Entity e) {
            return null;
        }

        public virtual bool RefreshValuesInternal(Entity entity, object previousData) {
            return false;
        }

        public virtual void Dispose() {
            DataFields = null;
            Objects = null;
            _disposed = true;
        }

        public void ShowDetails() {
            DetailedView = true;
        }

        public void HideDetails() {
            DetailedView = false;
        }
    }

    public sealed class UnmanagedComponentInfo : ComponentInfoBase, IEntityComponent
    {
        public UnmanagedComponentInfo(ComponentType type, string name, List<FieldInfo> fields, bool isSnapshot) : base(type, name, fields, isSnapshot) {
            Logging.DebugEvaluation($"[Component-Unmanaged] Type: {type.GetManagedType().FullName}, name: {name} | {string.Join(", ", fields.Select(f => $"{f.Name}{(f.IsPrivate ? "[P]" : "")}: {f.FieldType.Name}"))}");
            SpecialType = SpecialComponentType.UnManaged;
            if (type.IsEnableable)
            {
                SpecialType |= SpecialComponentType.Enableable;
            }
        }

        public override object UpdateBindingsInternal(Entity entity) {
            if (IsSnapshot && SnapshotService.Instance.TryGetSnapshot(entity, out SnapshotService.EntitySnapshotData data) && data.TryGetData(Type, out object value))
            {
                return value;
            }
            if ((SpecialType & SpecialComponentType.Enableable) != 0)
            {
                IsDisabled = !World.DefaultGameObjectInjectionWorld.EntityManager.IsComponentEnabled(entity, Type);
            }
            return Type.GetManagedType().GetComponentDataByType(World.DefaultGameObjectInjectionWorld.EntityManager, entity);
        }

        public override bool RefreshValuesInternal(Entity entity, object previousData) {
            visitedObjects.Clear();
            foreach (IInspectableObject o in Objects)
            {
                o.UpdateValue(_componentData, false, visitedObjects);
            }
            return true;
        }
    }

    public sealed class ManagedComponentInfo : ComponentInfoBase, IEntityComponent
    {
        public ManagedComponentInfo(ComponentType type, string name, List<FieldInfo> fields, bool isSnapshot) : base(type, name, fields, isSnapshot) {
            Logging.DebugEvaluation($"[Component-Managed] Type: {type.GetManagedType().FullName}, name: {name} | {string.Join(", ", fields.Select(f => $"{f.Name}{(f.IsPrivate ? "[P]" : "")}: {f.FieldType.Name}"))}");
            SpecialType = SpecialComponentType.Managed;
        }

        public override object UpdateBindingsInternal(Entity entity) {
            if (IsSnapshot && SnapshotService.Instance.TryGetSnapshot(entity, out SnapshotService.EntitySnapshotData data) && data.TryGetData(Type, out object value))
            {
                return value;
            }
            if ((SpecialType & SpecialComponentType.Enableable) != 0)
            {
                IsDisabled = !World.DefaultGameObjectInjectionWorld.EntityManager.IsComponentEnabled(entity, Type);
            }
            return Type.GetManagedType().GetComponentDataByType(World.DefaultGameObjectInjectionWorld.EntityManager, entity);
        }

        public override bool RefreshValuesInternal(Entity entity, object previousData) {
            visitedObjects.Clear();
            foreach (IInspectableObject o in Objects)
            {
                o.UpdateValue(_componentData, false, visitedObjects);
            }
            return true;
        }
    }

    public sealed class CommonComponentInfo : ComponentInfoBase, IEntityComponent
    {
        public CommonComponentInfo(ComponentType type, string name, List<FieldInfo> fields, bool isSnapshot) : base(type, name, fields, isSnapshot) {
            Logging.DebugEvaluation($"[Component-Common] Type: {type.GetManagedType().FullName}, name: {name} | {string.Join(", ", fields.Select(f => $"{f.Name}{(f.IsPrivate ? "[P]" : "")}: {f.FieldType.Name}"))}");
            SpecialType = SpecialComponentType.None;
        }

        public override object UpdateBindingsInternal(Entity entity) {
            if (IsSnapshot && SnapshotService.Instance.TryGetSnapshot(entity, out SnapshotService.EntitySnapshotData data) && data.TryGetData(Type, out object value))
            {
                return value;
            }
            if ((SpecialType & SpecialComponentType.Enableable) != 0)
            {
                IsDisabled = !World.DefaultGameObjectInjectionWorld.EntityManager.IsComponentEnabled(entity, Type);
            }
            return Type.GetManagedType().GetComponentDataByType(World.DefaultGameObjectInjectionWorld.EntityManager, entity);
        }

        public override bool RefreshValuesInternal(Entity entity, object previousData) {
            visitedObjects.Clear();
            foreach (IInspectableObject o in Objects)
            {
                o.UpdateValue(_componentData, false, visitedObjects);
            }
            return true;
        }
    }

    public sealed class SharedComponentInfo : ComponentInfoBase, IEntityComponent
    {
        public SharedComponentInfo(ComponentType type, string name, List<FieldInfo> fields, bool isSnapshot) : base(type, name, fields, isSnapshot) {
            Logging.DebugEvaluation($"[Component-Common] Type: {type.GetManagedType().FullName}, name: {name} | {string.Join(", ", fields.Select(f => $"{f.Name}{(f.IsPrivate ? "[P]" : "")}: {f.FieldType.Name}"))}");
            SpecialType = SpecialComponentType.Shared;
            if (type.IsEnableable)
            {
                SpecialType |= SpecialComponentType.Enableable;
            }
        }

        public override object UpdateBindingsInternal(Entity entity) {
            if (IsSnapshot && SnapshotService.Instance.TryGetSnapshot(entity, out SnapshotService.EntitySnapshotData data) && data.TryGetData(Type, out object value))
            {
                return value;
            }
            if ((SpecialType & SpecialComponentType.Enableable) != 0)
            {
                IsDisabled = !World.DefaultGameObjectInjectionWorld.EntityManager.IsComponentEnabled(entity, Type);
            }
            return Type.GetManagedType().GetSharedComponentDataByType(World.DefaultGameObjectInjectionWorld.EntityManager, entity);
        }

        public override bool RefreshValuesInternal(Entity entity, object previousData) {
            visitedObjects.Clear();
            foreach (IInspectableObject o in Objects)
            {
                o.UpdateValue(_componentData, false, visitedObjects);
            }
            return true;
        }
    }

    public sealed class PrefabRefComponentInfo : ComponentInfoBase, IEntityComponent
    {
        public string PrefabRefDataName { get; private set; }
        public bool MissingPrefab { get; private set; }

        public PrefabRefComponentInfo(ComponentType type, string name, List<FieldInfo> fields, bool isSnapshot) : base(type, name, fields, isSnapshot) {
            Logging.DebugEvaluation($"[Component-PrefabRef] Type: {type.GetManagedType().FullName}, name: {name} | {string.Join(", ", fields.Select(f => $"{f.Name}{(f.IsPrivate ? "[P]" : "")}: {f.FieldType.Name}"))}");
            SpecialType = SpecialComponentType.PrefabRef;
            if (type.IsEnableable)
            {
                SpecialType |= SpecialComponentType.Enableable;
            }
        }

        public override object UpdateBindingsInternal(Entity entity) {
            PrefabRef prefabRef;
            PrefabData data = new PrefabData() {m_Index = -1};
            if (IsSnapshot)
            {
                prefabRef = SnapshotService.Instance.TryGetSnapshot(entity, out SnapshotService.EntitySnapshotData snapshot) && snapshot.TryGetData(Type, out object val) && val != null
                    ? (PrefabRef)val
                    : new PrefabRef();
            }
            else
            {
                EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                if ((SpecialType & SpecialComponentType.Enableable) != 0)
                {
                    IsDisabled = !entityManager.IsComponentEnabled(entity, Type);
                }
                prefabRef = entityManager.GetComponentData<PrefabRef>(entity);
                if (entityManager.Exists(prefabRef) && entityManager.TryGetComponent(prefabRef.m_Prefab, out PrefabData prefabData))
                {
                    data = prefabData;
                }
            }
            PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
            if (prefabSystem.TryGetPrefab(prefabRef, out PrefabBase prefab))
            {
                PrefabRefDataName = prefab.name;
            }
            else if (data.m_Index < 0)
            {
                MissingPrefab = true;
                SpecialType |= SpecialComponentType.Invalid;
                PrefabRefDataName = $"[Missing] {prefabSystem.GetPrefabName(prefabRef.m_Prefab)}";
            }
            return prefabRef;
        }

        public override bool RefreshValuesInternal(Entity entity, object previousData) {
            visitedObjects.Clear();
            foreach (IInspectableObject o in Objects)
            {
                o.UpdateValue(_componentData, false, visitedObjects);
            }
            return true;
        }
    }

    public sealed class PrefabDataComponentInfo : ComponentInfoBase, IEntityComponent
    {
        public string PrefabDataName { get; private set; }
        public bool MissingPrefab { get; private set; }

        public PrefabDataComponentInfo(ComponentType type, string name, List<FieldInfo> fields, bool isSnapshot) : base(type, name, fields, isSnapshot) {
            Logging.DebugEvaluation($"[Component-PrefabData] Type: {type.GetManagedType().FullName}, name: {name} | {string.Join(", ", fields.Select(f => $"{f.Name}{(f.IsPrivate ? "[P]" : "")}: {f.FieldType.Name}"))}");
            SpecialType = SpecialComponentType.PrefabData;
            if (type.IsEnableable)
            {
                SpecialType |= SpecialComponentType.Enableable;
            }
        }

        public override object UpdateBindingsInternal(Entity entity) {
            PrefabData data;
            if (IsSnapshot)
            {
                data = SnapshotService.Instance.TryGetSnapshot(entity, out SnapshotService.EntitySnapshotData snapshot) && snapshot.TryGetData(Type, out object val) && val != null
                    ? (PrefabData)val
                    : new PrefabData() { m_Index = -1 };
            }
            else
            {
                if ((SpecialType & SpecialComponentType.Enableable) != 0)
                {
                    IsDisabled = !World.DefaultGameObjectInjectionWorld.EntityManager.IsComponentEnabled(entity, Type);
                }
                data = (PrefabData)Type.GetManagedType().GetComponentDataByType(World.DefaultGameObjectInjectionWorld.EntityManager, entity);
            }
            PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
            if (prefabSystem.TryGetPrefab(data, out PrefabBase prefab))
            {
                PrefabDataName = prefab.name;
            }
            else if (data.m_Index < 0)
            {
                if (Objects.Count > 0)
                {
                    Objects[0].CanInspectValue = false;
                }
                MissingPrefab = true;
                SpecialType |= SpecialComponentType.Invalid;
                PrefabDataName = $"[Missing] {prefabSystem.GetPrefabName(entity)}";
            }
            return data;
        }

        public override bool RefreshValuesInternal(Entity entity, object previousData) {
            visitedObjects.Clear();
            foreach (IInspectableObject o in Objects)
            {
                o.UpdateValue(_componentData, false, visitedObjects);
            }
            return true;
        }
    }

    public class EntityBufferComponentInfo : ComponentInfoBase, IEntityBufferComponent
    {
        public int CurrentPage { get; private set; } = 1;
        public int PageCount { get; private set; } = 1;
        public int ItemCount => DataArray.Count;
        public List<IInspectableObject> DataArray { get; private set; } = new List<IInspectableObject>();

        private List<object> _allItems = new List<object>();
        private int _previousPage = 1;
        private bool _initialized;
        private HashSet<object> _visitedEmpty = new HashSet<object>();

        public EntityBufferComponentInfo(ComponentType type, string name, List<FieldInfo> fields, bool isSnapshot) : base(type, name, fields, isSnapshot) {
            Logging.DebugEvaluation($"[Buffer] Type: {type.GetManagedType().FullName}, name: {name} | {string.Join(", ", fields.Select(f => $"{f.Name}{(f.IsPrivate ? "[P]" : "")}: {f.FieldType.Name}"))}");
            SpecialType = SpecialComponentType.Buffer;
            if (type.IsEnableable)
            {
                SpecialType |= SpecialComponentType.Enableable;
            }
        }

        public override object UpdateBindingsInternal(Entity entity) {
            if (IsSnapshot && SnapshotService.Instance.TryGetSnapshot(entity, out SnapshotService.EntitySnapshotData data) && data.TryGetData(Type, out object value))
            {
                return value;
            }
            if ((SpecialType & SpecialComponentType.Enableable) != 0)
            {
                IsDisabled = !World.DefaultGameObjectInjectionWorld.EntityManager.IsComponentEnabled(entity, Type);
            }
            return Type.GetManagedType().GetComponentBufferArrayByType(World.DefaultGameObjectInjectionWorld.EntityManager, entity);
        }

        public override bool RefreshValuesInternal(Entity entity, object newData) {
            if ((!DetailedView && !IsSnapshot) && _initialized)
            {
                return false;
            }
            List<object> newObjects = (List<object>)newData;

            bool requireUpdate = false;
            if (newObjects.Count != _allItems.Count)
            {
                Logging.DebugEvaluation($"Seq not equal ({newObjects.Count}): {string.Join(", ", newObjects.Select(o => o.ToString()))}");
                if (newObjects.Count > _allItems.Count)
                {
                    int diff = newObjects.Count - _allItems.Count;
                    Logging.DebugEvaluation($"[Add] New: ({newObjects.Count}), Old: {_allItems.Count} | diff: {diff}");
                    for (int i = 0; i < diff; i++)
                    {
                        DataArray.Add(new ComplexObject(Type.GetManagedType(), DataFields, IsSnapshot));
                    }
                }
                else
                {
                    int diff = _allItems.Count - newObjects.Count;
                    Logging.DebugEvaluation($"[Remove] New: ({newObjects.Count}), Old: {_allItems.Count} | diff: {diff}");
                    for (int i = newObjects.Count; i < _allItems.Count; i++)
                    {
                        DataArray[i].IsActive = false;
                        DataArray[i].Dispose();
                    }
                    DataArray.RemoveRange(newObjects.Count, diff);
                }
                _allItems = newObjects;
                ResetPage();
                requireUpdate = true;
            }
            else if (_allItems.Count > 0)
            {
                _allItems = newObjects;
                requireUpdate = true;
            }

            if (_previousPage != CurrentPage || requireUpdate)
            {
                Logging.DebugEvaluation($"Diff page: {_previousPage}, now: {CurrentPage} |reqUp: {requireUpdate}");
                _previousPage = CurrentPage;
                if (TryGetPaginationData(out int index, out int count))
                {
                    if (IsSnapshot)
                    {
                        index = 0;
                        count = DataArray.Count;
                    }
                    Logging.DebugEvaluation($"Updating values: {index} {count} | {ItemCount} | IsSnapshot: {IsSnapshot}");
                    for (; index < count; index++)
                    {
                        IInspectableObject inspectableObject = DataArray[index];
                        bool wasActive = inspectableObject.IsActive;
                        inspectableObject.IsActive = true;
                        inspectableObject.UpdateValue(_allItems[index], false, _visitedEmpty);
                        inspectableObject.IsActive = wasActive;
                    }
                }
                else
                {
                    Logging.DebugEvaluation($"Resetting page...");
                    foreach (IInspectableObject item in DataArray)
                    {
                        item.IsActive = false;
                    }
                    ResetPage();
                }
                _initialized = true;
            }

            return true;
        }


        public void PreviousPage() {
            if (CurrentPage - 1 > 0)
            {
                CurrentPage--;
            }
        }

        public void NextPage() {
            if (CurrentPage + 1 <= PageCount)
            {
                CurrentPage++;
            }
        }

        private void ResetPage() {
            CurrentPage = 1;
            PageCount = ItemCount > 10 ? (ItemCount / 10) + ((ItemCount % 10) > 0 ? 1 : 0) : 1;
        }

        private bool TryGetPaginationData(out int firstItemIndex, out int lastItemIndex) {
            firstItemIndex = (CurrentPage - 1) * 10;
            lastItemIndex = ItemCount - firstItemIndex > 10 ? firstItemIndex + 10 : ItemCount;
            return firstItemIndex < ItemCount;
        }

        public override void Dispose() {
            base.Dispose();
            _allItems.Clear();
            foreach (IInspectableObject item in DataArray)
            {
                item.IsActive = false;
                item.Dispose();
            }
            DataArray.Clear();
            ResetPage();
        }
    }

    public class EntityTagComponentInfo : ComponentInfoBase, IEntityTagComponent
    {
        public EntityTagComponentInfo(ComponentType type, string name, List<FieldInfo> fields, bool isSnapshot) : base(type, name, fields, isSnapshot)
        {
            SpecialType = SpecialComponentType.Tag;
            if (type.IsEnableable)
            {
                SpecialType |= SpecialComponentType.Enableable;
            }
        }

        public override object UpdateBindingsInternal(Entity entity)
        {
            if ((SpecialType & SpecialComponentType.Enableable) != 0)
            {
                IsDisabled = !World.DefaultGameObjectInjectionWorld.EntityManager.IsComponentEnabled(entity, Type);
            }
            return base.UpdateBindingsInternal(entity);
        }
    }

    public class EntityNotSupportedComponent : ComponentInfoBase, IEntityNotSupportedComponent
    {
        public EntityNotSupportedComponent(ComponentType type, string name, List<FieldInfo> fields, bool isSnapshot) : base(type, name, fields, isSnapshot) {
            SpecialType = SpecialComponentType.Unknown;
            if (type.IsEnableable)
            {
                SpecialType |= SpecialComponentType.Enableable;
            }
        }

        public override object UpdateBindingsInternal(Entity entity)
        {
            if ((SpecialType & SpecialComponentType.Enableable) != 0)
            {
                IsDisabled = !World.DefaultGameObjectInjectionWorld.EntityManager.IsComponentEnabled(entity, Type);
            }
            return base.UpdateBindingsInternal(entity);
        }
    }


    [Flags]
    public enum SpecialComponentType
    {
        None,
        PrefabRef,
        PrefabData,
        Buffer = 1 << 2,
        Tag = 1 << 3,
        UnManaged = 1 << 4,
        Managed = 1 << 5,
        Shared = 1 << 6,
        Unknown = 1 << 7,
        Invalid = 1 << 8,
        Enableable = 1 << 9,
    }
}
