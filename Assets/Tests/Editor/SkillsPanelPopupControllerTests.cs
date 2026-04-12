using NUnit.Framework;
using TMPro;
using UnityEngine;

public class SkillsPanelPopupControllerTests
{
    [Test]
    public void HideImmediate_ResetsPopupState()
    {
        GameObject popupRoot = new GameObject("PopupRoot", typeof(RectTransform), typeof(CanvasGroup));
        GameObject iconObject = new GameObject("PopupIcon", typeof(RectTransform), typeof(TextMeshProUGUI));
        GameObject textObject = new GameObject("PopupText", typeof(RectTransform), typeof(TextMeshProUGUI));

        try
        {
            RectTransform popupTransform = popupRoot.GetComponent<RectTransform>();
            popupTransform.anchoredPosition = new Vector2(5f, 10f);
            CanvasGroup popupCanvasGroup = popupRoot.GetComponent<CanvasGroup>();
            popupCanvasGroup.alpha = 1f;
            popupRoot.SetActive(true);

            SkillsPanelPopupController controller = new SkillsPanelPopupController(
                popupRoot,
                popupCanvasGroup,
                popupTransform,
                iconObject.GetComponent<TextMeshProUGUI>(),
                textObject.GetComponent<TextMeshProUGUI>());

            popupTransform.anchoredPosition = new Vector2(20f, 40f);
            controller.HideImmediate();

            Assert.AreEqual(new Vector2(20f, 40f), popupTransform.anchoredPosition);
            Assert.AreEqual(0f, popupCanvasGroup.alpha, 0.001f);
            Assert.False(popupRoot.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(popupRoot);
            Object.DestroyImmediate(iconObject);
            Object.DestroyImmediate(textObject);
        }
    }
}
