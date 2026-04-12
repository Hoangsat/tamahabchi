using System;
using System.Collections.Generic;
using UnityEngine;

public static class SaveNormalizer
{
    public const int CurrentSaveVersion = 7;

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
        normalized.idleData ??= new IdleData();

        NormalizePet(normalized.petData);
        NormalizeCurrency(normalized.currencyData);
        NormalizeInventory(normalized.inventoryData);
        NormalizeProgression(normalized.progressionData);
        NormalizeSkills(normalized.skillsData);
        NormalizeRoom(normalized.roomData);
        NormalizeMissions(normalized.missionData);
        NormalizeDailyReward(normalized.dailyRewardData);
        NormalizeFocusState(normalized.focusStateData);
        NormalizeIdle(normalized.idleData);
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
        petData.neglectDecayCarrySeconds = Mathf.Max(0f, petData.neglectDecayCarrySeconds);
        petData.statusText = string.IsNullOrWhiteSpace(petData.statusText) ? "Happy" : petData.statusText.Trim();
    }


    private static void NormalizeCurrency(CurrencyData currencyData)
    {
        currencyData.coins = Mathf.Max(0, currencyData.coins);
    }

    private static void NormalizeInventory(InventoryData inventoryData)
    {
        inventoryData.items ??= new List<InventoryEntry>();
        inventoryData.ownedSkins ??= new List<string>();
        inventoryData.food = Mathf.Max(0, inventoryData.food);
        inventoryData.snack = Mathf.Max(0, inventoryData.snack);
        inventoryData.meal = Mathf.Max(0, inventoryData.meal);
        inventoryData.premium = Mathf.Max(0, inventoryData.premium);
        inventoryData.equippedSkin = string.IsNullOrWhiteSpace(inventoryData.equippedSkin) ? "default" : inventoryData.equippedSkin.Trim();

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

        for (int i = inventoryData.ownedSkins.Count - 1; i >= 0; i--)
        {
            string skinId = inventoryData.ownedSkins[i];
            if (string.IsNullOrWhiteSpace(skinId))
            {
                inventoryData.ownedSkins.RemoveAt(i);
                continue;
            }

            inventoryData.ownedSkins[i] = skinId.Trim();
        }

        if (!inventoryData.ownedSkins.Contains("default"))
        {
            inventoryData.ownedSkins.Insert(0, "default");
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
            if (string.IsNullOrWhiteSpace(skill.archetypeId))
            {
                skill.archetypeId = SkillArchetypeCatalog.ResolveArchetypeIdFromLegacyIcon(skill.icon);
            }

            skill.archetypeId = SkillArchetypeCatalog.NormalizeArchetypeId(skill.archetypeId);
            skill.icon = SkillArchetypeCatalog.GetCanonicalIcon(skill.archetypeId);
            if (skill.totalSP <= 0 && skill.percent > 0f)
            {
                skill.totalSP = SkillProgressionModel.GetTotalSPForAxisPercent(skill.percent);
            }

            skill.totalSP = SkillProgressionModel.ClampTotalSP(skill.totalSP);
            skill.decayDebtSP = Mathf.Clamp(skill.decayDebtSP, 0, skill.totalSP);
            skill.isGolden = SkillProgressionModel.IsMaxed(skill.totalSP);
            skill.bonusExpMultiplier = Mathf.Max(0f, skill.bonusExpMultiplier);
            skill.totalFocusMinutes = Mathf.Max(0, skill.totalFocusMinutes);
            skill.lastFocusDate = NormalizeTimestamp(skill.lastFocusDate);
            skill.percent = 0f;
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
            mission.rewardMood = Mathf.Max(0, mission.rewardMood);
            mission.rewardEnergy = Mathf.Max(0, mission.rewardEnergy);
            if (mission.rewardSkillSP <= 0 && mission.rewardSkillPercent > 0f)
            {
                mission.rewardSkillSP = Mathf.Max(5, Mathf.RoundToInt(mission.rewardSkillPercent * 10f));
            }

            mission.rewardSkillSP = Mathf.Max(0, mission.rewardSkillSP);
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

    private static void NormalizeIdle(IdleData idleData)
    {
        if (idleData == null)
        {
            return;
        }

        idleData.currentActionId = idleData.currentActionId ?? string.Empty;
        idleData.currentArchetypeId = SkillArchetypeCatalog.NormalizeArchetypeId(idleData.currentArchetypeId);
        idleData.currentActionStartedAtUtcTicks = Math.Max(0L, idleData.currentActionStartedAtUtcTicks);
        idleData.nextActionAtUtcTicks = Math.Max(0L, idleData.nextActionAtUtcTicks);
        idleData.lastEventAtUtcTicks = Math.Max(0L, idleData.lastEventAtUtcTicks);
        idleData.lastResolvedUtcTicks = Math.Max(0L, idleData.lastResolvedUtcTicks);
        idleData.pendingEvents ??= new List<IdleEventEntryData>();
        idleData.collectedMomentIds ??= new List<string>();

        for (int i = idleData.pendingEvents.Count - 1; i >= 0; i--)
        {
            IdleEventEntryData entry = idleData.pendingEvents[i];
            if (entry == null)
            {
                idleData.pendingEvents.RemoveAt(i);
                continue;
            }

            entry.id = string.IsNullOrWhiteSpace(entry.id) ? "idle_" + Guid.NewGuid().ToString("N") : entry.id.Trim();
            entry.type = NormalizeIdleEventType(entry.type);
            entry.archetypeId = SkillArchetypeCatalog.NormalizeArchetypeId(entry.archetypeId);
            entry.title = entry.title ?? string.Empty;
            entry.summary = entry.summary ?? string.Empty;
            entry.coins = Mathf.Max(0, entry.coins);
            entry.itemId = entry.itemId ?? string.Empty;
            entry.skinId = entry.skinId ?? string.Empty;
            entry.momentId = entry.momentId ?? string.Empty;
            entry.createdAtUtcTicks = Math.Max(0L, entry.createdAtUtcTicks);
            entry.source = NormalizeIdleEventSource(entry.source);
        }

        for (int i = idleData.collectedMomentIds.Count - 1; i >= 0; i--)
        {
            string momentId = idleData.collectedMomentIds[i];
            if (string.IsNullOrWhiteSpace(momentId))
            {
                idleData.collectedMomentIds.RemoveAt(i);
                continue;
            }

            idleData.collectedMomentIds[i] = momentId.Trim();
        }
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
            if (focusStateData.lastResult.previousTotalSP <= 0 && focusStateData.lastResult.previousPercent > 0f)
            {
                focusStateData.lastResult.previousTotalSP = SkillProgressionModel.GetTotalSPForAxisPercent(focusStateData.lastResult.previousPercent);
            }

            if (focusStateData.lastResult.newTotalSP <= 0 && focusStateData.lastResult.newPercent > 0f)
            {
                focusStateData.lastResult.newTotalSP = SkillProgressionModel.GetTotalSPForAxisPercent(focusStateData.lastResult.newPercent);
            }

            focusStateData.lastResult.previousTotalSP = SkillProgressionModel.ClampTotalSP(focusStateData.lastResult.previousTotalSP);
            focusStateData.lastResult.newTotalSP = SkillProgressionModel.ClampTotalSP(
                Mathf.Max(focusStateData.lastResult.previousTotalSP, focusStateData.lastResult.newTotalSP));
            focusStateData.lastResult.skillSpReward = Mathf.Max(0, focusStateData.lastResult.skillSpReward);
            if (focusStateData.lastResult.skillSpReward <= 0 && focusStateData.lastResult.newTotalSP >= focusStateData.lastResult.previousTotalSP)
            {
                focusStateData.lastResult.skillSpReward = focusStateData.lastResult.newTotalSP - focusStateData.lastResult.previousTotalSP;
            }

            focusStateData.lastResult.previousLevel = SkillProgressionModel.GetLevel(focusStateData.lastResult.previousTotalSP);
            focusStateData.lastResult.newLevel = SkillProgressionModel.GetLevel(focusStateData.lastResult.newTotalSP);
            focusStateData.lastResult.previousAxisPercent = SkillProgressionModel.GetAxisPercent(focusStateData.lastResult.previousTotalSP);
            focusStateData.lastResult.newAxisPercent = SkillProgressionModel.GetAxisPercent(focusStateData.lastResult.newTotalSP);
            focusStateData.lastResult.previousProgressInLevel01 = SkillProgressionModel.GetProgressInLevel01(focusStateData.lastResult.previousTotalSP);
            focusStateData.lastResult.newProgressInLevel01 = SkillProgressionModel.GetProgressInLevel01(focusStateData.lastResult.newTotalSP);
            focusStateData.lastResult.previousPercent = focusStateData.lastResult.previousAxisPercent;
            focusStateData.lastResult.newPercent = focusStateData.lastResult.newAxisPercent;
            focusStateData.lastResult.deltaProgress = Mathf.Max(0f, focusStateData.lastResult.newAxisPercent - focusStateData.lastResult.previousAxisPercent);
            focusStateData.lastResult.coinsReward = Mathf.Max(0, focusStateData.lastResult.coinsReward);
            focusStateData.lastResult.xpReward = Mathf.Max(0, focusStateData.lastResult.xpReward);
            focusStateData.lastResult.energyReward = 0f;
            focusStateData.lastResult.moodReward = 0f;
            focusStateData.lastResult.energyBefore = Mathf.Clamp(focusStateData.lastResult.energyBefore, 0f, 100f);
            focusStateData.lastResult.energyAfter = Mathf.Clamp(focusStateData.lastResult.energyAfter, 0f, 100f);
            focusStateData.lastResult.lowEnergyPenaltyApplied = false;
            focusStateData.lastResult.becameGolden = SkillProgressionModel.IsMaxed(focusStateData.lastResult.newTotalSP)
                && !SkillProgressionModel.IsMaxed(focusStateData.lastResult.previousTotalSP);
            focusStateData.lastResult.isGolden = SkillProgressionModel.IsMaxed(focusStateData.lastResult.newTotalSP);
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

    private static string NormalizeIdleEventType(string rawType)
    {
        string normalized = string.IsNullOrWhiteSpace(rawType) ? string.Empty : rawType.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "chest":
            case "moment":
            case "rare":
                return normalized;
            default:
                return "coins";
        }
    }

    private static string NormalizeIdleEventSource(string rawSource)
    {
        string normalized = string.IsNullOrWhiteSpace(rawSource) ? string.Empty : rawSource.Trim().ToLowerInvariant();
        return normalized == "offline" ? "offline" : "live";
    }
}
