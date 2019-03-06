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

// Todo: maybe move to common code
class DynamicScrollItemIterator : IDynamicScrollItemProviderIterator
{
    public IDynamicScrollItem current => _current >= 0 && _current < _items?.Length ? _items[_current] : null;
    public bool isStart => _current == -1; 

    IDynamicScrollItem[] _items;
    int _current;

    public DynamicScrollItemIterator(IDynamicScrollItem[] items)
    {
        _items = items;
        SetCurrent(-1);
    }

    public void MovePrevious()
    {
        SetCurrent(_current - 1);
    }
        
    public void MoveNext()
    {
        SetCurrent(_current + 1);
    }

    void SetCurrent(int value)
    {
        _current = Mathf.Clamp(value, -1, _items.Length);
    }
}

public class ChatItemsProvider : IDynamicScrollItemProvider
{    
    readonly ChatItem[] _items;

    public ChatItemsProvider(ChatItem[] items)
    {
        _items = items;
    }

    public IDynamicScrollItemProviderIterator GetIterator()
    {
        return new DynamicScrollItemIterator(_items);
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
