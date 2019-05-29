using System;
using System.Collections;
using UnityEngine;

namespace DynamicScroll.Internal
{
    [RequireComponent(typeof(Scroller))]
    internal class ScrollNavigation : MonoBehaviour
    {
        public event Action<float> onScroll;

        Scroller _scroller;
        Coroutine _centerOnIndexCo;

        public void Init()
        {
            _scroller = GetComponent<Scroller>();
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

        public void CenterOnIndex(int index, float duration = 0f)
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
                RefreshViewport(ItemsEdge.Tail, true);

                // Todo: set position of needed index to appropriate viewport edge
                // ...
            }

            // Scroll widget to viewport center considering edges
            // Todo
            int relativeIndex = _itemsViewport.GetItemRelativeIndex(index);
            Vector2 headWorldPosition = _widgetsViewport.GetWidgetWorldPositionByRelativeIndex(relativeIndex);
            Vector2 viewportWorldRect = Helpers.GetWorldRect(_viewportNode).center;
            float totalDelta = Helpers.GetVectorComponent((viewportWorldRect - headWorldPosition) / _contentNode.lossyScale, Axis.Y); // Todo: AXIS

            if (Mathf.Approximately(duration, 0f))
            {
                OnScroll(totalDelta);
                _scroller.SetLocked(false);
            }
            else
            {
                _centerOnIndexCo = StartCoroutine(ScrollProcess(totalDelta, duration));
            }
        }

        IEnumerator ScrollProcess(float totalDelta, float duration)
        {
            float time = 0f;
            float prevDelta = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float normalizedTime = Mathf.Clamp01(time / duration);
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

    }
}
