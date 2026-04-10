using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDUI : MonoBehaviour
{
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI hungerText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI foodText;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI levelText;

    [SerializeField] private GameObject homeRoot;
    [SerializeField] private GameObject topBarRoot;
    [SerializeField] private GameObject mainStatusBlock;
    [SerializeField] private TextMeshProUGUI mainStatusText;
    [SerializeField] private GameObject homeBottomRoot;
    [SerializeField] private GameObject homeActionsRoot;
    [SerializeField] private Button missionsSummaryButton;
    [SerializeField] private TextMeshProUGUI missionsSummaryButtonText;
    [SerializeField] private GameObject deadOverlayRoot;
    [SerializeField] private GameObject deadModeRoot;
    [SerializeField] private TextMeshProUGUI deadTitleText;
    [SerializeField] private TextMeshProUGUI deadSubtitleText;
    [SerializeField] private TextMeshProUGUI deadCoinsText;
    [SerializeField] private Button reviveButton;
    [SerializeField] private TextMeshProUGUI reviveButtonText;
    [SerializeField] private Button deadWorkButton;
    [SerializeField] private TextMeshProUGUI deadWorkButtonText;
    [SerializeField] private GameObject legacyHiddenRoot;
    [SerializeField] private GameObject resetButtonObject;

    private GameManager gameManager;
    private AppShellUI appShellUI;
    private SkillsPanelUI skillsPanelUI;
    private MissionPanelUI missionPanelUI;
    private ShopPanelUI shopPanelUI;
    private RoomPanelUI roomPanelUI;
    private FocusPanelUI focusPanelUI;
    private HomeDetailsPanelUI homeDetailsPanelUI;

    private void Awake()
    {
        AutoResolveSceneBindings();
        HideLegacyHomeNoise();
        BindButtons();
    }

    public void SetDependencies(GameManager manager, AppShellUI shell, SkillsPanelUI skillsPanel, MissionPanelUI missionPanel, ShopPanelUI shopPanel, RoomPanelUI roomPanel, FocusPanelUI focusPanel, HomeDetailsPanelUI homeDetailsPanel)
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
        focusPanelUI = focusPanel;
        homeDetailsPanelUI = homeDetailsPanel;

        SubscribeToGameManager();
        SubscribeToShell();
    }

    private void AutoResolveSceneBindings()
    {
        RectTransform canvasRect = transform as RectTransform;
        Transform canvasRoot = canvasRect != null ? canvasRect : transform;
        Transform homeTransform = FindByPath(canvasRoot, "HomeRoot");
        Transform overlayTransform = FindByPath(homeTransform, "DeadModeOverlay");
        Transform deadBlockTransform = FindByPath(overlayTransform, "DeadModeBlock");

        if (homeRoot == null && homeTransform != null)
        {
            homeRoot = homeTransform.gameObject;
        }

        if (topBarRoot == null)
        {
            topBarRoot = GetPathObject(homeTransform, "TopBar");
        }

        if (mainStatusBlock == null)
        {
            mainStatusBlock = GetPathObject(homeTransform, "MainStatusBlock");
        }

        if (mainStatusText == null)
        {
            mainStatusText = GetPathText(homeTransform, "MainStatusBlock/MainStatusText");
        }

        if (homeBottomRoot == null)
        {
            homeBottomRoot = GetPathObject(homeTransform, "HomeBottomBlock");
        }

        if (homeActionsRoot == null)
        {
            homeActionsRoot = GetPathObject(homeTransform, "HomeBottomBlock/HomeActionsBlock");
        }

        if (missionsSummaryButton == null)
        {
            missionsSummaryButton = GetPathButton(homeTransform, "HomeBottomBlock/MissionsSummaryButton");
        }

        if (missionsSummaryButtonText == null && missionsSummaryButton != null)
        {
            missionsSummaryButtonText = missionsSummaryButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (coinsText == null)
        {
            coinsText = GetPathText(homeTransform, "TopBar/CoinsText");
        }

        if (levelText == null)
        {
            levelText = GetPathText(homeTransform, "TopBar/ProgressionRoot/LevelText");
        }

        if (xpText == null)
        {
            xpText = GetPathText(homeTransform, "TopBar/ProgressionRoot/XPText");
        }

        if (deadOverlayRoot == null && overlayTransform != null)
        {
            deadOverlayRoot = overlayTransform.gameObject;
        }

        if (deadModeRoot == null && deadBlockTransform != null)
        {
            deadModeRoot = deadBlockTransform.gameObject;
        }

        if (deadTitleText == null)
        {
            deadTitleText = GetPathText(overlayTransform, "DeadModeBlock/DeadTitleText");
        }

        if (deadSubtitleText == null)
        {
            deadSubtitleText = GetPathText(overlayTransform, "DeadModeBlock/DeadSubtitleText");
            if (deadSubtitleText == null)
            {
                deadSubtitleText = GetPathText(overlayTransform, "DeadModeBlock/DeadReasonText");
            }
        }

        if (deadCoinsText == null)
        {
            deadCoinsText = GetPathText(overlayTransform, "DeadModeBlock/DeadCoinsText");
            if (deadCoinsText == null)
            {
                deadCoinsText = GetPathText(overlayTransform, "DeadModeBlock/DeadCostText");
            }
        }

        if (reviveButton == null)
        {
            reviveButton = GetPathButton(overlayTransform, "DeadModeBlock/ReviveButton");
        }

        if (reviveButtonText == null && reviveButton != null)
        {
            reviveButtonText = reviveButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (deadWorkButton == null)
        {
            deadWorkButton = GetPathButton(overlayTransform, "DeadModeBlock/DeadWorkButton");
        }

        if (deadWorkButtonText == null && deadWorkButton != null)
        {
            deadWorkButtonText = deadWorkButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (legacyHiddenRoot == null)
        {
            legacyHiddenRoot = GetPathObject(canvasRoot, "LegacyHiddenRoot");
        }

        if (resetButtonObject == null)
        {
            resetButtonObject = GetPathObject(legacyHiddenRoot != null ? legacyHiddenRoot.transform : canvasRoot, "ActionPanel/ResetButton");
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

        gameManager.OnCoinsChanged += UpdateCoins;
        gameManager.OnPetChanged += UpdatePet;
        gameManager.OnPetFlowChanged += UpdatePetFlow;
        gameManager.OnInventoryChanged += UpdateInventory;
        gameManager.OnProgressionChanged += UpdateProgression;
        gameManager.OnMissionsChanged += UpdateMissionSummary;
        gameManager.OnFocusResultReady += HandleFocusResultReady;
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

        if (reviveButton != null)
        {
            reviveButton.onClick.RemoveListener(OnRevivePressed);
            reviveButton.onClick.AddListener(OnRevivePressed);
        }

        if (deadWorkButton != null)
        {
            deadWorkButton.onClick.RemoveListener(OnDeadWorkPressed);
            deadWorkButton.onClick.AddListener(OnDeadWorkPressed);
        }
    }

    private Transform FindByPath(Transform root, string path)
    {
        if (root == null || string.IsNullOrEmpty(path))
        {
            return null;
        }

        return root.Find(path);
    }

    private GameObject GetPathObject(Transform root, string path)
    {
        Transform target = FindByPath(root, path);
        return target != null ? target.gameObject : null;
    }

    private TextMeshProUGUI GetPathText(Transform root, string path)
    {
        Transform target = FindByPath(root, path);
        return target != null ? target.GetComponent<TextMeshProUGUI>() : null;
    }

    private Button GetPathButton(Transform root, string path)
    {
        Transform target = FindByPath(root, path);
        return target != null ? target.GetComponent<Button>() : null;
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

        UpdatePetFlow();
    }

    private void UpdatePet()
    {
        if (gameManager == null || gameManager.petData == null)
        {
            return;
        }

        PetStatusSummary summary = gameManager.GetPetStatusSummary();
        ApplyMainStatus(summary);
        UpdatePetFlow();
    }

    private void UpdateInventory()
    {
        HideLegacyHomeNoise();
    }

    private void UpdateProgression()
    {
        if (gameManager == null || gameManager.progressionData == null)
        {
            return;
        }

        if (levelText != null)
        {
            levelText.text = "Lv. " + gameManager.progressionData.level;
            levelText.fontSize = 24f;
        }

        if (xpText != null)
        {
            xpText.text = gameManager.progressionData.xp + " / " + gameManager.GetXpRequiredForNextLevel();
            xpText.fontSize = 20f;
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
        UpdateMissionSummary();
        UpdateDeadMode(summary);
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

        SetTextVisible(statusText, false);

        if (mainStatusText != null)
        {
            mainStatusText.text = summary.headline;
            mainStatusText.color = GetSummaryColor(summary.flowState);
            mainStatusText.gameObject.SetActive(summary.flowState != PetFlowState.Dead);
        }
    }

    private void UpdateHomeMode(PetStatusSummary summary)
    {
        bool isDead = summary != null && summary.flowState == PetFlowState.Dead;
        bool isHomeContext = IsHomeContext();
        bool showNormalHome = !isDead && isHomeContext;

        HideLegacyHomeNoise();

        SetObjectActive(gameManager != null ? gameManager.workButton : null, showNormalHome);
        SetObjectActive(gameManager != null ? gameManager.feedButton : null, showNormalHome);
        SetObjectActive(gameManager != null ? gameManager.focusButton : null, showNormalHome);
        SetObjectActive(gameManager != null ? gameManager.buyButton : null, false);
        SetObjectActive(gameManager != null ? gameManager.upgradeRoomButton : null, false);
        SetGameObjectActive(resetButtonObject, false);
        SetTextVisible(gameManager != null ? gameManager.onboardingHintText : null, false);

        SetGameObjectActive(topBarRoot, showNormalHome);
        SetGameObjectActive(mainStatusBlock, showNormalHome);
        SetGameObjectActive(homeBottomRoot, showNormalHome);
        SetObjectActive(missionsSummaryButton, showNormalHome);
    }

    private void UpdateDeadMode(PetStatusSummary summary)
    {
        if (gameManager == null)
        {
            return;
        }

        bool isDead = summary != null && summary.flowState == PetFlowState.Dead;
        SetGameObjectActive(deadOverlayRoot, isDead);
        SetGameObjectActive(deadModeRoot, isDead);

        if (!isDead)
        {
            return;
        }

        if (deadOverlayRoot != null)
        {
            deadOverlayRoot.transform.SetAsLastSibling();
        }

        if (deadTitleText != null)
        {
            deadTitleText.text = "Pet is dead";
        }

        if (deadSubtitleText != null)
        {
            deadSubtitleText.text = "Revive your pet to continue";
        }

        int reviveCost = gameManager.GetReviveCost();
        bool canRevive = gameManager.CanRevivePet();

        if (deadCoinsText != null && gameManager.currencyData != null)
        {
            deadCoinsText.text = $"Coins: {gameManager.currencyData.coins} / {reviveCost}";
        }

        SetObjectActive(reviveButton, canRevive);
        SetObjectActive(deadWorkButton, !canRevive);

        if (reviveButtonText != null)
        {
            reviveButtonText.text = "Revive";
        }

        if (deadWorkButtonText != null)
        {
            deadWorkButtonText.text = "Work to earn coins";
        }
    }

    private void UpdateMissionSummary()
    {
        if (gameManager == null || missionsSummaryButtonText == null)
        {
            return;
        }

        int availableClaims = 0;
        var missions = gameManager.GetAllDailyMissions();
        if (missions != null)
        {
            for (int i = 0; i < missions.Count; i++)
            {
                if (missions[i] != null && missions[i].isCompleted && !missions[i].isClaimed)
                {
                    availableClaims++;
                }
            }
        }

        missionsSummaryButtonText.text = $"Missions ({availableClaims})";
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

    private void OnRevivePressed()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.TryRevivePet();
        UpdatePet();
        UpdateCoins();
        UpdatePetFlow();
    }

    private void OnDeadWorkPressed()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnWorkButton();
        UpdateCoins();
        UpdatePetFlow();
    }

    private bool IsHomeContext()
    {
        bool skillsVisible = skillsPanelUI != null && skillsPanelUI.IsPanelVisible();
        bool missionsVisible = missionPanelUI != null && missionPanelUI.IsPanelVisible();
        bool shopVisible = shopPanelUI != null && shopPanelUI.IsPanelVisible();
        bool roomVisible = roomPanelUI != null && roomPanelUI.IsPanelVisible();
        bool focusVisible = focusPanelUI != null && focusPanelUI.IsPanelVisible();
        bool homeDetailsVisible = homeDetailsPanelUI != null && homeDetailsPanelUI.IsPanelVisible();

        return !skillsVisible && !missionsVisible && !shopVisible && !roomVisible && !focusVisible && !homeDetailsVisible;
    }

    private void HideLegacyHomeNoise()
    {
        if (legacyHiddenRoot != null && legacyHiddenRoot.activeSelf)
        {
            legacyHiddenRoot.SetActive(false);
        }

        SetTextVisible(hungerText, false);
        SetTextVisible(foodText, false);
        SetTextVisible(statusText, false);
        SetTextVisible(gameManager != null ? gameManager.focusTimerText : null, false);
    }

    private Color GetSummaryColor(PetFlowState flowState)
    {
        switch (flowState)
        {
            case PetFlowState.Dead:
                return new Color(1f, 0.42f, 0.42f, 1f);
            case PetFlowState.Critical:
                return new Color(1f, 0.63f, 0.28f, 1f);
            case PetFlowState.Warning:
                return new Color(1f, 0.84f, 0.4f, 1f);
            case PetFlowState.Revived:
                return new Color(0.47f, 0.93f, 0.62f, 1f);
            default:
                return new Color(0.76f, 0.88f, 1f, 1f);
        }
    }

    private void SetTextVisible(TextMeshProUGUI text, bool visible)
    {
        if (text != null)
        {
            text.gameObject.SetActive(visible);
        }
    }

    private void SetObjectActive(Component component, bool visible)
    {
        if (component != null)
        {
            component.gameObject.SetActive(visible);
        }
    }

    private void SetGameObjectActive(GameObject target, bool visible)
    {
        if (target != null)
        {
            target.SetActive(visible);
        }
    }

    private void OnDestroy()
    {
        if (missionsSummaryButton != null)
        {
            missionsSummaryButton.onClick.RemoveListener(OnMissionsSummaryPressed);
        }

        if (reviveButton != null)
        {
            reviveButton.onClick.RemoveListener(OnRevivePressed);
        }

        if (deadWorkButton != null)
        {
            deadWorkButton.onClick.RemoveListener(OnDeadWorkPressed);
        }

        UnsubscribeFromGameManager();
        UnsubscribeFromShell();
    }
}
