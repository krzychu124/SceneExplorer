using Game;
using Game.Common;
using Game.Modding;
using SceneExplorer.System;
using SceneExplorer.ToBeReplaced.Windows;

namespace SceneExplorer
{
    public class ModEntryPoint: IMod
    {
        
        public void OnLoad() {
        }

        public void OnLoad(UpdateSystem updateSystem) {
            Logging.Info("ModEntryPoint on OnLoad called!");
            updateSystem.UpdateAt<SceneExplorerUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<InspectObjectToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAfter<InspectorTooltipSystem>(SystemUpdatePhase.UITooltip);
            updateSystem.UpdateAt<InGameKeyListener>(SystemUpdatePhase.LateUpdate);                          // initially disabled
            updateSystem.UpdateBefore<InputGuiInteractionSystem, RaycastSystem>(SystemUpdatePhase.MainLoop); // initially disabled
#if DEBUG2
            // updateSystem.UpdateAt<ExperimentsUISystem>(SystemUpdatePhase.UIUpdate);
#endif
        }

        public void OnDispose() {
            Logging.Info("ModEntryPoint on OnDispose called!");
        }
    }
}
