﻿using Colossal.Logging.Utils;
using Game.Common;
using Game.Tools;
using System;
using SceneExplorer.ToBeReplaced.Helpers;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SceneExplorer
{
    public static class Utils
    {
        internal static void ChangeHighlighting_MainThread(this EntityManager entityManager, Entity entity, ChangeMode mode)
        {
            if (!entity.ExistsIn(entityManager))
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
            return ModEntryPoint.Settings.UseShortComponentNames ? type.Name : type.GetFriendlyName();
        }

        internal static Matrix4x4 GetScalingMatrix()
        {
            var normalizedScaling = ModEntryPoint.Settings.NormalizedScaling;
            Vector3 scaleVector = new Vector3(normalizedScaling, normalizedScaling, 1.0f);
            GUIUtility.ScaleAroundPivot(scaleVector, new Vector2());
            return GUI.matrix;
        }

        internal static Vector2 GetTransformedMousePosition()
        {
            Vector2 mousePosition = Mouse.current.position.value;
            var normalizedScaling = ModEntryPoint.Settings.NormalizedScaling;

            return new Vector2(mousePosition.x / normalizedScaling, (Screen.height - mousePosition.y) / normalizedScaling);
        }
    }
}
