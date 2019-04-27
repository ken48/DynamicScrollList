﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


// Todo: DIVIDE Data viewport and widget viewport.
// вьюпорт данных всегда одинаковый по сути от начала к концу (begin - end)
// вьюпорт верстки должен быть относительным (верх/низ право/лево)... Фиг знает..


public class DynamicScrollContent : MonoBehaviour
{
    enum Axis
    {
        X,
        Y,
    }

    static readonly Dictionary<Axis, Axis> OrthoAxes = new Dictionary<Axis, Axis>
    {
        { Axis.X, Axis.Y },
        { Axis.Y, Axis.X },
    };

    static readonly Dictionary<Axis, Vector2> AxisMasks = new Dictionary<Axis, Vector2>
    {
        { Axis.X, Vector2.right },
        { Axis.Y, Vector2.up },
    };

    // Todo: direction (swap)
    static readonly Dictionary<DynamicScrollViewport.Edge, Func<Rect, Vector2>> RectEdgePosition =
        new Dictionary<DynamicScrollViewport.Edge, Func<Rect, Vector2>>
    {
        { DynamicScrollViewport.Edge.Head, r => r.min },
        { DynamicScrollViewport.Edge.Tail, r => r.max },
    };

    // Todo: direction (sign)
    static readonly Dictionary<DynamicScrollViewport.Edge, Func<Vector2, Vector2, Axis, bool>> ViewportCheckEdge =
        new Dictionary<DynamicScrollViewport.Edge, Func<Vector2, Vector2, Axis, bool>>
    {
        { DynamicScrollViewport.Edge.Head, (v, p, a) => GetVectorComponent(v, a) < GetVectorComponent(p, a) },
        { DynamicScrollViewport.Edge.Tail, (v, p, a) => GetVectorComponent(v, a) > GetVectorComponent(p, a) },
    };

    static readonly Dictionary<DynamicScrollViewport.Edge, Vector2> AnchorBases =
        new Dictionary<DynamicScrollViewport.Edge, Vector2>
    {
        { DynamicScrollViewport.Edge.Head, Vector2.zero },
        { DynamicScrollViewport.Edge.Tail, Vector2.one },
    };

    [SerializeField]
    Axis _axis;
    [SerializeField]
    DynamicScrollViewport.Edge _startEdge;
    [SerializeField]
    float _spacing;

    RectTransform _node;
    DynamicScrollItemWidgetsPool _itemWidgetsPool;
    List<IDynamicScrollItemWidget> _widgets;
    Dictionary<DynamicScrollViewport.Edge, Vector2> _edgesLastPositions;
    Vector2 _spacingVector;

    public void Init(IDynamicScrollItemWidgetProvider itemWidgetProvider)
    {
        _node = (RectTransform)transform;
        _itemWidgetsPool = new DynamicScrollItemWidgetsPool(itemWidgetProvider, _node);
        _spacingVector = AxisMasks[_axis] * _spacing;

        _widgets = new List<IDynamicScrollItemWidget>();
        _edgesLastPositions = new Dictionary<DynamicScrollViewport.Edge, Vector2>
        {
            { DynamicScrollViewport.Edge.Head, Vector2.zero },
            { DynamicScrollViewport.Edge.Tail, _spacingVector * DynamicScrollViewport.EdgeInflationSigns[_startEdge] },
        };

        SetPivotAndAnchors(_node);
    }

    public void Shutdown()
    {
        _itemWidgetsPool.Dispose();
    }

    public DynamicScrollViewport.Edge Move(Vector2 delta)
    {
        Vector2 deltaAxis = delta * AxisMasks[_axis];
        _node.anchoredPosition += deltaAxis;

        var directionSign = (int)Mathf.Sign(GetVectorComponent(deltaAxis, _axis));
        var inflationSign = -directionSign;
        return DynamicScrollViewport.EdgeInflationSigns.FirstOrDefault(kv => kv.Value == inflationSign).Key;
    }

    public bool CanInflate(DynamicScrollViewport.Edge edge, Rect viewportWorldRect)
    {
        int sign = DynamicScrollViewport.EdgeInflationSigns[edge];
        Vector2 startPos = _node.TransformPoint(_edgesLastPositions[edge] + _spacingVector * sign);
        return ViewportCheckEdge[edge](RectEdgePosition[edge](viewportWorldRect), startPos, _axis);
    }

    public void Inflate(DynamicScrollViewport.Edge edge, IDynamicScrollItem item)
    {
        IDynamicScrollItemWidget widget = _itemWidgetsPool.GetWidget(item);
        widget.Fill(item);

        RectTransform widgetRectTransform = widget.rectTransform;
        SetPivotAndAnchors(widgetRectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(widget.rectTransform);

        int sign = DynamicScrollViewport.EdgeInflationSigns[edge];
        AddEdgeLastPosition(widget.rectTransform, sign, edge);
        widgetRectTransform.anchoredPosition = _edgesLastPositions[edge];

        switch (edge)
        {
            case DynamicScrollViewport.Edge.Head:
                _widgets.Insert(0, widget);
                break;

            case DynamicScrollViewport.Edge.Tail:
                _widgets.Add(widget);
                Vector2 axisMask = AxisMasks[_axis];
                widgetRectTransform.anchoredPosition -= widget.rectTransform.rect.size * axisMask;
                break;
        }
    }

    public bool CanDeflate(DynamicScrollViewport.Edge edge, Rect viewportWorldRect)
    {
        return !IsEmpty() && !DynamicScrollHelpers.GetWorldRect(GetEdgeWidget(edge).rectTransform).Overlaps(viewportWorldRect);
    }

    public void Deflate(DynamicScrollViewport.Edge edge)
    {
        IDynamicScrollItemWidget widget = GetEdgeWidget(edge);
        int sign = -DynamicScrollViewport.EdgeInflationSigns[edge];
        AddEdgeLastPosition(widget.rectTransform, sign, edge);

        _itemWidgetsPool.ReturnWidget(widget);
        _widgets.Remove(widget);
    }

    public Vector2 GetEdgeDelta(RectTransform viewport, DynamicScrollViewport.Edge edge)
    {
        Rect viewportRect = viewport.rect;
        Vector2 result = -_edgesLastPositions[edge] - _node.anchoredPosition + RectEdgePosition[edge](viewportRect) + viewportRect.size * 0.5f;
        Vector2 resultAxis = result * AxisMasks[_axis];
        var resultSign = (int)Mathf.Sign(GetVectorComponent(resultAxis, _axis));
        return resultSign == DynamicScrollViewport.EdgeInflationSigns[edge] ? resultAxis : Vector2.zero;
    }

    void AddEdgeLastPosition(RectTransform widgetRectTransform, int sign, DynamicScrollViewport.Edge edge)
    {
        Vector2 axisMask = AxisMasks[_axis];
        _edgesLastPositions[edge] += (widgetRectTransform.rect.size + _spacingVector) * sign * axisMask;
    }

    //
    // Helpers
    //

    bool IsEmpty()
    {
        return _widgets.Count == 0;
    }

    IDynamicScrollItemWidget GetEdgeWidget(DynamicScrollViewport.Edge edge)
    {
        switch (edge)
        {
            case DynamicScrollViewport.Edge.Head:
                return _widgets[0];
            case DynamicScrollViewport.Edge.Tail:
                return _widgets[_widgets.Count - 1];
            default:
                throw new Exception("Unhandled edge type " + edge);
        }
    }

    void SetPivotAndAnchors(RectTransform rectTransform)
    {
        Vector2 pivotBase = Vector2.one * 0.5f;
        Vector2 axisMask = AxisMasks[_axis];
        Vector2 orthoAxisMask = AxisMasks[OrthoAxes[_axis]];
        Vector2 baseVector = AnchorBases[_startEdge];
        Vector2 tailBase = AnchorBases[DynamicScrollViewport.Edge.Tail];

        rectTransform.anchorMin = baseVector * axisMask;
        rectTransform.anchorMax = rectTransform.anchorMin + tailBase * orthoAxisMask;
        rectTransform.pivot = baseVector * axisMask + pivotBase * orthoAxisMask;
    }

    static float GetVectorComponent(Vector2 vector, Axis axis)
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
