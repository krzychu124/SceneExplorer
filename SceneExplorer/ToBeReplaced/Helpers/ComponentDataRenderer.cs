using SceneExplorer.System;
using SceneExplorer.ToBeReplaced.Windows;
using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SceneExplorer.ToBeReplaced.Helpers
{
    public class ComponentDataRenderer : IRenderer
    {
        private static GUILayoutOption[] _paginationButton = new GUILayoutOption[] { GUILayout.MinWidth(60), GUILayout.MaxWidth(60), GUILayout.MaxHeight(22) };
        private InspectableObjectRenderer _objectRenderer;
        private IValueInspector _inspector;
        public static HoverData lastHovered;
#if DEBUG_PP
        private InspectObjectToolSystem _toolSystem;
#endif
        private bool _lastRendered;

#if DEBUG_PP
        public ComponentDataRenderer(IValueInspector inspector, InspectObjectToolSystem inspectObjectToolSystem)
        {
            _objectRenderer = new InspectableObjectRenderer();
            _inspector = inspector;
            _toolSystem = inspectObjectToolSystem;
        }
#endif

        public void Render(IEntityTagComponent component, Entity entity)
        {
            GUILayout.BeginHorizontal(options: null);
            GUI.enabled = false;
            GUILayout.Button("•", UIStyle.Instance.iconButton, options: CommonUI.ExpandButtonOptions);
            GUI.enabled = true;
            GUILayout.Label(GetTypeInfo(component.Type, component.Name, component.IsSnapshot), UIStyle.Instance.reducedPaddingLabelStyle, options: null);
            GUILayout.EndHorizontal();
            _lastRendered = false;
        }

        public void Render(IEntityComponent component, Entity entity, Rect rect)
        {
            string name = GetComponentName(component);
            if (CommonUI.CollapsibleHeader(name, component.DetailedView, rect, out bool titleHovered, CommonUI.ButtonLocation.Start,
                textStyle: CommonUI.CalculateTextStyle(component.SpecialType, component.DetailedView)))
            {
                if (component.DetailedView)
                {
                    component.HideDetails();
                }
                else
                {
                    component.ShowDetails();
                }
            }
            if (titleHovered)
            {
                var c = component as ComponentInfoBase;
                lastHovered = new HoverData() { entity = entity, DataType = HoverData.HoverType.Component, ComponentType = c.Type };
            }
            if (component.DetailedView)
            {
                GUILayout.BeginHorizontal(UIStyle.Instance.collapsibleContentStyle, options: null);

                GUILayout.Space(8);
                GUILayout.BeginVertical(options: null);

                GUILayout.Space(5);
                for (var i = 0; i < component.DataFields?.Count; i++)
                {
                    // var field = component.Fields[i];
                    if (component.Objects?.Count > i)
                    {
                        _objectRenderer.Render(component.Objects[i], _inspector, -1, rect, out var hoveredObject);
                        if (hoveredObject != null)
                        {
                            var c = component as ComponentInfoBase;
                            lastHovered = new HoverData() { entity = entity, DataType = HoverData.HoverType.ComponentItem, ComponentType = c.Type, ComponentItem = hoveredObject, Index = i };
                        }
                        if (i < component.DataFields.Count - 1)
                        {
                            CommonUI.DrawLine();
                        }
                    }
                }

                GUILayout.Space(5);
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }
            _lastRendered = true;
        }

        private static string GetComponentName(IEntityComponent component)
        {
            if (component == null) return "NULL!";
            return component switch
            {
                PrefabRefComponentInfo r => GetTypeInfo(component.Type, component.Name) + $" - {r.PrefabRefDataName}",
                PrefabDataComponentInfo d => GetTypeInfo(component.Type, component.Name) + $" - {d.PrefabDataName}",
                _ => GetTypeInfo(component.Type, component.Name, component.IsSnapshot)
            };
        }

        public void Render(IEntityBufferComponent component, Entity entity, Rect rect)
        {
            GUI.enabled = component.ItemCount > 0;
            if (CommonUI.CollapsibleHeader(GetTypeInfo(component.Type, $"{component.Name} ({component.ItemCount})", component.IsSnapshot), component.DetailedView, rect, out bool titleHovered, CommonUI.ButtonLocation.Start,
                textStyle: CommonUI.CalculateTextStyle(SpecialComponentType.Buffer, component.DetailedView)))
            {
                if (component.DetailedView)
                {
                    component.HideDetails();
                }
                else
                {
                    component.ShowDetails();
                }
            }
            GUI.enabled = true;
            if (titleHovered)
            {
                var c = component as EntityBufferComponentInfo;
                lastHovered = new HoverData() { entity = entity, DataType = HoverData.HoverType.Buffer, ComponentType = c.Type };
            }

            if (component.DetailedView)
            {
                GUILayout.BeginHorizontal(UIStyle.Instance.collapsibleContentStyle, options: null);
                GUILayout.Space(8);

                GUILayout.Label("Buffer Items", UIStyle.Instance.paginationLabelStyle, options: null);
                GUILayout.FlexibleSpace();
                int first = 1 + 10 * (component.CurrentPage - 1);
                int last = first + 9 > component.ItemCount ? component.ItemCount : first + 9;
                GUILayout.Label($" {first} - {last} of {component.ItemCount} ", UIStyle.Instance.paginationLabelStyle, options: null);
                GUI.enabled = component.CurrentPage > 1;
                if (GUILayout.Button(" ◀ ", UIStyle.Instance.iconButton, _paginationButton))
                {
                    component.PreviousPage();
                }
                GUI.enabled = component.CurrentPage < component.PageCount;
                if (GUILayout.Button(" ▶ ", UIStyle.Instance.iconButton, _paginationButton))
                {
                    component.NextPage();
                }
                GUI.enabled = true;

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(UIStyle.Instance.collapsibleContentStyle, options: null);
                GUILayout.Space(12);
                GUILayout.BeginVertical(options: null);

                GUILayout.Space(6);
                int count = component.DataArray.Count;
                int firstItem = (component.CurrentPage - 1) * 10;
                int lastItem = firstItem + 10;
                if (firstItem < count)
                {
                    int max = lastItem > count ? count : lastItem;
                    for (int i = firstItem; i < max; i++)
                    {
                        _objectRenderer.Render(component.DataArray[i], _inspector, i, rect, out var hoveredObject);
                        if (hoveredObject != null)
                        {
                            var c = component as EntityBufferComponentInfo;
                            lastHovered = new HoverData() { entity = entity, DataType = HoverData.HoverType.BufferItem, ComponentType = c.Type, Index = i };
                        }
                    }
                }

                GUILayout.Space(8);
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }
            _lastRendered = true;
        }

        public void Render(IEntityNotSupportedComponent component)
        {
            GUILayout.BeginHorizontal(options: null);
            GUI.enabled = false;
            GUILayout.Button("•", UIStyle.Instance.iconButton, options: CommonUI.ExpandButtonOptions);
            GUI.enabled = true;
            GUILayout.Label(GetTypeInfo(component.Type, component.Name), UIStyle.Instance.reducedPaddingLabelStyle, options: null);
            GUILayout.EndHorizontal();
            _lastRendered = false;
        }

        public void BeginSection(bool reset)
        {
            if (reset)
            {
                lastHovered = new HoverData();
            }
        }

        public void EndSection()
        {
            CommonUI.DrawLine();
            if (_lastRendered)
            {
#if DEBUG_PP
                _toolSystem.HoverData = lastHovered;
#endif
            }
        }

        private static string GetTypeInfo(ComponentType type, string name, bool isSnapshot = false)
        {
            return isSnapshot ? $"[S] {name}" : name;
            // if (type.IsZeroSized)
            // {
            //     return "[T] " + name;
            // }
            // if (type.IsComponent)
            // {
            //     return "[C] " /*+ $"[{AccessTypeString(type.AccessModeType)}] "*/ + name;
            // }
            // if (type.IsBuffer)
            // {
            //     return "[B] " /*+ $"[{AccessTypeString(type.AccessModeType)}] "*/ + name;
            // }
            //
            // return "[N] " + name;
        }

        private static string AccessTypeString(ComponentType.AccessMode typeAccessModeType)
        {
            switch (typeAccessModeType)
            {
                case ComponentType.AccessMode.ReadWrite:
                    return "RW";
                case ComponentType.AccessMode.ReadOnly:
                    return "RO";
                case ComponentType.AccessMode.Exclude:
                    return "E";
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeAccessModeType), typeAccessModeType, null);
            }
        }

        public static bool WasHovered(Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return false;

            return rect.Contains(Utils.GetTransformedMousePosition()) &&
                GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition);
        }

        public struct HoverData
        {
            public Entity entity;
            public HoverType DataType;
            public ComponentType ComponentType;
            public IInspectableObject ComponentItem;
            public int Index;

            public enum HoverType
            {
                None,
                Buffer,
                BufferItem,
                Component,
                ComponentItem,
            }
        }
    }

    public class InspectableObjectRenderer
    {
        private static GUILayoutOption[] _paginationButton = new GUILayoutOption[] { GUILayout.MinWidth(60), GUILayout.MaxWidth(60), GUILayout.MaxHeight(22) };

        public bool Render(IInspectableObject obj, IValueInspector valueInspector, int index, Rect rect, out IInspectableObject hoveredObject)
        {
            hoveredObject = null;
            if (obj is InspectableEntity entity)
            {
                GUILayout.BeginHorizontal(options: null);
                Entity value = (Entity)(entity.GetValueCached() ?? default(Entity));
                GUILayout.Label(entity.FieldInfo.Name + ":", UIStyle.Instance.reducedPaddingLabelStyle, options: null);
                GUILayout.Space(2);
                GUILayout.Label($"{value.ToString()} {(!string.IsNullOrEmpty(entity.PrefabName) ? $" - {entity.PrefabName}" : string.Empty)}", UIStyle.Instance.CalculateTextStyle(typeof(Entity)), options: null);
                GUILayout.FlexibleSpace();
                GUI.enabled = value.ExistsIn(World.DefaultGameObjectInjectionWorld.EntityManager);
                if (entity.CanJumpTo && GUILayout.Button("Jump To", UIStyle.Instance.iconButton, options: null))
                {
                    entity.InspectorPopupRef = valueInspector.Inspect(value, entity, InspectMode.JumpTo);
                }
                if (GUILayout.Button("Details", UIStyle.Instance.iconButton, options: null))
                {
                    entity.InspectorPopupRef = valueInspector.Inspect(value, entity, InspectMode.Linked);
                }
                //TODO Watchers
                // if (GUILayout.Button("Watch", UIStyle.Instance.iconButton, options: null))
                // {
                //     entity.InspectorPopupRef = valueInspector.Inspect(value, entity, InspectMode.Watcher);
                // }
                if (GUILayout.Button("Inspect", UIStyle.Instance.iconButton, options: null))
                {
                    entity.InspectorPopupRef = valueInspector.Inspect(value, entity, InspectMode.Standalone);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                if (ComponentDataRenderer.WasHovered(rect))
                {
                    hoveredObject = obj;
                }
            }
            else if (obj is CommonInspectableObject common)
            {
                GUILayout.BeginHorizontal(options: null);
                GUILayout.Label((common.FieldInfo?.Name ?? common.ValueType.Name) + ":", UIStyle.Instance.CalculateLabelStyle(common.FieldInfo?.IsPublic ?? true), options: null);
                GUILayout.Space(2);
                var value = common.GetValueCached();
                GUILayout.Label(value != null ? value.ToString() : "<NULL>", UIStyle.Instance.CalculateTextStyle(common.FieldInfo?.FieldType ?? common.ValueType), options: null);
                GUILayout.FlexibleSpace();
                if (common.CanInspectValue && GUILayout.Button("Preview", UIStyle.Instance.iconButton, options: null))
                {
                    common.InspectorPopupRef = valueInspector.Inspect(common.GetValueCached(), common, InspectMode.Linked);
                }
                if (common.CanInspectValue && GUILayout.Button("Inspect", UIStyle.Instance.iconButton, options: null))
                {
                    common.InspectorPopupRef = valueInspector.Inspect(common.GetValueCached(), common, InspectMode.Standalone);
                }
                GUILayout.EndHorizontal();
                if (ComponentDataRenderer.WasHovered(rect))
                {
                    hoveredObject = obj;
                }
            }
            else if (obj is ComplexObject complex)
            {
                string fieldName = obj.FieldInfo?.Name;
                object valueCached = complex.GetValueCached();
                GUI.enabled = valueCached != null;
                if (CommonUI.CollapsibleHeader(
                    $"{(!string.IsNullOrEmpty(fieldName) ? $"{fieldName} ({obj.FieldInfo?.FieldType}) {(valueCached == null ? " <NULL>" : string.Empty)}" : $"{complex.RootType.Name} {(index >= 0 ? $"[{index}] {(!string.IsNullOrEmpty(complex.PrefabName) ? $"- {complex.PrefabName}" : string.Empty)}" : string.Empty)}")}",
                    complex.IsActive, rect, out bool headerHovered, CommonUI.ButtonLocation.Start,
                    textStyle: CommonUI.CalculateTextStyle(complex.RootType.Name, complex.IsActive), focused: obj.IsActive))
                {
                    complex.IsActive = !complex.IsActive;
                }

                if (headerHovered)
                {
                    hoveredObject = obj;
                }

                GUI.enabled = true;
                if (complex.IsActive && complex.Children != null)
                {
                    GUILayout.BeginHorizontal(options: null);
                    GUILayout.Space(12);

                    GUILayout.BeginVertical(options: null);
                    for (var i = 0; i < complex.Children.Length; i++)
                    {
                        Render(complex.Children[i], valueInspector, i, rect, out var hoveredChildObject);
                        if (hoveredChildObject != null)
                        {
                            hoveredObject = hoveredChildObject;
                        }

                        if (i < complex.Children.Length - 1)
                        {
                            CommonUI.DrawLine();
                        }
                    }
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }
            }
            else if (obj is IterableObject iterable)
            {
                Logging.DebugEvaluation($"Rendering array: {obj.FieldInfo.FieldType.FullName}");
                GUI.enabled = iterable.ItemCount > 0;
                if (CommonUI.CollapsibleHeader($"{iterable.FieldInfo.Name} ({iterable.ItemCount}){(iterable.ItemCount == 0 ? " <EMPTY>" : string.Empty)}", iterable.IsActive, rect, out bool _, CommonUI.ButtonLocation.Start,
                    textStyle: CommonUI.CalculateTextStyle(SpecialComponentType.Buffer, iterable.IsActive)))
                {
                    iterable.IsActive = !iterable.IsActive;
                }
                GUI.enabled = true;

                if (iterable.IsActive)
                {
                    GUILayout.BeginHorizontal(UIStyle.Instance.collapsibleContentStyle, options: null);
                    GUILayout.Space(8);

                    GUILayout.Label("Items", UIStyle.Instance.paginationLabelStyle, options: null);
                    GUILayout.FlexibleSpace();
                    int first = 1 + 10 * (iterable.CurrentPage - 1);
                    int last = first + 9 > iterable.ItemCount ? iterable.ItemCount : first + 9;
                    GUILayout.Label($" {first} - {last} of {iterable.ItemCount} ", UIStyle.Instance.paginationLabelStyle, options: null);
                    GUI.enabled = iterable.CurrentPage > 1;
                    if (GUILayout.Button(" ◀ ", UIStyle.Instance.iconButton, _paginationButton))
                    {
                        iterable.PreviousPage();
                    }
                    GUI.enabled = iterable.CurrentPage < iterable.PageCount;
                    if (GUILayout.Button(" ▶ ", UIStyle.Instance.iconButton, _paginationButton))
                    {
                        iterable.NextPage();
                    }
                    GUI.enabled = true;

                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(UIStyle.Instance.collapsibleContentStyle, options: null);
                    GUILayout.Space(12);
                    GUILayout.BeginVertical(options: null);

                    GUILayout.Space(6);
                    int count = iterable.DataArray.Count;
                    int firstItem = (iterable.CurrentPage - 1) * 10;
                    int lastItem = firstItem + 10;
                    if (firstItem < count)
                    {
                        int max = lastItem > count ? count : lastItem;
                        for (int i = firstItem; i < max; i++)
                        {
                            Render(iterable.DataArray[i], valueInspector, i, rect, out var hoveredChildObject);
                            if (hoveredChildObject != null)
                            {
                                hoveredObject = hoveredChildObject;
                            }
                        }
                    }

                    GUILayout.Space(8);
                    GUILayout.EndVertical();

                    GUILayout.EndHorizontal();
                }
            }
            else if (obj is PrefabComponentsIterableObject iterableComponents)
            {
                Logging.DebugEvaluation($"Rendering array: {obj.FieldInfo.FieldType.FullName}");
                if (CommonUI.CollapsibleHeader($"{iterableComponents.FieldInfo.Name} ({iterableComponents.ItemCount})", iterableComponents.IsActive, rect, out bool _, CommonUI.ButtonLocation.Start,
                    textStyle: CommonUI.CalculateTextStyle(SpecialComponentType.Buffer, iterableComponents.IsActive)))
                {
                    iterableComponents.IsActive = !iterableComponents.IsActive;
                }

                if (iterableComponents.IsActive)
                {
                    GUILayout.BeginHorizontal(UIStyle.Instance.collapsibleContentStyle, options: null);
                    GUILayout.Space(8);

                    GUILayout.Label("Items", UIStyle.Instance.paginationLabelStyle, options: null);
                    GUILayout.FlexibleSpace();
                    int first = 1 + 10 * (iterableComponents.CurrentPage - 1);
                    int last = first + 9 > iterableComponents.ItemCount ? iterableComponents.ItemCount : first + 9;
                    GUILayout.Label($" {first} - {last} of {iterableComponents.ItemCount} ", UIStyle.Instance.paginationLabelStyle, options: null);
                    GUI.enabled = iterableComponents.CurrentPage > 1;
                    if (GUILayout.Button(" ◀ ", UIStyle.Instance.iconButton, _paginationButton))
                    {
                        iterableComponents.PreviousPage();
                    }
                    GUI.enabled = iterableComponents.CurrentPage < iterableComponents.PageCount;
                    if (GUILayout.Button(" ▶ ", UIStyle.Instance.iconButton, _paginationButton))
                    {
                        iterableComponents.NextPage();
                    }
                    GUI.enabled = true;

                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(UIStyle.Instance.collapsibleContentStyle, options: null);
                    GUILayout.Space(12);
                    GUILayout.BeginVertical(options: null);

                    GUILayout.Space(6);
                    int count = iterableComponents.DataArray.Count;
                    int firstItem = (iterableComponents.CurrentPage - 1) * 10;
                    int lastItem = firstItem + 10;
                    if (firstItem < count)
                    {
                        int max = lastItem > count ? count : lastItem;
                        for (int i = firstItem; i < max; i++)
                        {
                            Render(iterableComponents.DataArray[i], valueInspector, i, rect, out _);
                        }
                    }

                    GUILayout.Space(8);
                    GUILayout.EndVertical();

                    GUILayout.EndHorizontal();
                }

            }
            else if (obj is GenericListObject iterableList)
            {
                Logging.DebugEvaluation($"Rendering array: {obj.FieldInfo.FieldType.FullName}");
                if (CommonUI.CollapsibleHeader($"{iterableList.FieldInfo.Name} ({iterableList.ItemCount})", iterableList.IsActive, rect, out bool _, CommonUI.ButtonLocation.Start,
                    textStyle: CommonUI.CalculateTextStyle(SpecialComponentType.Buffer, iterableList.IsActive)))
                {
                    iterableList.IsActive = !iterableList.IsActive;
                }

                if (iterableList.IsActive)
                {
                    GUILayout.BeginHorizontal(UIStyle.Instance.collapsibleContentStyle, options: null);
                    GUILayout.Space(8);

                    GUILayout.Label("Items", UIStyle.Instance.paginationLabelStyle, options: null);
                    GUILayout.FlexibleSpace();
                    int first = 1 + 10 * (iterableList.CurrentPage - 1);
                    int last = first + 9 > iterableList.ItemCount ? iterableList.ItemCount : first + 9;
                    GUILayout.Label($" {first} - {last} of {iterableList.ItemCount} ", UIStyle.Instance.paginationLabelStyle, options: null);
                    GUI.enabled = iterableList.CurrentPage > 1;
                    if (GUILayout.Button(" ◀ ", UIStyle.Instance.iconButton, _paginationButton))
                    {
                        iterableList.PreviousPage();
                    }
                    GUI.enabled = iterableList.CurrentPage < iterableList.PageCount;
                    if (GUILayout.Button(" ▶ ", UIStyle.Instance.iconButton, _paginationButton))
                    {
                        iterableList.NextPage();
                    }
                    GUI.enabled = true;

                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(UIStyle.Instance.collapsibleContentStyle, options: null);
                    GUILayout.Space(12);
                    GUILayout.BeginVertical(options: null);

                    GUILayout.Space(6);
                    int count = iterableList.DataArray.Count;
                    int firstItem = (iterableList.CurrentPage - 1) * 10;
                    int lastItem = firstItem + 10;
                    if (firstItem < count)
                    {
                        int max = lastItem > count ? count : lastItem;
                        for (int i = firstItem; i < max; i++)
                        {
                            Render(iterableList.DataArray[i], valueInspector, i, rect, out _);
                        }
                    }

                    GUILayout.Space(8);
                    GUILayout.EndVertical();

                    GUILayout.EndHorizontal();
                }

            }
            else if (obj == null)
            {
                Logging.DebugEvaluation($"Object is null!");
            }

            return false;
        }
    }
}
