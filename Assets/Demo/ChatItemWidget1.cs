﻿using TMPro;
using UnityEngine;

public class ChatItemWidget1 : MonoBehaviour, IDynamicScrollItemWidget
{
    public RectTransform rectTransform => (RectTransform)transform;

    [SerializeField]
    TextMeshProUGUI _title;
    [SerializeField]
    TextMeshProUGUI _sender;
    [SerializeField]
    TextMeshProUGUI _message;
    [SerializeField]
    TextMeshProUGUI _stamp;

    public void Fill(IDynamicScrollItem item)
    {
        FillInternal((ChatItem1)item);
    }

    void FillInternal(ChatItem1 item)
    {
        _title.text = item.GetType().Name + " " + item.id;
        _sender.text = item.senderName;
        _message.text = item.message;
        _stamp.text = item.creation.ToString();
    }
}
