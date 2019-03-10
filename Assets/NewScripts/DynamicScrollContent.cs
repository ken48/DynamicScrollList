using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DynamicScrollContent : MonoBehaviour, IDisposable
{
    public IDynamicScrollItemWidget headWidget => _widgets.Count > 0 ? _widgets[0] : null;
    public IDynamicScrollItemWidget tailWidget => _widgets.Count > 0 ? _widgets[_widgets.Count - 1] : null;
    
    DynamicScrollItemWidgetsPool _itemWidgetsPool;
    List<IDynamicScrollItemWidget> _widgets;
    
    // Todo: handle layout: position, spacing...

    public void Init(IDynamicScrollItemWidgetProvider itemWidgetProvider)
    {
        _itemWidgetsPool = new DynamicScrollItemWidgetsPool(itemWidgetProvider, transform);
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
