using System;
using System.Collections.Generic;

public class DynamicScrollItemViewport
{
    public enum Edge
    {
        Head,
        Tail,
    }

    public static readonly Dictionary<Edge, Edge> OppositeEdges = new Dictionary<Edge, Edge>
    {
        { Edge.Head, Edge.Tail },
        { Edge.Tail, Edge.Head },
    };

    public static readonly Dictionary<Edge, int> EdgeInflationSigns = new Dictionary<Edge, int>
    {
        { Edge.Head, -1 },
        { Edge.Tail, 1 },
    };

    readonly Func<int, bool> _onCheckItem;
    readonly Dictionary<Edge, int> _itemsIndices;

    public DynamicScrollItemViewport(Func<int, bool> onCheckItem, int startIndex = 0)
    {
        _onCheckItem = onCheckItem;
        _itemsIndices = new Dictionary<Edge, int>
        {
            { Edge.Head, startIndex },
            { Edge.Tail, startIndex - 1 },
        };
    }

    public bool TryInflate(Edge edge)
    {
        int newIndex = _itemsIndices[edge] + EdgeInflationSigns[edge];
        if (!_onCheckItem(newIndex))
            return false;

        _itemsIndices[edge] = newIndex;
        if (IsEmpty())
            _itemsIndices[OppositeEdges[edge]] = newIndex;
        CheckIndices();
        return true;
    }

    public bool TryDeflate(Edge edge)
    {
        if (IsEmpty())
            return false;

        _itemsIndices[edge] -= EdgeInflationSigns[edge];
        CheckIndices();
        return true;
    }

    public int GetEdgeIndex(Edge edge)
    {
        return _itemsIndices[edge];
    }

    public bool CheckEdge(Edge edge)
    {
        return !_onCheckItem(_itemsIndices[edge] + EdgeInflationSigns[edge]);
    }

    bool IsEmpty()
    {
        return _itemsIndices[Edge.Head] > _itemsIndices[Edge.Tail];
    }

    void CheckIndices()
    {
        int headIndex = _itemsIndices[Edge.Head];
        int tailIndex = _itemsIndices[Edge.Tail];
        if (headIndex - tailIndex > 1)
            throw new Exception($"Wrong indices: {headIndex} {tailIndex}");
    }
}
