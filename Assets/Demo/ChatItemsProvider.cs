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
    public int itemsCount => _items.Length;

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
    public IDynamicScrollItemWidget GetNewItemWidget(IDynamicScrollItem item)
    {
        if (item is ChatItem1)
            return GameObject.Instantiate(Resources.Load<ChatItemWidget1>("Prefabs/ChatItemWidget1"));
        if (item is ChatItem2)
            return GameObject.Instantiate(Resources.Load<ChatItemWidget2>("Prefabs/ChatItemWidget2"));

        throw new Exception("Unknown item widget type");
    }

    public void ReturnItemWidget(IDynamicScrollItemWidget itemWidget)
    {
        // Todo: convert to Monobehaviour
        GameObject.Destroy(((MonoBehaviour)itemWidget).gameObject);
    }
}
