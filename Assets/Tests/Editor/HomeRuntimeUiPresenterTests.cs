using NUnit.Framework;

public class HomeRuntimeUiPresenterTests
{
    [Test]
    public void Build_WhenNeglected_DisablesFocusAndShowsCarePrompt()
    {
        HomeRuntimeUiViewData view = HomeRuntimeUiPresenter.Build(
            true,
            false,
            false,
            false,
            true,
            true,
            false,
            true,
            false,
            false,
            0f,
            false,
            12);

        Assert.IsFalse(view.CanFocus);
        Assert.AreEqual("Focus: Care first", view.FocusTimerText);
        Assert.AreEqual("Focus (+12)", view.FocusButtonText);
    }

    [Test]
    public void Build_WhenFocusRunning_ShowsFormattedTimerAndSessionLabel()
    {
        HomeRuntimeUiViewData view = HomeRuntimeUiPresenter.Build(
            true,
            true,
            true,
            true,
            true,
            true,
            true,
            false,
            false,
            true,
            65f,
            true,
            8);

        Assert.IsTrue(view.CanFocus);
        Assert.AreEqual("Focus: 01:05", view.FocusTimerText);
        Assert.AreEqual("Focus Session", view.FocusButtonText);
    }
}
