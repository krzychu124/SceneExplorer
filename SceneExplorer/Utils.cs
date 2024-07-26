using System;
using Colossal.Logging.Utils;
using Game.Common;
using Game.Tools;
using Unity.Entities;

namespace SceneExplorer
{
    public static class Utils
    {
        internal static void ChangeHighlighting_MainThread(this EntityManager entityManager, Entity entity, ChangeMode mode)
        {
            if (entity == Entity.Null || !entityManager.Exists(entity))
            {
                return;
            }
            bool changed = false;
            if (mode == ChangeMode.AddHighlight && !entityManager.HasComponent<Highlighted>(entity))
            {
                entityManager.AddComponent<Highlighted>(entity);
                changed = true;
            }
            else if (mode == ChangeMode.RemoveHighlight && entityManager.HasComponent<Highlighted>(entity))
            {
                entityManager.RemoveComponent<Highlighted>(entity);
                changed = true;
            }
            if (changed && !entityManager.HasComponent<BatchesUpdated>(entity))
            {
                entityManager.AddComponent<BatchesUpdated>(entity);
            }
        }

        internal enum ChangeMode
        {
            AddHighlight,
            RemoveHighlight,
        }

        internal static string GetTypeName(this Type type)
        {
            return ModEntryPoint._settings.UseShortComponentNames ? type.Name : type.GetFriendlyName();
        } 
    }
}
