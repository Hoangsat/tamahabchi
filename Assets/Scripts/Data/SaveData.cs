using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int saveVersion;
    public PetData petData;
    public CurrencyData currencyData;
    public InventoryData inventoryData;
    public ProgressionData progressionData;
    public SkillsData skillsData;
    public RoomData roomData;
    public MissionData missionData;
    public DailyRewardData dailyRewardData;
    public OnboardingData onboardingData;
    public FocusStateData focusStateData;
    public IdleData idleData;
    public string lastSeenUtc;
    public int lastResetBucket;
}

[System.Serializable]
public class OnboardingData
{
    public bool isCompleted;
    public bool didWork;
    public bool didBuyFood;
    public bool didFeed;
    public bool didFocus;
}
