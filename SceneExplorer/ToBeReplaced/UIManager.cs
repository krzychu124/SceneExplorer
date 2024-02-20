using System;
using System.Collections.Generic;
using Game.Prefabs;
using Game.SceneFlow;
using SceneExplorer.ToBeReplaced.Helpers;
using SceneExplorer.ToBeReplaced.Windows;
using Unity.Entities;
using UnityEngine;

namespace SceneExplorer.ToBeReplaced
{
    public interface IGuiControl
    {
        void Render();
        void Update();
    }

    public class UIManager : MonoBehaviour
    {
        private List<IGuiControl> _controls = new List<IGuiControl>();

        private FloatingWindowBase _floatingWindow;
        private EntityInspector _entityInspector;
        private FloatingWindowsManager _floatingWindowsManager;

        public bool CursorOverUI => _floatingWindowsManager.CursorOverUI;
    
        public void Awake() {
            _floatingWindowsManager = FloatingWindowBase.WindowManager;
            _floatingWindow = new GameObject("Floating Info Window").AddComponent<ObjectInfo>();
            DontDestroyOnLoad(_floatingWindow.gameObject);
        }

        public void OnDestroy() {
            if (_floatingWindow)
            {
                Destroy(_floatingWindow.gameObject);
                _floatingWindow = null;
            }
            _entityInspector = null;
            _floatingWindowsManager.CloseAll(force: true);
            _floatingWindowsManager = null;
        }

        public void SetBindings(ObjectInfo.DataBindings bindings) {
            // _bindings = bindings;
        }

        public void InspectEntity(Entity entity) {
            var inspector = GetInspectorInstance();
            inspector.SelectedEntity = entity;
            if (entity != Entity.Null)
            {
                inspector.Open();
            }
        }

        public void ShowUI() {
            var inspector = GetInspectorInstance();
            inspector.Open();
        }

        public void HideUI() {
            if (_floatingWindow.IsOpen)
            {
                _floatingWindow.Close();
            }
            _floatingWindowsManager.CloseAll(false);
        }

        public EntityInspector GetInspectorInstance() {
            if (!_entityInspector)
            {
                _entityInspector = new GameObject("Inspector Info Window").AddComponent<EntityInspector>();
                _entityInspector.MarkAsRoot();
            }

            return _entityInspector;
        }

        public void InspectPrefab(PrefabBase prefabBase) {
            var inspector = GetInspectorInstance();
            inspector.PreviewPrefab(prefabBase, prefabBase.name, true);
        }
    }
}
