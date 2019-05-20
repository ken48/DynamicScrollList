using UnityEngine;
using UnityEngine.UI;
using DynamicScroll;

public class ChatItemWidget1 : MonoBehaviour, IWidget
{
    public GameObject go => gameObject;
    public RectTransform rectTransform => (RectTransform)transform;

    [SerializeField]
    Text _title;
    [SerializeField]
    Text _sender;
    [SerializeField]
    Text _message;
    [SerializeField]
    Text _stamp;

    public void Fill(IItem item)
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
