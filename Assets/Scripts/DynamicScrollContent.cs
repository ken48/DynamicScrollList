using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Todo: set pivots and anchors in code for widgets depends on it's growth direction and axis

public class DynamicScrollContent : IDisposable
{
    // Todo: direction (swap)
    static readonly Dictionary<ViewportEdge, Func<Rect, Vector2>> RectEdgePosition = new Dictionary<ViewportEdge, Func<Rect, Vector2>>
    {
        { ViewportEdge.Head, r => r.min },
        { ViewportEdge.Tail, r => r.max },
    };

    // Todo: direction (sign)
    static readonly Dictionary<ViewportEdge, Func<Vector2, Vector2, bool>> ViewportCheckEdge = new Dictionary<ViewportEdge, Func<Vector2, Vector2, bool>>
    {
        { ViewportEdge.Head, (v, p) => DynamicScrollHelpers.GetVectorComponent(v) < DynamicScrollHelpers.GetVectorComponent(p) },
        { ViewportEdge.Tail, (v, p) => DynamicScrollHelpers.GetVectorComponent(v) > DynamicScrollHelpers.GetVectorComponent(p) },
    };

    readonly DynamicScrollItemWidgetsPool _itemWidgetsPool;
    readonly List<IDynamicScrollItemWidget> _widgets;
    readonly RectTransform _viewport;
    readonly RectTransform _node;
    readonly Vector2 _spacing;
    readonly Dictionary<ViewportEdge, Vector2> _edgesLastPositions;
    readonly Vector2 _axisMask;

    public DynamicScrollContent(IDynamicScrollItemWidgetProvider itemWidgetProvider, RectTransform viewport, RectTransform node,
        float spacing, Vector2 axisMask)
    {
        _itemWidgetsPool = new DynamicScrollItemWidgetsPool(itemWidgetProvider, node);
        _viewport = viewport;
        _node = node;
        _spacing = spacing * axisMask;
        _axisMask = axisMask;
        _widgets = new List<IDynamicScrollItemWidget>();
        _edgesLastPositions = new Dictionary<ViewportEdge, Vector2>
        {
            { ViewportEdge.Head, Vector2.zero },
            { ViewportEdge.Tail, -_spacing },
        };
    }

    public void Dispose()
    {
        _itemWidgetsPool.Dispose();
    }

    public void Move(Vector2 delta)
    {
        _node.anchoredPosition += delta;
    }

    public bool CanInflate(ViewportEdge edge, Rect viewportWorldRect)
    {
        int sign = DynamicScrollViewport.InflationSigns[edge];
        Vector2 startPos = _node.TransformPoint(_edgesLastPositions[edge] + _spacing * sign);
        return ViewportCheckEdge[edge](RectEdgePosition[edge](viewportWorldRect) * _axisMask, startPos * _axisMask);
    }

    public void Inflate(ViewportEdge edge, IDynamicScrollItem item)
    {
        IDynamicScrollItemWidget widget = _itemWidgetsPool.GetWidget(item);
        widget.Fill(item);
        LayoutRebuilder.ForceRebuildLayoutImmediate(widget.rectTransform);
        RectTransform widgetRectTransform = widget.rectTransform;

        float sign = DynamicScrollViewport.InflationSigns[edge];
        Vector2 newPosition = _edgesLastPositions[edge] + (widget.rectTransform.rect.size + _spacing) * sign * _axisMask;
        _edgesLastPositions[edge] = newPosition;
        widgetRectTransform.anchoredPosition = newPosition;

        switch (edge)
        {
            case ViewportEdge.Head:
                _widgets.Insert(0, widget);
                break;

            case ViewportEdge.Tail:
                _widgets.Add(widget);
                widgetRectTransform.anchoredPosition -= widget.rectTransform.rect.size * _axisMask;
                break;
        }
    }

    public bool CanDeflate(ViewportEdge edge, Rect viewportWorldRect)
    {
        return !IsEmpty() && !DynamicScrollHelpers.GetWorldRect(GetEdgeWidget(edge).rectTransform).Overlaps(viewportWorldRect);
    }

    public void Deflate(ViewportEdge edge)
    {
        IDynamicScrollItemWidget widget = GetEdgeWidget(edge);
        float sign = -DynamicScrollViewport.InflationSigns[edge];
        _edgesLastPositions[edge] += (widget.rectTransform.rect.size + _spacing) * sign * _axisMask;

        _itemWidgetsPool.ReturnWidget(widget);
        _widgets.Remove(widget);
    }

    public Vector2 GetEdgeDelta(ViewportEdge edge)
    {
        Rect viewportRect = _viewport.rect;
        Vector2 result = -_edgesLastPositions[edge] - _node.anchoredPosition + RectEdgePosition[edge](viewportRect) + viewportRect.size * 0.5f;
        float resultF = DynamicScrollHelpers.GetVectorComponent(result * _axisMask);
        if ((int)Mathf.Sign(resultF) != DynamicScrollViewport.InflationSigns[edge])
            return Vector2.zero;
        return result;
    }

    bool IsEmpty()
    {
        return _widgets.Count == 0;
    }

    IDynamicScrollItemWidget GetEdgeWidget(ViewportEdge edge)
    {
        return _widgets[edge == ViewportEdge.Head ? 0 : _widgets.Count - 1];
    }
}
