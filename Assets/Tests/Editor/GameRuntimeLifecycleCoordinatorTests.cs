using NUnit.Framework;

public class GameRuntimeLifecycleCoordinatorTests
{
    [Test]
    public void BroadcastPetStateChanged_InvokesBothCallbacks()
    {
        int petChangedCalls = 0;
        int petFlowChangedCalls = 0;
        GameRuntimeLifecycleCoordinator coordinator = CreateCoordinator(new GameRuntimeLifecycleCallbacks
        {
            OnPetChanged = () => petChangedCalls++,
            OnPetFlowChanged = () => petFlowChangedCalls++
        });

        coordinator.BroadcastPetStateChanged();

        Assert.AreEqual(1, petChangedCalls);
        Assert.AreEqual(1, petFlowChangedCalls);
    }

    [Test]
    public void HandleApplicationResume_WhenResetTriggered_SavesAndRefreshesUi()
    {
        int saveCalls = 0;
        int missionUiCalls = 0;
        int updateUiCalls = 0;
        GameRuntimeLifecycleCoordinator coordinator = CreateCoordinator(new GameRuntimeLifecycleCallbacks
        {
            ApplyDailyResetWindowIfNeeded = _ => true,
            SaveGame = () => saveCalls++,
            UpdateMissionUi = () => missionUiCalls++,
            UpdateUi = () => updateUiCalls++
        });

        coordinator.HandleApplicationResume(false, true, "resume");

        Assert.AreEqual(1, saveCalls);
        Assert.AreEqual(1, missionUiCalls);
        Assert.AreEqual(1, updateUiCalls);
    }

    [Test]
    public void SyncUiFromRuntime_InvokesNotifyAndRefreshCallbacks()
    {
        int notifyCalls = 0;
        int updateUiCalls = 0;
        int missionUiCalls = 0;
        int onboardingUiCalls = 0;
        GameRuntimeLifecycleCoordinator coordinator = CreateCoordinator(new GameRuntimeLifecycleCallbacks
        {
            NotifyAllUi = () => notifyCalls++,
            UpdateUi = () => updateUiCalls++,
            UpdateMissionUi = () => missionUiCalls++,
            UpdateOnboardingUi = () => onboardingUiCalls++
        });

        coordinator.SyncUiFromRuntime();

        Assert.AreEqual(1, notifyCalls);
        Assert.AreEqual(1, updateUiCalls);
        Assert.AreEqual(1, missionUiCalls);
        Assert.AreEqual(1, onboardingUiCalls);
    }

    [Test]
    public void Tick_WhenJustReset_ClearsFlagAndSkipsResetCallbacks()
    {
        int resetChecks = 0;
        bool justReset = true;
        GameRuntimeLifecycleCoordinator coordinator = CreateCoordinator(new GameRuntimeLifecycleCallbacks
        {
            ApplyDailyResetWindowIfNeeded = _ =>
            {
                resetChecks++;
                return false;
            }
        });

        coordinator.Tick(0.5f, 0.5f, ref justReset);

        Assert.IsFalse(justReset);
        Assert.AreEqual(0, resetChecks);
    }

    private static GameRuntimeLifecycleCoordinator CreateCoordinator(GameRuntimeLifecycleCallbacks callbacks)
    {
        return new GameRuntimeLifecycleCoordinator(
            new PetSystem(new PetData
            {
                hunger = 100f,
                mood = 100f,
                energy = 100f,
                hasIndependentStats = true,
                statusText = "Happy"
            }),
            new FocusSystem(),
            null,
            new PetData
            {
                hunger = 100f,
                mood = 100f,
                energy = 100f,
                hasIndependentStats = true,
                statusText = "Happy"
            },
            new BalanceConfig
            {
                hungerDrainPerSecond = 1f,
                lowHungerMoodThreshold = 30f,
                lowEnergyMoodThreshold = 20f,
                moodDecayPerSecondWhenHungry = 0.5f,
                moodDecayPerSecondWhenTired = 0.5f
            },
            null,
            null,
            null,
            callbacks);
    }
}
