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

    void Awake()
    {
        _scrollWidget.onScroll += OnScroll;
    }

    public void Init(IDynamicScrollItemProvider itemProvider, IDynamicScrollItemWidgetProvider itemWidgetProvider)
    {
        _itemProvider = itemProvider;
        _dynamicViewport = new DynamicScrollViewport(i => _itemProvider.GetItemByIndex(i) != null);
        _dynamicContent = new DynamicScrollContent(itemWidgetProvider, _viewportNode, _contentNode, _spacing, _scrollWidget.GetAxisMask());

        // Initial refresh
        OnScroll(-Vector2.one * Mathf.Epsilon);
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

        Rect viewportWorldRect = DynamicScrollHelpers.GetWorldRect(_viewportNode);

        // Select non zero vector component
        float deltaFloat = DynamicScrollHelpers.GetVectorComponent(delta * _scrollWidget.GetAxisMask());
        ViewportEdge inflationEdge = deltaFloat > 0f ? ViewportEdge.Head : ViewportEdge.Tail;
        while (TryDeflate(DynamicScrollViewport.OppositeEdges[inflationEdge], viewportWorldRect));
        while (TryInflate(inflationEdge, viewportWorldRect));
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
        foreach (var et in DynamicScrollHelpers.GetViewportEdges())
            if (_dynamicViewport.CheckEdge(et))
                return _dynamicContent.GetEdgeDelta(et);
        return Vector2.zero;
    }
}

//
// Helpers
//

public static class DynamicScrollHelpers
{
    static ViewportEdge[] _viewportEdges;

    public static ViewportEdge[] GetViewportEdges()
    {
        if (_viewportEdges == null)
            _viewportEdges = (ViewportEdge[])Enum.GetValues(typeof(ViewportEdge));
        return _viewportEdges;
    }

    // Todo: make it more explicitly
    public static float GetVectorComponent(Vector2 v)
    {
        // One of them is always zero
        return v.x + v.y;
    }

    public static Rect GetWorldRect(RectTransform rectTransform)
    {
        Rect rect = rectTransform.rect;
        Vector2 worldRectMin = rectTransform.TransformPoint(rect.min);
        Vector2 worldRectMax = rectTransform.TransformPoint(rect.max);
        return Rect.MinMaxRect(worldRectMin.x, worldRectMin.y, worldRectMax.x, worldRectMax.y);
    }
}
