

using System.Reflection;

namespace SceneExplorer
{
    using Colossal.IO.AssetDatabase;
    using Game;
    using Game.Common;
    using Game.Modding;
    using Game.SceneFlow;
    using JetBrains.Annotations;
    using SceneExplorer.System;
    using SceneExplorer.ToBeReplaced.Windows;

    [UsedImplicitly]
    public class ModEntryPoint : IMod
    {
        public static string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString(4);
        public static string InformationalVersion => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        
        internal static Settings _settings;

        public void OnLoad(UpdateSystem updateSystem)
        {
            Logging.Info("ModEntryPoint on OnLoad called!");
            _settings = new Settings(this);
            _settings.RegisterKeyBindings();
            _settings.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new Localization.LocaleEN(_settings));
            AssetDatabase.global.LoadSettings(nameof(SceneExplorer), _settings, new Settings(this));
            _settings.ApplyLoadedSettings();
            
            updateSystem.UpdateAt<SceneExplorerUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<InspectObjectToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAfter<InspectorTooltipSystem>(SystemUpdatePhase.UITooltip);
            updateSystem.UpdateBefore<InputGuiInteractionSystem, RaycastSystem>(SystemUpdatePhase.MainLoop); // initially disabled
        }

        public void OnDispose()
        {
            Logging.Info("ModEntryPoint on OnDispose called!");
            if (_settings != null)
            {
                _settings.UnregisterInOptionsUI();
                _settings = null;
            }
        }
    }
}
