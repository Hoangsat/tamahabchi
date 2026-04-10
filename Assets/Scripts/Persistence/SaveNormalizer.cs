using System;
using System.Collections.Generic;
using UnityEngine;

public static class SaveNormalizer
{
    public const int CurrentSaveVersion = 3;

    public static SaveData CreateDefault()
    {
        return Normalize(new SaveData());
    }

    public static SaveData Normalize(SaveData data)
    {
        SaveData normalized = data ?? new SaveData();

        if (normalized.saveVersion <= 0)
        {
            normalized.saveVersion = CurrentSaveVersion;
        }

        normalized.petData ??= new PetData();
        normalized.currencyData ??= new CurrencyData();
        normalized.inventoryData ??= new InventoryData();
        normalized.progressionData ??= new ProgressionData();
        normalized.skillsData ??= new SkillsData();
        normalized.roomData ??= new RoomData();
        normalized.missionData ??= new MissionData();
        normalized.dailyRewardData ??= new DailyRewardData();
        normalized.onboardingData ??= new OnboardingData();
        normalized.focusStateData ??= new FocusStateData();

        NormalizePet(normalized.petData);
        NormalizeCurrency(normalized.currencyData);
        NormalizeInventory(normalized.inventoryData);
        NormalizeProgression(normalized.progressionData);
        NormalizeSkills(normalized.skillsData);
        NormalizeRoom(normalized.roomData);
        NormalizeMissions(normalized.missionData);
        NormalizeDailyReward(normalized.dailyRewardData);
        NormalizeFocusState(normalized.focusStateData);
        normalized.lastSeenUtc = NormalizeUtcTimestamp(normalized.lastSeenUtc);
        normalized.lastResetBucket = NormalizeResetBucket(normalized.lastResetBucket, normalized.missionData);
        normalized.saveVersion = CurrentSaveVersion;
        return normalized;
    }

    private static void NormalizePet(PetData petData)
    {
        petData.hunger = Mathf.Clamp(petData.hunger, 0f, 100f);
        petData.mood = Mathf.Clamp(petData.mood, 0f, 100f);
        petData.energy = Mathf.Clamp(petData.energy, 0f, 100f);
        petData.statusText = string.IsNullOrWhiteSpace(petData.statusText) ? "Happy" : petData.statusText.Trim();

        if (petData.hunger <= 0f)
        {
            petData.hunger = 0f;
            petData.isDead = true;
            petData.statusText = "Dead";
        }
    }

    private static void NormalizeCurrency(CurrencyData currencyData)
    {
        currencyData.coins = Mathf.Max(0, currencyData.coins);
    }

    private static void NormalizeInventory(InventoryData inventoryData)
    {
        inventoryData.items ??= new List<InventoryEntry>();
        inventoryData.food = Mathf.Max(0, inventoryData.food);
        inventoryData.snack = Mathf.Max(0, inventoryData.snack);
        inventoryData.meal = Mathf.Max(0, inventoryData.meal);
        inventoryData.premium = Mathf.Max(0, inventoryData.premium);

        for (int i = inventoryData.items.Count - 1; i >= 0; i--)
        {
            InventoryEntry entry = inventoryData.items[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.itemId))
            {
                inventoryData.items.RemoveAt(i);
                continue;
            }

            entry.itemId = entry.itemId.Trim();
            entry.count = Mathf.Max(0, entry.count);
            if (entry.count == 0)
            {
                inventoryData.items.RemoveAt(i);
            }
        }

        MigrateLegacyInventoryCounter(inventoryData, "food_basic", inventoryData.food, value => inventoryData.food = value);
        MigrateLegacyInventoryCounter(inventoryData, "food_snack", inventoryData.snack, value => inventoryData.snack = value);
        MigrateLegacyInventoryCounter(inventoryData, "food_meal", inventoryData.meal, value => inventoryData.meal = value);
        MigrateLegacyInventoryCounter(inventoryData, "food_premium", inventoryData.premium, value => inventoryData.premium = value);
    }

    private static void MigrateLegacyInventoryCounter(InventoryData inventoryData, string itemId, int legacyCount, Action<int> clearLegacy)
    {
        if (legacyCount <= 0)
        {
            clearLegacy(0);
            return;
        }

        InventoryEntry existingEntry = inventoryData.items.Find(entry => entry != null && entry.itemId == itemId);
        if (existingEntry == null)
        {
            inventoryData.items.Add(new InventoryEntry
            {
                itemId = itemId,
                count = legacyCount
            });
        }
        else
        {
            existingEntry.count = Mathf.Max(existingEntry.count, legacyCount);
        }

        clearLegacy(0);
    }

    private static void NormalizeProgression(ProgressionData progressionData)
    {
        progressionData.level = Mathf.Max(1, progressionData.level);
        progressionData.xp = Mathf.Max(0, progressionData.xp);
    }

    private static void NormalizeSkills(SkillsData skillsData)
    {
        skillsData.skills ??= new List<SkillEntry>();

        for (int i = skillsData.skills.Count - 1; i >= 0; i--)
        {
            SkillEntry skill = skillsData.skills[i];
            if (skill == null)
            {
                skillsData.skills.RemoveAt(i);
                continue;
            }

            skill.id = string.IsNullOrWhiteSpace(skill.id) ? "skill_" + Guid.NewGuid().ToString("N") : skill.id.Trim();
            skill.name = skill.name ?? string.Empty;
            skill.icon = skill.icon ?? string.Empty;
            skill.percent = Mathf.Clamp(skill.percent, 0f, 100f);
            skill.bonusExpMultiplier = Mathf.Max(0f, skill.bonusExpMultiplier);
            skill.totalFocusMinutes = Mathf.Max(0, skill.totalFocusMinutes);
            skill.lastFocusDate = NormalizeTimestamp(skill.lastFocusDate);
        }
    }

    private static void NormalizeRoom(RoomData roomData)
    {
        roomData.roomLevel = Mathf.Max(0, roomData.roomLevel);
    }

    private static void NormalizeMissions(MissionData missionData)
    {
        missionData.lastDailyResetKey = missionData.lastDailyResetKey ?? string.Empty;
        missionData.missions ??= new List<MissionEntryData>();
        missionData.personalizationProfile ??= new MissionPersonalizationProfileData();
        missionData.personalizationProfile.lastDecayResetKey = missionData.personalizationProfile.lastDecayResetKey ?? string.Empty;
        missionData.personalizationProfile.skillPreferences ??= new List<MissionSkillPreferenceData>();
        missionData.personalizationProfile.recentFocusMinutes = Mathf.Max(0f, missionData.personalizationProfile.recentFocusMinutes);
        missionData.customRoutineCreateCount = Mathf.Max(0, missionData.customRoutineCreateCount);

        for (int i = missionData.missions.Count - 1; i >= 0; i--)
        {
            MissionEntryData mission = missionData.missions[i];
            if (mission == null)
            {
                missionData.missions.RemoveAt(i);
                continue;
            }

            mission.missionId = mission.missionId ?? string.Empty;
            mission.missionType = mission.missionType ?? string.Empty;
            mission.skillMissionMode = mission.skillMissionMode ?? string.Empty;
            mission.title = mission.title ?? string.Empty;
            mission.skillId = mission.skillId ?? string.Empty;
            mission.targetSkillId = mission.targetSkillId ?? string.Empty;
            mission.targetSkillName = mission.targetSkillName ?? string.Empty;
            mission.currentProgress = Mathf.Max(0, mission.currentProgress);
            mission.targetProgress = Mathf.Max(0, mission.targetProgress);
            mission.requiredMinutes = Mathf.Max(0f, mission.requiredMinutes);
            mission.progressMinutes = Mathf.Clamp(mission.progressMinutes, 0f, mission.requiredMinutes > 0f ? mission.requiredMinutes : Mathf.Max(0f, mission.progressMinutes));
            mission.rewardCoins = Mathf.Max(0, mission.rewardCoins);
            mission.rewardXp = Mathf.Max(0, mission.rewardXp);
            mission.rewardMood = Mathf.Max(0, mission.rewardMood);
            mission.rewardEnergy = Mathf.Max(0, mission.rewardEnergy);
            mission.rewardSkillPercent = Mathf.Max(0f, mission.rewardSkillPercent);
            mission.rewardSkillId = mission.rewardSkillId ?? string.Empty;
        }

        for (int i = missionData.personalizationProfile.skillPreferences.Count - 1; i >= 0; i--)
        {
            MissionSkillPreferenceData preference = missionData.personalizationProfile.skillPreferences[i];
            if (preference == null || string.IsNullOrWhiteSpace(preference.skillId))
            {
                missionData.personalizationProfile.skillPreferences.RemoveAt(i);
                continue;
            }

            preference.skillId = preference.skillId.Trim();
        }
    }

    private static void NormalizeDailyReward(DailyRewardData dailyRewardData)
    {
        dailyRewardData.lastClaimDate = dailyRewardData.lastClaimDate ?? string.Empty;
    }

    private static void NormalizeFocusState(FocusStateData focusStateData)
    {
        if (focusStateData == null)
        {
            return;
        }

        focusStateData.selectedSkillId = focusStateData.selectedSkillId ?? string.Empty;

        if (focusStateData.activeSession != null)
        {
            focusStateData.activeSession.skillId = focusStateData.activeSession.skillId ?? string.Empty;
            focusStateData.activeSession.configuredDurationSeconds = Mathf.Max(0f, focusStateData.activeSession.configuredDurationSeconds);
            focusStateData.activeSession.elapsedSeconds = Mathf.Clamp(
                focusStateData.activeSession.elapsedSeconds,
                0f,
                focusStateData.activeSession.configuredDurationSeconds > 0f
                    ? focusStateData.activeSession.configuredDurationSeconds
                    : Mathf.Max(0f, focusStateData.activeSession.elapsedSeconds));
            focusStateData.activeSession.savedAtUtc = NormalizeUtcTimestamp(focusStateData.activeSession.savedAtUtc);

            if (!focusStateData.activeSession.HasSessionData())
            {
                focusStateData.activeSession = null;
            }
        }

        if (focusStateData.lastResult != null)
        {
            focusStateData.lastResult.skillId = focusStateData.lastResult.skillId ?? string.Empty;
            focusStateData.lastResult.skillName = focusStateData.lastResult.skillName ?? string.Empty;
            focusStateData.lastResult.skillIcon = focusStateData.lastResult.skillIcon ?? string.Empty;
            focusStateData.lastResult.petReaction = focusStateData.lastResult.petReaction ?? string.Empty;
            focusStateData.lastResult.plannedDurationSeconds = Mathf.Max(0f, focusStateData.lastResult.plannedDurationSeconds);
            focusStateData.lastResult.actualDurationSeconds = Mathf.Clamp(
                focusStateData.lastResult.actualDurationSeconds,
                0f,
                focusStateData.lastResult.plannedDurationSeconds > 0f
                    ? focusStateData.lastResult.plannedDurationSeconds
                    : Mathf.Max(0f, focusStateData.lastResult.actualDurationSeconds));
            focusStateData.lastResult.previousPercent = Mathf.Clamp(focusStateData.lastResult.previousPercent, 0f, 100f);
            focusStateData.lastResult.newPercent = Mathf.Clamp(focusStateData.lastResult.newPercent, 0f, 100f);
            focusStateData.lastResult.deltaProgress = Mathf.Max(0f, focusStateData.lastResult.deltaProgress);
            focusStateData.lastResult.coinsReward = Mathf.Max(0, focusStateData.lastResult.coinsReward);
            focusStateData.lastResult.xpReward = Mathf.Max(0, focusStateData.lastResult.xpReward);
            focusStateData.lastResult.energyReward = Mathf.Max(0f, focusStateData.lastResult.energyReward);
            focusStateData.lastResult.moodReward = Mathf.Max(0f, focusStateData.lastResult.moodReward);
            focusStateData.lastResult.energyBefore = Mathf.Clamp(focusStateData.lastResult.energyBefore, 0f, 100f);
            focusStateData.lastResult.energyAfter = Mathf.Clamp(focusStateData.lastResult.energyAfter, 0f, 100f);
        }
    }

    private static int NormalizeResetBucket(int rawBucket, MissionData missionData)
    {
        int normalizedBucket = TimeService.NormalizeResetBucket(rawBucket);
        if (normalizedBucket <= 0 && missionData != null && TimeService.TryParseResetBucket(missionData.lastDailyResetKey, out int migratedBucket))
        {
            normalizedBucket = migratedBucket;
        }

        if (normalizedBucket > 0 && missionData != null && string.IsNullOrEmpty(missionData.lastDailyResetKey))
        {
            missionData.lastDailyResetKey = TimeService.FormatResetBucket(normalizedBucket);
        }

        return normalizedBucket;
    }

    private static string NormalizeUtcTimestamp(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        if (DateTime.TryParse(rawValue, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsed))
        {
            return parsed.ToUniversalTime().ToString("O");
        }

        return string.Empty;
    }

    private static string NormalizeTimestamp(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        if (DateTime.TryParse(rawValue, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsed))
        {
            return parsed.ToString("O");
        }

        return string.Empty;
    }
}
