#if DEBUG_PP
using System.Collections.Generic;
using System.Linq;
using Colossal.Annotations;
using Colossal.Entities;
using Game.Common;
using Game.Prefabs;
using Game.Reflection;
using Game.Tools;
using Game.UI.Editor;
using Game.UI.Localization;
using Game.UI.Widgets;
using SceneExplorer.ToBeReplaced.Helpers;
using SceneExplorer.ToBeReplaced.Windows;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using EditorContainer = Game.Tools.EditorContainer;
using SubLane = Game.Net.SubLane;

namespace SceneExplorer.System
{
    public partial class InspectorToolPanelSystem : EditorPanelSystemBase
    {
        private Entity _selectedEntity;
        private bool _prefabChanged;
        private EditorGenerator _generator;
        private PrefabBase _currentPrefab;
        private PrefabSystem _prefabSystem;
        private InspectObjectToolSystem _inspectToolSystem;
        private object _parentObject;

        private string _selectedName;
        private string _parentName;
        private string _text;

        protected override void OnCreate() {
        base.OnCreate();

        _inspectToolSystem = World.GetOrCreateSystemManaged<InspectObjectToolSystem>();
        _generator = new EditorGenerator();
        _prefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();

        title = "Select object";
        children = new List<IWidget>
        {
            GetToolControlsGroup()
        };

        _inspectToolSystem.RegisterOnUIReadyAction(() => {
            ObjectInfo.DataBindings b = ObjectInfo.BindingsBuilder.Create().SetTextGetter(() => _text).Build();
            b.UpdateBindings();
            _inspectToolSystem.UIManager.SetBindings(b);
        });
    }

        protected override void OnUpdate() {
            base.OnUpdate();

            if (!_prefabChanged)
            {
                return;
            }

            _prefabChanged = false;

            var list = new List<EditorSection>
            {
                GetToolControlsGroup()
            };
            if (!_currentPrefab)
            {
                title = "Select object";
                _text = "No object selected";
            }
            else
            {
                // title = _currentPrefab.name;
                List<ComponentBase> components = new List<ComponentBase>
                {
                    _currentPrefab,
                };

                if (_parentObject is PrefabBase parentPrefab && !parentPrefab.Equals(_currentPrefab))
                {
                    _text = $"Parent {parentPrefab.name}\n{string.Join(", ", parentPrefab.components)}";
                }
                else
                {
                    _text = "Root prefab";
                }
#if DEBUG2
                ObjectInfo.UIBuilder builder = new ObjectInfo.UIBuilder();
#endif
                components.AddRange(_currentPrefab.components);
                HashSet<ComponentType> tempComponentTypes = new HashSet<ComponentType>();
                for (var i = 0; i < components.Count; i++)
                {
                    object obj = components[i];

                    string name = obj.GetType().Name;
                    IWidget[] tempChildren = _generator.BuildMembers(new ObjectAccessor<object>(obj, true), 0, name).ToArray<IWidget>();
                    EditorSection editorSection = new EditorSection
                    {
                        path = new PathSegment(i),
                        displayName = LocalizedString.Value(WidgetReflectionUtils.NicifyVariableName(obj.GetType().Name)),
                        expanded = true,
                        children = tempChildren
                    };

                    PrefabBase prefabBase = obj as PrefabBase;
                    if (prefabBase != null)
                    {
                        editorSection.primary = true;
                        editorSection.color = new Color?(EditorSection.kPrefabColor);
                        editorSection.active = GetActiveAccessor(prefabBase);
#if DEBUG_TEST
                    builder.AddSection(prefabBase.name, new ValueBinding<string>(() => { return string.Join(", ", prefabBase.components); }));
                    for (var i1 = 0; i1 < prefabBase.components.Count; i1++)
                    {
                        ComponentBase prefabBaseComponent = prefabBase.components[i1];
                        tempComponentTypes.Clear();
                        prefabBaseComponent.GetArchetypeComponents(tempComponentTypes);
                        var temp = tempComponentTypes.ToArray();
                        builder.AddSection(prefabBaseComponent.name, new ValueBinding<string>(() => {
                            return $"Is active: {prefabBaseComponent.active},\n" +
                                $"IgnoreUnlockDependencies: {prefabBaseComponent.ignoreUnlockDependencies},\n" +
                                $"Prefab: {prefabBaseComponent.prefab.name}\n" +
                                $"Archetype components ({temp.Length}):\n{string.Join("\n", temp.Select(t => $"{t.GetManagedType().FullName}, isComponent: {t.IsComponent} isBuffer: {t.IsBuffer}"))}";
                        }));
                    }
#endif
                    }
                    else
                    {
                        ComponentBase component = obj as ComponentBase;
                        if (component != null)
                        {
                            editorSection.onDelete = delegate { ApplyPrefabsSystem.RemoveComponent(component.prefab, component.GetType()); };
                            editorSection.active = GetActiveAccessor(component);
#if DEBUG_TEST
                        tempComponentTypes.Clear();
                        component.GetArchetypeComponents(tempComponentTypes);
                        var temp = tempComponentTypes.ToArray();
                        builder.AddSection($"Editor Section: {component.name}",
                            new ValueBinding<string>(() => {
                                return
                                    $"Is active: {component.active}\n" +
                                    $"Archetype components ({temp.Length}):{string.Join("\n", temp.Select(t => $"{t.GetManagedType().Name}, isComponent: {t.IsComponent} isBuffer: {t.IsBuffer}"))}";
                            }));
#endif
                        }
                        else
                        {
                            // builder.AddSection($"Unknown type: {obj.GetType().FullName}", new ValueBinding<string>(() => { return $"No data!"; }));
                        }
                    }
                    list.Add(editorSection);
                }
                // RefreshBindings();

                // _inspectToolSystem.UIManager.UpdateContent(_currentPrefab.name, builder.Build());
            }
            children = new[] { Scrollable.WithChildren(list.ToArray()) };
        }

#if DEBUG_TEST
    private void RefreshBindings() {
        if (_selectedEntity == Entity.Null)
        {
            return;
        }
        ObjectInfo.UIBuilder builder = new ObjectInfo.UIBuilder();
        
        _inspectToolSystem.UIManager.InspectEntity(_selectedEntity);

        int componentCount = EntityManager.GetComponentCount(_selectedEntity);
        NativeArray<ComponentType> componentTypes = EntityManager.GetComponentTypes(_selectedEntity);
        ComponentType[] types = componentTypes.ToArray();
        Logging.Info($"Building for {_selectedEntity} ({componentCount}): [{string.Join(", ", types.Select(t => t.GetManagedType().FullName))}]");
        foreach (ComponentType componentType in types)
        {
            SectionGenerator.AddSection(componentType, builder, EntityManager, _selectedEntity, _prefabSystem);
        }
        componentTypes.Dispose();
        _inspectToolSystem.UIManager.UpdateContent($"{_currentPrefab.name} ({componentCount})", builder.Build());
    }
#endif

        protected override void OnValueChanged(IWidget widget) {
        base.OnValueChanged(widget);
        // this.UpdateParent(true);
    }

        private void UpdateParent(bool moveSubObjects) {
        PrefabBase prefabBase = this._parentObject as PrefabBase;
        if (prefabBase != null)
        {
            if (moveSubObjects)
            {
                this.MoveSubObjects(prefabBase);
            }
            _prefabSystem.UpdatePrefab(prefabBase, default(Entity));
        }
    }

        private void RefreshTitle() {
        string objectName = this.GetObjectName(this._currentPrefab);
        string objectName2 = this.GetObjectName(this._parentObject);
        Logging.Info($"Title: [{objectName2}] => [{objectName}] | {_parentObject} -> {_currentPrefab}");
        if (objectName != this._selectedName || objectName2 != this._parentName)
        {
            this._selectedName = objectName;
            this._parentName = objectName2;
            if (!this._parentObject.Equals(this._currentPrefab))
            {
                this.title = objectName2 + " > " + objectName;
                return;
            }
            this.title = objectName;
        }
    }

        [CanBeNull]
        private string GetObjectName([CanBeNull] object obj) {
        if (obj == null)
        {
            return null;
        }
        PrefabBase prefabBase = obj as PrefabBase;
        if (prefabBase != null)
        {
            return prefabBase.name;
        }
        return this._currentPrefab.GetType().Name;
    }

        private void MoveSubObjects(PrefabBase prefab) {
        Entity entity = _prefabSystem.GetEntity(prefab);
        DynamicBuffer<SubMesh> dynamicBuffer;
        if (base.EntityManager.TryGetBuffer(entity, false, out dynamicBuffer))
        {
            ObjectGeometryPrefab objectGeometryPrefab = prefab as ObjectGeometryPrefab;
            if (objectGeometryPrefab != null && objectGeometryPrefab.m_Meshes != null)
            {
                int num = math.min(dynamicBuffer.Length, objectGeometryPrefab.m_Meshes.Length);
                for (int i = 0; i < num; i++)
                {
                    SubMesh subMesh = dynamicBuffer[i];
                    ObjectMeshInfo objectMeshInfo = objectGeometryPrefab.m_Meshes[i];
                    if (!(subMesh.m_SubMesh != _prefabSystem.GetEntity(objectMeshInfo.m_Mesh)) && (!subMesh.m_Position.Equals(objectMeshInfo.m_Position) || !subMesh.m_Rotation.Equals(objectMeshInfo.m_Rotation)))
                    {
                        if (subMesh.m_Rotation.Equals(objectMeshInfo.m_Rotation))
                        {
                            float3 rhs = objectMeshInfo.m_Position - subMesh.m_Position;
                            ObjectSubObjects objectSubObjects;
                            if (prefab.TryGet<ObjectSubObjects>(out objectSubObjects) && objectSubObjects.m_SubObjects != null)
                            {
                                for (int j = 0; j < objectSubObjects.m_SubObjects.Length; j++)
                                {
                                    ObjectSubObjectInfo objectSubObjectInfo = objectSubObjects.m_SubObjects[j];
                                    if (objectSubObjectInfo.m_ParentMesh % 1000 == i)
                                    {
                                        objectSubObjectInfo.m_Position += rhs;
                                    }
                                }
                            }
                        }
                        else
                        {
                            float4x4 m = float4x4.TRS(subMesh.m_Position, subMesh.m_Rotation, 1f);
                            float4x4 a = math.mul(float4x4.TRS(objectMeshInfo.m_Position, objectMeshInfo.m_Rotation, 1f), math.inverse(m));
                            quaternion a2 = math.mul(objectMeshInfo.m_Rotation, math.inverse(subMesh.m_Rotation));
                            ObjectSubObjects objectSubObjects2;
                            if (prefab.TryGet<ObjectSubObjects>(out objectSubObjects2) && objectSubObjects2.m_SubObjects != null)
                            {
                                for (int k = 0; k < objectSubObjects2.m_SubObjects.Length; k++)
                                {
                                    ObjectSubObjectInfo objectSubObjectInfo2 = objectSubObjects2.m_SubObjects[k];
                                    if (objectSubObjectInfo2.m_ParentMesh % 1000 == i)
                                    {
                                        objectSubObjectInfo2.m_Position = math.transform(a, objectSubObjectInfo2.m_Position);
                                        objectSubObjectInfo2.m_Rotation = math.normalize(math.mul(a2, objectSubObjectInfo2.m_Rotation));
                                    }
                                }
                            }
                        }
                        subMesh.m_Position = objectMeshInfo.m_Position;
                        subMesh.m_Rotation = objectMeshInfo.m_Rotation;
                        dynamicBuffer[i] = subMesh;
                    }
                }
            }
        }
    }

        private bool SelectObjectForEntity(Entity entity) {
        PrefabRef refData;
        if (entity == Entity.Null || !base.EntityManager.TryGetComponent(entity, out refData))
        {
            this._parentObject = null;
            return false;
        }
        PrefabBase prefab = _prefabSystem.GetPrefab<PrefabBase>(refData);
        this._parentObject = prefab;
        Owner owner;
        PrefabRef refData2;
        EditorContainer editorContainer2;
        if (base.EntityManager.TryGetComponent(entity, out owner) && base.EntityManager.TryGetComponent(owner.m_Owner, out refData2))
        {
            Logging.Info($"Entity: {entity} has owner: {owner.m_Owner}");
            int num = -1;
            LocalTransformCache localTransformCache;
            if (base.EntityManager.TryGetComponent(entity, out localTransformCache))
            {
                num = localTransformCache.m_PrefabSubIndex;
                Logging.Info($"Entity {entity}, owner: {owner.m_Owner}, prefab subIndex: {num}");
            }
            if (num == -1)
            {
                Logging.Info($"Entity {entity}, owner: {owner.m_Owner}, incorrect prefab subIndex!");
                return false;
            }
            PrefabBase prefab2 = _prefabSystem.GetPrefab<PrefabBase>(refData2);
            EditorContainer editorContainer;
            ObjectSubObjects objectSubObjects;
            if (base.EntityManager.TryGetComponent(entity, out editorContainer))
            {
                Logging.Info($"Entity: {entity} has owner: {owner.m_Owner}, has editor container with prefab: {editorContainer.m_Prefab} group: {editorContainer.m_GroupIndex}");
                EffectSource effectSource;
                ActivityLocation activityLocation;
                if (base.EntityManager.HasComponent<EffectData>(editorContainer.m_Prefab) && prefab2.TryGet<EffectSource>(out effectSource) && effectSource.m_Effects != null && effectSource.m_Effects.Count > num)
                {
                    prefab = _prefabSystem.GetPrefab<PrefabBase>(editorContainer.m_Prefab);
                    EffectSource.EffectSettings effectSettings = effectSource.m_Effects[num];
                    if (effectSettings != null && effectSettings.m_Effect == prefab)
                    {
                        this._parentObject = prefab2;
                    }
                }
                else if (base.EntityManager.HasComponent<ActivityLocationData>(editorContainer.m_Prefab) && prefab2.TryGet<ActivityLocation>(out activityLocation) && activityLocation.m_Locations != null &&
                    activityLocation.m_Locations.Length > num)
                {
                    prefab = _prefabSystem.GetPrefab<PrefabBase>(editorContainer.m_Prefab);
                    ActivityLocation.LocationInfo locationInfo = activityLocation.m_Locations[num];
                    if (locationInfo != null && locationInfo.m_Activity == prefab)
                    {
                        this._parentObject = prefab2;
                    }
                }
            }
            else if (prefab2.TryGet<ObjectSubObjects>(out objectSubObjects) && objectSubObjects.m_SubObjects != null && objectSubObjects.m_SubObjects.Length > num)
            {
                Logging.Info($"Entity: {entity} has owner: {owner.m_Owner}, has Subobjects collection length: {objectSubObjects.m_SubObjects.Length}");
                ObjectSubObjectInfo objectSubObjectInfo = objectSubObjects.m_SubObjects[num];
                if (objectSubObjectInfo != null && objectSubObjectInfo.m_Object == prefab)
                {
                    Logging.Info($"Entity: {entity} has owner: {owner.m_Owner}, has Subobjects with entity");
                    this._parentObject = prefab2;
                }
            }
        }
        else if (base.EntityManager.TryGetComponent(entity, out editorContainer2) && _prefabSystem.TryGetPrefab<PrefabBase>(editorContainer2.m_Prefab, out prefab))
        {
            Logging.Info($"Entity: {entity} has editorContainer with prefab: {prefab.name}, group: {editorContainer2.m_GroupIndex}");
            this._parentObject = prefab;
        }
        return true;
    }

        public bool SelectEntity(Entity entity) {
        PrefabRef refData;
        if (entity == Entity.Null || !base.EntityManager.TryGetComponent(entity, out refData))
        {
            _selectedEntity = Entity.Null;
            _prefabChanged = _currentPrefab != null;
            _currentPrefab = null;
            _parentObject = null;
            _inspectToolSystem.UIManager.InspectEntity(Entity.Null);
            return false;
        }

        _selectedEntity = entity;
        _parentObject = null;
        PrefabBase newPrefab = _prefabSystem.GetPrefab<PrefabBase>(refData);
        if (newPrefab != _currentPrefab)
        {
            SelectObjectForEntity(entity);
            _currentPrefab = newPrefab;
            _prefabChanged = true;
            RefreshTitle();
            _inspectToolSystem.UIManager.InspectEntity(_selectedEntity);
        }

        return true;
    }

        private EditorSection GetToolControlsGroup() {
        EditorSection editorSection = new EditorSection
        {
            path = new PathSegment(key: "toolMode"),
            displayName = "Tool Mode",
            expanded = true,
            color = new Color(0.02f, 0.65f, 1f),
            children = new[]
            {
                new ToggleField
                {
                    path = "tool_0",
                    displayName = "Any Object",
                    accessor = (new DelegateAccessor<bool>(
                        getter: () => _inspectToolSystem.Mode == 0,
                        setter: b => {
                            if (b)
                            {
                                _inspectToolSystem.Mode = 0;
                            }
                        }))
                },
                new ToggleField
                {
                    path = "tool_1",
                    displayName = "Networks",
                    accessor = (new DelegateAccessor<bool>(
                        getter: () => _inspectToolSystem.Mode == 1,
                        setter: b => {
                            if (b)
                            {
                                _inspectToolSystem.Mode = 1;
                            }
                        }))
                },
                new ToggleField
                {
                    path = "tool_2",
                    displayName = "Props and other objects",
                    accessor = (new DelegateAccessor<bool>(
                        getter: () => _inspectToolSystem.Mode == 2,
                        setter: b => {
                            if (b)
                            {
                                _inspectToolSystem.Mode = 2;
                            }
                        }))
                },
                new ToggleField
                {
                    path = "tool_3",
                    displayName = "Net lanes",
                    accessor = (new DelegateAccessor<bool>(
                        getter: () => _inspectToolSystem.Mode == 3,
                        setter: b => {
                            if (b)
                            {
                                _inspectToolSystem.Mode = 3;
                            }
                        }))
                }
            }
        };
        return editorSection;
    }

        private static ITypedValueAccessor<bool> GetActiveAccessor(ComponentBase component) {
        return new DelegateAccessor<bool>(
            getter: () => component.active,
            setter: (bool value) => { component.active = value; });
    }

        private class Scrollable : LayoutContainer
        {
            public override string propertiesTypeName => "Game.UI.Widgets.Scrollable";
            public Scrollable() => this.flex = FlexLayout.Fill;

            public static Scrollable WithChildren(IList<IWidget> children) {
            Scrollable scrollable = new Scrollable();
            scrollable.children = children;
            return scrollable;
        }
        }
    }
#endif
}