using UnityEngine;

public class DynamicScrollList : MonoBehaviour
{
    [SerializeField]
    ScrollWidget _scrollWidget;
    [SerializeField]
    RectTransform _viewportNode;
    [SerializeField]
    RectTransform _contentNode;
    [SerializeField]
    float _spacing;

    IDynamicScrollItemProvider _itemProvider;
    DynamicScrollViewport _dynamicViewport;
    DynamicScrollContent _dynamicContent;

    void Awake()
    {
        _scrollWidget.onScroll += OnScroll;
    }

    public void Init(IDynamicScrollItemProvider itemProvider, IDynamicScrollItemWidgetProvider itemWidgetProvider)
    {
        _itemProvider = itemProvider;
        _dynamicViewport = new DynamicScrollViewport(i => _itemProvider.GetItemByIndex(i) != null);
        _dynamicContent = new DynamicScrollContent(itemWidgetProvider, _contentNode, _spacing);
    }

    public void Shutdown()
    {
        _dynamicContent.Dispose();
    }

    public void Dispose()
    {

    }

    void OnDestroy()
    {
        _scrollWidget.onScroll -= OnScroll;
    }

    void OnScroll(float delta)
    {
        _contentNode.anchoredPosition += new Vector2(0f, delta);

        // Check overlapping and call viewport moveNext...

    }


    /*
     * if (_dynamicViewport.HeadMovePrevious())
     *     _dynamicContent.PushHead(_itemProvider.GetItemByIndex(_dynamicViewport.headIndex));
     *
     * if (_dynamicViewport.HeadMoveNext())
     *     _dynamicContent.PopHead();
     *
     * if (_dynamicViewport.TailMovePrevious())
     *     _dynamicContent.PopTail();
     *
     * if (_dynamicViewport.TailMoveNext())
     *     _dynamicContent.PushTail(_itemProvider.GetItemByIndex(_dynamicViewport.tailIndex));
     */
}
