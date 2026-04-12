using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattlePanelUI : MonoBehaviour
{
    public GameObject panelRoot;
    public Button closeButton;
    public Button fightButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI playerPowerText;
    public TextMeshProUGUI playerSkillsText;
    public TextMeshProUGUI bossNameText;
    public TextMeshProUGUI bossMetaText;
    public TextMeshProUGUI bossPowerText;
    public TextMeshProUGUI resultText;
    public RadarChartGraphic bossRadarGraphic;

    private readonly List<Button> spawnedBossButtons = new List<Button>();
    private readonly List<TextMeshProUGUI> spawnedBossLabels = new List<TextMeshProUGUI>();

    private GameManager gameManager;
    private RectTransform screenRoot;
    private RectTransform bossListContent;
    private TextMeshProUGUI fightButtonLabel;
    private string selectedBossId = string.Empty;
    private BattleResultData lastResult;

    private static readonly Color[] BossPalette =
    {
        new Color(0.98f, 0.64f, 0.31f, 1f),
        new Color(0.93f, 0.34f, 0.29f, 1f),
        new Color(0.92f, 0.83f, 0.31f, 1f),
        new Color(0.65f, 0.78f, 0.98f, 1f),
        new Color(0.68f, 0.93f, 0.85f, 1f)
    };

    private void Awake()
    {
        BuildUiIfNeeded();
        BindButtons();

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
        BindButtons();
        EnsurePanelLayering();

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(false);
        }

        ResetTransientState();
        RefreshUI();
    }

    public void HidePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        ResetTransientState();
        SetStatus(string.Empty);
    }

    public bool IsPanelVisible()
    {
        return panelRoot != null && panelRoot.activeSelf;
    }

    private void AttachEvents()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnSkillsChanged -= RefreshUI;
        gameManager.OnFocusResultReady -= HandleFocusResultReady;
        gameManager.OnPetFlowChanged -= RefreshUI;

        gameManager.OnSkillsChanged += RefreshUI;
        gameManager.OnFocusResultReady += HandleFocusResultReady;
        gameManager.OnPetFlowChanged += RefreshUI;
    }

    private void DetachEvents()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnSkillsChanged -= RefreshUI;
        gameManager.OnFocusResultReady -= HandleFocusResultReady;
        gameManager.OnPetFlowChanged -= RefreshUI;
    }

    private void HandleFocusResultReady(FocusSessionResultData result)
    {
        RefreshUI();
    }

    private void ClosePanel()
    {
        HidePanel();
    }

    private void OnFightPressed()
    {
        if (gameManager == null)
        {
            return;
        }

        BossDefinitionData selectedBoss = EnsureSelectedBoss(gameManager.GetBattleBosses());
        if (selectedBoss == null)
        {
            lastResult = null;
            SetStatus("No boss selected.");
            return;
        }

        lastResult = gameManager.ResolveBattle(selectedBoss.id);
        SetStatus(lastResult != null && lastResult.wasBlocked ? lastResult.statusMessage : string.Empty);
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (panelRoot != null && !panelRoot.activeSelf)
        {
            return;
        }

        BuildUiIfNeeded();
        EnsurePanelLayering();

        if (titleText != null)
        {
            titleText.text = "Battle";
        }

        if (gameManager == null)
        {
            ApplyPlayerPreview(null);
            RebuildBossList(new List<BossDefinitionData>());
            ApplyBossDetail(null);
            ApplyResult(null);
            SetStatus("GameManager missing");
            return;
        }

        List<BossDefinitionData> bosses = gameManager.GetBattleBosses();
        BattleAvailabilityData availability = gameManager.GetBattleAvailability();
        BossDefinitionData selectedBoss = EnsureSelectedBoss(bosses);
        RebuildBossList(bosses);
        ApplyPlayerPreview(gameManager.GetBattlePlayerPreview(), availability);
        ApplyBossDetail(selectedBoss, availability);
        ApplyResult(lastResult, selectedBoss, availability);
        SetStatus(selectedBoss != null && availability != null && !availability.CanFight ? availability.blockedReason : string.Empty);
    }

    private void ApplyPlayerPreview(BattlePlayerPreviewData preview)
    {
        BattlePlayerPreviewViewData viewData = BattlePanelPresenter.BuildPlayerPreview(preview);
        if (playerPowerText != null)
        {
            playerPowerText.text = viewData.PowerText;
        }

        if (playerSkillsText != null)
        {
            playerSkillsText.text = viewData.SkillsText;
        }

    }

    private void ApplyPlayerPreview(BattlePlayerPreviewData preview, BattleAvailabilityData availability)
    {
        BattlePlayerPreviewViewData viewData = BattlePanelPresenter.BuildPlayerPreview(preview, availability);
        if (playerPowerText != null)
        {
            playerPowerText.text = viewData.PowerText;
        }

        if (playerSkillsText != null)
        {
            playerSkillsText.text = viewData.SkillsText;
        }
    }

    private void ApplyBossDetail(BossDefinitionData boss)
    {
        BattleBossDetailViewData viewData = BattlePanelPresenter.BuildBossDetail(boss);
        if (bossNameText != null)
        {
            bossNameText.text = viewData.NameText;
        }

        if (bossMetaText != null)
        {
            bossMetaText.text = viewData.MetaText;
        }

        if (bossPowerText != null)
        {
            bossPowerText.text = viewData.PowerText;
        }

        if (fightButton != null)
        {
            fightButton.interactable = viewData.CanFight;
        }

        if (fightButtonLabel != null)
        {
            fightButtonLabel.text = viewData.FightButtonText;
        }

        if (bossRadarGraphic != null)
        {
            bossRadarGraphic.SetValues(viewData.RadarValues, viewData.RadarColors);
        }

    }

    private void ApplyBossDetail(BossDefinitionData boss, BattleAvailabilityData availability)
    {
        BattleBossDetailViewData viewData = BattlePanelPresenter.BuildBossDetail(boss, availability);
        if (bossNameText != null)
        {
            bossNameText.text = viewData.NameText;
        }

        if (bossMetaText != null)
        {
            bossMetaText.text = viewData.MetaText;
        }

        if (bossPowerText != null)
        {
            bossPowerText.text = viewData.PowerText;
        }

        if (fightButton != null)
        {
            fightButton.interactable = viewData.CanFight;
        }

        if (fightButtonLabel != null)
        {
            fightButtonLabel.text = viewData.FightButtonText;
        }

        if (bossRadarGraphic != null)
        {
            bossRadarGraphic.SetValues(viewData.RadarValues, viewData.RadarColors);
        }
    }

    private void ApplyResult(BattleResultData result)
    {
        if (resultText == null)
        {
            return;
        }

        BattleResultViewData viewData = BattlePanelPresenter.BuildResult(result);
        resultText.text = viewData.Text;
        resultText.color = viewData.Color;
    }

    private void ApplyResult(BattleResultData result, BossDefinitionData selectedBoss, BattleAvailabilityData availability)
    {
        if (resultText == null)
        {
            return;
        }

        BattleResultViewData viewData = BattlePanelPresenter.BuildResult(result, selectedBoss, availability);
        resultText.text = viewData.Text;
        resultText.color = viewData.Color;
    }

    private BossDefinitionData EnsureSelectedBoss(List<BossDefinitionData> bosses)
    {
        if (bosses == null || bosses.Count == 0)
        {
            selectedBossId = string.Empty;
            return null;
        }

        for (int i = 0; i < bosses.Count; i++)
        {
            if (bosses[i] != null && bosses[i].id == selectedBossId)
            {
                return bosses[i];
            }
        }

        selectedBossId = bosses[0].id;
        if (gameManager != null)
        {
            gameManager.SelectBattleBoss(selectedBossId);
        }

        return bosses[0];
    }

    private void RebuildBossList(List<BossDefinitionData> bosses)
    {
        if (bossListContent == null)
        {
            return;
        }

        while (spawnedBossButtons.Count < (bosses != null ? bosses.Count : 0))
        {
            CreateBossListButton(spawnedBossButtons.Count);
        }

        int activeCount = bosses != null ? bosses.Count : 0;
        for (int i = 0; i < spawnedBossButtons.Count; i++)
        {
            bool active = i < activeCount && bosses[i] != null;
            spawnedBossButtons[i].gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            BossDefinitionData boss = bosses[i];
            bool isSelected = boss.id == selectedBossId;
            BattleBossListEntryViewData viewData = BattlePanelPresenter.BuildBossListEntry(boss, isSelected);
            Image buttonImage = spawnedBossButtons[i].GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = viewData.BackgroundColor;
            }

            spawnedBossLabels[i].text = viewData.LabelText;
            spawnedBossLabels[i].color = viewData.LabelColor;

            spawnedBossButtons[i].onClick.RemoveAllListeners();
            string bossId = boss.id;
            spawnedBossButtons[i].onClick.AddListener(() => SelectBoss(bossId));
        }

        LayoutRebuilder.MarkLayoutForRebuild(bossListContent);
    }

    private void SelectBoss(string bossId)
    {
        selectedBossId = bossId ?? string.Empty;
        if (gameManager != null)
        {
            gameManager.SelectBattleBoss(selectedBossId);
        }

        lastResult = null;
        RefreshUI();
    }

    private void ResetTransientState()
    {
        lastResult = null;
        selectedBossId = string.Empty;
        if (gameManager != null)
        {
            gameManager.SelectBattleBoss(string.Empty);
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

    private void BindButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
            closeButton.gameObject.SetActive(false);
        }

        if (fightButton != null)
        {
            fightButton.onClick.RemoveListener(OnFightPressed);
            fightButton.onClick.AddListener(OnFightPressed);
        }
    }

    private void BuildUiIfNeeded()
    {
        if (panelRoot != null && NeedsRuntimeLayoutRebuild())
        {
            DestroyRuntimeLayout();
        }

        if (panelRoot != null)
        {
            return;
        }

        RectTransform canvasRect = transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        panelRoot = CreatePanel(canvasRect, "BattlePanelRoot", new Color(0.05f, 0.08f, 0.14f, 0.96f));
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = new Vector2(0f, 146f);
        panelRect.offsetMax = Vector2.zero;

        screenRoot = CreateObject("BattleScreenRoot", panelRect);
        screenRoot.anchorMin = Vector2.zero;
        screenRoot.anchorMax = Vector2.one;
        screenRoot.offsetMin = new Vector2(24f, 24f);
        screenRoot.offsetMax = new Vector2(-24f, -24f);

        VerticalLayoutGroup screenLayout = screenRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        screenLayout.padding = new RectOffset(0, 0, 0, 0);
        screenLayout.spacing = 14f;
        screenLayout.childAlignment = TextAnchor.UpperLeft;
        screenLayout.childControlWidth = true;
        screenLayout.childControlHeight = true;
        screenLayout.childForceExpandWidth = true;
        screenLayout.childForceExpandHeight = false;

        RectTransform headerCard = CreateCard(screenRoot, "HeaderCard", 84f, new Color(0.2f, 0.22f, 0.32f, 0.98f));
        VerticalLayoutGroup headerLayout = headerCard.gameObject.AddComponent<VerticalLayoutGroup>();
        headerLayout.padding = new RectOffset(18, 18, 16, 14);
        headerLayout.spacing = 8f;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = true;
        headerLayout.childForceExpandHeight = false;

        titleText = CreateText(headerCard, "TitleText", "Battle", 34f, FontStyles.Bold, TextAlignmentOptions.Left);
        statusText = CreateText(headerCard, "StatusText", string.Empty, 21f, FontStyles.Normal, TextAlignmentOptions.Left);
        statusText.color = new Color(0.95f, 0.75f, 0.56f, 1f);
        statusText.gameObject.SetActive(false);

        RectTransform scrollRoot = CreateCard(screenRoot, "BattleScrollRoot", 0f, new Color(0.08f, 0.11f, 0.17f, 0.92f));
        LayoutElement scrollRootLayout = scrollRoot.GetComponent<LayoutElement>();
        scrollRootLayout.minHeight = 0f;
        scrollRootLayout.preferredHeight = 0f;
        scrollRootLayout.flexibleHeight = 1f;

        RectTransform viewport = CreatePanel(scrollRoot, "Viewport", new Color(0f, 0f, 0f, 0f)).GetComponent<RectTransform>();
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(16f, 16f);
        viewport.offsetMax = new Vector2(-16f, -16f);
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform content = CreateObject("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.spacing = 14f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter contentSize = content.gameObject.AddComponent<ContentSizeFitter>();
        contentSize.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSize.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        ScrollRect mainScroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
        mainScroll.horizontal = false;
        mainScroll.viewport = viewport;
        mainScroll.content = content;
        mainScroll.scrollSensitivity = 28f;
        mainScroll.movementType = ScrollRect.MovementType.Clamped;
        ScrollRectPerformanceHelper.Optimize(scrollRoot.gameObject, mainScroll);

        RectTransform playerCard = CreateCard(content, "PlayerCard", 250f, new Color(0.2f, 0.18f, 0.32f, 0.98f));
        VerticalLayoutGroup playerLayout = playerCard.gameObject.AddComponent<VerticalLayoutGroup>();
        playerLayout.padding = new RectOffset(18, 18, 16, 16);
        playerLayout.spacing = 8f;
        playerLayout.childAlignment = TextAnchor.UpperLeft;
        playerLayout.childControlWidth = true;
        playerLayout.childControlHeight = true;
        playerLayout.childForceExpandWidth = true;
        playerLayout.childForceExpandHeight = false;

        CreateText(playerCard, "PlayerTitle", "Player Battle Power", 24f, FontStyles.Bold, TextAlignmentOptions.Left);
        playerPowerText = CreateText(playerCard, "PlayerPowerText", "Player Battle Power: --", 30f, FontStyles.Bold, TextAlignmentOptions.Left);
        playerSkillsText = CreateText(playerCard, "PlayerSkillsText", string.Empty, 18f, FontStyles.Normal, TextAlignmentOptions.Left);
        playerSkillsText.enableAutoSizing = true;
        playerSkillsText.fontSizeMin = 14f;
        playerSkillsText.fontSizeMax = 18f;
        playerSkillsText.textWrappingMode = TextWrappingModes.Normal;

        RectTransform bossListCard = CreateCard(content, "BossListCard", 390f, new Color(0.14f, 0.18f, 0.26f, 0.96f));
        VerticalLayoutGroup bossListLayout = bossListCard.gameObject.AddComponent<VerticalLayoutGroup>();
        bossListLayout.padding = new RectOffset(18, 18, 16, 16);
        bossListLayout.spacing = 10f;
        bossListLayout.childAlignment = TextAnchor.UpperLeft;
        bossListLayout.childControlWidth = true;
        bossListLayout.childControlHeight = true;
        bossListLayout.childForceExpandWidth = true;
        bossListLayout.childForceExpandHeight = false;

        CreateText(bossListCard, "BossListTitle", "Boss List", 24f, FontStyles.Bold, TextAlignmentOptions.Left);

        GameObject scrollObject = CreatePanel(bossListCard, "BossListScroll", new Color(0.09f, 0.12f, 0.18f, 0.85f));
        LayoutElement scrollLayout = scrollObject.AddComponent<LayoutElement>();
        scrollLayout.preferredHeight = 240f;
        ScrollRect bossListScroll = scrollObject.AddComponent<ScrollRect>();
        bossListScroll.horizontal = false;
        bossListScroll.vertical = true;
        bossListScroll.scrollSensitivity = 28f;
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();

        GameObject viewportObject = CreatePanel(scrollRect, "Viewport", new Color(0f, 0f, 0f, 0f));
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportObject.AddComponent<RectMask2D>();

        bossListContent = CreateObject("Content", viewportRect);
        bossListContent.anchorMin = new Vector2(0f, 1f);
        bossListContent.anchorMax = new Vector2(1f, 1f);
        bossListContent.pivot = new Vector2(0.5f, 1f);
        bossListContent.offsetMin = new Vector2(0f, 0f);
        bossListContent.offsetMax = new Vector2(0f, 0f);

        VerticalLayoutGroup bossListContentLayout = bossListContent.gameObject.AddComponent<VerticalLayoutGroup>();
        bossListContentLayout.padding = new RectOffset(6, 6, 6, 6);
        bossListContentLayout.spacing = 8f;
        bossListContentLayout.childAlignment = TextAnchor.UpperLeft;
        bossListContentLayout.childControlWidth = true;
        bossListContentLayout.childControlHeight = true;
        bossListContentLayout.childForceExpandWidth = true;
        bossListContentLayout.childForceExpandHeight = false;

        ContentSizeFitter contentFitter = bossListContent.gameObject.AddComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        bossListScroll.viewport = viewportRect;
        bossListScroll.content = bossListContent;
        ScrollRectPerformanceHelper.Optimize(scrollObject, bossListScroll);

        RectTransform detailCard = CreateCard(content, "BossDetailCard", 470f, new Color(0.16f, 0.2f, 0.29f, 0.96f));
        VerticalLayoutGroup detailLayout = detailCard.gameObject.AddComponent<VerticalLayoutGroup>();
        detailLayout.padding = new RectOffset(18, 18, 16, 16);
        detailLayout.spacing = 10f;
        detailLayout.childAlignment = TextAnchor.UpperLeft;
        detailLayout.childControlWidth = true;
        detailLayout.childControlHeight = true;
        detailLayout.childForceExpandWidth = true;
        detailLayout.childForceExpandHeight = false;

        bossNameText = CreateText(detailCard, "BossNameText", "Select a boss", 30f, FontStyles.Bold, TextAlignmentOptions.Left);
        bossMetaText = CreateText(detailCard, "BossMetaText", "Target Lv.--", 22f, FontStyles.Normal, TextAlignmentOptions.Left);
        bossMetaText.textWrappingMode = TextWrappingModes.Normal;
        bossPowerText = CreateText(detailCard, "BossPowerText", "Boss Power: --", 24f, FontStyles.Bold, TextAlignmentOptions.Left);

        RectTransform radarWrap = CreateCard(detailCard, "BossRadarWrap", 220f, new Color(0.1f, 0.12f, 0.18f, 0.92f));
        radarWrap.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.18f, 0.92f);
        RectTransform radarRect = CreateObject("BossRadar", radarWrap);
        radarRect.anchorMin = new Vector2(0.5f, 0.5f);
        radarRect.anchorMax = new Vector2(0.5f, 0.5f);
        radarRect.pivot = new Vector2(0.5f, 0.5f);
        radarRect.sizeDelta = new Vector2(220f, 220f);
        bossRadarGraphic = radarRect.gameObject.AddComponent<RadarChartGraphic>();
        bossRadarGraphic.raycastTarget = false;

        resultText = CreateText(detailCard, "ResultText", string.Empty, 24f, FontStyles.Bold, TextAlignmentOptions.Left);
        resultText.textWrappingMode = TextWrappingModes.Normal;

        fightButton = CreateActionButton(detailCard, "FightButton", "Fight", OnFightPressed);
        fightButtonLabel = fightButton.GetComponentInChildren<TextMeshProUGUI>(true);

        BindButtons();
    }

    private void EnsurePanelLayering()
    {
        if (panelRoot == null)
        {
            return;
        }

        Transform shellRuntimeRoot = transform.Find("ShellRuntimeRoot");
        if (shellRuntimeRoot != null)
        {
            panelRoot.transform.SetSiblingIndex(shellRuntimeRoot.GetSiblingIndex());
        }
    }

    private void CreateBossListButton(int index)
    {
        Button button = CreateActionButton(bossListContent, $"BossButton_{index}", $"Boss {index + 1}", null);
        RectTransform buttonRect = button.transform as RectTransform;
        LayoutElement layout = button.gameObject.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = button.gameObject.AddComponent<LayoutElement>();
        }

        layout.preferredHeight = 52f;
        layout.flexibleWidth = 1f;
        buttonRect.sizeDelta = new Vector2(0f, 52f);

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        label.fontSize = 18f;
        label.alignment = TextAlignmentOptions.Left;
        label.margin = new Vector4(16f, 0f, 16f, 0f);
        label.enableAutoSizing = true;
        label.fontSizeMin = 14f;
        label.fontSizeMax = 18f;

        spawnedBossButtons.Add(button);
        spawnedBossLabels.Add(label);
    }

    private bool NeedsRuntimeLayoutRebuild()
    {
        if (panelRoot == null)
        {
            return false;
        }

        Transform root = panelRoot.transform.Find("BattleScreenRoot");
        if (root == null)
        {
            return true;
        }

        if (root.GetComponent<ContentSizeFitter>() != null)
        {
            return true;
        }

        return root.Find("BattleScrollRoot/Viewport/Content") == null;
    }

    private void DestroyRuntimeLayout()
    {
        GameObject oldRoot = panelRoot;
        if (oldRoot != null)
        {
            oldRoot.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(oldRoot);
            }
            else
            {
                DestroyImmediate(oldRoot);
            }
        }

        panelRoot = null;
        closeButton = null;
        fightButton = null;
        titleText = null;
        statusText = null;
        playerPowerText = null;
        playerSkillsText = null;
        bossNameText = null;
        bossMetaText = null;
        bossPowerText = null;
        resultText = null;
        bossRadarGraphic = null;
        screenRoot = null;
        bossListContent = null;
        fightButtonLabel = null;
        spawnedBossButtons.Clear();
        spawnedBossLabels.Clear();
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

    private RectTransform CreateCard(RectTransform parent, string name, float preferredHeight, Color color)
    {
        GameObject card = CreatePanel(parent, name, color);
        LayoutElement layout = card.AddComponent<LayoutElement>();
        layout.preferredHeight = preferredHeight;
        layout.flexibleWidth = 1f;
        return card.transform as RectTransform;
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

        ContentSizeFitter fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return text;
    }

    private Button CreateActionButton(RectTransform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreatePanel(parent, name, new Color(0.23f, 0.58f, 0.35f, 1f));
        Button button = buttonObject.AddComponent<Button>();
        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 62f;
        layout.flexibleWidth = 1f;
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        RectTransform textRect = CreateObject("Label", buttonObject.transform as RectTransform);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI labelText = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 22f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;
        labelText.textWrappingMode = TextWrappingModes.NoWrap;
        return button;
    }
}
