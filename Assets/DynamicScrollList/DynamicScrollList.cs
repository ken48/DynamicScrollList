using System;
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
            Axis axis = AxisMaskDesc.WidgetsAlignmentAxis[_alignment];

            _scroller = GetComponent<Scroller>();
            _scroller.Init(_viewportNode, axis, _speedCoef, _inertiaCoef, _elasticityCoef);
            _scroller.onScroll += OnScroll;

            _scrollNavigation = GetComponent<ScrollNavigation>();
            _scrollNavigation.Init(_itemsViewport, _widgetsViewport, axis, InitialRefreshViewport);
            _scrollNavigation.onScroll += OnScrollNavigation;
            _scrollNavigation.onScrollStarted += OnScrollNavigationStarted;
            _scrollNavigation.onScrollFinished += OnScrollNavigationFinished;

            InitialRefreshViewport();
        }

        public void Shutdown()
        {
            _scroller.onScroll -= OnScroll;
            _scrollNavigation.onScroll -= OnScrollNavigation;
            _scrollNavigation.onScrollStarted -= OnScrollNavigationStarted;
            _scrollNavigation.onScrollFinished -= OnScrollNavigationFinished;
            _widgetsViewport.Shutdown();
        }

        public void CenterOnIndex(int index, bool immediate)
        {
            _scrollNavigation.CenterOnIndex(index, Helpers.GetWorldRect(_viewportNode), immediate);
        }

        void InitialRefreshViewport()
        {
            RefreshViewport(ItemsEdge.Tail, true);
        }

        void Scroll(float delta, bool adjustEdgeImmediate)
        {
            ItemsEdge? inflationEdge = _widgetsViewport.Move(delta);
            if (inflationEdge.HasValue)
                RefreshViewport(inflationEdge.Value, adjustEdgeImmediate);
        }

        void OnScroll(float delta)
        {
            Scroll(delta, false);
        }

        void OnScrollNavigation(float delta)
        {
            Scroll(delta, true);
        }

        void OnScrollNavigationStarted()
        {
            _scroller.StopScrolling();
            _scroller.SetLocked(true);
        }

        void OnScrollNavigationFinished()
        {
            _scroller.SetLocked(false);
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
                return false;

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
