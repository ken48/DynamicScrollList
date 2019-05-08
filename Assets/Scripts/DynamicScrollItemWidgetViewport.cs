using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

enum Edge
{
    Left,
    Right,
    Bottom,
    Top,
}

public class DynamicScrollItemWidgetViewport : MonoBehaviour
{
    [SerializeField]
    Edge _startEdge;
    [SerializeField]
    float _spacing;

    RectTransform _node;
    DynamicScrollItemWidgetsPool _itemWidgetsPool;
    List<IDynamicScrollItemWidget> _widgets;
    Vector2 _spacingVector;
    Dictionary<Edge, Vector2> _edgesLastPositions;

    public void Init(IDynamicScrollItemWidgetProvider itemWidgetProvider)
    {
        _node = (RectTransform)transform;
        _itemWidgetsPool = new DynamicScrollItemWidgetsPool(itemWidgetProvider, _node);
        _widgets = new List<IDynamicScrollItemWidget>();
        _spacingVector = EdgesDescription.InflationMasks[_startEdge] * _spacing;
        _edgesLastPositions = new Dictionary<Edge, Vector2>
        {
            { _startEdge, Vector2.zero },
            { EdgesDescription.OppositeEdges[_startEdge], _spacingVector },
        };

        SetPivotAndAnchors(_node);
    }

    public void Shutdown()
    {
        _itemWidgetsPool.Dispose();
    }

    public DynamicScrollItemViewport.Edge Move(Vector2 delta)
    {
        Vector2 deltaAxis = delta * EdgesDescription.MoveMasks[_startEdge];
        _node.anchoredPosition += deltaAxis;

        var directionSign = (int)Mathf.Sign(GetVectorComponent(deltaAxis));
        var inflationSign = -directionSign;
        return DynamicScrollItemViewport.EdgeInflationSigns.FirstOrDefault(kv => kv.Value == inflationSign).Key;
    }

    public bool NeedInflate(DynamicScrollItemViewport.Edge itemEdge, Rect viewportWorldRect)
    {
        Edge itemWidgetEdge = GetItemWidgetEdge(itemEdge);
        float viewportEdgePosition = EdgesDescription.RectPositions[itemWidgetEdge](viewportWorldRect);
        Vector2 startPos = _node.TransformPoint(_edgesLastPositions[itemWidgetEdge] + _spacingVector);
        float widgetEdgePosition = GetVectorComponent(startPos);
        return EdgesDescription.ViewportHasSpace[itemWidgetEdge](viewportEdgePosition, widgetEdgePosition);
    }

    public void Inflate(DynamicScrollItemViewport.Edge edge, IDynamicScrollItem item)
    {
        IDynamicScrollItemWidget widget = _itemWidgetsPool.GetWidget(item);
        widget.Fill(item);

        RectTransform widgetRectTransform = widget.rectTransform;
        SetPivotAndAnchors(widgetRectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(widget.rectTransform);

        int sign = DynamicScrollItemViewport.EdgeInflationSigns[edge];
        _edgesLastPositions[edge] += (widgetRectTransform.rect.size + _spacingVector) * sign * AxisMasks[_axis];

        widgetRectTransform.anchoredPosition = _edgesLastPositions[edge];
        widgetRectTransform.anchoredPosition -= widget.rectTransform.rect.size * AxisMasks[_axis];

        switch (edge)
        {
            case DynamicScrollItemViewport.Edge.Begin:
                _widgets.Insert(0, widget);
                break;

            case DynamicScrollItemViewport.Edge.End:
                _widgets.Add(widget);
                break;
        }
    }

    public bool NeedDeflate(DynamicScrollItemViewport.Edge edge, Rect viewportWorldRect)
    {
        return !IsEmpty() && !DynamicScrollHelpers.GetWorldRect(GetEdgeWidget(edge).rectTransform).Overlaps(viewportWorldRect);
    }

    public void Deflate(DynamicScrollItemViewport.Edge edge)
    {
        IDynamicScrollItemWidget widget = GetEdgeWidget(edge);

        int sign = -DynamicScrollItemViewport.EdgeInflationSigns[edge];
        _edgesLastPositions[edge] += (widget.rectTransform.rect.size + _spacingVector) * sign * AxisMasks[_axis];

        _itemWidgetsPool.ReturnWidget(widget);
        _widgets.Remove(widget);
    }

    public Vector2 GetEdgeDelta(RectTransform viewport, DynamicScrollItemViewport.Edge edge)
    {
        Rect viewportRect = viewport.rect;
        Vector2 result = -_edgesLastPositions[edge] - _node.anchoredPosition + RectEdgePositions[edge](viewportRect) + viewportRect.size * 0.5f;
        Vector2 resultAxis = result * AxisMasks[_axis];
        var resultSign = (int)Mathf.Sign(GetVectorComponent(resultAxis, _axis));
        return resultSign == DynamicScrollItemViewport.EdgeInflationSigns[edge] ? resultAxis : Vector2.zero;
    }

    bool IsEmpty()
    {
        return _widgets.Count == 0;
    }

    IDynamicScrollItemWidget GetEdgeWidget(DynamicScrollItemViewport.Edge edge)
    {
        // Todo: refactor:
        // create Dictionary<DynamicScrollItemViewport.Edge, Widget> and you will not need order in list anymore

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
        rectTransform.anchorMin = EdgesDescription.AnchorsMin[_startEdge];
        rectTransform.anchorMax = EdgesDescription.AnchorsMax[_startEdge];
        rectTransform.pivot = EdgesDescription.Pivots[_startEdge];
    }

    float GetVectorComponent(Vector2 vector)
    {
        switch (_startEdge)
        {
            case Edge.Left:
            case Edge.Right:
                return vector.x;

            case Edge.Bottom:
            case Edge.Top:
                return vector.y;

            default:
                throw new Exception("Unhandled widget edge type " + _startEdge);
        }
    }

    Edge GetItemWidgetEdge(DynamicScrollItemViewport.Edge itemEdge)
    {
        switch (itemEdge)
        {
            case DynamicScrollItemViewport.Edge.Begin:
                return _startEdge;

            case DynamicScrollItemViewport.Edge.End:
                return EdgesDescription.OppositeEdges[_startEdge];

            default:
                throw new Exception("Unhandled item edge type " + itemEdge);
        }
    }
}

//
// Description
//

static class EdgesDescription
{
    public static readonly Dictionary<Edge, Edge> OppositeEdges = new Dictionary<Edge, Edge>
    {
        { Edge.Left, Edge.Right },
        { Edge.Right, Edge.Left },
        { Edge.Bottom, Edge.Top },
        { Edge.Top, Edge.Bottom },
    };

    public static readonly Dictionary<Edge, Vector2> InflationMasks = new Dictionary<Edge, Vector2>
    {
        { Edge.Left, Vector2.right },
        { Edge.Right, Vector2.left },
        { Edge.Bottom, Vector2.up },
        { Edge.Top, Vector2.down },
    };

    public static readonly Dictionary<Edge, Func<Rect, float>> RectPositions = new Dictionary<Edge, Func<Rect, float>>
    {
        { Edge.Left, r => r.xMin },
        { Edge.Right, r => r.xMax },
        { Edge.Bottom, r => r.yMin },
        { Edge.Top, r => r.yMax },
    };

    public static readonly Dictionary<Edge, Vector2> AnchorsMin = new Dictionary<Edge, Vector2>
    {
        { Edge.Left, Vector2.zero },
        { Edge.Right, Vector2.right },
        { Edge.Bottom, Vector2.zero },
        { Edge.Top, Vector2.up },
    };

    public static readonly Dictionary<Edge, Vector2> AnchorsMax = new Dictionary<Edge, Vector2>
    {
        { Edge.Left, Vector2.up },
        { Edge.Right, Vector2.one },
        { Edge.Bottom, Vector2.right },
        { Edge.Top, Vector2.one },
    };

    public static readonly Dictionary<Edge, Vector2> Pivots = new Dictionary<Edge, Vector2>
    {
        { Edge.Left, new Vector2(0f, 0.5f) },
        { Edge.Right, new Vector2(1f, 0.5f) },
        { Edge.Bottom, new Vector2(0.5f, 0f) },
        { Edge.Top, new Vector2(0.5f, 1f) },
    };

    public static readonly Dictionary<Edge, Vector2> MoveMasks = new Dictionary<Edge, Vector2>
    {
        { Edge.Left, Vector2.right },
        { Edge.Right, Vector2.right },
        { Edge.Bottom, Vector2.up },
        { Edge.Top, Vector2.up },
    };

    public static readonly Dictionary<Edge, Func<float, float, bool>> ViewportHasSpace = new Dictionary<Edge, Func<float, float, bool>>
    {
        { Edge.Left, (v, p) => v < p },
        { Edge.Right, (v, p) => v > p },
        { Edge.Bottom, (v, p) => v < p },
        { Edge.Top, (v, p) => v > p },
    };
}
