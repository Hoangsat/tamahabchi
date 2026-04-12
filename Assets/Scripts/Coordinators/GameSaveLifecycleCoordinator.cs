using System;

public sealed class GameSaveLifecycleCoordinator
{
    private readonly Action<SaveData> saveState;
    private readonly Action resetPersistentState;

    public GameSaveLifecycleCoordinator(Action<SaveData> saveState, Action resetPersistentState)
    {
        this.saveState = saveState;
        this.resetPersistentState = resetPersistentState;
    }

    public void SaveGame(Func<SaveData> createSaveDataSnapshot, PetData petData, CurrencyData currencyData, Action<string> logLifecycle)
    {
        if (createSaveDataSnapshot == null)
        {
            return;
        }

        SaveData data = createSaveDataSnapshot();
        if (data == null)
        {
            return;
        }

        float hunger = petData != null ? petData.hunger : 0f;
        int coins = currencyData != null ? currencyData.coins : 0;
        logLifecycle?.Invoke($"SaveGame: hunger={hunger}, coins={coins}, lastSeenUtc={data.lastSeenUtc}, lastResetBucket={data.lastResetBucket}");
        saveState?.Invoke(data);
    }

    public void HandleApplicationPause(bool pause, bool isResetting, bool isInitialized, Action saveGame, Action onResume)
    {
        if (pause)
        {
            if (!isResetting && isInitialized)
            {
                saveGame?.Invoke();
            }

            return;
        }

        onResume?.Invoke();
    }

    public void HandleApplicationQuit(bool isResetting, bool isInitialized, Action saveGame)
    {
        if (isResetting || !isInitialized)
        {
            return;
        }

        saveGame?.Invoke();
    }

    public void ResetGame(ref bool isResetting, ref bool isInitialized, ref bool justReset, Action<string> logLifecycle, Action reinitializeRuntimeState, Action<string> showFeedback, Action saveGame)
    {
        isResetting = true;
        isInitialized = false;
        justReset = true;
        logLifecycle?.Invoke("Reset: clearing save");

        resetPersistentState?.Invoke();

        logLifecycle?.Invoke("Reset: initializing default state");
        reinitializeRuntimeState?.Invoke();

        logLifecycle?.Invoke("Reset: saving fresh baseline");
        showFeedback?.Invoke("Game reset");
        saveGame?.Invoke();
        isInitialized = true;
        isResetting = false;
    }
}
