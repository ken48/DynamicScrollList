using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


// Todo: DIVIDE Data viewport and widget viewport.
// вьюпорт данных всегда одинаковый по сути от начала к концу (begin - end)
// вьюпорт верстки должен быть относительным (верх/низ право/лево)... Фиг знает..


public class DynamicScrollContent : MonoBehaviour
{
    enum Axis
    {
        X,
        Y,
    }

    static readonly Dictionary<Axis, Axis> OrthoAxes = new Dictionary<Axis, Axis>
    {
        { Axis.X, Axis.Y },
        { Axis.Y, Axis.X },
    };

    static readonly Dictionary<Axis, Vector2> AxisMasks = new Dictionary<Axis, Vector2>
    {
        { Axis.X, Vector2.right },
        { Axis.Y, Vector2.up },
    };

    // Todo: direction (swap)
    static readonly Dictionary<DynamicScrollDescription.Edge, Func<Rect, Vector2>> RectEdgePosition =
        new Dictionary<DynamicScrollDescription.Edge, Func<Rect, Vector2>>
    {
        { DynamicScrollDescription.Edge.Head, r => r.min },
        { DynamicScrollDescription.Edge.Tail, r => r.max },
    };

    // Todo: direction (sign)
    static readonly Dictionary<DynamicScrollDescription.Edge, Func<Vector2, Vector2, Axis, bool>> ViewportCheckEdge =
        new Dictionary<DynamicScrollDescription.Edge, Func<Vector2, Vector2, Axis, bool>>
    {
        { DynamicScrollDescription.Edge.Head, (v, p, a) => GetVectorComponent(v, a) < GetVectorComponent(p, a) },
        { DynamicScrollDescription.Edge.Tail, (v, p, a) => GetVectorComponent(v, a) > GetVectorComponent(p, a) },
    };

    static readonly Dictionary<DynamicScrollDescription.Edge, Vector2> AnchorBases =
        new Dictionary<DynamicScrollDescription.Edge, Vector2>
    {
        { DynamicScrollDescription.Edge.Head, Vector2.zero },
        { DynamicScrollDescription.Edge.Tail, Vector2.one },
    };

    [SerializeField]
    Axis _axis;
    [SerializeField]
    DynamicScrollDescription.Edge _startEdge;
    [SerializeField]
    float _spacing;

    RectTransform _node;
    DynamicScrollItemWidgetsPool _itemWidgetsPool;
    List<IDynamicScrollItemWidget> _widgets;
    Dictionary<DynamicScrollDescription.Edge, Vector2> _edgesLastPositions;
    Vector2 _spacingVector;

    public void Init(IDynamicScrollItemWidgetProvider itemWidgetProvider)
    {
        _node = (RectTransform)transform;
        _itemWidgetsPool = new DynamicScrollItemWidgetsPool(itemWidgetProvider, _node);
        _spacingVector = AxisMasks[_axis] * _spacing;

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

    public DynamicScrollDescription.Edge Move(Vector2 delta)
    {
        Vector2 deltaAxis = delta * AxisMasks[_axis];
        _node.anchoredPosition += deltaAxis;

        var directionSign = (int)Mathf.Sign(GetVectorComponent(deltaAxis, _axis));
        var inflationSign = -directionSign;
        return DynamicScrollDescription.EdgeInflationSigns.FirstOrDefault(kv => kv.Value == inflationSign).Key;
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
                Vector2 axisMask = AxisMasks[_axis];
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

    public Vector2 GetEdgeDelta(RectTransform viewport, DynamicScrollDescription.Edge edge)
    {
        Rect viewportRect = viewport.rect;
        Vector2 result = -_edgesLastPositions[edge] - _node.anchoredPosition + RectEdgePosition[edge](viewportRect) + viewportRect.size * 0.5f;
        Vector2 resultAxis = result * AxisMasks[_axis];
        var resultSign = (int)Mathf.Sign(GetVectorComponent(resultAxis, _axis));
        return resultSign == DynamicScrollDescription.EdgeInflationSigns[edge] ? resultAxis : Vector2.zero;
    }

    void AddEdgeLastPosition(RectTransform widgetRectTransform, int sign, DynamicScrollDescription.Edge edge)
    {
        Vector2 axisMask = AxisMasks[_axis];
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
        Vector2 axisMask = AxisMasks[_axis];
        Vector2 orthoAxisMask = AxisMasks[OrthoAxes[_axis]];
        Vector2 baseVector = AnchorBases[_startEdge];
        Vector2 tailBase = AnchorBases[DynamicScrollDescription.Edge.Tail];

        rectTransform.anchorMin = baseVector * axisMask;
        rectTransform.anchorMax = rectTransform.anchorMin + tailBase * orthoAxisMask;
        rectTransform.pivot = baseVector * axisMask + pivotBase * orthoAxisMask;
    }

    static float GetVectorComponent(Vector2 vector, Axis axis)
    {
        switch (axis)
        {
            case Axis.X:
                return vector.x;
            case Axis.Y:
                return vector.y;
            default:
                throw new Exception("Unhandled axis type " + axis);
        }
    }
}
