#if DEBUG_PP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Common;
using Game.Input;
using Game.Tools;
using Unity.Entities;
using Debug = UnityEngine.Debug;

namespace SceneExplorer
{
    public static class Utils
    {
        internal static void ChangeHighlighting_MainThread(this EntityManager entityManager, Entity entity, ChangeMode mode) {
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
    }
#endif
}