using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DynamicScrollContent : IDisposable
{
    DynamicScrollItemWidgetsPool _itemWidgetsPool;
    List<IDynamicScrollItemWidget> _widgets;
    RectTransform _node;
    float _spacing;

    public DynamicScrollContent(IDynamicScrollItemWidgetProvider itemWidgetProvider, RectTransform node, float spacing)
    {
        _itemWidgetsPool = new DynamicScrollItemWidgetsPool(itemWidgetProvider, node);
        _node = node;
        _spacing = spacing;
    }

    public void Dispose()
    {
        _itemWidgetsPool.Dispose();
    }

    public void PushHead(IDynamicScrollItem item)
    {
        AddWidget(item, 0);
    }

    public void PopHead()
    {
        RemoveWidget(0);
    }

    public void PushTail(IDynamicScrollItem item)
    {
        AddWidget(item, _widgets.Count);
    }

    public void PopTail()
    {
        RemoveWidget(_widgets.Count - 1);
    }

    public bool CheckHead()
    {
        // Rect overlaps
    }

    public bool CheckTail()
    {
        // Rect overlaps
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
}
