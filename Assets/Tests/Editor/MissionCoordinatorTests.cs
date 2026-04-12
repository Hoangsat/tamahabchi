using NUnit.Framework;
using UnityEngine;

public class MissionCoordinatorTests
{
    [Test]
    public void SelectMission_OnSuccessSavesAndRefreshesMissionUi()
    {
        MissionSystem missionSystem = new MissionSystem();
        missionSystem.Init(new MissionData());
        MissionCreationResult created = missionSystem.CreateSkillMission("math", "Math", 15, 20);

        bool saved = false;
        bool refreshed = false;

        MissionCoordinator coordinator = new MissionCoordinator(
            missionSystem,
            null,
            null,
            null,
            null,
            null,
            new MissionCoordinatorCallbacks
            {
                SaveGame = () => saved = true,
                UpdateMissionUi = () => refreshed = true
            });

        bool success = coordinator.SelectMission(created.createdMission.missionId, out string message);

        Assert.True(success);
        Assert.AreEqual(string.Empty, message);
        Assert.True(saved);
        Assert.True(refreshed);
    }

    [Test]
    public void ApplyClaimResult_AwardsRewardsAndEmitsCallbacks()
    {
        BalanceConfig balance = ScriptableObject.CreateInstance<BalanceConfig>();

        try
        {
            SkillsData skillsData = new SkillsData
            {
                skills =
                {
                    new SkillEntry
                    {
                        id = "math",
                        name = "Math",
                        totalSP = 0,
                        lastFocusDate = string.Empty
                    }
                }
            };

            SkillsSystem skillsSystem = new SkillsSystem();
            skillsSystem.Init(skillsData);

            PetData petData = new PetData { hunger = 40f, mood = 20f, energy = 50f };
            CurrencyData currencyData = new CurrencyData { coins = 2 };
            bool coinsChanged = false;
            bool petChanged = false;
            bool skillsChanged = false;
            bool saved = false;
            SkillProgressResult progress = null;
            string feedback = string.Empty;

            MissionCoordinator coordinator = new MissionCoordinator(
                null,
                skillsSystem,
                null,
                new CurrencySystem(currencyData),
                new PetSystem(petData),
                balance,
                new MissionCoordinatorCallbacks
                {
                    OnCoinsChanged = () => coinsChanged = true,
                    BroadcastPetStateChanged = () => petChanged = true,
                    OnSkillsChanged = () => skillsChanged = true,
                    OnSkillProgressAdded = result => progress = result,
                    SaveGame = () => saved = true,
                    ShowFeedback = message => feedback = message
                });

            coordinator.ApplyClaimResult(
                new MissionClaimResult
                {
                    success = true,
                    rewardCoins = 10,
                    rewardMood = 5,
                    rewardSkillSP = 12,
                    rewardSkillId = "math",
                    sourceTitle = "Focus Mission"
                },
                true);

            Assert.AreEqual(12, currencyData.coins);
            Assert.AreEqual(25f, petData.mood, 0.01f);
            Assert.AreEqual(12, skillsSystem.GetSkillById("math").totalSP);
            Assert.True(coinsChanged);
            Assert.True(petChanged);
            Assert.True(skillsChanged);
            Assert.True(saved);
            Assert.NotNull(progress);
            Assert.AreEqual(12, progress.deltaSP);
            Assert.AreEqual("Focus Mission: +10 Coins, +5 Mood, +12 SP", feedback);
        }
        finally
        {
            Object.DestroyImmediate(balance);
        }
    }
}
