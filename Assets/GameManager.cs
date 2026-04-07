using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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
    private PetSystem petSystem;
    private CurrencySystem currencySystem;
    private FocusSystem focusSystem;
    private InventorySystem inventorySystem;
    private ShopSystem shopSystem;
    private ProgressionSystem progressionSystem;
    private SaveManager saveManager = new SaveManager();
    public Button workButton;
    public Button feedButton;
    public Button buyButton;
    public Button focusButton;
    public TextMeshProUGUI focusTimerText;
    public TextMeshProUGUI workButtonText;
    public TextMeshProUGUI focusButtonText;

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
        NotifyAllUI();
        UpdateUI();
    }

    private void InitializeDefaultState()
    {
        petData = new PetData();
        currencyData = new CurrencyData();
        inventoryData = new InventoryData();
        progressionData = new ProgressionData();

        petData.hunger = balanceConfig.startingHunger;
        currencyData.coins = balanceConfig.startingCoins;
        progressionData.level = balanceConfig.startingLevel;
        progressionData.xp = balanceConfig.startingXp;
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

            Debug.Log("Fed pet");
            SaveGame();
        }
        else
        {
            Debug.Log("No food in inventory");
        }

        UpdateUI();
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

        Debug.Log("Focus completed, reward: " + focusReward);
        SaveGame();
    }

    public void OnWorkButton()
    {
        if (petData.isDead) return;

        int workReward = progressionSystem.GetWorkReward(balanceConfig.baseWorkReward);
        currencySystem.AddCoins(workReward);
        OnCoinsChanged?.Invoke();
        AddXP(balanceConfig.workXpGain);
        
        Debug.Log("Worked and earned " + workReward + " coins");
        SaveGame();

        UpdateUI();
    }

    void UpdateUI()
    {

        if (feedButton != null)
            feedButton.interactable = inventorySystem.HasFood(1) && !petData.isDead;

        if (buyButton != null)
            buyButton.interactable = currencyData.coins >= balanceConfig.foodPrice && progressionSystem.IsBuyUnlocked() && !petData.isDead;

        if (workButton != null)
            workButton.interactable = true;

        if (focusButton != null)
            focusButton.interactable = !focusSystem.IsRunning && !petData.isDead;

        if (petData.isDead)
        {
            if (workButton != null) workButton.interactable = false;
            if (feedButton != null) feedButton.interactable = false;
            if (buyButton != null) buyButton.interactable = false;
            if (focusButton != null) focusButton.interactable = false;
        }

        if (focusTimerText != null)
        {
            if (petData.isDead)
                focusTimerText.text = "Focus: Dead";
            else if (focusSystem.IsRunning)
                focusTimerText.text = "Focus: " + Mathf.CeilToInt(focusSystem.GetRemainingTime()) + "s";
            else
                focusTimerText.text = "Focus: Ready";
        }

        int workReward = progressionSystem.GetWorkReward(balanceConfig.baseWorkReward);
        int focusReward = progressionSystem.GetFocusReward(balanceConfig.baseFocusReward);

        if (workButtonText != null)
            workButtonText.text = "Work (+" + workReward + ")";

        if (focusButtonText != null)
            focusButtonText.text = "Focus (+" + focusReward + ")";
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

        SaveGame();
        NotifyAllUI();
        UpdateUI();

        Debug.Log("Save reset");
    }

    public void SaveGame()
    {
        SaveData data = new SaveData
        {
            petData = petData,
            currencyData = currencyData,
            inventoryData = inventoryData,
            progressionData = progressionData
        };

        saveManager.Save(data);
    }
}
