using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

[DefaultExecutionOrder(-200)]
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
    public event Action<SkillProgressResult> OnSkillProgressAdded;
    public event Action OnMissionsChanged;
    public event Action OnFocusSessionChanged;
    public event Action<FocusSessionResultData> OnFocusResultReady;
    public event Action OnPetFlowChanged;
    public event Action OnIdleChanged;

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
    public IdleData idleData = new IdleData();
    private PetSystem petSystem;
    private CurrencySystem currencySystem;
    private FocusSystem focusSystem;
    private SkillsSystem skillsSystem;
    private SkillDecaySystem skillDecaySystem;
    private BattleSystem battleSystem;
    private MissionSystem missionSystem;
    private InventorySystem inventorySystem;
    private ShopSystem shopSystem;
    private ProgressionSystem progressionSystem;
    private FocusCoordinator focusCoordinator;
    private BattleCoordinator battleCoordinator;
    private HomeDetailsCoordinator homeDetailsCoordinator;
    private MissionCoordinator missionCoordinator;
    private MissionHudCoordinator missionHudCoordinator;
    private ShopRoomCoordinator shopRoomCoordinator;
    private IdleCoordinator idleCoordinator;
    private HomeRuntimeUiCoordinator homeRuntimeUiCoordinator;
    private GameRuntimeLifecycleCoordinator runtimeLifecycleCoordinator;
    private PetFlowCoordinator petFlowCoordinator;
    private CoreLoopActionCoordinator coreLoopActionCoordinator;
    private GameUiShellCoordinator uiShellCoordinator;
    private SaveLifecycleCoordinator saveLifecycleCoordinator = new SaveLifecycleCoordinator();
    private GameSaveLifecycleCoordinator gameSaveLifecycleCoordinator;
    [SerializeField] private Transform sceneUiRoot;
    [SerializeField] private HUDUI hudUI;
    [SerializeField] private SkillsPanelUI skillsPanelUI;
    [SerializeField] private MissionPanelUI missionPanelUI;
    [SerializeField] private ShopPanelUI shopPanelUI;
    [SerializeField] private RoomPanelUI roomPanelUI;
    [SerializeField] private BattlePanelUI battlePanelUI;
    [SerializeField] private HomeDetailsPanelUI homeDetailsPanelUI;
    private bool justReset = false;
    private bool isResetting = false;
    private bool isInitialized = false;
    private double pendingOfflineSeconds = 0d;
    private int activeResetBucket = 0;
    private int lastAppliedResetBucket = 0;
    private const int MaxSupportedRoomLevel = 2;
    private const int MissionHudSlotCount = 5;

    public Button feedButton;
    public Button focusButton;
    public TextMeshProUGUI focusTimerText;
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
    [SerializeField] private bool lifecycleDebugLoggingEnabled = false;

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

    private void LogLifecycle(string message)
    {
        if (lifecycleDebugLoggingEnabled)
        {
            Debug.Log(message);
        }
    }
    
    private string debugInitLog = "Boot...";

    void Awake()
    {
        debugInitLog += " AW |";
        ResolveReferences();
        BindUiDependencies(false);
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

        debugInitLog += mode == BootstrapMode.LoadOrCreate ? " load/create |" : " defaults |";
        ApplyBootstrapState(GamePersistenceUtility.CreateBootstrapState(
            balanceConfig,
            mode == BootstrapMode.LoadOrCreate ? saveLifecycleCoordinator.LoadState() : null));
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

    private void ApplyBootstrapState(GameBootstrapStateData bootstrapState)
    {
        if (bootstrapState == null)
        {
            return;
        }

        petData = bootstrapState.PetData;
        currencyData = bootstrapState.CurrencyData;
        inventoryData = bootstrapState.InventoryData;
        progressionData = bootstrapState.ProgressionData;
        skillsData = bootstrapState.SkillsData;
        roomData = bootstrapState.RoomData;
        missionData = bootstrapState.MissionData;
        dailyRewardData = bootstrapState.DailyRewardData;
        onboardingData = bootstrapState.OnboardingData;
        focusStateData = bootstrapState.FocusStateData ?? new FocusStateData();
        idleData = bootstrapState.IdleData ?? new IdleData();
        pendingOfflineSeconds = bootstrapState.PendingOfflineSeconds;
        activeResetBucket = bootstrapState.ActiveResetBucket;
        lastAppliedResetBucket = bootstrapState.LastAppliedResetBucket;
        LogTimeBootstrap(bootstrapState.LastSeenUtc);
    }

    private void ResolveReferences()
    {
        debugInitLog += " refs |";

        if (sceneUiRoot == null)
        {
            sceneUiRoot = GameManagerUiBootstrapUtility.ResolveSceneUiRoot(new Component[]
            {
                feedbackText,
                onboardingHintText,
                missionFeedText,
                feedButton,
                focusButton
            });
        }

        if (sceneUiRoot == null)
        {
            return;
        }

        if (hudUI == null)
        {
            hudUI = GameManagerUiBootstrapUtility.ResolveUiComponent<HUDUI>(sceneUiRoot);
        }

        if (skillsPanelUI == null)
        {
            skillsPanelUI = GameManagerUiBootstrapUtility.ResolveUiComponent<SkillsPanelUI>(sceneUiRoot);
        }

        if (missionPanelUI == null)
        {
            missionPanelUI = GameManagerUiBootstrapUtility.ResolveUiComponent<MissionPanelUI>(sceneUiRoot);
        }

        if (shopPanelUI == null)
        {
            shopPanelUI = GameManagerUiBootstrapUtility.ResolveUiComponent<ShopPanelUI>(sceneUiRoot);
        }

        if (roomPanelUI == null)
        {
            roomPanelUI = GameManagerUiBootstrapUtility.ResolveUiComponent<RoomPanelUI>(sceneUiRoot);
        }

        if (battlePanelUI == null)
        {
            battlePanelUI = GameManagerUiBootstrapUtility.ResolveUiComponent<BattlePanelUI>(sceneUiRoot);
            if (battlePanelUI == null && sceneUiRoot != null)
            {
                battlePanelUI = sceneUiRoot.gameObject.AddComponent<BattlePanelUI>();
            }
        }

        if (homeDetailsPanelUI == null)
        {
            homeDetailsPanelUI = GameManagerUiBootstrapUtility.ResolveUiComponent<HomeDetailsPanelUI>(sceneUiRoot);
        }

        if (focusPanelUI == null)
        {
            focusPanelUI = GameManagerUiBootstrapUtility.ResolveUiComponent<FocusPanelUI>(sceneUiRoot);
        }

        if (appShellUI == null)
        {
            appShellUI = GameManagerUiBootstrapUtility.ResolveUiComponent<AppShellUI>(sceneUiRoot);
        }
    }

    private bool ValidateStartupPrerequisites()
    {
        if (balanceConfig == null)
        {
            debugInitLog += " ERR: balCfg null |";
            Debug.LogError("BalanceConfig is not assigned in GameManager.");
            return false;
        }

        if (sceneUiRoot == null)
        {
            debugInitLog += " ERR: uiRoot null |";
            Debug.LogError("GameManager could not resolve the scene UI root from assigned controls.");
            return false;
        }

        List<string> missingUiDependencies = GameManagerUiBootstrapUtility.GetMissingDependencies(new GameManagerCriticalUiRefs(
            hudUI,
            skillsPanelUI,
            missionPanelUI,
            shopPanelUI,
            roomPanelUI,
            battlePanelUI,
            focusPanelUI,
            appShellUI));
        if (missingUiDependencies.Count > 0)
        {
            debugInitLog += " ERR: ui deps |";
            Debug.LogError("GameManager is missing critical UI dependencies: " + string.Join(", ", missingUiDependencies));
            return false;
        }

        List<string> missingMissionHudBindings = GetMissingMissionHudBindings();
        if (missingMissionHudBindings.Count > 0)
        {
            debugInitLog += " ERR: mission hud |";
            Debug.LogError("GameManager is missing required Mission HUD bindings: " + string.Join(", ", missingMissionHudBindings));
            return false;
        }

        return true;
    }

    private List<string> GetMissingMissionHudBindings()
    {
        return EnsureMissionHudCoordinator().GetMissingBindings(GetMissionHudSlotBindings());
    }

    private void FinalizeStartup()
    {
        debugInitLog += " finalize |";
        SaveGame();
        isInitialized = true;
        debugInitLog = "START SUCCESS! refs ok.";
        Invoke(nameof(HideDebugStrap), 5f);
    }

    // Runtime/bootstrap phases
    private void ApplyStateToRuntime()
    {
        debugInitLog += " apply |";
        GamePersistenceUtility.NormalizeRuntimeState(
            balanceConfig,
            ref petData,
            ref roomData,
            ref progressionData,
            MaxSupportedRoomLevel);
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
        skillDecaySystem = new SkillDecaySystem();
        skillDecaySystem.Init(skillsData);
        battleSystem = new BattleSystem();
        battleSystem.Init(balanceConfig, skillsSystem);
        missionSystem = new MissionSystem();
        missionSystem.Init(missionData);
        missionSystem.SetDebugLoggingEnabled(missionDebugLoggingEnabled);
        shopSystem = new ShopSystem(currencySystem, inventorySystem);
        progressionSystem = new ProgressionSystem(
            progressionData,
            balanceConfig.xpToNextLevel,
            0
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
                OnPetChanged = BroadcastPetChanged,
                OnPetFlowChanged = BroadcastPetFlowChanged,
                OnProgressionChanged = () => OnProgressionChanged?.Invoke(),
                OnSkillsChanged = () => OnSkillsChanged?.Invoke(),
                OnSkillProgressAdded = result => OnSkillProgressAdded?.Invoke(result),
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
            focusCoordinator,
            balanceConfig,
            new PetFlowCoordinatorCallbacks
            {
                OnPetChanged = BroadcastPetChanged,
                OnPetFlowChanged = BroadcastPetFlowChanged,
                OnFocusSessionChanged = () => OnFocusSessionChanged?.Invoke(),
                SaveGame = SaveGame,
                UpdateUi = UpdateUI,
                ShowFeedback = ShowFeedback
            });
        missionCoordinator = new MissionCoordinator(
            missionSystem,
            skillsSystem,
            progressionSystem,
            currencySystem,
            petSystem,
            balanceConfig,
            new MissionCoordinatorCallbacks
            {
                OnCoinsChanged = () => OnCoinsChanged?.Invoke(),
                BroadcastPetStateChanged = BroadcastPetStateChanged,
                OnSkillsChanged = () => OnSkillsChanged?.Invoke(),
                OnSkillProgressAdded = result => OnSkillProgressAdded?.Invoke(result),
                SaveGame = SaveGame,
                UpdateMissionUi = UpdateMissionUI,
                ShowFeedback = ShowFeedback,
                GetCurrentResetBucket = GetCurrentResetBucketForSystems
            });
        homeDetailsCoordinator = new HomeDetailsCoordinator(
            petFlowCoordinator,
            currencySystem,
            currencyData,
            petData,
            progressionData,
            roomData,
            skillsSystem);
        battleCoordinator = new BattleCoordinator(
            battleSystem,
            petSystem,
            currencySystem,
            new BattleCoordinatorCallbacks
            {
                OnCoinsChanged = () => OnCoinsChanged?.Invoke(),
                BroadcastPetStateChanged = BroadcastPetStateChanged,
                SaveGame = SaveGame,
                UpdateUi = UpdateUI
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
                OnPetChanged = BroadcastPetChanged,
                OnInventoryChanged = () => OnInventoryChanged?.Invoke(),
                ShowMissionRefresh = UpdateMissionUI,
                RefreshOnboardingCompletion = RefreshOnboardingCompletion,
                UpdateOnboardingUi = UpdateOnboardingUI,
                UpdateUi = UpdateUI,
                SaveGame = SaveGame,
                ShowFeedback = ShowFeedback,
                ApplyMissionRewards = ApplyMissionClaimResult
            });
        shopRoomCoordinator = new ShopRoomCoordinator(
            balanceConfig,
            roomData,
            currencyData,
            currencySystem,
            inventorySystem,
            shopSystem,
            coreLoopActionCoordinator,
            MaxSupportedRoomLevel,
            new ShopRoomCoordinatorCallbacks
            {
                OnInventoryChanged = () => OnInventoryChanged?.Invoke(),
                BroadcastPetChanged = BroadcastPetChanged,
                SaveGame = SaveGame,
                UpdateUi = UpdateUI,
                ShowFeedback = ShowFeedback
            });
        homeRuntimeUiCoordinator = new HomeRuntimeUiCoordinator(
            inventorySystem,
            progressionSystem,
            focusSystem,
            petSystem,
            currencyData,
            onboardingData,
            balanceConfig);
        idleCoordinator = new IdleCoordinator(
            idleData,
            skillsSystem,
            petSystem,
            currencySystem,
            inventorySystem,
            roomData,
            balanceConfig);
        runtimeLifecycleCoordinator = new GameRuntimeLifecycleCoordinator(
            petSystem,
            focusSystem,
            skillDecaySystem,
            petData,
            balanceConfig,
            focusCoordinator,
            petFlowCoordinator,
            idleCoordinator,
            new GameRuntimeLifecycleCallbacks
            {
                NotifyAllUi = NotifyAllUI,
                UpdateUi = UpdateUI,
                UpdateMissionUi = UpdateMissionUI,
                UpdateOnboardingUi = UpdateOnboardingUI,
                SaveGame = SaveGame,
                OnPetChanged = () => OnPetChanged?.Invoke(),
                OnPetFlowChanged = () => OnPetFlowChanged?.Invoke(),
                OnIdleChanged = () => OnIdleChanged?.Invoke(),
                ApplyDailyResetWindowIfNeeded = ApplyDailyResetWindowIfNeeded
            });
        focusCoordinator.ResetRuntimeState();
        focusCoordinator.RestoreState(focusStateData, pendingOfflineSeconds);
        petFlowCoordinator.ResetRuntimeState(petSystem != null && petSystem.IsNeglected());
        battleCoordinator.ResetRuntimeState();
    }

    private void InitializeUiBindings()
    {
        debugInitLog += " ui-bind |";
        ResolveReferences();
        BindUiDependencies(true);
    }

    private void BindUiDependencies(bool validateOptionalUi)
    {
        uiShellCoordinator = CreateUiShellCoordinator();
        uiShellCoordinator.BindDependencies();

        if (!validateOptionalUi)
        {
            return;
        }

        ValidateOptionalUiWiring();
    }

    private void NotifyAllUI()
    {
        OnCoinsChanged?.Invoke();
        BroadcastPetChanged();
        BroadcastPetFlowChanged();
        OnInventoryChanged?.Invoke();
        OnProgressionChanged?.Invoke();
        OnSkillsChanged?.Invoke();
        OnMissionsChanged?.Invoke();
        OnFocusSessionChanged?.Invoke();
        OnIdleChanged?.Invoke();
    }

    private void BroadcastPetChanged()
    {
        EnsureRuntimeLifecycleCoordinator().BroadcastPetChanged();
    }

    private void BroadcastPetFlowChanged()
    {
        EnsureRuntimeLifecycleCoordinator().BroadcastPetFlowChanged();
    }

    private void BroadcastPetStateChanged()
    {
        EnsureRuntimeLifecycleCoordinator().BroadcastPetStateChanged();
    }

    private void SyncUiFromRuntime()
    {
        debugInitLog += " sync-ui |";
        EnsureRuntimeLifecycleCoordinator().SyncUiFromRuntime();
    }

    void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        EnsureRuntimeLifecycleCoordinator().Tick(Time.deltaTime, Time.unscaledDeltaTime, ref justReset);
    }

    public void OnFeedButton()       { TryFeedItem("food_basic", balanceConfig.feedAmount); }
    public void OnFeedSnackButton()  { TryFeedItem("food_snack", balanceConfig.snackRestore); }
    public void OnFeedMealButton()   { TryFeedItem("food_meal", balanceConfig.mealRestore); }
    public void OnFeedPremiumButton(){ TryFeedItem("food_premium", balanceConfig.premiumRestore); }

    private void TryFeedItem(string itemId, float amount)
    {
        coreLoopActionCoordinator?.TryFeedItem(itemId, amount);
    }

    public void OnBuySnackButton()  { TryBuyItem("food_snack", balanceConfig.snackPrice); }
    public void OnBuyMealButton()   { TryBuyItem("food_meal", balanceConfig.mealPrice); }
    public void OnBuyPremiumButton(){ TryBuyItem("food_premium", balanceConfig.premiumPrice); }

    private ShopPurchaseResult TryBuyItem(string itemId, int price)
    {
        return coreLoopActionCoordinator != null
            ? coreLoopActionCoordinator.TryBuyItem(itemId, price)
            : ShopPurchaseResult.Fail("Shop unavailable");
    }

    void UpdateUI()
    {
        EnsureHomeRuntimeUiCoordinator().UpdateUi(GetHomeRuntimeUiRefs());
    }

    public void OnFocusButton()
    {
        if (petSystem != null && petSystem.IsNeglected())
        {
            ShowFeedback("Pet neglected. Care first.");
            return;
        }

        ResolveReferences();
        if (EnsureUiShellCoordinator().OpenFocus())
        {
            return;
        }

        ShowFeedback("Focus panel missing");
    }

    public bool TryStartFocusSession(string skillId, int durationMinutes)
    {
        if (petSystem != null && petSystem.IsNeglected())
        {
            ShowFeedback("Pet neglected. Care first.");
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
        if (focusCoordinator != null && focusCoordinator.ClearLastResult())
        {
            SaveGame();
        }
    }

    public bool OpenFocusPanel(string preselectedSkillId = null)
    {
        ResolveReferences();
        return EnsureUiShellCoordinator().OpenFocus(preselectedSkillId);
    }

    public PetStatusSummary GetPetStatusSummary()
    {
        return EnsureHomeDetailsCoordinator().GetPetStatusSummary();
    }

    public string GetPetVitalsSummaryText()
    {
        return EnsureHomeDetailsCoordinator().GetPetVitalsSummaryText();
    }

    public HomeDetailsViewData GetHomeDetailsView(HomeDetailsTab tab)
    {
        return EnsureHomeDetailsCoordinator().GetView(tab);
    }

    public List<SkillEntry> GetSkills()
    {
        return skillsSystem != null ? skillsSystem.GetSkills() : new List<SkillEntry>();
    }

    public List<SkillProgressionViewData> GetSkillProgressionViews()
    {
        return skillsSystem != null ? skillsSystem.GetSkillProgressionViews() : new List<SkillProgressionViewData>();
    }

    public SkillProgressionViewData GetSkillProgressionView(string id)
    {
        return skillsSystem != null ? skillsSystem.GetSkillProgressionView(id) : null;
    }

    public SkillEntry GetSkillById(string id)
    {
        return skillsSystem != null ? skillsSystem.GetSkillById(id) : null;
    }

    public SkillArchetypeDefinition GetSkillArchetype(string archetypeId)
    {
        return SkillArchetypeCatalog.GetDefinition(archetypeId);
    }

    public List<SkillArchetypeDefinition> GetSelectableSkillArchetypes()
    {
        return new List<SkillArchetypeDefinition>(SkillArchetypeCatalog.GetPlayerSelectableDefinitions());
    }

    public IdleHomeView GetIdleHomeView()
    {
        return EnsureIdleCoordinator().GetHomeView();
    }

    public int GetPendingIdleEventCount()
    {
        return EnsureIdleCoordinator().GetPendingEventCount();
    }

    public IdleClaimResult ClaimPendingIdleEvents()
    {
        IdleClaimResult result = EnsureIdleCoordinator().ClaimPendingEvents();
        if (!result.Success)
        {
            return result;
        }

        SaveGame();
        OnIdleChanged?.Invoke();

        if (result.CoinsGranted > 0)
        {
            OnCoinsChanged?.Invoke();
        }

        if (result.ItemsGranted > 0 || result.SkinsGranted > 0)
        {
            OnInventoryChanged?.Invoke();
        }

        UpdateUI();
        ShowFeedback(result.Message);
        return result;
    }

    public BattlePlayerPreviewData GetBattlePlayerPreview()
    {
        return battleCoordinator != null ? battleCoordinator.GetPlayerPreview() : new BattlePlayerPreviewData();
    }

    public BattleAvailabilityData GetBattleAvailability()
    {
        return battleCoordinator != null ? battleCoordinator.GetAvailability() : new BattleAvailabilityData();
    }

    public List<BossDefinitionData> GetBattleBosses()
    {
        return battleCoordinator != null ? battleCoordinator.GetBosses() : new List<BossDefinitionData>();
    }

    public BossDefinitionData SelectBattleBoss(string bossId)
    {
        return battleCoordinator != null ? battleCoordinator.SelectBoss(bossId) : null;
    }

    public string GetSelectedBattleBossId()
    {
        return battleCoordinator != null ? battleCoordinator.GetSelectedBossId() : string.Empty;
    }

    public BattleResultData ResolveBattle(string bossId)
    {
        return battleCoordinator != null
            ? battleCoordinator.ResolveBattle(bossId)
            : new BattleResultData
            {
                wasBlocked = true,
                statusMessage = "Battle unavailable.",
                adviceMessage = "Battle system is not ready yet."
            };
    }

    public bool HasSkillName(string name)
    {
        return skillsSystem != null && skillsSystem.HasSkillName(name);
    }

    public List<MissionEntryData> GetAllDailyMissions()
    {
        EnsureDailySkillMissionsCurrent();
        return EnsureMissionCoordinator().GetActiveMissions();
    }

    public int GetAvailableMissionClaimCount()
    {
        EnsureDailySkillMissionsCurrent();
        return EnsureMissionCoordinator().GetAvailableClaimCount();
    }

    public List<MissionEntryData> GetSkillMissions()
    {
        EnsureDailySkillMissionsCurrent();
        return EnsureMissionCoordinator().GetSkillMissions();
    }

    public List<MissionEntryData> GetRoutineMissions()
    {
        EnsureDailySkillMissionsCurrent();
        return EnsureMissionCoordinator().GetRoutineMissions();
    }

    public int GetCurrentCoins()
    {
        if (currencySystem != null)
        {
            return currencySystem.GetCoins();
        }

        return currencyData != null ? currencyData.coins : 0;
    }

    public RoomPanelStateData GetRoomPanelState()
    {
        return EnsureShopRoomCoordinator().GetRoomPanelState();
    }

    public bool TryUpgradeRoomFromPanel(out string message)
    {
        return EnsureShopRoomCoordinator().TryUpgradeRoom(out message);
    }

    public bool OpenRoomPanel()
    {
        ResolveReferences();
        return EnsureUiShellCoordinator().OpenRoom();
    }

    public bool OpenHomeDetailsPanel()
    {
        ResolveReferences();
        return EnsureUiShellCoordinator().OpenHomeDetails();
    }

    public bool OpenBattlePanel()
    {
        ResolveReferences();
        return EnsureUiShellCoordinator().OpenBattle();
    }

    public List<ShopItemViewData> GetShopItems(ShopCategory category)
    {
        return EnsureShopRoomCoordinator().GetShopItems(category);
    }

    public string GetShopPlaceholderMessage(ShopCategory category)
    {
        return EnsureShopRoomCoordinator().GetShopPlaceholderMessage(category);
    }

    public string GetShopCategoryStatus(ShopCategory category)
    {
        return EnsureShopRoomCoordinator().GetShopCategoryStatus(category);
    }

    public bool TryPurchaseShopItem(string itemId, out string message)
    {
        return EnsureShopRoomCoordinator().TryPurchaseItem(itemId, out message);
    }

    public bool TryUseShopItem(string itemId, out string message)
    {
        return EnsureShopRoomCoordinator().TryUseItem(itemId, out message);
    }

    public bool TryEquipShopSkin(string itemId, out string message)
    {
        return EnsureShopRoomCoordinator().TryEquipSkin(itemId, out message);
    }

    public string GetEquippedSkinId()
    {
        return EnsureShopRoomCoordinator().GetEquippedSkinId();
    }

    public MissionBonusStatus GetSkillMissionBonusStatus()
    {
        return EnsureMissionCoordinator().GetSkillMissionBonusStatus();
    }

    public int GetRoutineCreationCost()
    {
        return EnsureMissionCoordinator().GetRoutineCreationCost();
    }

    public string GetMissionResetCountdownLabel()
    {
        return EnsureMissionCoordinator().GetMissionResetCountdownLabel();
    }

    public bool SelectMission(string missionId, out string message)
    {
        return EnsureMissionCoordinator().SelectMission(missionId, out message);
    }

    public bool UnselectMission(string missionId, out string message)
    {
        return EnsureMissionCoordinator().UnselectMission(missionId, out message);
    }

    public bool CompleteRoutineMission(string missionId, out string message)
    {
        return EnsureMissionCoordinator().CompleteRoutineMission(missionId, out message);
    }

    public bool ClaimSkillMissionBonus(out string message)
    {
        return EnsureMissionCoordinator().ClaimSkillMissionBonus(out message);
    }

    public bool CreateCustomSkillMission(string skillId, int durationMinutes, out string message)
    {
        return EnsureMissionCoordinator().CreateCustomSkillMission(skillId, durationMinutes, out message);
    }

    public bool CreateRoutineMission(string title, int rewardCoins, int rewardMood, int rewardEnergy, int rewardSkillSP, string rewardSkillId, out string message)
    {
        return EnsureMissionCoordinator().CreateRoutineMission(title, rewardCoins, rewardMood, rewardEnergy, rewardSkillSP, rewardSkillId, out message);
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
        HandleSkillCollectionChanged(addedSkill != null);
        return addedSkill;
    }

    public SkillEntry AddSkillWithArchetype(string name, string archetypeId)
    {
        if (skillsSystem == null)
        {
            return null;
        }

        SkillEntry addedSkill = skillsSystem.AddSkillWithArchetype(name, archetypeId);
        HandleSkillCollectionChanged(addedSkill != null);
        return addedSkill;
    }

    public bool ChangeSkillArchetype(string skillId, string archetypeId)
    {
        if (skillsSystem == null)
        {
            return false;
        }

        bool changed = skillsSystem.ChangeSkillArchetype(skillId, archetypeId);
        if (!changed)
        {
            return false;
        }

        SaveGame();
        OnSkillsChanged?.Invoke();
        return true;
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

    private void HandleSkillCollectionChanged(bool stateChanged)
    {
        if (!stateChanged)
        {
            return;
        }

        EnsureDailySkillMissionsCurrent();
        SaveGame();
        OnSkillsChanged?.Invoke();
        UpdateMissionUI();
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
        EnsureGameSaveLifecycleCoordinator().ResetGame(
            ref isResetting,
            ref isInitialized,
            ref justReset,
            LogLifecycle,
            () => RunBootstrapLifecycle(BootstrapMode.CreateDefaults),
            ShowFeedback,
            SaveGame);
    }

    // ── Mission helpers ──────────────────────────────────────────────────

    public void OnClaimMissionButton(string missionId)
    {
        EnsureMissionHudCoordinator().ClaimMission(missionId);
    }

    // Convenience wrappers for Unity UI Button OnClick (no string param support on all versions)
    public void OnClaimFeedMission()  { OnClaimMissionAtSlot(0); }
    public void OnClaimWorkMission()  { OnClaimMissionAtSlot(1); }
    public void OnClaimFocusMission() { OnClaimMissionAtSlot(2); }
    public void OnClaimExtra1Mission() { OnClaimMissionAtSlot(3); }
    public void OnClaimExtra2Mission() { OnClaimMissionAtSlot(4); }

    private void UpdateMissionUI()
    {
        EnsureMissionHudCoordinator().UpdateHud(GetMissionHudSlotBindings());
    }

    private void ApplyMissionClaimResult(MissionClaimResult claimResult, bool saveAfter)
    {
        EnsureMissionCoordinator().ApplyClaimResult(claimResult, saveAfter);
    }

    private void OnClaimMissionAtSlot(int slotIndex)
    {
        EnsureMissionHudCoordinator().ClaimMissionAtSlot(slotIndex);
    }

    private void EnsureDailySkillMissionsCurrent()
    {
        EnsureMissionCoordinator().EnsureDailySkillMissionsCurrent();
    }

    private MissionHudSlotRefs[] GetMissionHudSlotBindings()
    {
        return new[]
        {
            new MissionHudSlotRefs(nameof(missionFeedText), missionFeedText, nameof(claimFeedButton), claimFeedButton),
            new MissionHudSlotRefs(nameof(missionWorkText), missionWorkText, nameof(claimWorkButton), claimWorkButton),
            new MissionHudSlotRefs(nameof(missionFocusText), missionFocusText, nameof(claimFocusButton), claimFocusButton),
            new MissionHudSlotRefs(nameof(missionExtra1Text), missionExtra1Text, nameof(claimExtra1Button), claimExtra1Button),
            new MissionHudSlotRefs(nameof(missionExtra2Text), missionExtra2Text, nameof(claimExtra2Button), claimExtra2Button)
        };
    }


    // ── Onboarding helpers ────────────────────────────────────────────────


    private void RefreshOnboardingCompletion()
    {
        EnsureHomeRuntimeUiCoordinator().RefreshOnboardingCompletion();
    }

    private void UpdateOnboardingUI()
    {
        EnsureHomeRuntimeUiCoordinator().UpdateOnboardingUi(GetHomeRuntimeUiRefs());
    }

    // ── Offline Progress helpers ──────────────────────────────────────────

    private bool ApplyPendingOfflineProgress()
    {
        if (pendingOfflineSeconds <= 0d)
        {
            return false;
        }

        double offlineSeconds = pendingOfflineSeconds;
        bool petChanged = GamePersistenceUtility.ApplyOfflineProgress(
            petData,
            petSystem,
            skillDecaySystem,
            balanceConfig,
            offlineSeconds,
            LogLifecycle);
        IdleRuntimeUpdate idleUpdate = EnsureIdleCoordinator().ApplyOffline(offlineSeconds, TimeService.GetUtcNow());
        pendingOfflineSeconds = 0d;

        if (idleUpdate.StateChanged)
        {
            OnIdleChanged?.Invoke();
        }

        return petChanged || idleUpdate.StateChanged || idleUpdate.SaveRequired;
    }

    private bool ApplyDailyResetWindowIfNeeded(string reason)
    {
        return GamePersistenceUtility.ApplyDailyResetWindowIfNeeded(
            missionSystem,
            skillsSystem,
            missionData,
            ref lastAppliedResetBucket,
            ref activeResetBucket,
            reason,
            LogLifecycle);
    }

    private int GetCurrentResetBucketForSystems()
    {
        return GamePersistenceUtility.GetCurrentResetBucketForSystems(lastAppliedResetBucket, out activeResetBucket);
    }

    private void LogTimeBootstrap(string lastSeenUtc)
    {
        LogLifecycle(GamePersistenceUtility.BuildTimeBootstrapMessage(
            lastSeenUtc,
            pendingOfflineSeconds,
            activeResetBucket,
            lastAppliedResetBucket));
    }

    private void ValidateOptionalUiWiring()
    {
        if (EnsureHomeRuntimeUiCoordinator().HasPartialTierButtonWiring(GetHomeRuntimeUiRefs()))
        {
            Debug.LogWarning("Tiered food buttons are only partially wired in the scene. Premium/snack/meal flows may be hidden or still need inspector setup.");
        }
    }

    // ── Save ─────────────────────────────────────────────────────────────

    public void SaveGame()
    {
        EnsureGameSaveLifecycleCoordinator().SaveGame(CreateSaveDataSnapshot, petData, currencyData, LogLifecycle);
    }

    private void OnApplicationPause(bool pause)
    {
        EnsureGameSaveLifecycleCoordinator().HandleApplicationPause(
            pause,
            isResetting,
            isInitialized,
            SaveGame,
            () => EnsureRuntimeLifecycleCoordinator().HandleApplicationResume(isResetting, isInitialized, "resume"));
    }

    private void OnApplicationQuit()
    {
        EnsureGameSaveLifecycleCoordinator().HandleApplicationQuit(isResetting, isInitialized, SaveGame);
    }

    private SaveData CreateSaveDataSnapshot()
    {
        return GamePersistenceUtility.CreateSaveDataSnapshot(
            petData,
            currencyData,
            inventoryData,
            progressionData,
            skillsData,
            roomData,
            missionData,
            dailyRewardData,
            onboardingData,
            focusStateData,
            idleData,
            focusCoordinator,
            lastAppliedResetBucket);
    }

    private GameSaveLifecycleCoordinator EnsureGameSaveLifecycleCoordinator()
    {
        if (gameSaveLifecycleCoordinator == null)
        {
            gameSaveLifecycleCoordinator = new GameSaveLifecycleCoordinator(
                saveLifecycleCoordinator.SaveState,
                saveLifecycleCoordinator.ResetPersistentState);
        }

        return gameSaveLifecycleCoordinator;
    }

    public bool OpenSkillsPanel()
    {
        ResolveReferences();
        return EnsureUiShellCoordinator().OpenSkills();
    }

    public bool OpenShopPanel()
    {
        ResolveReferences();
        return EnsureUiShellCoordinator().OpenShop();
    }

    private HomeRuntimeUiRefs GetHomeRuntimeUiRefs()
    {
        return new HomeRuntimeUiRefs(
            feedButton,
            buySnackButton,
            buyMealButton,
            buyPremiumButton,
            feedSnackButton,
            feedMealButton,
            feedPremiumButton,
            focusButton,
            focusTimerText,
            focusButtonText,
            onboardingHintText);
    }

    private GameUiShellCoordinator CreateUiShellCoordinator()
    {
        return new GameUiShellCoordinator(
            this,
            hudUI,
            appShellUI,
            skillsPanelUI,
            missionPanelUI,
            shopPanelUI,
            roomPanelUI,
            battlePanelUI,
            focusPanelUI,
            homeDetailsPanelUI);
    }

    private GameUiShellCoordinator EnsureUiShellCoordinator()
    {
        if (uiShellCoordinator == null)
        {
            uiShellCoordinator = CreateUiShellCoordinator();
        }

        return uiShellCoordinator;
    }

    private MissionHudCoordinator EnsureMissionHudCoordinator()
    {
        if (missionHudCoordinator == null)
        {
            missionHudCoordinator = new MissionHudCoordinator(
                EnsureMissionCoordinator(),
                MissionHudSlotCount,
                () => OnMissionsChanged?.Invoke(),
                ShowFeedback);
        }

        return missionHudCoordinator;
    }

    private HomeRuntimeUiCoordinator EnsureHomeRuntimeUiCoordinator()
    {
        if (homeRuntimeUiCoordinator == null)
        {
            homeRuntimeUiCoordinator = new HomeRuntimeUiCoordinator(
                inventorySystem,
                progressionSystem,
                focusSystem,
                petSystem,
                currencyData,
                onboardingData,
                balanceConfig);
        }

        return homeRuntimeUiCoordinator;
    }

    private IdleCoordinator EnsureIdleCoordinator()
    {
        if (idleCoordinator == null)
        {
            idleCoordinator = new IdleCoordinator(
                idleData,
                skillsSystem,
                petSystem,
                currencySystem,
                inventorySystem,
                roomData,
                balanceConfig);
        }

        return idleCoordinator;
    }

    private GameRuntimeLifecycleCoordinator EnsureRuntimeLifecycleCoordinator()
    {
        if (runtimeLifecycleCoordinator == null)
        {
            runtimeLifecycleCoordinator = new GameRuntimeLifecycleCoordinator(
                petSystem,
                focusSystem,
            skillDecaySystem,
            petData,
            balanceConfig,
            focusCoordinator,
            petFlowCoordinator,
            idleCoordinator,
            new GameRuntimeLifecycleCallbacks
            {
                NotifyAllUi = NotifyAllUI,
                UpdateUi = UpdateUI,
                UpdateMissionUi = UpdateMissionUI,
                UpdateOnboardingUi = UpdateOnboardingUI,
                SaveGame = SaveGame,
                OnPetChanged = () => OnPetChanged?.Invoke(),
                OnPetFlowChanged = () => OnPetFlowChanged?.Invoke(),
                OnIdleChanged = () => OnIdleChanged?.Invoke(),
                ApplyDailyResetWindowIfNeeded = ApplyDailyResetWindowIfNeeded
            });
        }

        return runtimeLifecycleCoordinator;
    }

    private ShopRoomCoordinator EnsureShopRoomCoordinator()
    {
        if (shopRoomCoordinator == null)
        {
            shopRoomCoordinator = new ShopRoomCoordinator(
                balanceConfig,
                roomData,
                currencyData,
                currencySystem,
                inventorySystem,
                shopSystem,
                coreLoopActionCoordinator,
                MaxSupportedRoomLevel,
                new ShopRoomCoordinatorCallbacks
                {
                    OnInventoryChanged = () => OnInventoryChanged?.Invoke(),
                    BroadcastPetChanged = BroadcastPetChanged,
                    SaveGame = SaveGame,
                    UpdateUi = UpdateUI,
                    ShowFeedback = ShowFeedback
                });
        }

        return shopRoomCoordinator;
    }

    private HomeDetailsCoordinator EnsureHomeDetailsCoordinator()
    {
        if (homeDetailsCoordinator == null)
        {
            homeDetailsCoordinator = new HomeDetailsCoordinator(
                petFlowCoordinator,
                currencySystem,
                currencyData,
                petData,
                progressionData,
                roomData,
                skillsSystem);
        }

        return homeDetailsCoordinator;
    }

    private MissionCoordinator EnsureMissionCoordinator()
    {
        if (missionCoordinator == null)
        {
            missionCoordinator = new MissionCoordinator(
                missionSystem,
                skillsSystem,
                progressionSystem,
                currencySystem,
                petSystem,
                balanceConfig,
                new MissionCoordinatorCallbacks
                {
                    OnCoinsChanged = () => OnCoinsChanged?.Invoke(),
                    BroadcastPetStateChanged = BroadcastPetStateChanged,
                    OnSkillsChanged = () => OnSkillsChanged?.Invoke(),
                    OnSkillProgressAdded = result => OnSkillProgressAdded?.Invoke(result),
                    SaveGame = SaveGame,
                    UpdateMissionUi = UpdateMissionUI,
                    ShowFeedback = ShowFeedback,
                    GetCurrentResetBucket = GetCurrentResetBucketForSystems
                });
        }

        return missionCoordinator;
    }
}
