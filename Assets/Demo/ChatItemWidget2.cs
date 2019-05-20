using UnityEngine;
using UnityEngine.UI;
using DynamicScroll;

public class ChatItemWidget2 : MonoBehaviour, IWidget
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
        FillInternal((ChatItem2)item);
    }

    void FillInternal(ChatItem2 item)
    {
        _title.text = item.GetType().Name + " " + item.id;
        _sender.text = item.donorName;
        _message.text = item.donationValue.ToString();
        _stamp.text = item.creation.ToString();
    }
}
