using NUnit.Framework;
using UnityEngine;

public class HomeDetailsCoordinatorTests
{
    [Test]
    public void GetPetStatusSummary_UsesUnavailableFallbackWithoutPetFlowCoordinator()
    {
        HomeDetailsCoordinator coordinator = new HomeDetailsCoordinator(
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        PetStatusSummary summary = coordinator.GetPetStatusSummary();

        Assert.AreEqual("Pet status unavailable", summary.headline);
        Assert.True(summary.needsAttention);
    }

    [Test]
    public void GetView_BuildsStatsTabFromRuntimeState()
    {
        BalanceConfig balance = ScriptableObject.CreateInstance<BalanceConfig>();
        balance.lowHungerMoodThreshold = 30f;
        balance.lowEnergyMoodThreshold = 20f;

        try
        {
            PetData petData = new PetData { hunger = 49.6f, mood = 19.5f, energy = 70f };
            CurrencyData currencyData = new CurrencyData { coins = 15 };
            ProgressionData progressionData = new ProgressionData { level = 2, xp = 12 };
            RoomData roomData = new RoomData { roomLevel = 1 };
            SkillsData skillsData = new SkillsData
            {
                skills =
                {
                    new SkillEntry { id = "s1", name = "Coding" },
                    new SkillEntry { id = "s2", name = "Art" }
                }
            };

            PetFlowCoordinator petFlowCoordinator = new PetFlowCoordinator(
                new PetSystem(petData),
                null,
                balance,
                new PetFlowCoordinatorCallbacks());
            petFlowCoordinator.ResetRuntimeState(false);

            SkillsSystem skillsSystem = new SkillsSystem();
            skillsSystem.Init(skillsData);

            HomeDetailsCoordinator coordinator = new HomeDetailsCoordinator(
                petFlowCoordinator,
                new CurrencySystem(currencyData),
                currencyData,
                petData,
                progressionData,
                roomData,
                skillsSystem);

            HomeDetailsViewData view = coordinator.GetView(HomeDetailsTab.Stats);

            Assert.AreEqual("Stats", view.Title);
            StringAssert.Contains("Hunger: 50", view.Body);
            StringAssert.Contains("Mood: 20", view.Body);
            StringAssert.Contains("Coins: 15", view.Body);
            Assert.AreEqual("Low Mood  •  Coins 15", view.Subtitle);
        }
        finally
        {
            Object.DestroyImmediate(balance);
        }
    }
}
