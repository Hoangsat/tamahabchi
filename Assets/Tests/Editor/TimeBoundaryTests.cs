using System;
using System.Collections.Generic;
using NUnit.Framework;

public class TimeBoundaryTests
{
    [Test]
    public void ResetBucket_UsesFiveAmBoundary()
    {
        int beforeReset = TimeService.GetResetBucket(new DateTime(2026, 4, 9, 4, 59, 0));
        int afterReset = TimeService.GetResetBucket(new DateTime(2026, 4, 9, 5, 1, 0));

        Assert.AreEqual(20260408, beforeReset);
        Assert.AreEqual(20260409, afterReset);
    }

    [Test]
    public void ResetWindow_OnlyAdvancesForward()
    {
        int firstWindow = 20260409;
        int nextWindow = 20260410;

        Assert.False(TimeService.ShouldRunDailyReset(firstWindow, firstWindow));
        Assert.True(TimeService.ShouldRunDailyReset(firstWindow, nextWindow));
        Assert.False(TimeService.ShouldRunDailyReset(nextWindow, firstWindow));
        Assert.AreEqual(nextWindow, TimeService.GetEffectiveResetBucket(nextWindow, firstWindow));
    }

    [Test]
    public void PauseResumeAndRestart_AfterResetRemainSingleShot()
    {
        int beforePauseBucket = TimeService.GetResetBucket(new DateTime(2026, 4, 9, 4, 59, 0));
        int afterResumeBucket = TimeService.GetResetBucket(new DateTime(2026, 4, 9, 5, 1, 0));

        Assert.True(TimeService.ShouldRunDailyReset(beforePauseBucket, afterResumeBucket));
        Assert.False(TimeService.ShouldRunDailyReset(afterResumeBucket, afterResumeBucket));
        Assert.AreEqual(afterResumeBucket, TimeService.GetEffectiveResetBucket(afterResumeBucket, beforePauseBucket));
    }

    [Test]
    public void OfflineElapsed_IsClampedForFutureAndVeryLargeValues()
    {
        string futureTimestamp = TimeService.GetUtcNow().AddHours(1).ToString("O");
        string oldTimestamp = TimeService.GetUtcNow().AddDays(-30).ToString("O");

        Assert.AreEqual(0d, TimeService.GetOfflineElapsedSeconds(futureTimestamp, 3600d), 0.001d);
        Assert.AreEqual(TimeService.MaxOfflineElapsedCapSeconds, TimeService.GetOfflineElapsedSeconds(oldTimestamp, 0d), 1d);
        Assert.AreEqual(3600d, TimeService.GetOfflineElapsedSeconds(oldTimestamp, 3600d), 0.001d);
    }

    [Test]
    public void SaveNormalizer_MigratesLegacyMissionResetKeyIntoTopLevelBucket()
    {
        SaveData normalized = SaveNormalizer.Normalize(new SaveData
        {
            missionData = new MissionData
            {
                lastDailyResetKey = "2026-04-08"
            }
        });

        Assert.AreEqual(20260408, normalized.lastResetBucket);
        Assert.AreEqual("2026-04-08", normalized.missionData.lastDailyResetKey);
    }

    [Test]
    public void MissionSystem_ResetsAtMostOncePerBucketAndSupportsSkippedDays()
    {
        MissionData missionData = new MissionData();
        MissionSystem missionSystem = new MissionSystem();
        missionSystem.Init(missionData);

        missionSystem.EnsureDailySkillMissions(new List<SkillEntry>(), 20260409);
        List<string> firstMissionIds = missionData.missions.ConvertAll(mission => mission.missionId);

        missionSystem.EnsureDailySkillMissions(new List<SkillEntry>(), 20260409);
        List<string> repeatedMissionIds = missionData.missions.ConvertAll(mission => mission.missionId);

        missionSystem.EnsureDailySkillMissions(new List<SkillEntry>(), 20260412);

        CollectionAssert.AreEqual(firstMissionIds, repeatedMissionIds);
        Assert.AreEqual("2026-04-12", missionData.lastDailyResetKey);
        Assert.AreEqual(3, missionData.missions.Count);
    }

    [Test]
    public void PetOfflineProgress_AccruesNeglectTimeInsteadOfDeath()
    {
        PetData petData = new PetData
        {
            hunger = 5f,
            mood = 1f,
            energy = 50f,
            statusText = "Happy",
            hasIndependentStats = true
        };

        PetSystem petSystem = new PetSystem(petData);
        float neglectSeconds;

        bool firstApply = petSystem.ApplyOfflineProgress(
            60f,
            1f,
            40f,
            0.5f,
            out neglectSeconds);

        bool secondApply = petSystem.ApplyOfflineProgress(
            60f,
            1f,
            40f,
            0.5f,
            out float secondNeglectSeconds);

        Assert.True(firstApply);
        Assert.True(petSystem.IsNeglected());
        Assert.AreEqual("Neglected", petData.statusText);
        Assert.Greater(neglectSeconds, 0f);
        Assert.False(secondApply);
        Assert.Greater(secondNeglectSeconds, 0f);
    }

    [Test]
    public void FocusCompletion_IsConsumedOnlyOnceAcrossResumeBoundaries()
    {
        FocusSystem focusSystem = new FocusSystem();
        focusSystem.StartFocus(1f, "skill_focus");

        Assert.True(focusSystem.PauseFocus());
        Assert.True(focusSystem.ResumeFocus());
        Assert.True(focusSystem.Update(1.1f));

        Assert.True(focusSystem.TryConsumeCompletedSession(out FocusSessionCompletionData completion));
        Assert.NotNull(completion);
        Assert.False(focusSystem.TryConsumeCompletedSession(out _));
    }
}
