using System;
using UnityEngine;

public class DynamicScrollViewport : MonoBehaviour
{
    [SerializeField]
    DynamicScrollContent _dynamicContent;
    
    IDynamicScrollItemProvider _itemProvider;
    int _headIndex;
    int _tailIndex;
    
    public void Init(IDynamicScrollItemProvider itemProvider)
    {
        _itemProvider = itemProvider;
        _headIndex = _tailIndex = -1;
    }
    
    public bool HeadMovePrevious()
    {
        // Todo: стоит завязаться не на виджеты и не только на данные, но и на индексы head и tail.
        // Чтобы возможность сдвига индекса считалась еще и от того, насколько сам индекс может быть сдвинут:
        // Например, чтобы хвост не мог быть меньше головы. Надо как-то об этом подумать...
        // Хотя нет, никак. Если мы сместили резко все виджеты за вьюпорт, то надо понимать, за счет какого сдвига
        // это произошло: в сторону головы или хвоста.
        
        int newHeadIndex = _headIndex - 1;
        IDynamicScrollItem newHeadItem = _itemProvider.GetItemByIndex(newHeadIndex);
        if (newHeadItem == null)
            return false;

        _headIndex = newHeadIndex;
        CheckIndices();
        
        _dynamicContent.PushHead(newHeadItem);
        return true;
    }
    
    public bool HeadMoveNext()
    {
//        if (headWidget == null)
//            return false;

        _headIndex++;
        if (_tailIndex < _headIndex)
            _tailIndex = _headIndex;
        CheckIndices();
        
        _dynamicContent.PopHead();
        return true;
    }

    public bool TailMovePrevious()
    {
//        if (tailWidget == null)
//            return false;
        
        _tailIndex--;
        if (_headIndex > _tailIndex)
            _headIndex = _tailIndex;
        CheckIndices();

        _dynamicContent.PopTail();
        return true;
    }

    public bool TailMoveNext()
    {
        int newTailIndex = _tailIndex + 1;
        IDynamicScrollItem newTailItem = _itemProvider.GetItemByIndex(newTailIndex);
        if (newTailItem == null)
            return false;
        
        _tailIndex = newTailIndex;
        if (_headIndex == -1)
            _headIndex = _tailIndex;
        CheckIndices();

        _dynamicContent.PushTail(newTailItem);
        return true;
    }
    
    void CheckIndices()
    {
        if (_headIndex > _tailIndex)
            throw new Exception($"Wrong indices: {_headIndex} {_tailIndex}");
    }
}
