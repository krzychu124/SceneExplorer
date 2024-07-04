using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Colossal.Annotations;
using Game.Net;
using Game.Prefabs;
using SceneExplorer.Services;
using Unity.Entities;
using SubObject = Game.Prefabs.SubObject;

namespace SceneExplorer.ToBeReplaced.Helpers
{

    public enum InspectMode
    {
        Linked,
        Standalone,
        Watcher,
        JumpTo,
    }
    public interface IValueInspector
    {
        IClosablePopup Inspect(object value, IInspectableObject o, InspectMode mode);
    }

    public interface IClosablePopup
    {
        void TryClose();
        void ForceClose();
        bool IsActive { get; }
    }

    public interface IInspectableObject
    {
        public IInspectableObject Parent { get; }
        public IInspectableObject[] Children { get; }
        public FieldInfo FieldInfo { get; }
        public IClosablePopup InspectorPopupRef { get; set; }
        public object GetValueCached();
        public void UpdateValue(object instance, bool resetState);
        public bool IsActive { get; set; }
        public bool IsSnapshot { get; }
        public bool CanInspectValue { get; set; }
        public void Dispose();
    }

    public abstract class Inspectable : IInspectableObject
    {
        public IInspectableObject[] Children { get; protected set; }
        public FieldInfo FieldInfo { get; protected set; }
        public IClosablePopup InspectorPopupRef { get; set; }
        public virtual bool IsActive { get; set; }
        public bool IsSnapshot { get; }
        public bool CanInspectValue { get; set; }
        public bool CanJumpTo { get; set; }
        public abstract IInspectableObject Parent { get; protected set; }

        protected Inspectable(FieldInfo fieldInfo, bool isSnapshot) {
            FieldInfo = fieldInfo;
            IsSnapshot = isSnapshot;
        }

        public virtual object GetValue(object instance) {
            return null;
        }

        public virtual object GetValueCached() {
            return null;
        }

        public virtual void UpdateValue(object instance, bool resetState) {
        }

        public abstract void Dispose();

        public virtual void CloseInspectorPopup(bool force = true) {
            if (InspectorPopupRef != null && InspectorPopupRef.IsActive)
            {
                if (force)
                {
                    InspectorPopupRef.ForceClose();
                }
                else
                {
                    InspectorPopupRef.TryClose();
                }
                InspectorPopupRef = null;
            }
        }
    }

    public sealed class InspectableEntity : Inspectable
    {
        public override IInspectableObject Parent { get; protected set; }
        private bool _initialized;
        private object _value = Entity.Null;
        private Entity _entity = Entity.Null;
        public string PrefabName = string.Empty;

        public InspectableEntity(FieldInfo fieldInfo, IInspectableObject parentObject, bool isSnapshot) : base(fieldInfo, isSnapshot) {
            Parent = parentObject;

            if (fieldInfo == null)
            {
                Logging.Debug($"NULL INFO! {parentObject?.FieldInfo?.Name}");
            }
            Logging.Debug($"New Inspectable Entity! {fieldInfo?.FieldType.Name} | Parent: {parentObject?.FieldInfo?.Name}");
        }

        public override object GetValueCached() {
            return _value;
        }

        public override object GetValue(object instance) {
            return _value ?? FieldInfo.GetValue(instance);
        }

        public override void UpdateValue(object instance, bool resetState) {
            if (instance == null)
            {
                Logging.Debug($"NULL instance! {GetType().Name}");
                // IsValid = false;
                return;
            }

            // Logging.Info($"Updating value! {GetType().Name} {FieldInfo?.Name}");
            object prevValue = _value;
            _value = FieldInfo.GetValue(instance);
            _entity = (Entity)_value;
            if (IsSnapshot && (_entity == Entity.Null || !World.DefaultGameObjectInjectionWorld.EntityManager.Exists(_entity)))
            {
                Logging.Debug($"value not available: {_entity} | {prevValue} | {_value} | {instance.GetType().Name} | init: {_initialized}");
                // IsValid = false;
                PrefabName = string.Empty;
                CanInspectValue = false;
                CanJumpTo = false;
                CloseInspectorPopup();
            }
            else
            {
                if (!_initialized)
                {
                    Logging.Debug($"Initialized! ({FieldInfo.Name})");
                    World world = World.DefaultGameObjectInjectionWorld;
                    PrefabName = _entity.TryGetPrefabName(world.EntityManager, world.GetExistingSystemManaged<PrefabSystem>(), out _);
                }
                _initialized = true;
                // IsValid = true;
                CanInspectValue = true;
                CanJumpTo = InspectObjectUtils.EvaluateCanJumpTo(World.DefaultGameObjectInjectionWorld.EntityManager, _entity);
            }
            if (!prevValue.Equals(_value))
            {
                CloseInspectorPopup(force: false);
            }
        }


        public override void Dispose() {
            // IsValid = false;
            IsActive = false;
            _value = null;
            Parent = null;
            if (Children != null)
            {
                foreach (IInspectableObject inspectableObject in Children)
                {
                    inspectableObject.Dispose();
                }

                Children = null;
            }
            CloseInspectorPopup();
        }
    }

    public abstract class IterableObject : Inspectable
    {
        private readonly Type _elementType;
        private readonly List<FieldInfo> _elementTypeFields;
        public override IInspectableObject Parent { get; protected set; }

        public int PageCount { get; private set; } = 1;
        public int CurrentPage { get; private set; } = 1;
        public int ItemsPerPage => 10;
        private bool _initialized;
        private object _value;

        // public int CurrentPage { get; private set; } = 1;
        // public int PageCount { get; private set; } = 1;
        public int ItemCount => DataArray.Count;
        public List<IInspectableObject> DataArray { get; private set; } = new List<IInspectableObject>();
        protected List<object> _allItems = new List<object>();

        private int _previousPage = 1;
        // private bool _initialized;

        public IterableObject(FieldInfo fieldInfo, Type elementType, List<FieldInfo> elementTypeFields, IInspectableObject parent, bool isSnapshot) : base(fieldInfo, isSnapshot) {
            _elementType = elementType;
            _elementTypeFields = elementTypeFields;
            Parent = parent;
            Logging.Debug($"Created iterable object def: {fieldInfo.Name} ({fieldInfo.FieldType.Name})[{fieldInfo.FieldType.GetElementType()?.FullName}]");
        }

        public override object GetValueCached() {
            return _value;
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

        public override void UpdateValue(object instance, bool resetState) {
            if (instance == null)
            {
                Logging.Debug($"NULL instance! {GetType().Name}");
                return;
            }

            object prevValue = _value;
            _value = FieldInfo.GetValue(instance);

            if (_value == null)
            {
                Logging.Debug($"value not available: {prevValue} | {_value} | {instance.GetType().Name}");
                CloseInspectorPopup();
                return;
            }

            if (!IsActive && _initialized)
            {
                Logging.Debug($"[Iterable] Inactive and initialized. Page: {_previousPage}, now: {CurrentPage} | {FieldInfo.Name}");
                return;
            }

            bool requireUpdate = false;
            if (HasElementCountChanged(out int previousCount, out int newCount))
            {
                Logging.Debug($"[Iterable] Seq not equal ({newCount})");
                if (newCount > previousCount)
                {
                    int diff = newCount - previousCount;
                    Logging.Debug($"[Iterable] [Add] New: ({newCount}), Old: {previousCount} | diff: {diff}");
                    for (int i = 0; i < diff; i++)
                    {
                        DataArray.Add(_elementType.IsEnum ? new CommonInspectableObject(_elementType, ((info, o) => o.ToString()), IsSnapshot) : new ComplexObject(_elementType, _elementTypeFields, IsSnapshot));
                    }
                }
                else
                {
                    int diff = previousCount - newCount;
                    Logging.Debug($"[Iterable] [Remove] New: ({newCount}), Old: {previousCount} | diff: {diff}");
                    for (int i = newCount; i < previousCount; i++)
                    {
                        DataArray[i].IsActive = false;
                        DataArray[i].Dispose();
                    }
                    DataArray.RemoveRange(newCount, diff);
                }
                _allItems = CacheItems();
                ResetPage();
                requireUpdate = true;
            }
            else if (_allItems.Count > 0)
            {
                requireUpdate = true;
            }

            if (_previousPage != CurrentPage || requireUpdate)
            {
                Logging.Debug($"[Iterable] Diff page: {_previousPage}, now: {CurrentPage} |reqUp: {requireUpdate} | {FieldInfo.Name}");
                _previousPage = CurrentPage;
                if (TryGetPaginationData(out int index, out int count))
                {
                    Logging.Debug($"[Iterable] Updating values: {index} {count} | {ItemCount}");
                    for (; index < count; index++)
                    {
                        IInspectableObject inspectableObject = DataArray[index];
                        bool wasActive = inspectableObject.IsActive;
                        inspectableObject.IsActive = true;
                        inspectableObject.UpdateValue(_allItems[index], false);
                        inspectableObject.IsActive = wasActive;
                    }
                }
                else
                {
                    Logging.Debug($"Resetting page...");
                    foreach (IInspectableObject item in DataArray)
                    {
                        item.IsActive = false;
                    }
                    ResetPage();
                }
                _initialized = true;
            }
        }

        protected abstract List<object> CacheItems();

        protected abstract bool HasElementCountChanged(out int previousCount, out int newCount);

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
            CloseInspectorPopup();
        }
    }

    public class PrefabComponentsIterableObject : Inspectable
    {
        public int PageCount { get; private set; } = 1;
        public int CurrentPage { get; private set; } = 1;
        public int ItemCount => DataArray.Count;
        public List<IInspectableObject> DataArray { get; private set; } = new List<IInspectableObject>();
        protected List<ComponentBase> _allItems = new List<ComponentBase>();
        private bool _initialized;
        private int _previousPage;

        public PrefabComponentsIterableObject(FieldInfo fieldInfo, IInspectableObject parent, bool isSnapshot) : base(fieldInfo, isSnapshot) {
            Parent = parent;
        }

        public override IInspectableObject Parent { get; protected set; }

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

        public override void UpdateValue(object instance, bool resetState) {
            if (instance == null)
            {
                Logging.Debug($"NULL instance! {GetType().Name} {FieldInfo?.Name}");
                return;
            }
            if (!IsActive && _initialized)
            {
                return;
            }

            var value = FieldInfo.GetValue(instance);

            if (value == null)
            {
                Logging.Debug($"value not available | {instance.GetType().Name}");
                return;
            }

            bool requireUpdate = true;

            if (!_initialized)
            {
                _allItems = (List<ComponentBase>)FieldInfo.GetValue(instance);
                for (int i = 0; i < _allItems.Count; i++)
                {
                    Type t = _allItems[i].GetType();
                    var fields = TypeDescriptorService.Instance.GetFields(t);
                    DataArray.Add(new ComplexObject(t, fields, IsSnapshot));
                }
                ResetPage();
                _initialized = true;
            }

            Logging.Debug($"Diff page: {_previousPage}, now: {CurrentPage} |reqUp: {requireUpdate}");
            _previousPage = CurrentPage;
            if (TryGetPaginationData(out int index, out int count))
            {
                Logging.Debug($"Updating values: {index} {count} | {ItemCount}");
                for (; index < count; index++)
                {
                    IInspectableObject inspectableObject = DataArray[index];
                    bool wasActive = inspectableObject.IsActive;
                    inspectableObject.IsActive = true;
                    inspectableObject.UpdateValue(_allItems[index], false);
                    inspectableObject.IsActive = wasActive;
                }
            }
            else
            {
                Logging.Debug($"Resetting page...");
                foreach (IInspectableObject item in DataArray)
                {
                    item.IsActive = false;
                }
                ResetPage();
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

        }
    }

    public class GenericListObject : Inspectable
    {
        public int PageCount { get; private set; } = 1;
        public int CurrentPage { get; private set; } = 1;
        public int ItemCount => DataArray.Count;
        public List<IInspectableObject> DataArray { get; private set; } = new List<IInspectableObject>();
        protected IList _allItems = new List<object>();
        private bool _initialized;
        private int _previousPage;

        public GenericListObject(FieldInfo fieldInfo, IInspectableObject parent, bool isSnapshot) : base(fieldInfo, isSnapshot) {
            Parent = parent;
        }

        public override IInspectableObject Parent { get; protected set; }

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

        public override void UpdateValue(object instance, bool resetState) {
            if (instance == null)
            {
                Logging.Debug($"NULL instance! {GetType().Name} {FieldInfo?.Name}");
                return;
            }
            if (!IsActive && _initialized)
            {
                return;
            }

            var value = FieldInfo.GetValue(instance);

            if (value == null)
            {
                Logging.Debug($"value not available | {instance.GetType().Name}");
                return;
            }

            bool requireUpdate = false;

            if (!_initialized)
            {
                _allItems = (IList)FieldInfo.GetValue(instance);
                for (int i = 0; i < _allItems.Count; i++)
                {
                    Type t = _allItems[i].GetType();
                    var fields = TypeDescriptorService.Instance.GetFields(t);
                    DataArray.Add(t.IsEnum ? new CommonInspectableObject(t, ((info, o) => o.ToString()), IsSnapshot) : new ComplexObject(t, fields, IsSnapshot));
                }
                ResetPage();
                requireUpdate = true;
                _initialized = true;
            }

            if (_previousPage != CurrentPage || requireUpdate)
            {
                Logging.Debug($"Diff page: {_previousPage}, now: {CurrentPage} |reqUp: {requireUpdate}");
                _previousPage = CurrentPage;
                if (TryGetPaginationData(out int index, out int count))
                {
                    Logging.Debug($"Updating values: {index} {count} | {ItemCount}");
                    for (; index < count; index++)
                    {
                        IInspectableObject inspectableObject = DataArray[index];
                        bool wasActive = inspectableObject.IsActive;
                        inspectableObject.IsActive = true;
                        inspectableObject.UpdateValue(_allItems[index], false);
                        inspectableObject.IsActive = wasActive;
                    }
                }
                else
                {
                    Logging.Debug($"Resetting page...");
                    foreach (IInspectableObject item in DataArray)
                    {
                        item.IsActive = false;
                    }
                    ResetPage();
                }
                _initialized = true;
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

        }
    }

    public class ArrayIterableObject : IterableObject
    {
        public ArrayIterableObject(FieldInfo fieldInfo, Type elementType, List<FieldInfo> elementTypeFields, IInspectableObject parent, bool isSnapshot) : base(fieldInfo, elementType, elementTypeFields, parent, isSnapshot) {
        }

        protected override List<object> CacheItems() {
            return ((Array)GetValueCached()).Cast<object>().ToList<object>();
        }

        protected override bool HasElementCountChanged(out int previousCount, out int newCount) {
            previousCount = _allItems.Count;
            newCount = ((Array)GetValueCached()).Length;
            return previousCount != newCount;
        }
    }

    public class CommonInspectableObject : IInspectableObject
    {
        private readonly FieldInfo _fieldInfo;
        private readonly Type _valueType;
        private readonly Func<FieldInfo, object, object> _getValueFn;
        private object _value;
        public bool IsSnapshot { get; }
        public bool CanInspectValue { get; set; }
        public FieldInfo FieldInfo => _fieldInfo;
        public IClosablePopup InspectorPopupRef { get; set; }
        public IInspectableObject Parent { get; private set; }
        public IInspectableObject[] Children { get; private set; }

        public CommonInspectableObject(FieldInfo fieldInfo, Func<FieldInfo, object, object> getValueFn, bool isSnapshot) {
            _fieldInfo = fieldInfo;
            _valueType = _fieldInfo.FieldType;
            _getValueFn = getValueFn;
            IsSnapshot = isSnapshot;
            Logging.Debug($"Common object: {fieldInfo.FieldType.Name}, isEnum?: {fieldInfo.FieldType.IsEnum}");
        }

        public CommonInspectableObject(Type valueType, Func<FieldInfo, object, object> getValueFn, bool isSnapshot) {
            _fieldInfo = null;
            _valueType = valueType;
            _getValueFn = getValueFn;
            IsSnapshot = isSnapshot;
            Logging.Debug($"Common object: {valueType.Name}");
        }

        public object GetValueCached() {
            return _value;
        }

        public object GetValue(object instance) {
            return _value;
        }

        public void UpdateValue(object instance, bool resetState) {
            if (instance == null)
            {
                Logging.Debug($"NULL instance! {GetType().Name} {_fieldInfo?.Name}");
                return;
            }
            Logging.Debug($"Updating value! {GetType().Name} {_fieldInfo?.Name}");
            if (Children != null)
            {
                if (IsActive || IsSnapshot)
                {
                    for (var i = 0; i < Children.Length; i++)
                    {
                        Children[i].UpdateValue(instance, resetState);
                    }
                }
            }
            else
            {
                _value = _getValueFn(_fieldInfo, instance);
            }
        }

        public bool IsActive { get; set; }
        public bool IsValid { get; } = true;

        public Type ValueType => _valueType;

        public void Dispose() {
            IsActive = false;
            _value = null;
            Parent = null;
            if (Children != null)
            {
                foreach (IInspectableObject inspectableObject in Children)
                {
                    inspectableObject.Dispose();
                }

                Children = null;
            }
        }
    }

    public class ComplexObject : IInspectableObject
    {
        private readonly FieldInfo _fieldInfo;
        private readonly Type _rootType;
        private object _value;
        private bool _isActive;
        private bool _isDirty;
        public bool IsSnapshot { get; }
        public bool CanInspectValue { get; set; }
        public FieldInfo FieldInfo => _fieldInfo;
        public Type RootType => _rootType;
        public IInspectableObject Parent { get; private set; }
        public IInspectableObject[] Children { get; private set; }
        public IClosablePopup InspectorPopupRef { get; set; }

        public string PrefabName = string.Empty;

        private List<FieldInfo> _childrenFields;
        private PrefabInfoData _prefabInfoData;

        public ComplexObject([CanBeNull] FieldInfo fieldInfo, List<FieldInfo> childrenFields, bool isSnapshot) {
            _fieldInfo = fieldInfo;
            _rootType = fieldInfo.FieldType;
            _childrenFields = childrenFields;
            _isDirty = isSnapshot;
            IsSnapshot = isSnapshot;
            Logging.Debug($"New Complex object; {fieldInfo?.Name}({fieldInfo?.FieldType.FullName}) ({_rootType.FullName}) | {string.Join(", ", childrenFields.Select(f => f.Name))}");
        }

        public ComplexObject(Type rootType, List<FieldInfo> childrenFields, bool isSnapshot) {
            _fieldInfo = null;
            _rootType = rootType;
            _childrenFields = childrenFields;
            _isDirty = isSnapshot;
            IsSnapshot = isSnapshot;
            Logging.Debug($"New Complex object; ({rootType.FullName}) | {string.Join(", ", childrenFields.Select(f => f.Name))}");
        }

        public object GetValueCached() {
            return _value;
        }

        public object GetValue(object instance) {
            return _value;
        }

        public void UpdateValue(object instance, bool resetState) {
            if (instance == null)
            {
                Logging.Debug($"NULL instance! {GetType().Name} {_fieldInfo?.Name}");
                return;
            }

            if ((IsActive || IsSnapshot) && Children == null && _isDirty && _childrenFields.Count > 0)
            {
                _isDirty = false;
                Children = _childrenFields.Select(field => UIGenerator.GetInspectableObjectData(field, this, IsSnapshot)).ToArray();
                Logging.Debug($"Updating Children: {Children.Length} {_fieldInfo?.Name}({_fieldInfo?.FieldType.FullName}) | root: {_rootType.FullName}");
                _prefabInfoData = GetPrefabInfoRef(_rootType, Children);
            }

            if (_fieldInfo != null)
            {
                _value = _fieldInfo.GetValue(instance);
            }
            else
            {
                _value = instance;
            }
            if (Children != null && (IsActive || IsSnapshot))
            {
                for (int i = 0; i < Children.Length; i++)
                {
                    Children[i].UpdateValue(_value, resetState);
                }
                if (string.IsNullOrEmpty(PrefabName))
                {
                    if (_prefabInfoData != default)
                    {
                        PrefabName = GetPrefabName(Children[_prefabInfoData.fieldIndex].GetValueCached(), _prefabInfoData.fieldName, _prefabInfoData.isPrefabRef, _rootType);
                        Logging.Debug($"Calculated prefab name: {PrefabName} | {_prefabInfoData.fieldIndex}, {_prefabInfoData.fieldName}");
                    } 
                    else if (instance.GetType() == typeof(PrefabBase) && _value is PrefabBase pb)
                    {
                        PrefabName = pb.name;
                    }
                }
            }
        }

        private static string GetPrefabName(object value, string fieldName, bool isPrefabRef, Type rootType) {
            if (value != null)
            {
                if (value is PrefabBase pb)
                {
                    Logging.Debug($"Field: {fieldName} in {rootType.FullName} is PrefabBase. Returning PrefabBase name");
                    return pb.name;
                }

                Entity e = (Entity)value;
                var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();

                return e.TryGetPrefabName(entityManager, prefabSystem, out _);
            }

            return null;
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (value != _isActive)
                {
                    _isDirty = true;
                    _isActive = value;
                }
            }
        }

        public bool IsValid { get; } = true;

        public void Dispose() {
            IsActive = false;
            Children = null;
            IInspectableObject[] temp = Children;
            Parent = null;
            _value = null;
            _childrenFields = null;
            if (temp != null)
            {
                foreach (IInspectableObject inspectableObject in temp)
                {
                    inspectableObject.Dispose();
                }
            }
        }

        private static PrefabInfoData GetPrefabInfoRef(Type rootType, IInspectableObject[] objects) {
            PrefabInfoData prefabInfoData = default;
            if (rootType == typeof(NetCompositionPiece) || rootType == typeof(NetSectionPiece) || rootType == typeof(NetPieceInfo))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(NetSectionPiece.m_Piece)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(NetSectionPiece.m_Piece), false);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(NetPieceLane) || rootType == typeof(NetCompositionCrosswalk) || rootType == typeof(NetCompositionLane) || rootType == typeof(Game.Prefabs.SecondaryNetLane) || rootType == typeof(NetLaneInfo))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(NetPieceLane.m_Lane)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(NetPieceLane.m_Lane), false);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(NetGeometryComposition))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(NetGeometryComposition.m_Composition)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(NetGeometryComposition.m_Composition), true);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(NetGeometrySection) || rootType == typeof(NetSectionInfo) || rootType == typeof(NetSubSectionInfo))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(NetGeometrySection.m_Section)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(NetGeometrySection.m_Section), false);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(Game.Prefabs.SubObject) || rootType == typeof(Game.Prefabs.SubLane) || rootType == typeof(NetCompositionObject) || rootType == typeof(NetPieceObject))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(SubObject.m_Prefab)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(SubObject.m_Prefab), false);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(ObjectMeshInfo) || rootType == typeof(NetLaneMeshInfo))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(ObjectMeshInfo.m_Mesh)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(ObjectMeshInfo.m_Mesh), false);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(ObjectSubAreaInfo))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(ObjectSubAreaInfo.m_AreaPrefab)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(ObjectSubAreaInfo.m_AreaPrefab), false);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(ObjectSubLaneInfo))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(ObjectSubLaneInfo.m_LanePrefab)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(ObjectSubLaneInfo.m_LanePrefab), false);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(ObjectSubNetInfo))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(ObjectSubNetInfo.m_NetPrefab)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(ObjectSubNetInfo.m_NetPrefab), false);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(MultipleUnitTrainCarriageInfo))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(MultipleUnitTrainCarriageInfo.m_Carriage)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(MultipleUnitTrainCarriageInfo.m_Carriage), false);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(SlaveAreaInfo))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(SlaveAreaInfo.m_Area)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(SlaveAreaInfo.m_Area), false);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(SecondaryLaneInfo) || rootType == typeof(SecondaryLaneInfo2) || rootType == typeof(AuxiliaryLaneInfo))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(SecondaryLaneInfo.m_Lane)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(SecondaryLaneInfo.m_Lane), false);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(Game.Prefabs.SubMesh))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(Game.Prefabs.SubMesh.m_SubMesh)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(Game.Prefabs.SubMesh.m_SubMesh), false);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(Game.Prefabs.PlaceholderObjectElement) || rootType == typeof(NetSubObjectInfo) || rootType == typeof(ObjectSubObjectInfo) || rootType == typeof(AreaSubObjectInfo) || rootType == typeof(NetPieceObjectInfo))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(Game.Prefabs.PlaceholderObjectElement.m_Object)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(Game.Prefabs.PlaceholderObjectElement.m_Object), false);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(Game.Prefabs.ObjectRequirementElement))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(Game.Prefabs.ObjectRequirementElement.m_Requirement)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(Game.Prefabs.ObjectRequirementElement.m_Requirement), false);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(Game.Prefabs.BuildingUpgradeElement))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(Game.Prefabs.BuildingUpgradeElement.m_Upgrade)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(Game.Prefabs.BuildingUpgradeElement.m_Upgrade), false);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(Game.Prefabs.ServiceUpgradeBuilding))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(Game.Prefabs.ServiceUpgradeBuilding.m_Building)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(Game.Prefabs.ServiceUpgradeBuilding.m_Building), false);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(Game.Prefabs.Effect))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(Game.Prefabs.Effect.m_Effect)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(Game.Prefabs.Effect.m_Effect), false);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(Game.Net.SubLane))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(Game.Net.SubLane.m_SubLane)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(Game.Net.SubLane.m_SubLane), true);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(Game.Net.SubNet))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(Game.Net.SubNet.m_SubNet)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(Game.Net.SubNet.m_SubNet), true);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(Game.Objects.SubObject))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(Game.Objects.SubObject.m_SubObject)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(Game.Objects.SubObject.m_SubObject), true);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(Game.Net.AggregateElement) || rootType == typeof(ConnectedEdge))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(AggregateElement.m_Edge)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(AggregateElement.m_Edge), true);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            else if (rootType == typeof(LaneOverlap))
            {
                int index = Array.FindIndex(objects, o => o.FieldInfo?.Name.Equals(nameof(LaneOverlap.m_Other)) ?? false);
                prefabInfoData = new PrefabInfoData(index, nameof(LaneOverlap.m_Other), true);
                Logging.Debug($"Found entity: {prefabInfoData.fieldIndex}, {prefabInfoData.fieldName}");
            }
            return prefabInfoData;
        }

        public struct PrefabInfoData : IEquatable<PrefabInfoData>
        {
            public int fieldIndex;
            public string fieldName;
            public bool isPrefabRef;

            // public PrefabInfoData() {
            //     fieldIndex = -1;
            //     fieldName = null;
            //     isPrefabRef = false;
            // }

            public PrefabInfoData(int index, string name, bool prefabRef) {
                fieldIndex = index;
                fieldName = name;
                isPrefabRef = prefabRef;
            }

            public bool Equals(PrefabInfoData other) {
                return fieldIndex == other.fieldIndex && fieldName == other.fieldName;
            }

            public override bool Equals(object obj) {
                return obj is PrefabInfoData other && Equals(other);
            }

            public override int GetHashCode() {
                unchecked
                {
                    return (fieldIndex * 397) ^ (fieldName != null ? fieldName.GetHashCode() : 0);
                }
            }

            public static bool operator ==(PrefabInfoData left, PrefabInfoData right) {
                return left.Equals(right);
            }

            public static bool operator !=(PrefabInfoData left, PrefabInfoData right) {
                return !left.Equals(right);
            }
        }
    }
}