using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class HUDUI : MonoBehaviour
{
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI levelText;

    [SerializeField] private GameObject homeRoot;
    [SerializeField] private GameObject topBarRoot;
    [SerializeField] private GameObject mainStatusBlock;
    [SerializeField] private TextMeshProUGUI mainStatusText;
    [SerializeField] private TextMeshProUGUI petVitalsText;
    [SerializeField] private GameObject idleSummaryRoot;
    [SerializeField] private TextMeshProUGUI idleIconText;
    [SerializeField] private TextMeshProUGUI idleActionText;
    [SerializeField] private TextMeshProUGUI idleSummaryText;
    [SerializeField] private TextMeshProUGUI idleBadgeText;
    [SerializeField] private Button idleClaimButton;
    [SerializeField] private TextMeshProUGUI idleClaimButtonText;
    [SerializeField] private GameObject homeBottomRoot;
    [SerializeField] private GameObject homeActionsRoot;
    [SerializeField] private Button missionsSummaryButton;
    [SerializeField] private TextMeshProUGUI missionsSummaryButtonText;
    [FormerlySerializedAs("deadOverlayRoot")]
    [SerializeField] private GameObject neglectOverlayRoot;
    [FormerlySerializedAs("deadModeRoot")]
    [SerializeField] private GameObject neglectPanelRoot;
    [FormerlySerializedAs("deadTitleText")]
    [SerializeField] private TextMeshProUGUI neglectTitleText;
    [FormerlySerializedAs("deadSubtitleText")]
    [SerializeField] private TextMeshProUGUI neglectSubtitleText;
    [FormerlySerializedAs("deadCoinsText")]
    [SerializeField] private TextMeshProUGUI neglectDetailText;
    [FormerlySerializedAs("reviveButton")]
    [SerializeField] private Button careButton;
    [FormerlySerializedAs("reviveButtonText")]
    [SerializeField] private TextMeshProUGUI careButtonText;
    [FormerlySerializedAs("deadWorkButton")]
    [SerializeField] private Button neglectWorkButton;
    [FormerlySerializedAs("deadWorkButtonText")]
    [SerializeField] private TextMeshProUGUI neglectWorkButtonText;

    private GameManager gameManager;
    private AppShellUI appShellUI;
    private SkillsPanelUI skillsPanelUI;
    private MissionPanelUI missionPanelUI;
    private ShopPanelUI shopPanelUI;
    private RoomPanelUI roomPanelUI;
    private BattlePanelUI battlePanelUI;
    private FocusPanelUI focusPanelUI;
    private HomeDetailsPanelUI homeDetailsPanelUI;
    private Button runtimeFeedButton;
    private Button runtimeFocusButton;
    private TextMeshProUGUI runtimeFocusButtonText;
    private Button battleButton;
    private TextMeshProUGUI battleButtonText;
    private GameObject sideActionsRoot;
    private string idleFeedbackOverride = string.Empty;

    private void Awake()
    {
        AutoResolveSceneBindings();
        EnsureCoreActionButtons();
        EnsureBattleButton();
        EnsureIdleSummaryBlock();
        HideLegacyHomeNoise();
        BindButtons();
    }

    public void SetDependencies(GameManager manager, AppShellUI shell, SkillsPanelUI skillsPanel, MissionPanelUI missionPanel, ShopPanelUI shopPanel, RoomPanelUI roomPanel, BattlePanelUI battlePanel, FocusPanelUI focusPanel, HomeDetailsPanelUI homeDetailsPanel)
    {
        if (gameManager != null && gameManager != manager)
        {
            UnsubscribeFromGameManager();
        }

        UnsubscribeFromShell();

        gameManager = manager;
        appShellUI = shell;
        skillsPanelUI = skillsPanel;
        missionPanelUI = missionPanel;
        shopPanelUI = shopPanel;
        roomPanelUI = roomPanel;
        battlePanelUI = battlePanel;
        focusPanelUI = focusPanel;
        homeDetailsPanelUI = homeDetailsPanel;

        EnsureCoreActionButtons();
        EnsureBattleButton();
        BindButtons();
        SubscribeToGameManager();
        SubscribeToShell();
    }

    private void AutoResolveSceneBindings()
    {
        RectTransform canvasRect = transform as RectTransform;
        Transform canvasRoot = canvasRect != null ? canvasRect : transform;
        Transform homeTransform = HUDViewUtility.FindByPath(canvasRoot, "HomeRoot");
        Transform overlayTransform = HUDViewUtility.FindByPath(homeTransform, "NeglectOverlay");
        if (overlayTransform == null)
        {
            overlayTransform = HUDViewUtility.FindByPath(homeTransform, "DeadModeOverlay");
        }

        Transform deadBlockTransform = HUDViewUtility.FindByPath(overlayTransform, "NeglectPanel");
        if (deadBlockTransform == null)
        {
            deadBlockTransform = HUDViewUtility.FindByPath(overlayTransform, "DeadModeBlock");
        }

        if (homeRoot == null && homeTransform != null)
        {
            homeRoot = homeTransform.gameObject;
        }

        if (topBarRoot == null)
        {
            topBarRoot = HUDViewUtility.GetPathObject(homeTransform, "TopBar");
        }

        if (mainStatusBlock == null)
        {
            mainStatusBlock = HUDViewUtility.GetPathObject(homeTransform, "MainStatusBlock");
        }

        if (mainStatusText == null)
        {
            mainStatusText = HUDViewUtility.GetPathText(homeTransform, "MainStatusBlock/MainStatusText");
        }

        if (petVitalsText == null)
        {
            petVitalsText = HUDViewUtility.GetPathText(homeTransform, "MainStatusBlock/PetVitalsText");
        }

        if (idleSummaryRoot == null)
        {
            idleSummaryRoot = HUDViewUtility.GetPathObject(homeTransform, "MainStatusBlock/IdleSummaryBlock");
        }

        if (idleIconText == null)
        {
            idleIconText = HUDViewUtility.GetPathText(homeTransform, "MainStatusBlock/IdleSummaryBlock/Header/IconText");
        }

        if (idleActionText == null)
        {
            idleActionText = HUDViewUtility.GetPathText(homeTransform, "MainStatusBlock/IdleSummaryBlock/Header/ActionText");
        }

        if (idleSummaryText == null)
        {
            idleSummaryText = HUDViewUtility.GetPathText(homeTransform, "MainStatusBlock/IdleSummaryBlock/SummaryText");
        }

        if (idleBadgeText == null)
        {
            idleBadgeText = HUDViewUtility.GetPathText(homeTransform, "MainStatusBlock/IdleSummaryBlock/ClaimRow/BadgeText");
        }

        if (idleClaimButton == null)
        {
            idleClaimButton = HUDViewUtility.GetPathButton(homeTransform, "MainStatusBlock/IdleSummaryBlock/ClaimRow/ClaimButton");
        }

        if (idleClaimButtonText == null && idleClaimButton != null)
        {
            idleClaimButtonText = idleClaimButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        petVitalsText = HUDViewUtility.EnsurePetVitalsText(mainStatusBlock, petVitalsText);

        if (homeBottomRoot == null)
        {
            homeBottomRoot = HUDViewUtility.GetPathObject(homeTransform, "HomeBottomBlock");
        }

        if (homeActionsRoot == null)
        {
            homeActionsRoot = HUDViewUtility.GetPathObject(homeTransform, "HomeBottomBlock/HomeActionsBlock");
        }

        if (battleButton == null)
        {
            battleButton = HUDViewUtility.GetPathButton(homeTransform, "HomeBottomBlock/HomeActionsBlock/BattleButton");
        }

        if (battleButtonText == null && battleButton != null)
        {
            battleButtonText = battleButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (missionsSummaryButton == null)
        {
            missionsSummaryButton = HUDViewUtility.GetPathButton(homeTransform, "HomeBottomBlock/MissionsSummaryButton");
        }

        if (missionsSummaryButtonText == null && missionsSummaryButton != null)
        {
            missionsSummaryButtonText = missionsSummaryButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (coinsText == null)
        {
            coinsText = HUDViewUtility.GetPathText(homeTransform, "TopBar/CoinsText");
        }

        if (levelText == null)
        {
            levelText = HUDViewUtility.GetPathText(homeTransform, "TopBar/ProgressionRoot/LevelText");
        }

        if (xpText == null)
        {
            xpText = HUDViewUtility.GetPathText(homeTransform, "TopBar/ProgressionRoot/XPText");
        }

        if (neglectOverlayRoot == null && overlayTransform != null)
        {
            neglectOverlayRoot = overlayTransform.gameObject;
        }

        if (neglectPanelRoot == null && deadBlockTransform != null)
        {
            neglectPanelRoot = deadBlockTransform.gameObject;
        }

        if (neglectTitleText == null)
        {
            neglectTitleText = HUDViewUtility.GetPathText(overlayTransform, "NeglectPanel/NeglectTitleText");
            if (neglectTitleText == null)
            {
                neglectTitleText = HUDViewUtility.GetPathText(overlayTransform, "DeadModeBlock/DeadTitleText");
            }
        }

        if (neglectSubtitleText == null)
        {
            neglectSubtitleText = HUDViewUtility.GetPathText(overlayTransform, "NeglectPanel/NeglectSubtitleText");
            if (neglectSubtitleText == null)
            {
                neglectSubtitleText = HUDViewUtility.GetPathText(overlayTransform, "DeadModeBlock/DeadSubtitleText");
            }
            if (neglectSubtitleText == null)
            {
                neglectSubtitleText = HUDViewUtility.GetPathText(overlayTransform, "DeadModeBlock/DeadReasonText");
            }
        }

        if (neglectDetailText == null)
        {
            neglectDetailText = HUDViewUtility.GetPathText(overlayTransform, "NeglectPanel/NeglectDetailText");
            if (neglectDetailText == null)
            {
                neglectDetailText = HUDViewUtility.GetPathText(overlayTransform, "DeadModeBlock/DeadCoinsText");
            }
            if (neglectDetailText == null)
            {
                neglectDetailText = HUDViewUtility.GetPathText(overlayTransform, "DeadModeBlock/DeadCostText");
            }
        }

        if (careButton == null)
        {
            careButton = HUDViewUtility.GetPathButton(overlayTransform, "NeglectPanel/CareButton");
            if (careButton == null)
            {
                careButton = HUDViewUtility.GetPathButton(overlayTransform, "DeadModeBlock/ReviveButton");
            }
        }

        if (careButtonText == null && careButton != null)
        {
            careButtonText = careButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (neglectWorkButton == null)
        {
            neglectWorkButton = HUDViewUtility.GetPathButton(overlayTransform, "NeglectPanel/NeglectWorkButton");
            if (neglectWorkButton == null)
            {
                neglectWorkButton = HUDViewUtility.GetPathButton(overlayTransform, "DeadModeBlock/DeadWorkButton");
            }
        }

        if (neglectWorkButtonText == null && neglectWorkButton != null)
        {
            neglectWorkButtonText = neglectWorkButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    private void SubscribeToGameManager()
    {
        if (gameManager == null)
        {
            Debug.LogWarning("HUDUI is missing GameManager dependency.");
            return;
        }

        gameManager.OnCoinsChanged -= UpdateCoins;
        gameManager.OnPetChanged -= UpdatePet;
        gameManager.OnPetFlowChanged -= UpdatePetFlow;
        gameManager.OnInventoryChanged -= UpdateInventory;
        gameManager.OnProgressionChanged -= UpdateProgression;
        gameManager.OnMissionsChanged -= UpdateMissionSummary;
        gameManager.OnFocusResultReady -= HandleFocusResultReady;
        gameManager.OnIdleChanged -= UpdateIdle;

        gameManager.OnCoinsChanged += UpdateCoins;
        gameManager.OnPetChanged += UpdatePet;
        gameManager.OnPetFlowChanged += UpdatePetFlow;
        gameManager.OnInventoryChanged += UpdateInventory;
        gameManager.OnProgressionChanged += UpdateProgression;
        gameManager.OnMissionsChanged += UpdateMissionSummary;
        gameManager.OnFocusResultReady += HandleFocusResultReady;
        gameManager.OnIdleChanged += UpdateIdle;
    }

    private void UnsubscribeFromGameManager()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnCoinsChanged -= UpdateCoins;
        gameManager.OnPetChanged -= UpdatePet;
        gameManager.OnPetFlowChanged -= UpdatePetFlow;
        gameManager.OnInventoryChanged -= UpdateInventory;
        gameManager.OnProgressionChanged -= UpdateProgression;
        gameManager.OnMissionsChanged -= UpdateMissionSummary;
        gameManager.OnFocusResultReady -= HandleFocusResultReady;
        gameManager.OnIdleChanged -= UpdateIdle;
    }

    private void SubscribeToShell()
    {
        if (appShellUI == null)
        {
            return;
        }

        appShellUI.OnScreenChanged -= HandleShellScreenChanged;
        appShellUI.OnScreenChanged += HandleShellScreenChanged;
    }

    private void UnsubscribeFromShell()
    {
        if (appShellUI == null)
        {
            return;
        }

        appShellUI.OnScreenChanged -= HandleShellScreenChanged;
    }

    private void BindButtons()
    {
        if (missionsSummaryButton != null)
        {
            missionsSummaryButton.onClick.RemoveListener(OnMissionsSummaryPressed);
            missionsSummaryButton.onClick.AddListener(OnMissionsSummaryPressed);
        }

        if (careButton != null)
        {
            careButton.onClick.RemoveListener(OnCarePressed);
            careButton.onClick.AddListener(OnCarePressed);
        }

        if (neglectWorkButton != null)
        {
            neglectWorkButton.onClick.RemoveAllListeners();
        }

        if (battleButton != null)
        {
            battleButton.onClick.RemoveListener(OnBattlePressed);
            battleButton.onClick.AddListener(OnBattlePressed);
        }

        if (runtimeFeedButton != null)
        {
            runtimeFeedButton.onClick.RemoveListener(OnFeedPressed);
            runtimeFeedButton.onClick.AddListener(OnFeedPressed);
        }

        if (runtimeFocusButton != null)
        {
            runtimeFocusButton.onClick.RemoveListener(OnFocusPressed);
            runtimeFocusButton.onClick.AddListener(OnFocusPressed);
        }

        if (idleClaimButton != null)
        {
            idleClaimButton.onClick.RemoveListener(OnIdleClaimPressed);
            idleClaimButton.onClick.AddListener(OnIdleClaimPressed);
        }
    }

    private void UpdateCoins()
    {
        if (gameManager == null || gameManager.currencyData == null)
        {
            return;
        }

        if (coinsText != null)
        {
            coinsText.text = "Coins: " + gameManager.currencyData.coins;
        }
    }

    private void UpdatePet()
    {
        if (gameManager == null || gameManager.petData == null)
        {
            return;
        }

        PetStatusSummary summary = gameManager.GetPetStatusSummary();
        ApplyMainStatus(summary);
    }

    private void UpdateInventory()
    {
        HideLegacyHomeNoise();
    }

    private void UpdateIdle()
    {
        if (gameManager == null)
        {
            return;
        }

        EnsureIdleSummaryBlock();
        IdleHomeView view = gameManager.GetIdleHomeView();
        PetStatusSummary petSummary = gameManager.GetPetStatusSummary();
        bool idleRewardsBlocked = petSummary != null &&
                                  (petSummary.flowState == PetFlowState.Critical || petSummary.flowState == PetFlowState.Neglected);

        if (view.PendingCount > 0 || idleRewardsBlocked)
        {
            idleFeedbackOverride = string.Empty;
        }

        if (idleIconText != null)
        {
            idleIconText.text = string.IsNullOrWhiteSpace(view.IconId) ? "SKL" : view.IconId;
            UiIconViewUtility.ApplyIconToTextSlot(idleIconText, idleIconText.text);
        }

        if (idleActionText != null)
        {
            idleActionText.text = "Idle: " + (string.IsNullOrWhiteSpace(view.ActionText) ? "отдыхает" : view.ActionText);
        }

        if (idleSummaryText != null)
        {
            string summaryText = view.SummaryText;
            if (!idleRewardsBlocked &&
                view.PendingCount == 0 &&
                !string.IsNullOrWhiteSpace(idleFeedbackOverride))
            {
                summaryText = idleFeedbackOverride;
            }

            idleSummaryText.text = string.IsNullOrWhiteSpace(summaryText) ? "Пока ничего не нашёл." : summaryText;
        }

        if (idleBadgeText != null)
        {
            idleBadgeText.text = view.PendingCount > 0 ? $"x{view.PendingCount}" : "0";
        }

        if (idleClaimButtonText != null)
        {
            idleClaimButtonText.text = view.HasClaimableEvents ? "Забрать" : "Пусто";
        }

        if (idleClaimButton != null)
        {
            idleClaimButton.interactable = view.HasClaimableEvents;
        }

        HUDViewUtility.SetGameObjectActive(idleSummaryRoot, IsHomeContext());
    }

    private void UpdateProgression()
    {
        if (levelText != null)
        {
            levelText.text = string.Empty;
            levelText.gameObject.SetActive(false);
        }

        if (xpText != null)
        {
            xpText.text = string.Empty;
            xpText.gameObject.SetActive(false);
        }
    }

    private void UpdatePetFlow()
    {
        if (gameManager == null)
        {
            return;
        }

        PetStatusSummary summary = gameManager.GetPetStatusSummary();
        ApplyMainStatus(summary);
        UpdateHomeMode(summary);
        UpdateNeglectMode(summary);
    }

    private void HandleFocusResultReady(FocusSessionResultData result)
    {
        if (result == null)
        {
            return;
        }

        UpdatePetFlow();
    }

    private void HandleShellScreenChanged(AppScreen screen)
    {
        UpdatePetFlow();
    }

    private void ApplyMainStatus(PetStatusSummary summary)
    {
        if (summary == null)
        {
            return;
        }

        if (mainStatusText != null)
        {
            string vitals = gameManager != null ? gameManager.GetPetVitalsSummaryText() : "Hunger: --  |  Mood: --";
            mainStatusText.text = summary.headline + "\n" + vitals;
            mainStatusText.color = HUDViewUtility.GetSummaryColor(summary.flowState);
            mainStatusText.gameObject.SetActive(true);
        }

        if (petVitalsText != null)
        {
            petVitalsText.text = gameManager != null ? gameManager.GetPetVitalsSummaryText() : "Hunger: --  |  Mood: --";
            petVitalsText.color = new Color(0.84f, 0.9f, 0.98f, 0.96f);
            petVitalsText.gameObject.SetActive(false);
        }
    }

    private void UpdateHomeMode(PetStatusSummary summary)
    {
        bool isHomeContext = IsHomeContext();
        bool showNormalHome = isHomeContext;

        HideLegacyHomeNoise();

        HUDViewUtility.SetObjectActive(gameManager != null ? gameManager.feedButton : null, showNormalHome);
        HUDViewUtility.SetObjectActive(gameManager != null ? gameManager.focusButton : null, showNormalHome);
        HUDViewUtility.SetObjectActive(battleButton, showNormalHome);
        HUDViewUtility.SetTextVisible(gameManager != null ? gameManager.onboardingHintText : null, false);

        HUDViewUtility.SetGameObjectActive(topBarRoot, showNormalHome);
        HUDViewUtility.SetGameObjectActive(mainStatusBlock, showNormalHome);
        HUDViewUtility.SetGameObjectActive(homeBottomRoot, showNormalHome);
        HUDViewUtility.SetGameObjectActive(homeActionsRoot, false);
        HUDViewUtility.SetGameObjectActive(sideActionsRoot, showNormalHome);
        HUDViewUtility.SetGameObjectActive(idleSummaryRoot, showNormalHome);
        HUDViewUtility.SetObjectActive(missionsSummaryButton, showNormalHome);
        UpdateIdle();
    }

    private void UpdateNeglectMode(PetStatusSummary summary)
    {
        if (gameManager == null)
        {
            return;
        }

        bool isNeglected = summary != null && summary.flowState == PetFlowState.Neglected;
        HUDViewUtility.SetGameObjectActive(neglectOverlayRoot, isNeglected);
        HUDViewUtility.SetGameObjectActive(neglectPanelRoot, isNeglected);

        if (!isNeglected)
        {
            return;
        }

        if (neglectOverlayRoot != null)
        {
            neglectOverlayRoot.transform.SetAsLastSibling();
        }

        if (neglectTitleText != null)
        {
            neglectTitleText.text = "Pet neglected";
        }

        if (neglectSubtitleText != null)
        {
            neglectSubtitleText.text = "Skills weaken over time. Care first.";
        }

        if (neglectDetailText != null)
        {
            neglectDetailText.text = "Focus is blocked until hunger or mood recovers.";
        }

        HUDViewUtility.SetObjectActive(careButton, true);
        HUDViewUtility.SetObjectActive(neglectWorkButton, false);

        if (careButtonText != null)
        {
            careButtonText.text = "Open Shop";
        }
    }

    private void UpdateMissionSummary()
    {
        if (gameManager == null || missionsSummaryButtonText == null)
        {
            return;
        }

        missionsSummaryButtonText.text = $"Missions ({gameManager.GetAvailableMissionClaimCount()})";
    }

    private void OnMissionsSummaryPressed()
    {
        if (appShellUI != null)
        {
            appShellUI.OpenMissions();
            return;
        }

        if (missionPanelUI != null)
        {
            missionPanelUI.ShowPanel();
        }
    }

    private void OnBattlePressed()
    {
        if (appShellUI != null)
        {
            appShellUI.OpenBattle();
            return;
        }

        if (gameManager != null)
        {
            gameManager.OpenBattlePanel();
        }
    }

    private void OnFeedPressed()
    {
        gameManager?.OnFeedButton();
    }

    private void OnFocusPressed()
    {
        gameManager?.OnFocusButton();
    }

    private void OnIdleClaimPressed()
    {
        if (gameManager == null)
        {
            return;
        }

        IdleClaimResult result = gameManager.ClaimPendingIdleEvents();
        if (!result.Success)
        {
            return;
        }

        idleFeedbackOverride = result.Message;
        UpdateIdle();
    }

    private void OnCarePressed()
    {
        appShellUI?.OpenShop();
    }

    private bool IsHomeContext()
    {
        bool skillsVisible = skillsPanelUI != null && skillsPanelUI.IsPanelVisible();
        bool missionsVisible = missionPanelUI != null && missionPanelUI.IsPanelVisible();
        bool shopVisible = shopPanelUI != null && shopPanelUI.IsPanelVisible();
        bool roomVisible = roomPanelUI != null && roomPanelUI.IsPanelVisible();
        bool battleVisible = battlePanelUI != null && battlePanelUI.IsPanelVisible();
        bool focusVisible = focusPanelUI != null && focusPanelUI.IsPanelVisible();
        bool homeDetailsVisible = homeDetailsPanelUI != null && homeDetailsPanelUI.IsPanelVisible();

        return !skillsVisible && !missionsVisible && !shopVisible && !roomVisible && !battleVisible && !focusVisible && !homeDetailsVisible;
    }

    private void EnsureCoreActionButtons()
    {
        EnsureSideActionsRoot();
        if (sideActionsRoot == null)
        {
            return;
        }

        if (runtimeFeedButton == null)
        {
            runtimeFeedButton = HUDViewUtility.GetPathButton(homeRoot != null ? homeRoot.transform : transform, "HomeBottomBlock/HomeActionsBlock/FeedButton");
        }

        if (runtimeFeedButton != null)
        {
            GameObject feedObject = runtimeFeedButton.gameObject;
            runtimeFeedButton = null;

            if (feedObject != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(feedObject);
                }
                else
                {
                    DestroyImmediate(feedObject);
                }
            }
        }

        if (runtimeFocusButton == null)
        {
            runtimeFocusButton = HUDViewUtility.GetPathButton(sideActionsRoot.transform, "FocusButton");
        }

        if (runtimeFocusButton == null)
        {
            Button legacyFocus = HUDViewUtility.GetPathButton(homeRoot != null ? homeRoot.transform : transform, "HomeBottomBlock/HomeActionsBlock/FocusButton");
            if (legacyFocus != null)
            {
                GameObject legacyFocusObject = legacyFocus.gameObject;
                if (Application.isPlaying)
                {
                    Destroy(legacyFocusObject);
                }
                else
                {
                    DestroyImmediate(legacyFocusObject);
                }
            }

            runtimeFocusButton = CreateSideActionButton("FocusButton", "Focus", out runtimeFocusButtonText);
        }

        if (homeActionsRoot != null)
        {
            homeActionsRoot.SetActive(false);
        }

        if (gameManager != null)
        {
            gameManager.feedButton = null;

            if (gameManager.focusButton == null || gameManager.focusButton == runtimeFocusButton)
            {
                gameManager.focusButton = runtimeFocusButton;
            }

            if (runtimeFocusButtonText != null && (gameManager.focusButtonText == null || gameManager.focusButton == runtimeFocusButton))
            {
                gameManager.focusButtonText = runtimeFocusButtonText;
            }
        }
    }

    private void EnsureBattleButton()
    {
        EnsureSideActionsRoot();
        if (sideActionsRoot == null)
        {
            return;
        }

        if (battleButton == null)
        {
            battleButton = HUDViewUtility.GetPathButton(sideActionsRoot.transform, "BattleButton");
        }

        if (battleButton == null)
        {
            battleButton = CreateSideActionButton("BattleButton", "Battle", out battleButtonText, new Color(0.49f, 0.26f, 0.18f, 0.98f));
            battleButton.transform.SetAsLastSibling();
        }
    }

    private void EnsureIdleSummaryBlock()
    {
        if (mainStatusBlock == null)
        {
            return;
        }

        if (idleSummaryRoot != null && idleActionText != null && idleSummaryText != null && idleBadgeText != null && idleClaimButton != null)
        {
            return;
        }

        Transform existing = mainStatusBlock.transform.Find("IdleSummaryBlock");
        if (existing != null)
        {
            idleSummaryRoot = existing.gameObject;
            idleIconText = idleIconText ?? existing.Find("Header/IconText")?.GetComponent<TextMeshProUGUI>();
            idleActionText = idleActionText ?? existing.Find("Header/ActionText")?.GetComponent<TextMeshProUGUI>();
            idleSummaryText = idleSummaryText ?? existing.Find("SummaryText")?.GetComponent<TextMeshProUGUI>();
            idleBadgeText = idleBadgeText ?? existing.Find("ClaimRow/BadgeText")?.GetComponent<TextMeshProUGUI>();
            idleClaimButton = idleClaimButton ?? existing.Find("ClaimRow/ClaimButton")?.GetComponent<Button>();
            if (idleClaimButtonText == null && idleClaimButton != null)
            {
                idleClaimButtonText = idleClaimButton.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            return;
        }

        GameObject block = new GameObject("IdleSummaryBlock", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        RectTransform blockRect = block.GetComponent<RectTransform>();
        blockRect.SetParent(mainStatusBlock.transform, false);
        blockRect.localScale = Vector3.one;

        Image background = block.GetComponent<Image>();
        background.color = new Color(0.12f, 0.16f, 0.24f, 0.78f);

        VerticalLayoutGroup layout = block.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        LayoutElement blockLayout = block.GetComponent<LayoutElement>();
        blockLayout.preferredHeight = 118f;

        GameObject header = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.SetParent(blockRect, false);
        headerRect.localScale = Vector3.one;

        HorizontalLayoutGroup headerLayout = header.GetComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 10f;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childControlWidth = false;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;

        LayoutElement headerElement = header.GetComponent<LayoutElement>();
        headerElement.preferredHeight = 28f;

        idleIconText = CreateIdleText("IconText", header.transform, 18f, FontStyles.Bold, TextAlignmentOptions.Center);
        LayoutElement iconLayout = idleIconText.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 32f;
        iconLayout.preferredHeight = 28f;

        idleActionText = CreateIdleText("ActionText", header.transform, 18f, FontStyles.Bold, TextAlignmentOptions.Left);
        LayoutElement actionLayout = idleActionText.gameObject.AddComponent<LayoutElement>();
        actionLayout.flexibleWidth = 1f;
        actionLayout.preferredHeight = 28f;

        idleSummaryText = CreateIdleText("SummaryText", block.transform, 16f, FontStyles.Normal, TextAlignmentOptions.Left);
        idleSummaryText.textWrappingMode = TextWrappingModes.Normal;
        LayoutElement summaryLayout = idleSummaryText.gameObject.AddComponent<LayoutElement>();
        summaryLayout.preferredHeight = 42f;

        GameObject claimRow = new GameObject("ClaimRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        RectTransform claimRowRect = claimRow.GetComponent<RectTransform>();
        claimRowRect.SetParent(blockRect, false);
        claimRowRect.localScale = Vector3.one;

        HorizontalLayoutGroup claimRowLayout = claimRow.GetComponent<HorizontalLayoutGroup>();
        claimRowLayout.spacing = 8f;
        claimRowLayout.childAlignment = TextAnchor.MiddleLeft;
        claimRowLayout.childControlWidth = false;
        claimRowLayout.childControlHeight = true;
        claimRowLayout.childForceExpandWidth = false;
        claimRowLayout.childForceExpandHeight = false;

        LayoutElement claimLayout = claimRow.GetComponent<LayoutElement>();
        claimLayout.preferredHeight = 30f;

        idleBadgeText = CreateIdleText("BadgeText", claimRow.transform, 16f, FontStyles.Bold, TextAlignmentOptions.Center);
        idleBadgeText.text = "0";
        LayoutElement badgeLayout = idleBadgeText.gameObject.AddComponent<LayoutElement>();
        badgeLayout.preferredWidth = 44f;
        badgeLayout.preferredHeight = 28f;

        idleClaimButton = CreateActionButton(claimRow, "ClaimButton", "Забрать", out idleClaimButtonText, 124f, 30f, 16f, new Color(0.27f, 0.46f, 0.33f, 0.96f));
        idleSummaryRoot = block;
    }

    private void EnsureSideActionsRoot()
    {
        Transform searchRoot = homeRoot != null ? homeRoot.transform : transform;
        sideActionsRoot = HUDViewUtility.EnsureSideActionsRoot(searchRoot, sideActionsRoot);
    }

    private Button CreateSideActionButton(string name, string label, out TextMeshProUGUI labelText, Color? background = null)
    {
        return CreateActionButton(sideActionsRoot, name, label, out labelText, 132f, 52f, 20f, background);
    }

    private Button CreateActionButton(GameObject parentRoot, string name, string label, out TextMeshProUGUI labelText, float width, float height, float fontSize, Color? background = null)
    {
        return HUDViewUtility.CreateActionButton(parentRoot, name, label, out labelText, width, height, fontSize, background);
    }

    private static TextMeshProUGUI CreateIdleText(string name, Transform parent, float fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(parent, false);
        textRect.localScale = Vector3.one;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = new Color(0.97f, 0.96f, 0.9f, 1f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private void HideLegacyHomeNoise()
    {
        HUDViewUtility.SetTextVisible(gameManager != null ? gameManager.focusTimerText : null, false);
        HUDViewUtility.SetTextVisible(levelText, false);
        HUDViewUtility.SetTextVisible(xpText, false);
    }

    private void OnDestroy()
    {
        if (missionsSummaryButton != null)
        {
            missionsSummaryButton.onClick.RemoveListener(OnMissionsSummaryPressed);
        }

        if (careButton != null)
        {
            careButton.onClick.RemoveListener(OnCarePressed);
        }

        if (neglectWorkButton != null)
        {
            neglectWorkButton.onClick.RemoveAllListeners();
        }

        if (battleButton != null)
        {
            battleButton.onClick.RemoveListener(OnBattlePressed);
        }

        if (runtimeFeedButton != null)
        {
            runtimeFeedButton.onClick.RemoveListener(OnFeedPressed);
        }

        if (runtimeFocusButton != null)
        {
            runtimeFocusButton.onClick.RemoveListener(OnFocusPressed);
        }

        if (idleClaimButton != null)
        {
            idleClaimButton.onClick.RemoveListener(OnIdleClaimPressed);
        }

        UnsubscribeFromGameManager();
        UnsubscribeFromShell();
    }
}
