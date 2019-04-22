using System;
using System.Collections.Generic;

public class DynamicScrollViewport
{
    readonly Func<int, bool> _onCheckItem;
    readonly Dictionary<DynamicScrollDescription.Edge, int> _itemsIndices;

    public DynamicScrollViewport(Func<int, bool> onCheckItem)
    {
        _onCheckItem = onCheckItem;
        _itemsIndices = new Dictionary<DynamicScrollDescription.Edge, int>
        {
            { DynamicScrollDescription.Edge.Head, 0 },
            { DynamicScrollDescription.Edge.Tail, -1 },
        };
    }

    public bool Inflate(DynamicScrollDescription.Edge edge)
    {
        int newIndex = _itemsIndices[edge] + DynamicScrollDescription.EdgeInflationSigns[edge];
        if (!_onCheckItem(newIndex))
            return false;

        _itemsIndices[edge] = newIndex;
        if (IsEmpty())
            _itemsIndices[DynamicScrollDescription.OppositeEdges[edge]] = newIndex;
        CheckIndices();
        return true;
    }

    public bool Deflate(DynamicScrollDescription.Edge edge)
    {
        if (IsEmpty())
            return false;

        _itemsIndices[edge] -= DynamicScrollDescription.EdgeInflationSigns[edge];
        CheckIndices();
        return true;
    }

    public int GetEdgeIndex(DynamicScrollDescription.Edge edge)
    {
        return _itemsIndices[edge];
    }

    public bool CheckEdge(DynamicScrollDescription.Edge edge)
    {
        return !_onCheckItem(_itemsIndices[edge] + DynamicScrollDescription.EdgeInflationSigns[edge]);
    }

    bool IsEmpty()
    {
        return _itemsIndices[DynamicScrollDescription.Edge.Head] > _itemsIndices[DynamicScrollDescription.Edge.Tail];
    }

    void CheckIndices()
    {
        int headIndex = _itemsIndices[DynamicScrollDescription.Edge.Head];
        int tailIndex = _itemsIndices[DynamicScrollDescription.Edge.Tail];
        if (headIndex - tailIndex > 1 || tailIndex - headIndex < -1)
            throw new Exception($"Wrong indices: {headIndex} {tailIndex}");
    }
}
