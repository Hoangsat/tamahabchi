using System;

[Serializable]
public class RoomPanelStateData
{
    public int currentLevel;
    public int maxLevel;
    public int activeVisualLevel;
    public int currentUpgradeCost;
    public int currentUnlockLevel;
    public bool canUpgradeNow;
    public bool isMaxLevel;
    public string blockedReason = string.Empty;
    public string currentVisualStateLabel = string.Empty;
    public string nextVisualStateLabel = string.Empty;
    public string currentBonusSummary = string.Empty;
    public string nextBonusSummary = string.Empty;
    public string footerNote = "Customization coming later.";
}
