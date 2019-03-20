using UnityEngine;

// Todo: low fps inertia: too high speed on content returning + moving beyond the edge on returning
// Todo: crash on destroy
// Todo: Todos, review other
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

        OnScroll(0f);
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
        _dynamicContent.Move(delta);

        Rect viewportWorldRect = RectHelpers.GetWorldRect(_viewportNode);
        if (delta >= 0f)
        {
            TryPopHead(viewportWorldRect);
            TryPushTail(viewportWorldRect);

        }
        else
        {
            TryPopTail(viewportWorldRect);
            TryPushHead(viewportWorldRect);
        }

        if (!_dynamicContent.CheckEdges(out float edgesDelta))
            _scrollWidget.SetEdgesDelta(edgesDelta);
    }

    void TryPushHead(Rect viewportWorldRect)
    {
        while (_dynamicContent.CanPushHead(viewportWorldRect))
        {
            if (!_dynamicViewport.HeadMovePrevious())
            {
                _dynamicContent.SetHeadEdge();
                break;
            }

            _dynamicContent.PushHead(_itemProvider.GetItemByIndex(_dynamicViewport.headIndex));
        }
    }

    void TryPushTail(Rect viewportWorldRect)
    {
        while (_dynamicContent.CanPushTail(viewportWorldRect))
        {
            if (!_dynamicViewport.TailMoveNext())
            {
                _dynamicContent.SetTailEdge();
                break;
            }

            _dynamicContent.PushTail(_itemProvider.GetItemByIndex(_dynamicViewport.tailIndex));
        }
    }

    void TryPopHead(Rect viewportWorldRect)
    {
        while (_dynamicContent.CanPopHead(viewportWorldRect))
        {
            _dynamicContent.PopHead();
            if (!_dynamicViewport.HeadMoveNext())
                break;
        }
    }

    void TryPopTail(Rect viewportWorldRect)
    {
        while (_dynamicContent.CanPopTail(viewportWorldRect))
        {
            _dynamicContent.PopTail();
            if (!_dynamicViewport.TailMovePrevious())
                break;
        }
    }
}
