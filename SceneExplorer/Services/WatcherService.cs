using System;
using System.Collections.Generic;
using Game.Debug;
using Game.Prefabs;
using SceneExplorer.ToBeReplaced.Helpers;
using SceneExplorer.ToBeReplaced.Windows;
using Unity.Entities;
using UnityEngine;

namespace SceneExplorer.Services
{
    public class WatcherService
    {
        public interface IWatchable
        {
            string Preview();
            string PrefabName { get; }
            SnapshotService.EntitySnapshotData Snapshot { get; }
            void Inspect();
            void Update();
            bool IsValid { get; }
            void Dispose();
        }

        public static WatcherService Instance;

        static WatcherService() {
            Instance = new WatcherService();
            Instance.Watchables = Instance._watchables.AsReadOnly();
        }

        private List<IWatchable> _watchables = new List<IWatchable>();

        public IReadOnlyList<IWatchable> Watchables { get; private set; }
        public bool DataChanged { get; private set; }
        
        public void Add(IWatchable watchable) {
            _watchables.Add(watchable);
            DataChanged = true;
        }

        public void Remove(IWatchable watchable) {
            watchable.Dispose();
            _watchables.Remove(watchable);
            DataChanged = true;
        }

        public void Clear() {
            foreach (IWatchable watchable in _watchables)
            {
                watchable.Dispose();
            }
            _watchables.Clear();
            DataChanged = true;
        }

        public void Update() {
            foreach (IWatchable watchable in _watchables)
            {
                watchable.Update();
            }
        }

        public void ResetChanged() {
            DataChanged = false;
        }
        
        public class WatchableEntity : IWatchable, IDisposable
        {
            private Entity _entity;
            private bool _isWalid;
            private string _name = string.Empty;
            private PrefabSystem _prefabSystem;
            private bool _isSnapshot;

            public WatchableEntity(Entity e, PrefabSystem prefabSystem) {
                _entity = e;
                _prefabSystem = prefabSystem;
                Assert.IsNotNull(prefabSystem);
                Update();
            }
            
            public string Preview() {
                return _isSnapshot ? "[S] "+ _entity :_entity.ToString();
            }

            public string PrefabName => _name;

            public SnapshotService.EntitySnapshotData Snapshot => SnapshotService.Instance.TryGetSnapshot(_entity, out SnapshotService.EntitySnapshotData data) ? data : null;

            public void Inspect() {
                var inspector = new GameObject("Manual Object Inspector(Watchers)").AddComponent<ManualEntityInspector>();
                SnapshotService.EntitySnapshotData inspectorSnapshotData = Snapshot;
                inspector.ChainDepth = 1;
                inspector.SelectedEntity = _entity;
                inspector.SnapshotData = inspectorSnapshotData;
                inspector.Subtitle = $"{(inspectorSnapshotData != null ? "[S] ": string.Empty)}{_entity}";
                inspector.ForcePosition(inspector.Position + new Vector2(32, -32));
                inspector.Open();
            }

            public void Update() {
                var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                var snapshot = Snapshot;
                _isWalid = _entity != Entity.Null && (entityManager.Exists(_entity) || snapshot!= null);
                _isSnapshot = snapshot != null;
                if (IsValid)
                {
                    string prefabName = _entity.TryGetPrefabName(entityManager, _prefabSystem, out string prefabType);
                    _name = !string.IsNullOrEmpty(prefabName) ? $"({prefabType} - {prefabName})" : string.Empty;
                }
            }

            public bool IsValid => _isWalid;

            public void Dispose() {
                _prefabSystem = null;
            }
        }
    }
}
