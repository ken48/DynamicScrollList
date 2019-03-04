using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IDynamicScrollItem
{
}

public interface IDynamicScrollItemProvider
{
    IDynamicScrollItem GetItemByIndex(int index);
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
    class ActiveItem
    {
        public int index;
        public IDynamicScrollItemWidget widget;
    }

    [SerializeField]
    float _spacing;

    ScrollRect _scrollRect;
    IDynamicScrollItemProvider _itemProvider;
    DynamicScrollItemWidgetsPool _itemWidgetsPool;
    List<ActiveItem> _activeItems;
    int _itemMaxIndex;

    public void Init(IDynamicScrollItemProvider itemProvider, IDynamicScrollItemWidgetProvider itemWidgetProvider)
    {
        _scrollRect = GetComponent<ScrollRect>();
        _scrollRect.onValueChanged.AddListener(OnScroll);

        _itemProvider = itemProvider;
        _itemWidgetsPool = new DynamicScrollItemWidgetsPool(itemWidgetProvider, _scrollRect.content);
        _activeItems = new List<ActiveItem>();
        _itemMaxIndex = -1;
    }

    public void Shutdown()
    {
        foreach (ActiveItem activeItem in _activeItems)
            _itemWidgetsPool.ReturnWidget(activeItem.widget);
        _itemWidgetsPool.Dispose();
    }

    void OnScroll(Vector2 normalizedPosition)
    {
        Rect viewportWorldRect = RectHelpers.GetWorldRect(_scrollRect.viewport);

        // Remove
        // Todo: optimization - iterate from both ends
        _activeItems.RemoveAll(activeItem =>
        {
            Rect widgetWorldRect = RectHelpers.GetWorldRect(activeItem.widget.rectTransform);
            bool result = !widgetWorldRect.Overlaps(viewportWorldRect);
            if (result)
                _itemWidgetsPool.ReturnWidget(activeItem.widget);
            return result;
        });

        // Todo: what if there will be no active items after removing?
        // It can be on fast scrolling at low fps when the scroll step per one frame will more than viewport size
        // Cache last removed or (first/last active) for head or tail pivots

        // Add
        // Head
        while (true)
        {
            if (_activeItems.Count == 0)
                break;

            int prevItemIndex = -1;

            // Check free space from head
            ActiveItem headActiveItem = _activeItems[0];
            RectTransform headActiveItemRectTransform = headActiveItem.widget.rectTransform;
            Vector2 rt = headActiveItemRectTransform.TransformPoint(headActiveItemRectTransform.rect.max + Vector2.one * _spacing);

            // Todo: generalization for horizontal, vertical, from top, from bottom...

            if (viewportWorldRect.yMax >= rt.y)
                prevItemIndex = headActiveItem.index - 1;

            IDynamicScrollItem prevItem = _itemProvider.GetItemByIndex(prevItemIndex);
            if (prevItem == null)
                break;

            AddActiveItem(prevItem, prevItemIndex, true);
        }

        // Tail
        while (true)
        {
            int nextItemIndex = -1;
            if (_activeItems.Count == 0)
            {
                nextItemIndex = 0;
            }
            else
            {
                // Check free space from tail
                ActiveItem tailActiveItem = _activeItems[_activeItems.Count - 1];
                RectTransform tailActiveItemRectTransform = tailActiveItem.widget.rectTransform;
                Vector2 lb = tailActiveItemRectTransform.TransformPoint(tailActiveItemRectTransform.rect.min - Vector2.one * _spacing);
                if (viewportWorldRect.yMin <= lb.y)
                    nextItemIndex = tailActiveItem.index + 1;
            }

            IDynamicScrollItem nextItem = _itemProvider.GetItemByIndex(nextItemIndex);
            if (nextItem == null)
                break;

            AddActiveItem(nextItem, nextItemIndex, false);

            if (_itemMaxIndex < nextItemIndex)
            {
                _itemMaxIndex = nextItemIndex;
                float tailPos = GetTailPosition();
                if (_scrollRect.content.rect.height < Mathf.Abs(tailPos))
                    _scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Abs(tailPos));
            }
        }
    }

    void AddActiveItem(IDynamicScrollItem item, int itemIndex, bool fromHead)
    {
        IDynamicScrollItemWidget widget = _itemWidgetsPool.GetWidget(item);
        RectTransform widgetRectTransform = widget.rectTransform;
        widgetRectTransform.SetParent(_scrollRect.content);
        widget.Fill(item);

        var activeItem = new ActiveItem
        {
            index = itemIndex,
            widget = widget,
        };

        LayoutRebuilder.ForceRebuildLayoutImmediate(widgetRectTransform);
        Vector2 size = widgetRectTransform.rect.size;

        var newPos = Vector2.zero;
        if (fromHead)
        {
            Vector2 deltaPos = Vector2.up * (size.y + _spacing);
            newPos.y = GetHeadPosition() + deltaPos.y;
        }
        else
        {
            newPos.y = GetTailPosition() - _spacing;
        }

        widgetRectTransform.anchoredPosition = newPos;

        _activeItems.Add(activeItem);
        _activeItems.Sort((a, b) => a.index.CompareTo(b.index));
    }

    float GetHeadPosition()
    {
        if (_activeItems.Count == 0)
            return 0f;

        return _activeItems[0].widget.rectTransform.anchoredPosition.y;
    }

    float GetTailPosition()
    {
        if (_activeItems.Count == 0)
            return 0f;

        var widgetRt = _activeItems[_activeItems.Count - 1].widget.rectTransform;
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
