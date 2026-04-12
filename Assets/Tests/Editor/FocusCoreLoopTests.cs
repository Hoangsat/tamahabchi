using NUnit.Framework;
using System.Threading;

public class FocusCoreLoopTests
{
    [Test]
    public void RewardCalculation_EarlyFinishScalesByCompletionRatio()
    {
        FocusSystem focusSystem = new FocusSystem();

        FocusRewardData reward = focusSystem.BuildReward(
            60f * 60f,
            30f * 60f,
            40,
            20,
            12f,
            6f);

        Assert.AreEqual(0.5f, reward.completionRatio, 0.001f);
        Assert.AreEqual(20, reward.coins);
        Assert.AreEqual(10, reward.xp);
        Assert.AreEqual(6f, reward.energy, 0.001f);
        Assert.AreEqual(3f, reward.mood, 0.001f);
    }

    [Test]
    public void SkillProgress_ReachesMaxLevelAndMaxAxis()
    {
        SkillsData skillsData = new SkillsData();
        skillsData.skills.Add(new SkillEntry
        {
            id = "skill_focus",
            name = "Focus",
            totalSP = SkillProgressionModel.GetTotalSPForMaxLevel() - 50
        });

        SkillsSystem skillsSystem = new SkillsSystem();
        skillsSystem.Init(skillsData);

        SkillProgressResult result = skillsSystem.ApplySkillPoints("skill_focus", 500, 1800f, "2026-04-10T10:00:00.0000000Z", 0.05f);

        Assert.True(result.success);
        Assert.Greater(result.deltaSP, 0);
        Assert.AreEqual(SkillProgressionModel.MaxLevel, result.newLevel);
        Assert.AreEqual(100f, result.newAxisPercent, 0.001f);
        Assert.True(result.becameGolden);
    }

    [Test]
    public void PauseResume_RestorePreservesRemainingTimeForPausedSession()
    {
        FocusSessionSaveData saveData = new FocusSessionSaveData
        {
            state = FocusSessionState.Paused,
            skillId = "skill_pause",
            configuredDurationSeconds = 600f,
            elapsedSeconds = 120f,
            savedAtUtc = "2026-04-10T10:00:00.0000000Z"
        };
        FocusSystem restored = new FocusSystem();

        bool restoredOk = restored.RestoreSession(saveData, 1800d, out bool completedWhileOffline);

        Assert.True(restoredOk);
        Assert.False(completedWhileOffline);
        Assert.AreEqual(FocusSessionState.Paused, restored.State);
        Assert.AreEqual(480f, restored.GetRemainingTime(), 0.01f);
    }

    [Test]
    public void RestoreCompletesSessionWhenOfflineElapsedExceedsRemainingTime()
    {
        FocusSessionSaveData saveData = new FocusSessionSaveData
        {
            state = FocusSessionState.Running,
            skillId = "skill_restore",
            configuredDurationSeconds = 300f,
            elapsedSeconds = 120f,
            savedAtUtc = "2026-04-10T10:00:00.0000000Z"
        };
        FocusSystem restored = new FocusSystem();

        bool restoredOk = restored.RestoreSession(saveData, 300d, out bool completedWhileOffline);

        Assert.True(restoredOk);
        Assert.True(completedWhileOffline);
        Assert.True(restored.TryConsumeCompletedSession(out FocusSessionCompletionData completion));
        Assert.NotNull(completion);
        Assert.AreEqual("skill_restore", completion.skillId);
        Assert.AreEqual(300f, completion.actualDurationSeconds, 0.01f);
        Assert.True(completion.completedNaturally);
    }

    [Test]
    public void Update_PrefersRealtimeDeltaOverLargeScaledDelta()
    {
        FocusSystem focusSystem = new FocusSystem();
        focusSystem.StartFocus(1000f, "skill_timing");

        Thread.Sleep(25);
        Assert.False(focusSystem.Update(100f));

        float elapsed = focusSystem.GetElapsedTime();
        Assert.Less(elapsed, 1f);
    }
}
