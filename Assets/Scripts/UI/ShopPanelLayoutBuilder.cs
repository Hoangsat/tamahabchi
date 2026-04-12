using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class ShopPanelLayoutRefs
{
    public GameObject PanelRoot;
    public Button CloseButton;
    public TextMeshProUGUI TitleText;
    public TextMeshProUGUI CoinsText;
    public TextMeshProUGUI StatusText;
    public TextMeshProUGUI EmptyStateText;
    public TextMeshProUGUI EquippedSkinText;
    public RectTransform ScreenRoot;
    public RectTransform TabRow;
    public RectTransform ScrollContent;
    public readonly Dictionary<ShopCategory, Button> TabButtons = new Dictionary<ShopCategory, Button>();
    public readonly Dictionary<ShopCategory, TextMeshProUGUI> TabButtonLabels = new Dictionary<ShopCategory, TextMeshProUGUI>();

    public bool HasCoreReferences()
    {
        return PanelRoot != null &&
               ScreenRoot != null &&
               TabRow != null &&
               ScrollContent != null &&
               TitleText != null &&
               CoinsText != null &&
               StatusText != null;
    }
}

public static class ShopPanelLayoutBuilder
{
    public static ShopPanelLayoutRefs BuildLayout(
        RectTransform canvasRect,
        UnityAction onClose,
        Action<ShopCategory> onCategorySelected)
    {
        ShopPanelLayoutRefs refs = new ShopPanelLayoutRefs();
        if (canvasRect == null)
        {
            return refs;
        }

        refs.PanelRoot = ShopPanelViewUtility.CreatePanel(canvasRect, "ShopPanelRoot", new Color(0.05f, 0.09f, 0.14f, 0.96f));
        RectTransform panelRect = refs.PanelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = new Vector2(0f, 146f);
        panelRect.offsetMax = Vector2.zero;

        refs.ScreenRoot = ShopPanelViewUtility.CreateObject("ShopScreenRoot", panelRect);
        refs.ScreenRoot.anchorMin = Vector2.zero;
        refs.ScreenRoot.anchorMax = Vector2.one;
        refs.ScreenRoot.offsetMin = new Vector2(28f, 24f);
        refs.ScreenRoot.offsetMax = new Vector2(-28f, -24f);

        VerticalLayoutGroup rootLayout = refs.ScreenRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(0, 0, 0, 0);
        rootLayout.spacing = 18f;
        rootLayout.childAlignment = TextAnchor.UpperCenter;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        RectTransform headerRow = ShopPanelViewUtility.CreateObject("HeaderRow", refs.ScreenRoot);
        HorizontalLayoutGroup headerLayout = headerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 12f;
        headerLayout.childAlignment = TextAnchor.MiddleCenter;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = false;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;
        LayoutElement headerElement = headerRow.gameObject.AddComponent<LayoutElement>();
        headerElement.preferredHeight = 64f;

        refs.TitleText = ShopPanelViewUtility.CreateText(headerRow, "TitleText", "Shop", 42f, FontStyles.Bold, TextAlignmentOptions.Left);
        LayoutElement titleLayout = refs.TitleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredWidth = 320f;
        titleLayout.flexibleWidth = 1f;

        refs.CoinsText = ShopPanelViewUtility.CreateText(headerRow, "CoinsText", "Coins: 0", 26f, FontStyles.Bold, TextAlignmentOptions.Center);
        LayoutElement coinsLayout = refs.CoinsText.gameObject.AddComponent<LayoutElement>();
        coinsLayout.preferredWidth = 240f;

        refs.CloseButton = ShopPanelViewUtility.CreateActionButton(headerRow, "CloseButton", "Back", onClose);
        LayoutElement closeLayout = refs.CloseButton.gameObject.AddComponent<LayoutElement>();
        closeLayout.preferredWidth = 160f;
        closeLayout.preferredHeight = 52f;

        refs.TabRow = ShopPanelViewUtility.CreateObject("TabRow", refs.ScreenRoot);
        HorizontalLayoutGroup tabLayout = refs.TabRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 10f;
        tabLayout.childAlignment = TextAnchor.MiddleCenter;
        tabLayout.childControlWidth = true;
        tabLayout.childControlHeight = true;
        tabLayout.childForceExpandWidth = true;
        tabLayout.childForceExpandHeight = false;
        LayoutElement tabRowLayout = refs.TabRow.gameObject.AddComponent<LayoutElement>();
        tabRowLayout.preferredHeight = 60f;

        CreateTabButton(refs, ShopCategory.Food, onCategorySelected);
        CreateTabButton(refs, ShopCategory.Energy, onCategorySelected);
        CreateTabButton(refs, ShopCategory.Mood, onCategorySelected);
        CreateTabButton(refs, ShopCategory.Skins, onCategorySelected);
        CreateTabButton(refs, ShopCategory.Special, onCategorySelected);

        refs.StatusText = ShopPanelViewUtility.CreateText(
            refs.ScreenRoot,
            "StatusText",
            ShopPanelPresenter.GetCategoryStatus(ShopCategory.Food),
            22f,
            FontStyles.Normal,
            TextAlignmentOptions.Left);
        refs.StatusText.textWrappingMode = TextWrappingModes.Normal;
        refs.StatusText.color = new Color(0.83f, 0.89f, 0.96f, 0.96f);

        refs.EquippedSkinText = ShopPanelViewUtility.CreateText(
            refs.ScreenRoot,
            "EquippedSkinText",
            "Equipped skin: Default",
            20f,
            FontStyles.Normal,
            TextAlignmentOptions.Left);
        refs.EquippedSkinText.color = new Color(0.73f, 0.86f, 0.98f, 0.92f);

        GameObject scrollView = ShopPanelViewUtility.CreatePanel(refs.ScreenRoot, "ScrollView", new Color(0.09f, 0.12f, 0.18f, 0.7f));
        LayoutElement scrollLayout = scrollView.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1f;
        scrollLayout.preferredHeight = 600f;

        RectTransform viewport = ShopPanelViewUtility.CreatePanel(scrollView.transform as RectTransform, "Viewport", new Color(0f, 0f, 0f, 0f)).GetComponent<RectTransform>();
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(12f, 12f);
        viewport.offsetMax = new Vector2(-12f, -12f);
        viewport.gameObject.AddComponent<RectMask2D>();

        refs.ScrollContent = ShopPanelViewUtility.CreateObject("ContentHost", viewport);
        refs.ScrollContent.anchorMin = new Vector2(0f, 1f);
        refs.ScrollContent.anchorMax = new Vector2(1f, 1f);
        refs.ScrollContent.pivot = new Vector2(0.5f, 1f);
        refs.ScrollContent.anchoredPosition = Vector2.zero;
        refs.ScrollContent.sizeDelta = Vector2.zero;

        VerticalLayoutGroup contentLayout = refs.ScrollContent.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(20, 20, 20, 20);
        contentLayout.spacing = 14f;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = refs.ScrollContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        refs.EmptyStateText = ShopPanelViewUtility.CreateText(refs.ScrollContent, "EmptyStateText", string.Empty, 22f, FontStyles.Normal, TextAlignmentOptions.Center);
        refs.EmptyStateText.gameObject.SetActive(false);

        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.viewport = viewport;
        scrollRect.content = refs.ScrollContent;
        ScrollRectPerformanceHelper.Optimize(scrollView, scrollRect);

        return refs;
    }

    public static ShopPanelLayoutRefs ResolveExisting(GameObject panelRoot)
    {
        ShopPanelLayoutRefs refs = new ShopPanelLayoutRefs
        {
            PanelRoot = panelRoot
        };

        if (panelRoot == null)
        {
            return refs;
        }

        RectTransform panelRect = panelRoot.transform as RectTransform;
        if (panelRect == null)
        {
            return refs;
        }

        refs.ScreenRoot = panelRect.Find("ShopScreenRoot") as RectTransform;
        if (refs.ScreenRoot == null)
        {
            return refs;
        }

        EnsureStaticContentHost(refs);

        VerticalLayoutGroup existingRootLayout = refs.ScreenRoot.GetComponent<VerticalLayoutGroup>();
        if (existingRootLayout != null)
        {
            existingRootLayout.childControlWidth = true;
            existingRootLayout.childControlHeight = true;
            existingRootLayout.childForceExpandWidth = true;
            existingRootLayout.childForceExpandHeight = false;
        }

        refs.TabRow = refs.ScreenRoot.Find("TabRow") as RectTransform;
        refs.TitleText = refs.ScreenRoot.Find("HeaderRow/TitleText")?.GetComponent<TextMeshProUGUI>();
        refs.CoinsText = refs.ScreenRoot.Find("HeaderRow/CoinsText")?.GetComponent<TextMeshProUGUI>();
        refs.CloseButton = refs.ScreenRoot.Find("HeaderRow/CloseButton")?.GetComponent<Button>();
        refs.StatusText = refs.ScreenRoot.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
        refs.EquippedSkinText = refs.ScreenRoot.Find("EquippedSkinText")?.GetComponent<TextMeshProUGUI>();
        refs.EmptyStateText =
            refs.ScreenRoot.Find("ScrollView/Viewport/ContentHost/EmptyStateText")?.GetComponent<TextMeshProUGUI>() ??
            refs.ScreenRoot.Find("ScrollView/ContentHost/EmptyStateText")?.GetComponent<TextMeshProUGUI>() ??
            refs.ScreenRoot.Find("ScrollView/Viewport/Content/EmptyStateText")?.GetComponent<TextMeshProUGUI>();

        RegisterTab(refs, ShopCategory.Food);
        RegisterTab(refs, ShopCategory.Energy);
        RegisterTab(refs, ShopCategory.Mood);
        RegisterTab(refs, ShopCategory.Skins);
        RegisterTab(refs, ShopCategory.Special);

        return refs;
    }

    private static void EnsureStaticContentHost(ShopPanelLayoutRefs refs)
    {
        if (refs == null || refs.ScreenRoot == null)
        {
            return;
        }

        RectTransform scrollView = refs.ScreenRoot.Find("ScrollView") as RectTransform;
        if (scrollView == null)
        {
            return;
        }

        RectTransform viewport = scrollView.Find("Viewport") as RectTransform;
        if (viewport == null)
        {
            viewport = ShopPanelViewUtility.CreatePanel(scrollView, "Viewport", new Color(0f, 0f, 0f, 0f)).GetComponent<RectTransform>();
        }

        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(12f, 12f);
        viewport.offsetMax = new Vector2(-12f, -12f);

        if (viewport.GetComponent<RectMask2D>() == null)
        {
            viewport.gameObject.AddComponent<RectMask2D>();
        }

        RectTransform contentHost = viewport.Find("ContentHost") as RectTransform;
        RectTransform legacyContentHost = scrollView.Find("ContentHost") as RectTransform;
        if (contentHost == null && legacyContentHost != null)
        {
            contentHost = legacyContentHost;
            contentHost.SetParent(viewport, false);
        }

        if (contentHost == null)
        {
            contentHost = ShopPanelViewUtility.CreateObject("ContentHost", viewport);
        }

        contentHost.anchorMin = new Vector2(0f, 1f);
        contentHost.anchorMax = new Vector2(1f, 1f);
        contentHost.pivot = new Vector2(0.5f, 1f);
        contentHost.anchoredPosition = Vector2.zero;
        contentHost.sizeDelta = Vector2.zero;
        contentHost.localScale = Vector3.one;

        VerticalLayoutGroup contentLayout = contentHost.GetComponent<VerticalLayoutGroup>();
        if (contentLayout == null)
        {
            contentLayout = contentHost.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        contentLayout.padding = new RectOffset(20, 20, 20, 20);
        contentLayout.spacing = 14f;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentHost.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = contentHost.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = scrollView.gameObject.AddComponent<ScrollRect>();
        }

        scrollRect.horizontal = false;
        scrollRect.viewport = viewport;
        scrollRect.content = contentHost;
        ScrollRectPerformanceHelper.Optimize(scrollView.gameObject, scrollRect);

        refs.ScrollContent = contentHost;

        if (refs.EmptyStateText == null)
        {
            refs.EmptyStateText = contentHost.Find("EmptyStateText")?.GetComponent<TextMeshProUGUI>();
            if (refs.EmptyStateText == null)
            {
                refs.EmptyStateText = ShopPanelViewUtility.CreateText(contentHost, "EmptyStateText", string.Empty, 22f, FontStyles.Normal, TextAlignmentOptions.Center);
                refs.EmptyStateText.gameObject.SetActive(false);
            }
        }
    }

    private static void CreateTabButton(ShopPanelLayoutRefs refs, ShopCategory category, Action<ShopCategory> onCategorySelected)
    {
        if (refs == null || refs.TabRow == null)
        {
            return;
        }

        Button button = ShopPanelViewUtility.CreateActionButton(
            refs.TabRow,
            category + "TabButton",
            ShopPanelPresenter.GetTabLabel(category),
            () => onCategorySelected?.Invoke(category));
        LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 54f;
        refs.TabButtons[category] = button;

        TextMeshProUGUI labelText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (labelText != null)
        {
            refs.TabButtonLabels[category] = labelText;
        }
    }

    private static void RegisterTab(ShopPanelLayoutRefs refs, ShopCategory category)
    {
        if (refs == null || refs.TabRow == null)
        {
            return;
        }

        Button button = refs.TabRow.Find(category + "TabButton")?.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        refs.TabButtons[category] = button;
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            refs.TabButtonLabels[category] = label;
        }
    }
}
