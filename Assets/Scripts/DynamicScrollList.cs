using System;
using UnityEngine;

// Todo: adding, deleting, changing of element on fly
// Todo: navigation to some data index

public class DynamicScrollList : MonoBehaviour
{
    [SerializeField]
    ScrollWidget _scrollWidget;
    [SerializeField]
    RectTransform _viewportNode;
    [SerializeField]
    DynamicScrollContent _dynamicContent;

    IDynamicScrollItemProvider _itemProvider;
    DynamicScrollViewport _dynamicViewport;

    public void Init(IDynamicScrollItemProvider itemProvider, IDynamicScrollItemWidgetProvider itemWidgetProvider)
    {
        _itemProvider = itemProvider;
        _dynamicViewport = new DynamicScrollViewport(i => _itemProvider.GetItemByIndex(i) != null);
        _dynamicContent.Init(itemWidgetProvider);

        // Initial refresh
        HandleScroll(DynamicScrollViewport.Edge.Tail);

        _scrollWidget.onScroll += OnScroll;
    }

    public void Shutdown()
    {
        _scrollWidget.onScroll -= OnScroll;
        _dynamicContent.Shutdown();
    }

    void OnScroll(Vector2 delta)
    {
        DynamicScrollViewport.Edge inflationEdge = _dynamicContent.Move(delta);
        HandleScroll(inflationEdge);
    }

    void HandleScroll(DynamicScrollViewport.Edge inflationEdge)
    {
        Rect viewportWorldRect = DynamicScrollHelpers.GetWorldRect(_viewportNode);
        while (TryDeflate(DynamicScrollViewport.OppositeEdges[inflationEdge], viewportWorldRect));
        while (TryInflate(inflationEdge, viewportWorldRect));
        _scrollWidget.SetEdgeDelta(GetEdgeDelta());
    }

    bool TryInflate(DynamicScrollViewport.Edge edge, Rect viewportWorldRect)
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

    bool TryDeflate(DynamicScrollViewport.Edge edge, Rect viewportWorldRect)
    {
        if (!_dynamicContent.CanDeflate(edge, viewportWorldRect))
            return false;

        _dynamicContent.Deflate(edge);
        return _dynamicViewport.Deflate(edge);
    }

    Vector2 GetEdgeDelta()
    {
        foreach (DynamicScrollViewport.Edge edge in GetEdges())
            if (_dynamicViewport.CheckEdge(edge))
                return _dynamicContent.GetEdgeDelta(_viewportNode, edge);
        return Vector2.zero;
    }

    //
    // Helpers
    //

    static DynamicScrollViewport.Edge[] _edges;

    static DynamicScrollViewport.Edge[] GetEdges()
    {
        if (_edges == null)
            _edges = (DynamicScrollViewport.Edge[])Enum.GetValues(typeof(DynamicScrollViewport.Edge));
        return _edges;
    }
}
