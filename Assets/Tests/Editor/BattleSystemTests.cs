using NUnit.Framework;
using UnityEngine;

public class BattleSystemTests
{
    [Test]
    public void GetBossDefinitions_ReturnsTenDefaultsInAscendingPower()
    {
        BalanceConfig balance = ScriptableObject.CreateInstance<BalanceConfig>();
        balance.bossDefinitions = BattleSystem.CreateDefaultBossDefinitions();

        try
        {
            SkillsSystem skills = new SkillsSystem();
            skills.Init(new SkillsData());

            BattleSystem battle = new BattleSystem();
            battle.Init(balance, skills);

            var bosses = battle.GetBossDefinitions();
            Assert.AreEqual(10, bosses.Count);
            Assert.AreEqual(100, bosses[0].bossPower);
            Assert.AreEqual(30, bosses[0].rewardCoins);
            Assert.AreEqual(18180, bosses[bosses.Count - 1].bossPower);
        }
        finally
        {
            Object.DestroyImmediate(balance);
        }
    }

    [Test]
    public void CalculatePlayerBattlePower_UsesTopThreeSkillsOnly()
    {
        BalanceConfig balance = ScriptableObject.CreateInstance<BalanceConfig>();
        balance.bossDefinitions = BattleSystem.CreateDefaultBossDefinitions();

        try
        {
            SkillsData data = new SkillsData();
            data.skills.Add(CreateSkill("skill_1", "Coding", 3000, "2026-04-11T09:00:00"));
            data.skills.Add(CreateSkill("skill_2", "Art", 2500, "2026-04-10T09:00:00"));
            data.skills.Add(CreateSkill("skill_3", "Music", 2000, "2026-04-09T09:00:00"));
            data.skills.Add(CreateSkill("skill_4", "Dance", 25, "2026-04-08T09:00:00"));

            SkillsSystem skills = new SkillsSystem();
            skills.Init(data);

            BattleSystem battle = new BattleSystem();
            battle.Init(balance, skills);

            Assert.AreEqual(2500f, battle.CalculatePlayerBattlePower(), 0.01f);
            Assert.AreEqual(3, battle.GetTopCombatSkills().Count);
            Assert.AreEqual("skill_1", battle.GetTopCombatSkills()[0].skillId);
        }
        finally
        {
            Object.DestroyImmediate(balance);
        }
    }

    [Test]
    public void CalculatePlayerBattlePower_UsesAllAvailableWhenLessThanThreeSkills()
    {
        BalanceConfig balance = ScriptableObject.CreateInstance<BalanceConfig>();
        balance.bossDefinitions = BattleSystem.CreateDefaultBossDefinitions();

        try
        {
            SkillsData data = new SkillsData();
            data.skills.Add(CreateSkill("skill_1", "Coding", 100, "2026-04-11T09:00:00"));
            data.skills.Add(CreateSkill("skill_2", "Art", 260, "2026-04-10T09:00:00"));

            SkillsSystem skills = new SkillsSystem();
            skills.Init(data);

            BattleSystem battle = new BattleSystem();
            battle.Init(balance, skills);

            Assert.AreEqual(180f, battle.CalculatePlayerBattlePower(), 0.01f);
            Assert.AreEqual(2, battle.GetTopCombatSkills().Count);
        }
        finally
        {
            Object.DestroyImmediate(balance);
        }
    }

    [Test]
    public void AddingWeakSkillOutsideTopThree_DoesNotReducePower()
    {
        BalanceConfig balance = ScriptableObject.CreateInstance<BalanceConfig>();
        balance.bossDefinitions = BattleSystem.CreateDefaultBossDefinitions();

        try
        {
            SkillsData data = new SkillsData();
            data.skills.Add(CreateSkill("skill_1", "Coding", 3000, "2026-04-11T09:00:00"));
            data.skills.Add(CreateSkill("skill_2", "Art", 2500, "2026-04-10T09:00:00"));
            data.skills.Add(CreateSkill("skill_3", "Music", 2000, "2026-04-09T09:00:00"));

            SkillsSystem skills = new SkillsSystem();
            skills.Init(data);

            BattleSystem battle = new BattleSystem();
            battle.Init(balance, skills);
            float before = battle.CalculatePlayerBattlePower();

            data.skills.Add(CreateSkill("skill_4", "Dance", 0, "2026-04-08T09:00:00"));
            float after = battle.CalculatePlayerBattlePower();

            Assert.AreEqual(before, after, 0.01f);
        }
        finally
        {
            Object.DestroyImmediate(balance);
        }
    }

    [Test]
    public void ResolveBattle_EqualPowerCountsAsWin()
    {
        BalanceConfig balance = ScriptableObject.CreateInstance<BalanceConfig>();
        balance.bossDefinitions = BattleSystem.CreateDefaultBossDefinitions();

        try
        {
            SkillsData data = new SkillsData();
            data.skills.Add(CreateSkill("skill_1", "Coding", 100, "2026-04-11T09:00:00"));

            SkillsSystem skills = new SkillsSystem();
            skills.Init(data);

            BattleSystem battle = new BattleSystem();
            battle.Init(balance, skills);

            BattleResultData result = battle.ResolveBattle("boss_01");
            Assert.AreEqual(BattleOutcome.Win, result.result);
            Assert.AreEqual(100f, result.playerBattlePower, 0.01f);
        }
        finally
        {
            Object.DestroyImmediate(balance);
        }
    }

    private SkillEntry CreateSkill(string id, string name, int totalSP, string lastFocusDate)
    {
        return new SkillEntry
        {
            id = id,
            name = name,
            icon = name.Substring(0, Mathf.Min(3, name.Length)).ToUpperInvariant(),
            totalSP = totalSP,
            lastFocusDate = lastFocusDate
        };
    }
}
