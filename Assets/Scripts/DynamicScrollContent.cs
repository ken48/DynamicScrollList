using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Todo: common logic for horizontal and vertical axis
// Стоит пока начинать старт открутки снизу, чтобы обощить знак изменения как для горизонта, так и для вертикали.

public class DynamicScrollContent : IDisposable
{
    // Todo: axis masking
    static readonly Dictionary<ViewportEdge, Func<Rect, Vector2>> RectEdgePosition = new Dictionary<ViewportEdge, Func<Rect, Vector2>>
    {
        { ViewportEdge.Head, r => r.max },
        { ViewportEdge.Tail, r => r.min },
    };

    // Todo: axis masking
    static readonly Dictionary<ViewportEdge, Func<Rect, float, bool>> ViewportCheckEdge = new Dictionary<ViewportEdge, Func<Rect, float, bool>>
    {
        { ViewportEdge.Head, (v, p) => v.yMax > p },
        { ViewportEdge.Tail, (v, p) => v.yMin < p },
    };

    readonly DynamicScrollItemWidgetsPool _itemWidgetsPool;
    readonly List<IDynamicScrollItemWidget> _widgets;
    readonly RectTransform _viewport;
    readonly RectTransform _node;
    readonly Vector2 _spacing;
    readonly Dictionary<ViewportEdge, Vector2> _edgesLastPositions;

    public DynamicScrollContent(IDynamicScrollItemWidgetProvider itemWidgetProvider, RectTransform viewport, RectTransform node, Vector2 spacing)
    {
        _itemWidgetsPool = new DynamicScrollItemWidgetsPool(itemWidgetProvider, node);
        _viewport = viewport;
        _node = node;
        _spacing = spacing;
        _widgets = new List<IDynamicScrollItemWidget>();
        _edgesLastPositions = new Dictionary<ViewportEdge, Vector2>
        {
            { ViewportEdge.Head, Vector2.zero },
            { ViewportEdge.Tail, _spacing },
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
        float sign = -Mathf.Sign(DynamicScrollViewport.InflationShifts[edge]);

        float startPos;
        if (!IsEmpty())
        {
            RectTransform rectTransform = GetEdgeWidget(edge).rectTransform;
            Vector2 edgePosition = RectEdgePosition[edge](rectTransform.rect);
            startPos = rectTransform.TransformPoint(edgePosition + _spacing * sign).y; // Todo: axis masking
        }
        else
        {
            startPos = _node.TransformPoint(_edgesLastPositions[edge] + _spacing * sign).y; // Todo: axis masking
        }

        return ViewportCheckEdge[edge](viewportWorldRect, startPos);
    }

    public void Inflate(ViewportEdge edge, IDynamicScrollItem item)
    {
        IDynamicScrollItemWidget widget = _itemWidgetsPool.GetWidget(item);
        widget.Fill(item);
        LayoutRebuilder.ForceRebuildLayoutImmediate(widget.rectTransform);
        RectTransform widgetRectTransform = widget.rectTransform;

        Vector2 newPosition = _edgesLastPositions[edge];
        switch (edge)
        {
            case ViewportEdge.Head:
                _widgets.Insert(0, widget);
                newPosition += _spacing + widgetRectTransform.rect.size;
                _edgesLastPositions[edge] = newPosition;
                break;

            case ViewportEdge.Tail:
                _widgets.Add(widget);
                newPosition -= _spacing;
                _edgesLastPositions[edge] = newPosition - widgetRectTransform.rect.size;
                break;
        }

        widgetRectTransform.anchoredPosition = newPosition * Vector2.up; // Todo: axis masking
    }

    public bool CanDeflate(ViewportEdge edge, Rect viewportWorldRect)
    {
        return !IsEmpty() && !IsWidgetOverlapsViewport(GetEdgeWidget(edge), viewportWorldRect);
    }

    public void Deflate(ViewportEdge edge)
    {
        IDynamicScrollItemWidget widget = GetEdgeWidget(edge);
        float sign = Mathf.Sign(DynamicScrollViewport.InflationShifts[edge]);
        _edgesLastPositions[edge] += (widget.rectTransform.rect.size + _spacing) * sign * Vector2.up; // Todo: axis masking

        _itemWidgetsPool.ReturnWidget(widget);
        _widgets.Remove(widget);
    }

    public Vector2 GetEdgeDelta(ViewportEdge edge)
    {
        Vector2 edgeLastPosition = -_edgesLastPositions[edge];
        switch (edge)
        {
            case ViewportEdge.Head:
                Vector2 headEdgePosition = edgeLastPosition;
                if (_node.anchoredPosition.y < headEdgePosition.y) // Todo: axis masking
                    return headEdgePosition - _node.anchoredPosition;
                break;

            case ViewportEdge.Tail:
                Vector2 bottomEdgePosition = edgeLastPosition - _viewport.rect.size;
                if (_node.anchoredPosition.y > bottomEdgePosition.y) // Todo: axis masking
                    return bottomEdgePosition - _node.anchoredPosition;
                break;
        }
        return Vector2.zero;
    }

    bool IsEmpty()
    {
        return _widgets.Count == 0;
    }

    IDynamicScrollItemWidget GetEdgeWidget(ViewportEdge edge)
    {
        return _widgets[edge == ViewportEdge.Head ? 0 : _widgets.Count - 1];
    }

    static bool IsWidgetOverlapsViewport(IDynamicScrollItemWidget widget, Rect viewportWorldRect)
    {
        return RectHelpers.GetWorldRect(widget.rectTransform).Overlaps(viewportWorldRect);
    }
}

static class RectHelpers
{
    public static Rect GetWorldRect(RectTransform rectTransform)
    {
        Rect rect = rectTransform.rect;
        Vector2 worldRectMin = rectTransform.TransformPoint(rect.min);
        Vector2 worldRectMax = rectTransform.TransformPoint(rect.max);
        return Rect.MinMaxRect(worldRectMin.x, worldRectMin.y, worldRectMax.x, worldRectMax.y);
    }
}
