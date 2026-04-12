using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleSystem
{
    public const int DefaultCombatSkillCount = 3;
    public const float DefaultBattleEnergyCost = 10f;

    private BalanceConfig balanceConfig;
    private SkillsSystem skillsSystem;

    public void Init(BalanceConfig config, SkillsSystem skillSystem)
    {
        balanceConfig = config;
        skillsSystem = skillSystem;
    }

    public List<BossDefinitionData> GetBossDefinitions()
    {
        BossDefinitionData[] source = HasConfiguredBosses()
            ? balanceConfig.bossDefinitions
            : CreateDefaultBossDefinitions();

        List<BossDefinitionData> bosses = new List<BossDefinitionData>();
        if (source == null)
        {
            return bosses;
        }

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] != null)
            {
                bosses.Add(NormalizeBossDefinition(source[i]));
            }
        }

        bosses.Sort((left, right) => left.bossPower.CompareTo(right.bossPower));
        return bosses;
    }

    public BossDefinitionData GetBossDefinition(string bossId)
    {
        List<BossDefinitionData> bosses = GetBossDefinitions();
        string normalizedId = NormalizeId(bossId);
        for (int i = 0; i < bosses.Count; i++)
        {
            if (string.Equals(bosses[i].id, normalizedId, StringComparison.Ordinal))
            {
                return bosses[i];
            }
        }

        return bosses.Count > 0 ? bosses[0] : null;
    }

    public int GetTrackedSkillCount()
    {
        return skillsSystem != null ? skillsSystem.GetSkills().Count : 0;
    }

    public List<BattleCombatSkillSnapshotData> GetTopCombatSkills(int count = DefaultCombatSkillCount)
    {
        List<BattleCombatSkillSnapshotData> combatSkills = new List<BattleCombatSkillSnapshotData>();
        if (skillsSystem == null)
        {
            return combatSkills;
        }

        List<SkillProgressionViewData> views = skillsSystem.GetSkillProgressionViews();
        views.Sort(CompareCombatViews);

        int limit = Mathf.Min(Mathf.Max(0, count), views.Count);
        for (int i = 0; i < limit; i++)
        {
            SkillProgressionViewData view = views[i];
            if (view == null)
            {
                continue;
            }

            combatSkills.Add(new BattleCombatSkillSnapshotData
            {
                skillId = view.id ?? string.Empty,
                name = view.name ?? string.Empty,
                icon = view.icon ?? string.Empty,
                totalSP = view.totalSP,
                effectiveSP = view.effectiveSP,
                level = view.level,
                axisPercent = view.axisPercent
            });
        }

        return combatSkills;
    }

    public float CalculatePlayerBattlePower(int count = DefaultCombatSkillCount)
    {
        List<BattleCombatSkillSnapshotData> combatSkills = GetTopCombatSkills(count);
        if (combatSkills.Count == 0)
        {
            return 0f;
        }

        float total = 0f;
        for (int i = 0; i < combatSkills.Count; i++)
        {
            total += combatSkills[i].effectiveSP;
        }

        return total / combatSkills.Count;
    }

    public BattlePlayerPreviewData GetPlayerPreview(int count = DefaultCombatSkillCount)
    {
        List<BattleCombatSkillSnapshotData> combatSkills = GetTopCombatSkills(count);
        return new BattlePlayerPreviewData
        {
            playerBattlePower = CalculateAveragePower(combatSkills),
            combatSkills = combatSkills
        };
    }

    public BattleResultData ResolveBattle(string bossId)
    {
        BossDefinitionData boss = GetBossDefinition(bossId);
        List<BattleCombatSkillSnapshotData> combatSkills = GetTopCombatSkills(DefaultCombatSkillCount);
        float playerBattlePower = CalculateAveragePower(combatSkills);

        return new BattleResultData
        {
            bossId = boss != null ? boss.id : string.Empty,
            bossName = boss != null ? boss.name : "Unknown Boss",
            bossTargetLevel = boss != null ? boss.targetLevel : 0,
            bossPower = boss != null ? boss.bossPower : 0,
            playerBattlePower = playerBattlePower,
            result = ResolveBattleOutcome(playerBattlePower, boss != null ? boss.bossPower : 0),
            combatSkills = combatSkills
        };
    }

    public BattleOutcome ResolveBattleOutcome(float playerBattlePower, int bossPower)
    {
        return playerBattlePower >= bossPower ? BattleOutcome.Win : BattleOutcome.Loss;
    }

    public static BossDefinitionData[] CreateDefaultBossDefinitions()
    {
        return new[]
        {
            CreateBoss("boss_01", "Boss 1", 1, 100, "Tutorial",  new [] {0.32f, 0.24f, 0.28f, 0.26f, 0.34f}),
            CreateBoss("boss_02", "Boss 2", 2, 260, "Beginner",  new [] {0.38f, 0.28f, 0.33f, 0.31f, 0.4f}),
            CreateBoss("boss_03", "Boss 3", 3, 516, "Early",     new [] {0.45f, 0.34f, 0.38f, 0.36f, 0.46f}),
            CreateBoss("boss_04", "Boss 4", 4, 926, "Rising",    new [] {0.52f, 0.41f, 0.47f, 0.42f, 0.55f}),
            CreateBoss("boss_05", "Boss 5", 5, 1582, "Stable",   new [] {0.61f, 0.48f, 0.55f, 0.51f, 0.62f}),
            CreateBoss("boss_06", "Boss 6", 6, 2632, "Mid",      new [] {0.7f, 0.56f, 0.64f, 0.6f, 0.72f}),
            CreateBoss("boss_07", "Boss 7", 7, 4312, "Advanced", new [] {0.79f, 0.65f, 0.73f, 0.68f, 0.81f}),
            CreateBoss("boss_08", "Boss 8", 8, 7000, "Expert",   new [] {0.86f, 0.74f, 0.82f, 0.77f, 0.88f}),
            CreateBoss("boss_09", "Boss 9", 9, 11300, "Master",  new [] {0.92f, 0.82f, 0.9f, 0.85f, 0.94f}),
            CreateBoss("boss_10", "Boss 10", 10, 18180, "Endgame", new [] {0.98f, 0.91f, 0.96f, 0.93f, 1f})
        };
    }

    private bool HasConfiguredBosses()
    {
        return balanceConfig != null && balanceConfig.bossDefinitions != null && balanceConfig.bossDefinitions.Length > 0;
    }

    private float CalculateAveragePower(List<BattleCombatSkillSnapshotData> combatSkills)
    {
        if (combatSkills == null || combatSkills.Count == 0)
        {
            return 0f;
        }

        float total = 0f;
        for (int i = 0; i < combatSkills.Count; i++)
        {
            total += combatSkills[i].effectiveSP;
        }

        return total / combatSkills.Count;
    }

    private int CompareCombatViews(SkillProgressionViewData left, SkillProgressionViewData right)
    {
        float leftAxis = left != null ? left.axisPercent : 0f;
        float rightAxis = right != null ? right.axisPercent : 0f;
        int axisComparison = rightAxis.CompareTo(leftAxis);
        if (axisComparison != 0)
        {
            return axisComparison;
        }

        int leftTotalSP = left != null ? left.effectiveSP : 0;
        int rightTotalSP = right != null ? right.effectiveSP : 0;
        int totalSpComparison = rightTotalSP.CompareTo(leftTotalSP);
        if (totalSpComparison != 0)
        {
            return totalSpComparison;
        }

        string leftDate = left != null ? left.lastFocusDate ?? string.Empty : string.Empty;
        string rightDate = right != null ? right.lastFocusDate ?? string.Empty : string.Empty;
        return string.Compare(rightDate, leftDate, StringComparison.Ordinal);
    }

    private static BossDefinitionData CreateBoss(string id, string name, int targetLevel, int bossPower, string difficultyTier, float[] visualWebValues)
    {
        return new BossDefinitionData
        {
            id = id,
            name = name,
            targetLevel = targetLevel,
            bossPower = bossPower,
            rewardCoins = CalculateRewardCoins(targetLevel),
            difficultyTier = difficultyTier,
            visualWebValues01 = visualWebValues != null ? new List<float>(visualWebValues) : new List<float>()
        };
    }

    private static BossDefinitionData NormalizeBossDefinition(BossDefinitionData source)
    {
        BossDefinitionData clone = source != null ? source.Clone() : new BossDefinitionData();
        clone.id = clone.id ?? string.Empty;
        clone.name = string.IsNullOrWhiteSpace(clone.name) ? "Boss" : clone.name.Trim();
        clone.targetLevel = Mathf.Max(1, clone.targetLevel);
        clone.bossPower = Mathf.Max(0, clone.bossPower);
        clone.rewardCoins = clone.rewardCoins > 0 ? clone.rewardCoins : CalculateRewardCoins(clone.targetLevel);
        clone.difficultyTier = clone.difficultyTier ?? string.Empty;
        clone.visualWebValues01 = clone.visualWebValues01 != null ? new List<float>(clone.visualWebValues01) : new List<float>();
        return clone;
    }

    private static int CalculateRewardCoins(int targetLevel)
    {
        float tier01 = Mathf.InverseLerp(1f, 10f, Mathf.Clamp(targetLevel, 1, 10));
        return Mathf.RoundToInt(Mathf.Lerp(30f, 90f, tier01));
    }

    private string NormalizeId(string bossId)
    {
        return string.IsNullOrWhiteSpace(bossId) ? string.Empty : bossId.Trim();
    }
}
