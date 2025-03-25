using System;
using System.Collections.Generic;
using Game.Prefabs;
using SceneExplorer.Services;
using SceneExplorer.ToBeReplaced.Helpers;
using Unity.Entities;
using UnityEngine;

namespace SceneExplorer.ToBeReplaced.Windows
{
    public class Snapshots : FloatingWindowBase
    {
        private Pagination<Item> _pagination;
        private Vector2 _scrollPos;
        private EntityManager _entityManager;
        private List<Item> _items;
        private List<Item> _results;
        private string _searchName = string.Empty;
        private string _pageSizeStr = "20";
        private bool _updateResults;

        private PrefabSystem _prefabSystem;
        private SnapshotService _snapshotService;

        private QueryCreator _queryCreator;
        private Query _query;
        private CommonUI.LocalState _allString = new CommonUI.LocalState();
        private CommonUI.LocalState _anyString = new CommonUI.LocalState();
        private CommonUI.LocalState _noneString = new CommonUI.LocalState();

        protected override string Title => "Snapshots";
        public override bool CanRemove => false;

        public static Snapshots Instance = new GameObject("SceneManager Snapshots").AddComponent<Snapshots>();

        public Snapshots() {
            _minSize = new Vector2(300, 250);
            ForceSize(400, 350);
            _items = new List<Item>();
            _results = new List<Item>();
        }

        protected void Awake() {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _prefabSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
            _snapshotService = SnapshotService.Instance;
            _pagination = new Pagination<Item>(_results);
            _pagination.ItemPerPage = 20;
            _queryCreator = new QueryCreator(ComponentSearch._registeredComponents);
            _query = new Query();
        }

        public override void OnOpen() {
            base.OnOpen();
            RefreshData();
        }

        protected override void RenderWindowContent() {
            if (GUILayout.Button("Clear Snapshots", UIStyle.Instance.iconButton, options: null))
            {
                _snapshotService.Clear();
            }

            CommonUI.QueryCreatorSection(_queryCreator, _allString, _anyString, _noneString, Rect, Id);

            GUILayout.Label("Search (entity/prefab name):", options: null);
            string oldName = _searchName;
            _searchName = GUILayout.TextField(_searchName);
            if (!oldName.Equals(_searchName) && (_searchName.Length == 0 || _searchName.Length >= 2 || oldName.Length < 2))
            {
                _updateResults = true;
            }

            GUILayout.BeginHorizontal(options: null);
            GUILayout.Label("Items per page", options: null);
            GUILayout.FlexibleSpace();
            _pageSizeStr = GUILayout.TextField(_pageSizeStr, 4, UIStyle.Instance.textInputLayoutOptions);
            GUILayout.EndHorizontal();

            if (!PaginationHelpers.ValidatePageSizeString(_pageSizeStr, ref _pagination))
            {
                PaginationHelpers.RenderPageRangeError();
            }
            
            (int first, int last) = PaginationHelpers.CalculateFirstLast(ref _pagination);
            
            CommonUI.ListHeader(first, last, ref _pagination);

            if (_pagination.ItemCount > 0)
            {
                GUILayout.BeginHorizontal(UIStyle.Instance.collapsibleContentStyle, options: null);
                GUILayout.Space(12);
                GUILayout.BeginVertical(options: null);

                GUILayout.Space(6);
                int count = _pagination.Data.Count;
                int lastItem = first + _pagination.ItemPerPage;
                if (first < count)
                {
                    _scrollPos = GUILayout.BeginScrollView(_scrollPos, options: null);
                    int max = lastItem > count ? count : lastItem;
                    for (int i = first; i < max; i++)
                    {
                        var data = _pagination.Data[i];
                        GUILayout.BeginHorizontal(options: null);
                        GUILayout.Label(data.Entity.ToString(), UIStyle.Instance.reducedPaddingLabelStyle, options: null);
                        GUILayout.Space(5);
                        GUILayout.Label(data.PrefabName, UIStyle.Instance.focusedReducedPaddingLabelStyle, options: null);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Inspect", UIStyle.Instance.iconButton, options: null))
                        {
                            Inspect(data.Entity, data.SnapshotData);
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndScrollView();
                }

                GUILayout.Space(8);
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }
        }

        private void Inspect(Entity entity, SnapshotService.EntitySnapshotData data) {
            var inspector = new GameObject("Manual Object Inspector(Snapshot)").AddComponent<ManualEntityInspector>();
            inspector.ChainDepth = ChainDepth + 1;
            inspector.SelectedEntity = entity;
            inspector.SnapshotData = data;
            inspector.Subtitle = $"[S] {entity}";
            inspector.ForcePosition(Position + new Vector2(32, -32));
            inspector.Open();
        }

        private void LateUpdate() {
            if (_snapshotService.DataChanged)
            {
                RefreshData();
                _snapshotService.ResetChanged();
                _updateResults = true;
            }
            if (_queryCreator.Changed)
            {
                if (_queryCreator.Sync())
                {
                    _queryCreator.FillQuery(_query);
                }
                else
                {
                    _query.Reset();
                }
                _updateResults = true;
            }
            if (_updateResults)
            {
                _updateResults = false;
                FilterResults();
            }
        }

        private void RefreshData() {
            _items.ForEach(Item.DisposeItem);
            _items.Clear();
            foreach (Entity entity in _snapshotService.Entities)
            {
                if (_snapshotService.TryGetSnapshot(entity, out SnapshotService.EntitySnapshotData data))
                {
                    string prefabName = entity.TryGetPrefabName(_entityManager, _prefabSystem, out string prefabType);
                    string name = !string.IsNullOrEmpty(prefabName) ? $"({prefabType} - {prefabName})" : string.Empty;
                    _items.Add(new Item(entity, name, data));
                }
            }
            _items.Sort((item, item2) => item.Entity.CompareTo(item2.Entity));

            if (_pagination.FixPage(false))
            {
                _scrollPos = Vector2.zero;
            }
        }

        private void FilterResults() {
            _results.Clear();
            foreach (Item item in _items)
            {
                if (item.SnapshotData.Matching(_query) &&
                    (item.Entity.ToString().IndexOf(_searchName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.PrefabName.IndexOf(_searchName, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    _results.Add(item);
                }
            }

            if (_pagination.FixPage(false))
            {
                _scrollPos = Vector2.zero;
            }
        }

        public class Query : QueryCreator.IQuery
        {
            public List<ComponentType> WithAll { get; }
            public List<ComponentType> WithAny { get; }
            public List<ComponentType> WithNone { get; }

            public Query() {
                WithAll = new List<ComponentType>();
                WithAny = new List<ComponentType>();
                WithNone = new List<ComponentType>();
            }

            public void Reset() {
                WithAll.Clear();
                WithAny.Clear();
                WithNone.Clear();
            }

            public void AddAll(ComponentType type) {
                WithAll.Add(type);
            }

            public void AddAny(ComponentType type) {
                WithAny.Add(type);
            }

            public void AddNone(ComponentType type) {
                WithNone.Add(type);
            }
        }

        public class Item : IDisposable
        {
            public Entity Entity { get; }
            public string PrefabName { get; }
            public SnapshotService.EntitySnapshotData SnapshotData { get; private set; }

            public Item(Entity e, string prefabName, SnapshotService.EntitySnapshotData data) {
                Entity = e;
                PrefabName = prefabName;
                SnapshotData = data;
            }

            public void Dispose() {
                SnapshotData = null;
            }

            public static void DisposeItem(Item item) {
                item.Dispose();
            }
        }
    }
}
