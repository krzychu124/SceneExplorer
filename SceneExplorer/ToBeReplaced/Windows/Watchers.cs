using Game.Prefabs;
using SceneExplorer.Services;
using SceneExplorer.ToBeReplaced.Helpers;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SceneExplorer.ToBeReplaced.Windows
{
    public class Watchers : FloatingWindowBase
    {
        private Pagination<WatcherService.IWatchable> _pagination;
        private Vector2 _scrollPos;
        private EntityManager _entityManager;
        private List<WatcherService.IWatchable> _items;
        private List<WatcherService.IWatchable> _results;
        private string _searchName = string.Empty;
        private bool _updateResults;

        private PrefabSystem _prefabSystem;
        private SnapshotService _snapshotService;
        private WatcherService _watcherService;

        private QueryCreator _queryCreator;

        protected override string Title => "Watchers";
        public override bool CanRemove => false;

        public static Watchers Instance = new GameObject("SceneManager Watchers").AddComponent<Watchers>();

        public Watchers() {
            _minSize = new Vector2(300, 250);
            ForceSize(400, 350);
            _items = new List<WatcherService.IWatchable>();
            _results = new List<WatcherService.IWatchable>();
        }

        protected void Awake() {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _prefabSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
            _snapshotService = SnapshotService.Instance;
            _watcherService = WatcherService.Instance;
            _pagination = new Pagination<WatcherService.IWatchable>(_results);
            _pagination.ItemPerPage = 20;
            _queryCreator = new QueryCreator(ComponentSearch._registeredComponents);
        }

        public override void OnOpen() {
            base.OnOpen();
            RefreshData();
        }

        protected override void RenderWindowContent() {
            GUILayout.BeginHorizontal(options: null);
            if (GUILayout.Button("Clear Watchers", UIStyle.Instance.iconButton, options: null))
            {
                _watcherService.Clear();
            }
            if (GUILayout.Button("Update Watchers", UIStyle.Instance.iconButton, options: null))
            {
                _updateResults = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            int first = 1 + _pagination.ItemPerPage * (_pagination.CurrentPage - 1);
            int last = first + (_pagination.ItemPerPage - 1) > _pagination.ItemCount ? _pagination.ItemCount : first + (_pagination.ItemPerPage);

            CommonUI.ListHeader(first, last, ref _pagination);

            if (!_updateResults && _pagination.ItemCount > 0)
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
                    _scrollPos = GUILayout.BeginScrollView(_scrollPos, options: null);
                    int max = lastItem > count ? count : lastItem;
                    for (int i = firstItem; i < max; i++)
                    {
                        var data = _pagination.Data[i];
                        GUILayout.BeginHorizontal(options: null);
                        GUILayout.Label(data.Preview(), UIStyle.Instance.reducedPaddingLabelStyle, options: null);
                        GUILayout.Space(5);
                        GUILayout.Label(data.PrefabName, UIStyle.Instance.focusedReducedPaddingLabelStyle, options: null);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Inspect", UIStyle.Instance.iconButton, options: null))
                        {
                            data.Inspect();
                        }
                        GUILayout.Space(5);
                        if (GUILayout.Button("X", UIStyle.Instance.iconButton, options: null))
                        {
                            _watcherService.Remove(data);
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

        private void LateUpdate() {
            if (_watcherService.DataChanged)
            {
                RefreshData();
                _watcherService.ResetChanged();
                _updateResults = true;
            }
            if (_updateResults)
            {
                WatcherService.Instance.Update();
                _updateResults = false;
            }
        }

        private void RefreshData() {
            _items.Clear();
            _results.Clear();
            int index = 0;
            foreach (WatcherService.IWatchable w in _watcherService.Watchables)
            {
                if (w.IsValid)
                {
                    _items.Add(w);
                    _results.Add(w);//temp
                }
            }
            // _items.Sort((item, item2) => item.Entity.CompareTo(item2.Entity));

            if (_pagination.FixPage(false))
            {
                _scrollPos = Vector2.zero;
            }
        }
    }
}
