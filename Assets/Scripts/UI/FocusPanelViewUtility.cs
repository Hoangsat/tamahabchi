using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class FocusPanelViewUtility
{
    public static RectTransform CreateScrollContent(RectTransform parent, string name)
    {
        GameObject root = CreatePanel(parent, name, new Color(0.1f, 0.13f, 0.2f, 0.82f), false);
        LayoutElement rootLayout = root.AddComponent<LayoutElement>();
        rootLayout.preferredHeight = 420f;
        ScrollRect scrollRect = root.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        GameObject viewport = CreateObject("Viewport", root.transform as RectTransform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(10f, 10f);
        viewportRect.offsetMax = new Vector2(-10f, -10f);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
        viewport.AddComponent<RectMask2D>();

        GameObject content = CreateObject("Content", viewportRect);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        ScrollRectPerformanceHelper.Optimize(root, scrollRect);
        return contentRect;
    }

    public static GameObject CreateColumn(RectTransform parent, string name)
    {
        GameObject column = CreateObject(name, parent);
        VerticalLayoutGroup layout = column.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        LayoutElement element = column.AddComponent<LayoutElement>();
        element.flexibleHeight = 1f;
        return column;
    }

    public static Button CreateButton(RectTransform parent, string name, string label, UnityAction onClick)
    {
        GameObject root = CreatePanel(parent, name, new Color(0.2f, 0.28f, 0.42f, 1f), false);
        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.preferredHeight = 64f;
        Button button = root.AddComponent<Button>();
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        TextMeshProUGUI text = CreateText(root.transform as RectTransform, "Label", label, 22f, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
    }

    public static TextMeshProUGUI CreateText(RectTransform parent, string name, string value, float size, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject root = CreateObject(name, parent);
        TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = new Color(0.96f, 0.98f, 1f, 1f);
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    public static GameObject CreatePanel(RectTransform parent, string name, Color color, bool stretch)
    {
        GameObject root = CreateObject(name, parent);
        Image image = root.AddComponent<Image>();
        image.color = color;
        if (stretch)
        {
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        return root;
    }

    public static GameObject CreateObject(string name, RectTransform parent)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return root;
    }

    public static void SetButtonSelected(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (image != null)
        {
            image.color = selected ? new Color(0.28f, 0.55f, 0.86f, 1f) : new Color(0.2f, 0.28f, 0.42f, 1f);
        }

        if (text != null)
        {
            text.color = selected ? Color.white : new Color(0.93f, 0.96f, 1f, 1f);
        }
    }
}
