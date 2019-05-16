using System;
using UnityEngine;
using DynamicScroll;

public class ChatItem : IItem
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

public class ChatItemsProvider : IItemsProvider
{
    ChatItem[] _items;

    public ChatItemsProvider(ChatItem[] items)
    {
        _items = items;
    }

    public IItem GetItemByIndex(int index)
    {
        return index >= 0 && index < _items?.Length ? _items[index] : null;
    }
}

public class ChatItemWidgetsProvider : IWidgetsProvider
{
    public IWidget GetNewWidget(IItem item, Transform rootNode)
    {
        if (item is ChatItem1)
            return GameObject.Instantiate(Resources.Load<ChatItemWidget1>("Prefabs/ChatItemWidget1"), rootNode);
        if (item is ChatItem2)
            return GameObject.Instantiate(Resources.Load<ChatItemWidget2>("Prefabs/ChatItemWidget2"), rootNode);

        throw new Exception("Unknown item widget type");
    }

    public void ReturnWidget(IWidget itemWidget)
    {
        if (itemWidget as Component != null)
            GameObject.Destroy(itemWidget.go);
    }
}
