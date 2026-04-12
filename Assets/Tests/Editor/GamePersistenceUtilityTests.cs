using NUnit.Framework;

public class GamePersistenceUtilityTests
{
    [Test]
    public void CreateBootstrapState_WithoutSave_UsesConfiguredDefaults()
    {
        BalanceConfig balanceConfig = new BalanceConfig
        {
            startingHunger = 75f,
            startingMood = 64f,
            startingEnergy = 58f,
            startingCoins = 42,
            startingLevel = 3,
            startingXp = 11
        };

        GameBootstrapStateData state = GamePersistenceUtility.CreateBootstrapState(balanceConfig, null);

        Assert.AreEqual(75f, state.PetData.hunger);
        Assert.AreEqual(64f, state.PetData.mood);
        Assert.AreEqual(58f, state.PetData.energy);
        Assert.AreEqual(42, state.CurrencyData.coins);
        Assert.AreEqual(3, state.ProgressionData.level);
        Assert.AreEqual(11, state.ProgressionData.xp);
        Assert.NotNull(state.MissionData);
        Assert.NotNull(state.SkillsData);
        Assert.NotNull(state.OnboardingData);
        Assert.NotNull(state.IdleData);
    }

    [Test]
    public void ApplyPendingOfflineProgress_ClearsPendingSecondsAfterApplying()
    {
        double pendingOfflineSeconds = 120d;
        PetData petData = new PetData
        {
            hunger = 100f,
            mood = 100f,
            energy = 100f,
            hasIndependentStats = true,
            statusText = "Happy"
        };
        BalanceConfig balanceConfig = new BalanceConfig
        {
            hungerDrainPerSecond = 0.5f,
            lowHungerMoodThreshold = 30f,
            moodDecayPerSecondWhenHungry = 0.25f
        };

        bool changed = GamePersistenceUtility.ApplyPendingOfflineProgress(
            ref pendingOfflineSeconds,
            petData,
            new PetSystem(petData),
            null,
            balanceConfig,
            null);

        Assert.IsTrue(changed);
        Assert.AreEqual(0d, pendingOfflineSeconds);
        Assert.Less(petData.hunger, 100f);
    }

    [Test]
    public void CreateSaveDataSnapshot_UsesFallbackSkillsAndNormalizedResetBucket()
    {
        SaveData snapshot = GamePersistenceUtility.CreateSaveDataSnapshot(
            new PetData(),
            new CurrencyData { coins = 7 },
            new InventoryData(),
            new ProgressionData { level = 2, xp = 3 },
            null,
            new RoomData(),
            new MissionData(),
            new DailyRewardData(),
            new OnboardingData(),
            new FocusStateData(),
            new IdleData(),
            null,
            20260412);

        Assert.AreEqual(SaveNormalizer.CurrentSaveVersion, snapshot.saveVersion);
        Assert.NotNull(snapshot.skillsData);
        Assert.NotNull(snapshot.skillsData.skills);
        Assert.NotNull(snapshot.idleData);
        Assert.AreEqual(20260412, snapshot.lastResetBucket);
        Assert.IsFalse(string.IsNullOrEmpty(snapshot.lastSeenUtc));
    }
}
