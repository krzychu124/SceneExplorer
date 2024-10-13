using System;
using System.Collections.Generic;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using UnityEngine;

namespace SceneExplorer.ToBeReplaced.Helpers.ContentItems
{
    public class ObjectItem : ISectionItem
    {
        private static GUILayoutOption[] _expandButtonOptions = new GUILayoutOption[] { GUILayout.MaxWidth(21), GUILayout.MaxHeight(22) };
        private readonly string _title;
        private List<ISectionItem> _items;
        private bool _expanded;

        public ObjectItem(string title, int index, List<ISectionItem> items, string prefabName = null)
        {
            _title = !string.IsNullOrEmpty(prefabName) ? $"{title} [{index}] (\"{prefabName}]\")" : $"{title} [{index}]";
            _items = items;
        }

        public ObjectItem(string title, List<ISectionItem> items, string prefabName = null)
        {
            _title = !string.IsNullOrEmpty(prefabName) ? $"{title} (\"{prefabName}\")" : $"{title}: ";
            _items = items;
        }

        public void Render()
        {
            bool prevExpanded = _expanded;

            GUI.enabled = _items.Count > 0;
            if (CommonUI.CollapsibleHeader($"{_title} ", _expanded, Rect.zero, out bool _, location: CommonUI.ButtonLocation.AfterTitle, textStyle: !_expanded ? UIStyle.Instance.reducedPaddingLabelStyle : UIStyle.Instance.reducedPaddingHighlightedLabelStyle))
            {
                _expanded = !_expanded;
            }
            GUI.enabled = true;
            CommonUI.CollapsibleList(prevExpanded, _items, 8, drawSeparator: true);
        }

        public static void PrepareItems(Type type, object value, string sectionName, List<ISectionItem> items, int depth)
        {
            Logging.DebugEvaluation($"PrepareItems [{depth}]: {sectionName} ({type.Name}), {value}");
            if (depth >= UIGenerator.MAX_GENERATOR_DEPTH)
            {
                return;
            }
            Logging.DebugEvaluation($"Depth OK {depth}");
            foreach (FieldInfo fieldInfo in type.GetRuntimeFields())
            {
                if (fieldInfo.IsStatic)
                {
                    continue;
                }

                items.Add(UIGenerator.GetSectionItem(fieldInfo.Name, fieldInfo.GetValue(value), sectionName, depth));
            }
            Logging.DebugEvaluation($"PrepareItems [{depth}]: {sectionName} ({type.Name}), {value}, generated: {items.Count} items");
        }
    }
}
