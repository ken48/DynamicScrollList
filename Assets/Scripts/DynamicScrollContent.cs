using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DynamicScrollContent
{
    // Todo: direction (swap)
    static readonly Dictionary<DynamicScrollDescription.Edge, Func<Rect, Vector2>> RectEdgePosition =
        new Dictionary<DynamicScrollDescription.Edge, Func<Rect, Vector2>>
    {
        { DynamicScrollDescription.Edge.Head, r => r.min },
        { DynamicScrollDescription.Edge.Tail, r => r.max },
    };

    // Todo: direction (sign)
    static readonly Dictionary<DynamicScrollDescription.Edge, Func<Vector2, Vector2, DynamicScrollDescription.Axis, bool>> ViewportCheckEdge =
        new Dictionary<DynamicScrollDescription.Edge, Func<Vector2, Vector2, DynamicScrollDescription.Axis, bool>>
    {
        { DynamicScrollDescription.Edge.Head, (v, p, a) => DynamicScrollHelpers.GetVectorComponent(v, a) < DynamicScrollHelpers.GetVectorComponent(p, a) },
        { DynamicScrollDescription.Edge.Tail, (v, p, a) => DynamicScrollHelpers.GetVectorComponent(v, a) > DynamicScrollHelpers.GetVectorComponent(p, a) },
    };

    static readonly Dictionary<DynamicScrollDescription.Edge, Vector2> AnchorBases =
        new Dictionary<DynamicScrollDescription.Edge, Vector2>
    {
        { DynamicScrollDescription.Edge.Head, Vector2.zero },
        { DynamicScrollDescription.Edge.Tail, Vector2.one },
    };

    RectTransform _viewport;
    RectTransform _node;
    DynamicScrollItemWidgetsPool _itemWidgetsPool;
    List<IDynamicScrollItemWidget> _widgets;
    Dictionary<DynamicScrollDescription.Edge, Vector2> _edgesLastPositions;
    DynamicScrollDescription.Axis _axis;
    DynamicScrollDescription.Edge _startEdge;
    Vector2 _spacingVector;

    public DynamicScrollContent(RectTransform viewport, RectTransform node, IDynamicScrollItemWidgetProvider itemWidgetProvider,
        DynamicScrollDescription.Axis axis, DynamicScrollDescription.Edge startEdge, float spacing)
    {
        _viewport = viewport;
        _node = node;
        _itemWidgetsPool = new DynamicScrollItemWidgetsPool(itemWidgetProvider, _node);
        _axis = axis;
        _startEdge = startEdge;
        _spacingVector = DynamicScrollDescription.AxisMasks[axis] * spacing;

        _widgets = new List<IDynamicScrollItemWidget>();
        _edgesLastPositions = new Dictionary<DynamicScrollDescription.Edge, Vector2>
        {
            { DynamicScrollDescription.Edge.Head, Vector2.zero },
            { DynamicScrollDescription.Edge.Tail, _spacingVector * DynamicScrollDescription.EdgeInflationSigns[_startEdge] },
        };

        SetPivotAndAnchors(_node);
    }

    public void Shutdown()
    {
        _itemWidgetsPool.Dispose();
    }

    public void Move(Vector2 delta)
    {
        _node.anchoredPosition += delta;
    }

    public bool CanInflate(DynamicScrollDescription.Edge edge, Rect viewportWorldRect)
    {
        int sign = DynamicScrollDescription.EdgeInflationSigns[edge];
        Vector2 startPos = _node.TransformPoint(_edgesLastPositions[edge] + _spacingVector * sign);
        return ViewportCheckEdge[edge](RectEdgePosition[edge](viewportWorldRect), startPos, _axis);
    }

    public void Inflate(DynamicScrollDescription.Edge edge, IDynamicScrollItem item)
    {
        IDynamicScrollItemWidget widget = _itemWidgetsPool.GetWidget(item);
        widget.Fill(item);

        RectTransform widgetRectTransform = widget.rectTransform;
        SetPivotAndAnchors(widgetRectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(widget.rectTransform);

        int sign = DynamicScrollDescription.EdgeInflationSigns[edge];
        AddEdgeLastPosition(widget.rectTransform, sign, edge);
        widgetRectTransform.anchoredPosition = _edgesLastPositions[edge];

        switch (edge)
        {
            case DynamicScrollDescription.Edge.Head:
                _widgets.Insert(0, widget);
                break;

            case DynamicScrollDescription.Edge.Tail:
                _widgets.Add(widget);
                Vector2 axisMask = DynamicScrollDescription.AxisMasks[_axis];
                widgetRectTransform.anchoredPosition -= widget.rectTransform.rect.size * axisMask;
                break;
        }
    }

    public bool CanDeflate(DynamicScrollDescription.Edge edge, Rect viewportWorldRect)
    {
        return !IsEmpty() && !DynamicScrollHelpers.GetWorldRect(GetEdgeWidget(edge).rectTransform).Overlaps(viewportWorldRect);
    }

    public void Deflate(DynamicScrollDescription.Edge edge)
    {
        IDynamicScrollItemWidget widget = GetEdgeWidget(edge);
        int sign = -DynamicScrollDescription.EdgeInflationSigns[edge];
        AddEdgeLastPosition(widget.rectTransform, sign, edge);

        _itemWidgetsPool.ReturnWidget(widget);
        _widgets.Remove(widget);
    }

    public Vector2 GetEdgeDelta(DynamicScrollDescription.Edge edge)
    {
        Rect viewportRect = _viewport.rect;
        Vector2 result = -_edgesLastPositions[edge] - _node.anchoredPosition + RectEdgePosition[edge](viewportRect) + viewportRect.size * 0.5f;
        var resultSign = (int)Mathf.Sign(DynamicScrollHelpers.GetVectorComponent(result, _axis));
        return resultSign == DynamicScrollDescription.EdgeInflationSigns[edge] ? result : Vector2.zero;
    }

    void AddEdgeLastPosition(RectTransform widgetRectTransform, int sign, DynamicScrollDescription.Edge edge)
    {
        Vector2 axisMask = DynamicScrollDescription.AxisMasks[_axis];
        _edgesLastPositions[edge] += (widgetRectTransform.rect.size + _spacingVector) * sign * axisMask;
    }

    //
    // Helpers
    //

    bool IsEmpty()
    {
        return _widgets.Count == 0;
    }

    IDynamicScrollItemWidget GetEdgeWidget(DynamicScrollDescription.Edge edge)
    {
        switch (edge)
        {
            case DynamicScrollDescription.Edge.Head:
                return _widgets[0];
            case DynamicScrollDescription.Edge.Tail:
                return _widgets[_widgets.Count - 1];
            default:
                throw new Exception("Unhandled edge type " + edge);
        }
    }

    void SetPivotAndAnchors(RectTransform rectTransform)
    {
        Vector2 pivotBase = Vector2.one * 0.5f;
        Vector2 axisMask = DynamicScrollDescription.AxisMasks[_axis];
        Vector2 orthoAxisMask = DynamicScrollDescription.AxisMasks[DynamicScrollDescription.OrthoAxes[_axis]];
        Vector2 baseVector = AnchorBases[_startEdge];
        Vector2 tailBase = AnchorBases[DynamicScrollDescription.Edge.Tail];

        rectTransform.anchorMin = baseVector * axisMask;
        rectTransform.anchorMax = rectTransform.anchorMin + tailBase * orthoAxisMask;
        rectTransform.pivot = baseVector * axisMask + pivotBase * orthoAxisMask;
    }
}
