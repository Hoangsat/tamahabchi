using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class RoomPanelViewUtility
{
    public static RoomPanelResponsiveProfile BuildResponsiveProfile(float canvasHeight)
    {
        bool compact = canvasHeight > 0f && canvasHeight < 900f;
        bool veryCompact = canvasHeight > 0f && canvasHeight < 760f;

        return new RoomPanelResponsiveProfile
        {
            ScreenInset = veryCompact ? 14 : compact ? 18 : 24,
            ScreenSpacing = veryCompact ? 10f : compact ? 12f : 14f,
            HeaderSpacing = veryCompact ? 8f : compact ? 10f : 12f,
            CloseWidth = veryCompact ? 98f : compact ? 108f : 120f,
            CloseHeight = veryCompact ? 44f : compact ? 48f : 52f,
            TitleWidth = veryCompact ? 280f : compact ? 320f : 360f,
            HeroHeight = veryCompact ? 102f : compact ? 110f : 120f,
            HeroSidePadding = veryCompact ? 14 : compact ? 16 : 18,
            HeroTopPadding = veryCompact ? 18 : compact ? 20 : 24,
            HeroBottomPadding = veryCompact ? 12 : compact ? 14 : 16,
            HeroSpacing = veryCompact ? 4f : compact ? 5f : 6f,
            ViewportInset = veryCompact ? 10f : compact ? 12f : 16f,
            ContentSpacing = veryCompact ? 12f : compact ? 14f : 16f,
            ScrollHeight = veryCompact ? 640f : compact ? 680f : 720f,
            OverviewHeight = veryCompact ? 224f : compact ? 240f : 260f,
            NextHeight = veryCompact ? 190f : compact ? 208f : 230f,
            ActionHeight = veryCompact ? 132f : compact ? 142f : 154f,
            CardPadding = veryCompact ? 14 : compact ? 16 : 18,
            CardSpacing = veryCompact ? 10f : compact ? 11f : 12f,
            PreviewSpacing = veryCompact ? 8f : compact ? 9f : 10f,
            PreviewCardHeight = veryCompact ? 70f : compact ? 74f : 78f,
            PreviewPadding = veryCompact ? 8 : compact ? 9 : 10,
            UpgradeHeight = veryCompact ? 50f : compact ? 54f : 58f,
            TitleFontSize = veryCompact ? 34f : compact ? 38f : 42f,
            CoinsFontSize = veryCompact ? 20f : compact ? 22f : 24f,
            StatusFontSize = veryCompact ? 20f : compact ? 21f : 22f,
            HeroTitleFontSize = veryCompact ? 26f : compact ? 28f : 30f,
            HeroMetaFontSize = veryCompact ? 18f : compact ? 19f : 20f,
            HeroHintFontSize = veryCompact ? 16f : compact ? 17f : 19f,
            LevelFontSize = veryCompact ? 26f : compact ? 28f : 30f,
            CurrentVisualFontSize = veryCompact ? 21f : compact ? 22f : 24f,
            BodyFontSize = veryCompact ? 20f : compact ? 21f : 22f,
            NextTitleFontSize = veryCompact ? 24f : compact ? 26f : 28f,
            FooterFontSize = veryCompact ? 18f : compact ? 19f : 20f,
            UpgradeLabelFontSize = veryCompact ? 22f : compact ? 23f : 24f,
            PreviewLevelFontSize = veryCompact ? 16f : compact ? 17f : 18f,
            PreviewLabelFontSize = veryCompact ? 18f : compact ? 19f : 20f
        };
    }

    public static float GetReferenceCanvasHeight(RectTransform canvasRect, RectTransform screenRoot)
    {
        if (canvasRect != null && canvasRect.rect.height > 0.01f)
        {
            return canvasRect.rect.height;
        }

        if (screenRoot != null && screenRoot.rect.height > 0.01f)
        {
            return screenRoot.rect.height;
        }

        return Screen.height;
    }

    public static void ApplyLayoutPadding(HorizontalOrVerticalLayoutGroup layoutGroup, int left, int right, int top, int bottom)
    {
        if (layoutGroup == null)
        {
            return;
        }

        RectOffset padding = layoutGroup.padding;
        if (padding == null)
        {
            padding = new RectOffset();
            layoutGroup.padding = padding;
        }

        padding.left = left;
        padding.right = right;
        padding.top = top;
        padding.bottom = bottom;
    }

    public static void ApplyPanelBackground(GameObject panelRoot, Color color)
    {
        if (panelRoot == null)
        {
            return;
        }

        Image background = panelRoot.GetComponent<Image>();
        if (background != null)
        {
            background.color = color;
        }
    }

    public static void EnsurePanelLayering(Transform root, GameObject panelRoot)
    {
        if (root == null || panelRoot == null)
        {
            return;
        }

        Transform shellRuntimeRoot = root.Find("ShellRuntimeRoot");
        if (shellRuntimeRoot != null)
        {
            panelRoot.transform.SetSiblingIndex(shellRuntimeRoot.GetSiblingIndex());
        }
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

    public static TextMeshProUGUI CreateText(RectTransform parent, string name, string value, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateObject(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
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
        GameObject buttonObject = CreatePanel(parent, name, new Color(0.23f, 0.58f, 0.35f, 1f));
        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        RectTransform textRect = CreateObject("Label", buttonObject.transform as RectTransform);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonText = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        buttonText.text = label;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontSize = 24f;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.color = Color.white;
        buttonText.textWrappingMode = TextWrappingModes.Normal;
        return button;
    }

    public static Image CreateAccentStrip(RectTransform parent, string name, Color color, float height)
    {
        GameObject strip = CreatePanel(parent, name, color);
        RectTransform stripRect = strip.GetComponent<RectTransform>();
        stripRect.anchorMin = new Vector2(0f, 1f);
        stripRect.anchorMax = new Vector2(1f, 1f);
        stripRect.pivot = new Vector2(0.5f, 1f);
        stripRect.offsetMin = new Vector2(0f, -height);
        stripRect.offsetMax = Vector2.zero;
        return strip.GetComponent<Image>();
    }
}

public sealed class RoomPanelResponsiveProfile
{
    public int ScreenInset;
    public float ScreenSpacing;
    public float HeaderSpacing;
    public float CloseWidth;
    public float CloseHeight;
    public float TitleWidth;
    public float HeroHeight;
    public int HeroSidePadding;
    public int HeroTopPadding;
    public int HeroBottomPadding;
    public float HeroSpacing;
    public float ViewportInset;
    public float ContentSpacing;
    public float ScrollHeight;
    public float OverviewHeight;
    public float NextHeight;
    public float ActionHeight;
    public int CardPadding;
    public float CardSpacing;
    public float PreviewSpacing;
    public float PreviewCardHeight;
    public int PreviewPadding;
    public float UpgradeHeight;
    public float TitleFontSize;
    public float CoinsFontSize;
    public float StatusFontSize;
    public float HeroTitleFontSize;
    public float HeroMetaFontSize;
    public float HeroHintFontSize;
    public float LevelFontSize;
    public float CurrentVisualFontSize;
    public float BodyFontSize;
    public float NextTitleFontSize;
    public float FooterFontSize;
    public float UpgradeLabelFontSize;
    public float PreviewLevelFontSize;
    public float PreviewLabelFontSize;
}

public sealed class RoomPanelPreviewRefs
{
    public int Level;
    public Image BackgroundImage;
    public TextMeshProUGUI LevelText;
    public TextMeshProUGUI LabelText;
    public LayoutElement LayoutElement;
    public VerticalLayoutGroup LayoutGroup;
}
