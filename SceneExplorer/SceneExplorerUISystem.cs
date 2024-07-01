using System;
using System.Collections.Generic;
using System.Linq;
using Colossal;
using Colossal.Localization;
using Colossal.Serialization.Entities;
using Game;
using Game.Input;
using Game.SceneFlow;
using Game.UI;
using Game.UI.Editor;
using SceneExplorer.System;
using SceneExplorer.ToBeReplaced;
using SceneExplorer.ToBeReplaced.Windows;
using SceneExplorer.Tools;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace SceneExplorer
{
    public partial class SceneExplorerUISystem : UISystemBase
    {
        public UIManager UiManager;
        
        private ProxyAction _toggleExplorerAction;
        private ProxyAction _toggleComponentSearchAction;
        private ComponentSearch _searchWindow;
        
        public override GameMode gameMode
        {
            get { return GameMode.GameOrEditor; }
        }   
        
        protected override void OnCreate() {
            base.OnCreate();
            Logging.Info($"OnCreate in {nameof(SceneExplorerUISystem)}");
            
            UiManager = new GameObject("SceneExplorer GUI").AddComponent<UIManager>();
            Object.DontDestroyOnLoad(UiManager.gameObject);
            
            _toggleExplorerAction = ModEntryPoint._settings.GetAction(Settings.ToggleToolAction);
            _toggleComponentSearchAction = ModEntryPoint._settings.GetAction(Settings.ToggleComponentSearchAction);
            _toggleExplorerAction.onInteraction += OnToggleSceneExplorerTool;
            _toggleComponentSearchAction.onInteraction += OnToggleComponentSearchWindow;

            Logging.Info("Done!");
        }

        protected override void OnGamePreload(Purpose purpose, GameMode mode) {
            base.OnGamePreload(purpose, mode);
            if (mode == GameMode.Editor)
            {
                ToggleInputActions(true);
                World.GetExistingSystemManaged<InputGuiInteractionSystem>().Enabled = true;
#if DEBUG_PP
                EditorToolUISystem editorToolUISystem = World.GetExistingSystemManaged<EditorToolUISystem>();
                if (editorToolUISystem.tools.Any(t => t.id == InspectObjectTool.ToolID))
                {
                    return;
                }
                var newTool = new InspectObjectTool(World);
                IEditorTool[] tools = editorToolUISystem.tools;
                Array.Resize(ref tools, tools.Length +1);
                tools[tools.Length - 1] = newTool;
                editorToolUISystem.tools = tools;
#endif
            } 
            else if (mode == GameMode.Game)
            {
                ToggleInputActions(true);
                World.GetExistingSystemManaged<InputGuiInteractionSystem>().Enabled = true;
            }
            else
            {
                ToggleInputActions(false);
                World.GetExistingSystemManaged<InputGuiInteractionSystem>().Enabled = false;
            }
        }

        protected override void OnDestroy() {
            _toggleExplorerAction.onInteraction -= OnToggleSceneExplorerTool;
            _toggleComponentSearchAction.onInteraction -= OnToggleComponentSearchWindow;
            _toggleExplorerAction = null;
            _toggleComponentSearchAction = null;
            
            if (UiManager)
            {
                Object.Destroy(UiManager.gameObject);
                UiManager = null;
            }
            
            if (_searchWindow)
            {
                Object.Destroy(_searchWindow);
                _searchWindow = null;
            }
            base.OnDestroy();
        }

        private void ToggleInputActions(bool activate)
        {
            _toggleExplorerAction.shouldBeEnabled = activate;
            _toggleComponentSearchAction.shouldBeEnabled = activate;
        }

        private void OnToggleSceneExplorerTool(ProxyAction proxyAction, InputActionPhase inputActionPhase)
        {
            if (inputActionPhase != InputActionPhase.Performed)
            {
                return;
            }
            
            World.GetExistingSystemManaged<InspectObjectToolSystem>().ChangeToolMode();
        }
        
        private void OnToggleComponentSearchWindow(ProxyAction proxyAction, InputActionPhase inputActionPhase)
        {
            if (inputActionPhase != InputActionPhase.Performed)
            {
                return;
            }
            
            if (!_searchWindow)
            {
                _searchWindow = new GameObject("SceneExplorer_ComponentSearch").AddComponent<ComponentSearch>();
                Object.DontDestroyOnLoad(_searchWindow.gameObject);
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
}
