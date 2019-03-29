using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DynamicScrollContent : IDisposable
{
    readonly DynamicScrollItemWidgetsPool _itemWidgetsPool;
    readonly List<IDynamicScrollItemWidget> _widgets;
    readonly RectTransform _viewport;
    readonly RectTransform _node;
    readonly float _spacing;
    readonly Dictionary<ViewportEdge, float> _edgesLastPositions;

    public DynamicScrollContent(IDynamicScrollItemWidgetProvider itemWidgetProvider, RectTransform viewport, RectTransform node, float spacing)
    {
        _itemWidgetsPool = new DynamicScrollItemWidgetsPool(itemWidgetProvider, node);
        _viewport = viewport;
        _node = node;
        _spacing = spacing;
        _widgets = new List<IDynamicScrollItemWidget>();
        _edgesLastPositions = new Dictionary<ViewportEdge, float>
        {
            { ViewportEdge.Head, 0f },
            { ViewportEdge.Tail, _spacing },
        };
    }

    public void Dispose()
    {
        _itemWidgetsPool.Dispose();
    }

    public void Move(float delta)
    {
        _node.anchoredPosition += Vector2.up * delta;
    }

    public bool CanInflate(ViewportEdge edge, Rect viewportWorldRect)
    {
        float sign = -Mathf.Sign(DynamicScrollViewport.InflationShifts[edge]);

        float startPos;
        if (!IsEmpty())
        {
            RectTransform rectTransform = _widgets[GetEdgeIndex(edge)].rectTransform;
            Vector2 edgePosition = edge == ViewportEdge.Head ? rectTransform.rect.max : rectTransform.rect.min;
            startPos = rectTransform.TransformPoint(edgePosition + Vector2.up * _spacing * sign).y;
        }
        else
        {
            startPos = _node.TransformPoint(Vector2.up * _edgesLastPositions[edge] + Vector2.up * _spacing * sign).y;
        }

        return edge == ViewportEdge.Head ? viewportWorldRect.yMax > startPos : viewportWorldRect.yMin < startPos;
    }

    public void Inflate(ViewportEdge edge, IDynamicScrollItem item)
    {
        IDynamicScrollItemWidget widget = _itemWidgetsPool.GetWidget(item);
        widget.Fill(item);
        LayoutRebuilder.ForceRebuildLayoutImmediate(widget.rectTransform);
        RectTransform widgetRectTransform = widget.rectTransform;

        float newPosition = _edgesLastPositions[edge];
        switch (edge)
        {
            case ViewportEdge.Head:
                _widgets.Insert(0, widget);
                newPosition += _spacing + widgetRectTransform.rect.height;
                _edgesLastPositions[edge] = newPosition;
                break;

            case ViewportEdge.Tail:
                _widgets.Add(widget);
                newPosition -= _spacing;
                _edgesLastPositions[edge] = newPosition - widgetRectTransform.rect.height;
                break;
        }

        widgetRectTransform.anchoredPosition = Vector2.up * newPosition;
    }

    public bool CanDeflate(ViewportEdge edge, Rect viewportWorldRect)
    {
        return !IsEmpty() && !IsWidgetOverlapsViewport(_widgets[GetEdgeIndex(edge)], viewportWorldRect);
    }

    public void Deflate(ViewportEdge edge)
    {
        int index = GetEdgeIndex(edge);
        IDynamicScrollItemWidget widget = _widgets[index];
        float sign = Mathf.Sign(DynamicScrollViewport.InflationShifts[edge]);
        _edgesLastPositions[edge] += (widget.rectTransform.rect.height + _spacing) * sign;

        _itemWidgetsPool.ReturnWidget(widget);
        _widgets.RemoveAt(index);
    }

    public float GetEdgeDelta(ViewportEdge edge)
    {
        float edgeLastPosition = -_edgesLastPositions[edge];
        switch (edge)
        {
            case ViewportEdge.Head:
                float headEdgePosition = edgeLastPosition;
                if (_node.anchoredPosition.y < headEdgePosition)
                    return (Vector2.up * headEdgePosition - _node.anchoredPosition).y;
                break;

            case ViewportEdge.Tail:
                float bottomEdgePosition = edgeLastPosition - _viewport.rect.height;
                if (_node.anchoredPosition.y > bottomEdgePosition)
                    return (Vector2.up * bottomEdgePosition - _node.anchoredPosition).y;
                break;
        }
        return 0f;
    }

    bool IsEmpty()
    {
        return _widgets.Count == 0;
    }

    int GetEdgeIndex(ViewportEdge edge)
    {
        return edge == ViewportEdge.Head ? 0 : _widgets.Count - 1;
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
