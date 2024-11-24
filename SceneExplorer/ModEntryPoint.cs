using Colossal.IO.AssetDatabase;

using Game;
using Game.Common;
using Game.Modding;
using Game.SceneFlow;

using JetBrains.Annotations;

using SceneExplorer.System;

using System.Reflection;


namespace SceneExplorer
{
    [UsedImplicitly]
    public class ModEntryPoint : IMod
    {
        public static string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString(4);
        public static string InformationalVersion => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        
        internal static Settings Settings;

        public void OnLoad(UpdateSystem updateSystem)
        {
            Logging.Info("ModEntryPoint on OnLoad called!");
            Settings = new Settings(this);
            Settings.RegisterKeyBindings();
            Settings.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new Localization.LocaleEN(Settings));
            AssetDatabase.global.LoadSettings(nameof(SceneExplorer), Settings, new Settings(this));
            Settings.ApplyLoadedSettings();
            
            updateSystem.UpdateAt<SceneExplorerUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<InspectObjectToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAfter<InspectorTooltipSystem>(SystemUpdatePhase.UITooltip);
            updateSystem.UpdateBefore<InputGuiInteractionSystem, RaycastSystem>(SystemUpdatePhase.MainLoop); // initially disabled
        }

        public void OnDispose()
        {
            Logging.Info("ModEntryPoint on OnDispose called!");
            if (Settings != null)
            {
                Settings.UnregisterInOptionsUI();
                Settings = null;
            }
        }
    }
}
