using System;
using UnityEngine;

public class ChatItem : IDynamicScrollItem
{
    public long id;
    public DateTime creation;
}

public class ChatItem1 : ChatItem
{
    public string senderName;
    public string message;
}

public class ChatItem2 : ChatItem
{
    public string donorName;
    public int donationValue;
}

public class ChatItemsProvider : IDynamicScrollItemProvider
{
    readonly ChatItem[] _items;

    public ChatItemsProvider(ChatItem[] items)
    {
        _items = items;
    }

    public IDynamicScrollItem GetItemByIndex(int index)
    {
        // If null then no data for index.
        // Well, we can stop scrolling.
        return index >= 0 && index < _items?.Length ? _items[index] : null;
    }
}

public class ChatItemWidgetsProvider : IDynamicScrollItemWidgetProvider
{
    public IDynamicScrollItemWidget GetNewItemWidget(IDynamicScrollItem item, Transform rootNode)
    {
        if (item is ChatItem1)
            return GameObject.Instantiate(Resources.Load<ChatItemWidget1>("Prefabs/ChatItemWidget1"), rootNode);
        if (item is ChatItem2)
            return GameObject.Instantiate(Resources.Load<ChatItemWidget2>("Prefabs/ChatItemWidget2"), rootNode);

        throw new Exception("Unknown item widget type");
    }

    public void ReturnItemWidget(IDynamicScrollItemWidget itemWidget)
    {
        GameObject.Destroy(itemWidget.go);
    }
}
