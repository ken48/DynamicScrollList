using System;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicScroll.Internal
{
    public class WidgetsPool
    {
        class WidgetInfo
        {
            public IWidget itemWidget;
            public bool isActive;
        }

        readonly IWidgetsProvider _widgetsProvider;
        readonly Transform _rootNode;
        readonly Dictionary<Type, Type> _dataWidgetMap;
        readonly Dictionary<Type, List<WidgetInfo>> _itemWidgets;

        public WidgetsPool(IWidgetsProvider widgetsProvider, Transform rootNode)
        {
            _widgetsProvider = widgetsProvider;
            _rootNode = rootNode;
            _dataWidgetMap = new Dictionary<Type, Type>();
            _itemWidgets = new Dictionary<Type, List<WidgetInfo>>();
        }

        public void Clear()
        {
            foreach (List<WidgetInfo> widgetInfos in _itemWidgets.Values)
                foreach (WidgetInfo widgetInfo in widgetInfos)
                    _widgetsProvider.ReturnWidget(widgetInfo.itemWidget);

            _dataWidgetMap.Clear();
            _itemWidgets.Clear();
        }

        public IWidget GetWidget(IItem item)
        {
            Type itemType = item.GetType();
            WidgetInfo widgetInfo;
            if (!_dataWidgetMap.TryGetValue(itemType, out Type itemWidgetType))
            {
                IWidget itemWidget = _widgetsProvider.GetNewWidget(item, _rootNode);
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
                        itemWidget = _widgetsProvider.GetNewWidget(item, _rootNode),
                    };
                    itemWidgets.Add(widgetInfo);
                }
            }

            widgetInfo.isActive = true;
            widgetInfo.itemWidget.go.SetActive(true);
            return widgetInfo.itemWidget;
        }

        public void ReturnWidget(IWidget itemWidget)
        {
            if (!_itemWidgets.TryGetValue(itemWidget.GetType(), out List<WidgetInfo> widgets))
                throw new Exception("Return widget before creation");

            WidgetInfo widgetInfo = widgets.Find(w => w.itemWidget == itemWidget);
            itemWidget.go.SetActive(false);
            widgetInfo.isActive = false;
        }
    }
}
