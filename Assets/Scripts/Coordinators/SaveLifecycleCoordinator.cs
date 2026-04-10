using System;

public sealed class SaveLifecycleCoordinator
{
    private readonly SaveManager saveManager = new SaveManager();

    public SaveData LoadState()
    {
        return saveManager.Load();
    }

    public void SaveState(SaveData data)
    {
        if (data == null)
        {
            return;
        }

        saveManager.Save(data);
    }

    public void ResetPersistentState()
    {
        saveManager.Reset();
    }

    public void HandleApplicationPause(bool pause, bool isResetting, bool isInitialized, Action beforeSave, Action saveAction)
    {
        if (!pause || isResetting || !isInitialized)
        {
            return;
        }

        beforeSave?.Invoke();
        saveAction?.Invoke();
    }

    public void HandleApplicationQuit(bool isResetting, bool isInitialized, Action saveAction)
    {
        if (isResetting || !isInitialized)
        {
            return;
        }

        saveAction?.Invoke();
    }

    public void RunResetSequence(Action beginReset, Action reinitializeRuntimeState, Action finishReset)
    {
        beginReset?.Invoke();
        saveManager.Reset();
        reinitializeRuntimeState?.Invoke();
        finishReset?.Invoke();
    }
}
