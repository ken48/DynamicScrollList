using System;
using System.Collections.Generic;

public class DynamicScrollItemViewport
{
    public enum Edge
    {
        Begin,
        End,
    }

    public static readonly Dictionary<Edge, Edge> OppositeEdges = new Dictionary<Edge, Edge>
    {
        { Edge.Begin, Edge.End },
        { Edge.End, Edge.Begin },
    };

    public static readonly Dictionary<Edge, int> EdgeInflationSigns = new Dictionary<Edge, int>
    {
        { Edge.Begin, -1 },
        { Edge.End, 1 },
    };

    readonly Func<int, bool> _onCheckItem;
    readonly Dictionary<Edge, int> _itemsIndices;

    public DynamicScrollItemViewport(Func<int, bool> onCheckItem, int startIndex = 0)
    {
        _onCheckItem = onCheckItem;
        _itemsIndices = new Dictionary<Edge, int>
        {
            { Edge.Begin, startIndex },
            { Edge.End, startIndex - 1 },
        };
    }

    public bool Inflate(Edge edge)
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

    public bool Deflate(Edge edge)
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
        return _itemsIndices[Edge.Begin] > _itemsIndices[Edge.End];
    }

    void CheckIndices()
    {
        int headIndex = _itemsIndices[Edge.Begin];
        int tailIndex = _itemsIndices[Edge.End];
        if (headIndex - tailIndex > 1)
            throw new Exception($"Wrong indices: {headIndex} {tailIndex}");
    }
}
