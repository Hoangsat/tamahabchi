using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BattleCoordinatorCallbacks
{
    public Action OnCoinsChanged;
    public Action BroadcastPetStateChanged;
    public Action SaveGame;
    public Action UpdateUi;
}

public sealed class BattleCoordinator
{
    private readonly BattleSystem battleSystem;
    private readonly PetSystem petSystem;
    private readonly CurrencySystem currencySystem;
    private readonly BattleCoordinatorCallbacks callbacks;

    private string selectedBossId = string.Empty;

    public BattleCoordinator(
        BattleSystem battleSystem,
        PetSystem petSystem,
        CurrencySystem currencySystem,
        BattleCoordinatorCallbacks callbacks)
    {
        this.battleSystem = battleSystem;
        this.petSystem = petSystem;
        this.currencySystem = currencySystem;
        this.callbacks = callbacks ?? new BattleCoordinatorCallbacks();
    }

    public void ResetRuntimeState()
    {
        selectedBossId = string.Empty;
    }

    public BattlePlayerPreviewData GetPlayerPreview()
    {
        return battleSystem != null ? battleSystem.GetPlayerPreview() : new BattlePlayerPreviewData();
    }

    public BattleAvailabilityData GetAvailability()
    {
        BattleAvailabilityData availability = new BattleAvailabilityData
        {
            trackedSkillCount = battleSystem != null ? battleSystem.GetTrackedSkillCount() : 0,
            requiredSkillCount = BattleSystem.DefaultCombatSkillCount,
            currentEnergy = petSystem != null ? petSystem.GetEnergyPercent() : 0f,
            requiredEnergy = BattleSystem.DefaultBattleEnergyCost
        };

        availability.blockedReason = BuildBlockedReason(availability);
        availability.guidance = BuildGuidance(availability);
        return availability;
    }

    public List<BossDefinitionData> GetBosses()
    {
        return battleSystem != null ? battleSystem.GetBossDefinitions() : new List<BossDefinitionData>();
    }

    public BossDefinitionData SelectBoss(string bossId)
    {
        if (string.IsNullOrWhiteSpace(bossId))
        {
            selectedBossId = string.Empty;
            return null;
        }

        if (battleSystem == null)
        {
            selectedBossId = string.Empty;
            return null;
        }

        BossDefinitionData boss = battleSystem.GetBossDefinition(bossId);
        selectedBossId = boss != null ? boss.id : string.Empty;
        return boss;
    }

    public string GetSelectedBossId()
    {
        return selectedBossId ?? string.Empty;
    }

    public BattleResultData ResolveBattle(string bossId)
    {
        if (battleSystem == null)
        {
            return new BattleResultData
            {
                wasBlocked = true,
                statusMessage = "Battle unavailable.",
                adviceMessage = "Battle system is not ready yet."
            };
        }

        BossDefinitionData boss = SelectBoss(bossId);
        if (boss == null)
        {
            return new BattleResultData
            {
                wasBlocked = true,
                statusMessage = "No boss selected.",
                adviceMessage = "Choose a boss from the list to start a battle."
            };
        }

        BattleAvailabilityData availability = GetAvailability();
        BattlePlayerPreviewData preview = GetPlayerPreview();
        if (availability == null || !availability.CanFight)
        {
            return new BattleResultData
            {
                bossId = boss.id,
                bossName = boss.name,
                bossTargetLevel = boss.targetLevel,
                bossPower = boss.bossPower,
                playerBattlePower = preview != null ? preview.playerBattlePower : 0f,
                combatSkills = preview != null && preview.combatSkills != null
                    ? new List<BattleCombatSkillSnapshotData>(preview.combatSkills)
                    : new List<BattleCombatSkillSnapshotData>(),
                energyCost = availability != null ? availability.requiredEnergy : BattleSystem.DefaultBattleEnergyCost,
                wasBlocked = true,
                statusMessage = availability != null ? availability.blockedReason : "Battle unavailable.",
                adviceMessage = availability != null ? availability.guidance : "Battle is currently unavailable."
            };
        }

        BattleResultData result = battleSystem.ResolveBattle(boss.id);
        result.energyCost = availability.requiredEnergy;

        if (petSystem != null)
        {
            petSystem.ConsumeEnergy(result.energyCost);
            callbacks.BroadcastPetStateChanged?.Invoke();
        }

        if (result.result == BattleOutcome.Win)
        {
            if (currencySystem != null && boss.rewardCoins > 0)
            {
                currencySystem.AddCoins(boss.rewardCoins);
                callbacks.OnCoinsChanged?.Invoke();
            }

            result.rewardCoins = boss.rewardCoins;
        }

        result.adviceMessage = BuildAdvice(result, boss);

        callbacks.SaveGame?.Invoke();
        callbacks.UpdateUi?.Invoke();
        return result;
    }

    private string BuildBlockedReason(BattleAvailabilityData availability)
    {
        if (availability == null)
        {
            return "Battle unavailable.";
        }

        if (!availability.HasEnoughSkills)
        {
            return $"Track at least {availability.requiredSkillCount} skills before fighting ({availability.trackedSkillCount}/{availability.requiredSkillCount}).";
        }

        if (!availability.HasEnoughEnergy)
        {
            return $"Need {availability.requiredEnergy:0} energy to fight. Current energy: {availability.currentEnergy:0}.";
        }

        return string.Empty;
    }

    private string BuildGuidance(BattleAvailabilityData availability)
    {
        if (availability == null)
        {
            return "Battle compares your top 3 skills against boss power.";
        }

        if (!availability.HasEnoughSkills)
        {
            return "Add and grow more tracked skills. Battle v1 uses your top 3 skills by axis strength.";
        }

        if (!availability.HasEnoughEnergy)
        {
            return "Recover energy first, then challenge the boss again.";
        }

        return "Battle v1 is deterministic: your top 3 skills versus boss power.";
    }

    private string BuildAdvice(BattleResultData result, BossDefinitionData boss)
    {
        if (result == null)
        {
            return string.Empty;
        }

        if (result.wasBlocked)
        {
            return result.adviceMessage ?? string.Empty;
        }

        if (result.result == BattleOutcome.Win)
        {
            List<BossDefinitionData> bosses = GetBosses();
            for (int i = 0; i < bosses.Count; i++)
            {
                if (bosses[i] != null && string.Equals(bosses[i].id, boss != null ? boss.id : string.Empty, StringComparison.Ordinal))
                {
                    if (i + 1 < bosses.Count && bosses[i + 1] != null)
                    {
                        return $"Next target: {bosses[i + 1].name} at {bosses[i + 1].bossPower} power.";
                    }

                    break;
                }
            }

            return "You cleared this tier. Keep pushing your top skills for the next battle.";
        }

        float missingPower = Mathf.Max(0f, boss != null ? boss.bossPower - result.playerBattlePower : 0f);
        string anchorSkill = result.combatSkills != null && result.combatSkills.Count > 0
            ? result.combatSkills[0].name
            : "your core skills";
        return $"You are short by about {missingPower:0.#} power. Focus {anchorSkill} and strengthen your top 3 skills.";
    }
}
