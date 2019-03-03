using UnityEngine;

public class DynamicScrollItemWidgetsPool
{
    readonly IDynamicScrollItemWidgetProvider _itemWidgetsProvider;
    Transform _rootNode;

    // Todo: need real pooling

    public DynamicScrollItemWidgetsPool(IDynamicScrollItemWidgetProvider itemWidgetsProvider, Transform rootNode)
    {
        _itemWidgetsProvider = itemWidgetsProvider;
        _rootNode = rootNode;
    }

    public IDynamicScrollItemWidget GetWidget(IDynamicScrollItem item)
    {
        return _itemWidgetsProvider.GetNewItemWidget(item, _rootNode);
    }

    public void ReturnWidget(IDynamicScrollItemWidget itemWidget)
    {
        _itemWidgetsProvider.ReturnItemWidget(itemWidget);
    }
}
