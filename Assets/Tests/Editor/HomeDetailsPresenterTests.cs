using NUnit.Framework;

public class HomeDetailsPresenterTests
{
    [Test]
    public void Build_PetTab_UsesSummaryAndProgressCopy()
    {
        HomeDetailsViewData view = HomeDetailsPresenter.Build(
            HomeDetailsTab.Pet,
            new PetStatusSummary { headline = "Calm", guidance = "Keep the streak going." },
            42,
            3,
            1,
            7,
            80f,
            66f,
            0,
            0);

        Assert.AreEqual("Calm  •  Coins 42", view.Subtitle);
        Assert.AreEqual("Pet", view.Title);
        StringAssert.Contains("Guidance: Keep the streak going.", view.Body);
        StringAssert.Contains("Tracked skills: 7", view.Body);
        Assert.AreEqual("Scaffold panel inspired by the Home detail UX reference.", view.Footer);
    }

    [Test]
    public void Build_StatsTab_FormatsRoundedCoreStats()
    {
        HomeDetailsViewData view = HomeDetailsPresenter.Build(
            HomeDetailsTab.Stats,
            new PetStatusSummary { headline = "Stable", guidance = "All good." },
            15,
            2,
            1,
            5,
            49.6f,
            19.5f,
            12,
            30);

        Assert.AreEqual("Stats", view.Title);
        StringAssert.Contains("Hunger: 50", view.Body);
        StringAssert.Contains("Mood: 20", view.Body);
        StringAssert.DoesNotContain("XP:", view.Body);
        StringAssert.Contains("Coins: 15", view.Body);
        Assert.AreEqual("Scaffold panel inspired by the Home detail UX reference.", view.Footer);
    }

    [Test]
    public void BuildUnavailable_ReturnsFallbackCopy()
    {
        HomeDetailsViewData view = HomeDetailsPresenter.BuildUnavailable(HomeDetailsTab.Relic);

        Assert.AreEqual("Pet profile unavailable", view.Subtitle);
        Assert.AreEqual("Relic", view.Title);
        Assert.AreEqual("GameManager missing.", view.Body);
        Assert.AreEqual(string.Empty, view.Footer);
    }
}
