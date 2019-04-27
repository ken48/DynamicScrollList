using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DynamicScrollItemWidgetViewport : MonoBehaviour
{
    enum Edge
    {
        Left,
        Right,
        Bottom,
        Top,
    }

    static readonly Dictionary<Edge, Edge> OppositeEdges = new Dictionary<Edge, Edge>
    {
        { Edge.Left, Edge.Right },
        { Edge.Right, Edge.Left },
        { Edge.Bottom, Edge.Top },
        { Edge.Top, Edge.Bottom },
    };

    static readonly Dictionary<Edge, Vector2> EdgesMasks = new Dictionary<Edge, Vector2>
    {
        { Edge.Left, Vector2.right },
        { Edge.Right, Vector2.left },
        { Edge.Bottom, Vector2.up },
        { Edge.Top, Vector2.down },
    };

    // Todo: new Edges (left, right, bottom, top)
    static readonly Dictionary<DynamicScrollItemViewport.Edge, Func<Rect, Vector2>> RectEdgePosition =
        new Dictionary<DynamicScrollItemViewport.Edge, Func<Rect, Vector2>>
    {
        { DynamicScrollItemViewport.Edge.Begin, r => r.min },
        { DynamicScrollItemViewport.Edge.End, r => r.max },
    };

    // Todo: new Edges (left, right, bottom, top)
    static readonly Dictionary<DynamicScrollItemViewport.Edge, Func<Vector2, Vector2, Edge, bool>> ViewportCheckEdge =
        new Dictionary<DynamicScrollItemViewport.Edge, Func<Vector2, Vector2, Edge, bool>>
    {
        { DynamicScrollItemViewport.Edge.Begin, (v, p, a) => GetVectorComponent(v, a) < GetVectorComponent(p, a) },
        { DynamicScrollItemViewport.Edge.End, (v, p, a) => GetVectorComponent(v, a) > GetVectorComponent(p, a) },
    };

    // Todo: new Edges (left, right, bottom, top)
    static readonly Dictionary<DynamicScrollItemViewport.Edge, Vector2> AnchorBases =
        new Dictionary<DynamicScrollItemViewport.Edge, Vector2>
    {
        { DynamicScrollItemViewport.Edge.Begin, Vector2.zero },
        { DynamicScrollItemViewport.Edge.End, Vector2.one },
    };

    [SerializeField]
    Edge _startEdge;
    [SerializeField]
    float _spacing;

    RectTransform _node;
    DynamicScrollItemWidgetsPool _itemWidgetsPool;
    List<IDynamicScrollItemWidget> _widgets;
    Dictionary<DynamicScrollItemViewport.Edge, Vector2> _edgesLastPositions;
    Vector2 _spacingVector;

    public void Init(IDynamicScrollItemWidgetProvider itemWidgetProvider)
    {
        _node = (RectTransform)transform;
        _itemWidgetsPool = new DynamicScrollItemWidgetsPool(itemWidgetProvider, _node);
        _spacingVector = EdgesMasks[_startEdge] * _spacing;

        _widgets = new List<IDynamicScrollItemWidget>();

        // Todo: new Edges (left, right, bottom, top)
        _edgesLastPositions = new Dictionary<DynamicScrollItemViewport.Edge, Vector2>
        {
            { DynamicScrollItemViewport.Edge.Begin, Vector2.zero },
            { DynamicScrollItemViewport.Edge.End, _spacingVector },
        };

        SetPivotAndAnchors(_node);
    }

    public void Shutdown()
    {
        _itemWidgetsPool.Dispose();
    }

    public DynamicScrollItemViewport.Edge Move(Vector2 delta)
    {
        Vector2 deltaAxis = delta * AxisMasks[_axis];
        _node.anchoredPosition += deltaAxis;

        var directionSign = (int)Mathf.Sign(GetVectorComponent(deltaAxis, _axis));
        var inflationSign = -directionSign;
        return DynamicScrollItemViewport.EdgeInflationSigns.FirstOrDefault(kv => kv.Value == inflationSign).Key;
    }

    public bool CanInflate(DynamicScrollItemViewport.Edge edge, Rect viewportWorldRect)
    {
        int sign = DynamicScrollItemViewport.EdgeInflationSigns[edge];
        Vector2 startPos = _node.TransformPoint(_edgesLastPositions[edge] + _spacingVector * sign);
        return ViewportCheckEdge[edge](RectEdgePosition[edge](viewportWorldRect), startPos, _axis);
    }

    public void Inflate(DynamicScrollItemViewport.Edge edge, IDynamicScrollItem item)
    {
        IDynamicScrollItemWidget widget = _itemWidgetsPool.GetWidget(item);
        widget.Fill(item);

        RectTransform widgetRectTransform = widget.rectTransform;
        SetPivotAndAnchors(widgetRectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(widget.rectTransform);

        int sign = DynamicScrollItemViewport.EdgeInflationSigns[edge];
        AddEdgeLastPosition(widget.rectTransform, sign, edge);
        widgetRectTransform.anchoredPosition = _edgesLastPositions[edge];

        switch (edge)
        {
            case DynamicScrollItemViewport.Edge.Begin:
                _widgets.Insert(0, widget);
                break;

            case DynamicScrollItemViewport.Edge.End:
                _widgets.Add(widget);
                Vector2 axisMask = AxisMasks[_axis];
                widgetRectTransform.anchoredPosition -= widget.rectTransform.rect.size * axisMask;
                break;
        }
    }

    public bool CanDeflate(DynamicScrollItemViewport.Edge edge, Rect viewportWorldRect)
    {
        return !IsEmpty() && !DynamicScrollHelpers.GetWorldRect(GetEdgeWidget(edge).rectTransform).Overlaps(viewportWorldRect);
    }

    public void Deflate(DynamicScrollItemViewport.Edge edge)
    {
        IDynamicScrollItemWidget widget = GetEdgeWidget(edge);
        int sign = -DynamicScrollItemViewport.EdgeInflationSigns[edge];
        AddEdgeLastPosition(widget.rectTransform, sign, edge);

        _itemWidgetsPool.ReturnWidget(widget);
        _widgets.Remove(widget);
    }

    public Vector2 GetEdgeDelta(RectTransform viewport, DynamicScrollItemViewport.Edge edge)
    {
        Rect viewportRect = viewport.rect;
        Vector2 result = -_edgesLastPositions[edge] - _node.anchoredPosition + RectEdgePosition[edge](viewportRect) + viewportRect.size * 0.5f;
        Vector2 resultAxis = result * AxisMasks[_axis];
        var resultSign = (int)Mathf.Sign(GetVectorComponent(resultAxis, _axis));
        return resultSign == DynamicScrollItemViewport.EdgeInflationSigns[edge] ? resultAxis : Vector2.zero;
    }

    void AddEdgeLastPosition(RectTransform widgetRectTransform, int sign, DynamicScrollItemViewport.Edge edge)
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

    IDynamicScrollItemWidget GetEdgeWidget(DynamicScrollItemViewport.Edge edge)
    {
        switch (edge)
        {
            case DynamicScrollItemViewport.Edge.Begin:
                return _widgets[0];
            case DynamicScrollItemViewport.Edge.End:
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
        Vector2 baseVector = AnchorBases[startEdge];
        Vector2 tailBase = AnchorBases[DynamicScrollItemViewport.Edge.End];

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
