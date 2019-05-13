using System;
using System.Collections.Generic;
using UnityEngine;

//
// Interfaces
//

public interface IDynamicScrollItem
{
}

public interface IDynamicScrollItemProvider
{
    IDynamicScrollItem GetItemByIndex(int index);
}

public interface IDynamicScrollItemWidget
{
    GameObject go { get; }
    RectTransform rectTransform { get; }
    void Fill(IDynamicScrollItem item);
}

public interface IDynamicScrollItemWidgetProvider
{
    IDynamicScrollItemWidget GetNewItemWidget(IDynamicScrollItem item, Transform rootNode);
    void ReturnItemWidget(IDynamicScrollItemWidget itemWidget);
}

//
// Helpers
//

public static class DynamicScrollHelpers
{
    // Todo: move to DynamicContent somehow... The problem is optimization only
    // How not to calculate this each time
    public static Rect GetWorldRect(RectTransform rectTransform)
    {
        Rect rect = rectTransform.rect;
        Vector2 worldRectMin = rectTransform.TransformPoint(rect.min);
        Vector2 worldRectMax = rectTransform.TransformPoint(rect.max);
        return Rect.MinMaxRect(worldRectMin.x, worldRectMin.y, worldRectMax.x, worldRectMax.y);
    }

    public static bool CheckVectorMagnitude(Vector2 vector)
    {
        return vector.sqrMagnitude >= 1e-6f;
    }
}
