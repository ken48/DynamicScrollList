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
    RectTransform _contentNode;
    [SerializeField]
    float _spacing;

    IDynamicScrollItemProvider _itemProvider;
    DynamicScrollViewport _dynamicViewport;
    DynamicScrollContent _dynamicContent;
    ViewportEdge[] _allEdges;

    void Awake()
    {
        _scrollWidget.onScroll += OnScroll;
    }

    public void Init(IDynamicScrollItemProvider itemProvider, IDynamicScrollItemWidgetProvider itemWidgetProvider)
    {
        _itemProvider = itemProvider;
        _dynamicViewport = new DynamicScrollViewport(i => _itemProvider.GetItemByIndex(i) != null);
        _dynamicContent = new DynamicScrollContent(itemWidgetProvider, _viewportNode, _contentNode, _spacing, _scrollWidget.GetAxisMask());
        _allEdges = (ViewportEdge[])Enum.GetValues(typeof(ViewportEdge));

        // Initial refresh
        OnScroll(Vector2.zero);
    }

    public void Shutdown()
    {
        _dynamicContent.Dispose();
    }

    void OnDestroy()
    {
        _scrollWidget.onScroll -= OnScroll;
    }

    void OnScroll(Vector2 delta)
    {
        _dynamicContent.Move(delta);

        Rect viewportWorldRect = RectHelpers.GetWorldRect(_viewportNode);

        // Select non zero vector component
        float deltaFloat = delta.x + delta.y;
        if (deltaFloat >= 0f)
        {
            while (TryDeflate(ViewportEdge.Head, viewportWorldRect));
            while (TryInflate(ViewportEdge.Tail, viewportWorldRect));
        }
        else if (deltaFloat < 0f)
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

    Vector2 GetEdgeDelta()
    {
        foreach (ViewportEdge edge in _allEdges)
            if (_dynamicViewport.CheckEdge(edge))
                return _dynamicContent.GetEdgeDelta(edge);
        return Vector2.zero;
    }
}
