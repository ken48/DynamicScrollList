using UnityEngine;

public class DynamicScrollList : MonoBehaviour
{
    [SerializeField]
    ScrollWidget _scrollWidget;
    [SerializeField]
    DynamicScrollViewport _dynamicViewport;
    [SerializeField]
    DynamicScrollContent _dynamicContent;

    void Awake()
    {
        _scrollWidget.onScroll += OnScroll;
    }

    void OnDestroy()
    {
        _scrollWidget.onScroll -= OnScroll;
    }

    void OnScroll(float delta)
    {
        ((RectTransform)_dynamicContent.transform).anchoredPosition += new Vector2(0f, delta);
        
        // Check overlapping and call viewport moveNext...
        
    }
}
