using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;
using UnityEngine;

namespace SceneExplorer
{
    [FileLocation("SceneExplorer")]
    [SettingsUIKeyboardAction(ToggleToolAction, customUsages: new []{Usages.kDefaultUsage, Usages.kToolUsage})]
    [SettingsUIKeyboardAction(ChangeToolModeAction, customUsages: new []{Usages.kDefaultUsage, Usages.kToolUsage})]
    [SettingsUIKeyboardAction(ToggleComponentSearchAction, customUsages: new []{Usages.kDefaultUsage, Usages.kToolUsage})]
    [SettingsUIKeyboardAction(MakeSnapshotAction, customUsages: new []{Usages.kToolUsage})]
    [SettingsUIGroupOrder(KeybindingGroup, OtherSection, AboutSection)]
    [SettingsUIShowGroupName(KeybindingGroup, OtherSection, AboutSection)]
    public class Settings : ModSetting
    {
        public const string Section = "Main";
        public const string OtherSection = "Other";
        public const string AboutSection = "About";
        public const string KeybindingGroup = "KeyBinding";
        public const string ToggleToolAction = "ToggleToolAction";
        public const string ChangeToolModeAction = "ChangeToolModeAction";
        public const string ToggleComponentSearchAction = "ToggleComponentSearchAction";
        public const string MakeSnapshotAction = "MakeSnapshot";
        private string _switchToolModeKeybindingName = string.Empty;

        [SettingsUIHidden]
        internal string SwitchToolModeKeybind => _switchToolModeKeybindingName;
        
        [SettingsUIKeyboardBinding(BindingKeyboard.E, ToggleToolAction, ctrl: true)]
        [SettingsUISection(Section, KeybindingGroup)]
        public ProxyBinding ToggleSceneExplorerTool { get; set; }
        
        [SettingsUIKeyboardBinding(BindingKeyboard.D, ChangeToolModeAction, ctrl: true)]
        [SettingsUISection(Section, KeybindingGroup)]
        public ProxyBinding ChangeSceneExplorerToolMode { get; set; }
        
        [SettingsUIKeyboardBinding(BindingKeyboard.W, ToggleComponentSearchAction, ctrl: true)]
        [SettingsUISection(Section, KeybindingGroup)]
        public ProxyBinding ToggleComponentSearch { get; set; }
        
        [SettingsUIKeyboardBinding(BindingKeyboard.S, MakeSnapshotAction, ctrl: true)]
        [SettingsUISection(Section, KeybindingGroup)]
        public ProxyBinding MakeSnapshot { get; set; }
        
        [SettingsUISection(Section, AboutSection)]
        public string ModVersion => ModEntryPoint.Version;
        
        [SettingsUISection(Section, OtherSection)]
        public bool UseShortComponentNames { get; set; }
        
        [SettingsUISection(Section, AboutSection)]
        public string InformationalVersion => ModEntryPoint.InformationalVersion;

        [SettingsUISection(Section, AboutSection)]
        public bool OpenRepositoryAtVersion
        {
            set {
                try
                {
                    Application.OpenURL($"https://github.com/krzychu124/SceneExplorer/commit/{ModEntryPoint.InformationalVersion.Split('+')[1]}");
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }
        }
        
        [SettingsUISection(Section, KeybindingGroup)]
        public bool ResetBindings
        {
            set {
                Logging.Info("Reset key bindings");
                ResetKeyBindings();
            }
        }

        public Settings(IMod mod) : base(mod)
        {
            SetDefaults();
        }
        
        public sealed override void SetDefaults()
        {
            UseShortComponentNames = false;
        }
        
        internal void ApplyLoadedSettings()
        {
            UpdateKeybindingString(this);
            onSettingsApplied -= UpdateKeybindingString;
            onSettingsApplied += UpdateKeybindingString;
        }

        private void UpdateKeybindingString(Setting setting)
        {
            _switchToolModeKeybindingName = string.Join("+", ChangeSceneExplorerToolMode.ToHumanReadablePath());
        }
    }
}
