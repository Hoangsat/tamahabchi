using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class GameBootstrapStateData
{
    public PetData PetData;
    public CurrencyData CurrencyData;
    public InventoryData InventoryData;
    public ProgressionData ProgressionData;
    public SkillsData SkillsData;
    public RoomData RoomData;
    public MissionData MissionData;
    public DailyRewardData DailyRewardData;
    public OnboardingData OnboardingData;
    public FocusStateData FocusStateData;
    public IdleData IdleData;
    public double PendingOfflineSeconds;
    public int ActiveResetBucket;
    public int LastAppliedResetBucket;
    public string LastSeenUtc = string.Empty;
}

public static class GamePersistenceUtility
{
    public static GameBootstrapStateData CreateBootstrapState(BalanceConfig balanceConfig, SaveData loadedSave)
    {
        GameBootstrapStateData state = new GameBootstrapStateData
        {
            PendingOfflineSeconds = 0d,
            ActiveResetBucket = TimeService.GetCurrentResetBucketLocal(),
            LastAppliedResetBucket = 0,
            LastSeenUtc = string.Empty
        };

        if (loadedSave != null)
        {
            SaveData normalized = SaveNormalizer.Normalize(loadedSave);
            state.PetData = normalized.petData;
            state.CurrencyData = normalized.currencyData;
            state.InventoryData = normalized.inventoryData;
            state.ProgressionData = normalized.progressionData;
            state.SkillsData = normalized.skillsData;
            state.RoomData = normalized.roomData;
            state.MissionData = normalized.missionData;
            state.DailyRewardData = normalized.dailyRewardData;
            state.OnboardingData = normalized.onboardingData;
            state.FocusStateData = normalized.focusStateData ?? new FocusStateData();
            state.IdleData = normalized.idleData ?? new IdleData();
            state.LastAppliedResetBucket = normalized.lastResetBucket;
            state.PendingOfflineSeconds = GetOfflineElapsedSeconds(balanceConfig, normalized.lastSeenUtc);
            state.LastSeenUtc = normalized.lastSeenUtc ?? string.Empty;
            return state;
        }

        state.PetData = CreateDefaultPetData(balanceConfig);
        state.CurrencyData = new CurrencyData
        {
            coins = balanceConfig != null ? balanceConfig.startingCoins : 0
        };
        state.InventoryData = new InventoryData();
        state.ProgressionData = new ProgressionData
        {
            level = Mathf.Max(1, balanceConfig != null ? balanceConfig.startingLevel : 1),
            xp = Mathf.Max(0, balanceConfig != null ? balanceConfig.startingXp : 0)
        };
        state.SkillsData = CreateDefaultSkillsData();
        state.RoomData = new RoomData { roomLevel = 0 };
        state.MissionData = CreateDefaultMissionData();
        state.DailyRewardData = CreateDefaultDailyRewardData();
        state.OnboardingData = CreateDefaultOnboardingData();
        state.FocusStateData = new FocusStateData();
        state.IdleData = new IdleData();
        return state;
    }

    public static void NormalizeRuntimeState(BalanceConfig balanceConfig, ref PetData petData, ref RoomData roomData, ref ProgressionData progressionData, int maxSupportedRoomLevel)
    {
        NormalizePetState(balanceConfig, ref petData);
        NormalizeRoomState(ref roomData, maxSupportedRoomLevel);
        NormalizeProgressionState(ref progressionData);
    }

    public static double GetOfflineElapsedSeconds(BalanceConfig balanceConfig, string lastSeenUtc)
    {
        double maxSeconds = balanceConfig != null ? Mathf.Max(0f, balanceConfig.offlineHungerCapHours * 3600f) : 0d;
        return TimeService.GetOfflineElapsedSeconds(lastSeenUtc, maxSeconds);
    }

    public static bool ApplyOfflineProgress(PetData petData, PetSystem petSystem, SkillDecaySystem skillDecaySystem, BalanceConfig balanceConfig, double offlineSeconds, Action<string> logLifecycle)
    {
        if (offlineSeconds <= 0d || petData == null || petSystem == null || balanceConfig == null)
        {
            return false;
        }

        float neglectSecondsAccrued;
        bool changed = petSystem.ApplyOfflineProgress(
            (float)offlineSeconds,
            balanceConfig.hungerDrainPerSecond,
            balanceConfig.lowHungerMoodThreshold,
            balanceConfig.moodDecayPerSecondWhenHungry,
            out neglectSecondsAccrued);

        if (neglectSecondsAccrued > 0f && skillDecaySystem != null)
        {
            changed |= skillDecaySystem.ApplyNeglectDecay(neglectSecondsAccrued, ref petData.neglectDecayCarrySeconds);
        }

        if (changed)
        {
            logLifecycle?.Invoke($"Offline: {offlineSeconds:F0}s elapsed, pet state normalized");
        }

        return changed;
    }

    public static bool ApplyPendingOfflineProgress(ref double pendingOfflineSeconds, PetData petData, PetSystem petSystem, SkillDecaySystem skillDecaySystem, BalanceConfig balanceConfig, Action<string> logLifecycle)
    {
        if (pendingOfflineSeconds <= 0d)
        {
            return false;
        }

        bool changed = ApplyOfflineProgress(petData, petSystem, skillDecaySystem, balanceConfig, pendingOfflineSeconds, logLifecycle);
        pendingOfflineSeconds = 0d;
        return changed;
    }

    public static bool ApplyDailyResetWindowIfNeeded(MissionSystem missionSystem, SkillsSystem skillsSystem, MissionData missionData, ref int lastAppliedResetBucket, ref int activeResetBucket, string reason, Action<string> logLifecycle)
    {
        if (missionSystem == null || skillsSystem == null)
        {
            return false;
        }

        int previousResetBucket = TimeService.NormalizeResetBucket(lastAppliedResetBucket);
        int effectiveResetBucket = GetCurrentResetBucketForSystems(lastAppliedResetBucket, out activeResetBucket);
        bool hasPreviousBucket = previousResetBucket > 0;
        bool shouldRunReset = hasPreviousBucket && TimeService.ShouldRunDailyReset(previousResetBucket, effectiveResetBucket);
        bool establishedBaseline = !hasPreviousBucket && effectiveResetBucket > 0;
        int previousMissionCount = missionData != null && missionData.missions != null ? missionData.missions.Count : 0;
        string previousMissionResetKey = missionData != null ? missionData.lastDailyResetKey ?? string.Empty : string.Empty;

        if (establishedBaseline)
        {
            lastAppliedResetBucket = effectiveResetBucket;
        }

        missionSystem.EnsureDailySkillMissions(skillsSystem.GetSkills(), effectiveResetBucket);

        if (shouldRunReset)
        {
            logLifecycle?.Invoke($"Daily reset triggered ({reason}): {previousResetBucket} -> {effectiveResetBucket}");
            lastAppliedResetBucket = effectiveResetBucket;
        }

        string currentMissionResetKey = missionData != null ? missionData.lastDailyResetKey ?? string.Empty : string.Empty;
        int currentMissionCount = missionData != null && missionData.missions != null ? missionData.missions.Count : 0;
        bool missionsGenerated = previousMissionCount == 0 && currentMissionCount > 0;
        bool missionResetKeyChanged = !string.Equals(previousMissionResetKey, currentMissionResetKey, StringComparison.Ordinal);

        return establishedBaseline || shouldRunReset || missionsGenerated || missionResetKeyChanged;
    }

    public static int GetCurrentResetBucketForSystems(int lastAppliedResetBucket, out int activeResetBucket)
    {
        int observedResetBucket = TimeService.GetCurrentResetBucketLocal();
        activeResetBucket = TimeService.GetEffectiveResetBucket(lastAppliedResetBucket, observedResetBucket);
        return activeResetBucket;
    }

    public static string BuildTimeBootstrapMessage(string lastSeenUtc, double pendingOfflineSeconds, int activeResetBucket, int lastAppliedResetBucket)
    {
        return
            $"Time bootstrap: lastSeenUtc={(string.IsNullOrEmpty(lastSeenUtc) ? "<empty>" : lastSeenUtc)}, " +
            $"elapsed={pendingOfflineSeconds:F0}s, currentBucket={activeResetBucket}, lastBucket={lastAppliedResetBucket}";
    }

    public static SaveData CreateSaveDataSnapshot(
        PetData petData,
        CurrencyData currencyData,
        InventoryData inventoryData,
        ProgressionData progressionData,
        SkillsData skillsData,
        RoomData roomData,
        MissionData missionData,
        DailyRewardData dailyRewardData,
        OnboardingData onboardingData,
        FocusStateData focusStateData,
        IdleData idleData,
        FocusCoordinator focusCoordinator,
        int lastAppliedResetBucket)
    {
        string snapshotUtc = TimeService.GetUtcNow().ToString("O");
        return new SaveData
        {
            saveVersion = SaveNormalizer.CurrentSaveVersion,
            petData = petData,
            currencyData = currencyData,
            inventoryData = inventoryData,
            progressionData = progressionData,
            skillsData = skillsData ?? CreateDefaultSkillsData(),
            roomData = roomData,
            missionData = missionData,
            dailyRewardData = dailyRewardData,
            onboardingData = onboardingData,
            focusStateData = focusCoordinator != null ? focusCoordinator.CreateSaveData(snapshotUtc) : focusStateData,
            idleData = idleData ?? new IdleData(),
            lastSeenUtc = snapshotUtc,
            lastResetBucket = TimeService.NormalizeResetBucket(lastAppliedResetBucket)
        };
    }

    public static MissionData CreateDefaultMissionData()
    {
        return new MissionData
        {
            lastDailyResetKey = string.Empty,
            missions = new List<MissionEntryData>()
        };
    }

    public static SkillsData CreateDefaultSkillsData()
    {
        return new SkillsData
        {
            skills = new List<SkillEntry>()
        };
    }

    public static DailyRewardData CreateDefaultDailyRewardData()
    {
        return new DailyRewardData { lastClaimDate = string.Empty };
    }

    public static OnboardingData CreateDefaultOnboardingData()
    {
        return new OnboardingData
        {
            isCompleted = false,
            didBuyFood = false,
            didFeed = false,
            didFocus = false
        };
    }

    private static PetData CreateDefaultPetData(BalanceConfig balanceConfig)
    {
        return new PetData
        {
            hunger = balanceConfig != null ? balanceConfig.startingHunger : 0f,
            mood = balanceConfig != null ? balanceConfig.startingMood : 0f,
            energy = balanceConfig != null ? balanceConfig.startingEnergy : 0f,
            hasIndependentStats = true,
            neglectDecayCarrySeconds = 0f
        };
    }

    private static void NormalizePetState(BalanceConfig balanceConfig, ref PetData petData)
    {
        if (petData == null)
        {
            petData = new PetData();
        }

        petData.hunger = Mathf.Clamp(petData.hunger, 0f, 100f);
        if (!petData.hasIndependentStats)
        {
            petData.mood = balanceConfig != null ? balanceConfig.startingMood : 0f;
            petData.energy = balanceConfig != null ? balanceConfig.startingEnergy : 0f;
            petData.hasIndependentStats = true;
        }
        else
        {
            petData.mood = Mathf.Clamp(petData.mood, 0f, 100f);
            petData.energy = Mathf.Clamp(petData.energy, 0f, 100f);
        }

        if (string.IsNullOrEmpty(petData.statusText))
        {
            petData.statusText = "Happy";
        }

        petData.neglectDecayCarrySeconds = Mathf.Max(0f, petData.neglectDecayCarrySeconds);
    }

    private static void NormalizeRoomState(ref RoomData roomData, int maxSupportedRoomLevel)
    {
        if (roomData == null)
        {
            roomData = new RoomData();
        }

        roomData.roomLevel = Mathf.Clamp(roomData.roomLevel, 0, maxSupportedRoomLevel);
    }

    private static void NormalizeProgressionState(ref ProgressionData progressionData)
    {
        if (progressionData == null)
        {
            progressionData = new ProgressionData();
        }

        progressionData.level = Mathf.Max(1, progressionData.level);
        progressionData.xp = Mathf.Max(0, progressionData.xp);
    }
}
