using NUnit.Framework;

public class MissionHudPresenterTests
{
    [Test]
    public void Build_NullMission_ReturnsFallbackState()
    {
        MissionHudSlotViewData view = MissionHudPresenter.Build(null);

        Assert.AreEqual("No mission", view.DisplayText);
        Assert.False(view.CanClaim);
    }

    [Test]
    public void Build_MinuteSkillMission_FormatsMinutesAndCompletionState()
    {
        MissionEntryData mission = new MissionEntryData
        {
            title = "Math Sprint",
            requiredMinutes = 30f,
            progressMinutes = 18f,
            isCompleted = true,
            isClaimed = false
        };

        MissionHudSlotViewData view = MissionHudPresenter.Build(mission);

        Assert.AreEqual("Math Sprint: 18 / 30 min [Completed]", view.DisplayText);
        Assert.True(view.CanClaim);
    }

    [Test]
    public void Build_SessionMissionWithoutTitle_FormatsDerivedLabel()
    {
        MissionEntryData mission = new MissionEntryData
        {
            targetSkillName = "Coding",
            skillMissionMode = "sessions",
            currentProgress = 2,
            targetProgress = 5
        };

        MissionHudSlotViewData view = MissionHudPresenter.Build(mission);

        Assert.AreEqual("Complete 5 focus sessions on Coding: 2 / 5 sessions", view.DisplayText);
        Assert.False(view.CanClaim);
    }
}
