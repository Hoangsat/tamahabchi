using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class UiIconViewUtility
{
    public static void ApplyIconToTextSlot(TextMeshProUGUI textSlot, string iconId)
    {
        if (textSlot == null)
        {
            return;
        }

        Sprite sprite = UiIconProvider.GetSprite(iconId);
        Image overlayImage = EnsureOverlayImage(textSlot);
        bool hasSprite = sprite != null;

        if (overlayImage != null)
        {
            overlayImage.sprite = sprite;
            overlayImage.enabled = hasSprite;
            overlayImage.gameObject.SetActive(hasSprite);
        }

        textSlot.enabled = !hasSprite;
    }

    private static Image EnsureOverlayImage(TextMeshProUGUI textSlot)
    {
        Transform existing = textSlot.transform.Find("IconSprite");
        if (existing != null)
        {
            Image existingImage = existing.GetComponent<Image>();
            if (existingImage != null)
            {
                return existingImage;
            }
        }

        GameObject iconObject = new GameObject("IconSprite", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.SetParent(textSlot.transform, false);
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(4f, 4f);
        iconRect.offsetMax = new Vector2(-4f, -4f);
        iconRect.localScale = Vector3.one;

        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.color = Color.white;
        iconImage.enabled = false;
        iconObject.SetActive(false);
        return iconImage;
    }
}
