using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class MissionPanelCoordinatorTests
{
    [Test]
    public void GetViewState_FormatsHeaderFromMissionProgress()
    {
        GameObject host = new GameObject("GameManagerHost");
        try
        {
            GameManager manager = host.AddComponent<GameManager>();
            manager.balanceConfig = ScriptableObject.CreateInstance<BalanceConfig>();
            manager.progressionData = new ProgressionData { level = 1, xp = 0 };
            manager.currencyData = new CurrencyData { coins = 50 };
            manager.petData = new PetData { hunger = 50f, mood = 50f, energy = 50f };
            manager.inventoryData = new InventoryData();
            manager.skillsData = new SkillsData
            {
                skills =
                {
                    new SkillEntry { id = "math", name = "Math" }
                }
            };
            manager.roomData = new RoomData();
            manager.missionData = new MissionData();
            manager.dailyRewardData = new DailyRewardData();
            manager.onboardingData = new OnboardingData();
            manager.focusStateData = new FocusStateData();

            MethodInfo initializeMethod = typeof(GameManager).GetMethod("InitializeRuntimeSystems", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(initializeMethod);
            initializeMethod.Invoke(manager, null);

            manager.CreateCustomSkillMission("math", 15, out _);
            manager.SelectMission(manager.GetSkillMissions()[0].missionId, out _);

            MissionPanelCoordinator coordinator = new MissionPanelCoordinator(manager);
            MissionPanelViewState view = coordinator.GetViewState();

            Assert.AreEqual("Missions", view.Title);
            StringAssert.StartsWith("Reset in: ", view.ResetInfo);
            Assert.AreEqual("0/5 tracked missions completed", view.HeaderStats);
            Assert.GreaterOrEqual(view.SkillMissions.Count, 1);
            Assert.True(view.SkillMissions.Exists(m => m != null && !string.IsNullOrEmpty(m.title) && m.title.Contains("Math")));
            Assert.True(view.SkillMissions.Exists(m => m != null && m.isSelected));
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void ToggleSkillTracking_SelectsMissionAndReturnsUiMessage()
    {
        GameObject host = new GameObject("GameManagerHost");
        try
        {
            GameManager manager = host.AddComponent<GameManager>();
            manager.balanceConfig = ScriptableObject.CreateInstance<BalanceConfig>();
            manager.progressionData = new ProgressionData { level = 1, xp = 0 };
            manager.currencyData = new CurrencyData { coins = 50 };
            manager.petData = new PetData { hunger = 50f, mood = 50f, energy = 50f };
            manager.inventoryData = new InventoryData();
            manager.skillsData = new SkillsData
            {
                skills =
                {
                    new SkillEntry { id = "math", name = "Math" }
                }
            };
            manager.roomData = new RoomData();
            manager.missionData = new MissionData();
            manager.dailyRewardData = new DailyRewardData();
            manager.onboardingData = new OnboardingData();
            manager.focusStateData = new FocusStateData();

            MethodInfo initializeMethod = typeof(GameManager).GetMethod("InitializeRuntimeSystems", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(initializeMethod);
            initializeMethod.Invoke(manager, null);

            manager.CreateCustomSkillMission("math", 15, out _);
            string missionId = manager.GetSkillMissions()[0].missionId;

            MissionPanelCoordinator coordinator = new MissionPanelCoordinator(manager);
            MissionPanelActionResult result = coordinator.ToggleSkillTracking(missionId, true);

            Assert.True(result.Success);
            Assert.AreEqual("Mission tracking enabled", result.Message);
            Assert.True(manager.GetSkillMissions()[0].isSelected);
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }
}
