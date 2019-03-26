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
        _dynamicContent.Move(delta);

        Debug.Log($"{Time.frameCount} + {_dynamicViewport.headIndex} {_dynamicViewport.headEdge} {_dynamicViewport.tailIndex} {_dynamicViewport.tailEdge}");

        Rect viewportWorldRect = RectHelpers.GetWorldRect(_viewportNode);
        if (delta > 0f)
        {
            TryPopHead(viewportWorldRect);
            TryPushTail(viewportWorldRect);
        }
        else if (delta < 0f)
        {
            TryPopTail(viewportWorldRect);
            TryPushHead(viewportWorldRect);
        }

        Debug.Log($"{Time.frameCount} ++ {_dynamicViewport.headIndex} {_dynamicViewport.headEdge} {_dynamicViewport.tailIndex} {_dynamicViewport.tailEdge}");

        float edgeDelta = 0f;
        if (_dynamicViewport.headEdge)
        {
            edgeDelta = _dynamicContent.CheckHeadEdge();
            TryPopTail(viewportWorldRect);
        }
        else if (_dynamicViewport.tailEdge)
        {
            edgeDelta = _dynamicContent.CheckTailEdge();
            TryPopHead(viewportWorldRect);
        }

        _scrollWidget.SetEdgeDelta(edgeDelta);
    }

    void TryPushHead(Rect viewportWorldRect)
    {
        // Todo: pop tail on each push
        while (_dynamicContent.CanPushHead(viewportWorldRect) && _dynamicViewport.HeadMovePrevious())
            _dynamicContent.PushHead(_itemProvider.GetItemByIndex(_dynamicViewport.headIndex));
    }

    void TryPushTail(Rect viewportWorldRect)
    {
        // Todo: pop head on each push
        while (_dynamicContent.CanPushTail(viewportWorldRect) && _dynamicViewport.TailMoveNext())
            _dynamicContent.PushTail(_itemProvider.GetItemByIndex(_dynamicViewport.tailIndex));
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
