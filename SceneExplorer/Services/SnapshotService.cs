using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Prefabs;
using SceneExplorer.ToBeReplaced.Helpers;
using Unity.Collections;
using Unity.Entities;

namespace SceneExplorer.Services
{
    public class SnapshotService
    {
        internal static SnapshotService Instance { get; }

        private EntityManager _entityManager;
        private Dictionary<Entity, EntitySnapshotData> _data;
        private TypeDescriptorService _typeDescriptorService;
        public bool DataChanged { get; private set; }
    
        public IReadOnlyCollection<Entity> Entities => _data.Keys;

        static SnapshotService() {
            Instance = new SnapshotService();
        }

        private SnapshotService() {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _data = new Dictionary<Entity, EntitySnapshotData>();
            _typeDescriptorService = TypeDescriptorService.Instance;
        }

        public void ResetChanged() {
            DataChanged = false;
        }

        public void MakeSnapshot(Entity entity) {
            if (entity != Entity.Null && !_data.ContainsKey(entity))
            {
                HashSet<Entity> visited = new HashSet<Entity>();
                Logging.Debug($"Snapshotting: {entity}");
                MakeSnapshot(entity, visited);
                if (visited.Count > 0)
                {
                    DataChanged = true;
                }
            }
        }

        private void MakeSnapshot(Entity entity, HashSet<Entity> visited) {
            if (entity != Entity.Null && !_data.ContainsKey(entity) && MakeSnapshotInternal(entity, visited, out EntitySnapshotData data))
            {
                _data.Add(entity, data);
            }
        }

        public bool TryGetSnapshot(Entity entity, out EntitySnapshotData data) {
            return _data.TryGetValue(entity, out data);
        }

        public bool HasSnapshot(Entity e) {
            return _data.ContainsKey(e);
        }

        private bool MakeSnapshotInternal(Entity entity, HashSet<Entity> visited, out EntitySnapshotData data) {
            data = null;
            if (visited.Contains(entity))
            {
                return false;
            }
            if (!_entityManager.Exists(entity))
            {
                Logging.Debug($"[MakeSnapshotInternal] Entity {entity} does not exist");
                return false;
            }

            Logging.DebugEvaluation($"Snapshotting: {entity}, visited: {visited.Count}");
            visited.Add(entity);
            NativeArray<ComponentType> componentTypes = _entityManager.GetComponentTypes(entity, Allocator.Temp);
            data = new EntitySnapshotData(entity, componentTypes.ToArray());
        
            for (int index = 0; index < componentTypes.Length; index++)
            {
                ComponentType componentType = componentTypes[index];
                object value = null;
                if (componentType.IsZeroSized)
                {
                    value = null;
                }
                else if (componentType.IsComponent)
                {
                    Type t = componentType.GetManagedType();
                    value = t.GetComponentDataByType(_entityManager, entity);
                    if (!t.Equals(typeof(PrefabRef)) &&
                        !t.Equals(typeof(PrefabData)))
                    {
                        List<FieldInfo> fields = _typeDescriptorService.GetFields(componentType);
                        foreach (FieldInfo fieldInfo in fields)
                        {
                            if (fieldInfo.FieldType.Equals(typeof(Entity)))
                            {
                                MakeSnapshot((Entity)fieldInfo.GetValue(value), visited);
                            }
                        }
                    }
                }
                else if (componentType.IsBuffer)
                {
                    List<object> values = componentType.GetManagedType().GetComponentBufferArrayByType(_entityManager, entity);
                    List<FieldInfo> fields = _typeDescriptorService.GetFields(componentType);
                    foreach (FieldInfo fieldInfo in fields)
                    {
                        if (fieldInfo.FieldType.Equals(typeof(Entity)))
                        {
                            foreach (object v in values)
                            {
                                MakeSnapshot((Entity)fieldInfo.GetValue(v), visited);
                            }
                        }
                    }
                    value = values;
                }
                else
                {
                    value = null;
                }
            
                data._componentData.Add(componentType, value);
            }
            componentTypes.Dispose();

            return true;
        }

        public void Clear() {
            foreach (EntitySnapshotData entitySnapshotData in _data.Values)
            {
                entitySnapshotData.Dispose();
            }
            _data.Clear();
            DataChanged = true;
        }


        public class EntitySnapshotData
        {
            private Entity _entity;
            private ComponentType[] _componentTypes;
            internal Dictionary<ComponentType, object> _componentData;

            public Entity Entity => _entity;
            public bool Exist => World.DefaultGameObjectInjectionWorld.EntityManager.Exists(_entity);
            public ComponentType[] ComponentTypes => _componentTypes;

            public EntitySnapshotData(Entity entity, ComponentType[] types) {
                _entity = entity;
                _componentTypes = types;
                _componentData = new Dictionary<ComponentType, object>();
            }

            public bool TryGetData(ComponentType type, out object value) {
                return _componentData.TryGetValue(type, out value);
            }

            public bool Matching(QueryCreator.IQuery query) {
                List<ComponentType> componentTypes = query.WithAll;
                if (componentTypes.Count > 0)
                {
                    foreach (ComponentType componentType in componentTypes)
                    {
                        if (!_componentData.ContainsKey(componentType))
                        {
                            return false;
                        }
                    }
                }
            
                componentTypes = query.WithAny;
                if (componentTypes.Count > 0)
                {
                    bool match = false;
                    foreach (ComponentType componentType in componentTypes)
                    {
                        if (_componentData.ContainsKey(componentType))
                        {
                            match = true;
                            break;
                        }
                    }
                    if (!match)
                    {
                        return false;
                    }
                }

                componentTypes = query.WithNone;
                if (componentTypes.Count > 0)
                {
                    foreach (ComponentType componentType in componentTypes)
                    {
                        if (_componentData.ContainsKey(componentType))
                        {
                            return false;
                        }
                    }
                }
            
                return true;
            }

            public void Dispose() {
                _componentTypes = null;
                _componentData.Clear();
                _componentData = null;
            }
        }
    }
}
