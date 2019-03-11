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
        IDynamicScrollItemWidget widget = AddWidget(item);
        _widgets.Insert(0, widget);
    }

    public void PopHead()
    {
        RemoveWidget(_widgets[0]);
        _widgets.RemoveAt(0);
    }

    public void PushTail(IDynamicScrollItem item)
    {
        IDynamicScrollItemWidget widget = AddWidget(item);
        _widgets.Add(widget);
    }

    public void PopTail()
    {
        RemoveWidget(_widgets[_widgets.Count - 1]);
        _widgets.RemoveAt(_widgets.Count - 1);
    }

    public bool CheckHead()
    {
        // Rect overlaps
    }

    public bool CheckTail()
    {
        // Rect overlaps
    }

    IDynamicScrollItemWidget AddWidget(IDynamicScrollItem item)
    {
        IDynamicScrollItemWidget widget = _itemWidgetsPool.GetWidget(item);
        widget.Fill(item);
        LayoutRebuilder.ForceRebuildLayoutImmediate(widget.rectTransform);
        return widget;
    }

    void RemoveWidget(IDynamicScrollItemWidget widget)
    {
        _itemWidgetsPool.ReturnWidget(widget);
    }
}
