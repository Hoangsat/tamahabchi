using NUnit.Framework;
using UnityEngine;

public class BattleCoordinatorTests
{
    [Test]
    public void GetAvailability_ReportsMissingSkillsAndEnergy()
    {
        BalanceConfig balance = ScriptableObject.CreateInstance<BalanceConfig>();
        balance.bossDefinitions = BattleSystem.CreateDefaultBossDefinitions();

        try
        {
            SkillsSystem skills = new SkillsSystem();
            skills.Init(new SkillsData
            {
                skills =
                {
                    CreateSkill("skill_1", "Coding", 120),
                    CreateSkill("skill_2", "Art", 80)
                }
            });

            BattleSystem battleSystem = new BattleSystem();
            battleSystem.Init(balance, skills);

            BattleCoordinator coordinator = new BattleCoordinator(
                battleSystem,
                new PetSystem(new PetData { energy = 5f }),
                new CurrencySystem(new CurrencyData()),
                new BattleCoordinatorCallbacks());

            BattleAvailabilityData availability = coordinator.GetAvailability();

            Assert.False(availability.CanFight);
            Assert.False(availability.HasEnoughSkills);
            Assert.False(availability.HasEnoughEnergy);
            StringAssert.Contains("Track at least 3 skills", availability.blockedReason);
            StringAssert.Contains("top 3 skills", availability.guidance);
        }
        finally
        {
            Object.DestroyImmediate(balance);
        }
    }

    [Test]
    public void ResolveBattle_OnWinConsumesEnergyAwardsCoinsAndSaves()
    {
        BalanceConfig balance = ScriptableObject.CreateInstance<BalanceConfig>();
        balance.bossDefinitions = BattleSystem.CreateDefaultBossDefinitions();

        try
        {
            SkillsSystem skills = new SkillsSystem();
            skills.Init(new SkillsData
            {
                skills =
                {
                    CreateSkill("skill_1", "Coding", 120),
                    CreateSkill("skill_2", "Art", 100),
                    CreateSkill("skill_3", "Music", 90)
                }
            });

            BattleSystem battleSystem = new BattleSystem();
            battleSystem.Init(balance, skills);

            PetData petData = new PetData { energy = 40f, hunger = 80f, mood = 70f };
            CurrencyData currencyData = new CurrencyData { coins = 10 };
            bool petBroadcast = false;
            bool coinsBroadcast = false;
            bool saved = false;
            bool uiUpdated = false;

            BattleCoordinator coordinator = new BattleCoordinator(
                battleSystem,
                new PetSystem(petData),
                new CurrencySystem(currencyData),
                new BattleCoordinatorCallbacks
                {
                    BroadcastPetStateChanged = () => petBroadcast = true,
                    OnCoinsChanged = () => coinsBroadcast = true,
                    SaveGame = () => saved = true,
                    UpdateUi = () => uiUpdated = true
                });

            BattleResultData result = coordinator.ResolveBattle("boss_01");

            Assert.False(result.wasBlocked);
            Assert.AreEqual(BattleOutcome.Win, result.result);
            Assert.AreEqual(30, result.rewardCoins);
            Assert.AreEqual(30f, petData.energy, 0.01f);
            Assert.AreEqual(40, currencyData.coins);
            Assert.True(petBroadcast);
            Assert.True(coinsBroadcast);
            Assert.True(saved);
            Assert.True(uiUpdated);
            StringAssert.Contains("Next target:", result.adviceMessage);
        }
        finally
        {
            Object.DestroyImmediate(balance);
        }
    }

    private static SkillEntry CreateSkill(string id, string name, int totalSP)
    {
        return new SkillEntry
        {
            id = id,
            name = name,
            totalSP = totalSP,
            lastFocusDate = "2026-04-12T00:00:00"
        };
    }
}
