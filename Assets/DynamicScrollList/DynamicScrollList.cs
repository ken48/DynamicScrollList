﻿using System;
using UnityEngine;

namespace DynamicScroll
{
    using Internal;

    public enum WidgetsAlignment
    {
        Left,
        Right,
        Bottom,
        Top,
    }

    // Todo: adding, deleting, changing of element on fly
    // Todo: navigation to some data index

    [RequireComponent(typeof(Scroller))]
    public class DynamicScrollList : MonoBehaviour
    {
        [SerializeField]
        RectTransform _viewportNode;
        [SerializeField]
        RectTransform _contentNode;

        [Header("Scroll")]
        [SerializeField]
        float _speedCoef;
        [SerializeField]
        float _inertiaCoef;
        [SerializeField]
        float _elasticityCoef;

        [Header("Layout")]
        [SerializeField]
        WidgetsAlignment _alignment;
        [SerializeField]
        float _spacing;

        IItemsProvider _itemsProvider;
        ItemsViewport _itemsViewport;
        WidgetsViewport _widgetsViewport;
        Scroller _scroller;

        void Reset()
        {
            // Scroller
            _speedCoef = 15f;
            _inertiaCoef = 3f;
            _elasticityCoef = 0.5f;
        }

        public void Init(IItemsProvider itemsProvider, IWidgetsProvider widgetsProvider)
        {
            _itemsProvider = itemsProvider;
            _itemsViewport = new ItemsViewport(itemsProvider);
            _widgetsViewport = new WidgetsViewport(_contentNode, widgetsProvider, _alignment, _spacing);

            _scroller = GetComponent<Scroller>();
            _scroller.Init(_viewportNode, AxisMaskDesc.WidgetsAlignmentAxis[_alignment], _speedCoef, _inertiaCoef, _elasticityCoef);

            // Initial refresh
            RefreshViewport(ItemsEdge.Tail);

            _scroller.onScroll += OnScroll;
        }

        public void Shutdown()
        {
            _scroller.onScroll -= OnScroll;
            _widgetsViewport.Clear();
        }

        void OnScroll(float delta)
        {
            ItemsEdge? inflationEdge = _widgetsViewport.Move(delta);
            if (inflationEdge.HasValue)
                RefreshViewport(inflationEdge.Value);
        }

        void RefreshViewport(ItemsEdge inflationEdge)
        {
            Rect viewportWorldRect = Helpers.GetWorldRect(_viewportNode);
            while (TryDeflate(ItemsEdgeDesc.Opposites[inflationEdge], viewportWorldRect));
            while (TryInflate(inflationEdge, viewportWorldRect));
            _scroller.SetEdgeDelta(GetEdgeDelta(viewportWorldRect));
        }

        bool TryInflate(ItemsEdge edge, Rect viewportWorldRect)
        {
            if (!_widgetsViewport.NeedInflate(edge, viewportWorldRect) || !_itemsViewport.TryInflate(edge))
            {
                return false;
            }

            int index = _itemsViewport.GetEdgeIndex(edge);
            _widgetsViewport.Inflate(edge, _itemsProvider.GetItemByIndex(index));

            // Remove unnecessary element if the list was scrolled too much on this frame
            TryDeflate(ItemsEdgeDesc.Opposites[edge], viewportWorldRect);
            return true;
        }

        bool TryDeflate(ItemsEdge edge, Rect viewportWorldRect)
        {
            if (!_widgetsViewport.NeedDeflate(edge, viewportWorldRect))
                return false;

            _widgetsViewport.Deflate(edge);
            return _itemsViewport.TryDeflate(edge);
        }

        float GetEdgeDelta(Rect viewportWorldRect)
        {
            foreach (ItemsEdge edge in GetEdges())
                if (_itemsViewport.CheckEdge(edge))
                    return _widgetsViewport.GetEdgeDelta(edge, viewportWorldRect);

            return 0f;
        }

        //
        // Helpers
        //

        static ItemsEdge[] _edges;

        static ItemsEdge[] GetEdges()
        {
            if (_edges == null)
                _edges = (ItemsEdge[])Enum.GetValues(typeof(ItemsEdge));

            return _edges;
        }
    }
}