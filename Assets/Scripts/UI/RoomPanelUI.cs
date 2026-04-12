using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomPanelUI : MonoBehaviour
{
    public GameObject panelRoot;
    public Button closeButton;
    public Button upgradeButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI currentVisualText;
    public TextMeshProUGUI currentBonusText;
    public TextMeshProUGUI nextUpgradeText;
    public TextMeshProUGUI footerNoteText;

    private readonly Dictionary<int, Image> previewStateImages = new Dictionary<int, Image>();
    private readonly Dictionary<int, TextMeshProUGUI> previewStateLabels = new Dictionary<int, TextMeshProUGUI>();

    private GameManager gameManager;
    private RectTransform screenRoot;
    private Image heroCardBackgroundImage;
    private Image heroAccentImage;
    private TextMeshProUGUI heroTitleText;
    private TextMeshProUGUI heroMetaText;
    private TextMeshProUGUI heroHintText;
    private TextMeshProUGUI upgradeButtonLabel;
    private VerticalLayoutGroup screenLayoutGroup;
    private HorizontalLayoutGroup headerLayoutGroup;
    private LayoutElement closeButtonLayoutElement;
    private LayoutElement titleBlockLayoutElement;
    private LayoutElement heroCardLayoutElement;
    private VerticalLayoutGroup heroCardLayoutGroup;
    private LayoutElement scrollRootLayoutElement;
    private RectTransform scrollViewport;
    private VerticalLayoutGroup scrollContentLayoutGroup;
    private LayoutElement overviewCardLayoutElement;
    private VerticalLayoutGroup overviewCardLayoutGroup;
    private HorizontalLayoutGroup previewLayoutGroup;
    private readonly Dictionary<int, TextMeshProUGUI> previewStateLevelTexts = new Dictionary<int, TextMeshProUGUI>();
    private readonly List<LayoutElement> previewCardLayoutElements = new List<LayoutElement>();
    private readonly List<VerticalLayoutGroup> previewCardLayoutGroups = new List<VerticalLayoutGroup>();
    private LayoutElement nextCardLayoutElement;
    private VerticalLayoutGroup nextCardLayoutGroup;
    private TextMeshProUGUI nextCardTitleText;
    private LayoutElement actionCardLayoutElement;
    private VerticalLayoutGroup actionCardLayoutGroup;
    private LayoutElement upgradeButtonLayoutElement;

    private void Awake()
    {
        BuildUiIfNeeded();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
            closeButton.gameObject.SetActive(false);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(OnUpgradePressed);
            upgradeButton.onClick.AddListener(OnUpgradePressed);
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void OnEnable()
    {
        AttachEvents();
    }

    private void OnDisable()
    {
        DetachEvents();
    }

    public void SetGameManager(GameManager manager)
    {
        if (gameManager == manager)
        {
            return;
        }

        DetachEvents();
        gameManager = manager;
        AttachEvents();
        RefreshUI();
    }

    public void ShowPanel()
    {
        BuildUiIfNeeded();
        EnsurePanelLayering();

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(false);
        }

        SetStatus(string.Empty);
        RefreshUI();
    }

    public void HidePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        SetStatus(string.Empty);
    }

    public bool IsPanelVisible()
    {
        return panelRoot != null && panelRoot.activeSelf;
    }

    private void ClosePanel()
    {
        HidePanel();
    }

    private void AttachEvents()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnCoinsChanged -= RefreshUI;
        gameManager.OnPetChanged -= RefreshUI;
        gameManager.OnPetFlowChanged -= RefreshUI;
        gameManager.OnProgressionChanged -= RefreshUI;

        gameManager.OnCoinsChanged += RefreshUI;
        gameManager.OnPetChanged += RefreshUI;
        gameManager.OnPetFlowChanged += RefreshUI;
        gameManager.OnProgressionChanged += RefreshUI;
    }

    private void DetachEvents()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnCoinsChanged -= RefreshUI;
        gameManager.OnPetChanged -= RefreshUI;
        gameManager.OnPetFlowChanged -= RefreshUI;
        gameManager.OnProgressionChanged -= RefreshUI;
    }

    private void OnUpgradePressed()
    {
        if (gameManager == null)
        {
            return;
        }

        bool success = gameManager.TryUpgradeRoomFromPanel(out string message);
        SetStatus(message);
        if (success)
        {
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        if (panelRoot != null && !panelRoot.activeSelf)
        {
            return;
        }

        BuildUiIfNeeded();
        EnsurePanelLayering();
        ApplyScreenVisuals();
        ApplyResponsiveLayout();

        if (gameManager == null)
        {
            if (titleText != null)
            {
                titleText.text = "Room";
            }

            if (coinsText != null)
            {
                coinsText.text = "Coins: --";
            }

            if (levelText != null)
            {
                levelText.text = "Level -- / --";
            }

            RefreshHeroBlock(null);
            SetStatus("GameManager missing");
            SetPreviewState(0, 2);
            return;
        }

        RoomPanelStateData state = gameManager.GetRoomPanelState();
        RefreshHeroBlock(state);

        if (titleText != null)
        {
            titleText.text = "Room";
        }

        if (coinsText != null)
        {
            coinsText.text = $"Coins: {gameManager.GetCurrentCoins()}";
        }

        if (levelText != null)
        {
            levelText.text = $"Room level {state.currentLevel} / {state.maxLevel}";
        }

        if (currentVisualText != null)
        {
            currentVisualText.text = $"Live visual: {state.currentVisualStateLabel}";
        }

        if (currentBonusText != null)
        {
            currentBonusText.text = state.currentBonusSummary;
        }

        if (nextUpgradeText != null)
        {
            if (state.isMaxLevel)
            {
                nextUpgradeText.text = "Next upgrade: max level reached.\nYour room is already at the highest shipped state.";
            }
            else
            {
                nextUpgradeText.text =
                    $"Next state: {state.nextVisualStateLabel}\n" +
                    $"Cost: {state.currentUpgradeCost} coins\n" +
                    $"Unlock: Level {state.currentUnlockLevel}\n" +
                    $"{state.nextBonusSummary}";
            }
        }

        if (footerNoteText != null)
        {
            footerNoteText.text = state.footerNote;
        }

        if (upgradeButton != null)
        {
            upgradeButton.interactable = state.canUpgradeNow;
            Image buttonImage = upgradeButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = state.canUpgradeNow
                    ? new Color(0.23f, 0.58f, 0.35f, 1f)
                    : new Color(0.24f, 0.26f, 0.3f, 1f);
            }
        }

        if (upgradeButtonLabel != null)
        {
            upgradeButtonLabel.text = state.canUpgradeNow ? "Upgrade" : state.blockedReason;
        }

        SetPreviewState(state.activeVisualLevel, state.maxLevel);
    }

    private void RefreshHeroBlock(RoomPanelStateData state)
    {
        Color accent = state == null
            ? new Color(0.38f, 0.69f, 0.95f, 1f)
            : state.isMaxLevel
                ? new Color(0.72f, 0.52f, 0.95f, 1f)
                : new Color(0.44f, 0.81f, 0.58f, 1f);

        if (heroCardBackgroundImage != null)
        {
            heroCardBackgroundImage.color = Color.Lerp(new Color(0.14f, 0.18f, 0.27f, 0.98f), accent, 0.22f);
        }

        if (heroAccentImage != null)
        {
            heroAccentImage.color = accent;
        }

        if (heroTitleText != null)
        {
            heroTitleText.text = state == null ? "Room Upgrade Hub" : $"Room State: {state.currentVisualStateLabel}";
        }

        if (heroMetaText != null)
        {
            heroMetaText.text = state == null
                ? "Level -- | Coins --"
                : $"Level {state.currentLevel}/{state.maxLevel} | {gameManager.GetCurrentCoins()} coins";
        }

        if (heroHintText != null)
        {
            if (state == null)
            {
                heroHintText.text = "Room data unavailable.";
            }
            else if (state.isMaxLevel)
            {
                heroHintText.text = "Your room is fully upgraded in the current shipped build. Future customization can slot in later.";
            }
            else if (state.canUpgradeNow)
            {
                heroHintText.text = $"Next upgrade is ready now. Spend {state.currentUpgradeCost} coins to unlock {state.nextVisualStateLabel}.";
            }
            else
            {
                heroHintText.text = state.blockedReason;
            }
        }
    }

    private void SetStatus(string message)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = message ?? string.Empty;
        statusText.gameObject.SetActive(!string.IsNullOrEmpty(statusText.text));
    }

    private void SetPreviewState(int activeLevel, int maxLevel)
    {
        for (int i = 0; i <= maxLevel; i++)
        {
            if (previewStateImages.TryGetValue(i, out Image image) && image != null)
            {
                bool isActive = i == activeLevel;
                image.color = isActive
                    ? new Color(0.31f, 0.57f, 0.92f, 1f)
                    : new Color(0.17f, 0.21f, 0.29f, 0.95f);
            }

            if (previewStateLabels.TryGetValue(i, out TextMeshProUGUI label) && label != null)
            {
                label.color = i == activeLevel
                    ? new Color(1f, 1f, 1f, 1f)
                    : new Color(0.84f, 0.89f, 0.95f, 0.94f);
            }
        }
    }

    private void BuildUiIfNeeded()
    {
        if (panelRoot != null)
        {
            return;
        }

        RectTransform canvasRect = transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        RoomPanelLayoutRefs layoutRefs = RoomPanelLayoutBuilder.Build(canvasRect, ClosePanel, OnUpgradePressed);

        panelRoot = layoutRefs.PanelRoot;
        closeButton = layoutRefs.CloseButton;
        upgradeButton = layoutRefs.UpgradeButton;
        titleText = layoutRefs.TitleText;
        coinsText = layoutRefs.CoinsText;
        statusText = layoutRefs.StatusText;
        levelText = layoutRefs.LevelText;
        currentVisualText = layoutRefs.CurrentVisualText;
        currentBonusText = layoutRefs.CurrentBonusText;
        nextUpgradeText = layoutRefs.NextUpgradeText;
        footerNoteText = layoutRefs.FooterNoteText;
        screenRoot = layoutRefs.ScreenRoot;
        heroCardBackgroundImage = layoutRefs.HeroCardBackgroundImage;
        heroAccentImage = layoutRefs.HeroAccentImage;
        heroTitleText = layoutRefs.HeroTitleText;
        heroMetaText = layoutRefs.HeroMetaText;
        heroHintText = layoutRefs.HeroHintText;
        upgradeButtonLabel = layoutRefs.UpgradeButtonLabel;
        screenLayoutGroup = layoutRefs.ScreenLayoutGroup;
        headerLayoutGroup = layoutRefs.HeaderLayoutGroup;
        closeButtonLayoutElement = layoutRefs.CloseButtonLayoutElement;
        titleBlockLayoutElement = layoutRefs.TitleBlockLayoutElement;
        heroCardLayoutElement = layoutRefs.HeroCardLayoutElement;
        heroCardLayoutGroup = layoutRefs.HeroCardLayoutGroup;
        scrollRootLayoutElement = layoutRefs.ScrollRootLayoutElement;
        scrollViewport = layoutRefs.ScrollViewport;
        scrollContentLayoutGroup = layoutRefs.ScrollContentLayoutGroup;
        overviewCardLayoutElement = layoutRefs.OverviewCardLayoutElement;
        overviewCardLayoutGroup = layoutRefs.OverviewCardLayoutGroup;
        previewLayoutGroup = layoutRefs.PreviewLayoutGroup;
        nextCardLayoutElement = layoutRefs.NextCardLayoutElement;
        nextCardLayoutGroup = layoutRefs.NextCardLayoutGroup;
        nextCardTitleText = layoutRefs.NextCardTitleText;
        actionCardLayoutElement = layoutRefs.ActionCardLayoutElement;
        actionCardLayoutGroup = layoutRefs.ActionCardLayoutGroup;
        upgradeButtonLayoutElement = layoutRefs.UpgradeButtonLayoutElement;

        previewStateImages.Clear();
        previewStateLabels.Clear();
        previewStateLevelTexts.Clear();
        previewCardLayoutElements.Clear();
        previewCardLayoutGroups.Clear();

        foreach (RoomPanelPreviewRefs previewRefs in layoutRefs.PreviewStates)
        {
            previewStateImages[previewRefs.Level] = previewRefs.BackgroundImage;
            previewStateLabels[previewRefs.Level] = previewRefs.LabelText;
            previewStateLevelTexts[previewRefs.Level] = previewRefs.LevelText;
            previewCardLayoutElements.Add(previewRefs.LayoutElement);
            previewCardLayoutGroups.Add(previewRefs.LayoutGroup);
        }

        ApplyResponsiveLayout();
    }

    private void ApplyScreenVisuals()
    {
        RoomPanelViewUtility.ApplyPanelBackground(panelRoot, new Color(0.05f, 0.08f, 0.14f, 0.96f));
    }

    private void EnsurePanelLayering()
    {
        RoomPanelViewUtility.EnsurePanelLayering(transform, panelRoot);
    }

    private void ApplyResponsiveLayout()
    {
        RoomPanelResponsiveProfile profile = RoomPanelViewUtility.BuildResponsiveProfile(
            RoomPanelViewUtility.GetReferenceCanvasHeight(transform as RectTransform, screenRoot));

        if (screenRoot != null)
        {
            screenRoot.offsetMin = new Vector2(profile.ScreenInset, profile.ScreenInset);
            screenRoot.offsetMax = new Vector2(-profile.ScreenInset, -profile.ScreenInset);
        }

        if (screenLayoutGroup != null)
        {
            screenLayoutGroup.spacing = profile.ScreenSpacing;
        }

        if (headerLayoutGroup != null)
        {
            headerLayoutGroup.spacing = profile.HeaderSpacing;
        }

        if (closeButtonLayoutElement != null)
        {
            closeButtonLayoutElement.preferredWidth = profile.CloseWidth;
            closeButtonLayoutElement.preferredHeight = profile.CloseHeight;
        }

        if (titleBlockLayoutElement != null)
        {
            titleBlockLayoutElement.preferredWidth = profile.TitleWidth;
        }

        if (heroCardLayoutElement != null)
        {
            heroCardLayoutElement.preferredHeight = profile.HeroHeight;
        }

        RoomPanelViewUtility.ApplyLayoutPadding(heroCardLayoutGroup, profile.HeroSidePadding, profile.HeroSidePadding, profile.HeroTopPadding, profile.HeroBottomPadding);
        if (heroCardLayoutGroup != null)
        {
            heroCardLayoutGroup.spacing = profile.HeroSpacing;
        }

        if (scrollRootLayoutElement != null)
        {
            scrollRootLayoutElement.preferredHeight = profile.ScrollHeight;
        }

        if (scrollViewport != null)
        {
            scrollViewport.offsetMin = new Vector2(profile.ViewportInset, profile.ViewportInset);
            scrollViewport.offsetMax = new Vector2(-profile.ViewportInset, -profile.ViewportInset);
        }

        if (scrollContentLayoutGroup != null)
        {
            scrollContentLayoutGroup.spacing = profile.ContentSpacing;
        }

        if (overviewCardLayoutElement != null)
        {
            overviewCardLayoutElement.preferredHeight = profile.OverviewHeight;
        }

        RoomPanelViewUtility.ApplyLayoutPadding(overviewCardLayoutGroup, profile.CardPadding, profile.CardPadding, profile.CardPadding, profile.CardPadding);
        if (overviewCardLayoutGroup != null)
        {
            overviewCardLayoutGroup.spacing = profile.CardSpacing;
        }

        if (previewLayoutGroup != null)
        {
            previewLayoutGroup.spacing = profile.PreviewSpacing;
        }

        foreach (LayoutElement previewLayout in previewCardLayoutElements)
        {
            if (previewLayout != null)
            {
                previewLayout.preferredHeight = profile.PreviewCardHeight;
            }
        }

        foreach (VerticalLayoutGroup previewCardLayout in previewCardLayoutGroups)
        {
            RoomPanelViewUtility.ApplyLayoutPadding(previewCardLayout, profile.PreviewPadding, profile.PreviewPadding, profile.PreviewPadding, profile.PreviewPadding);
        }

        if (nextCardLayoutElement != null)
        {
            nextCardLayoutElement.preferredHeight = profile.NextHeight;
        }

        RoomPanelViewUtility.ApplyLayoutPadding(nextCardLayoutGroup, profile.CardPadding, profile.CardPadding, profile.CardPadding, profile.CardPadding);
        if (nextCardLayoutGroup != null)
        {
            nextCardLayoutGroup.spacing = profile.CardSpacing;
        }

        if (actionCardLayoutElement != null)
        {
            actionCardLayoutElement.preferredHeight = profile.ActionHeight;
        }

        RoomPanelViewUtility.ApplyLayoutPadding(actionCardLayoutGroup, profile.CardPadding, profile.CardPadding, profile.CardPadding, profile.CardPadding);
        if (actionCardLayoutGroup != null)
        {
            actionCardLayoutGroup.spacing = profile.CardSpacing;
        }

        if (upgradeButtonLayoutElement != null)
        {
            upgradeButtonLayoutElement.preferredHeight = profile.UpgradeHeight;
        }

        if (titleText != null)
        {
            titleText.fontSize = profile.TitleFontSize;
        }

        if (coinsText != null)
        {
            coinsText.fontSize = profile.CoinsFontSize;
        }

        if (statusText != null)
        {
            statusText.fontSize = profile.StatusFontSize;
        }

        if (heroTitleText != null)
        {
            heroTitleText.fontSize = profile.HeroTitleFontSize;
        }

        if (heroMetaText != null)
        {
            heroMetaText.fontSize = profile.HeroMetaFontSize;
        }

        if (heroHintText != null)
        {
            heroHintText.fontSize = profile.HeroHintFontSize;
        }

        if (levelText != null)
        {
            levelText.fontSize = profile.LevelFontSize;
        }

        if (currentVisualText != null)
        {
            currentVisualText.fontSize = profile.CurrentVisualFontSize;
        }

        if (currentBonusText != null)
        {
            currentBonusText.fontSize = profile.BodyFontSize;
        }

        if (nextCardTitleText != null)
        {
            nextCardTitleText.fontSize = profile.NextTitleFontSize;
        }

        if (nextUpgradeText != null)
        {
            nextUpgradeText.fontSize = profile.BodyFontSize;
        }

        if (footerNoteText != null)
        {
            footerNoteText.fontSize = profile.FooterFontSize;
        }

        if (upgradeButtonLabel != null)
        {
            upgradeButtonLabel.fontSize = profile.UpgradeLabelFontSize;
        }

        foreach (KeyValuePair<int, TextMeshProUGUI> pair in previewStateLevelTexts)
        {
            if (pair.Value != null)
            {
                pair.Value.fontSize = profile.PreviewLevelFontSize;
            }
        }

        foreach (KeyValuePair<int, TextMeshProUGUI> pair in previewStateLabels)
        {
            if (pair.Value != null)
            {
                pair.Value.fontSize = profile.PreviewLabelFontSize;
            }
        }
    }

}
