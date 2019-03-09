using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class DynamicScrollList : MonoBehaviour, IDragHandler
{
    public event Action<float> onScroll;

    [SerializeField] 
    RectTransform.Axis _axis;
    [SerializeField] 
    RectTransform _viewport;
    [SerializeField]
    RectTransform _content;

    Canvas _canvas;

    void Awake()
    {
        _canvas = _viewport.GetComponentInParent<Canvas>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localDelta = RectTransformHelpers.ScreenPointToLocalPoint(eventData.delta, _canvas);
        OnScroll(_axis == RectTransform.Axis.Horizontal ? localDelta.x : localDelta.y);
    }

    void OnScroll(float delta)
    {
        Vector2 mask = _axis == RectTransform.Axis.Horizontal ? Vector2.right : Vector2.up;
        _content.anchoredPosition += mask * delta;
        
        onScroll?.Invoke(delta);
    }
}

static class RectTransformHelpers
{
    public static Vector2 ScreenPointToLocalPoint(Vector2 screenPoint, Canvas canvas)
    {
        return screenPoint / canvas.scaleFactor;
    }
}
