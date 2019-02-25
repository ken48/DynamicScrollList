using UnityEngine;
using UnityEngine.UI;

public interface IDynamicScrollItem
{
}

public interface IDynamicScrollItemProvider
{
    IDynamicScrollItem GetItemByIndex(int index);
}

public interface IDynamicScrollItemWidget
{
    void Fill(IDynamicScrollItem item);
}

public interface IDynamicScrollItemWidgetProvider
{
    IDynamicScrollItemWidget GetNewItemWidget(IDynamicScrollItem item);
    void ReturnItemWidget(IDynamicScrollItemWidget itemWidget);
}

[RequireComponent(typeof(ScrollRect))]
public class DynamicScrollWidget : MonoBehaviour
{
    ScrollRect _scrollRect;
    IDynamicScrollItemProvider _itemProvider;
    IDynamicScrollItemWidgetProvider _itemWidgetProvider;

    void Awake()
    {
        _scrollRect = GetComponent<ScrollRect>();
        _scrollRect.onValueChanged.AddListener(OnScroll);
    }

    void OnDestroy()
    {
        _scrollRect.onValueChanged.RemoveListener(OnScroll);
    }

    public void Init(IDynamicScrollItemProvider itemProvider, IDynamicScrollItemWidgetProvider itemWidgetProvider)
    {
        _itemProvider = itemProvider;
        _itemWidgetProvider = itemWidgetProvider;
    }

    void OnScroll(Vector2 normalizedPosition)
    {

    }
}
