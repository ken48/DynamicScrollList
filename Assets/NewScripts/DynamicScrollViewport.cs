using System;

public class DynamicScrollViewport
{
    public int headIndex => _headIndex;
    public int tailIndex => _tailIndex;

    readonly Func<int, bool> _onCheckItem;
    int _headIndex;
    int _tailIndex;

    public DynamicScrollViewport(Func<int, bool> onCheckItem)
    {
        _onCheckItem = onCheckItem;
        _headIndex = _tailIndex = -1;
    }

    public bool HeadMovePrevious()
    {
        int newHeadIndex = _headIndex - 1;
        if (!_onCheckItem(newHeadIndex))
            return false;

        if (IsEmpty())
            _tailIndex = newHeadIndex;
        _headIndex = newHeadIndex;
        CheckIndices();

        return true;
    }

    public bool TailMoveNext()
    {
        int newTailIndex = _tailIndex + 1;
        if (!_onCheckItem(newTailIndex))
            return false;

        if (IsEmpty())
            _headIndex = newTailIndex;

        _tailIndex = newTailIndex;
        CheckIndices();

        return true;
    }

    public bool HeadMoveNext()
    {
        if (IsEmpty())
            return false;

        _headIndex++;
        CheckIndices();

        return true;
    }

    public bool TailMovePrevious()
    {
        if (IsEmpty())
            return false;

        _tailIndex--;
        CheckIndices();

        return true;
    }

    bool IsEmpty()
    {
        return _headIndex > _tailIndex;
    }

    void CheckIndices()
    {
        if (_headIndex - _tailIndex > 1 || _tailIndex - _headIndex < -1)
            throw new Exception($"Wrong indices: {_headIndex} {_tailIndex}");
    }
}
