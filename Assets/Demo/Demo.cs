using System;
using UnityEngine;
using DynamicScroll;

public class Demo : MonoBehaviour
{
    [SerializeField]
    DynamicScrollList _scrollList;
    [SerializeField]
    [Range(0.1f, 1f)]
    float _fpsCoef = 1f;
    [SerializeField]
    [Range(1, 100)]
    int _itemsCount = 20;

    // Start instead of Awake due to OnScroll on ScrollList.Init
    void Start()
    {
        QualitySettings.vSyncCount = 0;

        var chatItems = new ChatItem[_itemsCount];
        for (int i = 0; i < chatItems.Length; i++)
            chatItems[i] = UnityEngine.Random.value < 0.5f ? Helpers.GenerateChatItem1() : Helpers.GenerateChatItem2();

        _scrollList.Init(new ChatItemsProvider(chatItems), new ChatItemWidgetsProvider(), 0);
    }

    void OnDestroy()
    {
        _scrollList.Shutdown();
    }

    void Update()
    {
        Application.targetFrameRate = Mathf.RoundToInt(60 * _fpsCoef);
    }
}

static class Helpers
{
    const string AllowedChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz#@$^*()\n";

    static readonly System.Random _rnd = new System.Random();
    static char[] _charBuffer = new char[256];
    static int _id = 0;

    static string RandomString(int minLength, int maxLength)
    {
        if (maxLength > _charBuffer.Length)
            Array.Resize(ref _charBuffer, maxLength);

        int length = _rnd.Next(minLength, maxLength + 1);
        for (int i = 0; i < length; ++i)
            _charBuffer[i] = AllowedChars[_rnd.Next(AllowedChars.Length)];

        return new string(_charBuffer, 0, length);
    }

    static T CreateBaseItem<T>() where T : ChatItem, new()
    {
        return new T
        {
            id = ++_id,
            creation = DateTime.UtcNow,
        };
    }

    public static ChatItem GenerateChatItem1()
    {
        var result = CreateBaseItem<ChatItem1>();
        result.senderName = "Ann";
        result.message = RandomString(32, 256);
        return result;
    }

    public static ChatItem GenerateChatItem2()
    {
        var result = CreateBaseItem<ChatItem2>();
        result.donorName = "John";
        result.donationValue = UnityEngine.Random.Range(1, 100);
        return result;
    }
}
