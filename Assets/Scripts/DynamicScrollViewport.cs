using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DynamicScrollViewport : IDisposable
{
    public IDynamicScrollItemWidget headWidget => _widgets.Count > 0 ? _widgets[0] : null;
    public IDynamicScrollItemWidget tailWidget => _widgets.Count > 0 ? _widgets[_widgets.Count - 1] : null;
    
    IDynamicScrollItemProvider _itemProvider;
    DynamicScrollItemWidgetsPool _itemWidgetsPool;
    List<IDynamicScrollItemWidget> _widgets;
    int _headIndex;
    int _tailIndex;

    public DynamicScrollViewport(IDynamicScrollItemProvider itemProvider,
        IDynamicScrollItemWidgetProvider itemWidgetProvider, Transform itemWidgetsRoot)
    {
        _itemProvider = itemProvider;
        _itemWidgetsPool = new DynamicScrollItemWidgetsPool(itemWidgetProvider, itemWidgetsRoot);
        _widgets = new List<IDynamicScrollItemWidget>();
        _headIndex = _tailIndex = -1;
    }
    
    public void Dispose()
    {
        _itemWidgetsPool.Dispose();
    }

    public bool HeadMovePrevious()
    {
        int newHeadIndex = _headIndex - 1;
        IDynamicScrollItem newHeadItem = _itemProvider.GetItemByIndex(newHeadIndex);
        if (newHeadItem == null)
            return false;
        
        AddWidget(newHeadItem, 0);

        _headIndex = newHeadIndex;
        CheckIndices();

        return true;
    }

    public bool HeadMoveNext()
    {
        if (headWidget == null)
            return false;
        
        _itemWidgetsPool.ReturnWidget(headWidget);
        _widgets.RemoveAt(0);

        _headIndex++;
        if (_tailIndex < _headIndex)
            _tailIndex = _headIndex;
        CheckIndices();
        
        return true;
    }

    public bool TailMovePrevious()
    {
        if (tailWidget == null)
            return false;

        _itemWidgetsPool.ReturnWidget(tailWidget);
        _widgets.RemoveAt(_widgets.Count - 1);

        _tailIndex--;
        if (_headIndex > _tailIndex)
            _headIndex = _tailIndex;
        CheckIndices();
        
        return true;
    }

    public bool TailMoveNext()
    {
        int newTailIndex = _tailIndex + 1;
        IDynamicScrollItem newTailItem = _itemProvider.GetItemByIndex(newTailIndex);
        if (newTailItem == null)
            return false;
        
        AddWidget(newTailItem, _widgets.Count);
        
        _tailIndex = newTailIndex;
        if (_headIndex == -1)
            _headIndex = _tailIndex;
        CheckIndices();

        return true;
    }
    
    void AddWidget(IDynamicScrollItem item, int index)
    {
        IDynamicScrollItemWidget widget = _itemWidgetsPool.GetWidget(item);
        _widgets.Insert(index, widget);
        widget.Fill(item);
        LayoutRebuilder.ForceRebuildLayoutImmediate(widget.rectTransform);
    }

    void CheckIndices()
    {
        if (_headIndex > _tailIndex)
            throw new Exception($"Wrong indices: {_headIndex} {_tailIndex}");
    }
}
