using System;
using System.Collections.Generic;

public enum ViewportEdge
{
    Head,
    Tail,
}

public class DynamicScrollViewport
{
    public static readonly Dictionary<ViewportEdge, ViewportEdge> OppositeEdges = new Dictionary<ViewportEdge, ViewportEdge>
    {
        { ViewportEdge.Head, ViewportEdge.Tail },
        { ViewportEdge.Tail, ViewportEdge.Head },
    };

    public static readonly Dictionary<ViewportEdge, int> InflationShifts = new Dictionary<ViewportEdge, int>
    {
        { ViewportEdge.Head, -1 },
        { ViewportEdge.Tail, 1 },
    };

    readonly Func<int, bool> _onCheckItem;
    readonly Dictionary<ViewportEdge, int> _indices;

    public DynamicScrollViewport(Func<int, bool> onCheckItem)
    {
        _onCheckItem = onCheckItem;
        _indices = new Dictionary<ViewportEdge, int>
        {
            { ViewportEdge.Head, 0 },
            { ViewportEdge.Tail, -1 },
        };
    }

    public bool Inflate(ViewportEdge edge)
    {
        int newIndex = _indices[edge] + InflationShifts[edge];
        if (!_onCheckItem(newIndex))
            return false;

        _indices[edge] = newIndex;
        if (IsEmpty())
            _indices[OppositeEdges[edge]] = newIndex;
        CheckIndices();
        return true;
    }

    public bool Deflate(ViewportEdge edge)
    {
        if (IsEmpty())
            return false;

        _indices[edge] -= InflationShifts[edge];
        CheckIndices();
        return true;
    }

    public int GetEdgeIndex(ViewportEdge edge)
    {
        return _indices[edge];
    }

    public bool CheckEdge(ViewportEdge edge)
    {
        return !_onCheckItem(_indices[edge] + InflationShifts[edge]);
    }

    bool IsEmpty()
    {
        return _indices[ViewportEdge.Head] > _indices[ViewportEdge.Tail];
    }

    void CheckIndices()
    {
        int headIndex = _indices[ViewportEdge.Head];
        int tailIndex = _indices[ViewportEdge.Tail];
        if (headIndex - tailIndex > 1 || tailIndex - headIndex < -1)
            throw new Exception($"Wrong indices: {headIndex} {tailIndex}");
    }
}
