#if DEBUG_PP

#if DEBUG_PP
using SceneExplorer.System;
using SceneExplorer.Tools;
#endif
using System;
using System.Linq;
using Colossal.Serialization.Entities;
using Game;
using Game.Prefabs;
using Game.UI;
using Game.UI.Editor;
using SceneExplorer.ToBeReplaced;
using SceneExplorer.ToBeReplaced.Windows;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SceneExplorer
{
    public partial class SceneExplorerUISystem : UISystemBase
    {
        public UIManager UiManager;
        public PrefabSystem _prefabSystem;
        private InGameKeyListener _keyListener;
        
        public override GameMode gameMode
        {
            get { return GameMode.GameOrEditor; }
        }   
        
        protected override void OnCreate() {
            base.OnCreate();
            Logging.Info($"OnCreate in {nameof(SceneExplorerUISystem)}");
            
            UiManager = new GameObject("SceneExplorer GUI").AddComponent<UIManager>();
            Object.DontDestroyOnLoad(UiManager.gameObject);

            _prefabSystem = World.GetExistingSystemManaged<PrefabSystem>();
            Logging.Info("Done!");
        }

        protected override void OnGamePreload(Purpose purpose, GameMode mode) {
            base.OnGamePreload(purpose, mode);
            if (mode == GameMode.Editor)
            {
                World.GetExistingSystemManaged<InGameKeyListener>().Enabled = true;
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
                World.GetExistingSystemManaged<InGameKeyListener>().Enabled = true;
                World.GetExistingSystemManaged<InputGuiInteractionSystem>().Enabled = true;
            }
            else
            {
                World.GetExistingSystemManaged<InGameKeyListener>().Enabled = false;
                World.GetExistingSystemManaged<InputGuiInteractionSystem>().Enabled = false;
            }
        }

        protected override void OnUpdate() {
            base.OnUpdate();
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            
            if (UiManager)
            {
                Object.Destroy(UiManager.gameObject);
                UiManager = null;
            }
        }
    }
}

#endif