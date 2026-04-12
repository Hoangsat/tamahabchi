using NUnit.Framework;

public class GameSaveLifecycleCoordinatorTests
{
    [Test]
    public void HandleApplicationPause_WhenPausedAndInitialized_Saves()
    {
        int saveCalls = 0;
        GameSaveLifecycleCoordinator coordinator = new GameSaveLifecycleCoordinator(_ => { }, null);

        coordinator.HandleApplicationPause(true, false, true, () => saveCalls++, null);

        Assert.AreEqual(1, saveCalls);
    }

    [Test]
    public void HandleApplicationPause_WhenResuming_InvokesResumeAction()
    {
        int resumeCalls = 0;
        GameSaveLifecycleCoordinator coordinator = new GameSaveLifecycleCoordinator(_ => { }, null);

        coordinator.HandleApplicationPause(false, false, true, null, () => resumeCalls++);

        Assert.AreEqual(1, resumeCalls);
    }

    [Test]
    public void HandleApplicationQuit_WhenInitialized_Saves()
    {
        int saveCalls = 0;
        GameSaveLifecycleCoordinator coordinator = new GameSaveLifecycleCoordinator(_ => { }, null);

        coordinator.HandleApplicationQuit(false, true, () => saveCalls++);

        Assert.AreEqual(1, saveCalls);
    }

    [Test]
    public void ResetGame_UpdatesFlagsAndInvokesCallbacksInOrder()
    {
        bool isResetting = false;
        bool isInitialized = true;
        bool justReset = false;
        int resetPersistentCalls = 0;
        int reinitializeCalls = 0;
        int feedbackCalls = 0;
        int saveCalls = 0;
        string lastLog = null;

        GameSaveLifecycleCoordinator coordinator = new GameSaveLifecycleCoordinator(
            _ => { },
            () => resetPersistentCalls++);

        coordinator.ResetGame(
            ref isResetting,
            ref isInitialized,
            ref justReset,
            message => lastLog = message,
            () => reinitializeCalls++,
            _ => feedbackCalls++,
            () => saveCalls++);

        Assert.IsFalse(isResetting);
        Assert.IsTrue(isInitialized);
        Assert.IsTrue(justReset);
        Assert.AreEqual(1, resetPersistentCalls);
        Assert.AreEqual(1, reinitializeCalls);
        Assert.AreEqual(1, feedbackCalls);
        Assert.AreEqual(1, saveCalls);
        Assert.AreEqual("Reset: saving fresh baseline", lastLog);
    }
}
