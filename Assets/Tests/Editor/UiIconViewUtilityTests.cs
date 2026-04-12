using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiIconViewUtilityTests
{
    [Test]
    public void ApplyIconToTextSlot_CreatesOverlayAndDisablesText()
    {
        GameObject textObject = new GameObject("IconText", typeof(RectTransform), typeof(TextMeshProUGUI));

        try
        {
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = "DEV";

            UiIconViewUtility.ApplyIconToTextSlot(text, "DEV");

            Transform overlay = text.transform.Find("IconSprite");
            Assert.NotNull(overlay);
            Assert.NotNull(overlay.GetComponent<Image>());
            Assert.False(text.enabled);
            Assert.True(overlay.gameObject.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(textObject);
        }
    }
}
