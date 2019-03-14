﻿using System;
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

    // Todo: fix this caches
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
    }

    public void Dispose()
    {
        _itemWidgetsPool.Dispose();
    }

    public void Move(float delta)
    {
        // Todo: check last positions and compare it with _node.anchoredPosition consider edges
        // if (_headEdge && _node.anchoredPosition < 0f)
        // ...
        // else if (_tailEdge && _node.anchoredPosition > _lastTailPosition - _viewport.rect.height)
        // ...

        // if (_headEdge && delta < 0f)
        //     _node.anchoredPosition = Vector2.zero;
        // else if (_tailEdge && delta > 0f)
        //     _node.anchoredPosition = Vector2.down * (_lastTailPosition + _viewport.rect.height);

        _node.anchoredPosition += new Vector2(0, delta);
    }

    public void PushHead(IDynamicScrollItem item)
    {
        AddWidget(item, GetHeadIndex());

        IDynamicScrollItemWidget newHeadWidget = _widgets[GetHeadIndex()];
        _lastHeadPosition += newHeadWidget.rectTransform.rect.height;
        newHeadWidget.rectTransform.anchoredPosition = Vector2.up * _lastHeadPosition;

        _headEdge = false;
    }

    public void PushTail(IDynamicScrollItem item)
    {
        AddWidget(item, GetTailIndex() + 1);

        IDynamicScrollItemWidget newTailWidget = _widgets[GetTailIndex()];
        _lastTailPosition -= _spacing;
        newTailWidget.rectTransform.anchoredPosition = Vector2.up * _lastTailPosition;
        _lastTailPosition -= newTailWidget.rectTransform.rect.height;

        _tailEdge = false;
    }

    public void PopHead()
    {
        RemoveWidget(GetHeadIndex());
        _lastHeadPosition -= _widgets[GetHeadIndex()].rectTransform.rect.height + _spacing;

        _headEdge = false;
    }

    public void PopTail()
    {
        RemoveWidget(GetTailIndex());
        _lastTailPosition -= _widgets[GetTailIndex()].rectTransform.rect.height + _spacing;

        _tailEdge = false;
    }

    public void SetHeadEdge()
    {
        _headEdge = true;
    }

    public void SetTailEdge()
    {
        _tailEdge = true;
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
            startPos = _node.TransformPoint(Vector2.up * _spacing).y;
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
            startPos = _node.TransformPoint(Vector2.down * _spacing).y;
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
