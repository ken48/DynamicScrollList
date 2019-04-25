using System;
using System.Collections.Generic;
using UnityEngine;

//
// Description
//

public static class DynamicScrollDescription
{
    public enum Axis
    {
        X,
        Y,
    }

    public static readonly Dictionary<Axis, Axis> OrthoAxes = new Dictionary<Axis, Axis>
    {
        { Axis.X, Axis.Y },
        { Axis.Y, Axis.X },
    };

    public static readonly Dictionary<Axis, Vector2> AxisMasks = new Dictionary<Axis, Vector2>
    {
        { Axis.X, Vector2.right },
        { Axis.Y, Vector2.up },
    };

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
    public static Rect GetWorldRect(RectTransform rectTransform)
    {
        Rect rect = rectTransform.rect;
        Vector2 worldRectMin = rectTransform.TransformPoint(rect.min);
        Vector2 worldRectMax = rectTransform.TransformPoint(rect.max);
        return Rect.MinMaxRect(worldRectMin.x, worldRectMin.y, worldRectMax.x, worldRectMax.y);
    }
}
