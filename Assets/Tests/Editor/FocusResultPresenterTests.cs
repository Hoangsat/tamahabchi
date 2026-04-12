using NUnit.Framework;

public class FocusResultPresenterTests
{
    [Test]
    public void Build_ForCompletedEarly_ReturnsEarlyCopy()
    {
        FocusSessionResultData result = new FocusSessionResultData
        {
            outcome = FocusSessionOutcome.CompletedEarly,
            skillName = "Coding",
            skillIcon = "DEV",
            plannedDurationSeconds = 1800f,
            actualDurationSeconds = 900f,
            skillSpReward = 15,
            previousLevel = 1,
            newLevel = 1,
            previousAxisPercent = 12f,
            newAxisPercent = 13.5f,
            coinsReward = 8,
            petReaction = "Still happy."
        };

        FocusResultViewData view = FocusResultPresenter.Build(result);

        Assert.AreEqual("Completed Early", view.Title);
        StringAssert.Contains("15 of 30 min", view.Subtitle);
        Assert.AreEqual("DEV Coding", view.Skill);
        StringAssert.Contains("+15 SP", view.Progress);
        StringAssert.Contains("Coins: +8", view.Reward);
        StringAssert.Contains("Pet: Still happy.", view.Pet);
    }

    [Test]
    public void Build_ForGoldenLevelUp_UsesGoldenTitleAndRewardNote()
    {
        FocusSessionResultData result = new FocusSessionResultData
        {
            outcome = FocusSessionOutcome.Completed,
            skillName = "Writing",
            skillIcon = "WRT",
            plannedDurationSeconds = 3600f,
            actualDurationSeconds = 3600f,
            skillSpReward = 120,
            previousLevel = 9,
            newLevel = 10,
            previousAxisPercent = 92f,
            newAxisPercent = 100f,
            coinsReward = 25,
            petReaction = "Proud.",
            becameGolden = true,
            isGolden = true
        };

        FocusResultViewData view = FocusResultPresenter.Build(result);

        Assert.AreEqual("Skill Maxed", view.Title);
        StringAssert.Contains("full 60 min session", view.Subtitle);
        StringAssert.Contains("Lv.9 -> Lv.10", view.Progress);
        StringAssert.Contains("Golden state unlocked.", view.Reward);
    }

    [Test]
    public void Build_WithLowEnergyPenalty_UsesPenaltyPetCopy()
    {
        FocusSessionResultData result = new FocusSessionResultData
        {
            outcome = FocusSessionOutcome.Completed,
            skillName = "Art",
            plannedDurationSeconds = 1200f,
            actualDurationSeconds = 1200f,
            skillSpReward = 10,
            newLevel = 2,
            coinsReward = 5,
            petReaction = "Tired but okay.",
            lowEnergyPenaltyApplied = true
        };

        FocusResultViewData view = FocusResultPresenter.Build(result);

        StringAssert.Contains("Low energy penalty applied", view.Pet);
    }
}
