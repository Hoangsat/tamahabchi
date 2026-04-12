using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class HUDViewUtilityTests
{
    [Test]
    public void EnsureSideActionsRoot_CreatesExpectedAnchoredContainer()
    {
        GameObject homeRoot = new GameObject("HomeRoot", typeof(RectTransform));
        try
        {
            GameObject sideActionsRoot = HUDViewUtility.EnsureSideActionsRoot(homeRoot.transform, null);

            Assert.NotNull(sideActionsRoot);
            Assert.AreEqual("HomeSideActionsRoot", sideActionsRoot.name);

            RectTransform sideRect = sideActionsRoot.GetComponent<RectTransform>();
            Assert.NotNull(sideRect);
            Assert.AreEqual(new Vector2(1f, 0.5f), sideRect.anchorMin);
            Assert.AreEqual(new Vector2(1f, 0.5f), sideRect.anchorMax);
            Assert.AreEqual(new Vector2(140f, 180f), sideRect.sizeDelta);

            VerticalLayoutGroup layout = sideActionsRoot.GetComponent<VerticalLayoutGroup>();
            Assert.NotNull(layout);
            Assert.AreEqual(10f, layout.spacing, 0.01f);
            Assert.AreEqual(TextAnchor.MiddleRight, layout.childAlignment);
        }
        finally
        {
            Object.DestroyImmediate(homeRoot);
        }
    }
}
