using UnityEngine;

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

        // Initial refresh
        OnScroll(Mathf.Epsilon);
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
        if (delta > 0f)
        {
            while (TryDeflate(ViewportEdge.Head, viewportWorldRect));
            while (TryInflate(ViewportEdge.Tail, viewportWorldRect));
        }
        else if (delta < 0f)
        {
            while (TryDeflate(ViewportEdge.Tail, viewportWorldRect));
            while (TryInflate(ViewportEdge.Head, viewportWorldRect));
        }

        _scrollWidget.SetEdgeDelta(GetEdgeDelta());
    }

    bool TryInflate(ViewportEdge edge, Rect viewportWorldRect)
    {
        if (!_dynamicContent.CanInflate(edge, viewportWorldRect) ||
            !_dynamicViewport.Inflate(edge))
        {
            return false;
        }

        int index = _dynamicViewport.GetEdgeIndex(edge);
        _dynamicContent.Inflate(edge, _itemProvider.GetItemByIndex(index));

        // Remove unnecessary elements if the list was scrolled too much on this frame
        TryDeflate(DynamicScrollViewport.OppositeEdges[edge], viewportWorldRect);
        return true;
    }

    bool TryDeflate(ViewportEdge edge, Rect viewportWorldRect)
    {
        if (!_dynamicContent.CanDeflate(edge, viewportWorldRect))
            return false;

        _dynamicContent.Deflate(edge);
        return _dynamicViewport.Deflate(edge);
    }

    float GetEdgeDelta()
    {
        foreach (ViewportEdge edge in DynamicScrollViewport.OppositeEdges.Keys)
            if (_dynamicViewport.CheckEdge(edge))
                return _dynamicContent.GetEdgeDelta(edge);
        return 0f;
    }
}
