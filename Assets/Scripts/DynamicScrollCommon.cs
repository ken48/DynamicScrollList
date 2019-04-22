﻿using System;
using System.Collections.Generic;
using UnityEngine;

//
// Description
//

public static class DynamicScrollDescription
{
    public enum Axis
    {
        Horizontal,
        Vertical,
    }

    public enum Edge
    {
        Head,
        Tail,
    }

    public static readonly Dictionary<Edge, Edge> OppositeEdges = new Dictionary<Edge, Edge>
    {
        { Edge.Head, Edge.Tail },
        { Edge.Tail, Edge.Head },
    };

    public static readonly Dictionary<Edge, int> EdgeInflationSigns = new Dictionary<Edge, int>
    {
        { Edge.Head, -1 },
        { Edge.Tail, 1 },
    };

    public static readonly Dictionary<Axis, Vector2> AxisMasks = new Dictionary<Axis, Vector2>
    {
        { Axis.Horizontal, Vector2.right },
        { Axis.Vertical, Vector2.up },
    };
}

//
// Interfaces
//

public interface IDynamicScrollItem
{
}

public interface IDynamicScrollItemProvider
{
    IDynamicScrollItem GetItemByIndex(int index);
}

public interface IDynamicScrollItemWidget
{
    GameObject go { get; }
    RectTransform rectTransform { get; }
    void Fill(IDynamicScrollItem item);
}

public interface IDynamicScrollItemWidgetProvider
{
    IDynamicScrollItemWidget GetNewItemWidget(IDynamicScrollItem item, Transform rootNode);
    void ReturnItemWidget(IDynamicScrollItemWidget itemWidget);
}

//
// Helpers
//

public static class DynamicScrollHelpers
{
    public static float GetVectorComponent(Vector2 vector, DynamicScrollDescription.Axis axis)
    {
        Vector2 maskedVector = vector * DynamicScrollDescription.AxisMasks[axis];

        // One of them is always zero
        return maskedVector.x + maskedVector.y;
    }

    public static Rect GetWorldRect(RectTransform rectTransform)
    {
        Rect rect = rectTransform.rect;
        Vector2 worldRectMin = rectTransform.TransformPoint(rect.min);
        Vector2 worldRectMax = rectTransform.TransformPoint(rect.max);
        return Rect.MinMaxRect(worldRectMin.x, worldRectMin.y, worldRectMax.x, worldRectMax.y);
    }
}
