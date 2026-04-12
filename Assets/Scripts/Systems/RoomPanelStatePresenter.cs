using UnityEngine;

public static class RoomPanelStatePresenter
{
    public static RoomPanelStateData Build(BalanceConfig balanceConfig, int currentLevel, int maxSupportedRoomLevel, int currentCoins, int currentProgressionLevel)
    {
        int clampedLevel = Mathf.Clamp(currentLevel, 0, maxSupportedRoomLevel);
        int nextLevel = Mathf.Clamp(clampedLevel + 1, 0, maxSupportedRoomLevel);
        bool isMaxLevel = clampedLevel >= maxSupportedRoomLevel;
        string currentVisualLabel = GetRoomVisualLabel(clampedLevel, maxSupportedRoomLevel);
        string nextVisualLabel = isMaxLevel ? currentVisualLabel : GetRoomVisualLabel(nextLevel, maxSupportedRoomLevel);
        int upgradeCost = GetRoomUpgradeCostForLevel(balanceConfig, clampedLevel, maxSupportedRoomLevel);
        string blockedReason = GetRoomUpgradeBlockedReason(upgradeCost, isMaxLevel, currentCoins);

        return new RoomPanelStateData
        {
            currentLevel = clampedLevel,
            maxLevel = maxSupportedRoomLevel,
            activeVisualLevel = clampedLevel,
            currentUpgradeCost = isMaxLevel ? 0 : upgradeCost,
            currentUnlockLevel = 0,
            canUpgradeNow = !isMaxLevel && string.IsNullOrEmpty(blockedReason),
            isMaxLevel = isMaxLevel,
            blockedReason = blockedReason,
            currentVisualStateLabel = currentVisualLabel,
            nextVisualStateLabel = nextVisualLabel,
            currentBonusSummary = GetCurrentRoomBonusSummary(balanceConfig, clampedLevel, maxSupportedRoomLevel),
            nextBonusSummary = GetNextRoomBonusSummary(balanceConfig, nextLevel, isMaxLevel, maxSupportedRoomLevel),
            footerNote = GetRoomFooterNote(balanceConfig, nextLevel, upgradeCost, isMaxLevel, currentCoins, maxSupportedRoomLevel)
        };
    }

    private static string GetCurrentRoomBonusSummary(BalanceConfig balanceConfig, int currentLevel, int maxSupportedRoomLevel)
    {
        int moodBonus = GetRoundedMoodBonus(balanceConfig);
        if (currentLevel <= 0)
        {
            return "Current room: starter setup with no room upgrade bonus applied yet.";
        }

        return $"Current room: {GetRoomVisualLabel(currentLevel, maxSupportedRoomLevel)}. Each completed room upgrade grants +{moodBonus} Mood immediately.";
    }

    private static string GetNextRoomBonusSummary(BalanceConfig balanceConfig, int nextLevel, bool isMaxLevel, int maxSupportedRoomLevel)
    {
        if (isMaxLevel)
        {
            return "Max level reached. This room already shows the highest shipped visual state.";
        }

        int moodBonus = GetRoundedMoodBonus(balanceConfig);
        return $"Next upgrade unlocks {GetRoomVisualLabel(nextLevel, maxSupportedRoomLevel)} and grants +{moodBonus} Mood right away.";
    }

    private static string GetRoomFooterNote(BalanceConfig balanceConfig, int nextLevel, int upgradeCost, bool isMaxLevel, int currentCoins, int maxSupportedRoomLevel)
    {
        if (isMaxLevel)
        {
            return "Room v1 is complete here. More cosmetic slots and passive room bonuses can layer on top later.";
        }

        int moodBonus = GetRoundedMoodBonus(balanceConfig);
        if (currentCoins < upgradeCost)
        {
            return $"Save {upgradeCost - currentCoins} more coins to unlock {GetRoomVisualLabel(nextLevel, maxSupportedRoomLevel)}. The upgrade grants +{moodBonus} Mood instantly.";
        }

        return $"Upgrade now to unlock {GetRoomVisualLabel(nextLevel, maxSupportedRoomLevel)} and gain +{moodBonus} Mood instantly.";
    }

    private static string GetRoomUpgradeBlockedReason(int upgradeCost, bool isMaxLevel, int currentCoins)
    {
        if (isMaxLevel)
        {
            return "Room is max level";
        }

        if (currentCoins < upgradeCost)
        {
            return $"Need {upgradeCost} coins";
        }

        return string.Empty;
    }

    private static int GetRoomUpgradeCostForLevel(BalanceConfig balanceConfig, int roomLevel, int maxSupportedRoomLevel)
    {
        if (balanceConfig == null)
        {
            return 0;
        }

        switch (roomLevel)
        {
            case 0:
                return balanceConfig.roomUpgrade1Cost;
            case 1:
                return balanceConfig.roomUpgrade2Cost;
            case 2:
                return maxSupportedRoomLevel > 2 ? balanceConfig.roomUpgrade3Cost : 0;
            default:
                return 0;
        }
    }

    private static string GetRoomVisualLabel(int roomLevel, int maxSupportedRoomLevel)
    {
        switch (Mathf.Clamp(roomLevel, 0, maxSupportedRoomLevel))
        {
            case 0:
                return "Starter Room";
            case 1:
                return "Cozy Room";
            case 2:
                return "Dream Room";
            default:
                return "Room";
        }
    }

    private static int GetRoundedMoodBonus(BalanceConfig balanceConfig)
    {
        return balanceConfig != null ? Mathf.RoundToInt(balanceConfig.roomUpgradeMoodBonus) : 0;
    }
}
