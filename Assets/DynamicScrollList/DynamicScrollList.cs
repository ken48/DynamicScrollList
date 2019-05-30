﻿using System;
using System.Collections;
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

    [RequireComponent(typeof(Scroller))]
    [RequireComponent(typeof(ScrollNavigation))]
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
        ScrollNavigation _scrollNavigation;

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
            _scroller.onScroll += OnScroll;

            _scrollNavigation = GetComponent<ScrollNavigation>();
            _scrollNavigation.Init(_itemsViewport, _widgetsViewport, _viewportNode, _contentNode, InitialRefreshViewport);
            _scrollNavigation.onScroll += OnScroll;

            InitialRefreshViewport();
        }

        public void Shutdown()
        {
            _scroller.onScroll -= OnScroll;
            _widgetsViewport.Shutdown();
        }

        public void CenterOnIndex(int index, bool immediate)
        {
            _scrollNavigation.CenterOnIndex(index, immediate);
        }

        void OnScroll(float delta)
        {
            ItemsEdge? inflationEdge = _widgetsViewport.Move(delta);
            if (inflationEdge.HasValue)
                RefreshViewport(inflationEdge.Value, false);
        }

        void InitialRefreshViewport()
        {
            RefreshViewport(ItemsEdge.Tail, true);
        }

        void RefreshViewport(ItemsEdge inflationEdge, bool adjustEdgeImmediate)
        {
            Rect viewportWorldRect = Helpers.GetWorldRect(_viewportNode);
            while (TryDeflate(ItemsEdgeDesc.Opposites[inflationEdge], viewportWorldRect));
            while (TryInflate(inflationEdge, viewportWorldRect));
            _scroller.SetEdgeDelta(GetEdgeDelta(viewportWorldRect), adjustEdgeImmediate);
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
