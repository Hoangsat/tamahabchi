using NUnit.Framework;
using UnityEngine;

public class PetNeglectSystemTests
{
    [Test]
    public void PetSystem_EntersNeglected_WhenHungerAndMoodReachZero()
    {
        PetData petData = new PetData
        {
            hunger = 0f,
            mood = 0f
        };

        PetSystem petSystem = new PetSystem(petData);

        Assert.True(petSystem.UpdateStatus());
        Assert.True(petSystem.IsNeglected());
        Assert.AreEqual("Neglected", petData.statusText);
    }

    [Test]
    public void PetSystem_LeavesNeglected_WhenAnyCareRecoversHungerOrMood()
    {
        PetData petData = new PetData
        {
            hunger = 0f,
            mood = 0f,
            statusText = "Neglected"
        };

        PetSystem petSystem = new PetSystem(petData);
        petSystem.Feed(5f);
        petSystem.UpdateStatus();

        Assert.False(petSystem.IsNeglected());
        Assert.AreEqual("Starving", petData.statusText);
    }

    [Test]
    public void SkillDecaySystem_AppliesHourlyDebt_AndPreservesPartialCarry()
    {
        SkillsData data = new SkillsData();
        data.skills.Add(new SkillEntry
        {
            id = "skill_math",
            name = "Math",
            totalSP = 10,
            decayDebtSP = 0
        });

        SkillDecaySystem decaySystem = new SkillDecaySystem();
        decaySystem.Init(data);

        float carrySeconds = 0f;
        Assert.True(decaySystem.ApplyNeglectDecay(5400f, ref carrySeconds));
        Assert.AreEqual(1, data.skills[0].decayDebtSP);
        Assert.AreEqual(1800f, carrySeconds, 0.01f);

        Assert.True(decaySystem.ApplyNeglectDecay(1800f, ref carrySeconds));
        Assert.AreEqual(2, data.skills[0].decayDebtSP);
        Assert.AreEqual(0f, carrySeconds, 0.01f);
        Assert.AreEqual(10, data.skills[0].totalSP);
    }

    [Test]
    public void OfflineNeglect_AccruesDecayDebt_WithoutReducingTotalSp()
    {
        PetData petData = new PetData
        {
            hunger = 0f,
            mood = 0f,
            statusText = "Neglected"
        };

        PetSystem petSystem = new PetSystem(petData);
        SkillsData skillsData = new SkillsData();
        skillsData.skills.Add(new SkillEntry
        {
            id = "skill_focus",
            name = "Focus",
            totalSP = 20,
            decayDebtSP = 0
        });

        SkillDecaySystem decaySystem = new SkillDecaySystem();
        decaySystem.Init(skillsData);

        petSystem.ApplyOfflineProgress(6f * 3600f, 0f, 30f, 0.01f, out float neglectSeconds);
        float carrySeconds = 0f;
        decaySystem.ApplyNeglectDecay(neglectSeconds, ref carrySeconds);

        Assert.AreEqual(6f * 3600f, neglectSeconds, 0.01f);
        Assert.AreEqual(6, skillsData.skills[0].decayDebtSP);
        Assert.AreEqual(20, skillsData.skills[0].totalSP);
        Assert.AreEqual(0f, carrySeconds, 0.01f);
    }

    [Test]
    public void SkillsSystem_UsesEffectiveSpForAxis_WhileKeepingLevelFromTotalSp()
    {
        SkillsData data = new SkillsData();
        data.skills.Add(new SkillEntry
        {
            id = "skill_code",
            name = "Coding",
            totalSP = 1582,
            decayDebtSP = 1000
        });

        SkillsSystem skillsSystem = new SkillsSystem();
        skillsSystem.Init(data);

        SkillProgressionViewData view = skillsSystem.GetSkillProgressionView("skill_code");

        Assert.NotNull(view);
        Assert.AreEqual(5, view.level);
        Assert.AreEqual(582, view.effectiveSP);
        Assert.AreEqual(SkillProgressionModel.GetAxisPercent(582), view.axisPercent, 0.001f);
    }

    [Test]
    public void BattleSystem_UsesEffectiveSp_NotTotalSp()
    {
        BalanceConfig balance = ScriptableObject.CreateInstance<BalanceConfig>();
        balance.bossDefinitions = BattleSystem.CreateDefaultBossDefinitions();

        try
        {
            SkillsData data = new SkillsData();
            data.skills.Add(CreateSkill("skill_1", "Coding", 3000, 2900, "2026-04-11T09:00:00"));
            data.skills.Add(CreateSkill("skill_2", "Art", 1200, 0, "2026-04-10T09:00:00"));
            data.skills.Add(CreateSkill("skill_3", "Music", 1300, 0, "2026-04-09T09:00:00"));
            data.skills.Add(CreateSkill("skill_4", "Dance", 1400, 0, "2026-04-08T09:00:00"));

            SkillsSystem skills = new SkillsSystem();
            skills.Init(data);

            BattleSystem battle = new BattleSystem();
            battle.Init(balance, skills);

            Assert.AreEqual(1300f, battle.CalculatePlayerBattlePower(), 0.01f);
            Assert.AreEqual("skill_4", battle.GetTopCombatSkills()[0].skillId);
        }
        finally
        {
            Object.DestroyImmediate(balance);
        }
    }

    [Test]
    public void FocusCoordinator_BlocksStart_WhenPetIsNeglected()
    {
        BalanceConfig balance = ScriptableObject.CreateInstance<BalanceConfig>();

        try
        {
            FocusSystem focusSystem = new FocusSystem();
            SkillsSystem skillsSystem = new SkillsSystem();
            skillsSystem.Init(new SkillsData
            {
                skills =
                {
                    new SkillEntry { id = "skill_focus", name = "Focus", totalSP = 0 }
                }
            });

            ProgressionData progressionData = new ProgressionData { level = 1, xp = 0 };
            ProgressionSystem progressionSystem = new ProgressionSystem(progressionData, 10, 2);
            PetSystem petSystem = new PetSystem(new PetData { hunger = 0f, mood = 0f });
            MissionSystem missionSystem = new MissionSystem();
            CurrencySystem currencySystem = new CurrencySystem(new CurrencyData());

            string feedback = string.Empty;
            FocusCoordinator coordinator = new FocusCoordinator(
                focusSystem,
                skillsSystem,
                progressionSystem,
                petSystem,
                missionSystem,
                currencySystem,
                progressionData,
                new OnboardingData(),
                balance,
                new FocusCoordinatorCallbacks
                {
                    ShowFeedback = message => feedback = message
                });

            bool started = coordinator.TryStartSession("skill_focus", 15);

            Assert.False(started);
            Assert.False(focusSystem.HasActiveSession);
            Assert.AreEqual("Pet neglected. Care first.", feedback);
        }
        finally
        {
            Object.DestroyImmediate(balance);
        }
    }

    [Test]
    public void FocusCoordinator_StartSessionCostsOneMood()
    {
        BalanceConfig balance = ScriptableObject.CreateInstance<BalanceConfig>();

        try
        {
            FocusSystem focusSystem = new FocusSystem();
            SkillsSystem skillsSystem = new SkillsSystem();
            skillsSystem.Init(new SkillsData
            {
                skills =
                {
                    new SkillEntry { id = "skill_focus", name = "Focus", totalSP = 0 }
                }
            });

            ProgressionData progressionData = new ProgressionData { level = 1, xp = 0 };
            ProgressionSystem progressionSystem = new ProgressionSystem(progressionData, 10, 2);
            PetData petData = new PetData { hunger = 50f, mood = 25f };
            PetSystem petSystem = new PetSystem(petData);
            MissionSystem missionSystem = new MissionSystem();
            CurrencySystem currencySystem = new CurrencySystem(new CurrencyData());

            FocusCoordinator coordinator = new FocusCoordinator(
                focusSystem,
                skillsSystem,
                progressionSystem,
                petSystem,
                missionSystem,
                currencySystem,
                progressionData,
                new OnboardingData(),
                balance,
                new FocusCoordinatorCallbacks());

            bool started = coordinator.TryStartSession("skill_focus", 15);

            Assert.True(started);
            Assert.True(focusSystem.HasActiveSession);
            Assert.AreEqual(24f, petData.mood, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(balance);
        }
    }

    [Test]
    public void SkillsSystem_CalculateSkillPointsFromFocusDuration_UsesOnePointPerMinute()
    {
        SkillsSystem skillsSystem = new SkillsSystem();
        skillsSystem.Init(new SkillsData());

        Assert.AreEqual(0, skillsSystem.CalculateSkillPointsFromFocusDuration(59f));
        Assert.AreEqual(1, skillsSystem.CalculateSkillPointsFromFocusDuration(60f));
        Assert.AreEqual(2, skillsSystem.CalculateSkillPointsFromFocusDuration(125f));
    }

    private static SkillEntry CreateSkill(string id, string name, int totalSP, int decayDebtSP, string lastFocusDate)
    {
        return new SkillEntry
        {
            id = id,
            name = name,
            icon = name.Substring(0, Mathf.Min(3, name.Length)).ToUpperInvariant(),
            totalSP = totalSP,
            decayDebtSP = decayDebtSP,
            lastFocusDate = lastFocusDate
        };
    }
}
