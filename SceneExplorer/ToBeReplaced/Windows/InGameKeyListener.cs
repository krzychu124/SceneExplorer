using System;
using cohtml.Net;
using Game;
using Game.SceneFlow;
using SceneExplorer.System;
using SceneExplorer.Tools;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SceneExplorer.ToBeReplaced.Windows
{
    public partial class InGameKeyListener : GameSystemBase
    {
        private readonly float _clickInterval = 0.3f;
        private bool _hit;
        private float _lastClicked;

        private InputAction _toggleExplorerAction;
        private InputAction _toggleComponentSearchAction;
        private ComponentSearch _searchWindow;

        protected override void OnCreate() {
        base.OnCreate();
        _toggleExplorerAction = new InputAction("ToggleExplorerAction", InputActionType.Button);
        _toggleExplorerAction.AddCompositeBinding("OneModifier").With("Binding", "<keyboard>/e").With("Modifier", "<keyboard>/ctrl");
        _toggleComponentSearchAction = new InputAction("ToggleComponentSearchAction", InputActionType.Button);
        _toggleComponentSearchAction.AddCompositeBinding("OneModifier").With("Binding", "<keyboard>/w").With("Modifier", "<keyboard>/ctrl");
        Enabled = false;
    }

        protected override void OnStartRunning() {
        base.OnStartRunning();
        _toggleExplorerAction.Enable();
        _toggleComponentSearchAction.Enable();
    }

        protected override void OnStopRunning() {
        base.OnStopRunning();
        _toggleExplorerAction.Disable();
        _toggleComponentSearchAction.Disable();
    }

        protected override void OnUpdate() {
        if (_toggleExplorerAction.WasReleasedThisFrame())
        {
            World.GetExistingSystemManaged<InspectObjectToolSystem>().ChangeToolMode();
        }
        else if (_toggleComponentSearchAction.WasReleasedThisFrame())
        {
            if (!_searchWindow)
            {
                _searchWindow = new GameObject("SceneExplorer_ComponentSearch").AddComponent<ComponentSearch>();
                GameObject.DontDestroyOnLoad(_searchWindow.gameObject);
            }
            if (_searchWindow.IsOpen)
            {
                _searchWindow.Close();
            }
            else
            {
                _searchWindow.Open();
            }
        }
    }

        protected override void OnDestroy() {
        base.OnDestroy();
        if (_searchWindow)
        {
            GameObject.Destroy(_searchWindow);
            _searchWindow = null;
        }
    }
    }
}
