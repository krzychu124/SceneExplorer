using Colossal.IO.AssetDatabase;

using Game.Input;
using Game.Modding;
using Game.Settings;
using Game.UI;

using SceneExplorer.ToBeReplaced.Helpers;

using System;

using UnityEngine;

namespace SceneExplorer
{
    [FileLocation("SceneExplorer")]
    [SettingsUIKeyboardAction(ToggleToolAction, customUsages: new []{Usages.kDefaultUsage, Usages.kToolUsage})]
    [SettingsUIKeyboardAction(ChangeToolModeAction, customUsages: new []{Usages.kDefaultUsage, Usages.kToolUsage})]
    [SettingsUIKeyboardAction(ToggleComponentSearchAction, customUsages: new []{Usages.kDefaultUsage, Usages.kToolUsage})]
    [SettingsUIKeyboardAction(MakeSnapshotAction, customUsages: new []{Usages.kToolUsage})]
    [SettingsUIGroupOrder(KeybindingGroup, UIGroup, OtherSection, AboutSection)]
    [SettingsUIShowGroupName(KeybindingGroup, UIGroup, OtherSection, AboutSection)]
    public class Settings : ModSetting
    {
        public const string Section = "Main";
        public const string OtherSection = "Other";
        public const string AboutSection = "About";
        public const string KeybindingGroup = "KeyBinding";
        public const string UIGroup = "UISettings";
        public const string ToggleToolAction = "ToggleToolAction";
        public const string ChangeToolModeAction = "ChangeToolModeAction";
        public const string ToggleComponentSearchAction = "ToggleComponentSearchAction";
        public const string MakeSnapshotAction = "MakeSnapshot";
        private string _switchToolModeKeybindingName = string.Empty;
        private float _uiScalingValue;

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

        [SettingsUISlider(min = 0.5f, max = 3.0f, step = 0.1f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(Section, UIGroup)]
        public float UIScalingSlider 
        { 
            get
            {
                return _uiScalingValue;
            }

            set
            {
                _uiScalingValue = value;

                // Reset the cached UIStyle
                UIStyle.ResetStaticInstance();
            }
        }

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
            UIScalingSlider = 1.0f;
        }

        public int CalculateUIScaledValue(int value)
        {
            return (int)(UIScalingSlider * value);
        }

        public uint CalculateUIScaledValue(uint value)
        {
            return (uint)(UIScalingSlider * value);
        }

        public float CalculateUIScaledValue(float value)
        {
            return UIScalingSlider * value;
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
