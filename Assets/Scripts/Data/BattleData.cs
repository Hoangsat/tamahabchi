using System;
using System.Collections.Generic;

[Serializable]
public enum BattleOutcome
{
    Loss,
    Win
}

[Serializable]
public class BossDefinitionData
{
    public string id = string.Empty;
    public string name = string.Empty;
    public int targetLevel = 0;
    public int bossPower = 0;
    public int rewardCoins = 0;
    public string difficultyTier = string.Empty;
    public List<float> visualWebValues01 = new List<float>();

    public BossDefinitionData Clone()
    {
        return new BossDefinitionData
        {
            id = id ?? string.Empty,
            name = name ?? string.Empty,
            targetLevel = targetLevel,
            bossPower = bossPower,
            rewardCoins = rewardCoins,
            difficultyTier = difficultyTier ?? string.Empty,
            visualWebValues01 = visualWebValues01 != null ? new List<float>(visualWebValues01) : new List<float>()
        };
    }
}

[Serializable]
public class BattleCombatSkillSnapshotData
{
    public string skillId = string.Empty;
    public string name = string.Empty;
    public string icon = string.Empty;
    public int totalSP = 0;
    public int effectiveSP = 0;
    public int level = 0;
    public float axisPercent = 0f;

    public BattleCombatSkillSnapshotData Clone()
    {
        return new BattleCombatSkillSnapshotData
        {
            skillId = skillId ?? string.Empty,
            name = name ?? string.Empty,
            icon = icon ?? string.Empty,
            totalSP = totalSP,
            effectiveSP = effectiveSP,
            level = level,
            axisPercent = axisPercent
        };
    }
}

[Serializable]
public class BattlePlayerPreviewData
{
    public float playerBattlePower = 0f;
    public List<BattleCombatSkillSnapshotData> combatSkills = new List<BattleCombatSkillSnapshotData>();

    public BattlePlayerPreviewData Clone()
    {
        BattlePlayerPreviewData clone = new BattlePlayerPreviewData
        {
            playerBattlePower = playerBattlePower,
            combatSkills = new List<BattleCombatSkillSnapshotData>()
        };

        if (combatSkills != null)
        {
            for (int i = 0; i < combatSkills.Count; i++)
            {
                if (combatSkills[i] != null)
                {
                    clone.combatSkills.Add(combatSkills[i].Clone());
                }
            }
        }

        return clone;
    }
}

[Serializable]
public class BattleResultData
{
    public string bossId = string.Empty;
    public string bossName = string.Empty;
    public int bossTargetLevel = 0;
    public int bossPower = 0;
    public float playerBattlePower = 0f;
    public BattleOutcome result = BattleOutcome.Loss;
    public int rewardCoins = 0;
    public float energyCost = 0f;
    public bool wasBlocked = false;
    public string statusMessage = string.Empty;
    public string adviceMessage = string.Empty;
    public List<BattleCombatSkillSnapshotData> combatSkills = new List<BattleCombatSkillSnapshotData>();

    public BattleResultData Clone()
    {
        BattleResultData clone = new BattleResultData
        {
            bossId = bossId ?? string.Empty,
            bossName = bossName ?? string.Empty,
            bossTargetLevel = bossTargetLevel,
            bossPower = bossPower,
            playerBattlePower = playerBattlePower,
            result = result,
            rewardCoins = rewardCoins,
            energyCost = energyCost,
            wasBlocked = wasBlocked,
            statusMessage = statusMessage ?? string.Empty,
            adviceMessage = adviceMessage ?? string.Empty,
            combatSkills = new List<BattleCombatSkillSnapshotData>()
        };

        if (combatSkills != null)
        {
            for (int i = 0; i < combatSkills.Count; i++)
            {
                if (combatSkills[i] != null)
                {
                    clone.combatSkills.Add(combatSkills[i].Clone());
                }
            }
        }

        return clone;
    }
}

[Serializable]
public class BattleAvailabilityData
{
    public int trackedSkillCount = 0;
    public int requiredSkillCount = 0;
    public float currentEnergy = 0f;
    public float requiredEnergy = 0f;
    public string blockedReason = string.Empty;
    public string guidance = string.Empty;

    public bool HasEnoughSkills
    {
        get { return trackedSkillCount >= requiredSkillCount; }
    }

    public bool HasEnoughEnergy
    {
        get { return currentEnergy + 0.001f >= requiredEnergy; }
    }

    public bool CanFight
    {
        get { return HasEnoughSkills && HasEnoughEnergy; }
    }

    public BattleAvailabilityData Clone()
    {
        return new BattleAvailabilityData
        {
            trackedSkillCount = trackedSkillCount,
            requiredSkillCount = requiredSkillCount,
            currentEnergy = currentEnergy,
            requiredEnergy = requiredEnergy,
            blockedReason = blockedReason ?? string.Empty,
            guidance = guidance ?? string.Empty
        };
    }
}
