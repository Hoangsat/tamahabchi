using UnityEngine;

[CreateAssetMenu(fileName = "BalanceConfig", menuName = "Tamahabchi/Balance Config")]
public class BalanceConfig : ScriptableObject
{
    [Header("Pet")]
    public float startingHunger = 70f;
    public float startingMood = 70f;
    public float startingEnergy = 100f;
    public float hungerDrainPerSecond = 0.001f;
    public float lowHungerMoodThreshold = 30f;
    public float lowEnergyMoodThreshold = 20f;
    public float moodDecayPerSecondWhenHungry = 0.01f;
    public float moodDecayPerSecondWhenTired = 0.015f;
    public float feedMoodBonus = 5f;
    public float roomUpgradeMoodBonus = 8f;
    public int feedAmount = 10;
    public float snackRestore = 15f;
    public float mealRestore = 40f;
    public float premiumRestore = 80f;

    [Header("Currency")]
    public int startingCoins = 50;
    public int foodPrice = 10;
    public int snackPrice = 5;
    public int mealPrice = 20;
    public int premiumPrice = 50;

    [Header("Rewards")]
    public int baseFocusReward = 10;
    public float baseFocusDuration = 10f;
    public float focusEnergyCostPerMinute = 0f;
    public float focusEnergyRewardPerMinute = 0.35f;
    public float focusMoodRewardPerMinute = 0.25f;
    public float lowEnergyThreshold = 20f;
    public float lowEnergyRewardMultiplier = 0.8f;

    [Header("Skills")]
    public float skillProgressPerMinute = 0.2f;
    public float skillMinutesPerStep = 5f;
    public float skillLevelMultiplierStep = 0.02f;
    public float skillMoodBaseBonus = 0.5f;
    public float skillMoodScale = 0.01f;
    public float goldenSkillFocusXpBonus = 0.05f;

    [Header("Progression")]
    public int startingLevel = 1;
    public int startingXp = 0;
    public int xpToNextLevel = 10;
    
    [Header("XP Gains")]

    [Header("Unlocks")]
    [Header("Room Upgrades")]
    public int roomUpgrade1Cost = 25;
    public int roomUpgrade2Cost = 50;
    public int roomUpgrade3Cost = 150;

    public int roomUpgrade1UnlockLevel = 2;
    public int roomUpgrade2UnlockLevel = 4;
    public int roomUpgrade3UnlockLevel = 7;

    [Header("Daily Reward")]
    public int dailyRewardCoins = 20;

    [Header("Offline Progress")]
    public float offlineHungerCapHours = 8f;

    [Header("Battle")]
    public BossDefinitionData[] bossDefinitions = new BossDefinitionData[0];
}
