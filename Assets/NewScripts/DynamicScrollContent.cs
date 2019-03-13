using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DynamicScrollContent : IDisposable
{
    DynamicScrollItemWidgetsPool _itemWidgetsPool;
    List<IDynamicScrollItemWidget> _widgets;
    RectTransform _node;
    float _viewportSize;
    float _spacing;

    bool _headEdge;
    bool _tailEdge;

    public DynamicScrollContent(IDynamicScrollItemWidgetProvider itemWidgetProvider, RectTransform node, float viewportSize, float spacing)
    {
        _itemWidgetsPool = new DynamicScrollItemWidgetsPool(itemWidgetProvider, node);
        _node = node;
        _viewportSize = viewportSize;
        _spacing = spacing;
        _widgets = new List<IDynamicScrollItemWidget>();
    }

    public void Dispose()
    {
        _itemWidgetsPool.Dispose();
    }

    public void Move(float delta)
    {
        if (_headEdge && delta < 0f)
        {
            if (!IsEmpty())
            {
                var firstWidgetRt = _widgets[GetHeadIndex()].rectTransform;
                _node.anchoredPosition = new Vector2(0, -firstWidgetRt.anchoredPosition.y);
            }
            return;
        }

        if (_tailEdge && delta > 0f)
        {
            if (!IsEmpty())
            {
                var lastWidgetRt = _widgets[GetTailIndex()].rectTransform;
                _node.anchoredPosition = new Vector2(0, -lastWidgetRt.anchoredPosition.y + lastWidgetRt.rect.height - _viewportSize);
            }
            return;
        }

        _node.anchoredPosition += new Vector2(0, delta);
    }

    public void PushHead(IDynamicScrollItem item)
    {
        float previousHeadPosition = GetHeadPosition();
        AddWidget(item, GetHeadIndex());

        IDynamicScrollItemWidget newHeadWidget = _widgets[GetHeadIndex()];
        Vector2 size = newHeadWidget.rectTransform.rect.size;
        Vector2 deltaPos = Vector2.up * (size.y + _spacing);
        newHeadWidget.rectTransform.anchoredPosition = new Vector2(0f, previousHeadPosition + deltaPos.y);

        _headEdge = false;
    }

    public void PushTail(IDynamicScrollItem item)
    {
        float previousTailPosition = GetTailPosition();
        AddWidget(item, GetTailIndex() + 1);

        IDynamicScrollItemWidget newTailWidget = _widgets[GetTailIndex()];
        newTailWidget.rectTransform.anchoredPosition = new Vector2(0f, previousTailPosition - _spacing);

        _tailEdge = false;
    }

    public void PopHead()
    {
        RemoveWidget(GetHeadIndex());

        _headEdge = false;
    }

    public void PopTail()
    {
        RemoveWidget(GetTailIndex());

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

    float GetHeadPosition()
    {
        return !IsEmpty() ? _widgets[GetHeadIndex()].rectTransform.anchoredPosition.y : 0f;
    }

    float GetTailPosition()
    {
        if (IsEmpty())
            return 0f;

        IDynamicScrollItemWidget widget = _widgets[GetTailIndex()];
        RectTransform widgetRectTransform = widget.rectTransform;
        return widgetRectTransform.anchoredPosition.y - widgetRectTransform.rect.height;
    }

    static bool IsWidgetOverlapsViewport(IDynamicScrollItemWidget widget, Rect viewportWorldRect)
    {
        return RectHelpers.GetWorldRect(widget.rectTransform).Overlaps(viewportWorldRect);
    }
}
