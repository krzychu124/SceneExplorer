using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;

namespace SceneExplorer.Services
{
    public class TypeDescriptorService
    {
        private Dictionary<ComponentType, List<FieldInfo>> _componentFields = new Dictionary<ComponentType, List<FieldInfo>>();
        private Dictionary<Type, List<FieldInfo>> _otherFields = new Dictionary<Type, List<FieldInfo>>();
        private HashSet<Type> _skipTypes = new HashSet<Type>();

        internal static TypeDescriptorService Instance { get; }

        static TypeDescriptorService()
        {
            Instance = new TypeDescriptorService();
        }

        private TypeDescriptorService()
        {
            _skipTypes.Add(typeof(EntityArchetype));
            _skipTypes.Add(typeof(FixedString32Bytes));
            _skipTypes.Add(typeof(FixedString64Bytes));
            _skipTypes.Add(typeof(FixedString128Bytes));
            _skipTypes.Add(typeof(FixedString512Bytes));
            _skipTypes.Add(typeof(FixedString4096Bytes));
            _skipTypes.Add(typeof(FixedString4096Bytes));
            _skipTypes.Add(typeof(GCHandle));
        }

        public List<FieldInfo> GetFields(ComponentType type)
        {
            EnsureComponentFieldsData(type);
            return _componentFields[type];
        }

        public List<FieldInfo> GetFields(Type type)
        {
            EnsureOtherTypeFieldsData(type);
            return _otherFields[type];
        }

        public bool IsTagComponent(ComponentType type)
        {
            return _componentFields[type].Count == 0;
        }

        private void EnsureComponentFieldsData(ComponentType type)
        {
            if (!_componentFields.ContainsKey(type))
            {
                List<FieldInfo> fields = type.GetManagedType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();
                _componentFields[type] = fields;
            }
        }

        private void EnsureOtherTypeFieldsData(Type type)
        {
            if (!_otherFields.ContainsKey(type))
            {
                if (type.IsEnum)
                {
                    _otherFields[type] = new List<FieldInfo>(0);
                    return;
                }
                if (_skipTypes.Contains(type))
                {
                    _otherFields[type] = new List<FieldInfo>(0);
                    return;
                }

                List<FieldInfo> fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();
                _otherFields[type] = fields;
            }
        }
    }
}
