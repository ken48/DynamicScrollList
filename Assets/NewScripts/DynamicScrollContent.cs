using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DynamicScrollContent : IDisposable
{
    DynamicScrollItemWidgetsPool _itemWidgetsPool;
    List<IDynamicScrollItemWidget> _widgets;
    RectTransform _viewport;
    RectTransform _node;
    float _spacing;
    float _lastHeadPosition;
    float _lastTailPosition;
    bool _headEdge;
    bool _tailEdge;

    public DynamicScrollContent(IDynamicScrollItemWidgetProvider itemWidgetProvider, RectTransform viewport, RectTransform node, float spacing)
    {
        _itemWidgetsPool = new DynamicScrollItemWidgetsPool(itemWidgetProvider, node);
        _viewport = viewport;
        _node = node;
        _spacing = spacing;
        _widgets = new List<IDynamicScrollItemWidget>();

        _lastHeadPosition = 0f;
        _lastTailPosition = _spacing; // Shift first element to top
        _headEdge = _tailEdge = true;
    }

    public void Dispose()
    {
        _itemWidgetsPool.Dispose();
    }

    public void Move(float delta)
    {
        _node.anchoredPosition = _node.anchoredPosition + Vector2.up * delta;
    }

    public float CheckEdges()
    {
        if (_headEdge)
        {
            float headEdgePosition = -_lastHeadPosition;
            if (_node.anchoredPosition.y < headEdgePosition)
                return (Vector2.up * headEdgePosition - _node.anchoredPosition).y;
        }
        else if (_tailEdge)
        {
            float bottomEdgePosition = -_lastTailPosition - _viewport.rect.height;
            if (_node.anchoredPosition.y > bottomEdgePosition)
                return (Vector2.up * bottomEdgePosition - _node.anchoredPosition).y;
        }

        return 0f;
    }

    public void PushHead(IDynamicScrollItem item)
    {
        AddWidget(item, GetHeadIndex());

        IDynamicScrollItemWidget newHeadWidget = _widgets[GetHeadIndex()];
        _lastHeadPosition += _spacing + newHeadWidget.rectTransform.rect.height;
        newHeadWidget.rectTransform.anchoredPosition = Vector2.up * _lastHeadPosition;
    }

    public void PushTail(IDynamicScrollItem item)
    {
        AddWidget(item, GetTailIndex() + 1);

        IDynamicScrollItemWidget newTailWidget = _widgets[GetTailIndex()];
        _lastTailPosition -= _spacing;
        newTailWidget.rectTransform.anchoredPosition = Vector2.up * _lastTailPosition;
        _lastTailPosition -= newTailWidget.rectTransform.rect.height;
    }

    public void PopHead()
    {
        int headIndex = GetHeadIndex();
        _lastHeadPosition -= _widgets[headIndex].rectTransform.rect.height + _spacing;

        RemoveWidget(headIndex);

        _headEdge = false;
    }

    public void PopTail()
    {
        int tailIndex = GetTailIndex();
        RectTransform tailWidgetRectTransform = _widgets[tailIndex].rectTransform;
        _lastTailPosition += tailWidgetRectTransform.rect.height + _spacing;

        RemoveWidget(tailIndex);

        _tailEdge = false;
    }

    public bool CanPushHead(Rect viewportWorldRect)
    {
        float startPos;
        if (!IsEmpty())
        {
            RectTransform headRectTransform = _widgets[GetHeadIndex()].rectTransform;
            startPos = headRectTransform.TransformPoint(headRectTransform.rect.max + Vector2.up * _spacing).y;
        }
        else
        {
            startPos = _node.TransformPoint(Vector2.up * _lastHeadPosition).y;
        }

        return viewportWorldRect.yMax > startPos;
    }

    public bool CanPushTail(Rect viewportWorldRect)
    {
        float startPos;
        if (!IsEmpty())
        {
            RectTransform tailRectTransform = _widgets[GetTailIndex()].rectTransform;
            startPos = tailRectTransform.TransformPoint(tailRectTransform.rect.min + Vector2.down * _spacing).y;
        }
        else
        {
            startPos = _node.TransformPoint(Vector2.up * _lastTailPosition).y;
        }

        return viewportWorldRect.yMin < startPos;
    }

    public bool CanPopHead(Rect viewportWorldRect)
    {
        return !IsEmpty() && !IsWidgetOverlapsViewport(_widgets[GetHeadIndex()], viewportWorldRect);
    }

    public bool CanPopTail(Rect viewportWorldRect)
    {
        return !IsEmpty() && !IsWidgetOverlapsViewport(_widgets[GetTailIndex()], viewportWorldRect);
    }

    public void SetHeadEdge()
    {
        _headEdge = true;
    }

    public void SetTailEdge()
    {
        _tailEdge = true;
    }

    void AddWidget(IDynamicScrollItem item, int index)
    {
        IDynamicScrollItemWidget widget = _itemWidgetsPool.GetWidget(item);
        widget.Fill(item);
        LayoutRebuilder.ForceRebuildLayoutImmediate(widget.rectTransform);
        _widgets.Insert(index, widget);
    }

    void RemoveWidget(int index)
    {
        _itemWidgetsPool.ReturnWidget(_widgets[index]);
        _widgets.RemoveAt(index);
    }

    bool IsEmpty()
    {
        return _widgets.Count == 0;
    }

    int GetHeadIndex()
    {
        return 0;
    }

    int GetTailIndex()
    {
        return _widgets.Count - 1;
    }

    static bool IsWidgetOverlapsViewport(IDynamicScrollItemWidget widget, Rect viewportWorldRect)
    {
        return RectHelpers.GetWorldRect(widget.rectTransform).Overlaps(viewportWorldRect);
    }
}
