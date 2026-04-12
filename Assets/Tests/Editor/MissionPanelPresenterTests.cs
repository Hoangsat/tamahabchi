using NUnit.Framework;
using UnityEngine;

public class MissionPanelPresenterTests
{
    [Test]
    public void BuildBonusCard_WhenReadyAndUnclaimed_UsesReadyState()
    {
        MissionBonusStatus bonus = new MissionBonusStatus
        {
            completedSelectedSkillMissionCount = 5,
            isReady = true,
            isClaimed = false
        };

        MissionPanelBonusCardViewData view = MissionPanelPresenter.BuildBonusCard(bonus);

        Assert.AreEqual("5/5 Skill Mission Bonus Ready", view.TitleText);
        Assert.AreEqual("All tracked missions are complete. Claim the bonus now.", view.ProgressText);
        Assert.AreEqual("Claim Bonus", view.ClaimButtonText);
        Assert.True(view.CanClaim);
        Assert.AreEqual(new Color(0.63f, 0.96f, 0.72f, 1f), view.ProgressColor);
    }

    [Test]
    public void BuildRoutineCostText_WhenZero_ReturnsFreeCopy()
    {
        Assert.AreEqual("Routine cost: free", MissionPanelPresenter.BuildRoutineCostText(0));
    }

    [Test]
    public void BuildSkillMissionDraft_WithSkill_ReturnsReadySummary()
    {
        MissionPanelPopupDraftViewData view = MissionPanelPresenter.BuildSkillMissionDraft("Coding", 45);

        Assert.AreEqual("Create Skill Mission", view.TitleText);
        StringAssert.Contains("Coding", view.StatusText);
        StringAssert.Contains("45 min", view.StatusText);
    }

    [Test]
    public void BuildRoutineDraft_IncludesRewardBundleAndCost()
    {
        MissionPanelPopupDraftViewData view = MissionPanelPresenter.BuildRoutineDraft(
            "Hydrate",
            12,
            3,
            2,
            25,
            "Writing",
            40);

        Assert.AreEqual("Create Routine", view.TitleText);
        StringAssert.Contains("Hydrate", view.StatusText);
        StringAssert.Contains("12 coins", view.StatusText);
        StringAssert.Contains("25 SP to Writing", view.StatusText);
        StringAssert.Contains("Cost 40 coins", view.StatusText);
    }
}
