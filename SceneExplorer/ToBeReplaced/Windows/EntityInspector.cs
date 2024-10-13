using System;
using System.Collections.Generic;
using Game.Prefabs;
using SceneExplorer.Services;
using SceneExplorer.System;
using SceneExplorer.ToBeReplaced.Helpers;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SceneExplorer.ToBeReplaced.Windows
{
    public interface IParentInspector
    {
        public void PreviewEntity(Entity e, string fieldName, string typeName, bool standalone);
        public IClosablePopup PreviewPrefab(PrefabBase prefab, string typeName, bool standalone);
    }

    public class EntityInspector : FloatingWindowBase, IParentInspector, IValueInspector, IClosablePopup
    {
        private static GUILayoutOption[] _inputOptions = new[] { GUILayout.MinWidth(40) };
        public string TitleSuffix = string.Empty;
        private Dictionary<IInspectableObject, IClosablePopup> _activeInspectors = new Dictionary<IInspectableObject, IClosablePopup>();

        private List<ISection> _components = new List<ISection>();
        private bool _entityChanged;
        private EntityManager _entityManager;
        private EntityEvaluator _evaluator = new EntityEvaluator();

#if DEBUG_PP
        private InspectObjectToolSystem _inspectObjectToolSystem;
#endif
        private Color _legendColor = new Color(0.24f, 0.63f, 0.84f);
        private string _manualEntity = string.Empty;
        private string _manualEntityVersion = string.Empty;
        private List<EntityInspector> _manualInspectors = new List<EntityInspector>();
        private Color _parentNameColor = new Color(1f, 0.67f, 0.02f);
        private ComponentDataRenderer _renderer;
        private Vector2 _scrollPos;
        private Entity _selectedEntity;
        private bool _canJumpTo;
        private EntityInspector _sharedEntityInspectorPopup;
        private PrefabDataInspector _sharedPrefabInspectorPopup;
        private SnapshotService.EntitySnapshotData _snapshotData;
        private PrefabSystem _prefabSystem;
        private SceneExplorerUISystem _sceneExplorerUISystem;
        private Rect _titleSectionRect;

        public EntityInspector() {
            _minSize = new Vector2(250, 250);
            ForceSize(420, 460);
            _prefabSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
        }

        protected override string Title { get; } = "Entity Inspector";
        public override string Subtitle { get; set; } = string.Empty;

        public Entity SelectedEntity
        {
            get { return _selectedEntity; }
            set
            {
                Logging.Debug($"Selecting Entity: {value}");
                if (value != _selectedEntity)
                {
                    _selectedEntity = value;
                    _entityChanged = true;
                    if (_selectedEntity != Entity.Null)
                    {
                        _canJumpTo = InspectObjectUtils.EvaluateCanJumpTo(_entityManager, _selectedEntity);
                    }
                }
            }
        }

        public SnapshotService.EntitySnapshotData SnapshotData
        {
            get => _snapshotData;
            set
            {
                if (value != _snapshotData)
                {
                    _snapshotData = value;
                    if (_snapshotData != null)
                    {
                        _evaluator.SelectedEntity = _snapshotData.Entity;
                        _evaluator.UseSnapshot = true;
                        _evaluator.Evaluate(_entityManager, refreshOnly: false);
                    }
                }
            }
        }

        private void Awake() {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        protected override void Start() {
            base.Start();
            Logging.Info($"Starting EntityInspector: {gameObject.name}");

#if DEBUG_PP
            _prefabSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
            _inspectObjectToolSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<InspectObjectToolSystem>();
            _sceneExplorerUISystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<SceneExplorerUISystem>();
            _renderer = new ComponentDataRenderer(this, _inspectObjectToolSystem);
#endif
#if DEBUG_EXPERIMENTS
        Entity tempEntity = _entityManager.CreateEntity();
        Logging.Info($"TempEntity: {tempEntity}");
        _entityManager.AddComponent<Experiment>(tempEntity);
        Experiment experiment = new Experiment(1234)
        {
            testString = new FixedString128Bytes("TEST_TEXT!"),
            testHandle = GCHandle.Alloc(_renderer),
        };
        _entityManager.SetComponentData<Experiment>(tempEntity, experiment);
#endif
        }

        protected void Update() {
#if DEBUG_PP
            if (SnapshotData != null)
            {
                return;
            }
            InspectedObject inspectedObject = _entityManager.GetComponentData<InspectedObject>(_inspectObjectToolSystem.SystemHandle);
            bool isDirty = _selectedEntity != Entity.Null && _entityManager.Exists(_selectedEntity);
            if (inspectedObject.entityChanged || inspectedObject.isDirty)
            {
                _entityChanged = true;
                _entityManager.SetComponentData(_inspectObjectToolSystem.SystemHandle, new InspectedObject()
                {
                    entity = inspectedObject.entity,
                    entityChanged = false,
                    isDirty = false,
                });
                _selectedEntity = inspectedObject.entity;
                _canJumpTo = InspectObjectUtils.EvaluateCanJumpTo(_entityManager, _selectedEntity);
            }

            if ((isDirty || _entityChanged) && IsOpen)
            {
                bool isOnlyDirty = isDirty && !_entityChanged;
                _entityChanged = false;
                Logging.DebugEvaluation("Building UI");
                if (isOnlyDirty)
                {
                    _components.ForEach(c => c.UpdateBindings(refreshOnly: true));
                    _evaluator.SelectedEntity = _selectedEntity;
                    _evaluator.UseSnapshot = false;
                    _evaluator.Refresh();
                }
                else
                {
                    _evaluator.SelectedEntity = _selectedEntity;
                    _evaluator.UseSnapshot = false;
                    _evaluator.Evaluate(_entityManager);
                    if (_sharedEntityInspectorPopup && _sharedEntityInspectorPopup.IsOpen)
                    {
                        _sharedEntityInspectorPopup.Close();
                    }
                    if (_sharedPrefabInspectorPopup && _sharedPrefabInspectorPopup.IsOpen)
                    {
                        _sharedPrefabInspectorPopup.Close();
                    }
                    _components.ForEach(c => c.ParentInspector = null);
                    _components.Clear();
                    if (_selectedEntity != Entity.Null)
                    {
                        // Subtitle = _selectedEntity.ToString();
                        // Logging.Debug("Generating components");
                        // BuildComponentsUI(_selectedEntity, _entityManager, this, _components);
                        // foreach (ISection component in _components)
                        // {
                        //     component.UpdateBindings(refreshOnly: false);
                        // }
                        // Logging.Debug($"Generated {_components.Count} components");
                    }
                    else
                    {
                        Subtitle = string.Empty;
                    }
                }
            }
#endif
        }

        public override void OnDestroy() {
            base.OnDestroy();

            _components.ForEach(c => c.ParentInspector = null);
            _components.Clear();
        }

        public void PreviewEntity(Entity e, string fieldName, string typeName, bool standalone) {
            if (!_sharedEntityInspectorPopup)
            {
                _sharedEntityInspectorPopup = new GameObject("Shared Entity Inspector Popup").AddComponent<EntityInspector>();
                if (!standalone)
                {
                    _sharedEntityInspectorPopup.ParentWindowId = Id;
                }
            }

            _sharedEntityInspectorPopup.ChainDepth = ChainDepth + 1;
            _sharedEntityInspectorPopup.SelectedEntity = e;
            _sharedEntityInspectorPopup.TitleSuffix = $"{(!string.IsNullOrEmpty(TitleSuffix) ? $"{TitleSuffix} ➜ " : string.Empty)}{_selectedEntity.ToString()}➤{typeName}.{fieldName}";
            _sharedEntityInspectorPopup.Subtitle = $"{e.ToString()} | {typeName}.{fieldName}";
            _sharedEntityInspectorPopup.ForcePosition(Position + new Vector2(22, 22));
            _sharedEntityInspectorPopup.Open();
        }

        public IClosablePopup PreviewPrefab(PrefabBase prefab, string typeName, bool standalone) {
            if (!_sharedPrefabInspectorPopup)
            {
                _sharedPrefabInspectorPopup = new GameObject("Shared Prefab Inspector Popup").AddComponent<PrefabDataInspector>();
                if (!standalone)
                {
                    _sharedPrefabInspectorPopup.ParentWindowId = Id;
                }
            }

            _sharedPrefabInspectorPopup.ChainDepth = ChainDepth + 1;
            _sharedPrefabInspectorPopup.SelectedPrefab = prefab;
            _sharedPrefabInspectorPopup.TitleSuffix = $"{(!string.IsNullOrEmpty(TitleSuffix) ? $"{TitleSuffix} ➜ PrefabData.m_Index" : string.Empty)}";
            _sharedPrefabInspectorPopup.Subtitle = $"{typeName} \"{prefab.name}\"";
            _sharedPrefabInspectorPopup.ForcePosition(Position + new Vector2(22, 22));
            _sharedPrefabInspectorPopup.Open();
            return _sharedPrefabInspectorPopup;
        }

        bool IClosablePopup.IsActive
        {
            get { return this && IsOpen; }
        }

        public event Action<IValueInspector> OnClosed;

        public IClosablePopup Inspect(object value, IInspectableObject o, InspectMode mode) {
            Logging.Info($"Inspecting: {value} | {value.GetType().Name} | {o.FieldInfo?.Name}");
            if (value is Entity e)
            {
                if (mode == InspectMode.Watcher)
                {
                    WatcherService.Instance.Add(new WatcherService.WatchableEntity(e, _prefabSystem));
                    return null;
                }

                if (mode == InspectMode.JumpTo)
                {
                    _sceneExplorerUISystem.NavigateTo(e);
                    return null;
                }
                
                if (SnapshotData != null && SnapshotService.Instance.TryGetSnapshot(e, out SnapshotService.EntitySnapshotData data))
                {
                    return InspectEntity(e, o, mode, data);
                }
                else
                {
                    return InspectEntity(e, o, mode);
                }
            }
            if (mode == InspectMode.Watcher)
            {
                return null;
            }
            if (value is PrefabData pd)
            {
                return InspectPrefabData(pd, o, mode == InspectMode.Standalone);
            }
            if (o.FieldInfo != null && o.FieldInfo.Name.Equals(nameof(PrefabData.m_Index)))
            {
                PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
                PrefabData data = new PrefabData() { m_Index = (int)value };
                if (prefabSystem.TryGetPrefab(data, out PrefabBase prefab))
                {
                    return PreviewPrefab(prefab, prefab.GetType().Name, mode == InspectMode.Standalone);
                }
            }

            return null;
        }

        private IClosablePopup InspectEntity(Entity e, IInspectableObject o, InspectMode mode, SnapshotService.EntitySnapshotData data = null) {
            var inspector = new GameObject("Value Inspector").AddComponent<EntityInspector>();
            if (mode == InspectMode.Linked)
            {
                inspector.ParentWindowId = Id;
            }
            inspector.ChainDepth = ChainDepth + 1;
            inspector.SelectedEntity = e;
            inspector.SnapshotData = data;
            inspector.Subtitle = data != null ? $"[S] {e}" : e.ToString();
            inspector.ForcePosition(Position + new Vector2(20, 20));
            inspector.Open();
            return inspector;
        }

        public IClosablePopup InspectPrefabData(PrefabData data, IInspectableObject o, bool standalone) {
            PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
            if (prefabSystem.TryGetPrefab(data, out PrefabBase prefab))
            {
                return PreviewPrefab(prefab, prefab.GetType().Name, standalone);
            }
            return null;
        }

        public void InspectManagedObject(object value, IInspectableObject o, bool standalone) {
            Logging.DebugEvaluation($"Inspecting Value: {value.ToString()} | {o.GetType().FullName} | {o.FieldInfo?.Name}");
            if (o.FieldInfo != null && o.FieldInfo.Name.Equals(nameof(PrefabData.m_Index)))
            {
                PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
                PrefabData data = new PrefabData() { m_Index = (int)value };
                if (prefabSystem.TryGetPrefab(data, out PrefabBase prefab))
                {
                    PreviewPrefab(prefab, prefab.GetType().Name, standalone);
                }
            }
        }

        public void TryClose() {
            if (ParentWindowId > 0)
            {
                ForceClose();
            }
        }

        public void ForceClose() {
            Logging.DebugEvaluation($"Force closed: {Id}");

            Destroy(this.gameObject);
            OnClosed?.Invoke(this);
            OnClosed = null;
        }

        public void MarkAsRoot() {
            IsRoot = true;
        }

        private static void BuildComponentsUI(Entity selectedEntity, EntityManager entityManager, IParentInspector inspector, List<ISection> components) {
            NativeArray<ComponentType> componentTypes = entityManager.GetComponentTypes(selectedEntity);
            foreach (ComponentType componentType in componentTypes)
            {
                ISection section = UIGenerator.GetUIForComponent(componentType, selectedEntity);
                if (section != null)
                {
                    section.ParentInspector = inspector;
                    components.Add(section);
                }
            }
            componentTypes.Dispose();
        }

        protected override void RenderWindowContent() {

            if (_selectedEntity == Entity.Null && IsRoot)
            {
                GUILayout.Label("No Entity selected", options: null);
                GUILayout.BeginHorizontal(options: null);
                GUILayout.Label("Mode: ", options: null);
                GUI.color = new Color32(255, 216, 13, 255);
#if DEBUG_PP
                GUILayout.Label(InspectObjectUtils.GetModeName(_inspectObjectToolSystem.Mode), options: null);
#endif
                GUI.color = Color.white;
                GUILayout.Label($" ({ModEntryPoint._settings.SwitchToolModeKeybind})", options: null);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(options: null);
                GUILayout.Label("Inspect Entity", options: null);
                GUILayout.Space(3);
                GUILayout.Label("index|version", options: null);
                _manualEntity = GUILayout.TextField(_manualEntity, options: _inputOptions);
                _manualEntityVersion = GUILayout.TextField(_manualEntityVersion, options: _inputOptions);
                GUI.enabled = _manualEntity.Length > 0 && _manualEntityVersion.Length > 0;
                if (GUILayout.Button("Inspect", options: null) &&
                    int.TryParse(_manualEntity, out int index) &&
                    int.TryParse(_manualEntityVersion, out int version))
                {
                    Entity entity = new Entity() { Index = index, Version = version };
                    if (InspectManual(entity))
                    {
                        _manualEntity = string.Empty;
                        _manualEntityVersion = string.Empty;
                    }
                }
                GUI.enabled = true;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                return;
            }

            GUILayout.BeginVertical(options: null);
            if (!string.IsNullOrEmpty(TitleSuffix))
            {
                Color temp = GUI.color;
                GUI.color = _parentNameColor;
                GUILayout.Label($"{TitleSuffix}", options: null);
                GUI.color = temp;
            }
            else
            {
                Color temp = GUI.color;
                GUILayout.BeginHorizontal(options: null);
                GUILayout.Label(_selectedEntity.ToString(), Style.focusedLabelStyle);
                if (_canJumpTo)
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Jump To", UIStyle.Instance.iconButton, options: null))
                    {
                        _sceneExplorerUISystem.NavigateTo(_selectedEntity);
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Label("Right click to clear entity selection", options: null);
                if (this is not ManualEntityInspector)
                {
                    
                    GUILayout.BeginHorizontal(options: null);
                    GUILayout.Label("Mode: ", options: null);
                    GUI.color = new Color32(255, 216, 13, 255);
#if DEBUG_PP
                    GUILayout.Label(InspectObjectUtils.GetModeName(_inspectObjectToolSystem.Mode), options: null);
#endif
                    GUI.color = temp;
                    GUILayout.Label($" ({ModEntryPoint._settings.SwitchToolModeKeybind})", options: null);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                GUI.color = _legendColor;
                GUILayout.BeginHorizontal(options: null);
                var style = UIStyle.Instance.reducedPaddingLabelStyle;
                GUI.color = UIStyle.Instance.unManagedLabelStyle.normal.textColor;
                GUILayout.Label("[Component] ", style, options: null);
                GUI.color = UIStyle.Instance.managedLabelStyle.normal.textColor;
                GUILayout.Label("[Managed] ", style, options: null);
                GUI.color = UIStyle.Instance.bufferLabelStyle.normal.textColor;
                GUILayout.Label("[Buffer] ", style, options: null);
                GUI.color = UIStyle.Instance.sharedLabelStyle.normal.textColor;
                GUILayout.Label("[Shared] ", style, options: null);
                GUI.color = UIStyle.Instance.unknownLabelStyle.normal.textColor;
                GUILayout.Label("[Not Supported] ", style, options: null);
                GUI.color = Color.white;
                GUILayout.Label("[Tag] ", style, options: null);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUI.color = temp;
            }

            GUILayout.Space(12);
            CommonUI.DrawLine();
            GUILayout.EndVertical();
            Rect cached = _titleSectionRect;
            if (Event.current.type == EventType.Repaint)
            {
                Rect lastRect =GUILayoutUtility.GetLastRect();
                _titleSectionRect = new Rect(Rect.x + lastRect.x,Rect.y + lastRect.y + lastRect.height, lastRect.width, Rect.height - lastRect.height - lastRect.y - 20);
            }
            
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, options: null);
            GUILayout.Space(5);
            _evaluator.RenderComponents(_renderer, SelectedEntity, HasFocus ? cached : Rect.zero);

            GUILayout.EndScrollView();
        }


        private bool InspectManual(Entity entity) {
            if (_entityManager.Exists(entity))
            {
                var inspector = new GameObject("Manual Object Inspector").AddComponent<ManualEntityInspector>();
                inspector.ChainDepth = ChainDepth + 1;
                inspector.SelectedEntity = entity;
                inspector.ForcePosition(Position + new Vector2(22, 22));
                inspector.Open();
                return true;
            }

            Logging.Warning($"Entity: {entity} does not exist!");
            return false;
        }

        public override void Close() {
            base.Close();
            _manualEntity = string.Empty;
            _manualEntityVersion = string.Empty;
            _components.ForEach(c => c.ParentInspector = null);
            _components.Clear();
            if (_sharedEntityInspectorPopup && _sharedEntityInspectorPopup.IsOpen)
            {
                _sharedEntityInspectorPopup.Close();
            }
            if (_sharedPrefabInspectorPopup && _sharedPrefabInspectorPopup.IsOpen)
            {
                _sharedPrefabInspectorPopup.Close();
            }
            if (IsRoot)
            {
#if DEBUG_PP
                _inspectObjectToolSystem.RequestDisable();
#endif
                DestroyManualInspectors();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void DestroyManualInspectors() {
            foreach (EntityInspector inspector in _manualInspectors)
            {
                if (inspector)
                {
                    Destroy(inspector.gameObject);
                }
            }
            _manualInspectors.Clear();
        }
    }
}
