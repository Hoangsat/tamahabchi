using NUnit.Framework;

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
    public void SkillProgress_IsCappedAtHundredPercent()
    {
        SkillsData skillsData = new SkillsData();
        skillsData.skills.Add(new SkillEntry
        {
            id = "skill_focus",
            name = "Focus",
            percent = 98f
        });

        SkillsSystem skillsSystem = new SkillsSystem();
        skillsSystem.Init(skillsData);

        SkillProgressResult result = skillsSystem.ApplyFocusProgress("skill_focus", 10f, 1800f, "2026-04-10T10:00:00.0000000Z", 0.05f);

        Assert.True(result.success);
        Assert.AreEqual(2f, result.deltaApplied, 0.001f);
        Assert.AreEqual(100f, result.newPercent, 0.001f);
        Assert.True(result.becameGolden);
    }

    [Test]
    public void PauseResume_RestorePreservesRemainingTimeForPausedSession()
    {
        FocusSystem original = new FocusSystem();
        original.StartFocus(600f, "skill_pause");
        original.Update(120f);
        Assert.True(original.PauseFocus());

        FocusSessionSaveData saveData = original.CreateSaveData("2026-04-10T10:00:00.0000000Z");
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
        FocusSystem original = new FocusSystem();
        original.StartFocus(300f, "skill_restore");
        original.Update(120f);

        FocusSessionSaveData saveData = original.CreateSaveData("2026-04-10T10:00:00.0000000Z");
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
}
