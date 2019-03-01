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
    RectTransform rectTransform { get; }
    void Fill(IDynamicScrollItem item);
}

public interface IDynamicScrollItemWidgetProvider
{
    IDynamicScrollItemWidget GetNewItemWidget(IDynamicScrollItem item);
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

        // Add
        // Head
        while (true)
        {
            if (_activeItems.Count == 0)
                break;

            ActiveItem headActiveItem = _activeItems[0];
            int prevItemIndex = headActiveItem.index - 1;
            IDynamicScrollItem prevItem = _itemProvider.GetItemByIndex(prevItemIndex);
            if (prevItem == null)
                break;

            // Check free space from head
            RectTransform headActiveItemRectTransform = headActiveItem.widget.rectTransform;
            Vector2 rt = headActiveItemRectTransform.TransformPoint(headActiveItemRectTransform.rect.max + Vector2.one * _spacing);
            if (viewportWorldRect.yMax >= rt.y)
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
            if (nextItem != null)
            {
                AddActiveItem(nextItem, nextItemIndex, false);

                // Todo:
                // if (!fromHead && _itemMaxIndex < itemIndex)
                //     resize _scrollRect.content; _itemMaxIndex = itemIndex;
            }
        }
    }

    void AddActiveItem(IDynamicScrollItem item, int itemIndex, bool fromHead)
    {

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
