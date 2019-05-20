using System;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicScroll.Internal
{
    internal enum Axis
    {
        X,
        Y,
    }

    internal static class AxisMaskDesc
    {
        public static readonly Dictionary<Axis, Vector2> AxisMasks = new Dictionary<Axis, Vector2>
        {
            { Axis.X, Vector2.right },
            { Axis.Y, Vector2.up },
        };

        public static readonly Dictionary<WidgetsAlignment, Axis> WidgetsAlignmentAxis = new Dictionary<WidgetsAlignment, Axis>
        {
            { WidgetsAlignment.Left, Axis.X },
            { WidgetsAlignment.Right, Axis.X },
            { WidgetsAlignment.Bottom, Axis.Y },
            { WidgetsAlignment.Top, Axis.Y },
        };
    }

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

    internal static class Helpers
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

        public static bool IsZeroValue(float value)
        {
            return Mathf.Abs(value) < 0.001f;
        }

        public static float GetVectorComponent(Vector2 vector, Axis axis)
        {
            switch (axis)
            {
                case Axis.X:
                    return vector.x;
                case Axis.Y:
                    return vector.y;
                default:
                    throw new Exception("Unhandled axis type " + axis);
            }
        }
    }
}
