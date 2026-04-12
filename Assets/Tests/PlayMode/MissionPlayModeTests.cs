using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class MissionPlayModeTests
{
    private const string MainSceneName = "Main";

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        new SaveManager().Reset();
        yield return SceneManager.LoadSceneAsync(MainSceneName, LoadSceneMode.Single);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        new SaveManager().Reset();
        yield return null;
    }

    [UnityTest]
    public IEnumerator MainScene_MissionRoutineFlow_CreateCompleteAndReturnHome_Works()
    {
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        AppShellUI shell = Object.FindAnyObjectByType<AppShellUI>();
        MissionPanelUI missionPanel = Object.FindAnyObjectByType<MissionPanelUI>();

        Assert.NotNull(manager);
        Assert.NotNull(shell);
        Assert.NotNull(missionPanel);

        SkillEntry skill = manager.AddSkill("Coding", "DEV");
        Assert.NotNull(skill);
        manager.currencyData.coins = 100;
        manager.petData.mood = 30f;
        manager.petData.energy = 25f;
        yield return null;

        Assert.True(shell.OpenMissions());
        yield return null;

        Assert.True(missionPanel.IsPanelVisible());
        Assert.False(manager.feedButton != null && manager.feedButton.gameObject.activeSelf);

        int coinsBefore = manager.GetCurrentCoins();
        float moodBefore = manager.petData.mood;
        float energyBefore = manager.petData.energy;

        bool created = manager.CreateRoutineMission("Stretch", 12, 4, 5, 15, skill.id, out string createMessage);
        Assert.True(created, createMessage);

        yield return null;

        MissionEntryData routine = manager.GetRoutineMissions().Find(entry => entry != null && entry.title == "Stretch");
        Assert.NotNull(routine);
        Assert.False(routine.isClaimed);

        bool completed = manager.CompleteRoutineMission(routine.missionId, out string completeMessage);
        Assert.True(completed, completeMessage);
        yield return null;

        MissionEntryData completedRoutine = manager.GetRoutineMissions().Find(entry => entry != null && entry.missionId == routine.missionId);
        Assert.NotNull(completedRoutine);
        Assert.True(completedRoutine.isCompleted);
        Assert.True(completedRoutine.isClaimed);
        Assert.AreEqual(coinsBefore + 12, manager.GetCurrentCoins());
        Assert.Greater(manager.petData.mood, moodBefore);
        Assert.Greater(manager.petData.energy, energyBefore);

        SkillProgressionViewData skillView = manager.GetSkillProgressionView(skill.id);
        Assert.NotNull(skillView);
        Assert.GreaterOrEqual(skillView.totalSP, 15);

        bool completedAgain = manager.CompleteRoutineMission(routine.missionId, out _);
        Assert.False(completedAgain);

        Assert.True(shell.OpenHome());
        yield return null;

        Assert.False(missionPanel.IsPanelVisible());
        Assert.True(manager.feedButton != null && manager.feedButton.gameObject.activeSelf);
    }
}
