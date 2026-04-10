using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    private enum BootstrapMode
    {
        LoadOrCreate,
        CreateDefaults
    }

    public event Action OnCoinsChanged;
    public event Action OnPetChanged;
    public event Action OnInventoryChanged;
    public event Action OnProgressionChanged;
    public event Action OnSkillsChanged;
    public event Action<string, float, float> OnSkillProgressAdded;
    public event Action OnMissionsChanged;
    public event Action OnFocusSessionChanged;
    public event Action<FocusSessionResultData> OnFocusResultReady;
    public event Action OnPetFlowChanged;

    public BalanceConfig balanceConfig;

    public PetData petData = new PetData();
    public CurrencyData currencyData = new CurrencyData();
    public InventoryData inventoryData = new InventoryData();
    public ProgressionData progressionData = new ProgressionData();
    public SkillsData skillsData = new SkillsData();
    public RoomData roomData = new RoomData();
    public MissionData missionData = new MissionData();
    public DailyRewardData dailyRewardData = new DailyRewardData();
    public OnboardingData onboardingData = new OnboardingData();
    public FocusStateData focusStateData = new FocusStateData();
    private PetSystem petSystem;
    private CurrencySystem currencySystem;
    private FocusSystem focusSystem;
    private SkillsSystem skillsSystem;
    private MissionSystem missionSystem;
    private InventorySystem inventorySystem;
    private ShopSystem shopSystem;
    private ProgressionSystem progressionSystem;
    private FocusCoordinator focusCoordinator;
    private PetFlowCoordinator petFlowCoordinator;
    private CoreLoopActionCoordinator coreLoopActionCoordinator;
    private SaveLifecycleCoordinator saveLifecycleCoordinator = new SaveLifecycleCoordinator();
    [SerializeField] private RoomVisualController roomVisualController;
    [SerializeField] private HUDUI hudUI;
    [SerializeField] private SkillsPanelUI skillsPanelUI;
    [SerializeField] private MissionPanelUI missionPanelUI;
    private bool justReset = false;
    private bool isResetting = false;
    private bool isInitialized = false;
    private bool missionHudFallbackLogged = false;
    private double pendingOfflineSeconds = 0d;
    private int activeResetBucket = 0;
    private int lastAppliedResetBucket = 0;
    private const int MaxSupportedRoomLevel = 2;

    public Button workButton;
    public Button feedButton;
    public Button buyButton;
    public Button focusButton;
    public Button upgradeRoomButton;
    public TextMeshProUGUI focusTimerText;
    public TextMeshProUGUI workButtonText;
    public TextMeshProUGUI focusButtonText;

    [Header("New Tools (Optional)")]
    public Button buySnackButton;
    public Button buyMealButton;
    public Button buyPremiumButton;
    public Button feedSnackButton;
    public Button feedMealButton;
    public Button feedPremiumButton;

    [Header("Mission UI")]
    public TextMeshProUGUI missionFeedText;
    public TextMeshProUGUI missionWorkText;
    public TextMeshProUGUI missionFocusText;
    public TextMeshProUGUI missionExtra1Text;
    public TextMeshProUGUI missionExtra2Text;
    public Button claimFeedButton;
    public Button claimWorkButton;
    public Button claimFocusButton;
    public Button claimExtra1Button;
    public Button claimExtra2Button;

    [Header("Onboarding UI")]
    public TextMeshProUGUI onboardingHintText;

    [Header("Feedback UI")]
    public TextMeshProUGUI feedbackText;
    [SerializeField] private FocusPanelUI focusPanelUI;
    [SerializeField] private AppShellUI appShellUI;

    [Header("Mission Debug (Optional)")]
    [SerializeField] private bool missionDebugLoggingEnabled = false;

    private void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.gameObject.SetActive(true);
            CancelInvoke(nameof(HideFeedback));
            Invoke(nameof(HideFeedback), 2f);
        }
    }

    private void HideFeedback()
    {
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
    }
    
    private string debugInitLog = "Boot...";

    void Awake()
    {
        debugInitLog += " AW |";
    }

    void OnGUI()
    {
#if UNITY_EDITOR
        // Temporary diagnostic label for Android to see exactly where Start breaks
        if (!string.IsNullOrEmpty(debugInitLog))
        {
            GUI.color = Color.red;
            GUI.skin.label.fontSize = 24;
            GUI.Label(new Rect(10, Screen.height - 100, Screen.width - 20, 100), debugInitLog);
        }
#endif
    }

    void Start()
    {
        isInitialized = false;
        try 
        {
            RunStartupLifecycle();
        }
        catch (System.Exception e)
        {
            debugInitLog += "\nEX: " + e.Message;
            Debug.LogException(e);
            isInitialized = false;
        }
    }

    private void HideDebugStrap()
    {
        debugInitLog = "";
    }

    // Startup lifecycle
    private void RunStartupLifecycle()
    {
        debugInitLog += " ST_start |";
        if (!RunBootstrapLifecycle(BootstrapMode.LoadOrCreate))
        {
            return;
        }

        FinalizeStartup();
    }

    private bool RunBootstrapLifecycle(BootstrapMode mode)
    {
        ResolveReferences();
        if (!ValidateStartupPrerequisites())
        {
            return false;
        }

        LoadOrCreateState(mode);
        ApplyStateToRuntime();
        InitializeRuntimeSystems();
        InitializeCoordinators();
        InitializeUiBindings();
        bool timeBoundaryStateChanged = ApplyDailyResetWindowIfNeeded(mode == BootstrapMode.CreateDefaults ? "defaults" : "bootstrap");
        timeBoundaryStateChanged |= ApplyPendingOfflineProgress();
        if (timeBoundaryStateChanged)
        {
            SaveGame();
        }

        SyncUiFromRuntime();
        return true;
    }

    private void ResolveReferences()
    {
        debugInitLog += " refs |";

        if (focusPanelUI == null)
        {
            focusPanelUI = UnityEngine.Object.FindAnyObjectByType<FocusPanelUI>();
        }

        if (appShellUI == null)
        {
            appShellUI = UnityEngine.Object.FindAnyObjectByType<AppShellUI>();
        }

        if (roomVisualController == null)
        {
            roomVisualController = UnityEngine.Object.FindAnyObjectByType<RoomVisualController>();
        }

        if (hudUI == null)
        {
            hudUI = UnityEngine.Object.FindAnyObjectByType<HUDUI>();
        }

        if (skillsPanelUI == null)
        {
            skillsPanelUI = UnityEngine.Object.FindAnyObjectByType<SkillsPanelUI>();
        }

        if (missionPanelUI == null)
        {
            missionPanelUI = UnityEngine.Object.FindAnyObjectByType<MissionPanelUI>();
        }
    }

    private bool ValidateStartupPrerequisites()
    {
        if (balanceConfig != null)
        {
            return true;
        }

        debugInitLog += " ERR: balCfg null |";
        Debug.LogError("BalanceConfig is not assigned in GameManager.");
        return false;
    }

    private void FinalizeStartup()
    {
        debugInitLog += " finalize |";
        SaveGame();
        isInitialized = true;
        debugInitLog = "START SUCCESS! refs ok.";
        Invoke(nameof(HideDebugStrap), 5f);
    }

    // State creation / loading
    private void InitializeDefaultState()
    {
        pendingOfflineSeconds = 0d;
        petData = new PetData();
        currencyData = new CurrencyData();
        inventoryData = new InventoryData();
        progressionData = new ProgressionData();
        skillsData = CreateDefaultSkillsData();
        roomData = new RoomData();
        missionData = CreateDefaultMissionData();
        dailyRewardData = CreateDefaultDailyRewardData();
        onboardingData = CreateDefaultOnboardingData();

        petData.hunger = balanceConfig.startingHunger;
        petData.mood = balanceConfig.startingMood;
        petData.energy = balanceConfig.startingEnergy;
        petData.hasIndependentStats = true;
        currencyData.coins = balanceConfig.startingCoins;
        progressionData.level = balanceConfig.startingLevel;
        progressionData.xp = balanceConfig.startingXp;
        roomData.roomLevel = 0;
    }

    private void LoadOrCreateState(BootstrapMode mode)
    {
        debugInitLog += mode == BootstrapMode.LoadOrCreate ? " load/create |" : " defaults |";
        pendingOfflineSeconds = 0d;
        activeResetBucket = TimeService.GetCurrentResetBucketLocal();
        lastAppliedResetBucket = 0;

        if (mode == BootstrapMode.CreateDefaults)
        {
            InitializeDefaultState();
            LogTimeBootstrap(string.Empty);
            return;
        }

        SaveData loaded = saveLifecycleCoordinator.LoadState();
        if (loaded != null)
        {
            ApplyLoadedState(loaded);
            lastAppliedResetBucket = loaded.lastResetBucket;
            pendingOfflineSeconds = GetOfflineElapsedSeconds(loaded.lastSeenUtc);
            LogTimeBootstrap(loaded.lastSeenUtc);
            return;
        }

        InitializeDefaultState();
        LogTimeBootstrap(string.Empty);
    }

    private void ApplyLoadedState(SaveData loaded)
    {
        SaveData normalized = SaveNormalizer.Normalize(loaded);
        petData = normalized.petData;
        currencyData = normalized.currencyData;
        inventoryData = normalized.inventoryData;
        progressionData = normalized.progressionData;
        skillsData = normalized.skillsData;
        roomData = normalized.roomData;
        missionData = normalized.missionData;
        dailyRewardData = normalized.dailyRewardData;
        onboardingData = normalized.onboardingData;
        focusStateData = normalized.focusStateData ?? new FocusStateData();
    }

    private void NormalizePetState()
    {
        if (petData == null)
        {
            petData = new PetData();
        }

        petData.hunger = Mathf.Clamp(petData.hunger, 0f, 100f);

        if (!petData.hasIndependentStats)
        {
            petData.mood = balanceConfig.startingMood;
            petData.energy = balanceConfig.startingEnergy;
            petData.hasIndependentStats = true;
        }
        else
        {
            petData.mood = Mathf.Clamp(petData.mood, 0f, 100f);
            petData.energy = Mathf.Clamp(petData.energy, 0f, 100f);
        }

        if (string.IsNullOrEmpty(petData.statusText))
        {
            petData.statusText = "Happy";
        }
    }

    private void NormalizeRoomState()
    {
        if (roomData == null)
        {
            roomData = new RoomData();
        }

        roomData.roomLevel = Mathf.Clamp(roomData.roomLevel, 0, MaxSupportedRoomLevel);
    }

    // Runtime/bootstrap phases
    private void ApplyStateToRuntime()
    {
        debugInitLog += " apply |";
        NormalizeRoomState();
        NormalizePetState();

        if (petData.hunger <= 0f && !petData.isDead)
        {
            petData.isDead = true;
            petData.statusText = "Dead";
        }
    }

    private void InitializeRuntimeSystems()
    {
        debugInitLog += " systems |";
        petSystem = new PetSystem(petData);
        currencySystem = new CurrencySystem(currencyData);
        inventorySystem = new InventorySystem(inventoryData);
        focusSystem = new FocusSystem();
        skillsSystem = new SkillsSystem();
        skillsSystem.Init(skillsData);
        missionSystem = new MissionSystem();
        missionSystem.Init(missionData);
        missionSystem.SetDebugLoggingEnabled(missionDebugLoggingEnabled);
        shopSystem = new ShopSystem(currencySystem, inventorySystem);
        progressionSystem = new ProgressionSystem(
            progressionData,
            balanceConfig.xpToNextLevel,
            balanceConfig.buyUnlockLevel
        );
    }

    private void InitializeCoordinators()
    {
        debugInitLog += " coordinators |";
        focusCoordinator = new FocusCoordinator(
            focusSystem,
            skillsSystem,
            progressionSystem,
            petSystem,
            missionSystem,
            currencySystem,
            progressionData,
            onboardingData,
            balanceConfig,
            new FocusCoordinatorCallbacks
            {
                OnCoinsChanged = () => OnCoinsChanged?.Invoke(),
                OnPetChanged = () => OnPetChanged?.Invoke(),
                OnPetFlowChanged = () => OnPetFlowChanged?.Invoke(),
                OnProgressionChanged = () => OnProgressionChanged?.Invoke(),
                OnSkillsChanged = () => OnSkillsChanged?.Invoke(),
                OnSkillProgressAdded = (skillId, delta, newPercent) => OnSkillProgressAdded?.Invoke(skillId, delta, newPercent),
                OnFocusSessionChanged = () => OnFocusSessionChanged?.Invoke(),
                OnFocusResultReady = result => OnFocusResultReady?.Invoke(result),
                GetCurrentResetBucket = GetCurrentResetBucketForSystems,
                SaveGame = SaveGame,
                UpdateUi = UpdateUI,
                UpdateMissionUi = UpdateMissionUI,
                RefreshOnboardingCompletion = RefreshOnboardingCompletion,
                UpdateOnboardingUi = UpdateOnboardingUI,
                ShowFeedback = ShowFeedback,
                ApplyMissionRewards = ApplyMissionClaimResult
            });
        petFlowCoordinator = new PetFlowCoordinator(
            petSystem,
            currencySystem,
            focusCoordinator,
            petData,
            currencyData,
            balanceConfig,
            new PetFlowCoordinatorCallbacks
            {
                OnCoinsChanged = () => OnCoinsChanged?.Invoke(),
                OnPetChanged = () => OnPetChanged?.Invoke(),
                OnPetFlowChanged = () => OnPetFlowChanged?.Invoke(),
                OnFocusSessionChanged = () => OnFocusSessionChanged?.Invoke(),
                SaveGame = SaveGame,
                UpdateUi = UpdateUI,
                ShowFeedback = ShowFeedback
            });
        coreLoopActionCoordinator = new CoreLoopActionCoordinator(
            petData,
            currencyData,
            progressionData,
            roomData,
            onboardingData,
            petSystem,
            currencySystem,
            inventorySystem,
            shopSystem,
            progressionSystem,
            missionSystem,
            balanceConfig,
            MaxSupportedRoomLevel,
            new CoreLoopActionCoordinatorCallbacks
            {
                OnCoinsChanged = () => OnCoinsChanged?.Invoke(),
                OnPetChanged = () => OnPetChanged?.Invoke(),
                OnInventoryChanged = () => OnInventoryChanged?.Invoke(),
                ShowMissionRefresh = UpdateMissionUI,
                RefreshOnboardingCompletion = RefreshOnboardingCompletion,
                UpdateOnboardingUi = UpdateOnboardingUI,
                UpdateUi = UpdateUI,
                SaveGame = SaveGame,
                AddXp = AddXP,
                ShowFeedback = ShowFeedback,
                ApplyMissionRewards = ApplyMissionClaimResult,
                ApplyRoomVisuals = data =>
                {
                    if (roomVisualController != null)
                    {
                        roomVisualController.ApplyRoom(data);
                    }
                }
            });
        focusCoordinator.ResetRuntimeState();
        focusCoordinator.RestoreState(focusStateData, pendingOfflineSeconds);
        petFlowCoordinator.ResetRuntimeState(petData != null && petData.isDead);
    }

    private void InitializeUiBindings()
    {
        debugInitLog += " ui-bind |";
        focusPanelUI?.SetGameManager(this);
        skillsPanelUI?.SetGameManager(this);
        missionPanelUI?.SetGameManager(this);
        appShellUI?.SetDependencies(this, skillsPanelUI, missionPanelUI, focusPanelUI);
        hudUI?.SetDependencies(this, appShellUI, skillsPanelUI, missionPanelUI, focusPanelUI);
        ValidateOptionalUiWiring();
    }

    private void NotifyAllUI()
    {
        OnCoinsChanged?.Invoke();
        OnPetChanged?.Invoke();
        OnPetFlowChanged?.Invoke();
        OnInventoryChanged?.Invoke();
        OnProgressionChanged?.Invoke();
        OnSkillsChanged?.Invoke();
        OnMissionsChanged?.Invoke();
        OnFocusSessionChanged?.Invoke();
    }

    private void SyncUiFromRuntime()
    {
        debugInitLog += " sync-ui |";
        NotifyAllUI();
        Debug.Log("Pet hunger: " + petData.hunger);

        if (roomVisualController != null)
        {
            roomVisualController.ApplyRoom(roomData);
        }

        UpdateUI();
        UpdateMissionUI();
        UpdateOnboardingUI();
    }

    void Update()
    {
        if (!isInitialized || petSystem == null || focusSystem == null || balanceConfig == null || petData == null)
        {
            return;
        }

        if (justReset)
        {
            justReset = false;
            return;
        }

        if (ApplyDailyResetWindowIfNeeded("update"))
        {
            SaveGame();
            UpdateMissionUI();
            UpdateUI();
        }

        bool petChanged = petSystem.UpdateHunger(Time.deltaTime, balanceConfig.hungerDrainPerSecond);
        petChanged |= petSystem.UpdateMoodDecay(
            Time.deltaTime,
            balanceConfig.lowHungerMoodThreshold,
            balanceConfig.lowEnergyMoodThreshold,
            balanceConfig.moodDecayPerSecondWhenHungry,
            balanceConfig.moodDecayPerSecondWhenTired
        );
        petChanged |= petSystem.UpdateStatus();
        if (petChanged)
        {
            OnPetChanged?.Invoke();
            OnPetFlowChanged?.Invoke();
        }

        petFlowCoordinator?.HandleStateTransitions();
        bool focusChanged = focusCoordinator != null && focusCoordinator.Update(Time.unscaledDeltaTime, petData.isDead);

        if (petChanged || focusChanged)
        {
            UpdateUI();
        }
    }

    public void OnFeedButton()       { TryFeedItem("food_basic", balanceConfig.feedAmount); }
    public void OnFeedSnackButton()  { TryFeedItem("food_snack", balanceConfig.snackRestore); }
    public void OnFeedMealButton()   { TryFeedItem("food_meal", balanceConfig.mealRestore); }
    public void OnFeedPremiumButton(){ TryFeedItem("food_premium", balanceConfig.premiumRestore); }

    private void TryFeedItem(string itemId, float amount)
    {
        coreLoopActionCoordinator?.TryFeedItem(itemId, amount);
    }

    public void OnBuyButton()       { TryBuyItem("food_basic", balanceConfig.foodPrice); }
    public void OnBuySnackButton()  { TryBuyItem("food_snack", balanceConfig.snackPrice); }
    public void OnBuyMealButton()   { TryBuyItem("food_meal", balanceConfig.mealPrice); }
    public void OnBuyPremiumButton(){ TryBuyItem("food_premium", balanceConfig.premiumPrice); }

    private void TryBuyItem(string itemId, int price)
    {
        coreLoopActionCoordinator?.TryBuyItem(itemId, price);
    }

    public void OnWorkButton()
    {
        coreLoopActionCoordinator?.TryWork();
    }

    void UpdateUI()
    {
        bool dead = petData.isDead;
        bool bUnlocked = progressionSystem.IsBuyUnlocked();

        if (feedButton != null)
            feedButton.interactable = !dead && inventorySystem.HasItem("food_basic", 1);
        if (feedSnackButton != null) feedSnackButton.interactable = !dead && inventorySystem.HasItem("food_snack", 1);
        if (feedMealButton != null) feedMealButton.interactable = !dead && inventorySystem.HasItem("food_meal", 1);
        if (feedPremiumButton != null) feedPremiumButton.interactable = !dead && inventorySystem.HasItem("food_premium", 1);

        if (buyButton != null)
            buyButton.interactable = !dead && bUnlocked && currencyData.coins >= balanceConfig.foodPrice;
        if (buySnackButton != null) buySnackButton.interactable = !dead && bUnlocked && currencyData.coins >= balanceConfig.snackPrice;
        if (buyMealButton != null) buyMealButton.interactable = !dead && bUnlocked && currencyData.coins >= balanceConfig.mealPrice;
        if (buyPremiumButton != null) buyPremiumButton.interactable = !dead && bUnlocked && currencyData.coins >= balanceConfig.premiumPrice;

        if (workButton != null)
            workButton.interactable = !dead;

        if (focusButton != null)
            focusButton.interactable = !dead;

        if (upgradeRoomButton != null)
            upgradeRoomButton.interactable = !dead && coreLoopActionCoordinator != null && coreLoopActionCoordinator.CanUpgradeRoom();

        if (focusTimerText != null)
        {
            if (dead)
                focusTimerText.text = "Focus: Dead";
            else if (focusSystem.IsPaused)
                focusTimerText.text = "Focus: Paused";
            else if (focusSystem.IsRunning)
                focusTimerText.text = "Focus: " + FormatFocusTime(focusSystem.GetRemainingTime());
            else
                focusTimerText.text = "Focus: Ready";
        }

        int workReward  = coreLoopActionCoordinator != null ? coreLoopActionCoordinator.GetCurrentWorkReward() : progressionSystem.GetWorkReward(balanceConfig.baseWorkReward);
        int focusReward = progressionSystem.GetFocusReward(balanceConfig.baseFocusReward);

        if (workButtonText != null)
            workButtonText.text = $"Work (+{workReward})";

        if (focusButtonText != null)
            focusButtonText.text = focusSystem.HasActiveSession ? "Focus Session" : $"Focus (+{focusReward})";
    }

    void AddXP(int amount, bool saveAfter = true)
    {
        int oldLevel = progressionData.level;
        progressionSystem.AddXp(amount);
        if (progressionData.level > oldLevel)
        {
            ShowFeedback($"Level {progressionData.level} reached");
        }
        OnProgressionChanged?.Invoke();
        if (saveAfter)
        {
            SaveGame();
        }
    }

    private string FormatFocusTime(float remainingSeconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
        TimeSpan time = TimeSpan.FromSeconds(totalSeconds);
        return time.TotalHours >= 1d ? time.ToString(@"hh\:mm\:ss") : time.ToString(@"mm\:ss");
    }

    public void OnFocusButton()
    {
        if (petData.isDead)
        {
            ShowFeedback("Pet is dead");
            return;
        }

        ResolveReferences();

        if (appShellUI != null)
        {
            if (appShellUI.OpenFocus())
            {
                return;
            }
        }

        if (focusPanelUI != null)
        {
            focusPanelUI.OpenPanel();
            return;
        }

        ShowFeedback("Focus panel missing");
    }

    public bool TryStartFocusSession(string skillId, int durationMinutes)
    {
        if (petData == null || petData.isDead)
        {
            ShowFeedback("Pet is dead");
            return false;
        }

        return focusCoordinator != null && focusCoordinator.TryStartSession(skillId, durationMinutes);
    }

    public bool PauseFocusSession()
    {
        return focusCoordinator != null && focusCoordinator.PauseSession();
    }

    public bool ResumeFocusSession()
    {
        return focusCoordinator != null && focusCoordinator.ResumeSession();
    }

    public bool CancelFocusSession()
    {
        return focusCoordinator != null && focusCoordinator.CancelSession();
    }

    public bool FinishFocusSessionEarly()
    {
        return focusCoordinator != null && focusCoordinator.FinishSessionEarly();
    }

    public FocusSessionSnapshot GetFocusSessionSnapshot()
    {
        return focusCoordinator != null ? focusCoordinator.GetSnapshot() : new FocusSessionSnapshot();
    }

    public FocusSessionResultData GetLastFocusSessionResult()
    {
        return focusCoordinator != null ? focusCoordinator.GetLastResult() : null;
    }

    public void ClearLastFocusSessionResult()
    {
        focusCoordinator?.ClearLastResult();
        SaveGame();
    }

    public bool OpenFocusPanel(string preselectedSkillId = null)
    {
        ResolveReferences();

        if (appShellUI != null)
        {
            return appShellUI.OpenFocus(preselectedSkillId);
        }

        if (focusPanelUI == null)
        {
            return false;
        }

        focusPanelUI.OpenPanel(preselectedSkillId);
        return true;
    }

    public PetStatusSummary GetPetStatusSummary()
    {
        return petFlowCoordinator != null ? petFlowCoordinator.GetStatusSummary() : new PetStatusSummary
        {
            flowState = PetFlowState.Warning,
            priorityStatus = PetPriorityStatus.None,
            headline = "Pet status unavailable",
            guidance = "Try reopening the scene.",
            needsAttention = true
        };
    }

    public int GetReviveCost()
    {
        return petFlowCoordinator != null ? petFlowCoordinator.GetReviveCost() : 0;
    }

    public bool CanRevivePet()
    {
        return petFlowCoordinator != null && petFlowCoordinator.CanRevivePet();
    }

    public bool TryRevivePet()
    {
        return petFlowCoordinator != null && petFlowCoordinator.TryRevivePet();
    }

    public List<SkillEntry> GetSkills()
    {
        return skillsSystem != null ? skillsSystem.GetSkills() : new List<SkillEntry>();
    }

    public SkillEntry GetSkillById(string id)
    {
        return skillsSystem != null ? skillsSystem.GetSkillById(id) : null;
    }

    public List<MissionEntryData> GetAllDailyMissions()
    {
        EnsureDailySkillMissionsCurrent();
        return missionSystem != null ? missionSystem.GetActiveMissions() : new List<MissionEntryData>();
    }

    public List<MissionEntryData> GetSkillMissions()
    {
        EnsureDailySkillMissionsCurrent();
        return missionSystem != null ? missionSystem.GetSkillMissions() : new List<MissionEntryData>();
    }

    public List<MissionEntryData> GetRoutineMissions()
    {
        EnsureDailySkillMissionsCurrent();
        return missionSystem != null ? missionSystem.GetRoutineMissions() : new List<MissionEntryData>();
    }

    public MissionBonusStatus GetSkillMissionBonusStatus()
    {
        EnsureDailySkillMissionsCurrent();
        return missionSystem != null ? missionSystem.GetSkillMissionBonusStatus() : new MissionBonusStatus();
    }

    public int GetRoutineCreationCost()
    {
        EnsureDailySkillMissionsCurrent();
        return missionSystem != null ? missionSystem.GetRoutineCreationCost() : 0;
    }

    public string GetMissionResetCountdownLabel()
    {
        DateTime localNow = TimeService.GetLocalNow();
        DateTime nextReset = new DateTime(localNow.Year, localNow.Month, localNow.Day, TimeService.DailyResetHourLocal, 0, 0);
        if (localNow >= nextReset)
        {
            nextReset = nextReset.AddDays(1);
        }

        TimeSpan remaining = nextReset - localNow;
        if (remaining.TotalHours >= 1d)
        {
            return remaining.ToString(@"hh\:mm");
        }

        return remaining.ToString(@"mm\:ss");
    }

    public bool SelectMission(string missionId, out string message)
    {
        message = string.Empty;
        if (missionSystem == null)
        {
            message = "Mission system unavailable";
            return false;
        }

        MissionSelectionResult result = missionSystem.SelectMission(missionId);
        message = result.message;
        if (!result.success)
        {
            return false;
        }

        SaveGame();
        UpdateMissionUI();
        return true;
    }

    public bool UnselectMission(string missionId, out string message)
    {
        message = string.Empty;
        if (missionSystem == null)
        {
            message = "Mission system unavailable";
            return false;
        }

        MissionSelectionResult result = missionSystem.UnselectMission(missionId);
        message = result.message;
        if (!result.success)
        {
            return false;
        }

        SaveGame();
        UpdateMissionUI();
        return true;
    }

    public bool CompleteRoutineMission(string missionId, out string message)
    {
        message = string.Empty;
        if (missionSystem == null)
        {
            message = "Mission system unavailable";
            return false;
        }

        MissionClaimResult result = missionSystem.CompleteRoutine(missionId);
        if (!result.success)
        {
            message = "Routine unavailable";
            return false;
        }

        ApplyMissionClaimResult(result, true);
        return true;
    }

    public bool ClaimSkillMissionBonus(out string message)
    {
        message = string.Empty;
        if (missionSystem == null)
        {
            message = "Mission system unavailable";
            return false;
        }

        MissionClaimResult result = missionSystem.ClaimSkillMissionBonus();
        if (!result.success)
        {
            message = "Bonus not ready";
            return false;
        }

        ApplyMissionClaimResult(result, true);
        return true;
    }

    public bool CreateCustomSkillMission(string skillId, int durationMinutes, out string message)
    {
        message = string.Empty;
        if (missionSystem == null || skillsSystem == null)
        {
            message = "Mission system unavailable";
            return false;
        }

        SkillEntry skill = skillsSystem.GetSkillById(skillId);
        MissionCreationResult result = missionSystem.CreateSkillMission(
            skillId,
            skill != null ? skill.name : string.Empty,
            durationMinutes,
            progressionSystem != null ? progressionSystem.GetFocusReward(balanceConfig.baseFocusReward) : balanceConfig.baseFocusReward,
            balanceConfig.focusXpGain);

        message = result.message;
        if (!result.success)
        {
            return false;
        }

        SaveGame();
        UpdateMissionUI();
        return true;
    }

    public bool CreateRoutineMission(string title, int rewardCoins, int rewardXp, int rewardMood, int rewardEnergy, float rewardSkillPercent, string rewardSkillId, out string message)
    {
        message = string.Empty;
        if (missionSystem == null || currencySystem == null)
        {
            message = "Mission system unavailable";
            return false;
        }

        int creationCost = missionSystem.GetRoutineCreationCost();
        if (creationCost > 0 && !currencySystem.SpendCoins(creationCost))
        {
            message = $"Need {creationCost} coins";
            return false;
        }

        MissionCreationResult result = missionSystem.CreateRoutine(title, rewardCoins, rewardXp, rewardMood, rewardEnergy, rewardSkillPercent, rewardSkillId);
        message = result.message;
        if (!result.success)
        {
            if (creationCost > 0)
            {
                currencySystem.AddCoins(creationCost);
            }

            return false;
        }

        if (creationCost > 0)
        {
            OnCoinsChanged?.Invoke();
        }

        SaveGame();
        UpdateMissionUI();
        return true;
    }

    public MissionGenerationDebugSnapshot GetMissionGenerationDebugSnapshot()
    {
        return missionSystem != null ? missionSystem.GetLastGenerationDebugSnapshot() : null;
    }

    public string GetMissionDebugSummary()
    {
        return missionSystem != null ? missionSystem.GetMissionDebugSummary() : string.Empty;
    }

    public SkillEntry AddSkill(string name, string icon = "")
    {
        if (skillsSystem == null)
        {
            return null;
        }

        SkillEntry addedSkill = skillsSystem.AddSkill(name, icon);
        if (addedSkill != null)
        {
            EnsureDailySkillMissionsCurrent();
            SaveGame();
            OnSkillsChanged?.Invoke();
            UpdateMissionUI();
        }

        return addedSkill;
    }

    public bool RemoveSkill(string id)
    {
        if (skillsSystem == null)
        {
            return false;
        }

        bool removed = skillsSystem.RemoveSkill(id);
        if (removed)
        {
            if (GetSelectedFocusSkill() == id)
            {
                focusCoordinator?.SetSelectedSkill(string.Empty);
            }

            EnsureDailySkillMissionsCurrent();
            SaveGame();
            OnSkillsChanged?.Invoke();
            UpdateMissionUI();
        }

        return removed;
    }

    public bool SetSelectedFocusSkill(string skillId)
    {
        return focusCoordinator != null && focusCoordinator.SetSelectedSkill(skillId);
    }

    public string GetSelectedFocusSkill()
    {
        return focusCoordinator != null ? focusCoordinator.GetSelectedSkill() : string.Empty;
    }

    public void OnResetButton()
    {
        saveLifecycleCoordinator.RunResetSequence(BeginResetFlow, ReinitializeAfterReset, FinishResetFlow);
    }

    private void BeginResetFlow()
    {
        isResetting = true;
        isInitialized = false;
        justReset = true;
        Debug.Log("Reset: clearing save");
    }

    private void ReinitializeAfterReset()
    {
        Debug.Log("Reset: initializing default state");
        RunBootstrapLifecycle(BootstrapMode.CreateDefaults);
    }

    private void FinishResetFlow()
    {
        Debug.Log("Reset: saving fresh baseline");
        ShowFeedback("Game reset");
        SaveGame();
        isInitialized = true;
        isResetting = false;
    }

    public void OnUpgradeRoomButton()
    {
        coreLoopActionCoordinator?.TryUpgradeRoom();
    }

    // ── Mission helpers ──────────────────────────────────────────────────

    private MissionData CreateDefaultMissionData()
    {
        return new MissionData
        {
            lastDailyResetKey = string.Empty,
            missions = new List<MissionEntryData>()
        };
    }

    private SkillsData CreateDefaultSkillsData()
    {
        return new SkillsData
        {
            skills = new List<SkillEntry>()
        };
    }

    private MissionEntryData GetMission(string missionId)
    {
        if (missionData == null || missionData.missions == null) return null;
        return missionData.missions.Find(m => m.missionId == missionId);
    }

    private void RefreshMissionCompletion(MissionEntryData mission)
    {
        if (mission == null) return;
        if (!mission.isCompleted && mission.currentProgress >= mission.targetProgress)
        {
            mission.currentProgress = mission.targetProgress;
            mission.isCompleted = true;
        }
    }

    public void OnClaimMissionButton(string missionId)
    {
        if (missionSystem == null)
        {
            return;
        }

        MissionClaimResult claimResult = missionSystem.ClaimMission(missionId);
        if (!claimResult.success)
        {
            ShowFeedback("Mission not ready");
            return;
        }

        ApplyMissionClaimResult(claimResult, true);
    }

    // Convenience wrappers for Unity UI Button OnClick (no string param support on all versions)
    public void OnClaimFeedMission()  { OnClaimMissionAtSlot(0); }
    public void OnClaimWorkMission()  { OnClaimMissionAtSlot(1); }
    public void OnClaimFocusMission() { OnClaimMissionAtSlot(2); }
    public void OnClaimExtra1Mission() { OnClaimMissionAtSlot(3); }
    public void OnClaimExtra2Mission() { OnClaimMissionAtSlot(4); }

    private void UpdateMissionUI()
    {
        int hudSlots = GetMissionHudSlotCount();
        UpdateMissionEntry(GetMissionAtSlot(0), missionFeedText, claimFeedButton);
        UpdateMissionEntry(GetMissionAtSlot(1), missionWorkText, claimWorkButton);
        UpdateMissionEntry(GetMissionAtSlot(2), missionFocusText, claimFocusButton);

        if (hudSlots >= 4)
        {
            UpdateMissionEntry(GetMissionAtSlot(3), missionExtra1Text, claimExtra1Button);
        }
        else
        {
            UpdateMissionEntry(null, missionExtra1Text, claimExtra1Button);
        }

        if (hudSlots >= 5)
        {
            UpdateMissionEntry(GetMissionAtSlot(4), missionExtra2Text, claimExtra2Button);
        }
        else
        {
            UpdateMissionEntry(null, missionExtra2Text, claimExtra2Button);
        }

        OnMissionsChanged?.Invoke();
    }

    private void ApplyMissionClaimResult(MissionClaimResult claimResult, bool saveAfter)
    {
        if (claimResult == null || !claimResult.success)
        {
            return;
        }

        if (claimResult.rewardCoins > 0)
        {
            currencySystem.AddCoins(claimResult.rewardCoins);
            OnCoinsChanged?.Invoke();
        }

        if (claimResult.rewardXp > 0)
        {
            AddXP(claimResult.rewardXp, false);
        }

        if (claimResult.rewardMood > 0)
        {
            petSystem?.AddMood(claimResult.rewardMood);
            OnPetChanged?.Invoke();
            OnPetFlowChanged?.Invoke();
        }

        if (claimResult.rewardEnergy > 0)
        {
            petSystem?.AddEnergy(claimResult.rewardEnergy);
            OnPetChanged?.Invoke();
            OnPetFlowChanged?.Invoke();
        }

        if (claimResult.rewardSkillPercent > 0f && skillsSystem != null && !string.IsNullOrEmpty(claimResult.rewardSkillId))
        {
            SkillProgressResult skillProgressResult = skillsSystem.ApplyFocusProgress(
                claimResult.rewardSkillId,
                claimResult.rewardSkillPercent,
                0f,
                TimeService.GetUtcNow().ToString("O"),
                balanceConfig.goldenSkillFocusXpBonus);

            if (skillProgressResult.success && skillProgressResult.deltaApplied > 0f)
            {
                OnSkillProgressAdded?.Invoke(claimResult.rewardSkillId, skillProgressResult.deltaApplied, skillProgressResult.newPercent);
                OnSkillsChanged?.Invoke();
            }
        }

        UpdateMissionUI();

        string rewardSummary = $"+{claimResult.rewardCoins} Coins, +{claimResult.rewardXp} XP";
        if (claimResult.rewardMood > 0)
        {
            rewardSummary += $", +{claimResult.rewardMood} Mood";
        }
        if (claimResult.rewardEnergy > 0)
        {
            rewardSummary += $", +{claimResult.rewardEnergy} Energy";
        }
        if (claimResult.rewardChestCount > 0)
        {
            rewardSummary += $", +{claimResult.rewardChestCount} Chest";
        }

        string label = string.IsNullOrEmpty(claimResult.sourceTitle) ? "Mission" : claimResult.sourceTitle;
        ShowFeedback($"{label}: {rewardSummary}");

        if (saveAfter)
        {
            SaveGame();
        }
    }

    private void UpdateMissionEntry(MissionEntryData mission, TextMeshProUGUI label, Button claimBtn)
    {
        if (mission == null)
        {
            if (label != null) label.text = "No mission";
            if (claimBtn != null) claimBtn.interactable = false;
            return;
        }

        if (label != null)
        {
            string status = mission.isClaimed ? " [Claimed]" : (mission.isCompleted ? " [Completed]" : string.Empty);
            label.text = $"{GetMissionTitleLabel(mission)}: {GetMissionProgressLabel(mission)}{status}";
        }

        if (claimBtn != null)
        {
            claimBtn.interactable = mission.isCompleted && !mission.isClaimed;
        }
    }

    private MissionEntryData GetMissionAtSlot(int slotIndex)
    {
        int hudSlots = GetMissionHudSlotCount();
        if (missionSystem == null || slotIndex < 0 || slotIndex >= hudSlots)
        {
            return null;
        }

        List<MissionEntryData> visibleMissions = missionSystem.GetVisibleMissions(hudSlots);
        return slotIndex < visibleMissions.Count ? visibleMissions[slotIndex] : null;
    }

    private void OnClaimMissionAtSlot(int slotIndex)
    {
        MissionEntryData mission = GetMissionAtSlot(slotIndex);
        if (mission == null)
        {
            ShowFeedback("No mission available");
            return;
        }

        OnClaimMissionButton(mission.missionId);
    }

    private string GetMissionProgressLabel(MissionEntryData mission)
    {
        if (mission == null)
        {
            return "0 / 0";
        }

        if (string.Equals(mission.skillMissionMode, "sessions", StringComparison.Ordinal))
        {
            return $"{mission.currentProgress} / {mission.targetProgress} sessions";
        }

        if (mission.requiredMinutes > 0f)
        {
            return $"{mission.progressMinutes:0.#} / {mission.requiredMinutes:0.#} min";
        }

        return $"{mission.currentProgress} / {mission.targetProgress}";
    }

    private string GetMissionTitleLabel(MissionEntryData mission)
    {
        if (mission == null)
        {
            return "Mission";
        }

        if (!string.IsNullOrEmpty(mission.title))
        {
            return mission.title;
        }

        string skillName = !string.IsNullOrEmpty(mission.targetSkillName)
            ? mission.targetSkillName
            : "Unknown Skill";

        if (string.Equals(mission.skillMissionMode, "sessions", StringComparison.Ordinal))
        {
            return $"Complete {mission.targetProgress} focus sessions on {skillName}";
        }

        if (!string.IsNullOrEmpty(mission.targetSkillId) || !string.IsNullOrEmpty(mission.skillId))
        {
            float targetMinutes = mission.requiredMinutes > 0f ? mission.requiredMinutes : mission.targetProgress;
            return $"Focus {targetMinutes:0.#} min on {skillName}";
        }

        return "Mission";
    }

    private void EnsureDailySkillMissionsCurrent()
    {
        if (missionSystem == null || skillsSystem == null)
        {
            return;
        }

        missionSystem.EnsureDailySkillMissions(skillsSystem.GetSkills(), GetCurrentResetBucketForSystems());
    }

    private DailyRewardData CreateDefaultDailyRewardData()
    {
        return new DailyRewardData { lastClaimDate = string.Empty };
    }

    // ── Onboarding helpers ────────────────────────────────────────────────

    private OnboardingData CreateDefaultOnboardingData()
    {
        return new OnboardingData
        {
            isCompleted = false,
            didWork = false,
            didBuyFood = false,
            didFeed = false,
            didFocus = false
        };
    }

    private void RefreshOnboardingCompletion()
    {
        if (onboardingData == null) return;

        onboardingData.isCompleted =
            onboardingData.didWork &&
            onboardingData.didBuyFood &&
            onboardingData.didFeed &&
            onboardingData.didFocus;
    }

    private string GetCurrentOnboardingHint()
    {
        if (onboardingData == null || onboardingData.isCompleted)
            return string.Empty;

        if (!onboardingData.didWork)
            return "Hint: Tap Work to earn coins";

        if (!onboardingData.didBuyFood)
            return "Hint: Buy food for your pet";

        if (!onboardingData.didFeed)
            return "Hint: Feed your pet";

        if (!onboardingData.didFocus)
            return "Hint: Complete a focus session";

        return string.Empty;
    }

    private void UpdateOnboardingUI()
    {
        if (onboardingHintText == null) return;

        string hint = GetCurrentOnboardingHint();
        onboardingHintText.text = hint;
        onboardingHintText.gameObject.SetActive(!string.IsNullOrEmpty(hint));
    }

    // ── Offline Progress helpers ──────────────────────────────────────────

    private double GetOfflineElapsedSeconds(string lastSeenUtc)
    {
        double maxSeconds = balanceConfig != null ? Mathf.Max(0f, balanceConfig.offlineHungerCapHours * 3600f) : 0d;
        return TimeService.GetOfflineElapsedSeconds(lastSeenUtc, maxSeconds);
    }

    private bool ApplyOfflineProgress(double offlineSeconds)
    {
        if (offlineSeconds <= 0d) return false;
        if (petData == null || petData.isDead) return false;
        if (petSystem == null) return false;

        bool changed = petSystem.ApplyOfflineProgress(
            (float)offlineSeconds,
            balanceConfig.hungerDrainPerSecond,
            balanceConfig.lowHungerMoodThreshold,
            balanceConfig.lowEnergyMoodThreshold,
            balanceConfig.moodDecayPerSecondWhenHungry,
            balanceConfig.moodDecayPerSecondWhenTired
        );

        if (changed)
        {
            Debug.Log($"Offline: {offlineSeconds:F0}s elapsed, pet state normalized");
        }

        return changed;
    }

    private bool ApplyPendingOfflineProgress()
    {
        if (pendingOfflineSeconds <= 0d)
        {
            return false;
        }

        bool changed = ApplyOfflineProgress(pendingOfflineSeconds);
        pendingOfflineSeconds = 0d;
        return changed;
    }

    private bool ApplyDailyResetWindowIfNeeded(string reason)
    {
        if (missionSystem == null || skillsSystem == null)
        {
            return false;
        }

        int previousResetBucket = TimeService.NormalizeResetBucket(lastAppliedResetBucket);
        int effectiveResetBucket = GetCurrentResetBucketForSystems();
        bool hasPreviousBucket = previousResetBucket > 0;
        bool shouldRunReset = hasPreviousBucket && TimeService.ShouldRunDailyReset(previousResetBucket, effectiveResetBucket);
        bool establishedBaseline = !hasPreviousBucket && effectiveResetBucket > 0;
        int previousMissionCount = missionData != null && missionData.missions != null ? missionData.missions.Count : 0;
        string previousMissionResetKey = missionData != null ? missionData.lastDailyResetKey ?? string.Empty : string.Empty;

        if (establishedBaseline)
        {
            lastAppliedResetBucket = effectiveResetBucket;
        }

        missionSystem.EnsureDailySkillMissions(skillsSystem.GetSkills(), effectiveResetBucket);

        if (shouldRunReset)
        {
            Debug.Log($"Daily reset triggered ({reason}): {previousResetBucket} -> {effectiveResetBucket}");
            lastAppliedResetBucket = effectiveResetBucket;
        }

        string currentMissionResetKey = missionData != null ? missionData.lastDailyResetKey ?? string.Empty : string.Empty;
        int currentMissionCount = missionData != null && missionData.missions != null ? missionData.missions.Count : 0;
        bool missionsGenerated = previousMissionCount == 0 && currentMissionCount > 0;
        bool missionResetKeyChanged = !string.Equals(previousMissionResetKey, currentMissionResetKey, StringComparison.Ordinal);

        return establishedBaseline || shouldRunReset || missionsGenerated || missionResetKeyChanged;
    }

    private int GetCurrentResetBucketForSystems()
    {
        int observedResetBucket = TimeService.GetCurrentResetBucketLocal();
        activeResetBucket = TimeService.GetEffectiveResetBucket(lastAppliedResetBucket, observedResetBucket);
        return activeResetBucket;
    }

    private void LogTimeBootstrap(string lastSeenUtc)
    {
        Debug.Log(
            $"Time bootstrap: lastSeenUtc={(string.IsNullOrEmpty(lastSeenUtc) ? "<empty>" : lastSeenUtc)}, " +
            $"elapsed={pendingOfflineSeconds:F0}s, currentBucket={activeResetBucket}, lastBucket={lastAppliedResetBucket}");
    }

    private int GetMissionHudSlotCount()
    {
        bool hasExtraSlot1 = missionExtra1Text != null && claimExtra1Button != null;
        bool hasExtraSlot2 = missionExtra2Text != null && claimExtra2Button != null;

        if (hasExtraSlot1 && hasExtraSlot2)
        {
            return 5;
        }

        if (!missionHudFallbackLogged && (missionExtra1Text == null || claimExtra1Button == null || missionExtra2Text == null || claimExtra2Button == null))
        {
            Debug.LogWarning("Mission HUD extra slots are not fully wired. Falling back to 3 visible mission slots.");
            missionHudFallbackLogged = true;
        }

        return 3;
    }

    private void ValidateOptionalUiWiring()
    {
        if (roomVisualController == null)
        {
            Debug.LogWarning("RoomVisualController is not assigned. Room upgrades will not update visuals.");
        }

        bool hasAnyTierButtons =
            buySnackButton != null || buyMealButton != null || buyPremiumButton != null ||
            feedSnackButton != null || feedMealButton != null || feedPremiumButton != null;
        bool hasAllTierButtons =
            buySnackButton != null && buyMealButton != null && buyPremiumButton != null &&
            feedSnackButton != null && feedMealButton != null && feedPremiumButton != null;

        if (hasAnyTierButtons && !hasAllTierButtons)
        {
            Debug.LogWarning("Tiered food buttons are only partially wired in the scene. Premium/snack/meal flows may be hidden or still need inspector setup.");
        }
    }

    // ── Save ─────────────────────────────────────────────────────────────

    public void SaveGame()
    {
        SaveData data = CreateSaveDataSnapshot();
        Debug.Log($"SaveGame: hunger={petData.hunger}, coins={currencyData.coins}, lastSeenUtc={data.lastSeenUtc}, lastResetBucket={data.lastResetBucket}");
        saveLifecycleCoordinator.SaveState(data);
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            saveLifecycleCoordinator.HandleApplicationPause(
                true,
                isResetting,
                isInitialized,
                null,
                SaveGame);
            return;
        }

        HandleApplicationResume("resume");
    }

    private void OnApplicationQuit()
    {
        saveLifecycleCoordinator.HandleApplicationQuit(isResetting, isInitialized, SaveGame);
    }

    private void HandleApplicationResume(string reason)
    {
        if (isResetting || !isInitialized)
        {
            return;
        }

        if (ApplyDailyResetWindowIfNeeded(reason))
        {
            SaveGame();
            UpdateMissionUI();
            UpdateUI();
        }
    }

    public int GetXpRequiredForNextLevel()
    {
        if (progressionSystem != null)
        {
            return progressionSystem.GetXpRequiredForNextLevel();
        }

        return balanceConfig != null ? balanceConfig.xpToNextLevel : 10;
    }

    private SaveData CreateSaveDataSnapshot()
    {
        string snapshotUtc = TimeService.GetUtcNow().ToString("O");
        return new SaveData
        {
            saveVersion = SaveNormalizer.CurrentSaveVersion,
            petData = petData,
            currencyData = currencyData,
            inventoryData = inventoryData,
            progressionData = progressionData,
            skillsData = skillsData ?? CreateDefaultSkillsData(),
            roomData = roomData,
            missionData = missionData,
            dailyRewardData = dailyRewardData,
            onboardingData = onboardingData,
            focusStateData = focusCoordinator != null ? focusCoordinator.CreateSaveData(snapshotUtc) : focusStateData,
            lastSeenUtc = snapshotUtc,
            lastResetBucket = TimeService.NormalizeResetBucket(lastAppliedResetBucket)
        };
    }

    public bool OpenSkillsPanel()
    {
        ResolveReferences();

        if (appShellUI != null)
        {
            return appShellUI.OpenSkills();
        }

        if (skillsPanelUI == null)
        {
            return false;
        }

        skillsPanelUI.ShowPanel();
        return true;
    }
}
