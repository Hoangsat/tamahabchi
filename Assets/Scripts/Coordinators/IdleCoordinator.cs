using System;
using System.Collections.Generic;

public readonly struct IdleHomeView
{
    public IdleHomeView(string iconId, string actionText, int pendingCount, string summaryText, bool hasClaimableEvents)
    {
        IconId = iconId ?? string.Empty;
        ActionText = actionText ?? string.Empty;
        PendingCount = pendingCount;
        SummaryText = summaryText ?? string.Empty;
        HasClaimableEvents = hasClaimableEvents;
    }

    public string IconId { get; }
    public string ActionText { get; }
    public int PendingCount { get; }
    public string SummaryText { get; }
    public bool HasClaimableEvents { get; }
}

public readonly struct IdleClaimResult
{
    public IdleClaimResult(bool success, int claimedEventCount, int coinsGranted, int itemsGranted, int skinsGranted, int momentsLogged, string message)
    {
        Success = success;
        ClaimedEventCount = claimedEventCount;
        CoinsGranted = coinsGranted;
        ItemsGranted = itemsGranted;
        SkinsGranted = skinsGranted;
        MomentsLogged = momentsLogged;
        Message = message ?? string.Empty;
    }

    public bool Success { get; }
    public int ClaimedEventCount { get; }
    public int CoinsGranted { get; }
    public int ItemsGranted { get; }
    public int SkinsGranted { get; }
    public int MomentsLogged { get; }
    public string Message { get; }
}

public sealed class IdleCoordinator
{
    private readonly IdleBehaviorSystem idleBehaviorSystem;
    private readonly SkillsSystem skillsSystem;
    private readonly PetSystem petSystem;
    private readonly CurrencySystem currencySystem;
    private readonly InventorySystem inventorySystem;
    private readonly RoomData roomData;
    private readonly BalanceConfig balanceConfig;

    public IdleCoordinator(
        IdleData idleData,
        SkillsSystem skillsSystem,
        PetSystem petSystem,
        CurrencySystem currencySystem,
        InventorySystem inventorySystem,
        RoomData roomData,
        BalanceConfig balanceConfig,
        IIdleRandomSource randomSource = null)
    {
        idleBehaviorSystem = new IdleBehaviorSystem(idleData, randomSource);
        this.skillsSystem = skillsSystem;
        this.petSystem = petSystem;
        this.currencySystem = currencySystem;
        this.inventorySystem = inventorySystem;
        this.roomData = roomData;
        this.balanceConfig = balanceConfig;
    }

    public IdleData Data => idleBehaviorSystem.Data;

    public IdleRuntimeUpdate Tick(DateTime nowUtc)
    {
        return idleBehaviorSystem.Tick(GetSkillViews(), IsRewardsBlocked(), roomData, nowUtc);
    }

    public IdleRuntimeUpdate ApplyOffline(double elapsedSeconds, DateTime nowUtc)
    {
        return idleBehaviorSystem.ApplyOffline(elapsedSeconds, GetSkillViews(), IsRewardsBlocked(), roomData, nowUtc);
    }

    public IdleHomeView GetHomeView()
    {
        bool rewardsBlocked = IsRewardsBlocked();
        int pendingCount = Data.pendingEvents != null ? Data.pendingEvents.Count : 0;
        return new IdleHomeView(
            idleBehaviorSystem.GetCurrentIconId(),
            idleBehaviorSystem.GetCurrentActionLabel(),
            pendingCount,
            idleBehaviorSystem.GetLatestSummary(rewardsBlocked),
            pendingCount > 0);
    }

    public int GetPendingEventCount()
    {
        return Data.pendingEvents != null ? Data.pendingEvents.Count : 0;
    }

    public IdleClaimResult ClaimPendingEvents()
    {
        List<IdleEventEntryData> pendingEvents = Data.pendingEvents;
        if (pendingEvents == null || pendingEvents.Count == 0)
        {
            return new IdleClaimResult(false, 0, 0, 0, 0, 0, "Нечего забирать");
        }

        List<IdleEventEntryData> snapshot = new List<IdleEventEntryData>(pendingEvents);
        int coinsGranted = 0;
        int itemsGranted = 0;
        int skinsGranted = 0;
        int momentsLogged = 0;

        for (int i = 0; i < snapshot.Count; i++)
        {
            IdleEventEntryData entry = snapshot[i];
            if (entry == null)
            {
                continue;
            }

            if (entry.coins > 0 && currencySystem != null)
            {
                currencySystem.AddCoins(entry.coins);
                coinsGranted += entry.coins;
            }

            if (!string.IsNullOrWhiteSpace(entry.momentId))
            {
                if (!Data.collectedMomentIds.Contains(entry.momentId))
                {
                    Data.collectedMomentIds.Add(entry.momentId);
                    momentsLogged++;
                }
                else if (currencySystem != null)
                {
                    currencySystem.AddCoins(10);
                    coinsGranted += 10;
                }
            }

            if (!string.IsNullOrWhiteSpace(entry.skinId))
            {
                if (inventorySystem != null && IdleBehaviorSystem.IsKnownSkinId(entry.skinId))
                {
                    if (!inventorySystem.HasSkin(entry.skinId))
                    {
                        inventorySystem.AddSkin(entry.skinId);
                        skinsGranted++;
                    }
                    else
                    {
                        GrantFallbackItem(ref itemsGranted, "food_premium");
                    }
                }
                else
                {
                    GrantFallbackCoins(ref coinsGranted, 10);
                }
            }

            if (!string.IsNullOrWhiteSpace(entry.itemId))
            {
                if (inventorySystem != null && IdleBehaviorSystem.IsKnownItemId(entry.itemId))
                {
                    inventorySystem.AddItem(entry.itemId, 1);
                    itemsGranted++;
                }
                else
                {
                    GrantFallbackCoins(ref coinsGranted, 10);
                }
            }
        }

        pendingEvents.Clear();
        string message = snapshot.Count == 1
            ? "Награда питомца получена"
            : $"Награды питомца получены: {snapshot.Count}";

        return new IdleClaimResult(true, snapshot.Count, coinsGranted, itemsGranted, skinsGranted, momentsLogged, message);
    }

    private List<SkillProgressionViewData> GetSkillViews()
    {
        return skillsSystem != null
            ? skillsSystem.GetSkillProgressionViews()
            : new List<SkillProgressionViewData>();
    }

    private bool IsRewardsBlocked()
    {
        if (petSystem == null)
        {
            return false;
        }

        if (balanceConfig == null)
        {
            return petSystem.IsNeglected();
        }

        PetStatusSummary summary = petSystem.GetStatusSummary(
            balanceConfig.lowHungerMoodThreshold,
            balanceConfig.lowEnergyMoodThreshold);
        return summary != null &&
               (summary.flowState == PetFlowState.Critical || summary.flowState == PetFlowState.Neglected);
    }

    private void GrantFallbackCoins(ref int coinsGranted, int amount)
    {
        if (currencySystem == null || amount <= 0)
        {
            return;
        }

        currencySystem.AddCoins(amount);
        coinsGranted += amount;
    }

    private void GrantFallbackItem(ref int itemsGranted, string itemId)
    {
        if (inventorySystem == null || !IdleBehaviorSystem.IsKnownItemId(itemId))
        {
            return;
        }

        inventorySystem.AddItem(itemId, 1);
        itemsGranted++;
    }
}
