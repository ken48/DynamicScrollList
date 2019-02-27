using TMPro;
using UnityEngine;

public class ChatItemWidget2 : MonoBehaviour, IDynamicScrollItemWidget
{
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
