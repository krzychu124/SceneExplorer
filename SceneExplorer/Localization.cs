using System.Collections.Generic;
using Colossal;
using SceneExplorer.Tools;

namespace SceneExplorer
{
    public static class Localization
    {
        public class LocaleEN : IDictionarySource
        {
            private readonly Settings _settings;

            public LocaleEN(Settings settings)
            {
                _settings = settings;
            }

            public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
            {
                return new Dictionary<string, string>
                {
                    { _settings.GetSettingsLocaleID(), "Scene Explorer" },
                    { _settings.GetOptionTabLocaleID(Settings.Section), "Main" },
                    { _settings.GetOptionGroupLocaleID(Settings.KeybindingGroup), "Key bindings" },
                    { _settings.GetOptionGroupLocaleID(Settings.OtherSection), "Other" },
                    { _settings.GetOptionGroupLocaleID(Settings.AboutSection), "About" },
                    
                    { _settings.GetOptionLabelLocaleID(nameof(Settings.ToggleSceneExplorerTool)), "Toggle Scene Explorer tool" },
                    { _settings.GetOptionDescLocaleID(nameof(Settings.ToggleSceneExplorerTool)), "Toggles Scene Explorer tool in-game or Editor" },
                    { _settings.GetOptionLabelLocaleID(nameof(Settings.ChangeSceneExplorerToolMode)), "Change Inspect Mode of Scene Explorer tool" },
                    { _settings.GetOptionDescLocaleID(nameof(Settings.ChangeSceneExplorerToolMode)), "Changes the inspection mode of Scene Explorer tool in-game or Editor" },
                    { _settings.GetOptionLabelLocaleID(nameof(Settings.ToggleComponentSearch)), "Toggle ECS Component Search window" },
                    { _settings.GetOptionDescLocaleID(nameof(Settings.ToggleComponentSearch)), "Toggle window to search vanilla or modded ECS components by their name" },
                    { _settings.GetOptionLabelLocaleID(nameof(Settings.MakeSnapshot)), "Make Snapshot" },
                    { _settings.GetOptionDescLocaleID(nameof(Settings.MakeSnapshot)), "Triggers the snapshot generation of all current results from ECS Component Search - Entities by EntityQuery window.\nAvailable only in the Editor" },
                    { _settings.GetOptionLabelLocaleID(nameof(Settings.ResetBindings)), "Reset key bindings" },
                    { _settings.GetOptionDescLocaleID(nameof(Settings.ResetBindings)), "" },
                    
                    { _settings.GetBindingKeyLocaleID(Settings.ToggleToolAction), "Toggle Scene Explorer Tool" },
                    { _settings.GetBindingKeyLocaleID(Settings.ToggleComponentSearchAction), "Toggle ECS Component Search window" },
                    { _settings.GetBindingKeyLocaleID(Settings.MakeSnapshotAction), "Make Snapshot" },
                    
                    //Other
                    { _settings.GetOptionLabelLocaleID(nameof(Settings.UseShortComponentNames)), "Use short component names" },
                    { _settings.GetOptionDescLocaleID(nameof(Settings.UseShortComponentNames)), "When enabled, the inspector will display component names in short form instead of full with namespace. Reopen window to update names after changing this option" },
                    
                    //About
                    { _settings.GetOptionLabelLocaleID(nameof(Settings.ModVersion)), "Version" },
                    { _settings.GetOptionDescLocaleID(nameof(Settings.ModVersion)), "Mod current version" },
                    
                    { _settings.GetOptionLabelLocaleID(nameof(Settings.InformationalVersion)), "Informational Version" },
                    { _settings.GetOptionDescLocaleID(nameof(Settings.InformationalVersion)), "Mod version with the commit ID" },
                    
                    { _settings.GetOptionLabelLocaleID(nameof(Settings.OpenRepositoryAtVersion)), "Show on GitHub" },
                    { _settings.GetOptionDescLocaleID(nameof(Settings.OpenRepositoryAtVersion)), "Opens the mod GitHub repository for the current version" },
                    
                    { _settings.GetBindingMapLocaleID(), "Scene Explorer Settings" },
                    
                    //other
                    {$"Editor.TOOL[{InspectObjectTool.ToolID}]", "Scene Explorer - Inspect Object"}
                };
            }
            
            public void Unload() { }
        }
    }
}
