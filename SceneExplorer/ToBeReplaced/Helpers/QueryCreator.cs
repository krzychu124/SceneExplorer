using System;
using System.Collections.Generic;
using System.Linq;
using SceneExplorer.ToBeReplaced.Windows;
using Unity.Collections;
using Unity.Entities;

namespace SceneExplorer.ToBeReplaced.Helpers
{
    public class QueryCreator : IDisposable
    {
        private HashSet<ComponentType> _withAll = new HashSet<ComponentType>();
        private HashSet<ComponentType> _withAny = new HashSet<ComponentType>();
        private HashSet<ComponentType> _withNone = new HashSet<ComponentType>();
        private HashSet<ComponentType> _withAllPendingAdd = new HashSet<ComponentType>();
        private HashSet<ComponentType> _withAnyPendingAdd = new HashSet<ComponentType>();
        private HashSet<ComponentType> _withNonePendingAdd = new HashSet<ComponentType>();
        private HashSet<ComponentType> _withAllPendingRemove = new HashSet<ComponentType>();
        private HashSet<ComponentType> _withAnyPendingRemove = new HashSet<ComponentType>();
        private HashSet<ComponentType> _withNonePendingRemove = new HashSet<ComponentType>();
        private List<KeyValuePair<string, TypeIndex>> _items;
        private List<KeyValuePair<string, TypeIndex>> _filtered;

        public QueryCreator(List<TypeIndex> items) {
        _items = items.Select(i => new KeyValuePair<string, TypeIndex>(ComponentType.FromTypeIndex(i).GetManagedType().FullName, i)).ToList();
        _filtered = new List<KeyValuePair<string, TypeIndex>>(_items);
    }

        public List<KeyValuePair<string, TypeIndex>> Items => _filtered;
        public HashSet<ComponentType>.Enumerator WithAll => _withAll.GetEnumerator();
        public HashSet<ComponentType>.Enumerator WithAny => _withAny.GetEnumerator();
        public HashSet<ComponentType>.Enumerator WithNone => _withNone.GetEnumerator();
    
        public bool Changed { get; set; }

        public bool Add(ComponentType componentType, MatchingType match) {
        Changed = true;
        switch (match)
        {
            case MatchingType.WithAll:
                return _withAll.Add(componentType);
            case MatchingType.WithAny:
                return _withAny.Add(componentType);
            case MatchingType.WithNone:
                return _withNone.Add(componentType);
        }
        
        return false;
    }
    
        public bool AddDeferred(ComponentType componentType, MatchingType match) {
        Changed = true;
        switch (match)
        {
            case MatchingType.WithAll:
                return !_withAll.Contains(componentType) && _withAllPendingAdd.Add(componentType);
            case MatchingType.WithAny:
                return !_withAny.Contains(componentType) && _withAnyPendingAdd.Add(componentType);
            case MatchingType.WithNone:
                return !_withNone.Contains(componentType) && _withNonePendingAdd.Add(componentType);
        }
        
        return false;
    }
    
        public bool Remove(ComponentType componentType, MatchingType match) {
        Changed = true;
        switch (match)
        {
            case MatchingType.WithAll:
                return _withAll.Remove(componentType);
            case MatchingType.WithAny:
                return _withAny.Remove(componentType);
            case MatchingType.WithNone:
                return _withNone.Remove(componentType);
        }

        return false;
    }

        public bool RemoveDeferred(ComponentType componentType, MatchingType match) {
        Changed = true;
        switch (match)
        {
            case MatchingType.WithAll:
                return _withAll.Contains(componentType) && _withAllPendingRemove.Add(componentType);
            case MatchingType.WithAny:
                return _withAny.Contains(componentType) && _withAnyPendingRemove.Add(componentType);
            case MatchingType.WithNone:
                return _withNone.Contains(componentType) && _withNonePendingRemove.Add(componentType);
        }

        return false;
    }

        public void Clear() {
        _withAll.Clear();
        _withAny.Clear();
        _withNone.Clear();
    }

        public bool FillBuilder(EntityManager manager, out EntityQuery query) {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        NativeList<ComponentType> types = new NativeList<ComponentType>(4, Allocator.Temp);
        bool anyAdded = false;
        if (_withAll.Count > 0)
        {
            foreach (ComponentType componentType in _withAll)
            {
                types.Add(componentType);
            }
            builder.WithAll(ref types);
            anyAdded = true;
        }
        if (_withAny.Count > 0)
        {
            types.Clear();
            foreach (ComponentType componentType in _withAny)
            {
                types.Add(componentType);
            }
            builder.WithAny(ref types);
            anyAdded = true;
        }
        if (_withNone.Count > 0)
        {
            types.Clear();
            foreach (ComponentType componentType in _withNone)
            {
                types.Add(componentType);
            }
            builder.WithNone(ref types);
            anyAdded = true;
        }
        types.Dispose();
        query = builder.Build(manager);
        builder.Dispose();

        return anyAdded;
    }

        public void FillQuery(IQuery query) {
        query.Reset();
        if (_withAll.Count > 0)
        {
            foreach (ComponentType componentType in _withAll)
            {
                query.AddAll(componentType);
            }
        }
        if (_withAny.Count > 0)
        {
            foreach (ComponentType componentType in _withAny)
            {
                query.AddAny(componentType);
            }
        }
        if (_withNone.Count > 0)
        {
            foreach (ComponentType componentType in _withNone)
            {
                query.AddNone(componentType);
            }
        }
    }

        public void Dispose() {
        _withAll.Clear();
        _withAll = null;
        _withAny.Clear();
        _withAny = null;
        _withNone.Clear();
        _withNone = null;
        _withAllPendingAdd.Clear();
        _withAllPendingAdd = null;
        _withAllPendingRemove.Clear();
        _withAllPendingRemove = null;
        _withAnyPendingAdd.Clear();
        _withAnyPendingAdd = null;
        _withAnyPendingRemove.Clear();
        _withAnyPendingRemove = null;
        _withNonePendingAdd.Clear();
        _withNonePendingAdd = null;
        _withNonePendingRemove.Clear();
        _withNonePendingRemove = null;
    }

        public enum MatchingType
        {
            WithAll,
            WithAny,
            WithNone,
        }

        public void FilterItems(string obj, MatchingType type) {
        _filtered.Clear();
        if (string.IsNullOrEmpty(obj))
        {
            return;
        }
        HashSet<ComponentType> matching = type == MatchingType.WithAll 
            ? _withAll 
            : type == MatchingType.WithAny 
                ? _withAny : _withNone;
        foreach (KeyValuePair<string, TypeIndex> item in _items)
        {
            if (item.Key.IndexOf(obj, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _filtered.Add(item);
            }
        }
        _filtered.RemoveAll(pair => matching.Contains(ComponentType.FromTypeIndex(pair.Value)));
    }

        public bool Sync() {
        Changed = false;
        foreach (ComponentType componentType in _withAllPendingAdd)
        {
            _withAll.Add(componentType);
        }
        _withAllPendingAdd.Clear();
        foreach (ComponentType componentType in _withAnyPendingAdd)
        {
            _withAny.Add(componentType);
        }
        _withAnyPendingAdd.Clear();
        foreach (ComponentType componentType in _withNonePendingAdd)
        {
            _withNone.Add(componentType);
        }
        _withNonePendingAdd.Clear();
        
        foreach (ComponentType componentType in _withAllPendingRemove)
        {
            _withAll.Remove(componentType);
        }
        _withAllPendingRemove.Clear();
        foreach (ComponentType componentType in _withAnyPendingRemove)
        {
            _withAny.Remove(componentType);
        }
        _withAnyPendingRemove.Clear();
        foreach (ComponentType componentType in _withNonePendingRemove)
        {
            _withNone.Remove(componentType);
        }
        _withNonePendingRemove.Clear();
        
        return _withAll.Count > 0 || _withAny.Count > 0 || _withNone.Count > 0;
    }
    
        public interface IQuery
        {
            void Reset();
            List<ComponentType> WithAll { get; } 
            void AddAll(ComponentType type);
            List<ComponentType> WithAny { get; } 
            void AddAny(ComponentType type);
            List<ComponentType> WithNone { get; } 
            void AddNone(ComponentType type);
        }
    }
}
