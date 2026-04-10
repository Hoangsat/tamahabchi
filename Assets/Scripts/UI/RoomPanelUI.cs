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
    private RectTransform previewRow;
    private TextMeshProUGUI upgradeButtonLabel;

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
        BuildUiIfNeeded();
        EnsurePanelLayering();
        ApplyScreenVisuals();

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

        panelRoot = CreatePanel(canvasRect, "RoomPanelRoot", new Color(0.05f, 0.08f, 0.14f, 0.96f));
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = new Vector2(0f, 146f);
        panelRect.offsetMax = Vector2.zero;

        screenRoot = CreateObject("RoomScreenRoot", panelRect);
        screenRoot.anchorMin = Vector2.zero;
        screenRoot.anchorMax = Vector2.one;
        screenRoot.offsetMin = new Vector2(24f, 24f);
        screenRoot.offsetMax = new Vector2(-24f, -24f);

        VerticalLayoutGroup screenLayout = screenRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        screenLayout.padding = new RectOffset(0, 0, 0, 0);
        screenLayout.spacing = 14f;
        screenLayout.childAlignment = TextAnchor.UpperCenter;
        screenLayout.childControlWidth = true;
        screenLayout.childControlHeight = false;
        screenLayout.childForceExpandWidth = true;
        screenLayout.childForceExpandHeight = false;

        RectTransform headerRow = CreateObject("HeaderRow", screenRoot);
        HorizontalLayoutGroup headerLayout = headerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 12f;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childControlWidth = false;
        headerLayout.childControlHeight = false;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;

        closeButton = CreateActionButton(headerRow, "CloseButton", "Back", ClosePanel);
        LayoutElement closeLayout = closeButton.gameObject.AddComponent<LayoutElement>();
        closeLayout.preferredWidth = 120f;
        closeLayout.preferredHeight = 52f;

        RectTransform titleBlock = CreateObject("TitleBlock", headerRow);
        LayoutElement titleBlockLayout = titleBlock.gameObject.AddComponent<LayoutElement>();
        titleBlockLayout.preferredWidth = 360f;
        titleBlockLayout.flexibleWidth = 1f;

        VerticalLayoutGroup titleLayout = titleBlock.gameObject.AddComponent<VerticalLayoutGroup>();
        titleLayout.spacing = 4f;
        titleLayout.childAlignment = TextAnchor.MiddleLeft;
        titleLayout.childControlWidth = true;
        titleLayout.childControlHeight = false;
        titleLayout.childForceExpandWidth = true;
        titleLayout.childForceExpandHeight = false;

        titleText = CreateText(titleBlock, "TitleText", "Room", 42f, FontStyles.Bold, TextAlignmentOptions.Left);
        coinsText = CreateText(titleBlock, "CoinsText", "Coins: --", 24f, FontStyles.Normal, TextAlignmentOptions.Left);
        coinsText.color = new Color(1f, 0.86f, 0.4f, 1f);

        statusText = CreateText(screenRoot, "StatusText", string.Empty, 22f, FontStyles.Normal, TextAlignmentOptions.Left);
        statusText.color = new Color(0.98f, 0.82f, 0.52f, 1f);
        statusText.gameObject.SetActive(false);

        RectTransform heroCard = CreatePanel(screenRoot, "HeroCard", new Color(0.14f, 0.18f, 0.27f, 0.98f)).GetComponent<RectTransform>();
        LayoutElement heroLayout = heroCard.gameObject.AddComponent<LayoutElement>();
        heroLayout.preferredHeight = 120f;
        VerticalLayoutGroup heroCardLayout = heroCard.gameObject.AddComponent<VerticalLayoutGroup>();
        heroCardLayout.padding = new RectOffset(18, 18, 24, 16);
        heroCardLayout.spacing = 6f;
        heroCardLayout.childAlignment = TextAnchor.UpperLeft;
        heroCardLayout.childControlWidth = true;
        heroCardLayout.childControlHeight = false;
        heroCardLayout.childForceExpandWidth = true;
        heroCardLayout.childForceExpandHeight = false;

        heroCardBackgroundImage = heroCard.GetComponent<Image>();
        heroAccentImage = CreateAccentStrip(heroCard, "HeroAccentStrip", new Color(0.44f, 0.81f, 0.58f, 1f), 12f);
        heroTitleText = CreateText(heroCard, "HeroTitleText", "Room Upgrade Hub", 30f, FontStyles.Bold, TextAlignmentOptions.Left);
        heroMetaText = CreateText(heroCard, "HeroMetaText", "Level -- | Coins --", 20f, FontStyles.Bold, TextAlignmentOptions.Left);
        heroMetaText.color = new Color(1f, 0.88f, 0.48f, 1f);
        heroHintText = CreateText(heroCard, "HeroHintText", "Upgrade your room to improve the long-term comfort loop for your pet.", 19f, FontStyles.Normal, TextAlignmentOptions.Left);
        heroHintText.textWrappingMode = TextWrappingModes.Normal;
        heroHintText.color = new Color(0.86f, 0.91f, 0.97f, 0.95f);

        RectTransform scrollRoot = CreatePanel(screenRoot, "ScrollRoot", new Color(0.08f, 0.11f, 0.17f, 0.92f)).GetComponent<RectTransform>();
        LayoutElement scrollRootLayout = scrollRoot.gameObject.AddComponent<LayoutElement>();
        scrollRootLayout.flexibleHeight = 1f;
        scrollRootLayout.preferredHeight = 720f;

        RectTransform viewport = CreatePanel(scrollRoot, "Viewport", new Color(0f, 0f, 0f, 0f)).GetComponent<RectTransform>();
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(16f, 16f);
        viewport.offsetMax = new Vector2(-16f, -16f);
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        RectTransform content = CreateObject("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.spacing = 16f;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter contentSize = content.gameObject.AddComponent<ContentSizeFitter>();
        contentSize.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSize.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.viewport = viewport;
        scrollRect.content = content;

        GameObject overviewCard = CreatePanel(content, "OverviewCard", new Color(0.16f, 0.21f, 0.31f, 0.98f));
        LayoutElement overviewLayout = overviewCard.AddComponent<LayoutElement>();
        overviewLayout.preferredHeight = 260f;
        VerticalLayoutGroup overviewCardLayout = overviewCard.AddComponent<VerticalLayoutGroup>();
        overviewCardLayout.padding = new RectOffset(18, 18, 18, 18);
        overviewCardLayout.spacing = 12f;
        overviewCardLayout.childAlignment = TextAnchor.UpperLeft;
        overviewCardLayout.childControlWidth = true;
        overviewCardLayout.childControlHeight = false;
        overviewCardLayout.childForceExpandWidth = true;
        overviewCardLayout.childForceExpandHeight = false;

        levelText = CreateText(overviewCard.transform as RectTransform, "LevelText", "Room level -- / --", 30f, FontStyles.Bold, TextAlignmentOptions.Left);
        currentVisualText = CreateText(overviewCard.transform as RectTransform, "CurrentVisualText", "Live visual: --", 24f, FontStyles.Normal, TextAlignmentOptions.Left);
        currentVisualText.color = new Color(0.84f, 0.9f, 0.96f, 0.95f);

        previewRow = CreateObject("PreviewRow", overviewCard.transform as RectTransform);
        HorizontalLayoutGroup previewLayout = previewRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        previewLayout.spacing = 10f;
        previewLayout.childAlignment = TextAnchor.MiddleCenter;
        previewLayout.childControlWidth = true;
        previewLayout.childControlHeight = false;
        previewLayout.childForceExpandWidth = true;
        previewLayout.childForceExpandHeight = false;

        CreatePreviewState(previewRow, 0, "Starter");
        CreatePreviewState(previewRow, 1, "Cozy");
        CreatePreviewState(previewRow, 2, "Dream");

        currentBonusText = CreateText(overviewCard.transform as RectTransform, "CurrentBonusText", string.Empty, 22f, FontStyles.Normal, TextAlignmentOptions.Left);
        currentBonusText.textWrappingMode = TextWrappingModes.Normal;
        currentBonusText.color = new Color(0.87f, 0.92f, 0.97f, 0.95f);

        GameObject nextCard = CreatePanel(content, "NextUpgradeCard", new Color(0.14f, 0.18f, 0.28f, 0.98f));
        LayoutElement nextLayout = nextCard.AddComponent<LayoutElement>();
        nextLayout.preferredHeight = 230f;
        VerticalLayoutGroup nextCardLayout = nextCard.AddComponent<VerticalLayoutGroup>();
        nextCardLayout.padding = new RectOffset(18, 18, 18, 18);
        nextCardLayout.spacing = 12f;
        nextCardLayout.childAlignment = TextAnchor.UpperLeft;
        nextCardLayout.childControlWidth = true;
        nextCardLayout.childControlHeight = false;
        nextCardLayout.childForceExpandWidth = true;
        nextCardLayout.childForceExpandHeight = false;

        CreateText(nextCard.transform as RectTransform, "NextTitleText", "Next upgrade", 28f, FontStyles.Bold, TextAlignmentOptions.Left);
        nextUpgradeText = CreateText(nextCard.transform as RectTransform, "NextUpgradeText", string.Empty, 22f, FontStyles.Normal, TextAlignmentOptions.Left);
        nextUpgradeText.textWrappingMode = TextWrappingModes.Normal;
        nextUpgradeText.color = new Color(0.86f, 0.91f, 0.97f, 0.95f);

        GameObject actionCard = CreatePanel(content, "ActionCard", new Color(0.16f, 0.2f, 0.18f, 0.98f));
        LayoutElement actionLayout = actionCard.AddComponent<LayoutElement>();
        actionLayout.preferredHeight = 154f;
        VerticalLayoutGroup actionCardLayout = actionCard.AddComponent<VerticalLayoutGroup>();
        actionCardLayout.padding = new RectOffset(18, 18, 18, 18);
        actionCardLayout.spacing = 12f;
        actionCardLayout.childAlignment = TextAnchor.UpperCenter;
        actionCardLayout.childControlWidth = true;
        actionCardLayout.childControlHeight = false;
        actionCardLayout.childForceExpandWidth = true;
        actionCardLayout.childForceExpandHeight = false;

        upgradeButton = CreateActionButton(actionCard.transform as RectTransform, "UpgradeButton", "Upgrade", OnUpgradePressed);
        LayoutElement upgradeLayout = upgradeButton.gameObject.AddComponent<LayoutElement>();
        upgradeLayout.preferredHeight = 58f;
        upgradeLayout.preferredWidth = 0f;

        footerNoteText = CreateText(actionCard.transform as RectTransform, "FooterNoteText", "Customization coming later.", 20f, FontStyles.Italic, TextAlignmentOptions.Center);
        footerNoteText.color = new Color(0.8f, 0.87f, 0.94f, 0.88f);

        upgradeButtonLabel = upgradeButton.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void ApplyScreenVisuals()
    {
        if (panelRoot == null)
        {
            return;
        }

        Image background = panelRoot.GetComponent<Image>();
        if (background != null)
        {
            background.color = new Color(0.05f, 0.08f, 0.14f, 0.96f);
        }
    }

    private void EnsurePanelLayering()
    {
        if (panelRoot != null)
        {
            Transform shellRuntimeRoot = transform.Find("ShellRuntimeRoot");
            if (shellRuntimeRoot != null)
            {
                panelRoot.transform.SetSiblingIndex(shellRuntimeRoot.GetSiblingIndex());
            }
        }
    }

    private void CreatePreviewState(RectTransform parent, int level, string label)
    {
        GameObject stateCard = CreatePanel(parent, $"PreviewState_{level}", new Color(0.17f, 0.21f, 0.29f, 0.95f));
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

        CreateText(stateCard.transform as RectTransform, "BadgeLevel", $"Lv {level}", 18f, FontStyles.Bold, TextAlignmentOptions.Center);
        TextMeshProUGUI labelText = CreateText(stateCard.transform as RectTransform, "BadgeLabel", label, 20f, FontStyles.Normal, TextAlignmentOptions.Center);
        labelText.color = new Color(0.84f, 0.89f, 0.95f, 0.94f);

        previewStateImages[level] = stateCard.GetComponent<Image>();
        previewStateLabels[level] = labelText;
    }

    private Image CreateAccentStrip(RectTransform parent, string name, Color color, float height)
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

    private GameObject CreatePanel(RectTransform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private RectTransform CreateObject(string name, RectTransform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return rect;
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string name, string value, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
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

    private Button CreateActionButton(RectTransform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
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
}
