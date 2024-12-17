using Game;
using Game.Input;
using Game.Prefabs;
using Game.SceneFlow;
using SceneExplorer.Services;
using SceneExplorer.ToBeReplaced.Helpers;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SceneExplorer.ToBeReplaced.Windows
{
    public class EntitiesWithComponentSet : FloatingWindowBase
    {
        protected override string Title => "Entities";

        private int _updateInterval = 60;
        private int _itemUpdateInterval = 30;
        private int _lastUpdate = 0;
        private int _lastItemUpdate = 0;
        private bool _updateIntervalError1;
        private bool _updateIntervalError2;
        private string _updateIntervalStr1 = "60";
        private string _updateIntervalStr2 = "30";
        private bool _requireListUpdate;
        private TypeIndex _currentType = TypeIndex.Null;
        private Pagination<Item> _pagination;
        private Vector2 _scrollPos;
        private Vector2 _scrollPos2;
        private List<Item> _items = new List<Item>();
        private EntityManager _entityManager;
        private PrefabSystem _prefabSystem;

        private QueryCreator _queryCreator;
        private CommonUI.LocalState _allString = new CommonUI.LocalState();
        private CommonUI.LocalState _anyString = new CommonUI.LocalState();
        private CommonUI.LocalState _noneString = new CommonUI.LocalState();
        private ProxyAction _snapshotEntities;

        // private bool _searchByQuery;

        public override bool CanRemove => ParentWindowId != -1;
        public TypeIndex CurrentType => _currentType;

        public EntitiesWithComponentSet()
        {
            _minSize = new Vector2(300, 250);
            ForceSize(400, 350);
        }

        protected void Awake()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _prefabSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
            _pagination = new Pagination<Item>(_items);
            _pagination.ItemPerPage = 20;
            _queryCreator = new QueryCreator(ComponentSearch._registeredComponents);
            _snapshotEntities = ModEntryPoint.Settings.GetAction(Settings.MakeSnapshotAction);
            _snapshotEntities.onInteraction += OnMakeSnapshot;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (_snapshotEntities != null)
            {
                _snapshotEntities.onInteraction -= OnMakeSnapshot;
                _snapshotEntities = null;
            }
        }

        protected override void RenderWindowContent()
        {

            CommonUI.QueryCreatorSection(_queryCreator, _allString, _anyString, _noneString, Rect, Id);

            GUILayout.BeginHorizontal(options: null);
            GUILayout.Label("List update interval", options: null);
            GUILayout.FlexibleSpace();
            string lastFreq = _updateIntervalStr1;
            _updateIntervalStr1 = GUILayout.TextField(_updateIntervalStr1, 3, UIStyle.Instance.textInputLayoutOptions);
            GUILayout.EndHorizontal();

            ValidateInterval1(lastFreq);
            if (_updateIntervalError1)
            {
                Color temp = GUI.color;
                GUI.color = Color.red;
                GUILayout.Label("Invalid value. Min. 5 or 0 for no updates", options: null);
                GUI.color = temp;
            }

            GUILayout.BeginHorizontal(options: null);
            GUILayout.Label("Item validation interval", options: null);
            GUILayout.FlexibleSpace();
            lastFreq = _updateIntervalStr2;
            _updateIntervalStr2 = GUILayout.TextField(_updateIntervalStr2, 3, UIStyle.Instance.textInputLayoutOptions);
            GUILayout.EndHorizontal();

            ValidateInterval2(lastFreq);
            if (_updateIntervalError2)
            {
                Color temp = GUI.color;
                GUI.color = Color.red;
                GUILayout.Label("Invalid value. Min. 5 or 0 for no updates", options: null);
                GUI.color = temp;
            }

            GUILayout.Space(8);
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, options: null);

            int first = 1 + _pagination.ItemPerPage * (_pagination.CurrentPage - 1);
            int last = first + (_pagination.ItemPerPage - 1) > _pagination.ItemCount ? _pagination.ItemCount : first + (_pagination.ItemPerPage);

            CommonUI.ListHeader(first, last, ref _pagination);

            if (_pagination.ItemCount > 0)
            {
                GUILayout.BeginHorizontal(UIStyle.Instance.collapsibleContentStyle, options: null);
                GUILayout.Space(12);
                GUILayout.BeginVertical(options: null);

                GUILayout.Space(6);
                int count = _pagination.Data.Count;
                int firstItem = (_pagination.CurrentPage - 1) * _pagination.ItemPerPage;
                int lastItem = firstItem + _pagination.ItemPerPage;
                if (firstItem < count)
                {
                    _scrollPos2 = GUILayout.BeginScrollView(_scrollPos2, options: null);
                    int max = lastItem > count ? count : lastItem;
                    for (int i = firstItem; i < max; i++)
                    {
                        var data = _pagination.Data[i];
                        GUILayout.BeginHorizontal(options: null);
                        GUILayout.Label(data.Entity.ToString(), UIStyle.Instance.reducedPaddingLabelStyle, options: null);
                        GUILayout.Space(5);
                        GUILayout.Label(data.PrefabName, UIStyle.Instance.focusedReducedPaddingLabelStyle, options: null);
                        GUILayout.FlexibleSpace();

#if !DEBUG
                        // allow making entity snapshots only in the Editor
                        if (GameManager.instance.gameMode == GameMode.Editor)
                        {
#endif
                        if (data.SnapshotExists)
                        {
                            if (GUILayout.Button("Inspect Snapshot", UIStyle.Instance.iconButton, options: null))
                            {
                                InspectSnapshotManual(data.Entity);
                            }
                        }
                        else
                        {
                            if (data.IsValid && GUILayout.Button("Snapshot", UIStyle.Instance.iconButton, options: null))
                            {
                                data.TryMakeSnapshot();
                                _pagination.Data[i] = data;
                            }
                        }
#if !DEBUG
                        }
#endif
                        GUI.enabled = data.IsValid;
                        if (GUILayout.Button("Inspect", UIStyle.Instance.iconButton, options: null))
                        {
                            if (!InspectManual(data.Entity))
                            {
                                data.Validate(_entityManager, _prefabSystem);
                                _pagination.Data[i] = data;
                            }
                        }
                        GUI.enabled = true;
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndScrollView();
                }

                GUILayout.Space(8);
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();

            }
            GUILayout.EndScrollView();
        }

        private void ValidateInterval1(string last)
        {
            if (!last.Equals(_updateIntervalStr1))
            {
                if (int.TryParse(_updateIntervalStr1, out int freq) && (freq >= 1 || freq == 0))
                {
                    _updateInterval = freq;
                    _lastUpdate = 0;
                    _updateIntervalError1 = false;
                }
                else
                {
                    _updateIntervalError1 = true;
                }
            }
        }

        private void ValidateInterval2(string last)
        {
            if (!last.Equals(_updateIntervalStr2))
            {
                if (int.TryParse(_updateIntervalStr2, out int freq) && (freq >= 1 || freq == 0))
                {
                    _itemUpdateInterval = freq;
                    _lastItemUpdate = 0;
                    _updateIntervalError2 = false;
                }
                else
                {
                    _updateIntervalError2 = true;
                }
            }
        }

        public void InspectByQuery(TypeIndex typeIndex)
        {
            _currentType = typeIndex;
            _queryCreator.Clear();
            if (typeIndex != TypeIndex.Null)
            {
                _queryCreator.Add(ComponentType.FromTypeIndex(typeIndex), QueryCreator.MatchingType.WithAll);
                if (_queryCreator.FillBuilder(_entityManager, out EntityQuery query))
                {
                    UpdateListByQuery(query);
                }
            }
        }

        public void InspectComponentEntities(TypeIndex typeIndex)
        {
            if (_currentType != typeIndex)
            {
                _currentType = typeIndex;
                if (typeIndex != TypeIndex.Null)
                {
                    InspectByQuery(typeIndex);
                }
            }
        }

        public void RequestListUpdate(TypeIndex typeIndex)
        {
            if (typeIndex != TypeIndex.Null && _currentType == typeIndex)
            {
                _requireListUpdate = true;
            }
        }

        public void LateUpdate()
        {
            if (!IsOpen)
            {
                return;
            }

            bool updatedByQuery = false;
            if (_queryCreator.Changed)
            {
                if (_queryCreator.Sync())
                {
                    _queryCreator.FillBuilder(_entityManager, out EntityQuery query);
                    UpdateListByQuery(query, false);
                    updatedByQuery = true;
                }
                else
                {
                    _items.Clear();
                    _pagination.FixPage(true);
                    _scrollPos = Vector2.zero;
                    _scrollPos2 = Vector2.zero;
                }
            }

            if ( /*_currentType != TypeIndex.Null &&*/
                (_requireListUpdate || _updateInterval != 0 && _lastUpdate > _updateInterval))
            {
                _requireListUpdate = false;
                _lastUpdate = 0;
                if (!updatedByQuery && _queryCreator.FillBuilder(_entityManager, out EntityQuery query))
                {
                    UpdateListByQuery(query, false);
                }
            }

            if (_pagination.ItemCount > 0 && _itemUpdateInterval != 0 && _lastItemUpdate > _itemUpdateInterval)
            {
                _lastItemUpdate = 0;
                if (_pagination.TryGetPaginationData(out int index, out int lastItemIndex))
                {
                    List<Item> data = _pagination.Data;
                    for (; index < lastItemIndex; index++)
                    {
                        Item item = data[index];
                        item.Validate(_entityManager, _prefabSystem);
                        data[index] = item;
                    }
                }
                else
                {
                    Logging.DebugEvaluation("Resetting page!");
                    _pagination.FixPage(true);
                    _scrollPos = Vector2.zero;
                    _scrollPos2 = Vector2.zero;
                }
            }
            _lastUpdate++;
            _lastItemUpdate++;
        }

        private void OnMakeSnapshot(ProxyAction proxyAction, InputActionPhase inputActionPhase)
        {
            if (inputActionPhase != InputActionPhase.Performed || !IsOpen || IsMinimized || !HasFocus || _items.Count == 0)
            {
                return;
            }
#if !DEBUG 
            if (GameManager.instance.gameMode != GameMode.Editor) {
                return;
            }
#endif
            Logging.Info($"Making snapshot triggered for {_items.Count} entities");
            foreach (Item item in _items)
            {
                item.TryMakeSnapshot();
            }
        }

        private bool InspectManual(Entity entity)
        {
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

        private void InspectSnapshotManual(Entity entity)
        {
            if (SnapshotService.Instance.TryGetSnapshot(entity, out SnapshotService.EntitySnapshotData data))
            {
                var inspector = new GameObject("Manual Object Inspector(Snapshot)").AddComponent<ManualEntityInspector>();
                inspector.ChainDepth = ChainDepth + 1;
                inspector.SelectedEntity = entity;
                inspector.SnapshotData = data;
                inspector.ForcePosition(Position + new Vector2(22, 22));
                inspector.Open();
            }
        }

        private void UpdateListByQuery(EntityQuery query, bool reset = true)
        {
            _items.Clear();
            UpdateListInternal(query, reset);
        }

        // private void UpdateList(bool reset = true) {
        //     _items.Clear();
        //     NativeList<ComponentType> types = new NativeList<ComponentType>(Allocator.Temp);
        //     ComponentType componentType = ComponentType.FromTypeIndex(_currentType);
        //     types.Add(in componentType);
        //     EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll(ref types).Build(_entityManager);
        //     UpdateListInternal(query, reset);
        //     types.Dispose();
        // }

        private void UpdateListInternal(EntityQuery query, bool reset = true)
        {
            NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (var i = 0; i < entities.Length; i++)
            {
                Item item = new Item(entities[i]);
                item.Validate(_entityManager, _prefabSystem);
                _items.Add(item);
            }
            query.Dispose();
            entities.Dispose();
            if (_pagination.FixPage(reset))
            {
                _scrollPos = Vector2.zero;
                _scrollPos2 = Vector2.zero;
            }
        }

        public override void Close()
        {
            base.Close();
            GameManager.instance.inputManager.hasInputFieldFocus = false;

            Destroy(gameObject);
        }

        public struct Item
        {
            public Entity Entity { get; private set; }
            public string PrefabName { get; private set; }
            public bool IsValid { get; private set; }
            public bool SnapshotExists => SnapshotService.Instance.HasSnapshot(Entity);

            public Item(Entity entity)
            {
                Entity = entity;
                IsValid = true;
                PrefabName = string.Empty;
            }

            public void Validate(EntityManager entityManager, PrefabSystem prefabSystem)
            {
                IsValid = Entity != Entity.Null && entityManager.Exists(Entity);
                string prefabName = Entity.TryGetPrefabName(entityManager, prefabSystem, out string prefabType);
                PrefabName = !string.IsNullOrEmpty(prefabName) ? $"({prefabType} - {prefabName})" : string.Empty;
            }

            public void TryMakeSnapshot()
            {
                if (!SnapshotExists)
                {
                    SnapshotService.Instance.MakeSnapshot(Entity);
                }
            }
        }
    }
}
