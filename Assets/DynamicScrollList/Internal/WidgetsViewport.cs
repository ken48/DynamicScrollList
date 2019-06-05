using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DynamicScroll.Internal
{
    internal class WidgetsViewport
    {
        readonly RectTransform _node;
        readonly WidgetsPool _itemWidgetsPool;
        readonly List<IWidget> _widgets;
        readonly Dictionary<WidgetsAlignment, float> _edgesLastPositions;
        readonly WidgetsAlignment _alignment;
        readonly float _spacing;
        readonly Axis _axis;

        public WidgetsViewport(RectTransform node, IWidgetsProvider widgetsProvider, WidgetsAlignment alignment, float spacing)
        {
            _node = node;
            _itemWidgetsPool = new WidgetsPool(widgetsProvider, _node);
            _widgets = new List<IWidget>();
            _alignment = alignment;
            _spacing = spacing;
            _axis = AxisMaskDesc.WidgetsAlignmentAxis[_alignment];
            _edgesLastPositions = new Dictionary<WidgetsAlignment, float>();

            SetPivotAndAnchors(_node);
            Reset();
        }

        public void Reset()
        {
            _node.anchoredPosition = Vector2.zero;

            _edgesLastPositions[_alignment] = 0f;
            _edgesLastPositions[WidgetsAlignmentDesc.OppositeEdges[_alignment]] = _spacing * WidgetsAlignmentDesc.HeadInflationSigns[_alignment];

            while (_widgets.Count > 0)
            {
                _itemWidgetsPool.ReturnWidget(_widgets[0]);
                _widgets.RemoveAt(0);
            }
        }

        public void Shutdown()
        {
            _itemWidgetsPool.Clear();
        }

        public ItemsEdge? Move(float delta)
        {
            if (Helpers.IsZeroValue(delta))
                return null;

            _node.anchoredPosition += delta * AxisMaskDesc.AxisMasks[_axis];

            float inflationDirection = delta * WidgetsAlignmentDesc.HeadInflationSigns[_alignment];
            int inflationSign = Math.Sign(inflationDirection);
            return ItemsEdgeDesc.InflationSigns.FirstOrDefault(kv => kv.Value == inflationSign).Key;
        }

        public bool NeedInflate(ItemsEdge itemEdge, Rect viewportWorldRect)
        {
            WidgetsAlignment itemWidgetEdge = GetItemWidgetEdge(itemEdge);
            float viewportEdgePosition = WidgetsAlignmentDesc.RectPositions[itemWidgetEdge](viewportWorldRect);
            float inflationSign = WidgetsAlignmentDesc.HeadInflationSigns[itemWidgetEdge];
            float nextPositionFloat = _edgesLastPositions[itemWidgetEdge] + _spacing * inflationSign;
            Vector2 startPos = _node.TransformPoint(nextPositionFloat * AxisMaskDesc.AxisMasks[_axis]);
            float widgetEdgePosition = Helpers.GetVectorComponent(startPos, _axis);
            return WidgetsAlignmentDesc.ViewportHasSpace[itemWidgetEdge](viewportEdgePosition, widgetEdgePosition);
        }

        public void Inflate(ItemsEdge itemEdge, IItem item)
        {
            IWidget widget = _itemWidgetsPool.GetWidget(item);
            widget.Fill(item);

            RectTransform widgetRectTransform = widget.rectTransform;
            SetPivotAndAnchors(widgetRectTransform);
            widget.RecalcRect();

            // Change edge last position
            WidgetsAlignment itemWidgetEdge = GetItemWidgetEdge(itemEdge);
            float inflationSign = WidgetsAlignmentDesc.HeadInflationSigns[itemWidgetEdge];
            float nextPositionFloat = _edgesLastPositions[itemWidgetEdge] + _spacing * inflationSign;
            Vector2 axisMask = AxisMaskDesc.AxisMasks[_axis];
            Vector2 nextWidgetPosition = nextPositionFloat * axisMask;
            Vector2 newEdgesLastPositions = nextWidgetPosition + widgetRectTransform.rect.size * axisMask * inflationSign;
            _edgesLastPositions[itemWidgetEdge] = Helpers.GetVectorComponent(newEdgesLastPositions, _axis);

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
            _edgesLastPositions[itemWidgetEdge] -= (Helpers.GetVectorComponent(widget.rectTransform.rect.size, _axis) +
                _spacing) * WidgetsAlignmentDesc.HeadInflationSigns[itemWidgetEdge];
        }

        public float GetEdgeDelta(ItemsEdge itemEdge, Rect viewportWorldRect)
        {
            WidgetsAlignment itemWidgetEdge = GetItemWidgetEdge(itemEdge);
            float viewportEdgePosition = WidgetsAlignmentDesc.RectPositions[itemWidgetEdge](viewportWorldRect);
            Vector2 startPos = _node.TransformPoint(_edgesLastPositions[itemWidgetEdge] * AxisMaskDesc.AxisMasks[_axis]);
            float inflationSign = WidgetsAlignmentDesc.HeadInflationSigns[itemWidgetEdge];
            var res = (viewportEdgePosition - Helpers.GetVectorComponent(startPos, _axis)) * inflationSign;
            return res > 0f ? res * inflationSign / Helpers.GetVectorComponent(_node.lossyScale, _axis) : 0f;
        }

        public Rect GetWidgetWorldRectByRelativeIndex(int relativeIndex)
        {
            if (relativeIndex < 0 || relativeIndex >= _widgets.Count)
                throw new Exception($"Invalid relative index {relativeIndex} {_widgets.Count}");

            return Helpers.GetWorldRect(_widgets[relativeIndex].rectTransform);
        }

        public float GetLocalCoordinate(Vector2 worldCoordinates)
        {
            return Helpers.GetVectorComponent(worldCoordinates / _node.lossyScale, _axis);
        }

        public float GetInflationSign(ItemsEdge itemsEdge)
        {
            WidgetsAlignment alignment = GetItemWidgetEdge(itemsEdge);
            return WidgetsAlignmentDesc.HeadInflationSigns[alignment];
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

        public static readonly Dictionary<WidgetsAlignment, float> HeadInflationSigns = new Dictionary<WidgetsAlignment, float>
        {
            { WidgetsAlignment.Left, -1f },
            { WidgetsAlignment.Right, 1f },
            { WidgetsAlignment.Bottom, -1f },
            { WidgetsAlignment.Top, 1f }
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
