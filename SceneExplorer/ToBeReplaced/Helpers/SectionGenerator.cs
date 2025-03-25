using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SceneExplorer.ToBeReplaced.Helpers
{
using Colossal.Entities;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using SceneExplorer.ToBeReplaced.Windows;
using Unity.Collections;
using Unity.Entities;
using SubLane = Game.Net.SubLane;
using SubObject = Game.Objects.SubObject;
    public static class SectionGenerator
    {
        public static void AddSection(ComponentType componentType, ObjectInfo.UIBuilder builder, EntityManager entityManager, Entity entity, PrefabSystem prefabSystem) {
       
        if (!componentType.IsComponent)
        {
            if (componentType.IsBuffer)
            {
                Type type = componentType.GetManagedType();
                string typeName = type.FullName;
                StringBuilder sb = new StringBuilder();

                MethodInfo method = typeof(EntityManager).GetMethod(nameof(EntityManager.GetBuffer),  new Type[] { typeof(Entity), typeof(bool) });
                MethodInfo generic = method.MakeGenericMethod(type);
                object bufferValue = generic.Invoke(entityManager, new object[]{entity, true});
                IEnumerable nativeArray = (IEnumerable)bufferValue.GetType().GetMethod("AsNativeArray", BindingFlags.Instance | BindingFlags.Public).Invoke(bufferValue, null);
                
                foreach (object value in nativeArray)
                {
                    string s = GetValueString(value, entityManager, prefabSystem);
                    sb.AppendLine($"  item ({value.GetType().Name}):").Append(s);
                }
                IDisposable disposable = nativeArray as IDisposable;
                disposable.Dispose();
                
                string valueStr = sb.ToString();
                Logging.DebugEvaluation($"Section (buffer) ({typeName}):\n{valueStr}");
                builder.AddBufferSection(typeName, valueStr);
                
                return;
            }      
            
        }
        else
        {
            Type type = componentType.GetManagedType();
            string typeName = type.FullName;
            StringBuilder sb = new StringBuilder();
            
            MethodInfo method = typeof(EntityManager).GetMethod(nameof(EntityManager.GetComponentData),  new Type[] { typeof(Entity) });
            MethodInfo generic = method.MakeGenericMethod(type);
            object componentValue = generic.Invoke(entityManager, new object[]{entity});
            foreach (FieldInfo field in type.GetRuntimeFields())
            {
                if (field.IsStatic)
                {
                    continue;
                }

                if (field.Name.Equals("m_Prefab"))
                {
                    var pEntity = field.GetValue(componentValue);
                    if (((Entity)pEntity).ExistsIn(entityManager))
                    {
                        PrefabBase prefabData = prefabSystem.GetPrefab<PrefabBase>((Entity)pEntity);
                        sb.AppendLine($"\tPrefab name:{prefabData.name}");
                        Entity prefabEntity = (Entity)pEntity;
                        if (entityManager.HasComponent<NetGeometryData>(prefabEntity))
                        {
                            // NetGeometryData data = entityManager.GetComponentData<NetGeometryData>(prefabEntity);
                            // sb.AppendLine("\t  NetGeometryData: ").AppendLine(GetComponentData(data, 2));
                        }
                        if (entityManager.HasComponent<NetData>(prefabEntity))
                        {
                            NetData data = entityManager.GetComponentData<NetData>(prefabEntity);
                            sb.AppendLine("\t  NetData: ").AppendLine(GetComponentData(data, 2));
                        }
                        if (entityManager.HasComponent<PrefabData>(prefabEntity))
                        {
                            PrefabData data = entityManager.GetComponentData<PrefabData>(prefabEntity);
                            sb.AppendLine("\t  PrefabData: ").AppendLine(GetComponentData(data, 2));
                        }
                        // NetLanePrefab netLanePrefab = prefabSystem.GetPrefab<NetLanePrefab>((Entity)pEntity);
                        // if (netLanePrefab != null)
                        // {
                        //     sb.AppendLine("Has netLanePrefabs");
                        // }
                        continue;
                    }
                }
                
                var val = field.GetValue(componentValue);
                sb.Append("  ").Append(field.Name).Append(" = ").AppendLine(field.GetValue(componentValue).ToString());
                if (val is Entity e && e.ExistsIn(entityManager))
                {
                    NativeArray<ComponentType> types = entityManager.GetComponentTypes(e);
                    if (types.Contains(typeof(PrefabRef)))
                    {
                        PrefabRef prefabRef = entityManager.GetComponentData<PrefabRef>(e);
                        PrefabBase prefabData = prefabSystem.GetPrefab<PrefabBase>(prefabRef.m_Prefab);
                        sb.AppendLine($"\tSource prefab name: {prefabData.name}");
                        // if (entityManager.HasComponent<NetGeometryData>(prefabRef.m_Prefab))
                        // {
                            // NetGeometryData data = entityManager.GetComponentData<NetGeometryData>(prefabRef.m_Prefab);
                            // sb.AppendLine("\t  NetGeometryData: ").AppendLine(GetComponentData(data, 2));
                        // }
                        // NetLanePrefab netLanePrefab = prefabSystem.GetPrefab<NetLanePrefab>(e);
                        // if (netLanePrefab != null)
                        // {
                            // sb.AppendLine("Source has netLanePrefabs");
                        // }
                    }
                    sb.AppendLine($"\tEntity components ({types.Length}): ").Append("\t").AppendLine(string.Join(", ", types.ToArray().Select(t => t.GetManagedType().FullName)));
                    
                    foreach (ComponentType otherComponentType in types)
                    {
                        if (otherComponentType.IsComponent)
                        {
                            try
                            {
                                MethodInfo method2 = typeof(EntityManager).GetMethod(nameof(EntityManager.GetComponentData), new Type[] { typeof(Entity) });
                                MethodInfo generic2 = method2.MakeGenericMethod(otherComponentType.GetManagedType());
                                object componentValue2 = generic2.Invoke(entityManager, new object[] { e });
                                sb.Append("\t  ").AppendLine(otherComponentType.GetManagedType().Name).Append(GetComponentData(componentValue2, 2));
                            }
                            catch (Exception exception)
                            {
                                Logging.Error($"ComponentType: {otherComponentType.GetManagedType().FullName} "+ exception.ToString());
                            }
                        }
                    }
                    
                    types.Dispose();
                }
            }
            if (sb.Length > 0)
            {
                string value = sb.ToString();
                Logging.DebugEvaluation($"Section ({typeName}):\n{value}");
                builder.AddSection(typeName, new ValueBinding<string>(() => value));
            }
            else
            {
                builder.AddTagSection(typeName);
            }
            return;
        }
     
        Logging.DebugEvaluation($"Component type <{componentType.GetManagedType().FullName}> not supported");
    }

        private static string GetValueString(object value, EntityManager entityManager, PrefabSystem prefabSystem) {
        Type type = value.GetType();
        StringBuilder sb = new();
        foreach (FieldInfo field in type.GetRuntimeFields())
        {
            if (field.IsStatic)
            {
                continue;
            }
            var val = field.GetValue(value);
            sb.Append("\t").Append(field.Name).Append(" = ").AppendLine(field.GetValue(value).ToString());
            if (val is Entity e && e.ExistsIn(entityManager))
            {
                NativeArray<ComponentType> types = entityManager.GetComponentTypes(e);
                if (types.Contains(typeof(PrefabRef)))
                {
                    PrefabRef prefabRef = entityManager.GetComponentData<PrefabRef>(e);
                    PrefabBase prefabData = prefabSystem.GetPrefab<PrefabBase>(prefabRef.m_Prefab);
                    sb.AppendLine($"\t\tSource prefab name: {prefabData.name}");
                }
                sb.AppendLine($"\t\tEntity components ({types.Length}): ").Append("\t\t").AppendLine(string.Join(", ", types.ToArray().Select(t => t.GetManagedType().FullName)));
                
                foreach (ComponentType componentType in types)
                {
                    if (componentType.IsComponent)
                    {
                        try
                        {
                            MethodInfo method = typeof(EntityManager).GetMethod(nameof(EntityManager.GetComponentData), new Type[] { typeof(Entity) });
                            MethodInfo generic = method.MakeGenericMethod(componentType.GetManagedType());
                            object componentValue = generic.Invoke(entityManager, new object[] { e });
                            sb.Append("\t\t  ").AppendLine(componentType.GetManagedType().Name).Append(GetComponentData(componentValue, 3));
                        }
                        catch (Exception exception)
                        {
                            Logging.Error($"ComponentType: {componentType.GetManagedType().FullName} "+ exception.ToString());
                        }
                    }
                }
                
                types.Dispose();
            }
        }
        return sb.ToString();
    }

        private static string GetComponentData(object value, int indent) {
        StringBuilder sb = new StringBuilder();
        Type type = value.GetType();

        foreach (FieldInfo field in type.GetRuntimeFields())
        {
            if (field.IsStatic)
            {
                continue;
            }
            if (field.FieldType.Name.Equals("EntityArchetype"))
            {
                sb.AddIndent(indent).Append(field.Name).Append(" = ").AppendLine("<EntityArchetypeValue>");
                continue;
            }
            try
            {
                sb.AddIndent(indent).Append(field.Name).Append(" = ").AppendLine(field.GetValue(value).ToString());
            }
            catch (Exception e)
            {
                Logging.Info($"Error while getting value of field with name: {field.Name}" + e.ToString());
            }
        }
        if (sb.Length == 0)
        {
            sb.AddIndent(indent).AppendLine("[TAG]");
        }

        return sb.ToString();
    }

        private static StringBuilder AddIndent(this StringBuilder sb, int value) {
        for (int i = 0; i < value; i++)
        {
            sb.Append("\t");
        }
        return sb;
    }
    
    
    }
}
