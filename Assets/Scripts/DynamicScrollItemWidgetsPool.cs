using System.Collections.Generic;

public class DynamicScrollItemWidgetsPool
{
    readonly IDynamicScrollItemWidgetProvider _itemWidgetsProvider;
    List<IDynamicScrollItemWidget> _itemWidgets;

    public DynamicScrollItemWidgetsPool(IDynamicScrollItemWidgetProvider itemWidgetsProvider)
    {
        _itemWidgetsProvider = itemWidgetsProvider;
    }
}
