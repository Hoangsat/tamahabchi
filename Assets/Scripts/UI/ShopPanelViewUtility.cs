using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class ShopPanelViewUtility
{
    public static void EnsurePanelLayering(GameObject panelRoot, Transform ownerTransform)
    {
        if (panelRoot == null || ownerTransform == null)
        {
            return;
        }

        Transform shellRuntimeRoot = ownerTransform.Find("ShellRuntimeRoot");
        if (shellRuntimeRoot == null)
        {
            return;
        }

        panelRoot.transform.SetSiblingIndex(shellRuntimeRoot.GetSiblingIndex());
    }

    public static void UpdateTabSelection(
        Dictionary<ShopCategory, Button> tabButtons,
        Dictionary<ShopCategory, TextMeshProUGUI> tabButtonLabels,
        ShopCategory selectedCategory)
    {
        if (tabButtons == null)
        {
            return;
        }

        foreach (KeyValuePair<ShopCategory, Button> entry in tabButtons)
        {
            Image image = entry.Value != null ? entry.Value.GetComponent<Image>() : null;
            if (image != null)
            {
                image.color = entry.Key == selectedCategory
                    ? new Color(0.31f, 0.57f, 0.92f, 1f)
                    : new Color(0.18f, 0.22f, 0.3f, 1f);
            }

            if (tabButtonLabels != null &&
                tabButtonLabels.TryGetValue(entry.Key, out TextMeshProUGUI label) &&
                label != null)
            {
                label.color = entry.Key == selectedCategory
                    ? Color.white
                    : new Color(0.86f, 0.9f, 0.96f, 0.94f);
            }
        }
    }

    public static void ClearSpawnedContent(RectTransform scrollContent, TextMeshProUGUI emptyStateText, List<GameObject> spawnedContent)
    {
        if (scrollContent != null)
        {
            for (int i = scrollContent.childCount - 1; i >= 0; i--)
            {
                Transform child = scrollContent.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (emptyStateText != null && child == emptyStateText.transform)
                {
                    continue;
                }

                if (!child.name.StartsWith("ShopItem_") && child.name != "PlaceholderCard")
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Object.Destroy(child.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        if (spawnedContent == null)
        {
            return;
        }

        for (int i = 0; i < spawnedContent.Count; i++)
        {
            if (spawnedContent[i] != null)
            {
                LayoutElement layoutElement = spawnedContent[i].GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    layoutElement.ignoreLayout = true;
                }

                spawnedContent[i].SetActive(false);
                spawnedContent[i].transform.SetParent(null, false);

                if (Application.isPlaying)
                {
                    Object.Destroy(spawnedContent[i]);
                }
                else
                {
                    Object.DestroyImmediate(spawnedContent[i]);
                }
            }
        }

        spawnedContent.Clear();
    }

    public static void RebuildLayouts(RectTransform scrollContent, RectTransform screenRoot)
    {
        Canvas.ForceUpdateCanvases();

        if (scrollContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
        }

        if (screenRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(screenRoot);
        }

        Canvas.ForceUpdateCanvases();
    }

    public static GameObject CreatePanel(RectTransform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    public static RectTransform CreateObject(string name, RectTransform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return rect;
    }

    public static TextMeshProUGUI CreateText(
        RectTransform parent,
        string name,
        string value,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment)
    {
        RectTransform textRoot = CreateObject(name, parent);
        TextMeshProUGUI text = textRoot.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = new Color(0.94f, 0.97f, 1f, 1f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    public static Button CreateActionButton(RectTransform parent, string name, string label, UnityAction onClick)
    {
        GameObject buttonObject = CreatePanel(parent, name, new Color(0.22f, 0.36f, 0.58f, 1f));
        Button button = buttonObject.AddComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateText(buttonObject.transform as RectTransform, "Label", label, 20f, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.textWrappingMode = TextWrappingModes.Normal;
        return button;
    }

    public static GameObject CreateBadge(RectTransform parent, string name, string value)
    {
        GameObject badge = CreatePanel(parent, name, new Color(0.25f, 0.34f, 0.48f, 1f));
        TextMeshProUGUI text = CreateText(
            badge.transform as RectTransform,
            "Label",
            string.IsNullOrEmpty(value) ? "ITEM" : value,
            18f,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        UiIconViewUtility.ApplyIconToTextSlot(text, value);
        return badge;
    }
}
