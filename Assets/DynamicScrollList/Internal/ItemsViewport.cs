using System;
using System.Collections.Generic;

namespace DynamicScroll.Internal
{
    internal class ItemsViewport
    {
        readonly Dictionary<ItemsEdge, int> _itemsIndices;
        readonly IItemsProvider _itemsProvider;

        // Todo: variable start item index
        public ItemsViewport(IItemsProvider itemsProvider, int startIndex = 0)
        {
            _itemsProvider = itemsProvider;
            _itemsIndices = new Dictionary<ItemsEdge, int>
            {
                { ItemsEdge.Head, startIndex },
                { ItemsEdge.Tail, startIndex - 1 },
            };
        }

        public bool TryInflate(ItemsEdge edge)
        {
            int newIndex = _itemsIndices[edge] + ItemsEdgeDesc.InflationSigns[edge];
            if (!CheckItem(newIndex))
                return false;

            _itemsIndices[edge] = newIndex;
            if (IsEmpty())
                _itemsIndices[ItemsEdgeDesc.Opposites[edge]] = newIndex;
            CheckIndices();
            return true;
        }

        public bool TryDeflate(ItemsEdge edge)
        {
            if (IsEmpty())
                return false;

            _itemsIndices[edge] -= ItemsEdgeDesc.InflationSigns[edge];
            CheckIndices();
            return true;
        }

        public int GetEdgeIndex(ItemsEdge edge)
        {
            return _itemsIndices[edge];
        }

        public bool CheckEdge(ItemsEdge edge)
        {
            return !CheckItem(_itemsIndices[edge] + ItemsEdgeDesc.InflationSigns[edge]);
        }

        bool IsEmpty()
        {
            return _itemsIndices[ItemsEdge.Head] > _itemsIndices[ItemsEdge.Tail];
        }

        bool CheckItem(int index)
        {
            return _itemsProvider.GetItemByIndex(index) != null;
        }

        void CheckIndices()
        {
            int headIndex = _itemsIndices[ItemsEdge.Head];
            int tailIndex = _itemsIndices[ItemsEdge.Tail];
            if (headIndex - tailIndex > 1)
                throw new Exception($"Wrong indices: {headIndex} {tailIndex}");
        }
    }
}
