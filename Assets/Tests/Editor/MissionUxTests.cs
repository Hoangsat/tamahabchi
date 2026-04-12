using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

public class MissionUxTests
{
    [Test]
    public void FocusProgress_UpdatesAllSelectedMatchingSkillMissions()
    {
        MissionSystem missionSystem = CreateMissionSystem();

        MissionCreationResult shortMath = missionSystem.CreateSkillMission("math", "Math", 15, 20);
        MissionCreationResult longMath = missionSystem.CreateSkillMission("math", "Math", 30, 30);
        MissionCreationResult artMission = missionSystem.CreateSkillMission("art", "Art", 15, 20);

        SkillMissionProgressResult progress = missionSystem.ApplySkillFocusProgress("math", 20f, false);

        Assert.True(progress.anyProgress);
        MissionEntryData shortMission = FindMission(missionSystem.GetSkillMissions(), shortMath.createdMission.missionId);
        MissionEntryData longMission = FindMission(missionSystem.GetSkillMissions(), longMath.createdMission.missionId);
        MissionEntryData otherMission = FindMission(missionSystem.GetSkillMissions(), artMission.createdMission.missionId);

        Assert.NotNull(shortMission);
        Assert.NotNull(longMission);
        Assert.NotNull(otherMission);
        Assert.AreEqual(15f, shortMission.progressMinutes, 0.01f);
        Assert.True(shortMission.isCompleted);
        Assert.AreEqual(20f, longMission.progressMinutes, 0.01f);
        Assert.False(longMission.isCompleted);
        Assert.AreEqual(0f, otherMission.progressMinutes, 0.01f);
    }

    [Test]
    public void ClaimFlow_CompletedSkillMissionCanOnlyBeClaimedOnce()
    {
        MissionSystem missionSystem = CreateMissionSystem();
        MissionCreationResult create = missionSystem.CreateSkillMission("math", "Math", 15, 25);

        missionSystem.ApplySkillFocusProgress("math", 15f, false);

        MissionClaimResult firstClaim = missionSystem.ClaimMission(create.createdMission.missionId);
        MissionClaimResult secondClaim = missionSystem.ClaimMission(create.createdMission.missionId);

        Assert.True(firstClaim.success);
        Assert.AreEqual(25, firstClaim.rewardCoins);
        StringAssert.Contains("Math", firstClaim.sourceTitle);
        Assert.False(secondClaim.success);
    }

    [Test]
    public void RoutineCompletion_CompletesAndClaimsImmediately()
    {
        MissionSystem missionSystem = CreateMissionSystem();
        MissionCreationResult create = missionSystem.CreateRoutine("Stretch", 12, 4, 5, 15, "math");

        MissionClaimResult result = missionSystem.CompleteRoutine(create.createdMission.missionId);
        MissionEntryData routine = FindMission(missionSystem.GetRoutineMissions(), create.createdMission.missionId);

        Assert.True(result.success);
        Assert.AreEqual(12, result.rewardCoins);
        Assert.AreEqual(4, result.rewardMood);
        Assert.AreEqual(5, result.rewardEnergy);
        Assert.AreEqual(15, result.rewardSkillSP);
        Assert.NotNull(routine);
        Assert.True(routine.isCompleted);
        Assert.True(routine.isClaimed);
    }

    [Test]
    public void DailyGenericMissions_AreNotExposedAsManualRoutines()
    {
        MissionSystem missionSystem = CreateMissionSystem();
        int resetBucket = TimeService.GetResetBucket(new DateTime(2026, 4, 10, 6, 0, 0));

        missionSystem.EnsureDailySkillMissions(new List<SkillEntry>(), resetBucket);

        List<MissionEntryData> routines = missionSystem.GetRoutineMissions();
        MissionEntryData genericMission = missionSystem.GetActiveMissions().Find(mission =>
            mission != null
            && !string.Equals(mission.missionType, "routine", StringComparison.Ordinal)
            && !string.Equals(mission.missionType, "skill_focus", StringComparison.Ordinal));

        Assert.IsEmpty(routines);
        Assert.NotNull(genericMission);

        MissionClaimResult result = missionSystem.CompleteRoutine(genericMission.missionId);
        MissionEntryData refreshed = FindMission(missionSystem.GetActiveMissions(), genericMission.missionId);

        Assert.False(result.success);
        Assert.NotNull(refreshed);
        Assert.False(refreshed.isCompleted);
        Assert.False(refreshed.isClaimed);
    }

    [Test]
    public void EligibleCandidates_IncludeGenericWorkMission()
    {
        MissionSystem missionSystem = CreateMissionSystem();
        MethodInfo method = typeof(MissionSystem).GetMethod("BuildEligibleMissionCandidates", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        IList candidates = method.Invoke(
            missionSystem,
            new object[] { new List<SkillEntry>(), MissionDifficulty.Easy, new List<MissionEntryData>(), new HashSet<string>(), -1, "test" }) as IList;

        Assert.NotNull(candidates);

        bool hasWorkCandidate = false;
        for (int i = 0; i < candidates.Count; i++)
        {
            object candidate = candidates[i];
            FieldInfo keyField = candidate.GetType().GetField("key", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            string key = keyField != null ? keyField.GetValue(candidate) as string : null;
            if (string.Equals(key, "work:Easy", StringComparison.Ordinal))
            {
                hasWorkCandidate = true;
                break;
            }
        }

        Assert.True(hasWorkCandidate);
    }

    [Test]
    public void Bonus_BecomesClaimableAfterFiveOfFiveSelectedSkillMissions()
    {
        MissionSystem missionSystem = CreateMissionSystem();
        List<string> missionIds = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            MissionCreationResult create = missionSystem.CreateSkillMission("math", "Math", 15, 20);
            missionIds.Add(create.createdMission.missionId);
        }

        missionSystem.ApplySkillFocusProgress("math", 15f, false);
        MissionBonusStatus beforeClaim = missionSystem.GetSkillMissionBonusStatus();
        MissionClaimResult claim = missionSystem.ClaimSkillMissionBonus();
        MissionBonusStatus afterClaim = missionSystem.GetSkillMissionBonusStatus();

        Assert.True(beforeClaim.isReady);
        Assert.AreEqual(5, beforeClaim.selectedSkillMissionCount);
        Assert.AreEqual(5, beforeClaim.completedSelectedSkillMissionCount);
        Assert.True(claim.success);
        Assert.AreEqual(30, claim.rewardCoins);
        Assert.AreEqual(1, claim.rewardChestCount);
        Assert.True(afterClaim.isClaimed);
    }

    [Test]
    public void DailyResetAtFiveAm_RegeneratesMissionsAndResetsBonusAndRoutineCounters()
    {
        MissionData missionData = new MissionData();
        MissionSystem missionSystem = new MissionSystem();
        missionSystem.Init(missionData);

        int beforeResetBucket = TimeService.GetResetBucket(new DateTime(2026, 4, 10, 4, 59, 0));
        int afterResetBucket = TimeService.GetResetBucket(new DateTime(2026, 4, 10, 5, 1, 0));

        missionSystem.EnsureDailySkillMissions(new List<SkillEntry>(), beforeResetBucket);
        MissionCreationResult routine = missionSystem.CreateRoutine("Brush teeth", 5, 1, 1, 0, string.Empty);
        missionData.skillBonusClaimed = true;
        missionData.customRoutineCreateCount = 4;

        missionSystem.EnsureDailySkillMissions(new List<SkillEntry>(), afterResetBucket);

        CollectionAssert.DoesNotContain(missionData.missions.ConvertAll(mission => mission.missionId), routine.createdMission.missionId);
        Assert.AreEqual("2026-04-10", missionData.lastDailyResetKey);
        Assert.False(missionData.skillBonusClaimed);
        Assert.AreEqual(0, missionData.customRoutineCreateCount);
    }

    private static MissionSystem CreateMissionSystem()
    {
        MissionSystem missionSystem = new MissionSystem();
        missionSystem.Init(new MissionData());
        return missionSystem;
    }

    private static MissionEntryData FindMission(List<MissionEntryData> missions, string missionId)
    {
        return missions.Find(mission => mission != null && mission.missionId == missionId);
    }
}
