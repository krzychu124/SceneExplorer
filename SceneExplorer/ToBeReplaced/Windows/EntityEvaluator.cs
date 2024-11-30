using SceneExplorer.Services;
using SceneExplorer.ToBeReplaced.Helpers;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SceneExplorer.ToBeReplaced.Windows
{
    public interface IRenderer
    {
        void Render(IEntityTagComponent component, Entity entity);
        void Render(IEntityComponent component, Entity entity, Rect rect);
        void Render(IEntityBufferComponent component, Entity entity, Rect rect);
        void Render(IEntityNotSupportedComponent component);
        void BeginSection(bool reset);
        void EndSection();
    }

    public class EntityEvaluator
    {
        public Entity SelectedEntity
        {
            get => _selectedEntity;
            set {
                if (value != _selectedEntity)
                {
                    _selectedEntity = value;
                }
            }
        }

        public List<IEntityComponent> Components = new();
        public List<IEntityBufferComponent> Buffers = new();
        public List<IEntityTagComponent> Tags = new();
        public List<IEntityNotSupportedComponent> NotSupported = new();
        public bool Valid { get; private set; }
        public bool UseSnapshot { get; set; }
        private List<IInspectableComponent> _allComponents = new();
        private List<IInspectableComponent> _tempToRemove = new();
        private Entity _selectedEntity;

        public void Evaluate(EntityManager manager, bool refreshOnly = false)
        {
            if (SelectedEntity != Entity.Null)
            {
                if (!manager.Exists(SelectedEntity) && !UseSnapshot)
                {
                    Valid = false;
                    _selectedEntity = Entity.Null;
                    _allComponents.ForEach(c => { c.Dispose(); });
                    _allComponents.Clear();
                    Components.Clear();
                    Buffers.Clear();
                    Tags.Clear();
                    NotSupported.Clear();
                    Logging.DebugEvaluation("Evaluating: entity not exist");
                    return;
                }

                if (UseSnapshot)
                {
                    Logging.DebugEvaluation("Evaluating: Snapshot");
                    _allComponents.ForEach(c => { c.Dispose(); });
                    _allComponents.Clear();
                    Components.Clear();
                    Buffers.Clear();
                    Tags.Clear();
                    NotSupported.Clear();
                    if (SnapshotService.Instance.TryGetSnapshot(SelectedEntity, out SnapshotService.EntitySnapshotData data))
                    {
                        Logging.DebugEvaluation($"Preparing components for {data.ComponentTypes.Length} ComponentType's");
                        foreach (ComponentType componentType in data.ComponentTypes)
                        {
                            IInspectableComponent componentInfo = UIGenerator.CalculateComponentInfo(componentType, SelectedEntity, true);
                            componentInfo.UpdateBindings(_selectedEntity);
                            AddComponent(componentInfo);
                        }
                        Valid = true;
                        _allComponents.ForEach(c => c.RefreshValues(SelectedEntity));
                    }
                    else
                    {
                        Valid = false;
                    }
                    return;
                }

                if (!refreshOnly)
                {
                    Logging.DebugEvaluation("Evaluating: full");
                    _allComponents.ForEach(c => { c.Dispose(); });
                    _allComponents.Clear();
                    Components.Clear();
                    Buffers.Clear();
                    Tags.Clear();
                    NotSupported.Clear();

                    using (NativeArray<ComponentType> componentTypes = manager.GetComponentTypes(SelectedEntity))
                    {
                        foreach (ComponentType componentType in componentTypes)
                        {
                            IInspectableComponent componentInfo = UIGenerator.CalculateComponentInfo(componentType, SelectedEntity, false);
                            componentInfo.UpdateBindings(_selectedEntity);
                            AddComponent(componentInfo);
                        }
                    }
                    Valid = true;
                }
                else
                {
                    // Logging.Info("Evaluating: refresh");
                    using (NativeArray<ComponentType> componentTypes = manager.GetComponentTypes(SelectedEntity))
                    {
                        IEnumerable<ComponentType> components = _allComponents.Select(c => c.Type);
                        if (components.SequenceEqual(componentTypes))
                        {

                            foreach (IInspectableComponent inspectableComponent in _allComponents)
                            {
                                if (!inspectableComponent.UpdateBindings(_selectedEntity))
                                {
                                    _tempToRemove.Add(inspectableComponent);
                                }
                            }

                            if (_tempToRemove.Count > 0)
                            {
                                foreach (IInspectableComponent inspectableComponent in _tempToRemove)
                                {
                                    RemoveComponent(inspectableComponent);
                                }
                                _tempToRemove.Clear();
                            }
                            Valid = true;
                        }
                        else
                        {
                            Logging.DebugEvaluation("Evaluating: dif sequence");
                            Evaluate(manager, false);
                            Valid = true;
                        }
                    }
                }
            }
            else if (Valid)
            {
                Logging.DebugEvaluation("Evaluating: valid but selected is empty");
                _allComponents.ForEach(c => { c.Dispose(); });
                _allComponents.Clear();
                Components.Clear();
                Buffers.Clear();
                Tags.Clear();
                NotSupported.Clear();
                Valid = false;
            }
        }

        public void Refresh()
        {
            Evaluate(World.DefaultGameObjectInjectionWorld.EntityManager, true);
            if (Valid)
            {
                _allComponents.ForEach(c => c.RefreshValues(SelectedEntity));
            }
        }

        public void RenderComponents(IRenderer renderer, Entity selectedEntity, Rect rect)
        {
            if (_allComponents.Count == 0)
            {
                return;
            }
            renderer.BeginSection(true);
            for (int index = 0; index < Components.Count; index++)
            {
                IEntityComponent entityComponent = Components[index];
                renderer.Render(entityComponent, selectedEntity, rect);
                if (index < Components.Count - 1)
                {
                    CommonUI.DrawLine();
                }
            }
            renderer.EndSection();
            renderer.BeginSection(false);
            for (int index = 0; index < Buffers.Count; index++)
            {
                IEntityBufferComponent entityBufferComponent = Buffers[index];
                renderer.Render(entityBufferComponent, selectedEntity, rect);
                if (index < Buffers.Count - 1)
                {
                    CommonUI.DrawLine();
                }
            }
            renderer.EndSection();

            renderer.BeginSection(false);
            for (int index = 0; index < Tags.Count; index++)
            {
                IEntityTagComponent entityTagComponent = Tags[index];
                renderer.Render(entityTagComponent, selectedEntity);
                if (index < Tags.Count - 1)
                {
                    CommonUI.DrawLine();
                }
            }
            renderer.EndSection();
            if (NotSupported.Count > 0)
            {
                renderer.BeginSection(false);
                NotSupported.ForEach(renderer.Render);
                renderer.EndSection();
            }
        }

        private void AddComponent(IInspectableComponent componentInfo)
        {
            _allComponents.Add(componentInfo);
            if (componentInfo is IEntityTagComponent tag)
            {
                Tags.Add(tag);
            }
            if (componentInfo is IEntityComponent component)
            {
                Components.Add(component);
            }
            if (componentInfo is IEntityBufferComponent buffer)
            {
                Buffers.Add(buffer);
            }
            if (componentInfo is EntityNotSupportedComponent notSupported)
            {
                NotSupported.Add(notSupported);
            }
        }

        private void RemoveComponent(IInspectableComponent componentInfo)
        {
            _allComponents.Remove(componentInfo);
            if (componentInfo is IEntityTagComponent tag)
            {
                Tags.Remove(tag);
            }
            if (componentInfo is IEntityComponent component)
            {
                Components.Remove(component);
            }
            if (componentInfo is IEntityBufferComponent buffer)
            {
                Buffers.Remove(buffer);
            }
            if (componentInfo is EntityNotSupportedComponent notSupported)
            {
                NotSupported.Remove(notSupported);
            }
        }
    }
}
