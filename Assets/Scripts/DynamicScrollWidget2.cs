using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class DynamicScrollWidget2 : MonoBehaviour
{
    [SerializeField]
    float _spacing;

    ScrollRect _scrollRect;
    DynamicScrollViewport2 _viewport;
    float _lastHeadPosition;
    float _lastTailPosition;
    float _lastScrollNormalizedPosition;

    public void Init(IDynamicScrollItemProvider itemProvider, IDynamicScrollItemWidgetProvider itemWidgetProvider)
    {
        _scrollRect = GetComponent<ScrollRect>();
        _scrollRect.onValueChanged.AddListener(OnScroll);

        _viewport = new DynamicScrollViewport2(itemProvider, itemWidgetProvider, _scrollRect.content);
    }

    public void Shutdown()
    {
        _viewport.Dispose();
    }

    void OnScroll(Vector2 normalizedPosition)
    {
        // Todo: generalization for horizontal, vertical, from top, from bottom...
        // Todo: what if we remove some elements from data during scrolling?           
        
        // Todo: known bug: position of elements on fast scrolling when removed all widgets
        
        Rect viewportWorldRect = RectHelpers.GetWorldRect(_scrollRect.viewport);

        if (normalizedPosition.y > _lastScrollNormalizedPosition)
        {
            TryRemoveTail(viewportWorldRect);
            TryAddHead(viewportWorldRect);
        }
        else
        {
            TryRemoveHead(viewportWorldRect);
            TryAddTail(viewportWorldRect);
        }

        // Todo: optimization - do it only if widgets changed
        // Maybe we should subtract previous viewport size and add current viewport size (the sum of all widgets rect with spacings)
        if (_viewport.headWidget != null)
        {
            _lastHeadPosition = GetHeadPosition();
            _lastTailPosition = GetTailPosition();

            float absTailPosition = Mathf.Abs(_lastTailPosition);
            if (_scrollRect.content.rect.height < absTailPosition)
                _scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, absTailPosition);
        }

        _lastScrollNormalizedPosition = normalizedPosition.y;
    }

    void TryRemoveHead(Rect viewportWorldRect)
    {
        while (_viewport.headWidget != null && !IsWidgetOverlapsViewport(_viewport.headWidget, viewportWorldRect))
            _viewport.HeadMoveNext();
    }
    
    void TryRemoveTail(Rect viewportWorldRect)
    {
        while (_viewport.tailWidget != null && !IsWidgetOverlapsViewport(_viewport.tailWidget, viewportWorldRect))
            _viewport.TailMovePrevious();
    }

    void TryAddHead(Rect viewportWorldRect)
    {        
        while (true)
        {
            IDynamicScrollItemWidget headWidget = _viewport.headWidget;
            if (headWidget != null)
            {
                Vector2 rt = headWidget.rectTransform.TransformPoint(headWidget.rectTransform.rect.max + Vector2.one * _spacing);
                if (viewportWorldRect.yMax < rt.y)
                    break;
            }
            else
            {
                // Todo: the bug is here
                float nextHeadBottomPosition = GetHeadPosition() + _spacing;
                if (viewportWorldRect.yMax < _scrollRect.content.TransformPoint(new Vector3(0, nextHeadBottomPosition, 0)).y)
                    break;
            }

            float previousHeadPosition = GetHeadPosition();
            if (!_viewport.HeadMovePrevious())
                break;

            IDynamicScrollItemWidget newHeadWidget = _viewport.headWidget;
            Vector2 size = newHeadWidget.rectTransform.rect.size;
            Vector2 deltaPos = Vector2.up * (size.y + _spacing);
            newHeadWidget.rectTransform.anchoredPosition = new Vector2(0f, previousHeadPosition + deltaPos.y);
        }
    }

    void TryAddTail(Rect viewportWorldRect)
    {
        while (true)
        {
            IDynamicScrollItemWidget tailWidget = _viewport.tailWidget;
            if (tailWidget != null)
            {
                Vector2 lb = tailWidget.rectTransform.TransformPoint(tailWidget.rectTransform.rect.min - Vector2.one * _spacing);
                if (viewportWorldRect.yMin > lb.y)
                   break;
            }
            else
            {
                // Todo: the bug is here
                float nextTailPosition = GetTailPosition() - _spacing;
                if (viewportWorldRect.yMin > _scrollRect.content.TransformPoint(new Vector3(0, nextTailPosition, 0)).y)
                    break;
            }

            float previousTailPosition = GetTailPosition();
            if (!_viewport.TailMoveNext())
                break;
            
            IDynamicScrollItemWidget newTailWidget = _viewport.tailWidget;
            newTailWidget.rectTransform.anchoredPosition = new Vector2(0f, previousTailPosition - _spacing);
        }
    }

    float GetHeadPosition()
    {
        IDynamicScrollItemWidget widget = _viewport.headWidget;
        return widget?.rectTransform.anchoredPosition.y ?? _lastHeadPosition;
    }

    float GetTailPosition()
    {
        IDynamicScrollItemWidget widget = _viewport.tailWidget;
        RectTransform widgetRectTransform = widget?.rectTransform;
        return widgetRectTransform != null ? widgetRectTransform.anchoredPosition.y - widgetRectTransform.rect.height :
            _lastTailPosition;
    }

    bool IsWidgetOverlapsViewport(IDynamicScrollItemWidget widget, Rect viewportWorldRect)
    {
        return RectHelpers.GetWorldRect(widget.rectTransform).Overlaps(viewportWorldRect);
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
