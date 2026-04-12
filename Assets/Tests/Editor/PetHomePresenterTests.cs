using NUnit.Framework;

public class PetHomePresenterTests
{
    [Test]
    public void GetVitalsSummaryText_ClampsAndFormatsValues()
    {
        string text = PetHomePresenter.GetVitalsSummaryText(new PetData
        {
            hunger = 132f,
            mood = -8f
        });

        Assert.AreEqual("Hunger: 100  |  Mood: 0", text);
    }

    [Test]
    public void GetOnboardingHint_UsesNextIncompleteStep()
    {
        Assert.AreEqual("Hint: Claim missions, then buy care items", PetHomePresenter.GetOnboardingHint(new OnboardingData()));
        Assert.AreEqual("Hint: Feed or care for your pet", PetHomePresenter.GetOnboardingHint(new OnboardingData { didBuyFood = true }));
        Assert.AreEqual("Hint: Complete a focus session", PetHomePresenter.GetOnboardingHint(new OnboardingData { didBuyFood = true, didFeed = true }));
        Assert.AreEqual(string.Empty, PetHomePresenter.GetOnboardingHint(new OnboardingData { isCompleted = true, didBuyFood = true, didFeed = true, didFocus = true }));
    }

    [Test]
    public void IsOnboardingComplete_RequiresBuyFeedAndFocus()
    {
        Assert.False(PetHomePresenter.IsOnboardingComplete(new OnboardingData { didBuyFood = true, didFeed = true, didFocus = false }));
        Assert.True(PetHomePresenter.IsOnboardingComplete(new OnboardingData { didBuyFood = true, didFeed = true, didFocus = true }));
    }
}
