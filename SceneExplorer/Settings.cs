using System;
using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SceneExplorer
{
    [FileLocation("SceneExplorer")]
    [SettingsUIKeyboardAction(ToggleToolAction, customUsages: new []{Usages.kDefaultUsage, Usages.kToolUsage})]
    [SettingsUIKeyboardAction(ToggleComponentSearchAction, customUsages: new []{Usages.kDefaultUsage, Usages.kToolUsage})]
    [SettingsUIKeyboardAction(MakeSnapshotAction, customUsages: new []{Usages.kToolUsage})]
    public class Settings : ModSetting
    {
        public const string Section = "Main";
        public const string AboutSection = "About";
        public const string KeybindingGroup = "KeyBinding";
        public const string ToggleToolAction = "ToggleToolAction";
        public const string ToggleComponentSearchAction = "ToggleComponentSearchAction";
        public const string MakeSnapshotAction = "MakeSnapshot";
        
        [SettingsUIKeyboardBinding(Key.E, ToggleToolAction, ctrl: true)]
        [SettingsUISection(Section, KeybindingGroup)]
        public ProxyBinding ToggleSceneExplorerTool { get; set; }
        
        [SettingsUIKeyboardBinding(Key.W, ToggleComponentSearchAction, ctrl: true)]
        [SettingsUISection(Section, KeybindingGroup)]
        public ProxyBinding ToggleComponentSearch { get; set; }
        
        [SettingsUIKeyboardBinding(Key.S, MakeSnapshotAction, ctrl: true)]
        [SettingsUISection(Section, KeybindingGroup)]
        public ProxyBinding MakeSnapshot { get; set; }
        
        public Settings(IMod mod) : base(mod) { }
        
        [SettingsUISection(Section, AboutSection)]
        public string ModVersion => ModEntryPoint.Version;
        
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
        
        public override void SetDefaults()
        {
            
        }
    }
}
