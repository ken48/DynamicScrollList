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
    public Vector2 moveMask => EdgesDescription.MoveMasks[_headEdge];

    [SerializeField]
    Edge _headEdge;
    [SerializeField]
    float _spacing;

    RectTransform _node;
    DynamicScrollItemWidgetsPool _itemWidgetsPool;
    List<IDynamicScrollItemWidget> _widgets;
    Dictionary<Edge, Vector2> _edgesLastPositions;

    public void Init(IDynamicScrollItemWidgetProvider itemWidgetProvider)
    {
        _node = (RectTransform)transform;
        _itemWidgetsPool = new DynamicScrollItemWidgetsPool(itemWidgetProvider, _node);
        _widgets = new List<IDynamicScrollItemWidget>();
        _edgesLastPositions = new Dictionary<Edge, Vector2>
        {
            { _headEdge, Vector2.zero },
            { EdgesDescription.OppositeEdges[_headEdge], Vector2.zero },
        };

        SetPivotAndAnchors(_node);
    }

    public void Shutdown()
    {
        _itemWidgetsPool.Dispose();
    }

    public DynamicScrollItemViewport.Edge? Move(Vector2 delta)
    {
        Vector2 deltaMasked = delta * EdgesDescription.MoveMasks[_headEdge];
        if (!DynamicScrollHelpers.CheckVectorMagnitude(deltaMasked))
            return null;

        _node.anchoredPosition += deltaMasked;

        Vector2 inflationDirection = deltaMasked * EdgesDescription.HeadInflationMasks[_headEdge];
        var inflationSign = (int)Mathf.Sign(GetVectorComponent(inflationDirection));
        return DynamicScrollItemViewport.EdgeInflationSigns.FirstOrDefault(kv => kv.Value == inflationSign).Key;
    }

    public bool NeedInflate(DynamicScrollItemViewport.Edge itemEdge, Rect viewportWorldRect)
    {
        Edge itemWidgetEdge = GetItemWidgetEdge(itemEdge);
        float viewportEdgePosition = EdgesDescription.RectPositions[itemWidgetEdge](viewportWorldRect);
        Vector2 edgeMask = EdgesDescription.HeadInflationMasks[itemWidgetEdge];
        Vector2 startPos = _node.TransformPoint(_edgesLastPositions[itemWidgetEdge] + edgeMask * _spacing);
        float widgetEdgePosition = GetVectorComponent(startPos);
        return EdgesDescription.ViewportHasSpace[itemWidgetEdge](viewportEdgePosition, widgetEdgePosition);
    }

    public void Inflate(DynamicScrollItemViewport.Edge itemEdge, IDynamicScrollItem item)
    {
        IDynamicScrollItemWidget widget = _itemWidgetsPool.GetWidget(item);
        widget.Fill(item);

        RectTransform widgetRectTransform = widget.rectTransform;
        SetPivotAndAnchors(widgetRectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(widget.rectTransform);

        Edge itemWidgetEdge = GetItemWidgetEdge(itemEdge);
        Vector2 prevEdgeLastPosition = _edgesLastPositions[itemWidgetEdge];
        ChangeEdgeLastPosition(itemEdge, widgetRectTransform, 1f);

        switch (itemEdge)
        {
            case DynamicScrollItemViewport.Edge.Head:
                _widgets.Insert(0, widget);
                widgetRectTransform.anchoredPosition = _edgesLastPositions[itemWidgetEdge];
                break;

            case DynamicScrollItemViewport.Edge.Tail:
                _widgets.Add(widget);
                widgetRectTransform.anchoredPosition = prevEdgeLastPosition;
                break;
        }
    }

    public bool NeedDeflate(DynamicScrollItemViewport.Edge edge, Rect viewportWorldRect)
    {
        return !IsEmpty() && !DynamicScrollHelpers.GetWorldRect(GetEdgeWidget(edge).rectTransform).Overlaps(viewportWorldRect);
    }

    public void Deflate(DynamicScrollItemViewport.Edge itemEdge)
    {
        IDynamicScrollItemWidget widget = GetEdgeWidget(itemEdge);
        ChangeEdgeLastPosition(itemEdge, widget.rectTransform, -1f);
        _itemWidgetsPool.ReturnWidget(widget);
        _widgets.Remove(widget);
    }

    public Vector2 GetEdgeDelta(DynamicScrollItemViewport.Edge itemEdge, Rect viewportWorldRect)
    {
        Edge itemWidgetEdge = GetItemWidgetEdge(itemEdge);
        float viewportEdgePosition = EdgesDescription.RectPositions[itemWidgetEdge](viewportWorldRect);
        Vector2 edgeMask = EdgesDescription.HeadInflationMasks[itemWidgetEdge];
        Vector2 startPos = _node.TransformPoint(_edgesLastPositions[itemWidgetEdge]);

        Debug.Log(itemEdge + " " + itemWidgetEdge + " " + (viewportEdgePosition - startPos.y) * edgeMask.y);

        // Todo: wtf - tail last position contains spacing
        var res = (viewportEdgePosition - startPos.y) * edgeMask.y;

        // Todo: scaleFactor
        return res > 0f ? res * edgeMask / GetComponentInParent<Canvas>().scaleFactor : Vector2.zero;
    }

    void ChangeEdgeLastPosition(DynamicScrollItemViewport.Edge itemEdge, RectTransform widgetRectTransform, float sign)
    {
        Edge itemWidgetEdge = GetItemWidgetEdge(itemEdge);
        Vector2 edgeMask = EdgesDescription.HeadInflationMasks[itemWidgetEdge];
        _edgesLastPositions[itemWidgetEdge] += (widgetRectTransform.rect.size * edgeMask + _spacing * edgeMask) * sign;
    }

    bool IsEmpty()
    {
        return _widgets.Count == 0;
    }

    IDynamicScrollItemWidget GetEdgeWidget(DynamicScrollItemViewport.Edge edge)
    {
        switch (edge)
        {
            case DynamicScrollItemViewport.Edge.Head:
                return _widgets[0];
            case DynamicScrollItemViewport.Edge.Tail:
                return _widgets[_widgets.Count - 1];
            default:
                throw new Exception("Unhandled edge type " + edge);
        }
    }

    void SetPivotAndAnchors(RectTransform rectTransform)
    {
        rectTransform.anchorMin = EdgesDescription.AnchorsMin[_headEdge];
        rectTransform.anchorMax = EdgesDescription.AnchorsMax[_headEdge];
        rectTransform.pivot = EdgesDescription.Pivots[_headEdge];
    }

    float GetVectorComponent(Vector2 vector)
    {
        switch (_headEdge)
        {
            case Edge.Left:
            case Edge.Right:
                return vector.x;

            case Edge.Bottom:
            case Edge.Top:
                return vector.y;

            default:
                throw new Exception("Unhandled widget edge type " + _headEdge);
        }
    }

    Edge GetItemWidgetEdge(DynamicScrollItemViewport.Edge itemEdge)
    {
        switch (itemEdge)
        {
            case DynamicScrollItemViewport.Edge.Head:
                return _headEdge;

            case DynamicScrollItemViewport.Edge.Tail:
                return EdgesDescription.OppositeEdges[_headEdge];

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

    public static readonly Dictionary<Edge, Vector2> HeadInflationMasks = new Dictionary<Edge, Vector2>
    {
        { Edge.Left, Vector2.left },
        { Edge.Right, Vector2.right },
        { Edge.Bottom, Vector2.down },
        { Edge.Top, Vector2.up }
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
