using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class RoomPanelLayoutBuilder
{
    public static RoomPanelLayoutRefs Build(RectTransform canvasRect, UnityAction closePanel, UnityAction onUpgradePressed)
    {
        RoomPanelLayoutRefs refs = new RoomPanelLayoutRefs();

        refs.PanelRoot = RoomPanelViewUtility.CreatePanel(canvasRect, "RoomPanelRoot", new Color(0.05f, 0.08f, 0.14f, 0.96f));
        RectTransform panelRect = refs.PanelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = new Vector2(0f, 146f);
        panelRect.offsetMax = Vector2.zero;

        refs.ScreenRoot = RoomPanelViewUtility.CreateObject("RoomScreenRoot", panelRect);
        refs.ScreenRoot.anchorMin = Vector2.zero;
        refs.ScreenRoot.anchorMax = Vector2.one;
        refs.ScreenRoot.offsetMin = new Vector2(24f, 24f);
        refs.ScreenRoot.offsetMax = new Vector2(-24f, -24f);

        refs.ScreenLayoutGroup = refs.ScreenRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        refs.ScreenLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
        refs.ScreenLayoutGroup.spacing = 14f;
        refs.ScreenLayoutGroup.childAlignment = TextAnchor.UpperCenter;
        refs.ScreenLayoutGroup.childControlWidth = true;
        refs.ScreenLayoutGroup.childControlHeight = false;
        refs.ScreenLayoutGroup.childForceExpandWidth = true;
        refs.ScreenLayoutGroup.childForceExpandHeight = false;

        RectTransform headerRow = RoomPanelViewUtility.CreateObject("HeaderRow", refs.ScreenRoot);
        refs.HeaderLayoutGroup = headerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        refs.HeaderLayoutGroup.spacing = 12f;
        refs.HeaderLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
        refs.HeaderLayoutGroup.childControlWidth = false;
        refs.HeaderLayoutGroup.childControlHeight = false;
        refs.HeaderLayoutGroup.childForceExpandWidth = false;
        refs.HeaderLayoutGroup.childForceExpandHeight = false;

        refs.CloseButton = RoomPanelViewUtility.CreateActionButton(headerRow, "CloseButton", "Back", closePanel);
        refs.CloseButtonLayoutElement = refs.CloseButton.gameObject.AddComponent<LayoutElement>();
        refs.CloseButtonLayoutElement.preferredWidth = 120f;
        refs.CloseButtonLayoutElement.preferredHeight = 52f;

        RectTransform titleBlock = RoomPanelViewUtility.CreateObject("TitleBlock", headerRow);
        refs.TitleBlockLayoutElement = titleBlock.gameObject.AddComponent<LayoutElement>();
        refs.TitleBlockLayoutElement.preferredWidth = 360f;
        refs.TitleBlockLayoutElement.flexibleWidth = 1f;

        VerticalLayoutGroup titleLayout = titleBlock.gameObject.AddComponent<VerticalLayoutGroup>();
        titleLayout.spacing = 4f;
        titleLayout.childAlignment = TextAnchor.MiddleLeft;
        titleLayout.childControlWidth = true;
        titleLayout.childControlHeight = false;
        titleLayout.childForceExpandWidth = true;
        titleLayout.childForceExpandHeight = false;

        refs.TitleText = RoomPanelViewUtility.CreateText(titleBlock, "TitleText", "Room", 42f, FontStyles.Bold, TextAlignmentOptions.Left);
        refs.CoinsText = RoomPanelViewUtility.CreateText(titleBlock, "CoinsText", "Coins: --", 24f, FontStyles.Normal, TextAlignmentOptions.Left);
        refs.CoinsText.color = new Color(1f, 0.86f, 0.4f, 1f);

        refs.StatusText = RoomPanelViewUtility.CreateText(refs.ScreenRoot, "StatusText", string.Empty, 22f, FontStyles.Normal, TextAlignmentOptions.Left);
        refs.StatusText.color = new Color(0.98f, 0.82f, 0.52f, 1f);
        refs.StatusText.gameObject.SetActive(false);

        RectTransform heroCard = RoomPanelViewUtility.CreatePanel(refs.ScreenRoot, "HeroCard", new Color(0.14f, 0.18f, 0.27f, 0.98f)).GetComponent<RectTransform>();
        refs.HeroCardLayoutElement = heroCard.gameObject.AddComponent<LayoutElement>();
        refs.HeroCardLayoutElement.preferredHeight = 120f;
        refs.HeroCardLayoutGroup = heroCard.gameObject.AddComponent<VerticalLayoutGroup>();
        refs.HeroCardLayoutGroup.padding = new RectOffset(18, 18, 24, 16);
        refs.HeroCardLayoutGroup.spacing = 6f;
        refs.HeroCardLayoutGroup.childAlignment = TextAnchor.UpperLeft;
        refs.HeroCardLayoutGroup.childControlWidth = true;
        refs.HeroCardLayoutGroup.childControlHeight = false;
        refs.HeroCardLayoutGroup.childForceExpandWidth = true;
        refs.HeroCardLayoutGroup.childForceExpandHeight = false;

        refs.HeroCardBackgroundImage = heroCard.GetComponent<Image>();
        refs.HeroAccentImage = RoomPanelViewUtility.CreateAccentStrip(heroCard, "HeroAccentStrip", new Color(0.44f, 0.81f, 0.58f, 1f), 12f);
        refs.HeroTitleText = RoomPanelViewUtility.CreateText(heroCard, "HeroTitleText", "Room Upgrade Hub", 30f, FontStyles.Bold, TextAlignmentOptions.Left);
        refs.HeroMetaText = RoomPanelViewUtility.CreateText(heroCard, "HeroMetaText", "Level -- | Coins --", 20f, FontStyles.Bold, TextAlignmentOptions.Left);
        refs.HeroMetaText.color = new Color(1f, 0.88f, 0.48f, 1f);
        refs.HeroHintText = RoomPanelViewUtility.CreateText(heroCard, "HeroHintText", "Upgrade your room to improve the long-term comfort loop for your pet.", 19f, FontStyles.Normal, TextAlignmentOptions.Left);
        refs.HeroHintText.textWrappingMode = TextWrappingModes.Normal;
        refs.HeroHintText.color = new Color(0.86f, 0.91f, 0.97f, 0.95f);

        RectTransform scrollRoot = RoomPanelViewUtility.CreatePanel(refs.ScreenRoot, "ScrollRoot", new Color(0.08f, 0.11f, 0.17f, 0.92f)).GetComponent<RectTransform>();
        refs.ScrollRootLayoutElement = scrollRoot.gameObject.AddComponent<LayoutElement>();
        refs.ScrollRootLayoutElement.flexibleHeight = 1f;
        refs.ScrollRootLayoutElement.preferredHeight = 720f;

        refs.ScrollViewport = RoomPanelViewUtility.CreatePanel(scrollRoot, "Viewport", new Color(0f, 0f, 0f, 0f)).GetComponent<RectTransform>();
        refs.ScrollViewport.anchorMin = Vector2.zero;
        refs.ScrollViewport.anchorMax = Vector2.one;
        refs.ScrollViewport.offsetMin = new Vector2(16f, 16f);
        refs.ScrollViewport.offsetMax = new Vector2(-16f, -16f);
        refs.ScrollViewport.gameObject.AddComponent<RectMask2D>();

        RectTransform content = RoomPanelViewUtility.CreateObject("Content", refs.ScrollViewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;

        refs.ScrollContentLayoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();
        refs.ScrollContentLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
        refs.ScrollContentLayoutGroup.spacing = 16f;
        refs.ScrollContentLayoutGroup.childAlignment = TextAnchor.UpperCenter;
        refs.ScrollContentLayoutGroup.childControlWidth = true;
        refs.ScrollContentLayoutGroup.childControlHeight = false;
        refs.ScrollContentLayoutGroup.childForceExpandWidth = true;
        refs.ScrollContentLayoutGroup.childForceExpandHeight = false;

        ContentSizeFitter contentSize = content.gameObject.AddComponent<ContentSizeFitter>();
        contentSize.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSize.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.viewport = refs.ScrollViewport;
        scrollRect.content = content;
        ScrollRectPerformanceHelper.Optimize(scrollRoot.gameObject, scrollRect);

        GameObject overviewCard = RoomPanelViewUtility.CreatePanel(content, "OverviewCard", new Color(0.16f, 0.21f, 0.31f, 0.98f));
        refs.OverviewCardLayoutElement = overviewCard.AddComponent<LayoutElement>();
        refs.OverviewCardLayoutElement.preferredHeight = 260f;
        refs.OverviewCardLayoutGroup = overviewCard.AddComponent<VerticalLayoutGroup>();
        refs.OverviewCardLayoutGroup.padding = new RectOffset(18, 18, 18, 18);
        refs.OverviewCardLayoutGroup.spacing = 12f;
        refs.OverviewCardLayoutGroup.childAlignment = TextAnchor.UpperLeft;
        refs.OverviewCardLayoutGroup.childControlWidth = true;
        refs.OverviewCardLayoutGroup.childControlHeight = false;
        refs.OverviewCardLayoutGroup.childForceExpandWidth = true;
        refs.OverviewCardLayoutGroup.childForceExpandHeight = false;

        refs.LevelText = RoomPanelViewUtility.CreateText(overviewCard.transform as RectTransform, "LevelText", "Room level -- / --", 30f, FontStyles.Bold, TextAlignmentOptions.Left);
        refs.CurrentVisualText = RoomPanelViewUtility.CreateText(overviewCard.transform as RectTransform, "CurrentVisualText", "Live visual: --", 24f, FontStyles.Normal, TextAlignmentOptions.Left);
        refs.CurrentVisualText.color = new Color(0.84f, 0.9f, 0.96f, 0.95f);

        RectTransform previewRow = RoomPanelViewUtility.CreateObject("PreviewRow", overviewCard.transform as RectTransform);
        refs.PreviewLayoutGroup = previewRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        refs.PreviewLayoutGroup.spacing = 10f;
        refs.PreviewLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
        refs.PreviewLayoutGroup.childControlWidth = true;
        refs.PreviewLayoutGroup.childControlHeight = false;
        refs.PreviewLayoutGroup.childForceExpandWidth = true;
        refs.PreviewLayoutGroup.childForceExpandHeight = false;

        refs.PreviewStates.Add(CreatePreviewState(previewRow, 0, "Starter"));
        refs.PreviewStates.Add(CreatePreviewState(previewRow, 1, "Cozy"));
        refs.PreviewStates.Add(CreatePreviewState(previewRow, 2, "Dream"));

        refs.CurrentBonusText = RoomPanelViewUtility.CreateText(overviewCard.transform as RectTransform, "CurrentBonusText", string.Empty, 22f, FontStyles.Normal, TextAlignmentOptions.Left);
        refs.CurrentBonusText.textWrappingMode = TextWrappingModes.Normal;
        refs.CurrentBonusText.color = new Color(0.87f, 0.92f, 0.97f, 0.95f);

        GameObject nextCard = RoomPanelViewUtility.CreatePanel(content, "NextUpgradeCard", new Color(0.14f, 0.18f, 0.28f, 0.98f));
        refs.NextCardLayoutElement = nextCard.AddComponent<LayoutElement>();
        refs.NextCardLayoutElement.preferredHeight = 230f;
        refs.NextCardLayoutGroup = nextCard.AddComponent<VerticalLayoutGroup>();
        refs.NextCardLayoutGroup.padding = new RectOffset(18, 18, 18, 18);
        refs.NextCardLayoutGroup.spacing = 12f;
        refs.NextCardLayoutGroup.childAlignment = TextAnchor.UpperLeft;
        refs.NextCardLayoutGroup.childControlWidth = true;
        refs.NextCardLayoutGroup.childControlHeight = false;
        refs.NextCardLayoutGroup.childForceExpandWidth = true;
        refs.NextCardLayoutGroup.childForceExpandHeight = false;

        refs.NextCardTitleText = RoomPanelViewUtility.CreateText(nextCard.transform as RectTransform, "NextTitleText", "Next upgrade", 28f, FontStyles.Bold, TextAlignmentOptions.Left);
        refs.NextUpgradeText = RoomPanelViewUtility.CreateText(nextCard.transform as RectTransform, "NextUpgradeText", string.Empty, 22f, FontStyles.Normal, TextAlignmentOptions.Left);
        refs.NextUpgradeText.textWrappingMode = TextWrappingModes.Normal;
        refs.NextUpgradeText.color = new Color(0.86f, 0.91f, 0.97f, 0.95f);

        GameObject actionCard = RoomPanelViewUtility.CreatePanel(content, "ActionCard", new Color(0.16f, 0.2f, 0.18f, 0.98f));
        refs.ActionCardLayoutElement = actionCard.AddComponent<LayoutElement>();
        refs.ActionCardLayoutElement.preferredHeight = 154f;
        refs.ActionCardLayoutGroup = actionCard.AddComponent<VerticalLayoutGroup>();
        refs.ActionCardLayoutGroup.padding = new RectOffset(18, 18, 18, 18);
        refs.ActionCardLayoutGroup.spacing = 12f;
        refs.ActionCardLayoutGroup.childAlignment = TextAnchor.UpperCenter;
        refs.ActionCardLayoutGroup.childControlWidth = true;
        refs.ActionCardLayoutGroup.childControlHeight = false;
        refs.ActionCardLayoutGroup.childForceExpandWidth = true;
        refs.ActionCardLayoutGroup.childForceExpandHeight = false;

        refs.UpgradeButton = RoomPanelViewUtility.CreateActionButton(actionCard.transform as RectTransform, "UpgradeButton", "Upgrade", onUpgradePressed);
        refs.UpgradeButtonLayoutElement = refs.UpgradeButton.gameObject.AddComponent<LayoutElement>();
        refs.UpgradeButtonLayoutElement.preferredHeight = 58f;
        refs.UpgradeButtonLayoutElement.preferredWidth = 0f;

        refs.FooterNoteText = RoomPanelViewUtility.CreateText(actionCard.transform as RectTransform, "FooterNoteText", "Customization coming later.", 20f, FontStyles.Italic, TextAlignmentOptions.Center);
        refs.FooterNoteText.color = new Color(0.8f, 0.87f, 0.94f, 0.88f);

        refs.UpgradeButtonLabel = refs.UpgradeButton.GetComponentInChildren<TextMeshProUGUI>(true);
        return refs;
    }

    private static RoomPanelPreviewRefs CreatePreviewState(RectTransform parent, int level, string label)
    {
        GameObject stateCard = RoomPanelViewUtility.CreatePanel(parent, $"PreviewState_{level}", new Color(0.17f, 0.21f, 0.29f, 0.95f));
        LayoutElement stateLayout = stateCard.AddComponent<LayoutElement>();
        stateLayout.preferredWidth = 0f;
        stateLayout.preferredHeight = 78f;
        stateLayout.flexibleWidth = 1f;

        VerticalLayoutGroup cardLayout = stateCard.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(10, 10, 10, 10);
        cardLayout.spacing = 4f;
        cardLayout.childAlignment = TextAnchor.MiddleCenter;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = false;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        TextMeshProUGUI badgeLevelText = RoomPanelViewUtility.CreateText(stateCard.transform as RectTransform, "BadgeLevel", $"Lv {level}", 18f, FontStyles.Bold, TextAlignmentOptions.Center);
        TextMeshProUGUI labelText = RoomPanelViewUtility.CreateText(stateCard.transform as RectTransform, "BadgeLabel", label, 20f, FontStyles.Normal, TextAlignmentOptions.Center);
        labelText.color = new Color(0.84f, 0.89f, 0.95f, 0.94f);

        return new RoomPanelPreviewRefs
        {
            Level = level,
            BackgroundImage = stateCard.GetComponent<Image>(),
            LevelText = badgeLevelText,
            LabelText = labelText,
            LayoutElement = stateLayout,
            LayoutGroup = cardLayout
        };
    }
}

public sealed class RoomPanelLayoutRefs
{
    public GameObject PanelRoot;
    public RectTransform ScreenRoot;
    public Button CloseButton;
    public Button UpgradeButton;
    public TextMeshProUGUI TitleText;
    public TextMeshProUGUI CoinsText;
    public TextMeshProUGUI StatusText;
    public TextMeshProUGUI LevelText;
    public TextMeshProUGUI CurrentVisualText;
    public TextMeshProUGUI CurrentBonusText;
    public TextMeshProUGUI NextUpgradeText;
    public TextMeshProUGUI FooterNoteText;
    public Image HeroCardBackgroundImage;
    public Image HeroAccentImage;
    public TextMeshProUGUI HeroTitleText;
    public TextMeshProUGUI HeroMetaText;
    public TextMeshProUGUI HeroHintText;
    public TextMeshProUGUI UpgradeButtonLabel;
    public VerticalLayoutGroup ScreenLayoutGroup;
    public HorizontalLayoutGroup HeaderLayoutGroup;
    public LayoutElement CloseButtonLayoutElement;
    public LayoutElement TitleBlockLayoutElement;
    public LayoutElement HeroCardLayoutElement;
    public VerticalLayoutGroup HeroCardLayoutGroup;
    public LayoutElement ScrollRootLayoutElement;
    public RectTransform ScrollViewport;
    public VerticalLayoutGroup ScrollContentLayoutGroup;
    public LayoutElement OverviewCardLayoutElement;
    public VerticalLayoutGroup OverviewCardLayoutGroup;
    public HorizontalLayoutGroup PreviewLayoutGroup;
    public LayoutElement NextCardLayoutElement;
    public VerticalLayoutGroup NextCardLayoutGroup;
    public TextMeshProUGUI NextCardTitleText;
    public LayoutElement ActionCardLayoutElement;
    public VerticalLayoutGroup ActionCardLayoutGroup;
    public LayoutElement UpgradeButtonLayoutElement;
    public readonly List<RoomPanelPreviewRefs> PreviewStates = new List<RoomPanelPreviewRefs>();
}
