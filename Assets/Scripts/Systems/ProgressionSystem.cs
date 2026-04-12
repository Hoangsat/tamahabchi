using UnityEngine;

public class ProgressionSystem
{
    private readonly ProgressionData progressionData;

    public ProgressionSystem(ProgressionData progressionData, int xpToNextLevel, int buyUnlockLevel)
    {
        this.progressionData = progressionData;
        // Guard only — do NOT overwrite data that was loaded from save
        NormalizeState();
    }

    public int GetLevel()
    {
        if (progressionData == null) return 1;
        return Mathf.Max(1, progressionData.level);
    }

    public int GetXp()
    {
        if (progressionData == null) return 0;
        return Mathf.Max(0, progressionData.xp);
    }

    public int GetFocusReward(int baseReward)
    {
        return baseReward;
    }

    public bool IsBuyUnlocked()
    {
        return true;
    }

    private void NormalizeState()
    {
        if (progressionData == null) return;
        // Only enforce floor values — never forcibly reset to 1/0.
        progressionData.level = Mathf.Max(1, progressionData.level);
        progressionData.xp = Mathf.Max(0, progressionData.xp);
    }
}
