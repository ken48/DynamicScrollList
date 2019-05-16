﻿using System.Collections.Generic;
using UnityEngine;

namespace DynamicScroll
{
    public enum WidgetsAlignment
    {
        Left,
        Right,
        Bottom,
        Top,
    }
}

namespace DynamicScroll.Internal
{
    //
    // ItemsEdge
    //

    internal enum ItemsEdge
    {
        Head,
        Tail,
    }

    internal static class ItemsEdgeDesc
    {
        public static readonly Dictionary<ItemsEdge, ItemsEdge> Opposites = new Dictionary<ItemsEdge, ItemsEdge>
        {
            { ItemsEdge.Head, ItemsEdge.Tail },
            { ItemsEdge.Tail, ItemsEdge.Head },
        };

        public static readonly Dictionary<ItemsEdge, int> InflationSigns = new Dictionary<ItemsEdge, int>
        {
            { ItemsEdge.Head, -1 },
            { ItemsEdge.Tail, 1 },
        };
    }

    //
    // Helpers
    //

    internal static class DynamicScrollHelpers
    {
        // Todo: move to DynamicContent somehow... The problem is optimization only
        // How not to calculate this each time
        public static Rect GetWorldRect(RectTransform rectTransform)
        {
            Rect rect = rectTransform.rect;
            Vector2 worldRectMin = rectTransform.TransformPoint(rect.min);
            Vector2 worldRectMax = rectTransform.TransformPoint(rect.max);
            return Rect.MinMaxRect(worldRectMin.x, worldRectMin.y, worldRectMax.x, worldRectMax.y);
        }

        public static bool CheckVectorMagnitude(Vector2 vector)
        {
            return vector.sqrMagnitude >= 1e-6f;
        }
    }
}