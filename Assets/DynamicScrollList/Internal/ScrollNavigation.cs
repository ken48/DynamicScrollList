using System;
using System.Collections;
using UnityEngine;

namespace DynamicScroll.Internal
{
    [RequireComponent(typeof(Scroller))]
    internal class ScrollNavigation : MonoBehaviour
    {
        const float Duration = 0.33f;

        public event Action<float> onScroll;

        Scroller _scroller;
        ItemsViewport _itemsViewport;
        WidgetsViewport _widgetsViewport;
        RectTransform _viewportNode;
        RectTransform _contentNode;
        Action _refreshViewportFunc;
        Coroutine _centerOnIndexCo;

        public void Init(ItemsViewport itemsViewport, WidgetsViewport widgetsViewport,
            RectTransform viewportNode, RectTransform contentNode, Action refreshViewportFunc)
        {
            _scroller = GetComponent<Scroller>();
            _itemsViewport = itemsViewport;
            _widgetsViewport = widgetsViewport;
            _viewportNode = viewportNode;
            _contentNode = contentNode;
            _refreshViewportFunc = refreshViewportFunc;
        }

        void OnDisable()
        {
            if (_centerOnIndexCo != null)
            {
                StopCoroutine(_centerOnIndexCo);
                _centerOnIndexCo = null;
                _scroller.SetLocked(false);
            }
        }

        public void CenterOnIndex(int index, bool immediate)
        {
            _scroller.StopScrolling();
            _scroller.SetLocked(true);

            if (_centerOnIndexCo != null)
            {
                StopCoroutine(_centerOnIndexCo);
                _centerOnIndexCo = null;
            }

            if (!_itemsViewport.IsItemInsideViewport(index))
            {
                _itemsViewport.ResetToIndex(index);
                _widgetsViewport.Reset();
                _refreshViewportFunc();

                // Todo: set position of needed index to appropriate viewport edge
                // ...
            }

            // Todo: consider edges
            int relativeIndex = _itemsViewport.GetItemRelativeIndex(index);
            Vector2 headWorldPosition = _widgetsViewport.GetWidgetWorldPositionByRelativeIndex(relativeIndex);
            Rect viewportWorldRect = Helpers.GetWorldRect(_viewportNode);
            Vector2 viewportWorldCenter = viewportWorldRect.center;

            // Todo: AXIS
            float totalDelta = Helpers.GetVectorComponent((viewportWorldCenter - headWorldPosition) / _contentNode.lossyScale, Axis.Y);

            OnScroll(totalDelta);
            float edgeDelta = GetEdgeDelta(viewportWorldRect);

            // Todo: in scrollList.OnScroll this code brakes current logic
            // _scroller.SetEdgeDelta(GetEdgeDelta(viewportWorldRect), adjustEdgeImmediate);

            // StartCoroutine(Test(totalDelta, edgeDelta));

            OnScroll(-totalDelta);
            totalDelta += edgeDelta;

            if (immediate)
            {
                OnScroll(totalDelta);
                _scroller.SetLocked(false);
            }
            else
            {
                _centerOnIndexCo = StartCoroutine(ScrollProcess(totalDelta));
            }
        }


        IEnumerator Test(float totalDelta, float edgeDelta)
        {

            yield return new WaitForSecondsRealtime(2f);

            Debug.Log("-  " + totalDelta);

            OnScroll(-totalDelta);

            yield return new WaitForSecondsRealtime(2f);

            OnScroll(totalDelta + edgeDelta);

            Debug.Log("+  " + totalDelta);
        }



        IEnumerator ScrollProcess(float totalDelta)
        {
            float time = 0f;
            float prevDelta = 0f;
            while (time < Duration)
            {
                time += Time.unscaledDeltaTime;
                float normalizedTime = Mathf.Clamp01(time / Duration);
                float delta = totalDelta * EasingOutQuad(normalizedTime);
                OnScroll(delta - prevDelta);
                prevDelta = delta;
                yield return null;
            }

            _scroller.SetLocked(false);
        }

        void OnScroll(float delta)
        {
            onScroll?.Invoke(delta);
        }

        static float EasingOutQuad(float t)
        {
            return t * (2f - t);
        }

        //
        // Todo: copypaste
        //

        float GetEdgeDelta(Rect viewportWorldRect)
        {
            foreach (ItemsEdge edge in GetEdges())
                if (_itemsViewport.CheckEdge(edge))
                    return _widgetsViewport.GetEdgeDelta(edge, viewportWorldRect);

            return 0f;
        }

        static ItemsEdge[] _edges;
        static ItemsEdge[] GetEdges()
        {
            if (_edges == null)
                _edges = (ItemsEdge[])Enum.GetValues(typeof(ItemsEdge));
            return _edges;
        }
    }
}
