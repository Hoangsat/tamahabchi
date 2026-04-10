using System;
using UnityEngine;

public sealed class CoreLoopActionCoordinatorCallbacks
{
    public Action OnCoinsChanged;
    public Action OnPetChanged;
    public Action OnInventoryChanged;
    public Action ShowMissionRefresh;
    public Action RefreshOnboardingCompletion;
    public Action UpdateOnboardingUi;
    public Action UpdateUi;
    public Action SaveGame;
    public Action<int, bool> AddXp;
    public Action<string> ShowFeedback;
    public Action<MissionClaimResult, bool> ApplyMissionRewards;
}

public sealed class CoreLoopActionCoordinator
{
    private readonly PetData petData;
    private readonly CurrencyData currencyData;
    private readonly ProgressionData progressionData;
    private readonly RoomData roomData;
    private readonly OnboardingData onboardingData;
    private readonly PetSystem petSystem;
    private readonly CurrencySystem currencySystem;
    private readonly InventorySystem inventorySystem;
    private readonly ShopSystem shopSystem;
    private readonly ProgressionSystem progressionSystem;
    private readonly MissionSystem missionSystem;
    private readonly BalanceConfig balanceConfig;
    private readonly CoreLoopActionCoordinatorCallbacks callbacks;
    private readonly int maxSupportedRoomLevel;

    public CoreLoopActionCoordinator(
        PetData petData,
        CurrencyData currencyData,
        ProgressionData progressionData,
        RoomData roomData,
        OnboardingData onboardingData,
        PetSystem petSystem,
        CurrencySystem currencySystem,
        InventorySystem inventorySystem,
        ShopSystem shopSystem,
        ProgressionSystem progressionSystem,
        MissionSystem missionSystem,
        BalanceConfig balanceConfig,
        int maxSupportedRoomLevel,
        CoreLoopActionCoordinatorCallbacks callbacks)
    {
        this.petData = petData;
        this.currencyData = currencyData;
        this.progressionData = progressionData;
        this.roomData = roomData;
        this.onboardingData = onboardingData;
        this.petSystem = petSystem;
        this.currencySystem = currencySystem;
        this.inventorySystem = inventorySystem;
        this.shopSystem = shopSystem;
        this.progressionSystem = progressionSystem;
        this.missionSystem = missionSystem;
        this.balanceConfig = balanceConfig;
        this.maxSupportedRoomLevel = maxSupportedRoomLevel;
        this.callbacks = callbacks ?? new CoreLoopActionCoordinatorCallbacks();
    }

    public ShopPurchaseResult TryFeedItem(string itemId, float amount)
    {
        if (petData == null || petData.isDead)
        {
            const string deadMessage = "Pet is dead";
            callbacks.ShowFeedback?.Invoke(deadMessage);
            return ShopPurchaseResult.Fail(deadMessage);
        }

        string itemName = FormatItemName(itemId);
        if (inventorySystem != null && inventorySystem.ConsumeItem(itemId, 1))
        {
            petSystem?.Feed(amount);
            petSystem?.AddMood(balanceConfig.feedMoodBonus);
            missionSystem?.RecordFeedAction();
            missionSystem?.IncrementGenericMissionProgress("generic_feed", 1);
            callbacks.ApplyMissionRewards?.Invoke(missionSystem?.CollectCompletedRoutineRewards(), false);
            callbacks.OnInventoryChanged?.Invoke();
            callbacks.OnPetChanged?.Invoke();
            callbacks.AddXp?.Invoke(balanceConfig.feedXpGain, true);

            if (onboardingData != null)
            {
                onboardingData.didFeed = true;
                callbacks.RefreshOnboardingCompletion?.Invoke();
                callbacks.UpdateOnboardingUi?.Invoke();
            }

            string successMessage = $"Fed {itemName}";
            callbacks.ShowFeedback?.Invoke(successMessage);
            callbacks.SaveGame?.Invoke();
            callbacks.UpdateUi?.Invoke();
            callbacks.ShowMissionRefresh?.Invoke();
            return ShopPurchaseResult.Success(successMessage);
        }

        string failureMessage = $"No {itemName}";
        callbacks.ShowFeedback?.Invoke(failureMessage);
        callbacks.UpdateUi?.Invoke();
        callbacks.ShowMissionRefresh?.Invoke();
        return ShopPurchaseResult.Fail(failureMessage);
    }

    public ShopPurchaseResult TryBuyItem(string itemId, int price)
    {
        if (petData == null || petData.isDead)
        {
            const string deadMessage = "Pet is dead";
            callbacks.ShowFeedback?.Invoke(deadMessage);
            return ShopPurchaseResult.Fail(deadMessage);
        }

        if (progressionSystem == null || !progressionSystem.IsBuyUnlocked())
        {
            string unlockMessage = $"Unlocks at level {balanceConfig.buyUnlockLevel}";
            callbacks.ShowFeedback?.Invoke(unlockMessage);
            callbacks.UpdateUi?.Invoke();
            return ShopPurchaseResult.Fail(unlockMessage);
        }

        string itemName = FormatItemName(itemId);
        if (shopSystem != null && shopSystem.BuyItem(itemId, price, 1))
        {
            callbacks.OnCoinsChanged?.Invoke();
            callbacks.OnInventoryChanged?.Invoke();
            callbacks.AddXp?.Invoke(balanceConfig.buyXpGain, true);

            if (onboardingData != null)
            {
                onboardingData.didBuyFood = true;
                callbacks.RefreshOnboardingCompletion?.Invoke();
                callbacks.UpdateOnboardingUi?.Invoke();
            }

            string successMessage = $"Bought {itemName}";
            callbacks.ShowFeedback?.Invoke(successMessage);
            callbacks.SaveGame?.Invoke();
            callbacks.UpdateUi?.Invoke();
            return ShopPurchaseResult.Success(successMessage);
        }

        const string failureMessage = "Not enough coins";
        callbacks.ShowFeedback?.Invoke(failureMessage);
        callbacks.UpdateUi?.Invoke();
        return ShopPurchaseResult.Fail(failureMessage);
    }

    public ShopPurchaseResult TryBuySkin(string itemId, int price)
    {
        if (petData == null || petData.isDead)
        {
            const string deadMessage = "Pet is dead";
            callbacks.ShowFeedback?.Invoke(deadMessage);
            return ShopPurchaseResult.Fail(deadMessage);
        }

        if (progressionSystem == null || !progressionSystem.IsBuyUnlocked())
        {
            string unlockMessage = $"Unlocks at level {balanceConfig.buyUnlockLevel}";
            callbacks.ShowFeedback?.Invoke(unlockMessage);
            callbacks.UpdateUi?.Invoke();
            return ShopPurchaseResult.Fail(unlockMessage);
        }

        string itemName = FormatItemName(itemId);
        if (shopSystem != null && shopSystem.BuySkin(itemId, price))
        {
            callbacks.OnCoinsChanged?.Invoke();
            callbacks.OnInventoryChanged?.Invoke();
            callbacks.AddXp?.Invoke(balanceConfig.buyXpGain, true);
            string successMessage = $"Bought {itemName}";
            callbacks.ShowFeedback?.Invoke(successMessage);
            callbacks.SaveGame?.Invoke();
            callbacks.UpdateUi?.Invoke();
            return ShopPurchaseResult.Success(successMessage);
        }

        const string failureMessage = "Already owned or not enough coins";
        callbacks.ShowFeedback?.Invoke(failureMessage);
        callbacks.UpdateUi?.Invoke();
        return ShopPurchaseResult.Fail(failureMessage);
    }

    public ShopPurchaseResult TryUseConsumableItem(string itemId, float amount, ShopCategory category)
    {
        if (petData == null || petData.isDead)
        {
            const string deadMessage = "Pet is dead";
            callbacks.ShowFeedback?.Invoke(deadMessage);
            return ShopPurchaseResult.Fail(deadMessage);
        }

        if (category == ShopCategory.Food)
        {
            return TryFeedItem(itemId, amount);
        }

        string itemName = FormatItemName(itemId);
        if (inventorySystem == null || !inventorySystem.ConsumeItem(itemId, 1))
        {
            string noStockMessage = $"No {itemName}";
            callbacks.ShowFeedback?.Invoke(noStockMessage);
            callbacks.UpdateUi?.Invoke();
            return ShopPurchaseResult.Fail(noStockMessage);
        }

        switch (category)
        {
            case ShopCategory.Energy:
                petSystem?.AddEnergy(amount);
                break;
            case ShopCategory.Mood:
                petSystem?.AddMood(amount);
                break;
            default:
                const string unsupportedMessage = "Item cannot be used";
                callbacks.ShowFeedback?.Invoke(unsupportedMessage);
                callbacks.UpdateUi?.Invoke();
                return ShopPurchaseResult.Fail(unsupportedMessage);
        }

        callbacks.OnInventoryChanged?.Invoke();
        callbacks.OnPetChanged?.Invoke();
        callbacks.UpdateUi?.Invoke();
        callbacks.SaveGame?.Invoke();

        string successMessage = $"Used {itemName}";
        callbacks.ShowFeedback?.Invoke(successMessage);
        return ShopPurchaseResult.Success(successMessage);
    }

    public void TryWork()
    {
        if (petData == null)
        {
            return;
        }

        int workReward = GetCurrentWorkReward();
        currencySystem?.AddCoins(workReward);
        missionSystem?.RecordWorkAction();
        missionSystem?.IncrementGenericMissionProgress("generic_work", 1);
        callbacks.ApplyMissionRewards?.Invoke(missionSystem?.CollectCompletedRoutineRewards(), false);
        callbacks.OnCoinsChanged?.Invoke();
        callbacks.AddXp?.Invoke(balanceConfig.workXpGain, false);

        if (onboardingData != null)
        {
            onboardingData.didWork = true;
            callbacks.RefreshOnboardingCompletion?.Invoke();
            callbacks.UpdateOnboardingUi?.Invoke();
        }

        callbacks.ShowFeedback?.Invoke($"+{workReward} Coins");
        callbacks.SaveGame?.Invoke();
        callbacks.UpdateUi?.Invoke();
        callbacks.ShowMissionRefresh?.Invoke();
    }

    public ShopPurchaseResult TryUpgradeRoom()
    {
        string blockedReason = GetRoomUpgradeBlockedReason();
        if (!string.IsNullOrEmpty(blockedReason))
        {
            callbacks.ShowFeedback?.Invoke(blockedReason);
            callbacks.UpdateUi?.Invoke();
            return ShopPurchaseResult.Fail(blockedReason);
        }

        int cost = GetCurrentRoomUpgradeCost();
        if (currencySystem == null || !currencySystem.SpendCoins(cost))
        {
            const string spendFailureMessage = "Need more coins";
            callbacks.ShowFeedback?.Invoke(spendFailureMessage);
            callbacks.UpdateUi?.Invoke();
            return ShopPurchaseResult.Fail(spendFailureMessage);
        }

        roomData.roomLevel++;
        petSystem?.AddMood(balanceConfig.roomUpgradeMoodBonus);
        callbacks.OnCoinsChanged?.Invoke();
        callbacks.OnPetChanged?.Invoke();
        callbacks.UpdateUi?.Invoke();
        callbacks.SaveGame?.Invoke();

        string successMessage = $"Room upgraded to level {roomData.roomLevel}";
        callbacks.ShowFeedback?.Invoke(successMessage);
        return ShopPurchaseResult.Success(successMessage);
    }

    public bool CanUpgradeRoom()
    {
        return string.IsNullOrEmpty(GetRoomUpgradeBlockedReason());
    }

    public int GetCurrentWorkReward()
    {
        return progressionSystem != null ? progressionSystem.GetWorkReward(balanceConfig.baseWorkReward) : 0;
    }

    public int GetCurrentRoomUnlockLevel()
    {
        if (roomData == null)
        {
            return 999;
        }

        if (roomData.roomLevel == 0) return balanceConfig.roomUpgrade1UnlockLevel;
        if (roomData.roomLevel == 1) return balanceConfig.roomUpgrade2UnlockLevel;
        if (roomData.roomLevel == 2 && maxSupportedRoomLevel > 2) return balanceConfig.roomUpgrade3UnlockLevel;
        return 999;
    }

    public int GetCurrentRoomUpgradeCost()
    {
        if (roomData == null)
        {
            return 0;
        }

        if (roomData.roomLevel == 0) return balanceConfig.roomUpgrade1Cost;
        if (roomData.roomLevel == 1) return balanceConfig.roomUpgrade2Cost;
        if (roomData.roomLevel == 2 && maxSupportedRoomLevel > 2) return balanceConfig.roomUpgrade3Cost;
        return 0;
    }

    public string GetRoomUpgradeBlockedReason()
    {
        if (petData == null || petData.isDead)
        {
            return "Pet is dead";
        }

        if (roomData == null)
        {
            return "Room unavailable";
        }

        if (roomData.roomLevel >= maxSupportedRoomLevel)
        {
            return "Room is max level";
        }

        int unlockLevel = GetCurrentRoomUnlockLevel();
        if (progressionData == null || progressionData.level < unlockLevel)
        {
            return $"Unlock at level {unlockLevel}";
        }

        int cost = GetCurrentRoomUpgradeCost();
        if (currencyData == null || currencyData.coins < cost)
        {
            return $"Need {cost} coins";
        }

        return string.Empty;
    }

    private string FormatItemName(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return string.Empty;
        }

        string[] parts = itemId.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return itemId;
        }

        int startIndex = 0;
        if (parts.Length > 1 && (parts[0] == "food" || parts[0] == "skin"))
        {
            startIndex = 1;
        }
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int i = startIndex; i < parts.Length; i++)
        {
            string part = parts[i];
            if (string.IsNullOrEmpty(part))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(char.ToUpper(part[0]));
            if (part.Length > 1)
            {
                builder.Append(part.Substring(1));
            }
        }

        return builder.Length > 0 ? builder.ToString() : itemId;
    }
}
