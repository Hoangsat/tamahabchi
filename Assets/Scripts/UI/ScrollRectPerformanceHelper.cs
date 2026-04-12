using UnityEngine;
using UnityEngine.UI;

public static class ScrollRectPerformanceHelper
{
    public static void Optimize(GameObject scrollRoot, ScrollRect scrollRect, float scrollSensitivity = 28f)
    {
        if (scrollRoot == null)
        {
            return;
        }

        Canvas isolatedCanvas = scrollRoot.GetComponent<Canvas>();
        if (isolatedCanvas == null)
        {
            isolatedCanvas = scrollRoot.AddComponent<Canvas>();
        }

        isolatedCanvas.overrideSorting = false;
        isolatedCanvas.pixelPerfect = false;

        if (scrollRoot.GetComponent<GraphicRaycaster>() == null)
        {
            scrollRoot.AddComponent<GraphicRaycaster>();
        }

        if (scrollRect == null)
        {
            return;
        }

        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = scrollSensitivity;
        scrollRect.inertia = true;
    }
}
