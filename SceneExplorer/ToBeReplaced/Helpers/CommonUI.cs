using System;
using System.Collections.Generic;
using Game.Debug;
using Game.Prefabs;
using SceneExplorer.ToBeReplaced.Windows;
using Unity.Entities;
using UnityEngine;

namespace SceneExplorer.ToBeReplaced.Helpers
{
    public static class CommonUI
    {
        private static GUILayoutOption[] _expandButtonOptions = new GUILayoutOption[] { GUILayout.MaxWidth(21), GUILayout.MaxHeight(22) };
        private static GUILayoutOption[] _lineOptions = new[] { GUILayout.ExpandWidth(true), GUILayout.Height(1f) };
        private static GUILayoutOption[] _paginationButton = new GUILayoutOption[] { GUILayout.MinWidth(60), GUILayout.MaxWidth(60), GUILayout.MaxHeight(22) };

        public static GUILayoutOption[] ExpandButtonOptions => _expandButtonOptions;

        public static void DrawLine()
        {
            GUILayout.Box(GUIContent.none, UIStyle.Instance.line, _lineOptions);
        }

        public static bool ButtonExpand(bool expanded)
        {
            return GUILayout.Button(expanded ? "▼" : "▶", UIStyle.Instance.iconButton, _expandButtonOptions);
        }

        public static bool CollapsibleHeader(string text, bool expanded, Rect rect, out bool hovered, ButtonLocation location = ButtonLocation.Start, string prefix = null, GUIStyle textStyle = null, GUIStyle style = null, bool focused = false)
        {
            bool clicked;
            hovered = false;
            GUILayout.BeginHorizontal(style: style ?? GUIStyle.none);
            textStyle ??= GUIStyle.none;
            var tempTextStyle = textStyle.fontStyle;
            textStyle.fontStyle = focused ? FontStyle.Bold : tempTextStyle;
            switch (location)
            {
                case ButtonLocation.Start:
                    clicked = GUILayout.Button(expanded ? "▼" : "▶", UIStyle.Instance.iconButton, _expandButtonOptions);
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        GUILayout.Label(prefix, textStyle, options: null);
                    }

                    GUILayout.Label(text, textStyle, options: null);
                    GUILayout.FlexibleSpace();
                    break;

                case ButtonLocation.AfterTitle:
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        GUILayout.Label(prefix, textStyle, options: null);
                    }
                    GUILayout.Label(text, textStyle, options: null);
                    clicked = GUILayout.Button(expanded ? "▼" : "▶", UIStyle.Instance.iconButton, _expandButtonOptions);
                    GUILayout.FlexibleSpace();
                    break;

                case ButtonLocation.End:
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        GUILayout.Label(prefix, textStyle, options: null);
                    }
                    GUILayout.Label(text, textStyle, options: null);
                    GUILayout.FlexibleSpace();
                    clicked = GUILayout.Button(expanded ? "▼" : "▶", UIStyle.Instance.iconButton, _expandButtonOptions);
                    break;

                case ButtonLocation.EndCenteredText:
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        GUILayout.Label(prefix, textStyle, options: null);
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(text, textStyle, options: null);
                    GUILayout.FlexibleSpace();
                    clicked = GUILayout.Button(expanded ? "▼" : "▶", UIStyle.Instance.iconButton, _expandButtonOptions);
                    break;
                default:
                    textStyle.fontStyle = tempTextStyle;
                    throw new ArgumentOutOfRangeException(nameof(location), location, null);
            }

            textStyle.fontStyle = tempTextStyle;

            GUILayout.EndHorizontal();

            if (ComponentDataRenderer.WasHovered(rect))
            {
                hovered = true;
            }
            return clicked;
        }

        public static void CollapsibleList(bool expanded, List<ISectionItem> items, int leftMargin = 10, bool drawSeparator = false, GUIStyle style = null)
        {
            if (expanded)
            {
                GUILayout.BeginHorizontal(style ?? UIStyle.Instance.collapsibleContentStyle, options: null);

                GUILayout.Space(leftMargin);
                GUILayout.BeginVertical(options: null);

                GUILayout.Space(5);
                for (var i = 0; i < items.Count; i++)
                {
                    items[i].Render();
                    if (drawSeparator && i < items.Count - 1)
                    {
                        DrawLine();
                    }
                }

                GUILayout.Space(5);
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }
        }

        public static void CollapsibleList(bool expanded, List<ISection> items, int leftMargin = 10, bool drawSeparator = false, GUIStyle style = null)
        {
            if (expanded)
            {
                GUILayout.BeginHorizontal(style ?? UIStyle.Instance.collapsibleContentStyle, options: null);

                GUILayout.Space(leftMargin);
                GUILayout.BeginVertical(options: null);

                GUILayout.Space(5);
                for (var i = 0; i < items.Count; i++)
                {
                    items[i].Render();
                    if (drawSeparator && i < items.Count - 1)
                    {
                        DrawLine();
                    }
                }

                GUILayout.Space(5);
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }
        }


        public static GUIStyle CalculateTextStyle(string name, bool highlight)
        {
            switch (name)
            {
                case nameof(PrefabRef):
                case nameof(PrefabData):
                    return !highlight ? UIStyle.Instance.focusedReducedPaddingLabelStyle : UIStyle.Instance.focusedReducedPaddingHighlightedLabelStyle;
                default:
                    return !highlight ? UIStyle.Instance.reducedPaddingLabelStyle : UIStyle.Instance.reducedPaddingHighlightedLabelStyle;
            }
        }

        public static void ListHeader<T>(int first, int last, ref Pagination<T> pagination)
        {
            GUILayout.BeginHorizontal(UIStyle.Instance.collapsibleContentStyle, options: null);
            GUILayout.Space(8);

            GUILayout.Label("Items", UIStyle.Instance.paginationLabelStyle, options: null);
            GUILayout.FlexibleSpace();
            GUILayout.Label($" {first} - {last} of {pagination.ItemCount} ", UIStyle.Instance.paginationLabelStyle, options: null);
            GUI.enabled = pagination.CurrentPage > 1;
            if (GUILayout.Button(" ◀ ", UIStyle.Instance.iconButton, _paginationButton))
            {
                pagination.PreviousPage();
            }
            GUI.enabled = pagination.CurrentPage < pagination.PageCount;
            if (GUILayout.Button(" ▶ ", UIStyle.Instance.iconButton, _paginationButton))
            {
                pagination.NextPage();
            }
            GUI.enabled = true;

            GUILayout.EndHorizontal();
        }

        public static void QueryCreatorSection(QueryCreator creator, LocalState allString, LocalState anyString, LocalState noneString, Rect windowPos, int ownerWindowId)
        {
            GUILayout.Label("EntityQuery:", UIStyle.Instance.reducedPaddingHighlightedLabelStyle, options: null);
            GUILayout.BeginHorizontal(UIStyle.Instance.collapsibleContentStyle, options: null);
            GUILayout.Space(6);
            GUILayout.BeginVertical(options: null);
            {
                GUILayout.BeginHorizontal(options: null);
                GUILayout.Label("With All", options: null);
                GUILayout.FlexibleSpace();
                DropdownControl.DrawButton("Add", UIStyle.Instance.iconButton, allString, (i, stateAny) => OnAddItem(creator, QueryCreator.MatchingType.WithAll, stateAny, i), windowPos, ownerWindowId);
                GUILayout.EndHorizontal();

                RenderQueryItems(creator, creator.WithAll, QueryCreator.MatchingType.WithAll);
                DrawLine();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(UIStyle.Instance.collapsibleContentStyle, options: null);
            GUILayout.Space(6);
            GUILayout.BeginVertical(options: null);
            {
                GUILayout.BeginHorizontal(options: null);
                GUILayout.Label("With Any", options: null);
                GUILayout.FlexibleSpace();
                DropdownControl.DrawButton("Add", UIStyle.Instance.iconButton, anyString, (i, stateAny) => OnAddItem(creator, QueryCreator.MatchingType.WithAny, stateAny, i), windowPos, ownerWindowId);
                GUILayout.EndHorizontal();

                RenderQueryItems(creator, creator.WithAny, QueryCreator.MatchingType.WithAny);
                DrawLine();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(UIStyle.Instance.collapsibleContentStyle, options: null);
            GUILayout.Space(6);
            GUILayout.BeginVertical(options: null);
            GUILayout.BeginHorizontal(options: null);
            {
                GUILayout.Label("With None", options: null);
                GUILayout.FlexibleSpace();
                DropdownControl.DrawButton("Add", UIStyle.Instance.iconButton, noneString, (i, stateAny) => OnAddItem(creator, QueryCreator.MatchingType.WithNone, stateAny, i), windowPos, ownerWindowId);
                GUILayout.EndHorizontal();

                RenderQueryItems(creator, creator.WithNone, QueryCreator.MatchingType.WithNone);
                DrawLine();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private static void RenderQueryItems(QueryCreator creator, HashSet<ComponentType>.Enumerator enumerator, QueryCreator.MatchingType type)
        {

            using (HashSet<ComponentType>.Enumerator creatorWithAll = enumerator)
            {
                while (creatorWithAll.MoveNext())
                {
                    GUILayout.BeginHorizontal(options: null);
                    if (GUILayout.Button(creatorWithAll.Current.GetManagedType().FullName, UIStyle.Instance.iconButton, options: null))
                    {
                        creator.RemoveDeferred(creatorWithAll.Current, type);
                    }
                    GUILayout.Space(40);
                    GUILayout.EndHorizontal();
                }
            }
        }

        private static bool OnAddItem(QueryCreator creator, QueryCreator.MatchingType type, LocalState state, int i)
        {
            string temp = state.value;
            state.value = GUILayout.TextField(state.value, options: null);
            if (temp != state.value)
            {
                creator.FilterItems(state.value, type);
            }
            state.scrollPos = GUILayout.BeginScrollView(state.scrollPos, options: null);
            bool result = false;
            foreach (KeyValuePair<string, TypeIndex> item in creator.Items)
            {
                if (GUILayout.Button(item.Key, UIStyle.Instance.iconButton))
                {
                    state.Reset();
                    result = creator.AddDeferred(ComponentType.FromTypeIndex(item.Value), type);
                }
            }
            GUILayout.EndScrollView();
            return result;
        }

        public static GUIStyle CalculateTextStyle(SpecialComponentType type, bool highlight)
        {
            switch (type)
            {
                case SpecialComponentType.PrefabRef:
                case SpecialComponentType.PrefabData:
                    return !highlight ? UIStyle.Instance.focusedReducedPaddingLabelStyle : UIStyle.Instance.focusedReducedPaddingHighlightedLabelStyle;
                case SpecialComponentType.Managed:
                    return !highlight ? UIStyle.Instance.managedLabelStyle : UIStyle.Instance.managedHighlightedLabelStyle;
                case SpecialComponentType.UnManaged:
                    return !highlight ? UIStyle.Instance.unManagedLabelStyle : UIStyle.Instance.unManagedHighlightedLabelStyle;
                case SpecialComponentType.Buffer:
                    return !highlight ? UIStyle.Instance.bufferLabelStyle : UIStyle.Instance.bufferHighlightedLabelStyle;
                case SpecialComponentType.Shared:
                    return !highlight ? UIStyle.Instance.sharedLabelStyle : UIStyle.Instance.sharedHighlightedLabelStyle;
                case SpecialComponentType.Unknown:
                    return !highlight ? UIStyle.Instance.unknownLabelStyle : UIStyle.Instance.unknownHighlightedLabelStyle;
                default:
                    return !highlight ? UIStyle.Instance.reducedPaddingLabelStyle : UIStyle.Instance.reducedPaddingHighlightedLabelStyle;
            }
        }


        public enum ButtonLocation
        {
            Start,
            AfterTitle,
            EndCenteredText,
            End,
        }

        public class LocalState
        {
            public string value = string.Empty;
            public Vector2 scrollPos = Vector2.zero;

            public void Reset()
            {
                value = string.Empty;
                scrollPos = Vector2.zero;
            }
        }
    }
}
