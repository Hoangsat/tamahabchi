using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FocusPanelViewUtilityTests
{
    [Test]
    public void SetButtonSelected_UpdatesButtonAndLabelColors()
    {
        GameObject buttonObject = new GameObject("FocusButton", typeof(RectTransform), typeof(Image), typeof(Button));
        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        try
        {
            labelObject.transform.SetParent(buttonObject.transform, false);
            Button button = buttonObject.GetComponent<Button>();
            Image image = buttonObject.GetComponent<Image>();
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();

            FocusPanelViewUtility.SetButtonSelected(button, true);
            Assert.AreEqual(new Color(0.28f, 0.55f, 0.86f, 1f), image.color);
            Assert.AreEqual(Color.white, label.color);

            FocusPanelViewUtility.SetButtonSelected(button, false);
            Assert.AreEqual(new Color(0.2f, 0.28f, 0.42f, 1f), image.color);
            Assert.AreEqual(new Color(0.93f, 0.96f, 1f, 1f), label.color);
        }
        finally
        {
            Object.DestroyImmediate(buttonObject);
        }
    }
}
