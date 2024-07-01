using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Prefabs;
using SceneExplorer.Services;
using SceneExplorer.ToBeReplaced.Helpers;
using SceneExplorer.ToBeReplaced.Helpers.ContentItems;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SceneExplorer.ToBeReplaced.Windows
{
    public class PrefabDataInspector : FloatingWindowBase, IParentInspector, IClosablePopup, IValueInspector
    {
        public string TitleSuffix = string.Empty;
        public List<ISectionItem> _components = new List<ISectionItem>();
        private bool _dataChanged;

        private Color _parentNameColor = new Color(1f, 0.67f, 0.02f);
        private string _prefabTypeName = string.Empty;
        private Vector2 _scrollPos;
        private PrefabBase _selectedPrefab;

        private EntityInspector _sharedInspectorPopup;
        private PrefabDataInspector _sharedPrefabInspectorPopup;
        private ComplexObject _complexObject;
        private InspectableObjectRenderer _objectRenderer;

        public PrefabDataInspector() {
            _minSize = new Vector2(200, 300);
            ForceSize(420, 460);
        }

        protected override string Title { get; } = "PrefabData Inspector";
        public override string Subtitle { get; set; } = string.Empty;

        public PrefabBase SelectedPrefab
        {
            get { return _selectedPrefab; }
            set
            {
                Logging.Debug("Selecting PrefabData");
                if (value != _selectedPrefab)
                {
                    _selectedPrefab = value;
                    _dataChanged = true;
                }
            }
        }

        protected override void Start() {
            base.Start();
            Logging.Info($"Starting EntityInspector: {gameObject.name}");
            _objectRenderer = new InspectableObjectRenderer();

        }

        protected void Update() {
            if (_dataChanged)
            {
                _dataChanged = false;
                Logging.Debug("Building UI");
                if (_sharedInspectorPopup && _sharedInspectorPopup.IsOpen)
                {
                    _sharedInspectorPopup.Close();
                }
                if (_sharedPrefabInspectorPopup && _sharedPrefabInspectorPopup.IsOpen)
                {
                    _sharedPrefabInspectorPopup.Close();
                }
                _components.Clear();
                if (_selectedPrefab)
                {
                    Logging.Debug("Generating data");
                    BuildComponentsUI();
                    Logging.Debug($"Generated {_components.Count} components");
                }
                if (_complexObject != null)
                {
                    var temp = _complexObject.IsActive;
                    _complexObject.IsActive = true;
                    _complexObject.UpdateValue(_selectedPrefab, false);
                    _complexObject.IsActive = temp;
                }
                return;
            }

            _complexObject?.UpdateValue(_selectedPrefab, false);
        }

        public override void OnDestroy() {
            base.OnDestroy();

            _components.Clear();
            if (_sharedInspectorPopup)
            {
                Destroy(_sharedInspectorPopup.gameObject);
                _sharedInspectorPopup = null;
            }
            if (_sharedPrefabInspectorPopup)
            {
                Destroy(_sharedPrefabInspectorPopup.gameObject);
                _sharedPrefabInspectorPopup = null;
            }
        }

        public void TryClose() {
            if (ParentWindowId > 0)
            {
                ForceClose();
            }
        }

        public void ForceClose() {
            Destroy(gameObject);
        }

        public bool IsActive => this && IsOpen;

        public void PreviewEntity(Entity e, string fieldName, string typeName, bool standalone) {
            if (!_sharedInspectorPopup)
            {
                _sharedInspectorPopup = new GameObject("Shared Inspector Popup").AddComponent<EntityInspector>();
                if (!standalone)
                {
                    _sharedInspectorPopup.ParentWindowId = Id;
                }
            }
            _sharedInspectorPopup.ChainDepth = ChainDepth + 1;
            _sharedInspectorPopup.SelectedEntity = e;
            _sharedInspectorPopup.TitleSuffix = $"{(!string.IsNullOrEmpty(TitleSuffix) ? $"{TitleSuffix} ➜ " : string.Empty)}?➜{typeName}.{fieldName}";
            _sharedInspectorPopup.Subtitle = e.ToString();
            _sharedInspectorPopup.ForcePosition(Position + new Vector2(22, 22));
            _sharedInspectorPopup.Open();
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
            _sharedPrefabInspectorPopup.TitleSuffix = $"{(!string.IsNullOrEmpty(TitleSuffix) ? $"{TitleSuffix} ➜ [{prefab.GetType().Name}]" : string.Empty)}";
            _sharedPrefabInspectorPopup.Subtitle = typeName;
            _sharedPrefabInspectorPopup.ForcePosition(Position + new Vector2(22, 22));
            _sharedPrefabInspectorPopup.Open();
            return _sharedPrefabInspectorPopup;
        }

        public override bool InsideUI(Vector2 cursorPos) {
            return base.InsideUI(cursorPos);
            // bool result = base.InsideUI(cursorPos);
            // if (_sharedInspectorPopup && _sharedInspectorPopup.IsOpen)
            // {
            //     result |= _sharedInspectorPopup.InsideUI(cursorPos);
            // }
            // if (_sharedPrefabInspectorPopup && _sharedPrefabInspectorPopup.IsOpen)
            // {
            //     result |= _sharedPrefabInspectorPopup.InsideUI(cursorPos);
            // }
            // return result;
        }

        private void BuildComponentsUI() {
            _prefabTypeName = _selectedPrefab.GetType().Name;
            Logging.Debug($"Prefab: {_prefabTypeName}");
            var t = _selectedPrefab.GetType();
            List<FieldInfo> fields = TypeDescriptorService.Instance.GetFields(t);
            _complexObject = new ComplexObject(t, fields, false);
        }

        protected override void RenderWindowContent() {
            if (!_selectedPrefab)
            {
                GUILayout.Label("No PrefabData selected", options: null);
                return;
            }

            if (!string.IsNullOrEmpty(TitleSuffix))
            {
                Color temp = GUI.color;
                GUI.color = _parentNameColor;
                GUILayout.Label($"{TitleSuffix}", options: null);
                GUI.color = temp;
            }

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, options: null);
            // GUILayout.Space(5);
            //
            // for (var i = 0; i < _components.Count; i++)
            // {
            //     _components[i].Render();
            //     CommonUI.DrawLine();
            // }
            //
            if (_complexObject != null)
            {
                GUILayout.Space(20);
                _complexObject.IsActive = true;
                _objectRenderer.Render(_complexObject, this, -1, HasFocus ? Rect : Rect.zero, out _);
            }
            GUILayout.EndScrollView();
        }

        public override void Close() {
            base.Close();
            _components.Clear();
            _selectedPrefab = default;
            if (_sharedInspectorPopup && _sharedInspectorPopup.IsOpen)
            {
                _sharedInspectorPopup.Close();
            }
            if (_sharedPrefabInspectorPopup && _sharedPrefabInspectorPopup.IsOpen)
            {
                _sharedPrefabInspectorPopup.Close();
            }
        }

        public IClosablePopup Inspect(object value, IInspectableObject o, InspectMode mode) {
            return null;
        }
    }
}
