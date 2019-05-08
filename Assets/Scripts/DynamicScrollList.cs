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
    DynamicScrollItemWidgetViewport _dynamicItemWidgetViewport;

    IDynamicScrollItemProvider _itemProvider;
    DynamicScrollItemViewport _dynamicItemViewport;

    public void Init(IDynamicScrollItemProvider itemProvider, IDynamicScrollItemWidgetProvider itemWidgetProvider)
    {
        _itemProvider = itemProvider;
        _dynamicItemViewport = new DynamicScrollItemViewport(i => _itemProvider.GetItemByIndex(i) != null);
        _dynamicItemWidgetViewport.Init(itemWidgetProvider);

        // Initial refresh
        RefreshViewport(DynamicScrollItemViewport.Edge.Tail);

        _scrollWidget.onScroll += OnScroll;
    }

    public void Shutdown()
    {
        _scrollWidget.onScroll -= OnScroll;
        _dynamicItemWidgetViewport.Shutdown();
    }

    void OnScroll(Vector2 delta)
    {
        DynamicScrollItemViewport.Edge inflationEdge = _dynamicItemWidgetViewport.Move(delta);
        RefreshViewport(inflationEdge);
    }

    void RefreshViewport(DynamicScrollItemViewport.Edge inflationEdge)
    {
        Rect viewportWorldRect = DynamicScrollHelpers.GetWorldRect(_viewportNode);
        while (TryDeflate(DynamicScrollItemViewport.OppositeEdges[inflationEdge], viewportWorldRect));
        while (TryInflate(inflationEdge, viewportWorldRect));
        _scrollWidget.SetEdgeDelta(GetEdgeDelta());
    }

    bool TryInflate(DynamicScrollItemViewport.Edge edge, Rect viewportWorldRect)
    {
        if (!_dynamicItemWidgetViewport.NeedInflate(edge, viewportWorldRect) ||
            !_dynamicItemViewport.TryInflate(edge))
        {
            return false;
        }

        int index = _dynamicItemViewport.GetEdgeIndex(edge);
        _dynamicItemWidgetViewport.Inflate(edge, _itemProvider.GetItemByIndex(index));

        // Remove unnecessary element if the list was scrolled too much on this frame
        TryDeflate(DynamicScrollItemViewport.OppositeEdges[edge], viewportWorldRect);
        return true;
    }

    bool TryDeflate(DynamicScrollItemViewport.Edge edge, Rect viewportWorldRect)
    {
        if (!_dynamicItemWidgetViewport.NeedDeflate(edge, viewportWorldRect))
            return false;

        _dynamicItemWidgetViewport.Deflate(edge);
        return _dynamicItemViewport.TryDeflate(edge);
    }

    Vector2 GetEdgeDelta()
    {
        foreach (DynamicScrollItemViewport.Edge edge in GetEdges())
            if (_dynamicItemViewport.CheckEdge(edge))
                return _dynamicItemWidgetViewport.GetEdgeDelta(_viewportNode, edge);
        return Vector2.zero;
    }

    //
    // Helpers
    //

    static DynamicScrollItemViewport.Edge[] _edges;

    static DynamicScrollItemViewport.Edge[] GetEdges()
    {
        if (_edges == null)
            _edges = (DynamicScrollItemViewport.Edge[])Enum.GetValues(typeof(DynamicScrollItemViewport.Edge));
        return _edges;
    }
}
