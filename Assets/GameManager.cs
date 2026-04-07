using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public event Action OnCoinsChanged;
    public event Action OnPetChanged;
    public event Action OnInventoryChanged;
    public event Action OnProgressionChanged;

    public BalanceConfig balanceConfig;

    public PetData petData = new PetData();
    public CurrencyData currencyData = new CurrencyData();
    public InventoryData inventoryData = new InventoryData();
    public ProgressionData progressionData = new ProgressionData();
    public RoomData roomData = new RoomData();
    public MissionData missionData = new MissionData();
    public DailyRewardData dailyRewardData = new DailyRewardData();
    private PetSystem petSystem;
    private CurrencySystem currencySystem;
    private FocusSystem focusSystem;
    private InventorySystem inventorySystem;
    private ShopSystem shopSystem;
    private ProgressionSystem progressionSystem;
    private SaveManager saveManager = new SaveManager();
    [SerializeField] private RoomVisualController roomVisualController;

    public Button workButton;
    public Button feedButton;
    public Button buyButton;
    public Button focusButton;
    public Button upgradeRoomButton;
    public TextMeshProUGUI focusTimerText;
    public TextMeshProUGUI workButtonText;
    public TextMeshProUGUI focusButtonText;

    [Header("Mission UI")]
    public TextMeshProUGUI missionFeedText;
    public TextMeshProUGUI missionWorkText;
    public TextMeshProUGUI missionFocusText;
    public Button claimFeedButton;
    public Button claimWorkButton;
    public Button claimFocusButton;

    [Header("Daily Reward UI")]
    public TextMeshProUGUI dailyRewardStatusText;
    public Button claimDailyRewardButton;

    void Start()
    {
        if (balanceConfig == null)
        {
            Debug.LogError("BalanceConfig is not assigned in GameManager.");
            return;
        }

        InitializeDataFromSaveOrDefaults();
        InitializeSystems();

        Debug.Log("Pet hunger: " + petData.hunger);
        if (roomVisualController != null) roomVisualController.ApplyRoom(roomData);
        NotifyAllUI();
        UpdateUI();
        UpdateMissionUI();
        UpdateDailyRewardUI();
    }

    private void InitializeDefaultState()
    {
        petData = new PetData();
        currencyData = new CurrencyData();
        inventoryData = new InventoryData();
        progressionData = new ProgressionData();
        roomData = new RoomData();
        missionData = CreateDefaultMissionData();
        dailyRewardData = CreateDefaultDailyRewardData();

        petData.hunger = balanceConfig.startingHunger;
        currencyData.coins = balanceConfig.startingCoins;
        progressionData.level = balanceConfig.startingLevel;
        progressionData.xp = balanceConfig.startingXp;
        roomData.roomLevel = 0;
    }

    private void InitializeDataFromSaveOrDefaults()
    {
        SaveData loaded = saveManager.Load();

        if (loaded != null)
        {
            petData = loaded.petData ?? new PetData();
            currencyData = loaded.currencyData ?? new CurrencyData();
            inventoryData = loaded.inventoryData ?? new InventoryData();
            progressionData = loaded.progressionData ?? new ProgressionData();
            roomData = loaded.roomData ?? new RoomData();
            missionData = loaded.missionData ?? CreateDefaultMissionData();
            dailyRewardData = loaded.dailyRewardData ?? CreateDefaultDailyRewardData();
        }
        else
        {
            InitializeDefaultState();
        }

        if (petData.hunger <= 0f && !petData.isDead)
        {
            petData.isDead = true;
            petData.statusText = "Dead";
        }
    }

    private void InitializeSystems()
    {
        petSystem = new PetSystem(petData);
        currencySystem = new CurrencySystem(currencyData);
        inventorySystem = new InventorySystem(inventoryData);
        focusSystem = new FocusSystem();
        shopSystem = new ShopSystem(currencySystem, inventorySystem);
        progressionSystem = new ProgressionSystem(
            progressionData,
            balanceConfig.xpToNextLevel,
            balanceConfig.buyUnlockLevel
        );
    }

    private void NotifyAllUI()
    {
        OnCoinsChanged?.Invoke();
        OnPetChanged?.Invoke();
        OnInventoryChanged?.Invoke();
        OnProgressionChanged?.Invoke();
    }

    void Update()
    {
        petSystem.UpdateHunger(Time.deltaTime, balanceConfig.hungerDrainPerSecond);
        petSystem.UpdateStatus();
        OnPetChanged?.Invoke();

        if (focusSystem.IsRunning && !petData.isDead)
        {
            bool completed = focusSystem.Update(Time.deltaTime);
            if (completed)
            {
                OnFocusCompleted();
            }
        }

        UpdateUI();
    }

    public void OnFeedButton()
    {
        if (inventorySystem.ConsumeFood(1))
        {
            petSystem.Feed(balanceConfig.feedAmount);
            OnInventoryChanged?.Invoke();
            OnPetChanged?.Invoke();

            AddXP(balanceConfig.feedXpGain);
            IncrementMissionProgress("feed_1");

            Debug.Log("Fed pet");
            SaveGame();
        }
        else
        {
            Debug.Log("No food in inventory");
        }

        UpdateUI();
        UpdateMissionUI();
    }

    public void OnBuyButton()
    {
        if (petData.isDead) return;

        if (shopSystem.BuyFood(balanceConfig.foodPrice, 1))
        {
            OnCoinsChanged?.Invoke();
            OnInventoryChanged?.Invoke();

            AddXP(balanceConfig.buyXpGain);

            Debug.Log("Food bought");
            SaveGame();
        }
        else
        {
            Debug.Log("Not enough coins");
        }

        UpdateUI();
    }

    void OnFocusCompleted()
    {
        int focusReward = progressionSystem.GetFocusReward(balanceConfig.baseFocusReward);
        currencySystem.AddCoins(focusReward);
        OnCoinsChanged?.Invoke();
        AddXP(balanceConfig.focusXpGain);
        IncrementMissionProgress("focus_1");

        Debug.Log("Focus completed, reward: " + focusReward);
        SaveGame();
        UpdateMissionUI();
    }

    public void OnWorkButton()
    {
        if (petData.isDead) return;

        int workReward = progressionSystem.GetWorkReward(balanceConfig.baseWorkReward);
        currencySystem.AddCoins(workReward);
        OnCoinsChanged?.Invoke();
        AddXP(balanceConfig.workXpGain);
        IncrementMissionProgress("work_3");

        Debug.Log("Worked and earned " + workReward + " coins");
        SaveGame();

        UpdateUI();
        UpdateMissionUI();
    }

    void UpdateUI()
    {
        bool dead = petData.isDead;

        if (feedButton != null)
            feedButton.interactable = !dead && inventorySystem.HasFood(1);

        if (buyButton != null)
            buyButton.interactable = !dead && progressionSystem.IsBuyUnlocked() && currencyData.coins >= balanceConfig.foodPrice;

        if (workButton != null)
            workButton.interactable = !dead;

        if (focusButton != null)
            focusButton.interactable = !dead && !focusSystem.IsRunning;

        if (upgradeRoomButton != null)
            upgradeRoomButton.interactable = CanUpgradeRoom();

        if (focusTimerText != null)
        {
            if (dead)
                focusTimerText.text = "Focus: Dead";
            else if (focusSystem.IsRunning)
                focusTimerText.text = "Focus: " + Mathf.CeilToInt(focusSystem.GetRemainingTime()) + "s";
            else
                focusTimerText.text = "Focus: Ready";
        }

        int workReward  = progressionSystem.GetWorkReward(balanceConfig.baseWorkReward);
        int focusReward = progressionSystem.GetFocusReward(balanceConfig.baseFocusReward);

        if (workButtonText != null)
            workButtonText.text = $"Work (+{workReward})";

        if (focusButtonText != null)
            focusButtonText.text = $"Focus (+{focusReward})";
    }

    void AddXP(int amount)
    {
        progressionSystem.AddXp(amount);
        OnProgressionChanged?.Invoke();
        SaveGame();
    }

    public void OnFocusButton()
    {
        if (petData.isDead)
            return;

        if (!focusSystem.IsRunning)
        {
            focusSystem.StartFocus(balanceConfig.baseFocusDuration);
            Debug.Log("Focus started");
        }
    }

    public void OnResetButton()
    {
        saveManager.Reset();
        PlayerPrefs.DeleteAll(); // fallback cleanup

        InitializeDefaultState();
        InitializeSystems();

        if (roomVisualController != null) roomVisualController.ApplyRoom(roomData);

        SaveGame();
        NotifyAllUI();
        UpdateUI();
        UpdateMissionUI();
        UpdateDailyRewardUI();

        Debug.Log("Save reset");
    }

    private int GetCurrentRoomUpgradeCost()
    {
        if (roomData == null) return 0;
        if (roomData.roomLevel == 0) return balanceConfig.roomUpgrade1Cost;
        if (roomData.roomLevel == 1) return balanceConfig.roomUpgrade2Cost;
        return 0;
    }

    private int GetCurrentRoomUnlockLevel()
    {
        if (roomData == null) return 999;
        if (roomData.roomLevel == 0) return balanceConfig.roomUpgrade1UnlockLevel;
        if (roomData.roomLevel == 1) return balanceConfig.roomUpgrade2UnlockLevel;
        return 999;
    }

    private bool CanUpgradeRoom()
    {
        if (roomData == null) return false;
        if (roomData.roomLevel >= 2) return false;
        if (progressionData.level < GetCurrentRoomUnlockLevel()) return false;
        if (currencyData.coins < GetCurrentRoomUpgradeCost()) return false;
        return true;
    }

    public void OnUpgradeRoomButton()
    {
        if (!CanUpgradeRoom()) return;

        int cost = GetCurrentRoomUpgradeCost();
        if (currencySystem.SpendCoins(cost))
        {
            roomData.roomLevel++;
            if (roomVisualController != null) roomVisualController.ApplyRoom(roomData);
            
            OnCoinsChanged?.Invoke();
            SaveGame();
            
            Debug.Log($"Room upgraded to level {roomData.roomLevel}");
        }
    }

    // ── Mission helpers ──────────────────────────────────────────────────

    private MissionData CreateDefaultMissionData()
    {
        return new MissionData
        {
            missions = new List<MissionEntryData>
            {
                new MissionEntryData { missionId = "feed_1",  currentProgress = 0, targetProgress = 1, rewardCoins = 10,  isCompleted = false, isClaimed = false },
                new MissionEntryData { missionId = "work_3",  currentProgress = 0, targetProgress = 3, rewardCoins = 15,  isCompleted = false, isClaimed = false },
                new MissionEntryData { missionId = "focus_1", currentProgress = 0, targetProgress = 1, rewardCoins = 20,  isCompleted = false, isClaimed = false }
            }
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

    private void IncrementMissionProgress(string missionId, int amount = 1)
    {
        var mission = GetMission(missionId);
        if (mission == null || mission.isClaimed) return;
        if (mission.isCompleted) return;

        mission.currentProgress += amount;
        RefreshMissionCompletion(mission);
    }

    public void OnClaimMissionButton(string missionId)
    {
        var mission = GetMission(missionId);
        if (mission == null) return;
        if (!mission.isCompleted) return;
        if (mission.isClaimed) return;

        currencySystem.AddCoins(mission.rewardCoins);
        mission.isClaimed = true;

        OnCoinsChanged?.Invoke();
        SaveGame();
        UpdateMissionUI();

        Debug.Log($"Mission {missionId} claimed: +{mission.rewardCoins} coins");
    }

    // Convenience wrappers for Unity UI Button OnClick (no string param support on all versions)
    public void OnClaimFeedMission()  { OnClaimMissionButton("feed_1"); }
    public void OnClaimWorkMission()  { OnClaimMissionButton("work_3"); }
    public void OnClaimFocusMission() { OnClaimMissionButton("focus_1"); }

    private void UpdateMissionUI()
    {
        UpdateMissionEntry("feed_1",  missionFeedText,  claimFeedButton,  "Feed pet");
        UpdateMissionEntry("work_3",  missionWorkText,  claimWorkButton,  "Work");
        UpdateMissionEntry("focus_1", missionFocusText, claimFocusButton, "Focus");
    }

    private void UpdateMissionEntry(string missionId, TextMeshProUGUI label, Button claimBtn, string displayName)
    {
        var mission = GetMission(missionId);
        if (mission == null) return;

        if (label != null)
        {
            string status = mission.isClaimed ? " ✓" : (mission.isCompleted ? " (Ready!)" : "");
            label.text = $"{displayName}: {mission.currentProgress}/{mission.targetProgress}{status}";
        }

        if (claimBtn != null)
            claimBtn.interactable = mission.isCompleted && !mission.isClaimed;
    }

    // ── Daily Reward helpers ──────────────────────────────────────────────

    private DailyRewardData CreateDefaultDailyRewardData()
    {
        return new DailyRewardData { lastClaimDate = string.Empty };
    }

    private string GetTodayDateString()
    {
        return DateTime.Now.ToString("yyyy-MM-dd");
    }

    private bool CanClaimDailyReward()
    {
        if (dailyRewardData == null) return false;
        return dailyRewardData.lastClaimDate != GetTodayDateString();
    }

    public void OnClaimDailyRewardButton()
    {
        if (!CanClaimDailyReward()) return;

        currencySystem.AddCoins(balanceConfig.dailyRewardCoins);
        dailyRewardData.lastClaimDate = GetTodayDateString();

        OnCoinsChanged?.Invoke();
        SaveGame();
        UpdateDailyRewardUI();

        Debug.Log($"Daily reward claimed: +{balanceConfig.dailyRewardCoins} coins");
    }

    private void UpdateDailyRewardUI()
    {
        bool canClaim = CanClaimDailyReward();

        if (dailyRewardStatusText != null)
            dailyRewardStatusText.text = canClaim ? "Daily reward available" : "Daily reward claimed today";

        if (claimDailyRewardButton != null)
            claimDailyRewardButton.interactable = canClaim;
    }

    // ── Save ─────────────────────────────────────────────────────────────

    public void SaveGame()
    {
        SaveData data = new SaveData
        {
            petData = petData,
            currencyData = currencyData,
            inventoryData = inventoryData,
            progressionData = progressionData,
            roomData = roomData,
            missionData = missionData,
            dailyRewardData = dailyRewardData
        };

        saveManager.Save(data);
    }
}
