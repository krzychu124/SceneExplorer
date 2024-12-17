using Game;
using Game.SceneFlow;
using SceneExplorer.ToBeReplaced.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SceneExplorer.ToBeReplaced.Windows
{

    public class ComponentSearch : FloatingWindowBase
    {
        protected override string Title => "Component Search";

        internal static List<TypeIndex> _registeredComponents = new List<TypeIndex>();
        private List<KeyValuePair<TypeIndex, int>> _results = new List<KeyValuePair<TypeIndex, int>>();
        private string _searchName = string.Empty;
        private int _updateInterval = 0;
        private string _updateIntervalStr = "0";
        private bool _updateResults;
        private bool _updateEntityCount;
        private bool _fullNameMatch;
        private bool _caseSensitive = true;
        private bool _updateIntervalError = false;
        private Vector2 _scrollPos;
        private Vector2 _scrollPos2;
        private NativeList<ComponentType> _tempComponents;
        private int _lastUpdate;

        private static GUILayoutOption[] _paginationButton = new GUILayoutOption[] { GUILayout.MinWidth(60), GUILayout.MaxWidth(60), GUILayout.MaxHeight(22) };
        private Pagination<KeyValuePair<TypeIndex, int>> _pagination;
        private EntitiesWithComponentSet _sharedEntitiesInspector;
        private bool _canRemove = false;
        public override bool CanRemove => _canRemove;

        private QueryCreator _queryCreator;
        private CommonUI.LocalState _allString = new CommonUI.LocalState();
        private CommonUI.LocalState _anyString = new CommonUI.LocalState();
        private CommonUI.LocalState _noneString = new CommonUI.LocalState();

        public ComponentSearch() {
            _minSize = new Vector2(300, 350);
            ForceSize(420, 500);
        }

        protected override void Start() {
            base.Start();
            _tempComponents = new NativeList<ComponentType>(1, Allocator.Persistent);
            _registeredComponents = TypeManager.AllTypes
                .Where(t => t.TypeIndex != TypeIndex.Null && (t.Category == TypeManager.TypeCategory.ComponentData || t.Category == TypeManager.TypeCategory.BufferData || t.Category == TypeManager.TypeCategory.ISharedComponentData))
                .Select(t => t.TypeIndex)
                .ToList();
            _registeredComponents.Sort((index, typeIndex) => string.Compare(ComponentType.FromTypeIndex(index).GetManagedType()?.FullName, ComponentType.FromTypeIndex(typeIndex).GetManagedType()?.FullName));
            _pagination = new Pagination<KeyValuePair<TypeIndex, int>>(_results);
            _queryCreator = new QueryCreator(_registeredComponents);
        }

        protected override void RenderWindowContent() {
            GUILayout.Label($"Registered components: {_registeredComponents.Count}", options: null);
            GUILayout.Label("Search by name:", options: null);
            string oldName = _searchName;
            _searchName = GUILayout.TextField(_searchName);
            if (!oldName.Equals(_searchName) && (_searchName.Length >= 3 || oldName.Length < 3))
            {
                _updateResults = true;
            }
            bool old = _fullNameMatch;
            GUILayout.Space(8);
            GUILayout.BeginHorizontal(options: null);
            if (GUILayout.Button("Search with query", UIStyle.Instance.iconButton, options: null))
            {
                OpenTypeEntities(TypeIndex.Null, standalone: true);
            }
            #if DEBUG
            if (GUILayout.Button("Snapshots", UIStyle.Instance.iconButton, options: null))
            #else
            if (GameManager.instance.gameMode == GameMode.Editor && GUILayout.Button("Snapshots", UIStyle.Instance.iconButton, options: null))
            #endif
            {
                OpenSnapshots();
            }
            //TODO Watchers
            // if (GUILayout.Button("Watchers", UIStyle.Instance.iconButton, options: null))
            // {
            //     OpenWatchers();
            // }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);
            _fullNameMatch = GUILayout.Toggle(_fullNameMatch, "Type FullName matching", options: null);
            _updateResults = _updateResults || old != _fullNameMatch;

            old = _caseSensitive;
            _caseSensitive = GUILayout.Toggle(_caseSensitive, "Case Sensitive", options: null);
            _updateResults = _updateResults || old != _caseSensitive;

            GUILayout.BeginHorizontal(options: null);
            GUILayout.Label("Update interval: ", options: null);
            string lastFreq = _updateIntervalStr;
            _updateIntervalStr = GUILayout.TextField(_updateIntervalStr, 3, UIStyle.Instance.textInputLayoutOptions);
            GUILayout.EndHorizontal();
            ValidateInterval(lastFreq);
            if (_updateIntervalError)
            {
                Color temp = GUI.color;
                GUI.color = Color.red;
                GUILayout.Label("Invalid value. Min. 5 or 0 for no updates", options: null);
                GUI.color = temp;
            }
            if (_searchName.Length >= 3 && _updateInterval != 0 && _lastUpdate >= 0)
            {
                GUILayout.Label($"Next update in {_updateInterval - _lastUpdate} ({_updateInterval})", options: null);
            }
            GUILayout.Space(8);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, options: null);

            GUILayout.BeginHorizontal(UIStyle.Instance.collapsibleContentStyle, options: null);
            GUILayout.Space(8);

            GUILayout.Label("Items", UIStyle.Instance.paginationLabelStyle, options: null);
            GUILayout.FlexibleSpace();
            int first = 1 + _pagination.ItemPerPage * (_pagination.CurrentPage - 1);
            int last = first + (_pagination.ItemPerPage - 1) > _pagination.ItemCount ? _pagination.ItemCount : first + (_pagination.ItemPerPage);
            GUILayout.Label($" {first} - {last} of {_pagination.ItemCount} ", UIStyle.Instance.paginationLabelStyle, options: null);
            GUI.enabled = _pagination.CurrentPage > 1;
            if (GUILayout.Button(" ◀ ", UIStyle.Instance.iconButton, _paginationButton))
            {
                _pagination.PreviousPage();
            }
            GUI.enabled = _pagination.CurrentPage < _pagination.PageCount;
            if (GUILayout.Button(" ▶ ", UIStyle.Instance.iconButton, _paginationButton))
            {
                _pagination.NextPage();
            }
            GUI.enabled = true;

            GUILayout.EndHorizontal();

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
                        GUILayout.Label(data.Key.IsBuffer ? "[B] " :
                            data.Key.IsZeroSized ? "[T] " :
                            data.Key.IsComponentType && !data.Key.IsSharedComponentType ? "[C] " : "[N] ", UIStyle.Instance.reducedPaddingLabelStyle);
                        GUILayout.Label(TypeManager.GetType(data.Key).FullName, options: null);
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(data.Value.ToString(), UIStyle.Instance.reducedPaddingLabelStyle, options: null);
                        GUILayout.Space(5);
                        if (GUILayout.Button("Entities", UIStyle.Instance.iconButton, options: null))
                        {
                            OpenTypeEntities(data.Key);
                        }
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

        private void OpenTypeEntities(TypeIndex dataKey, bool standalone = false) {
            if (standalone)
            {
                EntitiesWithComponentSet popup = new GameObject("Shared Entities with Component Inspector").AddComponent<EntitiesWithComponentSet>();
                popup.ChainDepth = ChainDepth + 1;
                popup.ForcePosition(Position + new Vector2(22, 22));
                popup.InspectByQuery(dataKey);
                popup.Subtitle = "by EntityQuery";
                popup.Open();
                return;
            }

            if (!_sharedEntitiesInspector)
            {
                _sharedEntitiesInspector = new GameObject("Shared Entities with Component Inspector").AddComponent<EntitiesWithComponentSet>();
                _sharedEntitiesInspector.ParentWindowId = Id;
                _sharedEntitiesInspector.ChainDepth = ChainDepth + 1;
                _sharedEntitiesInspector.ForcePosition(Position + new Vector2(22, 22));
                _sharedEntitiesInspector.InspectByQuery(dataKey);
                _sharedEntitiesInspector.Subtitle = ComponentType.FromTypeIndex(dataKey).GetManagedType().FullName;
                _sharedEntitiesInspector.Open();
            }
            else
            {
                _sharedEntitiesInspector.InspectByQuery(dataKey);
                _sharedEntitiesInspector.Subtitle = ComponentType.FromTypeIndex(dataKey).GetManagedType().FullName;
                _sharedEntitiesInspector.Open();
            }
        }

        private void OpenSnapshots() {
            Snapshots.Instance.Open();
        }

        private void OpenWatchers() {
            Watchers.Instance.Open();
        }

        private void ValidateInterval(string last) {
            if (!last.Equals(_updateIntervalStr))
            {
                if (int.TryParse(_updateIntervalStr, out int freq) && (freq >= 5 || freq == 0))
                {
                    _updateInterval = freq;
                    _lastUpdate = 0;
                    _updateIntervalError = false;
                }
                else
                {
                    _updateIntervalError = true;
                }
            }
        }

        public void LateUpdate() {
            if (!IsOpen)
            {
                return;
            }

            if (_updateResults)
            {
                _lastUpdate = 0;
                _updateResults = false;
                _results.Clear();
                if (_searchName.Length >= 3)
                {
                    EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
                    foreach (TypeIndex registeredComponent in _registeredComponents)
                    {
                        ComponentType type = ComponentType.FromTypeIndex(registeredComponent);
                        Type managedType = type.GetManagedType();
                        if (managedType == null)
                        {
                            continue;
                        }
                        string name = _fullNameMatch ? managedType.FullName : managedType.Name;
                        if (name.IndexOf(_searchName, _caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _tempComponents.Clear();
                            _tempComponents.Add(in type);
                            EntityQuery q = new EntityQueryBuilder(Allocator.Temp).WithAll(ref _tempComponents).Build(manager);
                            _results.Add(new KeyValuePair<TypeIndex, int>(registeredComponent, q.CalculateEntityCount()));
                            q.Dispose();
                        }
                    }
                }
                if (_pagination.FixPage(reset: false))
                {
                    _scrollPos = Vector2.zero;
                    _scrollPos2 = Vector2.zero;
                }
            }
            else if (_pagination.ItemCount > 0 && _updateInterval != 0 && _lastUpdate > _updateInterval)
            {
                _lastUpdate = 0;
                EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
                if (_pagination.TryGetPaginationData(out int index, out int lastItemIndex))
                {
                    List<KeyValuePair<TypeIndex, int>> data = _pagination.Data;
                    for (; index < lastItemIndex; index++)
                    {
                        KeyValuePair<TypeIndex, int> result = data[index];
                        ComponentType type = ComponentType.FromTypeIndex(result.Key);
                        _tempComponents.Clear();
                        _tempComponents.Add(in type);
                        EntityQuery q = new EntityQueryBuilder(Allocator.Temp).WithAll(ref _tempComponents).Build(manager);
                        data[index] = new KeyValuePair<TypeIndex, int>(result.Key, q.CalculateEntityCount());
                        q.Dispose();
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
        }

        public override void Close() {
            base.Close();
            if (_sharedEntitiesInspector && _sharedEntitiesInspector.IsOpen)
            {
                _sharedEntitiesInspector.Close();
            }
            GameManager.instance.inputManager.hasInputFieldFocus = false;
        }

        private void OnDisable() {
            GameManager.instance.inputManager.hasInputFieldFocus = false;
        }
    }
}