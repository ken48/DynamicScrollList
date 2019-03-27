using UnityEngine;

// Todo: low fps inertia: too high speed on content returning + moving beyond the edge on returning
// Todo: crash on destroy
// Todo: todos, review all architecture
// Todo: common logic for horizontal & vertical scroll
// Todo: adding, deleting, changing of element
// Todo: items array enlarging on fly
// Todo: navigation to some data index

public class DynamicScrollList : MonoBehaviour
{
    [SerializeField]
    ScrollWidget _scrollWidget;
    [SerializeField]
    RectTransform _viewportNode;
    [SerializeField]
    RectTransform _contentNode;
    [SerializeField]
    float _spacing;

    IDynamicScrollItemProvider _itemProvider;
    DynamicScrollViewport _dynamicViewport;
    DynamicScrollContent _dynamicContent;

    void Awake()
    {
        _scrollWidget.onScroll += OnScroll;
    }

    public void Init(IDynamicScrollItemProvider itemProvider, IDynamicScrollItemWidgetProvider itemWidgetProvider)
    {
        _itemProvider = itemProvider;
        _dynamicViewport = new DynamicScrollViewport(i => _itemProvider.GetItemByIndex(i) != null);
        _dynamicContent = new DynamicScrollContent(itemWidgetProvider, _viewportNode, _contentNode, _spacing);

        // Todo: initial refresh
        OnScroll(0.00001f);
    }

    public void Shutdown()
    {
        _dynamicContent.Dispose();
    }

    void OnDestroy()
    {
        _scrollWidget.onScroll -= OnScroll;
    }

    void OnScroll(float delta)
    {
        // Move content
        _dynamicContent.Move(delta);

        // Refresh viewport and widgets
        Rect viewportWorldRect = RectHelpers.GetWorldRect(_viewportNode);
        if (delta > 0f)
        {
            // Try move forward from head
            while (TryHeadMoveForward(viewportWorldRect));

            // Try move forward from tail
            while (_dynamicContent.CanPushTail(viewportWorldRect) && _dynamicViewport.TailMoveForward())
            {
                _dynamicContent.PushTail(_itemProvider.GetItemByIndex(_dynamicViewport.tailIndex));
                TryHeadMoveForward(viewportWorldRect);
            }
        }
        else if (delta < 0f)
        {
            // Try move back from tail
            while (TryTailMoveBack(viewportWorldRect));

            // Try move back from head
            while (_dynamicContent.CanPushHead(viewportWorldRect) && _dynamicViewport.HeadMoveBack())
            {
                _dynamicContent.PushHead(_itemProvider.GetItemByIndex(_dynamicViewport.headIndex));
                TryTailMoveBack(viewportWorldRect);
            }
        }

        // Check edges
        float edgeDelta = 0f;
        if (_dynamicViewport.headEdge)
            edgeDelta = _dynamicContent.CheckHeadEdge();
        else if (_dynamicViewport.tailEdge)
            edgeDelta = _dynamicContent.CheckTailEdge();
        _scrollWidget.SetEdgeDelta(edgeDelta);
    }

    bool TryHeadMoveForward(Rect viewportWorldRect)
    {
        if (!_dynamicContent.CanPopHead(viewportWorldRect))
            return false;

        _dynamicContent.PopHead();
        return _dynamicViewport.HeadMoveForward();
    }

    bool TryTailMoveBack(Rect viewportWorldRect)
    {
        if (!_dynamicContent.CanPopTail(viewportWorldRect))
            return false;

        _dynamicContent.PopTail();
        return _dynamicViewport.TailMoveBack();
    }
}
