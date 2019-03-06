using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IDynamicScrollItem
{
}

public interface IDynamicScrollItemProviderIterator
{
    IDynamicScrollItem current { get; }
    bool isStart { get; }

    void MovePrevious();
    void MoveNext();
}

public interface IDynamicScrollItemProvider
{    
    IDynamicScrollItemProviderIterator GetIterator();
}

public interface IDynamicScrollItemWidget
{
    GameObject go { get; }
    RectTransform rectTransform { get; }
    void Fill(IDynamicScrollItem item);
}

public interface IDynamicScrollItemWidgetProvider
{
    IDynamicScrollItemWidget GetNewItemWidget(IDynamicScrollItem item, Transform rootNode);
    void ReturnItemWidget(IDynamicScrollItemWidget itemWidget);
}

[RequireComponent(typeof(ScrollRect))]
public class DynamicScrollWidget : MonoBehaviour
{
    [SerializeField]
    float _spacing;

    ScrollRect _scrollRect;
    DynamicScrollItemWidgetsPool _itemWidgetsPool;
    List<IDynamicScrollItemWidget> _widgets;
    IDynamicScrollItemProviderIterator _headIterator;
    IDynamicScrollItemProviderIterator _tailIterator;
    float _lastHeadPosition;
    float _lastTailPosition;

    public void Init(IDynamicScrollItemProvider itemProvider, IDynamicScrollItemWidgetProvider itemWidgetProvider)
    {
        _scrollRect = GetComponent<ScrollRect>();
        _scrollRect.onValueChanged.AddListener(OnScroll);

        _itemWidgetsPool = new DynamicScrollItemWidgetsPool(itemWidgetProvider, _scrollRect.content);
        _widgets = new List<IDynamicScrollItemWidget>();
        _headIterator = itemProvider.GetIterator();
        _tailIterator = itemProvider.GetIterator();
    }

    public void Shutdown()
    {
        foreach (IDynamicScrollItemWidget widget in _widgets)
            _itemWidgetsPool.ReturnWidget(widget);
        _itemWidgetsPool.Dispose();
    }

    void OnScroll(Vector2 normalizedPosition)
    {
        // Todo: generalization for horizontal, vertical, from top, from bottom...
        // Todo: what if we remove some elements from data during scrolling?           
        // Todo: what if there will be no active items after removing?
        // It can be on fast scrolling at low fps when the scroll step per one frame will more than viewport size
        // Cache last removed or (first/last active) for head or tail pivots
        
        // Todo: known bug: on fast scrolling some items are skipped
        // Todo: known bug: sometimes (may be linked bug) content size is bigger than sum of items 
        // Todo: known bug: sometimes there are no widget items at all. _headIterator == lastItem, tailIterator == lastItem + 1 
        
        Rect viewportWorldRect = RectHelpers.GetWorldRect(_scrollRect.viewport);
        RemoveWidget(viewportWorldRect, true);
        RemoveWidget(viewportWorldRect, false);
        AddHead(viewportWorldRect);
        AddTail(viewportWorldRect);

        if (_widgets.Count > 0)
        {
            _lastHeadPosition = GetHeadPosition();
            _lastTailPosition = GetTailPosition();
            if (_scrollRect.content.rect.height < Mathf.Abs(_lastTailPosition))
                _scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Abs(_lastTailPosition));
        }
    }

    void RemoveWidget(Rect viewportWorldRect, bool head)
    {
        while (_widgets.Count > 0)
        {
            int index = head ? 0 : _widgets.Count - 1;
            IDynamicScrollItemWidget widget = _widgets[index];
            Rect widgetWorldRect = RectHelpers.GetWorldRect(widget.rectTransform);
            if (widgetWorldRect.Overlaps(viewportWorldRect))
                break;

            _itemWidgetsPool.ReturnWidget(widget);
            _widgets.RemoveAt(index);
            
            // Todo: the bug is here!!!
            // |   |   |   |
            // V   V   V   V 
            if (head)
                _headIterator.MoveNext();
            else
                _tailIterator.MovePrevious();
        }
    }

    void AddHead(Rect viewportWorldRect)
    {        
        if (_widgets.Count == 0)
            return;
        
        if (_headIterator.isStart)
            _headIterator.MoveNext();
        
        while (true)
        {
            // Check free space from head
            IDynamicScrollItemWidget headWidget = _widgets[0];
            Vector2 rt = headWidget.rectTransform.TransformPoint(headWidget.rectTransform.rect.max + Vector2.one * _spacing);
            if (viewportWorldRect.yMax < rt.y)
                break;

            _headIterator.MovePrevious();
            IDynamicScrollItem prevItem = _headIterator.current;
            if (prevItem == null)
                break;

            float headPosition = GetHeadPosition();
            IDynamicScrollItemWidget widget = AddWidget(prevItem, 0);
            Vector2 size = widget.rectTransform.rect.size;
            Vector2 deltaPos = Vector2.up * (size.y + _spacing);
            widget.rectTransform.anchoredPosition = new Vector2(0f, headPosition + deltaPos.y);
        }
    }

    void AddTail(Rect viewportWorldRect)
    {
        while (true)
        {
            if (_widgets.Count > 0)
            {
                // Check free space from tail
                IDynamicScrollItemWidget tailWidget = _widgets[_widgets.Count - 1];
                Vector2 lb = tailWidget.rectTransform.TransformPoint(tailWidget.rectTransform.rect.min - Vector2.one * _spacing);
                if (viewportWorldRect.yMin > lb.y)
                    break;
            }

            _tailIterator.MoveNext();
            IDynamicScrollItem nextItem = _tailIterator.current;
            if (nextItem == null)
                break;

            float tailPosition = GetTailPosition();
            IDynamicScrollItemWidget widget = AddWidget(nextItem, _widgets.Count);            
            widget.rectTransform.anchoredPosition = new Vector2(0f, tailPosition - _spacing);
        }
    }

    IDynamicScrollItemWidget AddWidget(IDynamicScrollItem item, int index)
    {
        IDynamicScrollItemWidget widget = _itemWidgetsPool.GetWidget(item);
        widget.rectTransform.SetParent(_scrollRect.content);
        _widgets.Insert(index, widget);
        
        widget.Fill(item);
        LayoutRebuilder.ForceRebuildLayoutImmediate(widget.rectTransform);
        
        return widget;
    }

    float GetHeadPosition()
    {
        if (_widgets.Count == 0)
            return _lastHeadPosition;

        return _widgets[0].rectTransform.anchoredPosition.y;
    }

    float GetTailPosition()
    {
        if (_widgets.Count == 0)
            return _lastTailPosition;

        var widgetRt = _widgets[_widgets.Count - 1].rectTransform;
        return widgetRt.anchoredPosition.y - widgetRt.rect.height;
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
