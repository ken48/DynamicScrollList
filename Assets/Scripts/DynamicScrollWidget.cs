using System.Collections.Generic;
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
    struct WidgetInfo
    {
        public float position;
        public float size;
    }

    ScrollRect _scrollRect;
    IDynamicScrollItemProvider _itemProvider;
    IDynamicScrollItemWidgetProvider _itemWidgetProvider;
    List<WidgetInfo> _itemWidgetInfos;

    public void Init(IDynamicScrollItemProvider itemProvider, IDynamicScrollItemWidgetProvider itemWidgetProvider)
    {
        _scrollRect = GetComponent<ScrollRect>();
        _scrollRect.onValueChanged.AddListener(OnScroll);

        _itemWidgetInfos = new List<WidgetInfo>();

        _itemProvider = itemProvider;
        _itemWidgetProvider = itemWidgetProvider;

        // // Todo: temp
        // FillItems();
    }

    // void FillItems()
    // {
    //     int index = 0;
    //     IDynamicScrollItem item = null;
    //     while((item = _itemProvider.GetItemByIndex(index++)) != null)
    //     {
    //         var widget = _itemWidgetProvider.GetNewItemWidget(item);
    //         ((MonoBehaviour)widget).transform.SetParent(_scrollRect.content, false);
    //         widget.Fill(item);
    //     }
    // }

    void OnScroll(Vector2 normalizedPosition)
    {
        // Todo: converter from normalizedPosition to item index
        // Use history, if no history then 0;

        int index = GetCurrentItemIndex();

        Debug.Log(normalizedPosition.y);

    }

    int GetCurrentItemIndex(Vector2 normalizedPosition)
    {
        // Find nearest widget normalized

        return _itemWidgetSizes.
    }


}
