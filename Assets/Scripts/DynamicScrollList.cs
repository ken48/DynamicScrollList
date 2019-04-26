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
        HandleScroll(DynamicScrollDescription.Edge.Tail);

        _scrollWidget.onScroll += OnScroll;
    }

    public void Shutdown()
    {
        _scrollWidget.onScroll -= OnScroll;
        _dynamicContent.Shutdown();
    }

    void OnScroll(Vector2 delta)
    {
        DynamicScrollDescription.Edge inflationEdge = _dynamicContent.Move(delta);
        HandleScroll(inflationEdge);
    }

    void HandleScroll(DynamicScrollDescription.Edge inflationEdge)
    {
        Rect viewportWorldRect = DynamicScrollHelpers.GetWorldRect(_viewportNode);
        while (TryDeflate(DynamicScrollDescription.OppositeEdges[inflationEdge], viewportWorldRect));
        while (TryInflate(inflationEdge, viewportWorldRect));
        _scrollWidget.SetEdgeDelta(GetEdgeDelta());
    }

    bool TryInflate(DynamicScrollDescription.Edge edge, Rect viewportWorldRect)
    {
        if (!_dynamicContent.CanInflate(edge, viewportWorldRect) ||
            !_dynamicViewport.Inflate(edge))
        {
            return false;
        }

        int index = _dynamicViewport.GetEdgeIndex(edge);
        _dynamicContent.Inflate(edge, _itemProvider.GetItemByIndex(index));

        // Remove unnecessary elements if the list was scrolled too much on this frame
        TryDeflate(DynamicScrollDescription.OppositeEdges[edge], viewportWorldRect);
        return true;
    }

    bool TryDeflate(DynamicScrollDescription.Edge edge, Rect viewportWorldRect)
    {
        if (!_dynamicContent.CanDeflate(edge, viewportWorldRect))
            return false;

        _dynamicContent.Deflate(edge);
        return _dynamicViewport.Deflate(edge);
    }

    Vector2 GetEdgeDelta()
    {
        foreach (DynamicScrollDescription.Edge edge in GetEdges())
            if (_dynamicViewport.CheckEdge(edge))
                return _dynamicContent.GetEdgeDelta(_viewportNode, edge);
        return Vector2.zero;
    }

    //
    // Helpers
    //

    static DynamicScrollDescription.Edge[] _edges;

    static DynamicScrollDescription.Edge[] GetEdges()
    {
        if (_edges == null)
            _edges = (DynamicScrollDescription.Edge[])Enum.GetValues(typeof(DynamicScrollDescription.Edge));
        return _edges;
    }
}
