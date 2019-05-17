using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DynamicScroll.Internal
{
    // Todo: bring order to the approach: 1-floating number or vector approach
    internal class WidgetsViewport
    {
        readonly RectTransform _node;
        readonly WidgetsPool _itemWidgetsPool;
        readonly List<IWidget> _widgets;
        readonly Dictionary<WidgetsAlignment, float> _edgesLastPositions;
        readonly WidgetsAlignment _alignment;
        readonly float _spacing;

        public WidgetsViewport(RectTransform node, IWidgetsProvider widgetsProvider, WidgetsAlignment alignment, float spacing)
        {
            _node = node;
            _itemWidgetsPool = new WidgetsPool(widgetsProvider, _node);
            _widgets = new List<IWidget>();
            _alignment = alignment;
            _spacing = spacing;

            _edgesLastPositions = new Dictionary<WidgetsAlignment, float>
            {
                { _alignment, 0f },
                { WidgetsAlignmentDesc.OppositeEdges[_alignment], _spacing * WidgetsAlignmentDesc.HeadInflationMasks[_alignment] },
            };

            SetPivotAndAnchors(_node);
        }

        public void Clear()
        {
            _itemWidgetsPool.Clear();
        }

        public ItemsEdge? Move(float delta)
        {
            if (Helpers.IsZeroValue(delta))
                return null;

            _node.anchoredPosition += delta * GetAlignmentMask();

            float inflationDirection = delta * WidgetsAlignmentDesc.HeadInflationMasks[_alignment];
            int inflationSign = Math.Sign(inflationDirection);
            return ItemsEdgeDesc.InflationSigns.FirstOrDefault(kv => kv.Value == inflationSign).Key;
        }

        public bool NeedInflate(ItemsEdge itemEdge, Rect viewportWorldRect)
        {
            WidgetsAlignment itemWidgetEdge = GetItemWidgetEdge(itemEdge);
            float viewportEdgePosition = WidgetsAlignmentDesc.RectPositions[itemWidgetEdge](viewportWorldRect);
            float nextPositionFloat = _edgesLastPositions[itemWidgetEdge] + _spacing *
                WidgetsAlignmentDesc.HeadInflationMasks[itemWidgetEdge];
            Vector2 startPos = _node.TransformPoint(nextPositionFloat * GetAlignmentMask());
            float widgetEdgePosition = Helpers.GetVectorComponent(startPos, AxisMaskDesc.WidgetsAlignmentAxis[_alignment]);
            return WidgetsAlignmentDesc.ViewportHasSpace[itemWidgetEdge](viewportEdgePosition, widgetEdgePosition);
        }

        public void Inflate(ItemsEdge itemEdge, IItem item)
        {
            IWidget widget = _itemWidgetsPool.GetWidget(item);
            widget.Fill(item);

            RectTransform widgetRectTransform = widget.rectTransform;
            SetPivotAndAnchors(widgetRectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(widget.rectTransform);

            // Change edge last position
            WidgetsAlignment itemWidgetEdge = GetItemWidgetEdge(itemEdge);
            float nextPositionFloat = _edgesLastPositions[itemWidgetEdge] + _spacing *
                WidgetsAlignmentDesc.HeadInflationMasks[itemWidgetEdge];
            Vector2 alignmentMask = GetAlignmentMask();
            Vector2 nextWidgetPosition = nextPositionFloat * alignmentMask;
            Vector2 newEdgesLastPositions = nextWidgetPosition + widget.rectTransform.rect.size * alignmentMask;
            _edgesLastPositions[itemWidgetEdge] = GetVectorComponent(newEdgesLastPositions);

            switch (itemEdge)
            {
                case ItemsEdge.Head:
                    _widgets.Insert(0, widget);
                    widgetRectTransform.anchoredPosition = newEdgesLastPositions;
                    break;

                case ItemsEdge.Tail:
                    _widgets.Add(widget);
                    widgetRectTransform.anchoredPosition = nextWidgetPosition;
                    break;
            }
        }

        public bool NeedDeflate(ItemsEdge edge, Rect viewportWorldRect)
        {
            return !IsEmpty() && !Helpers.GetWorldRect(GetEdgeWidget(edge).rectTransform).Overlaps(viewportWorldRect);
        }

        public void Deflate(ItemsEdge itemEdge)
        {
            IWidget widget = GetEdgeWidget(itemEdge);
            _itemWidgetsPool.ReturnWidget(widget);
            _widgets.Remove(widget);

            // Change edge last position
            WidgetsAlignment itemWidgetEdge = GetItemWidgetEdge(itemEdge);

            Vector2 alignmentMask = GetAlignmentMask();

            Vector2 edgeMask = WidgetsAlignmentDesc.HeadInflationMasks[itemWidgetEdge];
            _edgesLastPositions[itemWidgetEdge] -= widget.rectTransform.rect.size * edgeMask + _spacing * edgeMask;
        }

        public float GetEdgeDelta(ItemsEdge itemEdge, Rect viewportWorldRect)
        {
            WidgetsAlignment itemWidgetEdge = GetItemWidgetEdge(itemEdge);
            float viewportEdgePosition = WidgetsAlignmentDesc.RectPositions[itemWidgetEdge](viewportWorldRect);
            Vector2 edgeMask = WidgetsAlignmentDesc.HeadInflationMasks[itemWidgetEdge];
            Vector2 startPos = _node.TransformPoint(_edgesLastPositions[itemWidgetEdge]);
            var res = (viewportEdgePosition - startPos.y) * edgeMask.y;
            return res > 0f ? res * edgeMask / _node.lossyScale : 0f;
        }

        bool IsEmpty()
        {
            return _widgets.Count == 0;
        }

        IWidget GetEdgeWidget(ItemsEdge edge)
        {
            switch (edge)
            {
                case ItemsEdge.Head:
                    return _widgets[0];
                case ItemsEdge.Tail:
                    return _widgets[_widgets.Count - 1];
                default:
                    throw new Exception("Unhandled edge type " + edge);
            }
        }

        void SetPivotAndAnchors(RectTransform rectTransform)
        {
            rectTransform.anchorMin = WidgetsAlignmentDesc.AnchorsMin[_alignment];
            rectTransform.anchorMax = WidgetsAlignmentDesc.AnchorsMax[_alignment];
            rectTransform.pivot = WidgetsAlignmentDesc.Pivots[_alignment];
        }

        WidgetsAlignment GetItemWidgetEdge(ItemsEdge itemEdge)
        {
            switch (itemEdge)
            {
                case ItemsEdge.Head:
                    return _alignment;
                case ItemsEdge.Tail:
                    return WidgetsAlignmentDesc.OppositeEdges[_alignment];
                default:
                    throw new Exception("Unhandled item edge type " + itemEdge);
            }
        }

        Vector2 GetAlignmentMask()
        {
            Axis axis = AxisMaskDesc.WidgetsAlignmentAxis[_alignment];
            return AxisMaskDesc.AxisMasks[axis];
        }
    }

    //
    // Alignment description
    //

    static class WidgetsAlignmentDesc
    {
        public static readonly Dictionary<WidgetsAlignment, WidgetsAlignment> OppositeEdges = new Dictionary<WidgetsAlignment, WidgetsAlignment>
        {
            { WidgetsAlignment.Left, WidgetsAlignment.Right },
            { WidgetsAlignment.Right, WidgetsAlignment.Left },
            { WidgetsAlignment.Bottom, WidgetsAlignment.Top },
            { WidgetsAlignment.Top, WidgetsAlignment.Bottom },
        };

        public static readonly Dictionary<WidgetsAlignment, Vector2> HeadInflationMasks = new Dictionary<WidgetsAlignment, Vector2>
        {
            { WidgetsAlignment.Left, Vector2.left },
            { WidgetsAlignment.Right, Vector2.right },
            { WidgetsAlignment.Bottom, Vector2.down },
            { WidgetsAlignment.Top, Vector2.up }
        };

        public static readonly Dictionary<WidgetsAlignment, Func<Rect, float>> RectPositions = new Dictionary<WidgetsAlignment, Func<Rect, float>>
        {
            { WidgetsAlignment.Left, r => r.xMin },
            { WidgetsAlignment.Right, r => r.xMax },
            { WidgetsAlignment.Bottom, r => r.yMin },
            { WidgetsAlignment.Top, r => r.yMax },
        };

        public static readonly Dictionary<WidgetsAlignment, Vector2> AnchorsMin = new Dictionary<WidgetsAlignment, Vector2>
        {
            { WidgetsAlignment.Left, Vector2.zero },
            { WidgetsAlignment.Right, Vector2.right },
            { WidgetsAlignment.Bottom, Vector2.zero },
            { WidgetsAlignment.Top, Vector2.up },
        };

        public static readonly Dictionary<WidgetsAlignment, Vector2> AnchorsMax = new Dictionary<WidgetsAlignment, Vector2>
        {
            { WidgetsAlignment.Left, Vector2.up },
            { WidgetsAlignment.Right, Vector2.one },
            { WidgetsAlignment.Bottom, Vector2.right },
            { WidgetsAlignment.Top, Vector2.one },
        };

        public static readonly Dictionary<WidgetsAlignment, Vector2> Pivots = new Dictionary<WidgetsAlignment, Vector2>
        {
            { WidgetsAlignment.Left, new Vector2(0f, 0.5f) },
            { WidgetsAlignment.Right, new Vector2(1f, 0.5f) },
            { WidgetsAlignment.Bottom, new Vector2(0.5f, 0f) },
            { WidgetsAlignment.Top, new Vector2(0.5f, 1f) },
        };

        public static readonly Dictionary<WidgetsAlignment, Func<float, float, bool>> ViewportHasSpace = new Dictionary<WidgetsAlignment, Func<float, float, bool>>
        {
            { WidgetsAlignment.Left, (v, p) => v < p },
            { WidgetsAlignment.Right, (v, p) => v > p },
            { WidgetsAlignment.Bottom, (v, p) => v < p },
            { WidgetsAlignment.Top, (v, p) => v > p },
        };
    }
}
