using UnityEngine;
using TMPro;
using DynamicScroll;

public class ChatItemWidget2 : MonoBehaviour, IWidget
{
    public GameObject go => gameObject;
    public RectTransform rectTransform => (RectTransform)transform;

    [SerializeField]
    TextMeshProUGUI _title;
    [SerializeField]
    TextMeshProUGUI _sender;
    [SerializeField]
    TextMeshProUGUI _message;
    [SerializeField]
    TextMeshProUGUI _stamp;

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
