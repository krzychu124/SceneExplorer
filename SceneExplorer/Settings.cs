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
    [SettingsUIKeyboardAction(ToggleComponentSearchAction, customUsages: new []{Usages.kDefaultUsage, Usages.kToolUsage})]
    [SettingsUIKeyboardAction(MakeSnapshotAction, customUsages: new []{Usages.kToolUsage})]
    [SettingsUIMouseAction(ApplyToolAction, allowModifiers: false, usages: new []{"SceneExplorer.InspectObject"})]
    [SettingsUIMouseAction(CancelToolAction, allowModifiers: false, usages: new []{"SceneExplorer.InspectObject"})]
    public class Settings : ModSetting
    {
        public const string Section = "Main";
        public const string AboutSection = "About";
        public const string KeybindingGroup = "KeyBinding";
        public const string ApplyToolAction = "ApplyToolAction";
        public const string CancelToolAction = "CancelToolAction";
        public const string ToggleToolAction = "ToggleToolAction";
        public const string ToggleComponentSearchAction = "ToggleComponentSearchAction";
        public const string MakeSnapshotAction = "MakeSnapshot";
        private Dictionary<string, ProxyBinding.Watcher> _vanillaBindingWatchers;
  
        [SettingsUISection(Section, KeybindingGroup)]
        [SettingsUISetter(typeof(Settings), nameof(OnUseVanillaToolActionsSet))]
        public bool UseVanillaToolActions { get; set; }
        
        [SettingsUISection(Section, KeybindingGroup)]
        [SettingsUIMouseBinding(BindingMouse.Left, ApplyToolAction, ctrl: false)]
        [SettingsUIDisableByCondition(typeof(Settings), nameof(UseVanillaToolActions))]
        public ProxyBinding ApplyTool { get; set; }
        
        [SettingsUISection(Section, KeybindingGroup)]
        [SettingsUIMouseBinding(BindingMouse.Right, CancelToolAction, ctrl: false)]
        [SettingsUIDisableByCondition(typeof(Settings), nameof(UseVanillaToolActions))]
        public ProxyBinding CancelTool { get; set; }
        
        [SettingsUIKeyboardBinding(BindingKeyboard.E, ToggleToolAction, ctrl: true)]
        [SettingsUISection(Section, KeybindingGroup)]
        public ProxyBinding ToggleSceneExplorerTool { get; set; }
        
        [SettingsUIKeyboardBinding(BindingKeyboard.W, ToggleComponentSearchAction, ctrl: true)]
        [SettingsUISection(Section, KeybindingGroup)]
        public ProxyBinding ToggleComponentSearch { get; set; }
        
        [SettingsUIKeyboardBinding(BindingKeyboard.S, MakeSnapshotAction, ctrl: true)]
        [SettingsUISection(Section, KeybindingGroup)]
        public ProxyBinding MakeSnapshot { get; set; }
        
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
                if (UseVanillaToolActions)
                {
                    DisposeToolActionWatchers();
                    RegisterToolActionWatchers();
                }
            }
        }

        public Settings(IMod mod) : base(mod)
        {
            _vanillaBindingWatchers = new Dictionary<string, ProxyBinding.Watcher>();
            SetDefaults();
        }
        
        public sealed override void SetDefaults()
        {
            UseVanillaToolActions = true;
        }
        
        private void OnUseVanillaToolActionsSet(bool value)
        {
            if (value)
            {
                RegisterToolActionWatchers();
            }
            else
            {
                DisposeToolActionWatchers();
            }
        }

        
        private void RegisterToolActionWatchers()
        {
            ProxyAction builtInApplyAction =  InputManager.instance.FindAction(InputManager.kToolMap, "Apply");
            ProxyBinding.Watcher applyWatcher = MimicVanillaAction(builtInApplyAction, GetAction(ApplyToolAction), "Mouse");
            if (_vanillaBindingWatchers.TryGetValue("Apply_Mouse", out ProxyBinding.Watcher oldApplyWatcher))
            {
                oldApplyWatcher.Dispose();
                _vanillaBindingWatchers.Remove("Apply_Mouse");
            }
            _vanillaBindingWatchers.Add("Apply_Mouse", applyWatcher);
            
            ProxyAction builtInCancelAction = InputManager.instance.FindAction(InputManager.kToolMap, "Mouse Cancel");
            ProxyBinding.Watcher cancelWatcher = MimicVanillaAction(builtInCancelAction, GetAction(CancelToolAction), "Mouse");
            if (_vanillaBindingWatchers.TryGetValue("Cancel_Mouse", out ProxyBinding.Watcher oldCancelWatcher))
            {
                oldCancelWatcher.Dispose();
                _vanillaBindingWatchers.Remove("Cancel_Mouse");
            }
            _vanillaBindingWatchers.Add("Cancel_Mouse", cancelWatcher);
        }
        
        private void DisposeToolActionWatchers()
        {
            if (_vanillaBindingWatchers.TryGetValue("Apply_Mouse", out ProxyBinding.Watcher applyWatcher))
            {
                applyWatcher.Dispose();
                _vanillaBindingWatchers.Remove("Apply_Mouse");
            }
            
            if (_vanillaBindingWatchers.TryGetValue("Cancel_Mouse", out ProxyBinding.Watcher cancelWatcher))
            {
                cancelWatcher.Dispose();
                _vanillaBindingWatchers.Remove("Cancel_Mouse");
            }
        }
        
        private ProxyBinding.Watcher MimicVanillaAction(ProxyAction vanillaAction, ProxyAction customAction, string actionGroup)
        {
            ProxyBinding customActionBinding = customAction.bindings.FirstOrDefault(b => b.group == actionGroup);
            ProxyBinding vanillaActionBinding = vanillaAction.bindings.FirstOrDefault(b => b.group == actionGroup);
            ProxyBinding.Watcher actionWatcher = new ProxyBinding.Watcher(vanillaActionBinding, binding => SetMimic(customActionBinding, binding));
            SetMimic(customActionBinding, actionWatcher.binding);
            return actionWatcher;
        }
        
        private void SetMimic(ProxyBinding mimic, ProxyBinding buildIn)
        {
            var newMimicBinding = mimic.Copy();
            newMimicBinding.path = buildIn.path;
            newMimicBinding.modifiers = buildIn.modifiers;
            InputManager.instance.SetBinding(newMimicBinding, out _);
        }
        
        internal void ApplyLoadedSettings()
        {
            if (UseVanillaToolActions)
            {
                RegisterToolActionWatchers();
            }
        }
    }
}
