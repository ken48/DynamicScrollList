using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DynamicScrollViewport2 : IDisposable
{
    public IDynamicScrollItemWidget headWidget => _widgets.Count > 0 ? _widgets[0] : null;
    public IDynamicScrollItemWidget tailWidget => _widgets.Count > 0 ? _widgets[_widgets.Count - 1] : null;
    
    IDynamicScrollItemProvider _itemProvider;
    DynamicScrollItemWidgetsPool _itemWidgetsPool;
    List<IDynamicScrollItemWidget> _widgets;
    int _headIndex;
    int _tailIndex;

    public DynamicScrollViewport2(IDynamicScrollItemProvider itemProvider,
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

        _headIndex = newHeadIndex;
        CheckIndices();
        
        AddWidget(newHeadItem, true);

        return true;
    }

    public bool HeadMoveNext()
    {
        if (headWidget == null)
            return false;

        _headIndex++;
        if (_tailIndex < _headIndex)
            _tailIndex = _headIndex;
        CheckIndices();
        
        RemoveWidget(true);
        
        return true;
    }

    public bool TailMovePrevious()
    {
        if (tailWidget == null)
            return false;
        
        _tailIndex--;
        if (_headIndex > _tailIndex)
            _headIndex = _tailIndex;
        CheckIndices();

        RemoveWidget(false);
        
        return true;
    }

    public bool TailMoveNext()
    {
        int newTailIndex = _tailIndex + 1;
        IDynamicScrollItem newTailItem = _itemProvider.GetItemByIndex(newTailIndex);
        if (newTailItem == null)
            return false;
        
        _tailIndex = newTailIndex;
        if (_headIndex == -1)
            _headIndex = _tailIndex;
        CheckIndices();
        
        AddWidget(newTailItem, false);

        return true;
    }
    
    void AddWidget(IDynamicScrollItem item, bool head)
    {
        IDynamicScrollItemWidget widget = _itemWidgetsPool.GetWidget(item);
        _widgets.Insert(GetWidgetIndex(head), widget);
        widget.Fill(item);
        LayoutRebuilder.ForceRebuildLayoutImmediate(widget.rectTransform);
    }

    void RemoveWidget(bool head)
    {
        int index = GetWidgetIndex(head);
        _itemWidgetsPool.ReturnWidget(_widgets[index]);
        _widgets.RemoveAt(index);
    }

    int GetWidgetIndex(bool head)
    {
        return head ? 0 : _widgets.Count - 1;
    }

    void CheckIndices()
    {
        if (_headIndex > _tailIndex)
            throw new Exception($"Wrong indices: {_headIndex} {_tailIndex}");
    }
}
