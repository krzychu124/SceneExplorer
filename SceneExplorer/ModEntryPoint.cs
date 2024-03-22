using Game;
using Game.Common;
using Game.Modding;
using JetBrains.Annotations;
using SceneExplorer.System;
using SceneExplorer.ToBeReplaced.Windows;

namespace SceneExplorer
{
    [UsedImplicitly]
    public class ModEntryPoint: IMod
    {
        public void OnLoad(UpdateSystem updateSystem) {
            Logging.Info("ModEntryPoint on OnLoad called!");
            updateSystem.UpdateAt<SceneExplorerUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<InspectObjectToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAfter<InspectorTooltipSystem>(SystemUpdatePhase.UITooltip);
            updateSystem.UpdateAt<InGameKeyListener>(SystemUpdatePhase.LateUpdate);                          // initially disabled
            updateSystem.UpdateBefore<InputGuiInteractionSystem, RaycastSystem>(SystemUpdatePhase.MainLoop); // initially disabled
        }

        public void OnDispose() {
            Logging.Info("ModEntryPoint on OnDispose called!");
        }
    }
}
