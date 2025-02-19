using cohtml.Net;
using Game;
using Game.Input;
using Game.SceneFlow;
using SceneExplorer.ToBeReplaced;
using UnityEngine;

namespace SceneExplorer.System
{
    public partial class InputGuiInteractionSystem : GameSystemBase
    {
        private UIManager _uiManager;
        private InputManager _inputManager;
        private bool _guiWasFocused;
        public bool UIHasInput { get; private set; }
    
        protected override void OnCreate() {
            base.OnCreate();
            _uiManager = GameObject.FindObjectOfType<UIManager>();
            _inputManager = InputManager.instance;
            Enabled = false;
        }

        protected override void OnUpdate() {
            if (_uiManager.CursorOverUI)
            {
                _inputManager.mouseOverUI = true;
            }
            HandleGUIInputFocus();
        }

        private void HandleGUIInputFocus() {
            bool hasGUIFocus = GUIUtility.keyboardControl != 0;
            if (!UIHasInput)
            {
                if (hasGUIFocus && !_guiWasFocused)
                {
                    _inputManager.hasInputFieldFocus = true;
                }
                else if (!hasGUIFocus && _guiWasFocused)
                {
                    _inputManager.hasInputFieldFocus = false;
                }
            }
            _guiWasFocused = hasGUIFocus;
        }

        protected override void OnStartRunning() {
            base.OnStartRunning();
            GameManager.instance.userInterface.view.Listener.TextInputTypeChanged += OnTextInputTypeChanged;

        }

        protected override void OnStopRunning() {
            base.OnStopRunning();
            GameManager.instance.userInterface.view.Listener.TextInputTypeChanged -= OnTextInputTypeChanged;
        }

        private void OnTextInputTypeChanged(ControlType obj) {
            UIHasInput = obj == ControlType.TextInput;
        }
    }
}
