using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class MissionPanelSectionHeaderRefs
{
    public GameObject root;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
}

public sealed class MissionPanelBonusCardRefs
{
    public GameObject root;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI progressText;
    public Button claimButton;
    public TextMeshProUGUI claimButtonText;
}

public static class MissionPanelViewUtility
{
    private static readonly Color SelectedChoiceColor = new Color(0.28f, 0.56f, 0.92f, 1f);
    private static readonly Color UnselectedChoiceColor = new Color(0.17f, 0.24f, 0.36f, 1f);

    public static T EnsureCached<T>(List<T> cache, int index, Func<int, T> factory) where T : class
    {
        while (cache.Count <= index)
        {
            cache.Add(factory(cache.Count));
        }

        return cache[index];
    }

    public static void HideUnused<T>(List<T> cache, int usedCount) where T : Component
    {
        for (int i = usedCount; i < cache.Count; i++)
        {
            if (cache[i] != null)
            {
                cache[i].gameObject.SetActive(false);
            }
        }
    }

    public static void PrepareContentRoot(GameObject root, int siblingIndex)
    {
        if (root == null)
        {
            return;
        }

        root.SetActive(true);
        root.transform.SetSiblingIndex(siblingIndex);
    }

    public static void ConfigureChoiceButton(Button button, string label, UnityAction action, bool selected, int siblingIndex)
    {
        if (button == null)
        {
            return;
        }

        button.gameObject.SetActive(true);
        button.transform.SetSiblingIndex(siblingIndex);
        button.onClick.RemoveAllListeners();
        if (action != null)
        {
            button.onClick.AddListener(action);
        }

        TextMeshProUGUI labelText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (labelText != null)
        {
            labelText.text = label ?? string.Empty;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = selected ? SelectedChoiceColor : UnselectedChoiceColor;
        }
    }

    public static RectTransform CreateObject(string name, RectTransform parent)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        return rect;
    }

    public static GameObject CreatePanel(RectTransform parent, string name, Color color, bool stretch)
    {
        RectTransform rect = CreateObject(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        if (stretch)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        return rect.gameObject;
    }

    public static TextMeshProUGUI CreateText(RectTransform parent, string name, string value, float size, FontStyles style, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateObject(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = new Color(0.96f, 0.98f, 1f, 1f);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.enableAutoSizing = false;
        return text;
    }

    public static Button CreateActionButton(RectTransform parent, string name, string label, UnityAction action)
    {
        GameObject root = CreatePanel(parent, name, new Color(0.24f, 0.33f, 0.5f, 1f), false);
        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.preferredHeight = 60f;

        Button button = root.AddComponent<Button>();
        if (action != null)
        {
            button.onClick.AddListener(action);
        }

        TextMeshProUGUI text = CreateText(root.transform as RectTransform, "Label", label, 22f, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        return button;
    }

    public static void ApplyButtonSizing(Button button, float preferredWidth, float preferredHeight, bool flexibleWidth)
    {
        if (button == null)
        {
            return;
        }

        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = button.gameObject.AddComponent<LayoutElement>();
        }

        if (preferredWidth > 0f)
        {
            layout.preferredWidth = preferredWidth;
            layout.minWidth = preferredWidth;
        }

        layout.preferredHeight = preferredHeight;
        layout.minHeight = preferredHeight;
        layout.flexibleWidth = flexibleWidth ? 1f : 0f;
    }

    public static RectTransform CreateScrollContent(RectTransform parent, string name)
    {
        RectTransform root = CreateObject(name, parent);
        LayoutElement rootLayout = root.gameObject.AddComponent<LayoutElement>();
        rootLayout.flexibleWidth = 1f;
        rootLayout.flexibleHeight = 1f;
        rootLayout.minHeight = 400f;

        ScrollRect scrollRect = root.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        RectTransform viewportRect = CreateObject("Viewport", root);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportRect.gameObject.AddComponent<RectMask2D>();

        RectTransform contentRect = CreateObject("Content", viewportRect);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup contentLayout = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 18f;
        contentLayout.padding = new RectOffset(0, 0, 0, 24);
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentRect.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        ScrollRectPerformanceHelper.Optimize(root.gameObject, scrollRect);
        return contentRect;
    }

    public static MissionPanelSectionHeaderRefs CreateSectionHeader(RectTransform parent, string name)
    {
        GameObject card = CreatePanel(parent, name, new Color(0.09f, 0.13f, 0.2f, 0.96f), false);
        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 18, 18);
        layout.spacing = 6f;

        MissionPanelSectionHeaderRefs refs = new MissionPanelSectionHeaderRefs
        {
            root = card,
            titleText = CreateText(card.transform as RectTransform, "Title", string.Empty, 30f, FontStyles.Bold, TextAlignmentOptions.Left),
            subtitleText = CreateText(card.transform as RectTransform, "Subtitle", string.Empty, 18f, FontStyles.Normal, TextAlignmentOptions.Left)
        };
        refs.subtitleText.color = new Color(0.83f, 0.89f, 0.98f, 0.84f);
        refs.root.SetActive(false);
        return refs;
    }

    public static MissionPanelBonusCardRefs CreateBonusCard(RectTransform parent, UnityAction onClaimBonus)
    {
        GameObject card = CreatePanel(parent, "BonusCard", new Color(0.16f, 0.21f, 0.14f, 0.98f), false);
        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 10f;

        MissionPanelBonusCardRefs refs = new MissionPanelBonusCardRefs
        {
            root = card,
            titleText = CreateText(card.transform as RectTransform, "Title", "5/5 Skill Mission Bonus", 28f, FontStyles.Bold, TextAlignmentOptions.Left),
            progressText = CreateText(card.transform as RectTransform, "Progress", string.Empty, 20f, FontStyles.Normal, TextAlignmentOptions.Left),
            claimButton = CreateActionButton(card.transform as RectTransform, "BonusClaimButton", "Claim Bonus", onClaimBonus)
        };

        ApplyButtonSizing(refs.claimButton, 0f, 60f, true);
        refs.claimButtonText = refs.claimButton.GetComponentInChildren<TextMeshProUGUI>(true);
        refs.root.SetActive(false);
        return refs;
    }
}
