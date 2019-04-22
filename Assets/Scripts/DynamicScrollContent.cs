using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Todo: set pivots and anchors in code for widgets depends on it's growth direction and axis

public class DynamicScrollContent : MonoBehaviour
{
    // Todo: direction (swap)
    static readonly Dictionary<DynamicScrollDescription.Edge, Func<Rect, Vector2>> RectEdgePosition = new Dictionary<DynamicScrollDescription.Edge, Func<Rect, Vector2>>
    {
        { DynamicScrollDescription.Edge.Head, r => r.min },
        { DynamicScrollDescription.Edge.Tail, r => r.max },
    };

    // Todo: direction (sign)
    static readonly Dictionary<DynamicScrollDescription.Edge, Func<Vector2, Vector2, bool>> ViewportCheckEdge = new Dictionary<DynamicScrollDescription.Edge, Func<Vector2, Vector2, bool>>
    {
        { DynamicScrollDescription.Edge.Head, (v, p) => DynamicScrollHelpers.GetVectorComponent(v) < DynamicScrollHelpers.GetVectorComponent(p) },
        { DynamicScrollDescription.Edge.Tail, (v, p) => DynamicScrollHelpers.GetVectorComponent(v) > DynamicScrollHelpers.GetVectorComponent(p) },
    };

    [SerializeField]
    float _spacing;

    RectTransform _node;
    RectTransform _viewport;
    DynamicScrollItemWidgetsPool _itemWidgetsPool;
    List<IDynamicScrollItemWidget> _widgets;
    Dictionary<DynamicScrollDescription.Edge, Vector2> _edgesLastPositions;
    DynamicScrollDescription.Axis _axis;
    Vector2 _spacingVector;

    public void Init(IDynamicScrollItemWidgetProvider itemWidgetProvider, RectTransform viewport, DynamicScrollDescription.Axis axis)
    {
        _node = (RectTransform)transform;
        _itemWidgetsPool = new DynamicScrollItemWidgetsPool(itemWidgetProvider, _node);
        _viewport = viewport;
        _axis = axis;
        _spacingVector = DynamicScrollDescription.AxisMasks[axis] * _spacing;

        _widgets = new List<IDynamicScrollItemWidget>();
        _edgesLastPositions = new Dictionary<DynamicScrollDescription.Edge, Vector2>
        {
            { DynamicScrollDescription.Edge.Head, Vector2.zero },
            { DynamicScrollDescription.Edge.Tail, -_spacingVector },
        };
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
        return ViewportCheckEdge[edge](RectEdgePosition[edge](viewportWorldRect) * _axisMask, startPos * _axisMask);
    }

    public void Inflate(DynamicScrollDescription.Edge edge, IDynamicScrollItem item)
    {
        IDynamicScrollItemWidget widget = _itemWidgetsPool.GetWidget(item);
        widget.Fill(item);
        LayoutRebuilder.ForceRebuildLayoutImmediate(widget.rectTransform);
        RectTransform widgetRectTransform = widget.rectTransform;

        float sign = DynamicScrollDescription.EdgeInflationSigns[edge];
        Vector2 newPosition = _edgesLastPositions[edge] + (widget.rectTransform.rect.size + _spacingVector) * sign * _axisMask;
        _edgesLastPositions[edge] = newPosition;
        widgetRectTransform.anchoredPosition = newPosition;

        switch (edge)
        {
            case DynamicScrollDescription.Edge.Head:
                _widgets.Insert(0, widget);
                break;

            case DynamicScrollDescription.Edge.Tail:
                _widgets.Add(widget);
                widgetRectTransform.anchoredPosition -= widget.rectTransform.rect.size * _axisMask;
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
        float sign = -DynamicScrollDescription.EdgeInflationSigns[edge];
        _edgesLastPositions[edge] += (widget.rectTransform.rect.size + _spacingVector) * sign * _axisMask;

        _itemWidgetsPool.ReturnWidget(widget);
        _widgets.Remove(widget);
    }

    public Vector2 GetEdgeDelta(DynamicScrollDescription.Edge edge)
    {
        Rect viewportRect = _viewport.rect;
        Vector2 result = -_edgesLastPositions[edge] - _node.anchoredPosition + RectEdgePosition[edge](viewportRect) + viewportRect.size * 0.5f;
        float resultFloat = DynamicScrollHelpers.GetVectorComponent(result, _axis);
        if ((int)Mathf.Sign(resultFloat) != DynamicScrollDescription.EdgeInflationSigns[edge])
            return Vector2.zero;
        return result;
    }

    bool IsEmpty()
    {
        return _widgets.Count == 0;
    }

    IDynamicScrollItemWidget GetEdgeWidget(DynamicScrollDescription.Edge edge)
    {
        return _widgets[edge == DynamicScrollDescription.Edge.Head ? 0 : _widgets.Count - 1];
    }
}
