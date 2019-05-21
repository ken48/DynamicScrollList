using UnityEngine;

namespace DynamicScroll
{
    public interface IItem
    {
    }

    public interface IItemsProvider
    {
        IItem GetItemByIndex(int index);
    }

    public interface IWidget
    {
        GameObject go { get; }
        RectTransform rectTransform { get; }
        void Fill(IItem item);
        void RecalcRect();
    }

    public interface IWidgetsProvider
    {
        IWidget GetNewWidget(IItem item, Transform rootNode);
        void ReturnWidget(IWidget widget);
    }
}