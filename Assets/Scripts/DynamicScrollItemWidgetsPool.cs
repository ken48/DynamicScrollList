using System;
using System.Collections.Generic;
using UnityEngine;

public class DynamicScrollItemWidgetsPool : IDisposable
{
    class WidgetInfo
    {
        public IDynamicScrollItemWidget itemWidget;
        public bool isActive;
    }

    readonly IDynamicScrollItemWidgetProvider _itemWidgetsProvider;
    readonly Transform _rootNode;
    readonly Dictionary<Type, Type> _dataWidgetMap;
    readonly Dictionary<Type, List<WidgetInfo>> _itemWidgets;

    public DynamicScrollItemWidgetsPool(IDynamicScrollItemWidgetProvider itemWidgetsProvider, Transform rootNode)
    {
        _itemWidgetsProvider = itemWidgetsProvider;
        _rootNode = rootNode;
        _dataWidgetMap = new Dictionary<Type, Type>();
        _itemWidgets = new Dictionary<Type, List<WidgetInfo>>();
    }

    public void Dispose()
    {
        foreach (List<WidgetInfo> widgetInfos in _itemWidgets.Values)
            foreach (WidgetInfo widgetInfo in widgetInfos)
                _itemWidgetsProvider.ReturnItemWidget(widgetInfo.itemWidget);

        _dataWidgetMap.Clear();
        _itemWidgets.Clear();
    }

    public IDynamicScrollItemWidget GetWidget(IDynamicScrollItem item)
    {
        Type itemType = item.GetType();
        WidgetInfo widgetInfo;
        if (!_dataWidgetMap.TryGetValue(itemType, out Type itemWidgetType))
        {
            IDynamicScrollItemWidget itemWidget = _itemWidgetsProvider.GetNewItemWidget(item, _rootNode);
            itemWidgetType = itemWidget.GetType();
            _dataWidgetMap.Add(itemType, itemWidgetType);

            widgetInfo = new WidgetInfo
            {
                itemWidget = itemWidget,
            };
            _itemWidgets.Add(itemWidgetType, new List<WidgetInfo> { widgetInfo });
        }
        else
        {
            List<WidgetInfo> itemWidgets = _itemWidgets[itemWidgetType];
            widgetInfo = itemWidgets.FindLast(w => !w.isActive);
            if (widgetInfo == null)
            {
                widgetInfo = new WidgetInfo
                {
                    itemWidget = _itemWidgetsProvider.GetNewItemWidget(item, _rootNode),
                };
                itemWidgets.Add(widgetInfo);
            }
        }

        widgetInfo.isActive = true;
        widgetInfo.itemWidget.go.SetActive(true);
        return widgetInfo.itemWidget;
    }

    public void ReturnWidget(IDynamicScrollItemWidget itemWidget)
    {
        if (!_itemWidgets.TryGetValue(itemWidget.GetType(), out List<WidgetInfo> widgets))
            throw new Exception("Return widget before creation");

        WidgetInfo widgetInfo = widgets.Find(w => w.itemWidget == itemWidget);
        itemWidget.go.SetActive(false);
        widgetInfo.isActive = false;
    }
}
