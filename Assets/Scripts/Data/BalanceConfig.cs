using UnityEngine;

[CreateAssetMenu(fileName = "BalanceConfig", menuName = "Tamahabchi/Balance Config")]
public class BalanceConfig : ScriptableObject
{
    [Header("Pet")]
    public float startingHunger = 50f;
    public float hungerDrainPerSecond = 5f;
    public int feedAmount = 10;

    [Header("Currency")]
    public int startingCoins = 0;
    public int foodPrice = 10;

    [Header("Rewards")]
    public int baseWorkReward = 3;
    public int baseFocusReward = 10;
    public float baseFocusDuration = 10f;

    [Header("Progression")]
    public int startingLevel = 1;
    public int startingXp = 0;
    public int xpToNextLevel = 10;
    
    [Header("XP Gains")]
    public int workXpGain = 1;
    public int focusXpGain = 5;
    public int buyXpGain = 3;
    public int feedXpGain = 2;

    [Header("Unlocks")]
    public int buyUnlockLevel = 2;
}
